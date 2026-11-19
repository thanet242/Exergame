using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// หน้าสรุปผล (GAME OVER / STAGE CLEAR) สร้างด้วยโค้ด ใช้ภาพม้วนกระดาษชุดเดียวกับฉากถ้ำ
/// ช่อง Time / Traps Cleared / Calories Burned และปุ่ม Home / Restart อยู่ในภาพแล้ว โค้ดนี้วางตัวเลขและตัวเลือกปุ่มทับ
/// การควบคุม (เหมือนหน้าสรุปผลของฉากถ้ำ): ซ้าย/ขวา = เลือกปุ่ม, เหยียบขึ้น 2 ครั้ง หรือ Enter = ยืนยัน
/// </summary>
public class ResultPopupUI : MonoBehaviour
{
    public enum Kind { GameOver, StageClear }

    public struct Data
    {
        public float seconds;
        public string middleValue;     // ช่อง Traps Cleared
        public string middleSubText;   // บรรทัดเล็กใต้ค่าตรงกลาง
        public float calories;
        public string extraLine;       // บรรทัดเพิ่มเติมใต้หัวข้อ (เช่น คะแนน / เหรียญ)
        public Sprite panelOverride;   // ใช้ภาพม้วนกระดาษอื่นแทนภาพมาตรฐาน (เช่น หัวข้อช่องกลางต่างไป)
        public string statsLine;       // สรุปการเคลื่อนไหวทั้งหมด (แถบใต้ม้วนกระดาษ)
    }

    // ตำแหน่งในภาพ (สัดส่วน 0-1 จากมุมซ้ายบน) ของช่องข้อมูลและปุ่มที่วาดไว้ในภาพ
    private static readonly Rect TimeBox = new Rect(0.16f, 0.465f, 0.186f, 0.205f);
    private static readonly Rect TrapBox = new Rect(0.408f, 0.465f, 0.183f, 0.205f);
    private static readonly Rect CalorieBox = new Rect(0.652f, 0.465f, 0.186f, 0.205f);
    private static readonly Rect HomeButton = new Rect(0.293f, 0.729f, 0.186f, 0.101f);
    private static readonly Rect RestartButton = new Rect(0.523f, 0.729f, 0.186f, 0.101f);
    private static readonly Rect ExtraLine = new Rect(0.2f, 0.335f, 0.6f, 0.05f);

    private const KeyCode PadUp = KeyCode.Joystick1Button5;
    private const KeyCode PadLeft = KeyCode.Joystick1Button8;
    private const KeyCode PadRight = KeyCode.Joystick1Button3;
    private const KeyCode PadAction = KeyCode.Joystick1Button0;

    [Tooltip("หน่วงเวลาก่อนรับปุ่ม กันเท้าที่ยังเหยียบแผ่นอยู่กดโดยไม่ตั้งใจ")]
    public float inputDelay = 0.8f;
    public float doubleTapThreshold = 0.5f;

    private Action onHome;
    private Action onRestart;
    private Action onNext;
    private Image[] highlights;
    private int selected = 1;               // เริ่มที่ Restart
    private float shownAt;
    private float lastUpTime = -10f;
    private bool done;
    private AudioSource audioSource;

    private static ResultPopupUI current;

    /// <summary>
    /// แสดงหน้าสรุปผล (ปิดหน้าเดิมถ้ามี)
    /// onNext: ถ้าใส่มา ปุ่มขวาจะเป็น "Next" (ไปด่านต่อไป) แทน Restart
    /// </summary>
    public static ResultPopupUI Show(Kind kind, Data data, Action onHome, Action onRestart, Action onNext = null)
    {
        if (current != null) Destroy(current.gameObject);

        var go = new GameObject(kind == Kind.GameOver ? "ResultPopup_GameOver" : "ResultPopup_StageClear");
        current = go.AddComponent<ResultPopupUI>();
        current.onHome = onHome;
        current.onRestart = onRestart;
        current.onNext = onNext;
        current.Build(kind, data);
        return current;
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
    }

