using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ตกแต่งหน้า QuestBoard (สร้างด้วยโค้ดตอนเริ่มฉาก ไม่ต้องจัดใน Scene)
/// - ป้ายไม้ "QUEST BOARD" บนคานบอร์ด
/// - กรอบเรืองแสงสีทองรอบกระดาษเควสที่เลือก (เลื่อนตามอย่างนุ่มนวล + เต้นเบาๆ)
/// - แถบด้านล่าง: ชื่อเควสที่เลือก + วิธีควบคุมด้วยแผ่นเหยียบ + เกจเหยียบค้างเพื่อกลับเมนู
/// - แสงตะเกียง/เทียนกะพริบ และฝุ่นละอองลอยในร้าน
/// วางไว้ที่ GameObject เดียวกับ QuestMenuController
/// </summary>
[RequireComponent(typeof(QuestMenuController))]
public class QuestBoardDecor : MonoBehaviour
{
    [Header("Title Sign")]
    [SerializeField] private Sprite titleSprite;
    [SerializeField] private Vector2 titlePosition = new Vector2(1f, 140f);
    [SerializeField] private Vector2 titleSize = new Vector2(176f, 37f);

    [Header("Selected Quest Glow")]
    [SerializeField] private Color glowColor = new Color(1f, 0.8f, 0.3f, 1f);
    [SerializeField] private float glowPadding = 9f;

    [Header("Bottom Bar")]
    [SerializeField] private float barHeight = 30f;
    // เหยียบขึ้นครั้งเดียว = เริ่ม / เหยียบลงค้าง 2 วินาที = กลับหน้าแรก
    private const string ControlsHint = "LEFT / RIGHT : CHOOSE          STEP UP (W / ENTER) : START          HOLD DOWN 2s (ESC) : BACK";

    [Header("Ambience")]
    [Tooltip("ตำแหน่งแสงไฟ (พิกัดบน Canvas จากกึ่งกลาง) x, y = ตำแหน่ง, z = ขนาด")]
    [SerializeField] private Vector3[] lights =
    {
        new Vector3(-212f, 75f, 70f),   // ตะเกียงซ้ายของบอร์ด
        new Vector3(215f, 17f, 70f),    // ตะเกียงขวาของบอร์ด
        new Vector3(313f, 32f, 120f),   // เตาผิง
        new Vector3(-267f, 55f, 50f),   // เทียนบนเคาน์เตอร์
        new Vector3(-283f, -66f, 55f),  // เทียนโต๊ะซ้าย
        new Vector3(296f, -21f, 50f),   // เทียนโต๊ะขวา
    };
    [SerializeField] private Color lightColor = new Color(1f, 0.62f, 0.25f, 0.28f);
    [SerializeField] private int dustCount = 26;

    private QuestMenuController menu;
    private RectTransform canvasRect;
    private TMP_FontAsset font;
    private Image glowFrame;
    private TextMeshProUGUI questNameText;
    private Image holdBack, holdFill;
    private TextMeshProUGUI holdLabel;
    private readonly List<Image> lightImages = new List<Image>();
    private readonly List<RectTransform> dust = new List<RectTransform>();
    private readonly List<Vector3> dustState = new List<Vector3>(); // x = ความเร็วขึ้น, y = เฟสส่าย, z = ความสว่าง
    private int shownIndex = -1;
    private Vector2 glowPos, glowSize;
    private static Sprite whiteSprite, glowRingSprite, softSprite;

