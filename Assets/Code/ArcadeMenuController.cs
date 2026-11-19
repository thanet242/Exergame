using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class ArcadeMenuController : MonoBehaviour
{
    [Header("Main Menu UI Buttons")]
    public Button playButton;
    public Button optionButton;
    public Button exitButton;

    [Header("Option Panel UI")]
    public GameObject optionPanel;
    public Button optionBackButton;
    public Slider volumeSlider;

    [Header("Audio Settings")]
    [Tooltip("ใส่ไฟล์เสียงที่จะเล่นตอนเลื่อนเลือกหรือเอาเมาส์ไปชี้ที่ปุ่ม")]
    public AudioClip selectSound;

    [Range(0f, 1f)]
    [Tooltip("ระดับความดังของเสียงเลือกปุ่ม (0.0 = ปิดเสียง, 1.0 = ดังสุด, ค่าแนะนำ 0.2 - 0.4)")]
    public float selectSoundVolume = 0.3f;

    private AudioSource audioSource;

    [Header("Arcade Foot-Pad KeyCodes")]
    [Tooltip("ปุ่มหน้า (UP) เหยียบครั้งเดียว = ยืนยัน")]
    public KeyCode upKey = KeyCode.Joystick1Button5;
    [Tooltip("ปุ่มหลัง (DOWN) = เลื่อนไปปุ่มถัดไป")]
    public KeyCode downKey = KeyCode.Joystick1Button11;
    [Tooltip("ปุ่มซ้าย (LEFT) สำหรับเลื่อนเลือก")]
    public KeyCode leftKey = KeyCode.Joystick1Button8;
    [Tooltip("ปุ่มขวา (RIGHT) สำหรับเลื่อนเลือก")]
    public KeyCode rightKey = KeyCode.Joystick1Button3;

    [Header("Confirm Settings")]
    [Tooltip("หน่วงเวลาหลังเปิดหน้าเมนู ก่อนรับการเหยียบยืนยัน (กันเท้าที่เหยียบค้างมาจากหน้าก่อน)")]
    public float confirmDelay = 0.6f;
    private bool exhibitLayout;

    [Header("Scene Settings")]
    public string gameSceneName = "Cave3";

    [Header("Selected Button Effect")]
    [Tooltip("ปุ่มที่ถูกเลือกจะขยาย-หดเป็นจังหวะ ให้ผู้เล่นที่ยืนบนแผ่นเหยียบเห็นชัดว่ากำลังเลือกปุ่มไหน")]
    public float selectedScale = 1.08f;
    public float pulseAmount = 0.03f;
    public float pulseSpeed = 5f;

    [Header("Option Panel - Foot Pad")]
    [Tooltip("เหยียบซ้าย/ขวาในหน้า Option เพื่อลด/เพิ่มเสียงครั้งละเท่านี้")]
    public float volumeStep = 0.1f;
    public const string PREF_VOLUME = "MasterVolume";

    [Tooltip("หน้า Option แบบใหม่ (หาอัตโนมัติจาก Option Panel ถ้าไม่ได้ใส่)")]
    public OptionMenuUI optionMenu;
    private int optionClosedFrame = -1;

    private List<Button> currentMenuButtons = new List<Button>();
    private int currentSelectedIndex = 0;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        if (optionMenu == null && optionPanel != null) optionMenu = optionPanel.GetComponent<OptionMenuUI>();
        if (optionPanel != null) optionPanel.SetActive(false);

        // คืนค่าระดับเสียงที่ผู้เล่นตั้งไว้ครั้งก่อน
        AudioListener.volume = PlayerPrefs.GetFloat(PREF_VOLUME, AudioListener.volume);

        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
        }

        // ผูกปุ่มด้วยโค้ด กรณีใน Scene ยังไม่ได้ตั้ง OnClick (เช่น ปุ่มปิดหน้า Option)
        BindIfNoPersistentCall(optionButton, OnClickOption);
        BindIfNoPersistentCall(optionBackButton, OnClickCloseOption);

        SetupMouseHoverEvents();
        // แถบด้านล่างอาจยกปุ่มขึ้นพ้นแถบ: สร้างก่อน แล้วค่อยจำตำแหน่งปุ่ม Play เดิมไว้ใช้ตอนปิดโหมดงานแสดง
        BuildControlsBar();
        ApplyExhibitLayout();
        UpdateActiveButtonsList();
        SelectButtonByIndex(0, playAudio: false);
    }

    // แถบบอกปุ่มด้านล่าง: เหยียบขึ้นครั้งเดียว = ยืนยัน (ไม่ต้องเหยียบ 2 ครั้งแล้ว)
    private const string ControlsHint = "LEFT / RIGHT  :  SELECT          STEP UP  ( W / ENTER )  :  CONFIRM          MOUSE  :  CLICK";
    private GameObject controlsBar;

    // งานแสดง: ซ่อนปุ่ม Option / Exit ไม่ให้ผู้เล่นเปลี่ยนปุ่มหรือปิดโปรแกรม (ทีมงานกด F12 เพื่อเปิดกลับ)
    void ApplyExhibitLayout()
    {
        exhibitLayout = ExhibitMode.On;
        if (optionButton != null) optionButton.gameObject.SetActive(!exhibitLayout);
        if (exitButton != null) exitButton.gameObject.SetActive(!exhibitLayout);
        PlacePlayButton(exhibitLayout);
    }

    [Tooltip("งานแสดง: ปุ่ม Play อยู่กลางจอ สูงจากขอบล่างเท่านี้ (สัดส่วนความสูงจอ) และขยายขนาดเท่านี้")]
    public float exhibitPlayHeight = 0.22f;
    public float exhibitPlaySize = 1.4f;
    private bool playLayoutSaved;
    private Vector2 playAnchorMin, playAnchorMax, playPivot, playPos, playSize, playRealSize;

    // ปุ่มอื่นถูกซ่อน: ย้าย Play มากลางจอและขยายให้ใหญ่ขึ้น / ปิดโหมดงานแสดง: คืนตำแหน่งเดิม
    void PlacePlayButton(bool centered)
    {
        if (playButton == null) return;
        RectTransform rt = (RectTransform)playButton.transform;
        if (!playLayoutSaved)
        {
            playAnchorMin = rt.anchorMin; playAnchorMax = rt.anchorMax; playPivot = rt.pivot;
            playPos = rt.anchoredPosition; playSize = rt.sizeDelta;
            playRealSize = rt.rect.size; // ขนาดจริง (กรณี anchor ยืดตามพ่อ sizeDelta ไม่ใช่ขนาดปุ่ม)
            playLayoutSaved = true;
        }

        if (centered)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, exhibitPlayHeight);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = playRealSize * exhibitPlaySize;
        }
        else
        {
            rt.anchorMin = playAnchorMin; rt.anchorMax = playAnchorMax; rt.pivot = playPivot;
            rt.anchoredPosition = playPos; rt.sizeDelta = playSize;
        }
    }

    // แถบด้านล่างจอบอกว่ากดปุ่มอะไรเพื่อทำอะไร (สไตล์เดียวกับแถบล่างหน้า QuestBoard)
    void BuildControlsBar()
    {
        Button anyButton = playButton != null ? playButton : optionButton;
        if (anyButton == null) return;
        Canvas canvas = anyButton.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        canvas = canvas.rootCanvas;
        RectTransform root = (RectTransform)canvas.transform;
        float height = root.rect.height * 0.065f;

        controlsBar = new GameObject("ControlsBar", typeof(RectTransform), typeof(Image));
        controlsBar.transform.SetParent(root, false);
        RectTransform bar = (RectTransform)controlsBar.transform;
        bar.anchorMin = new Vector2(0f, 0f); bar.anchorMax = new Vector2(1f, 0f);
        bar.pivot = new Vector2(0.5f, 0f);
        bar.sizeDelta = new Vector2(0f, height);
        bar.anchoredPosition = Vector2.zero;
        Image bg = controlsBar.GetComponent<Image>();
        bg.color = new Color(0.09f, 0.05f, 0.02f, 0.82f);
        bg.raycastTarget = false;

        var lineGo = new GameObject("GoldLine", typeof(RectTransform), typeof(Image));
        lineGo.transform.SetParent(bar, false);
        RectTransform line = (RectTransform)lineGo.transform;
        line.anchorMin = new Vector2(0f, 1f); line.anchorMax = new Vector2(1f, 1f);
        line.pivot = new Vector2(0.5f, 1f);
        line.sizeDelta = new Vector2(0f, Mathf.Max(1f, height * 0.04f));
        Image lineImg = lineGo.GetComponent<Image>();
        lineImg.color = new Color(0.86f, 0.62f, 0.2f, 0.9f);
        lineImg.raycastTarget = false;

        var textGo = new GameObject("Hint", typeof(RectTransform));
        textGo.transform.SetParent(bar, false);
        RectTransform tr = (RectTransform)textGo.transform;
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(12f, 0f); tr.offsetMax = new Vector2(-12f, 0f);
        TMPro.TextMeshProUGUI t = textGo.AddComponent<TMPro.TextMeshProUGUI>();
        t.text = ControlsHint;
        t.alignment = TMPro.TextAlignmentOptions.Center;
        t.fontStyle = TMPro.FontStyles.Bold;
        t.color = new Color(1f, 0.93f, 0.78f, 0.95f);
        t.enableAutoSizing = true;
        t.fontSizeMin = 4f;
        t.fontSizeMax = height * 0.42f;
        t.enableWordWrapping = false;
        t.raycastTarget = false;

        // ให้อยู่ใต้หน้า Option (หน้า Option มีคำแนะนำปุ่มของตัวเอง)
        if (optionPanel != null && optionPanel.transform.parent == root)
            controlsBar.transform.SetSiblingIndex(optionPanel.transform.GetSiblingIndex());

        // ปุ่มเมนูอยู่ชิดขอบล่าง: ถ้าแถบทับปุ่ม ให้ยกปุ่มขึ้นพ้นแถบ
        Canvas.ForceUpdateCanvases();
        Vector3[] corners = new Vector3[4];
        bar.GetWorldCorners(corners);
        float barTop = corners[1].y;
        float lowest = float.MaxValue;
        foreach (Button b in new[] { playButton, optionButton, exitButton })
        {
            if (b == null) continue;
            ((RectTransform)b.transform).GetWorldCorners(corners);
            lowest = Mathf.Min(lowest, corners[0].y);
        }
        float gap = root.lossyScale.y * height * 0.25f;
        if (lowest < barTop + gap && lowest != float.MaxValue)
        {
            float shift = barTop + gap - lowest;
            foreach (Button b in new[] { playButton, optionButton, exitButton })
                if (b != null) b.transform.position += new Vector3(0f, shift, 0f);
        }
    }

    void Update()
    {
        if (controlsBar != null) controlsBar.SetActive(!IsOptionPanelOpen);
        if (exhibitLayout != ExhibitMode.On && !IsOptionPanelOpen)
        {
            ApplyExhibitLayout();
            UpdateActiveButtonsList();
            SelectButtonByIndex(0, playAudio: false);
        }

        // หน้า Option แบบใหม่จัดการปุ่มเอง / เฟรมที่เพิ่งปิดหน้า Option ไม่รับปุ่มซ้ำ
        if ((IsOptionPanelOpen && optionMenu != null) || Time.frameCount == optionClosedFrame) return;

        HandleKeyboardInput();
        HandleFootPadInput();
        AnimateSelectedButton();
    }

    void BindIfNoPersistentCall(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;

        for (int i = 0; i < btn.onClick.GetPersistentEventCount(); i++)
        {
            if (btn.onClick.GetPersistentTarget(i) != null) return;
        }
        btn.onClick.AddListener(action);
    }

    void AnimateSelectedButton()
    {
        float pulse = selectedScale + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;

        for (int i = 0; i < currentMenuButtons.Count; i++)
        {
            Button btn = currentMenuButtons[i];
            if (btn == null) continue;

            float target = i == currentSelectedIndex ? pulse : 1f;
            Transform t = btn.transform;
            t.localScale = Vector3.Lerp(t.localScale, new Vector3(target, target, 1f), Time.unscaledDeltaTime * 12f);
        }
    }

    bool IsOptionPanelOpen => optionPanel != null && optionPanel.activeSelf;

    // ในหน้า Option: ซ้าย/ขวา = ปรับเสียง (มีปุ่มให้เลือกแค่ปุ่มปิด จึงไม่ต้องใช้เลื่อนเลือก)
    bool TryAdjustVolume(int direction)
    {
        if (!IsOptionPanelOpen) return false;

        float value = Mathf.Clamp01(AudioListener.volume + direction * volumeStep);
        if (volumeSlider != null) volumeSlider.value = value; // เรียก OnVolumeSliderChanged ให้เอง
        else OnVolumeSliderChanged(value);
        PlayHoverSound();
        return true;
    }

    void SetupMouseHoverEvents()
    {
        Button[] allButtons = { playButton, optionButton, exitButton, optionBackButton };

        foreach (Button btn in allButtons)
        {
            if (btn == null) continue;

            EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = btn.gameObject.AddComponent<EventTrigger>();
            }

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerEnter;
            Button capturedBtn = btn;
            entry.callback.AddListener((data) => { OnButtonHovered(capturedBtn); });
            trigger.triggers.Add(entry);
        }
    }

    void OnButtonHovered(Button hoveredButton)
    {
        if (!hoveredButton.interactable) return;

        int index = currentMenuButtons.IndexOf(hoveredButton);
        if (index != -1 && index != currentSelectedIndex)
        {
            SelectButtonByIndex(index, playAudio: true);
        }
    }

    void UpdateActiveButtonsList()
    {
        currentMenuButtons.Clear();

        if (optionPanel != null && optionPanel.activeSelf)
        {
            if (optionBackButton != null) currentMenuButtons.Add(optionBackButton);
        }
        else
        {
            foreach (Button b in new[] { playButton, optionButton, exitButton })
                if (b != null && b.gameObject.activeSelf) currentMenuButtons.Add(b);
        }
    }

    void SelectButtonByIndex(int index, bool playAudio = true)
    {
        if (currentMenuButtons.Count == 0) return;

        currentSelectedIndex = Mathf.Clamp(index, 0, currentMenuButtons.Count - 1);
        Button targetBtn = currentMenuButtons[currentSelectedIndex];

        if (targetBtn != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(targetBtn.gameObject);
        }

        if (playAudio)
        {
            PlayHoverSound();
        }
    }

    public void PlayHoverSound()
    {
        if (audioSource != null && selectSound != null)
        {
            audioSource.PlayOneShot(selectSound, selectSoundVolume);
        }
    }

    void HandleFootPadInput()
    {
        // แก้ไข: ปุ่มซ้าย-ขวา ใช้สำหรับเลื่อนภารกิจ
        if (Input.GetKeyDown(leftKey) && !TryAdjustVolume(-1))
        {
            currentSelectedIndex--;
            if (currentSelectedIndex < 0) currentSelectedIndex = currentMenuButtons.Count - 1;
            SelectButtonByIndex(currentSelectedIndex, playAudio: true);
        }

        if (Input.GetKeyDown(rightKey) && !TryAdjustVolume(1))
        {
            currentSelectedIndex++;
            if (currentSelectedIndex >= currentMenuButtons.Count) currentSelectedIndex = 0;
            SelectButtonByIndex(currentSelectedIndex, playAudio: true);
        }

        // เหยียบขึ้นครั้งเดียว = ยืนยัน / เหยียบลง = เลื่อนไปปุ่มถัดไป (เหมือนทุกหน้าในเกม)
        if (Input.GetKeyDown(upKey)) ConfirmIfReady();
        if (Input.GetKeyDown(downKey) && !IsOptionPanelOpen) SelectNext();
    }

    void HandleKeyboardInput()
    {
        // แก้ไข: คีย์บอร์ดซ้าย-ขวา ใช้สำหรับเลื่อนภารกิจ
        if ((Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) && !TryAdjustVolume(-1))
        {
            currentSelectedIndex--;
            if (currentSelectedIndex < 0) currentSelectedIndex = currentMenuButtons.Count - 1;
            SelectButtonByIndex(currentSelectedIndex, playAudio: true);
        }

        if ((Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) && !TryAdjustVolume(1))
        {
            currentSelectedIndex++;
            if (currentSelectedIndex >= currentMenuButtons.Count) currentSelectedIndex = 0;
            SelectButtonByIndex(currentSelectedIndex, playAudio: true);
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) ConfirmIfReady();
        else if ((Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) && !IsOptionPanelOpen) SelectNext();

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            TriggerCurrentSelectedButton();
        }
    }

    void ConfirmIfReady()
    {
        if (Time.timeSinceLevelLoad < confirmDelay) return;
        TriggerCurrentSelectedButton();
    }

    void SelectNext()
    {
        if (currentMenuButtons.Count == 0) return;
        SelectButtonByIndex((currentSelectedIndex + 1) % currentMenuButtons.Count, playAudio: true);
    }

    void TriggerCurrentSelectedButton()
    {
        if (currentMenuButtons.Count == 0) return;

        Button targetBtn = currentMenuButtons[currentSelectedIndex];
        if (targetBtn != null && targetBtn.interactable)
        {
            targetBtn.onClick.Invoke();
        }
    }

    public void OnClickPlay()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnClickOption()
    {
        if (optionPanel != null)
        {
            optionPanel.SetActive(true);
            if (optionMenu != null)
            {
                optionMenu.Open(this);
                PlayHoverSound();
                return;
            }
            UpdateActiveButtonsList();
            SelectButtonByIndex(0, playAudio: true);
        }
    }

    public void OnClickCloseOption()
    {
        if (optionPanel != null)
        {
            optionPanel.SetActive(false);
            optionClosedFrame = Time.frameCount;
            UpdateActiveButtonsList();
            SelectButtonByIndex(1, playAudio: true); 
        }
    }

    public void OnClickExit()
    {
        Application.Quit();
    }

    public void OnVolumeSliderChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(PREF_VOLUME, value);
    }
}