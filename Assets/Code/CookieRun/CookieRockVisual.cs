using UnityEngine;

namespace CookieRun
{
    /// <summary>
    /// ตกแต่งหินกีดขวางให้กลมกลืนกับฉากถ้ำและมองเห็นชัด
    /// - ย้อมสีหินให้เข้ากับโทนถ้ำ
    /// - เส้นขอบเข้มรอบหิน (Outline) ให้หินเด่นจากฉากหลัง
    /// - เงาวงรีใต้หิน ให้หินดูวางอยู่บนพื้นจริง
    /// - สุ่มรูปหินและกลับด้านทุกครั้งที่ Chunk ถูกสร้าง/นำกลับมาใช้ ฉากจะไม่ซ้ำจำเจ
    /// ฐานหินจะอยู่ระดับเดิมเสมอแม้รูปหินแต่ละแบบสูงไม่เท่ากัน และ Hitbox จะปรับตามรูปที่สุ่มได้
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CookieSpriteHitbox))]
    public class CookieRockVisual : MonoBehaviour, ICookiePoolable
    {
        [Header("Variants (รูปหินที่สุ่มได้)")]
        [Tooltip("รูปหินที่สุ่มใช้ (ว่าง = ใช้รูปเดิมของ SpriteRenderer)")]
        [SerializeField] private Sprite[] spriteVariants;
        [SerializeField] private bool randomFlipX = true;

        [Header("Color")]
        [Tooltip("สีย้อมหิน (โทนฟ้าอมเทาเข้ากับถ้ำ)")]
        [SerializeField] private Color tint = new Color(0.78f, 0.82f, 0.95f, 1f);

        [Header("Outline")]
        [SerializeField] private bool enableOutline = true;
        [SerializeField] private Color outlineColor = new Color(0.04f, 0.05f, 0.1f, 0.9f);
        [Tooltip("ความหนาเส้นขอบ (ยูนิต) 0.02 = 2 พิกเซลของรูป 100 PPU")]
        [SerializeField] private float outlineWidth = 0.02f;

        [Header("Ground Shadow")]
        [SerializeField] private bool enableShadow = true;
        [SerializeField] private Color shadowColor = new Color(0f, 0f, 0.05f, 0.55f);
        [Tooltip("ความกว้างเงาเทียบกับความกว้างหิน")]
        [SerializeField] private float shadowWidthRatio = 1.15f;
        [SerializeField] private float shadowHeight = 0.16f;

        private static readonly Vector2[] OutlineDirections =
        {
            new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f)
        };

        private SpriteRenderer spriteRenderer;
        private CookieSpriteHitbox hitbox;
        private SpriteRenderer[] outlineRenderers;
        private SpriteRenderer shadowRenderer;

        private Vector3 initialLocalPosition;
        private float baseBottomY;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            hitbox = GetComponent<CookieSpriteHitbox>();

            // ให้หินอยู่หน้าเส้นขอบและเงาเสมอ (พื้นดินใช้ Sorting Order 1)
            if (spriteRenderer.sortingOrder < 4) spriteRenderer.sortingOrder = 4;

            initialLocalPosition = transform.localPosition;
            baseBottomY = spriteRenderer.sprite != null ? spriteRenderer.sprite.bounds.min.y * transform.localScale.y : 0f;

            BuildDecorations();
            ApplyRandomLook();
        }

        public void OnSpawnFromPool()
        {
            ApplyRandomLook();
        }

        private void BuildDecorations()
        {
            if (enableShadow)
            {
                GameObject shadowObj = new GameObject("RockShadow");
                shadowObj.transform.SetParent(transform, false);
                shadowRenderer = shadowObj.AddComponent<SpriteRenderer>();
                shadowRenderer.sprite = CookieFxTextures.SoftCircleSprite;
                shadowRenderer.color = shadowColor;
                shadowRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
                shadowRenderer.sortingOrder = spriteRenderer.sortingOrder - 2;
            }

            if (enableOutline)
            {
                outlineRenderers = new SpriteRenderer[OutlineDirections.Length];
                for (int i = 0; i < OutlineDirections.Length; i++)
                {
                    GameObject outlineObj = new GameObject("RockOutline");
                    outlineObj.transform.SetParent(transform, false);
                    outlineObj.transform.localPosition = OutlineDirections[i] * outlineWidth;

                    SpriteRenderer sr = outlineObj.AddComponent<SpriteRenderer>();
                    sr.color = outlineColor;
                    sr.sortingLayerID = spriteRenderer.sortingLayerID;
                    sr.sortingOrder = spriteRenderer.sortingOrder - 1;
                    outlineRenderers[i] = sr;
                }
            }
        }

        private void ApplyRandomLook()
        {
            if (spriteVariants != null && spriteVariants.Length > 0)
            {
                Sprite pick = spriteVariants[Random.Range(0, spriteVariants.Length)];
                if (pick != null) spriteRenderer.sprite = pick;
            }

            if (randomFlipX) spriteRenderer.flipX = Random.value < 0.5f;
            // ย้อมสีตามโซนปัจจุบัน (ถ้ำเห็ด / เหมือง / ลาวา / น้ำแข็ง)
            spriteRenderer.color = tint * CookieZoneManager.CurrentRockTint;

            Sprite sprite = spriteRenderer.sprite;
            if (sprite == null) return;

            // คงระดับฐานหินไว้ที่เดิม แม้รูปที่สุ่มได้จะสูงต่ำไม่เท่ากัน
            float bottomY = sprite.bounds.min.y * transform.localScale.y;
            transform.localPosition = initialLocalPosition + new Vector3(0f, baseBottomY - bottomY, 0f);

            if (outlineRenderers != null)
            {
                for (int i = 0; i < outlineRenderers.Length; i++)
                {
                    outlineRenderers[i].sprite = sprite;
                    outlineRenderers[i].flipX = spriteRenderer.flipX;
                }
            }

            if (shadowRenderer != null)
            {
                Bounds b = sprite.bounds;
                float centerX = spriteRenderer.flipX ? -b.center.x : b.center.x;
                shadowRenderer.transform.localPosition = new Vector3(centerX, b.min.y + shadowHeight * 0.15f, 0f);
                shadowRenderer.transform.localScale = new Vector3(b.size.x * shadowWidthRatio, shadowHeight, 1f);
            }

            hitbox.Fit();
        }
    }
}