    private void Build(Kind kind, Data data)
    {
        UIArtLibrary art = UIArtLibrary.Instance;

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.ignoreListenerPause = true;

        RectTransform root = (RectTransform)transform;

        // พื้นหลังมืด
        Image dim = NewImage("Dim", root, null, new Color(0f, 0f, 0f, 0.6f));
        Stretch(dim.rectTransform);

        // ภาพม้วนกระดาษ (อัตราส่วน 1672x941)
        Sprite sprite = data.panelOverride;
        if (sprite == null && art != null)
        {
            if (kind == Kind.GameOver) sprite = art.gameOverSprite;
            else sprite = onNext != null && art.stageClearNextSprite != null ? art.stageClearNextSprite : art.stageClearSprite;
        }
        bool hasStats = !string.IsNullOrEmpty(data.statsLine);
        Image panel = NewImage("Panel", root, sprite, Color.white);
        RectTransform pr = panel.rectTransform;
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        // มีแถบสรุปการเคลื่อนไหว: ย่อม้วนกระดาษลงเล็กน้อยแล้วยกขึ้น เพื่อเว้นที่ด้านล่าง
        float panelWidth = hasStats ? 1380f : 1500f;
        pr.sizeDelta = new Vector2(panelWidth, panelWidth * 941f / 1672f);
        pr.anchoredPosition = new Vector2(0f, hasStats ? 50f : 10f);

        if (hasStats) BuildStatsStrip(root, data.statsLine);

        Color valueColor = new Color(0.29f, 0.14f, 0.04f, 1f);
        int minutes = Mathf.FloorToInt(data.seconds / 60f);
        int secs = Mathf.FloorToInt(data.seconds % 60f);
        NewText("TimeValue", pr, TimeBox, $"{minutes:00}:{secs:00}", 64f, valueColor);
        string middle = string.IsNullOrEmpty(data.middleSubText) ? data.middleValue : $"{data.middleValue}\n<size=55%>{data.middleSubText}</size>";
        NewText("TrapValue", pr, TrapBox, middle, 64f, valueColor);
        NewText("CalorieValue", pr, CalorieBox, $"{data.calories:0.0}<size=55%> kcal</size>", 64f, valueColor);
        if (!string.IsNullOrEmpty(data.extraLine))
        {
            NewText("Extra", pr, ExtraLine, data.extraLine, 30f, new Color(0.45f, 0.2f, 0.05f, 1f));
        }

        // กรอบเลือกปุ่ม (เรืองทอง) + ปุ่มกดด้วยเมาส์
        highlights = new Image[2];
        highlights[0] = NewButtonOverlay("Home", pr, HomeButton, 0);
        highlights[1] = NewButtonOverlay("Restart", pr, RestartButton, 1);

        // คำแนะนำปุ่ม
        Image hintBg = NewImage("HintBg", root, null, new Color(0f, 0f, 0f, 0.55f));
        RectTransform hb = hintBg.rectTransform;
        hb.anchorMin = new Vector2(0f, 0f); hb.anchorMax = new Vector2(1f, 0f);
        hb.pivot = new Vector2(0.5f, 0f);
        hb.sizeDelta = new Vector2(0f, 60f);
        TextMeshProUGUI hint = NewText("Hint", hb, new Rect(0f, 0f, 1f, 1f), "LEFT / RIGHT : SELECT        STEP UP TWICE (OR ENTER) : CONFIRM", 28f, new Color(1f, 0.88f, 0.55f, 1f));
        hint.rectTransform.anchorMin = Vector2.zero; hint.rectTransform.anchorMax = Vector2.one;
        hint.rectTransform.offsetMin = hint.rectTransform.offsetMax = Vector2.zero;

        shownAt = Time.unscaledTime;
        RefreshHighlight();

        // เด้งเข้า
        pr.localScale = Vector3.one * 0.85f;
    }

