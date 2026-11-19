using UnityEngine;
using UnityEngine.SceneManagement;

public class QuestMenuController : MonoBehaviour
{
    [System.Serializable]
    public class QuestItem
    {
        public string questName = "ภารกิจ";
        public Transform boardPosition;       // ตำแหน่งป้าย (Quest paper Mine / Quest paper def)[cite: 3, 11]
        public GameObject questPopupPanel;    // หน้าต่าง Popup (QuestPopup Mine / QuestPopup def)[cite: 3, 11]
        public string targetSceneName;        // ชื่อฉากที่จะโหลดเมื่อกดเริ่มเกม[cite: 3, 11]
    }

    [Header("Popup Settings (การเปิด-ปิด Popup)")]
    [Tooltip("ติ๊กถูก = เปิดหน้าต่าง Popup ก่อนเริ่ม / เอาออก = กดเลือกแล้วเข้าเกมทันที")]
    public bool usePopup = false;

    [Header("1. รายการภารกิจทั้งหมด (Quest Items)")]
    public QuestItem[] quests; //[cite: 3, 11]

    [Header("2. กรอบหรือไอคอนไฮไลต์ (Highlight Frame / Icon)")]
    public GameObject highlightFrame; //[cite: 3, 11]
    
    [Tooltip("ปรับระยะเยื้องแกน X และ Y ของไอคอนไฮไลต์")]
    public Vector2 iconOffset = Vector2.zero; //[cite: 3, 11]

    [Header("Audio Settings (ระบบเสียง)")]
    [Tooltip("ใส่ไฟล์เสียงที่จะเล่นตอนเลื่อนเปลี่ยนป้ายเควสต์")]
    public AudioClip selectSound; //
    [Tooltip("ใส่ไฟล์เสียงตอนกดยืนยันเลือกเควสต์ (ไม่ใส่ก็ได้)")]
    public AudioClip confirmSound; //

    [Range(0f, 1f)]
    [Tooltip("ระดับความดังของเสียง (0.0 = ปิดเสียง, 1.0 = ดังสุด, ค่าแนะนำ 0.2 - 0.4)")]
    public float soundVolume = 0.3f;

    private AudioSource audioSource;

    [Header("3. ปุ่มควบคุมแผ่นเหยียบ (Foot-Pad KeyCodes)")]
    public KeyCode upKey = KeyCode.Joystick1Button5;       // แผ่นด้านบน (ใช้ Double-Tap ยืนยัน)[cite: 3]
    public KeyCode downKey = KeyCode.Joystick1Button11;    // แผ่นด้านล่าง (เลื่อนป้าย)[cite: 3]
    public KeyCode leftKey = KeyCode.Joystick1Button8;     // แผ่นด้านซ้าย (เลื่อนป้ายย้อนกลับ)[cite: 3]
    public KeyCode rightKey = KeyCode.Joystick1Button3;    // แผ่นด้านขวา (เลื่อนป้ายถัดไป)[cite: 3]
    public KeyCode actionConfirmKey = KeyCode.Joystick1Button0; //[cite: 3]

    [Header("4. การตั้งค่า Double-Tap (ปุ่มบนเพื่อยืนยัน)")]
    public float doubleTapThreshold = 0.5f; //[cite: 3]
    private float lastUpPressTime = -1f; //[cite: 3]

    [Header("5. เหยียบค้าง 3 วินาที (Hold to Close / Back)")]
    public string mainMenuSceneName = "MainMenu"; //[cite: 3]
    public float holdDuration = 3.0f; //[cite: 3]
    private float currentHoldTimer = 0f; //[cite: 3]

    private int currentIndex = 0; //[cite: 3]
    private bool isPopupOpen = false; //[cite: 3]

