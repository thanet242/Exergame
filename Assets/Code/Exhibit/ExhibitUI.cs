using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ตัวช่วยสร้าง UI ด้วยโค้ดของระบบงานแสดง (ป้ายเริ่มเล่น / สอนเล่น / หยุดเกม / แผ่นเหยียบมุมจอ)
/// ใช้ฟอนต์ไทยของ Windows (Leelawadee UI) และสไปรต์ขอบมน/ลูกศรที่วาดด้วยโค้ด
/// </summary>
public static class ExhibitUI
{
    public static readonly Color Ink = new Color(0.97f, 0.95f, 0.9f, 1f);
    public static readonly Color Muted = new Color(0.78f, 0.74f, 0.68f, 1f);
    public static readonly Color Gold = new Color(1f, 0.78f, 0.3f, 1f);
    public static readonly Color Panel = new Color(0.1f, 0.07f, 0.05f, 0.94f);
    public static readonly Color Line = new Color(0.86f, 0.62f, 0.2f, 1f);

    // สีของแผ่นเหยียบแต่ละทิศ (เหมือนผังแผ่นเหยียบในคู่มือ)
    public static readonly Color PadUpColor = new Color(0.1f, 0.76f, 1f);
    public static readonly Color PadDownColor = new Color(1f, 0.31f, 0.55f);
    public static readonly Color PadLeftColor = new Color(0.55f, 0.89f, 0.24f);
    public static readonly Color PadRightColor = new Color(1f, 0.65f, 0.17f);

    private static Font font;
    private static Sprite roundSprite, arrowSprite, starSprite;

    public static Font Font
    {
        get
        {
            if (font == null) font = Font.CreateDynamicFontFromOSFont(new[] { "Leelawadee UI", "Leelawadee", "Tahoma", "Segoe UI", "Arial" }, 40);
            return font;
        }
    }

    /// <summary>Canvas เต็มจอ อ้างอิง 1920x1080 อยู่บนสุดตาม sortingOrder</summary>
    public static RectTransform NewCanvas(string name, int sortingOrder, Transform parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        if (parent != null) go.transform.SetParent(parent, false);
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return (RectTransform)go.transform;
    }

    public static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>กล่องขอบมน (ขอบสีทองบางๆ ถ้าใส่สีขอบ)</summary>
    public static Image NewPanel(string name, Transform parent, Color fill, Color border, float borderWidth = 3f)
    {
        Image img = NewImage(name, parent, RoundSprite, fill);
        img.type = Image.Type.Sliced;
        if (border.a > 0f)
        {
            Outline o = img.gameObject.AddComponent<Outline>();
            o.effectColor = border;
            o.effectDistance = new Vector2(borderWidth, -borderWidth);
            Outline o2 = img.gameObject.AddComponent<Outline>();
            o2.effectColor = border;
            o2.effectDistance = new Vector2(-borderWidth, borderWidth);
        }
        return img;
    }

