using UnityEngine;

namespace CookieRun
{
    /// <summary>
    /// ต่อแท่งหินจากโคนหินย้อยขึ้นไปจนถึงเพดาน (ขอบบนจอ) ให้เห็นว่าหินย้อยห้อยลงมาจากเพดานจริงๆ ไม่ได้ลอยกลางอากาศ
    /// - แท่งหินใช้ Sprite แบบ Tiled ยืดตามระยะที่เหลือถึงเพดาน
    /// - ก้อนหินเพดานวางชิดขอบบนสุด ปิดปลายแท่งหิน
    /// - เป็นภาพตกแต่งเท่านั้น ไม่มี Collider (Hitbox ของหินย้อยเหมือนเดิม)
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public class CookieCeilingRoot : MonoBehaviour
    {
        [Tooltip("แท่งหินที่ต่อกันได้ในแนวตั้ง")]
        [SerializeField] private Sprite rootSprite;
        [Tooltip("ก้อนหินเพดานที่ปิดปลายด้านบน")]
        [SerializeField] private Sprite capSprite;
        [Tooltip("ความสูงของเพดานใน World (ควรสูงกว่าขอบบนของจอเล็กน้อย)")]
        [SerializeField] private float ceilingY = 5.9f;
        [Tooltip("ระยะที่แท่งหินซ้อนลงไปในโคนหินย้อย ไม่ให้เห็นช่องว่าง")]
        [SerializeField] private float overlap = 0.12f;
        [Tooltip("ความกว้างของแท่งหินเทียบกับภาพ (1 = เท่าภาพ)")]
        [SerializeField] private float widthScale = 1f;

        private void Start()
        {
            Build();
        }

        private void Build()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr.sprite == null || rootSprite == null) return;

            float top = sr.bounds.max.y - overlap;
            float height = ceilingY - top;
            if (height <= 0.05f) return;

            Vector3 lossy = transform.lossyScale;
            Vector3 inverse = new Vector3(1f / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)), 1f / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)), 1f);
            float centerX = sr.bounds.center.x;

            var rootObj = new GameObject("CeilingRoot");
            rootObj.transform.SetParent(transform, false);
            rootObj.transform.localScale = new Vector3(inverse.x * widthScale, inverse.y, 1f);
            rootObj.transform.position = new Vector3(centerX, top + height * 0.5f, transform.position.z);
            SpriteRenderer rootSr = rootObj.AddComponent<SpriteRenderer>();
            rootSr.sprite = rootSprite;
            rootSr.drawMode = SpriteDrawMode.Tiled;
            rootSr.tileMode = SpriteTileMode.Continuous;
            rootSr.size = new Vector2(rootSprite.bounds.size.x, height);
            rootSr.sortingLayerID = sr.sortingLayerID;
            rootSr.sortingOrder = sr.sortingOrder - 2;
            rootSr.color = sr.color;

            if (capSprite == null) return;
            var capObj = new GameObject("CeilingCap");
            capObj.transform.SetParent(transform, false);
            capObj.transform.localScale = inverse;
            // วางก้อนหินให้ขอบบนอยู่ที่เพดานพอดี
            capObj.transform.position = new Vector3(centerX, ceilingY - capSprite.bounds.max.y, transform.position.z);
            SpriteRenderer capSr = capObj.AddComponent<SpriteRenderer>();
            capSr.sprite = capSprite;
            capSr.sortingLayerID = sr.sortingLayerID;
            capSr.sortingOrder = sr.sortingOrder - 1;
            capSr.color = sr.color;
        }
    }
}