    private void Start()
    {
        menu = GetComponent<QuestMenuController>();
        if (menu.quests == null || menu.quests.Length == 0 || menu.quests[0].boardPosition == null) return;
        Canvas canvas = menu.quests[0].boardPosition.GetComponentInParent<Canvas>().rootCanvas;
        canvasRect = (RectTransform)canvas.transform;

        TMP_Text anyText = canvas.GetComponentInChildren<TMP_Text>(true);
        font = anyText != null ? anyText.font : TMP_Settings.defaultFontAsset;

        BuildSprites();
        int afterBackground = 1; // ลูกตัวแรกของ Canvas คือภาพพื้นหลัง

        // แสงไฟ (อยู่เหนือพื้นหลัง ใต้กระดาษ)
        foreach (Vector3 l in lights)
        {
            Image img = NewImage("AmbientLight", softSprite, lightColor);
            Place(img.rectTransform, new Vector2(l.x, l.y), new Vector2(l.z, l.z));
            img.transform.SetSiblingIndex(afterBackground++);
            lightImages.Add(img);
        }

        // กรอบเรืองแสงรอบเควสที่เลือก (ใต้ภาพ/ข้อความบนกระดาษ)
        glowFrame = NewImage("SelectedQuestGlow", glowRingSprite, glowColor);
        glowFrame.type = Image.Type.Sliced;
        glowFrame.pixelsPerUnitMultiplier = 2f; // Canvas นี้เล็ก (กว้าง 689) ลดความหนาของขอบเรืองแสงลงครึ่งหนึ่ง
        glowFrame.transform.SetSiblingIndex(afterBackground++);

        // ป้ายชื่อบอร์ด
        if (titleSprite != null)
        {
            Image title = NewImage("QuestBoardTitle", titleSprite, Color.white);
            title.preserveAspect = true;
            Place(title.rectTransform, titlePosition, titleSize);
        }

        BuildDust();
        BuildBottomBar();
        SnapGlow();
    }

    private void Update()
    {
        if (menu == null || glowFrame == null) return;
        float t = Time.unscaledTime;

        // กรอบเลือก: เลื่อนตามเควสที่เลือก + เต้นเบาๆ
        if (menu.CurrentIndex != shownIndex) UpdateTarget();
        RectTransform g = glowFrame.rectTransform;
        g.anchoredPosition = Vector2.Lerp(g.anchoredPosition, glowPos, Time.unscaledDeltaTime * 14f);
        float pulse = 1f + 0.04f * Mathf.Sin(t * 4f);
        g.sizeDelta = Vector2.Lerp(g.sizeDelta, glowSize * pulse, Time.unscaledDeltaTime * 14f);
        Color gc = glowColor; gc.a = 0.7f + 0.3f * Mathf.Sin(t * 4f);
        glowFrame.color = gc;

        // ชื่อเควสในแถบล่าง ค่อยๆ เด้งเข้า
        questNameText.transform.localScale = Vector3.Lerp(questNameText.transform.localScale, Vector3.one, Time.unscaledDeltaTime * 12f);

        // เกจเหยียบค้างเพื่อกลับเมนู
        float hold = menu.HoldProgress;
        bool showHold = hold > 0.12f;
        holdBack.gameObject.SetActive(showHold);
        holdLabel.gameObject.SetActive(showHold);
        holdFill.fillAmount = hold;

        // แสงไฟกะพริบแบบเปลวเทียน
        for (int i = 0; i < lightImages.Count; i++)
        {
            float n = Mathf.PerlinNoise(t * 3.2f, i * 7.3f);
            Color c = lightColor; c.a = lightColor.a * (0.55f + 0.7f * n);
            lightImages[i].color = c;
            lightImages[i].transform.localScale = Vector3.one * (0.92f + 0.16f * n);
        }

        // ฝุ่นลอยขึ้นช้าๆ ส่ายไปมา
        Vector2 half = canvasRect.rect.size * 0.5f;
        for (int i = 0; i < dust.Count; i++)
        {
            Vector3 st = dustState[i];
            Vector2 p = dust[i].anchoredPosition;
            p.y += st.x * Time.unscaledDeltaTime;
            p.x += Mathf.Sin(t * 0.7f + st.y) * 4f * Time.unscaledDeltaTime;
            if (p.y > half.y + 4f) { p.y = -half.y - 4f; p.x = Random.Range(-half.x, half.x); }
            dust[i].anchoredPosition = p;
            Image img = dust[i].GetComponent<Image>();
            Color c = img.color; c.a = st.z * (0.5f + 0.5f * Mathf.Sin(t * 1.3f + st.y * 3f));
            img.color = c;
        }
    }

