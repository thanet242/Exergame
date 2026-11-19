using System.Collections.Generic;
using UnityEngine;

namespace CookieRun
{
    /// <summary>
    /// ปรับ Hitbox ให้ตรงกับรูปทรงที่มองเห็นจริงของ Sprite (ตัดส่วนโปร่งใสทิ้ง)
    /// - ใช้ Physics Shape ที่ Unity สร้างจากค่า Alpha ของรูป (Generate Physics Shape ใน Import Settings)
    /// - หดเข้าหาจุดกึ่งกลางอีกเล็กน้อย เพื่อให้ Hitbox ไม่เกินขอบที่ตาเห็นเด็ดขาด
    /// - แทนที่ BoxCollider2D เดิมด้วย PolygonCollider2D อัตโนมัติ
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public class CookieSpriteHitbox : MonoBehaviour
    {
        [Tooltip("สัดส่วนขนาด Hitbox เทียบกับรูปทรงที่มองเห็น (1 = เท่าขอบรูปพอดี, 0.9 = เล็กกว่าขอบ 10% ให้ผู้เล่นพลาดได้นิดหน่อย)")]
        [Range(0.5f, 1f)]
        [SerializeField] private float hitboxScale = 0.9f;

        private SpriteRenderer spriteRenderer;
        private PolygonCollider2D polygon;
        private Sprite fittedSprite;
        private bool fittedFlipX;
        private bool fittedFlipY;

        private static readonly List<Vector2> ShapeBuffer = new List<Vector2>(64);

        public Collider2D Hitbox => polygon;

        private void Awake()
        {
            Fit();
        }

        /// <summary>
        /// สร้าง/อัปเดต Hitbox ตาม Sprite ปัจจุบัน (เรียกซ้ำได้เมื่อเปลี่ยน Sprite หรือกลับด้าน)
        /// </summary>
        public void Fit()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            Sprite sprite = spriteRenderer.sprite;
            if (sprite == null) return;

            if (polygon == null) CreatePolygon();

            if (sprite == fittedSprite && spriteRenderer.flipX == fittedFlipX && spriteRenderer.flipY == fittedFlipY) return;
            fittedSprite = sprite;
            fittedFlipX = spriteRenderer.flipX;
            fittedFlipY = spriteRenderer.flipY;

            // จุดกึ่งกลางของรูป ใช้เป็นจุดหดเข้า
            Vector2 center = sprite.bounds.center;
            float sx = spriteRenderer.flipX ? -1f : 1f;
            float sy = spriteRenderer.flipY ? -1f : 1f;

            int shapeCount = sprite.GetPhysicsShapeCount();
            if (shapeCount > 0)
            {
                polygon.pathCount = shapeCount;
                for (int i = 0; i < shapeCount; i++)
                {
                    sprite.GetPhysicsShape(i, ShapeBuffer);
                    for (int p = 0; p < ShapeBuffer.Count; p++)
                    {
                        Vector2 pt = center + (ShapeBuffer[p] - center) * hitboxScale;
                        ShapeBuffer[p] = new Vector2(pt.x * sx, pt.y * sy);
                    }
                    polygon.SetPath(i, ShapeBuffer);
                }
            }
            else
            {
                // ไม่มี Physics Shape: ใช้กรอบรูปทั้งภาพแทน (หดตามสัดส่วนเหมือนกัน)
                Vector2 half = (Vector2)sprite.bounds.extents * hitboxScale;
                Vector2 c = new Vector2(center.x * sx, center.y * sy);
                ShapeBuffer.Clear();
                ShapeBuffer.Add(c + new Vector2(-half.x, -half.y));
                ShapeBuffer.Add(c + new Vector2(half.x, -half.y));
                ShapeBuffer.Add(c + new Vector2(half.x, half.y));
                ShapeBuffer.Add(c + new Vector2(-half.x, half.y));
                polygon.pathCount = 1;
                polygon.SetPath(0, ShapeBuffer);
            }
        }

        private void CreatePolygon()
        {
            polygon = GetComponent<PolygonCollider2D>();
            bool isTrigger = true;

            // ปิด BoxCollider2D เดิม (ไม่ลบทิ้ง เพราะ CookieObstacle ต้องมี Collider2D เสมอ)
            BoxCollider2D box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                isTrigger = box.isTrigger;
                box.enabled = false;
            }

            if (polygon == null) polygon = gameObject.AddComponent<PolygonCollider2D>();
            polygon.isTrigger = isTrigger;

            // แจ้ง CookieObstacle ให้ใช้ Hitbox ใหม่ (ใช้ตอนปิด/เปิด Collider เมื่อชนหรือถูกนำกลับมาใช้)
            CookieObstacle obstacle = GetComponent<CookieObstacle>();
            if (obstacle != null) obstacle.SetHitbox(polygon);
        }
    }
}
