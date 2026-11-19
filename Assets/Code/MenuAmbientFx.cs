using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// เอฟเฟกต์เคลื่อนไหวทับภาพพื้นหลังของหน้าเมนู (สร้างด้วยโค้ดทั้งหมด ไม่ต้องใช้วิดีโอ)
/// - MainMenu: ไฟตะเกียงกะพริบ แสงอาทิตย์เรืองพร้อมรัศมีหมุนช้าๆ เมฆลอย หิ่งห้อย ใบไม้ปลิว และภาพซูมเข้าออกช้าๆ
/// - Tavern (QuestBoard): ประกายไฟลอยจากเตาผิง ลำแสงจากหน้าต่าง เปลวเทียนไหว และควันจางๆ
/// ตำแหน่งทั้งหมดกำหนดเป็นสัดส่วนของภาพพื้นหลัง (0-1) จึงตรงกับภาพเสมอไม่ว่าจอจะขนาดไหน
/// เลเยอร์เอฟเฟกต์อยู่เหนือภาพพื้นหลังแต่อยู่ใต้ปุ่มและป้ายต่างๆ และไม่รับการคลิก
/// </summary>
public class MenuAmbientFx : MonoBehaviour
{
    public enum Preset { MainMenu, Tavern }

    [SerializeField] private Preset preset = Preset.MainMenu;
    [Tooltip("ภาพพื้นหลังที่จะวางเอฟเฟกต์ทับ")]
    [SerializeField] private RectTransform background;
    [Tooltip("ซูมภาพพื้นหลังเข้าออกช้าๆ (0 = ปิด, 0.03 = ขยายสุด 3%)")]
    [SerializeField] private float breatheZoom = 0.025f;
    [SerializeField] private float breathePeriod = 24f;

    private RectTransform layer;
    private Vector3 bgBaseScale;
    private static Sprite softSprite, raySprite, leafSprite, beamSprite;

    // ---------- ชิ้นเอฟเฟกต์ ----------
    private class Light { public Image img; public Color color; public float size, speed, seed, strength; }
    private class Mote { public Image img; public Vector2 home, pos, vel; public float life, age, size, seed; public Color color; }

    private readonly List<Light> lights = new List<Light>();
    private readonly List<Mote> fireflies = new List<Mote>();
    private readonly List<Mote> leaves = new List<Mote>();
    private readonly List<Mote> embers = new List<Mote>();
    private readonly List<Mote> clouds = new List<Mote>();
    private readonly List<Image> beams = new List<Image>();
    private Image sunRays;

    private void Start()
    {
        if (background == null)
        {
            Debug.LogWarning("[MenuAmbientFx] ยังไม่ได้กำหนดภาพพื้นหลัง");
            enabled = false;
            return;
        }

        // หน้าเควสมีกระดาษเควสวางตรงบอร์ดในภาพ ถ้าซูมภาพ กระดาษจะไม่ตรงบอร์ด
        if (preset == Preset.Tavern) breatheZoom = 0f;

        BuildSprites();
        BuildLayer();
        if (preset == Preset.MainMenu) BuildMainMenu(); else BuildTavern();
    }

    // เลเยอร์ขนาดเท่าภาพพื้นหลัง วางเป็นลำดับถัดจากภาพ (อยู่ใต้ UI อื่นทั้งหมด)
    private void BuildLayer()
    {
        var go = new GameObject("AmbientFx", typeof(RectTransform));
        layer = (RectTransform)go.transform;
        layer.SetParent(background.parent, false);
        layer.SetSiblingIndex(background.GetSiblingIndex() + 1);
        layer.anchorMin = background.anchorMin;
        layer.anchorMax = background.anchorMax;
        layer.pivot = background.pivot;
        layer.anchoredPosition = background.anchoredPosition;
        // ภาพพื้นหลังอาจถูกยืดด้วย Scale: ใช้ขนาดจริงแทน เอฟเฟกต์วงกลมจะได้ไม่บี้
        Vector3 s = background.localScale;
        layer.sizeDelta = new Vector2(background.sizeDelta.x * s.x, background.sizeDelta.y * s.y);
        bgBaseScale = s;
    }

    private Vector2 Size => layer.rect.size;
    private Vector2 ToLocal(Vector2 n) => new Vector2((n.x - layer.pivot.x) * Size.x, (n.y - layer.pivot.y) * Size.y);