    private void UpdateTarget()
    {
        shownIndex = menu.CurrentIndex;
        Rect r = QuestRect(shownIndex);
        glowPos = r.center;
        glowSize = r.size + Vector2.one * glowPadding * 2f;
        string name = menu.quests[shownIndex].questName;
        questNameText.text = string.IsNullOrEmpty(name) ? "" : name.ToUpperInvariant();
        questNameText.transform.localScale = Vector3.one * 1.25f;
    }

    private void SnapGlow()
    {
        UpdateTarget();
        glowFrame.rectTransform.anchoredPosition = glowPos;
        glowFrame.rectTransform.sizeDelta = glowSize;
        questNameText.transform.localScale = Vector3.one;
    }

    // กรอบของกระดาษเควสในพิกัด Canvas (คิดจากมุมทั้งสี่ของ boardPosition รวมสเกล/การหมุน)
    private Rect QuestRect(int index)
    {
        RectTransform rt = menu.quests[index].boardPosition as RectTransform;
        if (rt == null) return new Rect(Vector2.zero, Vector2.one * 100f);
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
        foreach (Vector3 c in corners)
        {
            Vector2 local = canvasRect.InverseTransformPoint(c);
            min = Vector2.Min(min, local); max = Vector2.Max(max, local);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private void BuildBottomBar()
    {
        Image bar = NewImage("ControlsBar", whiteSprite, new Color(0.09f, 0.05f, 0.02f, 0.82f));
        RectTransform br = bar.rectTransform;
        br.anchorMin = new Vector2(0f, 0f); br.anchorMax = new Vector2(1f, 0f);
        br.pivot = new Vector2(0.5f, 0f);
        br.sizeDelta = new Vector2(0f, barHeight);
        br.anchoredPosition = Vector2.zero;

        Image line = NewImage("GoldLine", whiteSprite, new Color(0.86f, 0.62f, 0.2f, 0.9f), br);
        line.rectTransform.anchorMin = new Vector2(0f, 1f); line.rectTransform.anchorMax = new Vector2(1f, 1f);
        line.rectTransform.pivot = new Vector2(0.5f, 1f);
        line.rectTransform.sizeDelta = new Vector2(0f, 1.2f);
        line.rectTransform.anchoredPosition = Vector2.zero;

        questNameText = NewText("SelectedQuestName", br, 13f, new Color(1f, 0.85f, 0.42f), TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        RectTransform qn = questNameText.rectTransform;
        qn.anchorMin = new Vector2(0f, 0f); qn.anchorMax = new Vector2(0.4f, 1f);
        qn.offsetMin = new Vector2(14f, 0f); qn.offsetMax = Vector2.zero;
        qn.pivot = new Vector2(0f, 0.5f);

        TextMeshProUGUI hint = NewText("ControlsHint", br, 8f, new Color(1f, 0.93f, 0.78f, 0.92f), TextAlignmentOptions.MidlineRight, FontStyles.Bold);
        hint.text = ControlsHint;
        hint.enableAutoSizing = true;
        hint.fontSizeMin = 4f;
        hint.fontSizeMax = 8f;
        RectTransform hr = hint.rectTransform;
        hr.anchorMin = new Vector2(0.38f, 0f); hr.anchorMax = new Vector2(1f, 1f);
        hr.offsetMin = Vector2.zero; hr.offsetMax = new Vector2(-14f, 0f);

        // เกจเหยียบค้าง: ลอยเหนือแถบด้านขวา
        holdBack = NewImage("HoldGaugeBack", whiteSprite, new Color(0.09f, 0.05f, 0.02f, 0.85f));
        RectTransform hb = holdBack.rectTransform;
        hb.anchorMin = hb.anchorMax = new Vector2(1f, 0f);
        hb.pivot = new Vector2(1f, 0f);
        hb.sizeDelta = new Vector2(130f, 8f);
        hb.anchoredPosition = new Vector2(-14f, barHeight + 6f);
        holdFill = NewImage("Fill", whiteSprite, new Color(1f, 0.8f, 0.3f, 1f), hb);
        holdFill.type = Image.Type.Filled;
        holdFill.fillMethod = Image.FillMethod.Horizontal;
        holdFill.rectTransform.anchorMin = Vector2.zero; holdFill.rectTransform.anchorMax = Vector2.one;
        holdFill.rectTransform.offsetMin = new Vector2(1f, 1f); holdFill.rectTransform.offsetMax = new Vector2(-1f, -1f);
        holdLabel = NewText("HoldLabel", canvasRect, 7f, new Color(1f, 0.93f, 0.78f), TextAlignmentOptions.BottomRight, FontStyles.Bold);
        holdLabel.text = "KEEP HOLDING DOWN TO GO BACK...";
        RectTransform hl = holdLabel.rectTransform;
        hl.anchorMin = hl.anchorMax = new Vector2(1f, 0f);
        hl.pivot = new Vector2(1f, 0f);
        hl.sizeDelta = new Vector2(160f, 12f);
        hl.anchoredPosition = new Vector2(-14f, barHeight + 15f);
        holdBack.gameObject.SetActive(false);
        holdLabel.gameObject.SetActive(false);
    }

    private void BuildDust()
    {
        Vector2 half = canvasRect.rect.size * 0.5f;
        for (int i = 0; i < dustCount; i++)
        {
            float size = Random.Range(1.2f, 2.6f);
            Image d = NewImage("Dust", softSprite, new Color(1f, 0.86f, 0.6f, 0f));
            Place(d.rectTransform, new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y)), Vector2.one * size);
            dust.Add(d.rectTransform);
            dustState.Add(new Vector3(Random.Range(3f, 9f), Random.Range(0f, 6.28f), Random.Range(0.25f, 0.6f)));
        }
    }

    private Image NewImage(string name, Sprite sprite, Color color, RectTransform parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent != null ? parent : canvasRect, false);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private TextMeshProUGUI NewText(string name, RectTransform parent, float size, Color color, TextAlignmentOptions align, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = style;
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        return t;
    }

    private static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void BuildSprites()
    {
        if (whiteSprite != null) return;
        Texture2D white = Texture2D.whiteTexture;
        whiteSprite = Sprite.Create(white, new Rect(0, 0, white.width, white.height), new Vector2(0.5f, 0.5f), 100f);

        // วงแหวนเรืองแสงรอบสี่เหลี่ยมขอบมน (9-slice ยืดได้ทุกขนาด)
        const int size = 64, border = 24;
        var ring = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // ระยะจากขอบสี่เหลี่ยมด้านใน (เส้นกรอบอยู่ห่างจากขอบภาพ 12px)
                float dx = Mathf.Max(Mathf.Abs(x + 0.5f - size / 2f) - (size / 2f - 12f), 0f);
                float dy = Mathf.Max(Mathf.Abs(y + 0.5f - size / 2f) - (size / 2f - 12f), 0f);
                float outside = Mathf.Sqrt(dx * dx + dy * dy);
                float inner = Mathf.Min(size / 2f - 12f - Mathf.Abs(x + 0.5f - size / 2f), size / 2f - 12f - Mathf.Abs(y + 0.5f - size / 2f));
                float d = outside > 0f ? outside : -inner; // ระยะมีเครื่องหมายจากเส้นกรอบ
                float core = Mathf.Clamp01(1.6f - Mathf.Abs(d)) ;          // เส้นคม
                float halo = Mathf.Exp(-Mathf.Abs(d) / 4.5f) * 0.65f;      // แสงฟุ้ง
                float a = Mathf.Clamp01(Mathf.Max(core, halo));
                if (d < -10f) a = 0f; // ด้านในกรอบโปร่งใส ไม่ทับกระดาษ
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        ring.SetPixels32(px);
        ring.Apply(false, true);
        glowRingSprite = Sprite.Create(ring, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));

        // จุดแสงฟุ้งกลม
        const int s = 64;
        var soft = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var sp = new Color32[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(s / 2f, s / 2f)) / (s / 2f);
                float a = Mathf.Clamp01(1f - d);
                sp[y * s + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
            }
        soft.SetPixels32(sp);
        soft.Apply(false, true);
        softSprite = Sprite.Create(soft, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }
}