    /// <summary>เควสที่เลือกอยู่ (ใช้โดย QuestBoardDecor แสดงกรอบเรืองแสงและชื่อเควส)</summary>
    public int CurrentIndex => currentIndex;
    /// <summary>ความคืบหน้าการเหยียบค้างเพื่อกลับเมนู 0-1</summary>
    public float HoldProgress => holdDuration > 0f ? Mathf.Clamp01(currentHoldTimer / holdDuration) : 0f;

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
        CloseAllPopups(); //[cite: 3]
        Invoke(nameof(UpdateHighlightPosition), 0.05f); //[cite: 3]
    }

    void Update()
    {
        HandleHoldToBack(); //[cite: 3]
        HandleInput(); //[cite: 3]
        HandleMouse();
    }

    private Vector3 lastMousePosition = new Vector3(-1f, -1f, 0f);

    // เมาส์: ชี้ที่กระดาษเควส = เลือก / คลิก = เริ่มเควสนั้น
    // (เลือกตามการขยับเมาส์เท่านั้น เมาส์ที่วางนิ่งไว้จะไม่แย่งการเลือกจากแผ่นเหยียบ)
    void HandleMouse()
    {
        if (isPopupOpen || quests == null) return;
        Vector3 mouse = Input.mousePosition;
        bool moved = (mouse - lastMousePosition).sqrMagnitude > 1f;
        bool clicked = Input.GetMouseButtonDown(0);
        lastMousePosition = mouse;
        if (!moved && !clicked) return;

        for (int i = 0; i < quests.Length; i++)
        {
            RectTransform rt = quests[i].boardPosition as RectTransform;
            if (rt == null) continue;
            Canvas canvas = rt.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (!RectTransformUtility.RectangleContainsScreenPoint(rt, mouse, cam)) continue;

            if (i != currentIndex)
            {
                currentIndex = i;
                UpdateHighlightPosition();
                PlaySound(selectSound);
            }
            if (clicked) OnConfirmAction();
            return;
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // คีย์บอร์ด: Esc กลับทันที (แผ่นเหยียบใช้เหยียบค้าง 3 วินาที)
            if (isPopupOpen) CloseQuestPopup();
            else if (!string.IsNullOrEmpty(mainMenuSceneName)) { Time.timeScale = 1f; SceneManager.LoadScene(mainMenuSceneName); }
            return;
        }

        if (currentHoldTimer > 0.3f) return; //[cite: 3]

        // ตรวจจับการ Double-Tap ปุ่มบน (UP) เพื่อยืนยัน
        if (Input.GetKeyDown(upKey) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) //[cite: 3]
        {
            float timeSinceLast = Time.time - lastUpPressTime; //[cite: 3]

            if (timeSinceLast <= doubleTapThreshold) //[cite: 3]
            {
                OnConfirmAction();
                lastUpPressTime = -1f; //[cite: 3]
                return;
            }

            lastUpPressTime = Time.time; //[cite: 3]
        }

        // ยืนยันผ่าน Keyboard (Enter / Space) หรือปุ่ม Action
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(actionConfirmKey)) //[cite: 3]
        {
            OnConfirmAction();
            return;
        }

        // การเลื่อนเปลี่ยนป้ายเควสต์ (ทำงานเฉพาะตอน Popup ไม่ได้เปิดอยู่)
        if (!isPopupOpen && quests != null && quests.Length > 0) //[cite: 3]
        {
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(rightKey)) //[cite: 3]
            {
                MoveIndex(1); //[cite: 3]
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(leftKey)) //[cite: 3]
            {
                MoveIndex(-1); //[cite: 3]
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(downKey)) //[cite: 3]
            {
                MoveIndex(1); //[cite: 3]
            }
        }
    }

    void OnConfirmAction()
    {
        // กรณีปิดการใช้งาน Popup: เข้าเกมทันที
        if (!usePopup)
        {
            StartQuestGame();
        }
        // กรณีเปิดใช้งาน Popup: เปิด Popup ก่อน หากเปิดอยู่แล้วจึงเริ่มเกม
        else
        {
            if (isPopupOpen)
            {
                StartQuestGame();
            }
            else
            {
                OpenQuestPopup();
            }
        }
    }

    void MoveIndex(int step)
    {
        currentIndex = (currentIndex + step + quests.Length) % quests.Length; //[cite: 3]
        UpdateHighlightPosition(); //[cite: 3]
        PlaySound(selectSound);
    }

    void UpdateHighlightPosition()
    {
        if (highlightFrame == null || quests == null || quests.Length == 0) return; //[cite: 3]

        Transform targetPos = quests[currentIndex].boardPosition; //[cite: 3]
        if (targetPos != null) //[cite: 3]
        {
            highlightFrame.SetActive(true); //[cite: 3]
            highlightFrame.transform.position = targetPos.position + new Vector3(iconOffset.x, iconOffset.y, 0f); //[cite: 3]
            highlightFrame.transform.SetAsLastSibling(); //[cite: 3]
        }
    }

    public void OpenQuestPopup()
    {
        if (currentIndex >= 0 && currentIndex < quests.Length) //[cite: 3]
        {
            PlaySound(confirmSound);
            CloseAllPopups(); //[cite: 3]
            if (quests[currentIndex].questPopupPanel != null) //[cite: 3]
            {
                quests[currentIndex].questPopupPanel.SetActive(true); //[cite: 3]
                isPopupOpen = true; //[cite: 3]
                if (highlightFrame != null) highlightFrame.SetActive(false); //[cite: 3]
            }
        }
    }

    public void CloseQuestPopup()
    {
        CloseAllPopups(); //[cite: 3]
        isPopupOpen = false; //[cite: 3]
        UpdateHighlightPosition(); //[cite: 3]
    }

    void CloseAllPopups()
    {
        for (int i = 0; i < quests.Length; i++) //[cite: 3]
        {
            if (quests[i].questPopupPanel != null) //[cite: 3]
            {
                quests[i].questPopupPanel.SetActive(false); //[cite: 3]
            }
        }
    }

    public void StartQuestGame()
    {
        if (currentIndex >= 0 && currentIndex < quests.Length) //[cite: 3]
        {
            PlaySound(confirmSound);
            string targetScene = quests[currentIndex].targetSceneName; //[cite: 3]
            Debug.Log($"<color=green>[Start Game]</color> เข้าสู่ฉาก: {targetScene}"); //[cite: 3]

            if (!string.IsNullOrEmpty(targetScene)) //[cite: 3]
            {
                Time.timeScale = 1f; //[cite: 3]

                if (ScreenSlideTransition.Instance != null)
                {
                    ScreenSlideTransition.Instance.LoadSceneWithSlide(targetScene);
                }
                else
                {
                    SceneManager.LoadScene(targetScene); //[cite: 3]
                }
            }
            else
            {
                Debug.LogWarning("[Warning] ยังไม่ได้ใส่ชื่อ Target Scene Name ในป้ายนี้!"); //[cite: 3]
            }
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }

    void HandleHoldToBack()
    {
        bool isPressing = Input.GetKey(leftKey) || Input.GetKey(rightKey) || 
                          Input.GetKey(upKey) || Input.GetKey(downKey) || 
                          Input.GetKey(actionConfirmKey) ||
                          Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow) ||
                          Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) ||
                          Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) || 
                          Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) ||
                          Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.Backspace); //[cite: 3]

        if (isPressing) //[cite: 3]
        {
            currentHoldTimer += Time.deltaTime; //[cite: 3]

            if (currentHoldTimer >= holdDuration) //[cite: 3]
            {
                currentHoldTimer = 0f; //[cite: 3]

                if (isPopupOpen) //[cite: 3]
                {
                    Debug.Log("<color=yellow>[Hold Action]</color> ปิด Popup รายละเอียดภารกิจ"); //[cite: 3]
                    CloseQuestPopup(); //[cite: 3]
                }
                else
                {
                    Debug.Log($"<color=yellow>[Hold Action]</color> กำลังย้อนกลับไป Scene: {mainMenuSceneName}"); //[cite: 3]
                    if (!string.IsNullOrEmpty(mainMenuSceneName)) //[cite: 3]
                    {
                        Time.timeScale = 1f; //[cite: 3]
                        SceneManager.LoadScene(mainMenuSceneName); //[cite: 3]
                    }
                    else
                    {
                        Debug.LogError("[Error] ยังไม่ได้ระบุชื่อ Main Menu Scene Name ใน Inspector!"); //[cite: 3]
                    }
                }
            }
        }
        else
        {
            currentHoldTimer = 0f; //[cite: 3]
        }
    }
}