    private void Update()
    {
        // เด้งเข้า + ตัวเลือกกะพริบ
        RectTransform panel = (RectTransform)transform.Find("Panel");
        if (panel != null) panel.localScale = Vector3.Lerp(panel.localScale, Vector3.one, Time.unscaledDeltaTime * 10f);
        float pulse = 0.35f + 0.2f * Mathf.Sin(Time.unscaledTime * 6f);
        for (int i = 0; i < highlights.Length; i++)
        {
            Color c = highlights[i].color;
            c.a = i == selected ? pulse : 0f;
            highlights[i].color = c;
        }

        if (done || Time.unscaledTime - shownAt < inputDelay) return;

        if (Input.GetKeyDown(PadLeft) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Select(selected - 1);
        else if (Input.GetKeyDown(PadRight) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Select(selected + 1);

        if (Input.GetKeyDown(PadUp) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            if (Time.unscaledTime - lastUpTime <= doubleTapThreshold) { Confirm(); return; }
            lastUpTime = Time.unscaledTime;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(PadAction)) Confirm();
    }

    private void Select(int index)
    {
        selected = (index + 2) % 2;
        PlayClip(UIArtLibrary.Instance != null ? UIArtLibrary.Instance.selectSound : null);
        RefreshHighlight();
    }

    private void RefreshHighlight()
    {
        for (int i = 0; i < highlights.Length; i++)
        {
            highlights[i].rectTransform.localScale = i == selected ? new Vector3(1.06f, 1.12f, 1f) : Vector3.one;
        }
    }

    private void Confirm()
    {
        if (done) return;
        done = true;
        PlayClip(UIArtLibrary.Instance != null ? UIArtLibrary.Instance.confirmSound : null);
        Time.timeScale = 1f;
        if (selected == 0) onHome?.Invoke();
        else if (onNext != null) onNext.Invoke();
        else onRestart?.Invoke();
    }

    /// <summary>แถบไม้ใต้ม้วนกระดาษ แสดงสรุปการเคลื่อนไหวทั้งหมดของรอบนี้</summary>
    private void BuildStatsStrip(RectTransform root, string statsLine)
    {
        // แผ่นไม้สีเข้มขอบทอง (ไม่ยืดภาพป้ายเพราะสัดส่วนยาวกว่าภาพมาก ขอบจะเพี้ยน)
        Image strip = NewImage("MovementStats", root, null, new Color(0.2f, 0.1f, 0.04f, 0.94f));
        RectTransform sr = strip.rectTransform;
        sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 0f);
        sr.pivot = new Vector2(0.5f, 0f);
        sr.sizeDelta = new Vector2(1240f, 88f);
        sr.anchoredPosition = new Vector2(0f, 70f);
        Outline border = strip.gameObject.AddComponent<Outline>();
        border.effectColor = new Color(0.86f, 0.62f, 0.2f, 1f);
        border.effectDistance = new Vector2(4f, -4f);

        TextMeshProUGUI t = NewText("Value", sr, new Rect(0.06f, 0.12f, 0.88f, 0.76f), statsLine, 34f, new Color(1f, 0.93f, 0.78f, 1f));
        t.richText = true;
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip, 0.5f);
    }

    #region UI helpers
    private Image NewButtonOverlay(string name, RectTransform panel, Rect r, int index)
    {
        Image img = NewImage(name + "Highlight", panel, null, new Color(1f, 0.82f, 0.25f, 0f));
        Place(img.rectTransform, r);
        img.raycastTarget = true;
        Button btn = img.gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() => { selected = index; Confirm(); });
        return img;
    }

    private static void Place(RectTransform rt, Rect r)
    {
        // r ใช้มุมซ้ายบนของภาพเป็นจุดอ้างอิง -> แปลงเป็น anchor ของ Unity (มุมซ้ายล่าง)
        rt.anchorMin = new Vector2(r.xMin, 1f - r.yMax);
        rt.anchorMax = new Vector2(r.xMax, 1f - r.yMin);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static Image NewImage(string name, RectTransform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI NewText(string name, RectTransform parent, Rect r, string text, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, r);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = false;
        t.enableAutoSizing = true;
        t.fontSizeMin = 12f;
        t.fontSizeMax = size;
        t.raycastTarget = false;
        return t;
    }
    #endregion
}
