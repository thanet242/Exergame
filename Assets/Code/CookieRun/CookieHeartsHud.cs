using UnityEngine;
using UnityEngine.UI;

namespace CookieRun
{
    /// <summary>
    /// แถบหัวใจบน HUD ของโหมดวิ่ง
    /// - หัวใจแดง = หัวใจที่เหลือ, หัวใจมืด = หัวใจที่เสียไป
    /// - หัวใจสีฟ้าต่อท้าย = โล่ (รับการชนแทน 1 ครั้ง)
    /// - เสียหัวใจ: ดวงนั้นสั่นแล้วหด / ได้หัวใจคืน: ดวงนั้นเด้งขยาย / เหลือดวงเดียว: ดวงสุดท้ายเต้นเตือน
    /// </summary>
    public class CookieHeartsHud : MonoBehaviour
    {
        private const float HeartWidth = 64f, HeartHeight = 58f, Spacing = 72f;

        // ลายหัวใจพิกเซล 11x10 (o = ขอบ, h = ไฮไลต์, r = เนื้อ, d = เงา)
        private static readonly string[] Pattern =
        {
            ".ooo...ooo.",
            "ohhro.orrro",
            "ohrrrorrrdo",
            "orrrrrrrrdo",
            "orrrrrrrrdo",
            ".orrrrrrdo.",
            "..orrrrdo..",
            "...orrdo...",
            "....odo....",
            ".....o.....",
        };

        private static Sprite fullSprite, emptySprite, shieldSprite;

        private CookieHealth health;
        private CookiePlayerController player;
        private Image[] hearts;
        private float[] popTimers;
        private bool[] popIsLoss;
        private Image shieldHeart;
        private float shieldPop;
        private int shownHearts = -1;
        private bool shownShield;

        public static CookieHeartsHud Create(RectTransform parent, CookieHealth health, CookiePlayerController player, Vector2 anchoredPosition)
        {
            var obj = new GameObject("HeartsHud", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)obj.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = new Vector2(Spacing * (health.MaxHearts + 1), HeartHeight);

            CookieHeartsHud hud = obj.AddComponent<CookieHeartsHud>();
            hud.Init(health, player);
            return hud;
        }

        private void Init(CookieHealth h, CookiePlayerController p)
        {
            health = h;
            player = p;
            EnsureSprites();

            int max = health.MaxHearts;
            hearts = new Image[max];
            popTimers = new float[max];
            popIsLoss = new bool[max];
            for (int i = 0; i < max; i++) hearts[i] = CreateHeart("Heart" + i, i, fullSprite);

            shieldHeart = CreateHeart("ShieldHeart", max, shieldSprite);
            shieldHeart.gameObject.SetActive(false);

            Refresh(true);
        }

        private Image CreateHeart(string name, int slot, Sprite sprite)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(transform, false);
            RectTransform rt = (RectTransform)obj.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(HeartWidth, HeartHeight);
            rt.anchoredPosition = new Vector2(HeartWidth * 0.5f + slot * Spacing, -HeartHeight * 0.5f);
            Image img = obj.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        private void Update()
        {
            if (health == null) return;
            Refresh(false);
            Animate();
        }

        private void Refresh(bool force)
        {
            int count = health.Hearts;
            if (force || count != shownHearts)
            {
                for (int i = 0; i < hearts.Length; i++)
                {
                    bool filled = i < count;
                    hearts[i].sprite = filled ? fullSprite : emptySprite;
                    if (!force && shownHearts >= 0)
                    {
                        bool lost = i >= count && i < shownHearts;
                        bool gained = i < count && i >= shownHearts;
                        if (lost || gained) { popTimers[i] = 0.45f; popIsLoss[i] = lost; }
                    }
                }
                shownHearts = count;
            }

            bool hasShield = player != null && player.HasShield;
            if (force || hasShield != shownShield)
            {
                shieldHeart.gameObject.SetActive(hasShield);
                if (hasShield) shieldPop = 0.45f;
                shownShield = hasShield;
            }
        }

        private void Animate()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < hearts.Length; i++)
            {
                RectTransform rt = hearts[i].rectTransform;
                float baseX = HeartWidth * 0.5f + i * Spacing;
                float scale = 1f, shake = 0f;

                if (popTimers[i] > 0f)
                {
                    popTimers[i] -= dt;
                    float t = Mathf.Clamp01(popTimers[i] / 0.45f);
                    if (popIsLoss[i]) { shake = Mathf.Sin(t * 60f) * 8f * t; scale = 1f + 0.25f * t; }
                    else scale = 1f + 0.5f * t;
                }
                else if (shownHearts == 1 && i == 0)
                {
                    // เหลือหัวใจดวงเดียว: เต้นเตือน
                    scale = 1f + 0.12f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f));
                }

                rt.anchoredPosition = new Vector2(baseX + shake, -HeartHeight * 0.5f);
                rt.localScale = Vector3.one * scale;
            }

            if (shieldHeart.gameObject.activeSelf)
            {
                float s = 1f;
                if (shieldPop > 0f) { shieldPop -= dt; s = 1f + 0.5f * Mathf.Clamp01(shieldPop / 0.45f); }
                else s = 1f + 0.05f * Mathf.Sin(Time.unscaledTime * 3f);
                shieldHeart.rectTransform.localScale = Vector3.one * s;

                // โล่ใกล้หมดเวลา: หัวใจสีฟ้ากะพริบพร้อมฟองโล่
                float alpha = 1f;
                if (player != null && player.IsShieldExpiring)
                {
                    float period = player.ShieldBlinkPeriod;
                    alpha = Mathf.Repeat(Time.time, period) < period * 0.6f ? 1f : 0.2f;
                }
                shieldHeart.color = new Color(1f, 1f, 1f, alpha);
            }
        }

        private static void EnsureSprites()
        {
            if (fullSprite != null) return;
            fullSprite = BuildSprite(new Color32(45, 8, 16, 255), new Color32(255, 214, 222, 255), new Color32(232, 38, 64, 255), new Color32(150, 14, 38, 255));
            emptySprite = BuildSprite(new Color32(25, 14, 18, 230), new Color32(92, 74, 82, 200), new Color32(58, 44, 52, 200), new Color32(44, 32, 40, 200));
            shieldSprite = BuildSprite(new Color32(6, 24, 52, 255), new Color32(214, 246, 255, 255), new Color32(40, 170, 255, 255), new Color32(18, 96, 190, 255));
        }

        private static Sprite BuildSprite(Color32 outline, Color32 highlight, Color32 fill, Color32 shade)
        {
            int w = Pattern[0].Length, h = Pattern.Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                string row = Pattern[h - 1 - y]; // Texture เริ่มนับจากแถวล่าง
                for (int x = 0; x < w; x++)
                {
                    Color32 c;
                    switch (row[x])
                    {
                        case 'o': c = outline; break;
                        case 'h': c = highlight; break;
                        case 'r': c = fill; break;
                        case 'd': c = shade; break;
                        default: c = new Color32(0, 0, 0, 0); break;
                    }
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 10f);
        }
    }
}