    private Image NewImage(string name, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(layer, false);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private void AddLight(Vector2 n, float size, Color color, float speed = 6f, float strength = 0.45f)
    {
        Image img = NewImage("Glow", softSprite, color);
        img.rectTransform.anchoredPosition = ToLocal(n);
        lights.Add(new Light { img = img, color = color, size = size, speed = speed, seed = Random.value * 100f, strength = strength });
    }

    #region Presets
    private void BuildMainMenu()
    {
        // แสงอาทิตย์ยามเย็น + รัศมีหมุนช้าๆ
        sunRays = NewImage("SunRays", raySprite, new Color(1f, 0.85f, 0.55f, 0.22f));
        sunRays.rectTransform.anchoredPosition = ToLocal(new Vector2(0.872f, 0.84f));
        float raySize = Size.x * 0.42f;
        sunRays.rectTransform.sizeDelta = new Vector2(raySize, raySize);
        AddLight(new Vector2(0.872f, 0.84f), 0.2f, new Color(1f, 0.78f, 0.45f, 0.35f), 0.6f, 0.25f);

        // เมฆบางๆ ลอยผ่านท้องฟ้า (เฉพาะฝั่งขวาที่เห็นท้องฟ้า)
        for (int i = 0; i < 4; i++)
        {
            Image img = NewImage("Cloud", softSprite, new Color(1f, 0.88f, 0.75f, 0f));
            clouds.Add(new Mote { img = img, pos = new Vector2(0.7f + 0.1f * i, Random.Range(0.8f, 0.96f)), vel = new Vector2(Random.Range(0.004f, 0.008f), 0f), size = Random.Range(0.1f, 0.16f), seed = Random.value * 10f });
        }

        // ตะเกียง 3 ดวง
        Color lamp = new Color(1f, 0.68f, 0.28f, 0.55f);
        AddLight(new Vector2(0.415f, 0.425f), 0.085f, lamp);
        AddLight(new Vector2(0.598f, 0.505f), 0.085f, lamp);
        AddLight(new Vector2(0.962f, 0.555f), 0.075f, lamp);

        // หิ่งห้อยลอยไปมาบริเวณพื้นและพุ่มไม้
        for (int i = 0; i < 22; i++)
        {
            Image img = NewImage("Firefly", softSprite, Color.clear);
            Vector2 home = new Vector2(Random.Range(0.02f, 0.98f), Random.Range(0.06f, 0.5f));
            fireflies.Add(new Mote { img = img, home = home, pos = home, size = Random.Range(0.008f, 0.014f), seed = Random.value * 100f, color = new Color(1f, 0.95f, 0.55f, 1f) });
        }

        // ใบไม้ร่วงจากเถาวัลย์ฝั่งซ้ายและต้นไม้ฝั่งขวา
        for (int i = 0; i < 9; i++)
        {
            Image img = NewImage("Leaf", leafSprite, Color.clear);
            Mote m = new Mote { img = img, seed = Random.value * 100f };
            ResetLeaf(m, true);
            leaves.Add(m);
        }
    }

    private void BuildTavern()
    {
        // ลำแสงส่องจากหน้าต่าง (ไล่จากบนลงล่าง เอียงซ้าย)
        for (int i = 0; i < 3; i++)
        {
            Image b = NewImage("LightShaft", beamSprite, new Color(1f, 0.85f, 0.55f, 0f));
            RectTransform r = b.rectTransform;
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = ToLocal(new Vector2(0.64f + 0.022f * i, 1f));
            r.sizeDelta = new Vector2(Size.x * (0.035f + 0.012f * i), Size.y * 0.75f);
            r.localRotation = Quaternion.Euler(0f, 0f, -24f);
            beams.Add(b);
        }

        // เปลวเทียนและตะเกียง (ไหวเร็วกว่าแสงรอบห้อง)
        Color flame = new Color(1f, 0.8f, 0.4f, 0.7f);
        foreach (Vector2 p in new[] {
                     new Vector2(0.113f, 0.64f), new Vector2(0.088f, 0.335f), new Vector2(0.932f, 0.44f),
                     new Vector2(0.19f, 0.68f), new Vector2(0.81f, 0.535f), new Vector2(0.335f, 0.95f), new Vector2(0.82f, 0.8f) })
            AddLight(p, 0.03f, flame, 11f, 0.6f);

        // เตาผิง: แสงวูบวาบแรง + ประกายไฟลอยขึ้น
        AddLight(new Vector2(0.955f, 0.57f), 0.14f, new Color(1f, 0.5f, 0.15f, 0.45f), 8f, 0.6f);
        for (int i = 0; i < 16; i++)
        {
            Image img = NewImage("Ember", softSprite, Color.clear);
            Mote m = new Mote { img = img, seed = Random.value * 100f };
            ResetEmber(m);
            m.age = Random.value * m.life;
            embers.Add(m);
        }

        // ฝุ่นในลำแสงลอยช้าๆ
        for (int i = 0; i < 14; i++)
        {
            Image img = NewImage("BeamDust", softSprite, Color.clear);
            Vector2 home = new Vector2(Random.Range(0.4f, 0.72f), Random.Range(0.35f, 0.95f));
            fireflies.Add(new Mote { img = img, home = home, pos = home, size = Random.Range(0.004f, 0.007f), seed = Random.value * 100f, color = new Color(1f, 0.92f, 0.75f, 0.8f) });
        }
    }
    #endregion

    private void ResetLeaf(Mote m, bool anywhere)
    {
        bool left = Random.value < 0.65f;
        m.pos = left ? new Vector2(Random.Range(0.0f, 0.28f), Random.Range(0.75f, 1f)) : new Vector2(Random.Range(0.66f, 0.74f), Random.Range(0.6f, 0.78f));
        if (anywhere) m.pos.y -= Random.Range(0f, 0.5f);
        m.vel = new Vector2(Random.Range(0.01f, 0.035f), -Random.Range(0.025f, 0.05f));
        m.size = Random.Range(0.012f, 0.018f);
        m.age = 0f;
        m.color = Random.value < 0.5f ? new Color(0.45f, 0.7f, 0.25f, 1f) : new Color(0.85f, 0.6f, 0.2f, 1f);
    }

    private void ResetEmber(Mote m)
    {
        m.pos = new Vector2(0.955f + Random.Range(-0.025f, 0.025f), 0.54f);
        m.vel = new Vector2(Random.Range(-0.01f, 0.01f), Random.Range(0.05f, 0.1f));
        m.life = Random.Range(1.2f, 2.4f);
        m.age = 0f;
        m.size = Random.Range(0.004f, 0.008f);
    }

    private void Update()
    {
        if (layer == null) return;
        float t = Time.unscaledTime, dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        Vector2 size = Size;

        // ภาพทั้งหมดซูมเข้าออกช้าๆ (ภาพพื้นหลังกับเลเยอร์เอฟเฟกต์ไปพร้อมกัน ตำแหน่งจึงไม่เพี้ยน)
        if (breatheZoom > 0f)
        {
            float z = 1f + breatheZoom * (0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 2f / breathePeriod));
            background.localScale = bgBaseScale * z;
            layer.localScale = new Vector3(z, z, 1f);
        }

        foreach (Light l in lights)
        {
            float n = Mathf.PerlinNoise(l.seed, t * l.speed * 0.25f);
            float k = 1f - l.strength + l.strength * 2f * n;
            Color c = l.color; c.a = l.color.a * k;
            l.img.color = c;
            float s = size.x * l.size * (0.92f + 0.12f * n);
            l.img.rectTransform.sizeDelta = new Vector2(s, s);
        }

        if (sunRays != null) sunRays.rectTransform.localRotation = Quaternion.Euler(0f, 0f, t * 2.5f);

        foreach (Mote m in clouds)
        {
            m.pos += m.vel * dt;
            if (m.pos.x > 1.08f) { m.pos.x = 0.66f; m.pos.y = Random.Range(0.8f, 0.96f); }
            // จางที่ขอบซ้าย (ตัวอาคาร) และขอบขวาของภาพ
            float a = Mathf.Clamp01((m.pos.x - 0.7f) / 0.06f) * Mathf.Clamp01((1.04f - m.pos.x) / 0.06f);
            m.img.color = new Color(1f, 0.9f, 0.8f, 0.16f * a);
            m.img.rectTransform.anchoredPosition = ToLocal(m.pos);
            m.img.rectTransform.sizeDelta = new Vector2(size.x * m.size * 2.6f, size.x * m.size * 0.7f);
        }

        foreach (Mote m in fireflies)
        {
            // วนรอบจุดบ้านแบบสุ่ม + กะพริบเป็นจังหวะ
            m.pos = m.home + new Vector2(
                (Mathf.PerlinNoise(m.seed, t * 0.15f) - 0.5f) * 0.12f,
                (Mathf.PerlinNoise(m.seed + 7f, t * 0.15f) - 0.5f) * 0.08f);
            float blink = Mathf.Clamp01(Mathf.Sin(t * (1.2f + m.seed % 1f) + m.seed) * 1.6f - 0.3f);
            Color c = m.color; c.a *= blink;
            m.img.color = c;
            m.img.rectTransform.anchoredPosition = ToLocal(m.pos);
            float s = size.x * m.size * (1f + blink * 0.6f);
            m.img.rectTransform.sizeDelta = new Vector2(s, s);
        }

        foreach (Mote m in leaves)
        {
            m.age += dt;
            m.pos += new Vector2(m.vel.x + Mathf.Sin(t * 1.7f + m.seed) * 0.02f, m.vel.y) * dt;
            if (m.pos.y < 0.05f || m.pos.x > 1.02f) ResetLeaf(m, false);
            float a = Mathf.Clamp01(m.age / 0.6f) * Mathf.Clamp01((m.pos.y - 0.05f) / 0.12f);
            Color c = m.color; c.a = a;
            m.img.color = c;
            RectTransform r = m.img.rectTransform;
            r.anchoredPosition = ToLocal(m.pos);
            r.sizeDelta = new Vector2(size.x * m.size, size.x * m.size * 0.6f);
            r.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.3f + m.seed) * 50f);
        }

        foreach (Mote m in embers)
        {
            m.age += dt;
            if (m.age >= m.life) ResetEmber(m);
            m.pos += new Vector2(m.vel.x + Mathf.Sin(t * 3f + m.seed) * 0.01f, m.vel.y) * dt;
            float k = m.age / m.life;
            m.img.color = new Color(1f, Mathf.Lerp(0.85f, 0.35f, k), 0.15f, (1f - k) * 0.95f);
            m.img.rectTransform.anchoredPosition = ToLocal(m.pos);
            float s = size.x * m.size * (1f - k * 0.5f);
            m.img.rectTransform.sizeDelta = new Vector2(s, s);
        }

        for (int i = 0; i < beams.Count; i++)
        {
            float a = 0.07f + 0.05f * Mathf.PerlinNoise(i * 3.1f, t * 0.2f);
            beams[i].color = new Color(1f, 0.85f, 0.55f, a);
        }
    }

    #region Sprites
    private static void BuildSprites()
    {
        if (softSprite != null) return;

        softSprite = MakeSprite(64, 64, (u, v) =>
        {
            float d = Mathf.Clamp01(Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f);
            float a = 1f - d; return a * a;
        });

        // รัศมีแสงอาทิตย์: แฉกแสงจางๆ รอบจุดกลาง
        raySprite = MakeSprite(256, 256, (u, v) =>
        {
            Vector2 p = new Vector2(u - 0.5f, v - 0.5f);
            float r = p.magnitude * 2f;
            if (r >= 1f) return 0f;
            float ang = Mathf.Atan2(p.y, p.x);
            float rays = Mathf.Pow(Mathf.Abs(Mathf.Sin(ang * 6f)), 6f) * 0.7f + Mathf.Pow(Mathf.Abs(Mathf.Sin(ang * 11f + 1f)), 10f) * 0.5f;
            return Mathf.Clamp01(rays * (1f - r) * (1f - r) * 1.4f);
        });

        // ใบไม้ทรงรี มีเส้นกลางใบ (ระบายสีด้วย Image.color)
        leafSprite = MakeSprite(20, 12, (u, v) =>
        {
            float x = (u - 0.5f) * 2f, y = (v - 0.5f) * 2f;
            float edge = 1f - x * x;
            if (Mathf.Abs(y) > edge) return 0f;
            return Mathf.Abs(y) < 0.12f ? 0.65f : 1f;
        }, FilterMode.Point);

        // ลำแสง: สว่างตรงกลาง จางที่ขอบซ้ายขวาและปลายล่าง
        beamSprite = MakeSprite(32, 128, (u, v) =>
        {
            float side = 1f - Mathf.Abs(u - 0.5f) * 2f;
            return side * side * Mathf.Clamp01(v * 1.4f);
        });
    }

    private static Sprite MakeSprite(int w, int h, System.Func<float, float, float> alpha, FilterMode filter = FilterMode.Bilinear)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = filter };
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha((x + 0.5f) / w, (y + 0.5f) / h)) * 255f));
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }
    #endregion
}