    public static Text NewText(string name, Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text t = go.GetComponent<Text>();
        t.font = Font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = anchor;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        // ขอบดำบางๆ ให้อ่านออกบนพื้นหลังทุกแบบ
        Outline o = go.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.6f);
        o.effectDistance = new Vector2(1.5f, -1.5f);
        return t;
    }

    /// <summary>วางจุดกึ่งกลางที่ (x จากกึ่งกลาง, y จากกึ่งกลาง) ของพ่อ</summary>
    public static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    /// <summary>ผังแผ่นเหยียบ 4 ทิศ (ลูกศรในช่องสี) labels: ข้อความใต้ลูกศรแต่ละทิศ (null = แผ่นนั้นจาง)</summary>
    public static Dictionary<int, Image> NewPadDiagram(Transform parent, float x, float y, float cell, string up, string down, string left, string right)
    {
        var root = new GameObject("PadDiagram", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        Place((RectTransform)root.transform, x, y, cell * 3.2f, cell * 3.2f);
        var tiles = new Dictionary<int, Image>();
        // 0 = บน, 1 = ลง, 2 = ซ้าย, 3 = ขวา
        float step = cell * 1.06f;
        Vector2[] pos = { new Vector2(0, step), new Vector2(0, -step), new Vector2(-step, 0), new Vector2(step, 0) };
        Color[] colors = { PadUpColor, PadDownColor, PadLeftColor, PadRightColor };
        string[] labels = { up, down, left, right };
        float[] angles = { 0f, 180f, 90f, -90f };
        Image center = NewPanel("Center", root.transform, new Color(0.3f, 0.27f, 0.36f, 0.9f), Color.clear);
        Place(center.rectTransform, 0, 0, cell, cell);
        for (int i = 0; i < 4; i++)
        {
            bool used = labels[i] != null;
            Color c = colors[i];
            Image tile = NewPanel("Pad" + i, root.transform, used ? c : new Color(c.r, c.g, c.b, 0.18f), Color.clear);
            Place(tile.rectTransform, pos[i].x, pos[i].y, cell, cell);
            Image arrow = NewImage("Arrow", tile.transform, ArrowSprite, used ? Color.white : new Color(1, 1, 1, 0.35f));
            Place(arrow.rectTransform, 0, used && labels[i].Length > 0 ? cell * 0.12f : 0f, cell * 0.42f, cell * 0.42f);
            arrow.rectTransform.localEulerAngles = new Vector3(0, 0, angles[i]);
            if (used && labels[i].Length > 0)
            {
                Text t = NewText("Label", tile.transform, labels[i], Mathf.RoundToInt(cell * 0.2f), Color.white);
                Place(t.rectTransform, 0, -cell * 0.28f, cell * 1.1f, cell * 0.34f);
            }
            tiles[i] = tile;
        }
        return tiles;
    }

    /// <summary>สี่เหลี่ยมขอบมน 64px ใช้แบบ 9-slice</summary>
    public static Sprite RoundSprite
    {
        get
        {
            if (roundSprite != null) return roundSprite;
            const int size = 64, r = 18;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0, Mathf.Max(r - x - 0.5f, x + 0.5f - (size - r)));
                float dy = Mathf.Max(0, Mathf.Max(r - y - 0.5f, y + 0.5f - (size - r)));
                float a = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            roundSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            return roundSprite;
        }
    }

    /// <summary>ดาว 5 แฉก (หน้าสรุปผล 1-3 ดาว)</summary>
    public static Sprite StarSprite
    {
        get
        {
            if (starSprite != null) return starSprite;
            const int size = 96;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            // จุดยอด 10 จุด สลับรัศมีนอก/ใน แล้วเช็กจุดอยู่ในรูปหลายเหลี่ยม (ขอบเรียบด้วยการสุ่ม 4 จุดต่อพิกเซล)
            var poly = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float ang = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float rad = (i % 2 == 0 ? 0.48f : 0.2f) * size;
                poly[i] = new Vector2(size / 2f + Mathf.Cos(ang) * rad, size / 2f + 2f + Mathf.Sin(ang) * rad);
            }
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int s = 0; s < 4; s++)
                    if (InPolygon(poly, new Vector2(x + 0.25f + (s % 2) * 0.5f, y + 0.25f + (s / 2) * 0.5f))) hits++;
                px[y * size + x] = new Color32(255, 255, 255, (byte)(hits * 255 / 4));
            }
            tex.SetPixels32(px);
            tex.Apply();
            starSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return starSprite;
        }
    }

    private static bool InPolygon(Vector2[] poly, Vector2 p)
    {
        bool inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                inside = !inside;
        }
        return inside;
    }

    /// <summary>ลูกศรชี้ขึ้น (หมุนเพื่อชี้ทิศอื่น)</summary>
    public static Sprite ArrowSprite
    {
        get
        {
            if (arrowSprite != null) return arrowSprite;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float fx = (x + 0.5f) / size, fy = (y + 0.5f) / size;
                // หัวลูกศร: สามเหลี่ยมครึ่งบน / ก้าน: สี่เหลี่ยมครึ่งล่าง
                bool head = fy >= 0.45f && Mathf.Abs(fx - 0.5f) <= (0.98f - fy) * 0.95f;
                bool stem = fy < 0.5f && fy > 0.04f && Mathf.Abs(fx - 0.5f) <= 0.15f;
                px[y * size + x] = head || stem ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
            tex.SetPixels32(px);
            tex.Apply();
            arrowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return arrowSprite;
        }
    }
}
