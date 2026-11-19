using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// หน้า Option แบบม้วนกระดาษ (สไตล์เดียวกับหน้า Stage Clear / Game Over)
/// การควบคุม (แผ่นเหยียบ / คีย์บอร์ด):
/// - ขึ้น/ลง      : เลื่อนเลือกรายการ
/// - ขวา         : ยืนยัน (เริ่มเปลี่ยนปุ่ม / กดปุ่ม RESET, BACK) และเพิ่มเสียงในแถว VOLUME
/// - ซ้าย         : ลดเสียงในแถว VOLUME
/// - Esc         : กลับ
/// เปลี่ยนปุ่ม: เลือกแถว แล้วเหยียบขวา จากนั้นเหยียบทิศของแผ่น (หรือกดคีย์บอร์ด) ที่ต้องการใช้แทน
/// UI ทั้งหมดถูกสร้างด้วยโค้ดตอนเปิดหน้านี้ครั้งแรก ภายใน GameObject ของ OptionPanel
/// </summary>
public class OptionMenuUI : MonoBehaviour
{
    [Header("Art")]
    [Tooltip("ภาพม้วนกระดาษหัวข้อ OPTIONS")]
    public Sprite panelSprite;
    [Tooltip("ป้ายไม้ขอบทองสำหรับปุ่มและช่องแสดงปุ่ม")]
    public Sprite buttonSprite;

    [Header("Volume")]
    [Range(0.05f, 0.5f)] public float volumeStep = 0.1f;

    [Header("Rebinding")]
    [Tooltip("ยกเลิกการรอปุ่มอัตโนมัติถ้าไม่มีการกดภายในเวลานี้ (วินาที)")]
    public float listenTimeout = 6f;

    [Header("Colors")]
    public Color labelColor = new Color(0.27f, 0.14f, 0.05f, 1f);
    public Color valueColor = new Color(1f, 0.93f, 0.78f, 1f);
    public Color rowColor = new Color(0.36f, 0.2f, 0.08f, 0.12f);
    public Color rowSelectedColor = new Color(1f, 0.76f, 0.25f, 0.6f);
    public Color headerColor = new Color(0.55f, 0.22f, 0.05f, 1f);

    private enum RowKind { Volume, Binding, Reset, Back }

    private class Row
    {
        public RowKind kind;
        public GameAction action;
        public string label;
        public RectTransform root;
        public Image background;
        public TextMeshProUGUI labelText;
        public TextMeshProUGUI valueText;
        public Image[] volumeSegments;
        public Vector3 baseScale = Vector3.one;
    }

    private readonly List<Row> rows = new List<Row>();
    private int selected;
    private bool built;
    private int openedFrame = -1;

    private bool listening;
    private float listenStartTime;
    private Row listeningRow;

    private ArcadeMenuController menu;
    private TextMeshProUGUI hintText;

    private static KeyCode[] captureKeys;

    // ปุ่มนำทางเมนู (คงที่ ไม่เปลี่ยนตามการตั้งค่า เพื่อไม่ให้ผู้เล่นตั้งผิดจนออกจากเมนูไม่ได้)
    private static readonly KeyCode[] navUp = { GameInputBindings.PadUp, KeyCode.UpArrow, KeyCode.W };
    private static readonly KeyCode[] navDown = { GameInputBindings.PadDown, KeyCode.DownArrow, KeyCode.S };
    private static readonly KeyCode[] navRight = { GameInputBindings.PadRight, KeyCode.RightArrow, KeyCode.D, KeyCode.Return, KeyCode.Space };
    private static readonly KeyCode[] navLeft = { GameInputBindings.PadLeft, KeyCode.LeftArrow, KeyCode.A };

    public bool IsListening => listening;

    public void Open(ArcadeMenuController owner)
    {
        menu = owner;
        if (!built) Build();

        openedFrame = Time.frameCount;
        listening = false;
        selected = 0;
        RefreshAll();
    }

    private void OnEnable()
    {
        GameInputBindings.OnBindingsChanged += RefreshAll;
    }

    private void OnDisable()
    {
        GameInputBindings.OnBindingsChanged -= RefreshAll;
        listening = false;
    }

    private void Update()
    {
        if (!built || Time.frameCount == openedFrame) return;

        if (listening)
        {
            UpdateListening();
        }
        else
        {
            UpdateNavigation();
        }

        AnimateRows();
    }

    #region Input
    private static bool AnyDown(KeyCode[] keys)
    {
        for (int i = 0; i < keys.Length; i++) if (Input.GetKeyDown(keys[i])) return true;
        return false;
    }

    private void UpdateNavigation()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
        {
            Close();
            return;
        }

