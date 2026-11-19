using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// หลอดเลือดมอนสเตอร์แบบแถบบนจอ (โหมดเหมือง Cave3)
/// - แสดงด้านบนกลางจอ หนึ่งแถวต่อมอนสเตอร์หนึ่งตัวที่โผล่ออกมาแล้ว ไม่บังหินในฉาก
/// - เลือดลดแบบลื่นๆ พร้อมแถบสีอ่อนค้างแสดงดาเมจที่เพิ่งโดน แถวกระพริบตอนโดนตี
/// - มอนสเตอร์ตายแล้วแถวค่อยๆ จางหายไป
/// </summary>
public static class MonsterHealthHud
{
    public class Row
    {
        internal RectTransform root;
        internal CanvasGroup group;
        internal Image frame, fill, ghost;
        internal TextMeshProUGUI hpText;
        internal float target = 1f, shown = 1f, ghostValue = 1f, ghostHold, flash;
        internal float max = 1f;
        internal bool removing;

        /// <summary>อัปเดตเลือด (เรียกตอนโดนดาเมจ)</summary>
        public void Set(float current, float maxHp)
        {
            max = Mathf.Max(1f, maxHp);
            float pct = Mathf.Clamp01(current / max);
            if (pct < target) { flash = 1f; ghostHold = 0.45f; }
            target = pct;
            hpText.text = $"{Mathf.CeilToInt(Mathf.Max(0f, current))}/{Mathf.CeilToInt(max)}";
        }

        /// <summary>มอนสเตอร์ตาย: ค่อยๆ จางแล้วลบแถว</summary>
        public void Remove()
        {
            removing = true;
            target = 0f;
        }
    }

    private static Canvas canvas;
    private static RectTransform list;
    private static Sprite rounded;
    private static int counter;
    private static readonly List<Row> rows = new List<Row>();

    private const float RowWidth = 300f, RowHeight = 40f;
    /// <summary>ความสูงแถบ HUD ด้านบนจอ (หน่วยของจอ 1920x1080)</summary>
    public const float BandHeight = 112f;
    private static readonly Color FrameColor = new Color(0.09f, 0.07f, 0.08f, 0.88f);
    private static readonly Color TrackColor = new Color(0.22f, 0.12f, 0.13f, 1f);
    private static readonly Color FillFull = new Color(0.95f, 0.27f, 0.22f, 1f);
    private static readonly Color FillLow = new Color(1f, 0.62f, 0.15f, 1f);
    private static readonly Color GhostColor = new Color(1f, 0.92f, 0.65f, 0.9f);

    public static Row Register(float current, float maxHp)
    {
        EnsureCanvas();
        counter++;

        var row = new Row();
        row.root = NewRect($"MonsterRow{counter}", list);
        row.root.sizeDelta = new Vector2(RowWidth, RowHeight);
        var le = row.root.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = RowWidth; le.preferredHeight = RowHeight;
        row.group = row.root.gameObject.AddComponent<CanvasGroup>();
        row.group.blocksRaycasts = false;

        row.frame = AddImage(row.root, "Frame", FrameColor);
        Stretch(row.frame.rectTransform, 0f, 0f, 0f, 0f);

        // ป้ายชื่อด้านซ้าย
        var label = AddText(row.root, "Label", $"MON {counter}", 16f, new Color(1f, 0.85f, 0.75f, 1f), TextAlignmentOptions.MidlineLeft);
        Anchor(label.rectTransform, 0f, 0f, 0f, 1f, new Vector2(10f, 0f), new Vector2(72f, 0f));

        // ราง + แถบดาเมจค้าง + แถบเลือด
        Image track = AddImage(row.root, "Track", TrackColor);
        Anchor(track.rectTransform, 0f, 0f, 1f, 1f, new Vector2(76f, 9f), new Vector2(-66f, -9f));
        row.ghost = AddFilled(track.rectTransform, "Ghost", GhostColor);
        row.fill = AddFilled(track.rectTransform, "Fill", FillFull);

        // ไฮไลต์ด้านบนของแถบให้ดูมีมิติ
        Image shine = AddImage(row.fill.rectTransform, "Shine", new Color(1f, 1f, 1f, 0.22f));
        Anchor(shine.rectTransform, 0f, 0.55f, 1f, 1f, new Vector2(2f, 0f), new Vector2(-2f, -2f));

        row.hpText = AddText(row.root, "Hp", "", 16f, Color.white, TextAlignmentOptions.MidlineRight);
        Anchor(row.hpText.rectTransform, 1f, 0f, 1f, 1f, new Vector2(-62f, 0f), new Vector2(-10f, 0f));

        row.Set(current, maxHp);
        row.shown = row.ghostValue = row.target;
        rows.Add(row);
        Apply(row);
        return row;
    }