        if (AnyDown(navUp)) Select(selected - 1);
        else if (AnyDown(navDown)) Select(selected + 1);
        else if (AnyDown(navRight)) Confirm();
        else if (AnyDown(navLeft)) Left();
    }

    private void Select(int index)
    {
        selected = (index + rows.Count) % rows.Count;
        PlaySound();
        RefreshHighlight();
    }

    private void Confirm()
    {
        Row row = rows[selected];
        switch (row.kind)
        {
            case RowKind.Volume:
                ChangeVolume(+1);
                break;
            case RowKind.Binding:
                listening = true;
                listeningRow = row;
                listenStartTime = Time.unscaledTime;
                PlaySound();
                RefreshRow(row);
                RefreshHint();
                break;
            case RowKind.Reset:
                GameInputBindings.ResetToDefaults();
                SetVolume(1f);
                PlaySound();
                break;
            case RowKind.Back:
                Close();
                break;
        }
    }

    private void Left()
    {
        if (rows[selected].kind == RowKind.Volume) ChangeVolume(-1);
    }

    private void UpdateListening()
    {
        float elapsed = Time.unscaledTime - listenStartTime;

        // หน่วงสั้นๆ กันการเหยียบขวา (ที่ใช้ยืนยัน) ถูกจับเป็นปุ่มใหม่ทันที
        if (elapsed < 0.25f) return;

        if (elapsed > listenTimeout || Input.GetKeyDown(KeyCode.Escape))
        {
            StopListening();
            return;
        }

        if (captureKeys == null) BuildCaptureKeys();
        foreach (KeyCode key in captureKeys)
        {
            if (Input.GetKeyDown(key))
            {
                GameInputBindings.SetBinding(listeningRow.action, key);
                PlaySound();
                StopListening();
                return;
            }
        }
    }

    private void StopListening()
    {
        Row row = listeningRow;
        listening = false;
        listeningRow = null;
        if (row != null) RefreshRow(row);
        RefreshHint();
    }

    // ปุ่มที่รับได้ตอนเปลี่ยนปุ่ม: ปุ่มคีย์บอร์ดทั้งหมด (ยกเว้นเมาส์/Esc) + ปุ่มของแผ่นเหยียบ/จอยตัวที่ 1
    private static void BuildCaptureKeys()
    {
        var list = new List<KeyCode>();
        foreach (KeyCode k in System.Enum.GetValues(typeof(KeyCode)))
        {
            int v = (int)k;
            bool keyboard = v > 0 && v < (int)KeyCode.Mouse0 && k != KeyCode.Escape;
            bool pad = k >= KeyCode.Joystick1Button0 && k <= KeyCode.Joystick1Button19;
            if ((keyboard || pad) && !list.Contains(k)) list.Add(k);
        }
        captureKeys = list.ToArray();
    }

    private void Close()
    {
        listening = false;
        PlaySound();
        if (menu != null) menu.OnClickCloseOption();
        else gameObject.SetActive(false);
    }
    #endregion

    #region Volume
    private void ChangeVolume(int direction)
    {
        float v = Mathf.Round((AudioListener.volume + direction * volumeStep) * 100f) / 100f;
        SetVolume(Mathf.Clamp01(v));
        PlaySound();
    }

    private void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(ArcadeMenuController.PREF_VOLUME, value);
        foreach (Row r in rows) if (r.kind == RowKind.Volume) RefreshRow(r);
    }
    #endregion

    #region Build UI
    private void Build()
    {
        built = true;

        // ซ่อน UI หน้า Option แบบเก่า และใช้พื้นหลังมืดโปร่งแสงแทน
        for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(false);
        Image dim = GetComponent<Image>();
        if (dim != null)
        {
            dim.sprite = null;
            dim.color = new Color(0f, 0f, 0f, 0.65f);
        }

        RectTransform panel = CreateImage("ScrollPanel", (RectTransform)transform, panelSprite, Color.white).rectTransform;
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(600f, 337.6f);
        panel.anchoredPosition = new Vector2(0f, 6f);
        panel.GetComponent<Image>().preserveAspect = true;

        // พื้นที่กระดาษด้านในของม้วน (หน่วยของ Canvas)
        const float colW = 212f, leftX = -116f, rightX = 116f;

        CreateHeader(panel, "SOUND", leftX, 47f, colW);
        AddRow(panel, RowKind.Volume, default, "VOLUME", leftX, 25f, colW);

        CreateHeader(panel, "RUNNER MODE", leftX, 0f, colW);
        AddRow(panel, RowKind.Binding, GameAction.RunJump, "JUMP", leftX, -22f, colW);
        AddRow(panel, RowKind.Binding, GameAction.RunSlide, "SLIDE", leftX, -48f, colW);

        CreateHeader(panel, "CAVE MODE (MOVE)", rightX, 47f, colW);
        AddRow(panel, RowKind.Binding, GameAction.MoveUp, "UP", rightX, 25f, colW);
        AddRow(panel, RowKind.Binding, GameAction.MoveDown, "DOWN", rightX, -1f, colW);
        AddRow(panel, RowKind.Binding, GameAction.MoveLeft, "LEFT", rightX, -27f, colW);
        AddRow(panel, RowKind.Binding, GameAction.MoveRight, "RIGHT", rightX, -53f, colW);

        AddButton(panel, RowKind.Reset, "RESET", -64f, -89f);
        AddButton(panel, RowKind.Back, "BACK", 64f, -89f);

        // คำแนะนำการควบคุม ใต้ม้วนกระดาษ
        hintText = CreateText("Hint", (RectTransform)transform, "", 9.5f, new Color(1f, 0.9f, 0.65f, 1f), TextAlignmentOptions.Center);
        RectTransform hr = hintText.rectTransform;
        hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 0f);
        hr.sizeDelta = new Vector2(620f, 18f);
        hr.anchoredPosition = new Vector2(0f, 12f);
        hintText.enableAutoSizing = true;
        hintText.fontSizeMin = 5f;
        hintText.fontSizeMax = 9.5f;
    }

    private void CreateHeader(RectTransform parent, string text, float x, float y, float width)
    {
        TextMeshProUGUI t = CreateText("Header_" + text, parent, text, 10f, headerColor, TextAlignmentOptions.Left);
        t.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        t.characterSpacing = 4f;
        Place(t.rectTransform, x, y, width, 16f);

        // เส้นคั่นใต้หัวข้อ
        Image line = CreateImage("Line", parent, null, new Color(headerColor.r, headerColor.g, headerColor.b, 0.45f));
        Place(line.rectTransform, x, y - 8f, width, 1.2f);
    }

    private void AddRow(RectTransform parent, RowKind kind, GameAction action, string label, float x, float y, float width)
    {
        var row = new Row { kind = kind, action = action, label = label };

        row.background = CreateImage("Row_" + label, parent, null, rowColor);
        row.root = row.background.rectTransform;
        Place(row.root, x, y, width, 22f);

        row.labelText = CreateText("Label", row.root, label, 11f, labelColor, TextAlignmentOptions.Left);
        row.labelText.fontStyle = FontStyles.Bold;
        Stretch(row.labelText.rectTransform, 7f, 0f, -width * 0.48f, 0f);

        Image box = CreateImage("ValueBox", row.root, buttonSprite, Color.white);
        RectTransform br = box.rectTransform;
        br.anchorMin = br.anchorMax = new Vector2(1f, 0.5f);
        br.pivot = new Vector2(1f, 0.5f);
        br.sizeDelta = new Vector2(width * 0.56f, 21f);
        br.anchoredPosition = new Vector2(-3f, 0f);

        if (kind == RowKind.Volume)
        {
            // แถบเสียง 10 ช่อง
            row.volumeSegments = new Image[10];
            float segW = 7.5f, gap = 1.6f;
            float total = segW * 10 + gap * 9;
            for (int i = 0; i < 10; i++)
            {
                Image seg = CreateImage("Seg" + i, br, null, Color.white);
                RectTransform sr = seg.rectTransform;
                sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 0.5f);
                sr.sizeDelta = new Vector2(segW, 8f);
                sr.anchoredPosition = new Vector2(-total / 2f + segW / 2f + i * (segW + gap), 0f);
                row.volumeSegments[i] = seg;
            }
        }
        else
        {
            row.valueText = CreateText("Value", br, "", 8.5f, valueColor, TextAlignmentOptions.Center);
            row.valueText.fontStyle = FontStyles.Bold;
            row.valueText.enableAutoSizing = true;
            row.valueText.fontSizeMin = 5f;
            row.valueText.fontSizeMax = 8.5f;
            Stretch(row.valueText.rectTransform, 12f, 2f, -12f, -2f);
        }

        AddPointer(row, rows.Count, kind == RowKind.Volume ? br : null);
        rows.Add(row);
    }

    // เมาส์: ชี้ = เลือกแถว / คลิก = ยืนยัน (แถว VOLUME: คลิกบนแถบ = ตั้งระดับเสียงตามตำแหน่งที่คลิก)
    private void AddPointer(Row row, int index, RectTransform volumeBar)
    {
        row.background.raycastTarget = true;
        EventTrigger trigger = row.background.gameObject.AddComponent<EventTrigger>();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => { if (!listening && selected != index) Select(index); });
        trigger.triggers.Add(enter);

        var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        click.callback.AddListener(data => OnRowClicked(index, (PointerEventData)data, volumeBar));
        trigger.triggers.Add(click);
    }

    private void OnRowClicked(int index, PointerEventData e, RectTransform volumeBar)
    {
        if (listening || Time.frameCount == openedFrame) return;
        selected = index;
        RefreshHighlight();

        if (rows[index].kind == RowKind.Volume)
        {
            if (volumeBar != null &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(volumeBar, e.position, e.pressEventCamera, out Vector2 local) &&
                volumeBar.rect.Contains(local))
            {
                float t = Mathf.InverseLerp(volumeBar.rect.xMin, volumeBar.rect.xMax, local.x);
                SetVolume(Mathf.Clamp01(Mathf.Ceil(t * 10f) / 10f));
                PlaySound();
            }
            else
            {
                // คลิกที่ชื่อแถว: คลิกซ้าย = เพิ่ม / คลิกขวา = ลด
                ChangeVolume(e.button == PointerEventData.InputButton.Right ? -1 : 1);
            }
            return;
        }
        Confirm();
    }

    private void AddButton(RectTransform parent, RowKind kind, string label, float x, float y)
    {
        var row = new Row { kind = kind, label = label };
        row.background = CreateImage("Btn_" + label, parent, buttonSprite, Color.white);
        row.root = row.background.rectTransform;
        Place(row.root, x, y, 104f, 32f);

        row.labelText = CreateText("Label", row.root, label, 12f, valueColor, TextAlignmentOptions.Center);
        row.labelText.fontStyle = FontStyles.Bold;
        row.labelText.characterSpacing = 3f;
        Stretch(row.labelText.rectTransform, 10f, 2f, -10f, -2f);

        AddPointer(row, rows.Count, null);
        rows.Add(row);
    }

    private static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);
    }

    private static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(right, top);
    }

    private static Image CreateImage(string name, RectTransform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI CreateText(string name, RectTransform parent, string text, float size, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        t.enableWordWrapping = false;
        return t;
    }
    #endregion

    #region Refresh / Animation
    private void RefreshAll()
    {
        if (!built) return;
        foreach (Row r in rows) RefreshRow(r);
        RefreshHighlight();
        RefreshHint();
    }

    private void RefreshRow(Row row)
    {
        switch (row.kind)
        {
            case RowKind.Volume:
                int filled = Mathf.RoundToInt(AudioListener.volume * 10f);
                for (int i = 0; i < row.volumeSegments.Length; i++)
                {
                    row.volumeSegments[i].color = i < filled ? new Color(1f, 0.8f, 0.25f, 1f) : new Color(0.15f, 0.08f, 0.03f, 0.85f);
                }
                row.labelText.text = $"{row.label}  <size=80%>{Mathf.RoundToInt(AudioListener.volume * 100f)}%</size>";
                break;

            case RowKind.Binding:
                row.valueText.text = (listening && listeningRow == row)
                    ? "<color=#FFE066>STEP ON A PAD...</color>"
                    : GameInputBindings.DescribeAction(row.action);
                break;
        }
    }

    private void RefreshHighlight()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            Row r = rows[i];
            bool isSelected = i == selected;
            if (r.kind == RowKind.Reset || r.kind == RowKind.Back)
            {
                r.background.color = isSelected ? new Color(1f, 0.9f, 0.6f, 1f) : Color.white;
                r.labelText.color = isSelected ? new Color(1f, 0.86f, 0.35f, 1f) : valueColor;
            }
            else
            {
                r.background.color = isSelected ? rowSelectedColor : rowColor;
                r.labelText.color = isSelected ? new Color(0.18f, 0.08f, 0.02f, 1f) : labelColor;
            }
        }
    }

    private void RefreshHint()
    {
        if (hintText == null) return;
        hintText.text = listening
            ? "STEP ON THE PAD DIRECTION (OR PRESS A KEY) TO USE FOR THIS ACTION      ESC : CANCEL"
            : "UP / DOWN (W / S) : SELECT      RIGHT (D) / ENTER : CONFIRM      LEFT / RIGHT : VOLUME      MOUSE : CLICK      ESC : BACK";
    }

    private void AnimateRows()
    {
        float pulse = 1.04f + Mathf.Sin(Time.unscaledTime * 5f) * 0.02f;
        for (int i = 0; i < rows.Count; i++)
        {
            Row r = rows[i];
            float target = i == selected ? pulse : 1f;
            r.root.localScale = Vector3.Lerp(r.root.localScale, new Vector3(target, target, 1f), Time.unscaledDeltaTime * 12f);
        }

        // ข้อความรอปุ่มกะพริบ
        if (listening && listeningRow != null && listeningRow.valueText != null)
        {
            listeningRow.valueText.alpha = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));
        }
        else
        {
            foreach (Row r in rows) if (r.valueText != null) r.valueText.alpha = 1f;
        }
    }

    private void PlaySound()
    {
        if (menu != null) menu.PlayHoverSound();
    }
    #endregion
}