    private static void Tick(float dt)
    {
        for (int i = rows.Count - 1; i >= 0; i--)
        {
            Row r = rows[i];
            if (r.root == null) { rows.RemoveAt(i); continue; }

            r.shown = Mathf.MoveTowards(r.shown, r.target, dt * 2.5f);
            if (r.ghostHold > 0f) r.ghostHold -= dt;
            else r.ghostValue = Mathf.MoveTowards(r.ghostValue, r.shown, dt * 0.9f);
            r.flash = Mathf.Max(0f, r.flash - dt * 4f);

            if (r.removing && r.ghostValue <= r.shown + 0.001f)
            {
                r.group.alpha -= dt * 2.5f;
                if (r.group.alpha <= 0f)
                {
                    Object.Destroy(r.root.gameObject);
                    rows.RemoveAt(i);
                    continue;
                }
            }
            Apply(r);
        }
    }

    private static void Apply(Row r)
    {
        r.fill.fillAmount = r.shown;
        r.ghost.fillAmount = Mathf.Max(r.ghostValue, r.shown);
        // เลือดน้อยเปลี่ยนเป็นสีส้ม เตือนว่าใกล้ตาย
        Color c = Color.Lerp(FillLow, FillFull, Mathf.InverseLerp(0.15f, 0.6f, r.shown));
        r.fill.color = Color.Lerp(c, Color.white, r.flash * 0.6f);
        r.frame.color = Color.Lerp(FrameColor, new Color(0.55f, 0.12f, 0.1f, 0.95f), r.flash);
        r.root.localScale = Vector3.one * (1f + r.flash * 0.04f);
    }

    #region UI building
    private class Runner : MonoBehaviour
    {
        private void Update() => Tick(Time.deltaTime);
        private void OnDestroy() { rows.Clear(); counter = 0; canvas = null; list = null; }
    }

    /// <summary>
    /// แถบ HUD ด้านบนจอที่ใช้ร่วมกัน (หลอดเลือดผู้เล่นซ้าย / มอนสเตอร์ขวา)
    /// กล้องด่านถ้ำเว้นพื้นที่ว่างเหนือแผนที่ไว้ให้ แถบนี้จึงไม่บังหิน
    /// </summary>
    public static RectTransform Band
    {
        get { EnsureCanvas(); return band; }
    }

    public static Sprite RoundedSprite
    {
        get { if (rounded == null) rounded = MakeRounded(32, 8); return rounded; }
    }

    private static RectTransform band;

    private static void EnsureCanvas()
    {
        if (canvas != null) return;

        var go = new GameObject("CaveTopHud");
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<Runner>();

        if (rounded == null) rounded = MakeRounded(32, 8);

        // แถบพื้นหลังด้านบนเต็มความกว้าง
        band = NewRect("Band", go.transform);
        band.anchorMin = new Vector2(0f, 1f);
        band.anchorMax = new Vector2(1f, 1f);
        band.pivot = new Vector2(0.5f, 1f);
        band.sizeDelta = new Vector2(0f, BandHeight);
        Image bandBg = band.gameObject.AddComponent<Image>();
        bandBg.color = new Color(0.05f, 0.045f, 0.05f, 0.92f);
        bandBg.raycastTarget = false;
        Image edge = AddImage(band, "Edge", new Color(0.78f, 0.55f, 0.25f, 0.9f));
        edge.sprite = null;
        edge.rectTransform.anchorMin = new Vector2(0f, 0f);
        edge.rectTransform.anchorMax = new Vector2(1f, 0f);
        edge.rectTransform.sizeDelta = new Vector2(0f, 3f);
        edge.rectTransform.anchoredPosition = Vector2.zero;

        // หลอดเลือดมอนสเตอร์เรียงจากขวาไปซ้าย ในแถบด้านบน
        list = NewRect("Rows", band);
        list.anchorMin = list.anchorMax = new Vector2(1f, 0.5f);
        list.pivot = new Vector2(1f, 0.5f);
        list.anchoredPosition = new Vector2(-28f, 0f);
        var layout = list.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        var fitter = list.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    internal static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    internal static Image AddImage(RectTransform parent, string name, Color color)
    {
        Image img = NewRect(name, parent).gameObject.AddComponent<Image>();
        img.sprite = rounded;
        img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    internal static Image AddFilled(RectTransform parent, string name, Color color)
    {
        Image img = NewRect(name, parent).gameObject.AddComponent<Image>();
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = 0;
        img.color = color;
        img.raycastTarget = false;
        Stretch(img.rectTransform, 2f, 2f, 2f, 2f);
        return img;
    }

    internal static TextMeshProUGUI AddText(RectTransform parent, string name, string text, float size, Color color, TextAlignmentOptions align)
    {
        var t = NewRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.color = color;
        t.alignment = align;
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        t.outlineWidth = 0.2f;
        t.outlineColor = new Color32(0, 0, 0, 255);
        return t;
    }

    internal static void Stretch(RectTransform r, float l, float b, float rgt, float t)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(l, b); r.offsetMax = new Vector2(-rgt, -t);
    }

    internal static void Anchor(RectTransform r, float minX, float minY, float maxX, float maxY, Vector2 offMin, Vector2 offMax)
    {
        r.anchorMin = new Vector2(minX, minY); r.anchorMax = new Vector2(maxX, maxY);
        r.offsetMin = offMin; r.offsetMax = offMax;
    }

    // สี่เหลี่ยมมุมมน (9-slice) ใช้ทั้งกรอบ ราง และแถบเลือด
    private static Sprite MakeRounded(int size, int radius)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius), cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(radius - d + 0.5f) * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }
    #endregion
}
