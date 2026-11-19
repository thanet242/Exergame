using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SummaryPopup : MonoBehaviour
{
    [Header("1. UI Text References")]
    public TMP_Text timeText;
    public TMP_Text trapText;
    public TMP_Text caloryText;

    [Header("2. Stage Settings")]
    public int totalTrapsInStage = 10;
    public string mainMenuSceneName = "Mainmenu";

    [Header("3. ปุ่มตัวเลือก (Buttons)")]
    public Transform[] buttons;

    [Header("4. ตัวชี้หรือกรอบไฮไลต์ (Highlight Frame)")]
    public GameObject highlightFrame;
    public Vector2 highlightOffset = Vector2.zero;
    public Vector2 highlightSize = new Vector2(65f, 65f);

    [Header("Animation (ขยับดุ๊กดิ๊ก)")]
    public bool enablePulse = true;
    public float pulseSpeed = 5f;
    public float minScale = 0.9f;
    public float maxScale = 1.15f;

    [Header("5. ปุ่มควบคุม (Foot-Pad & Controller)")]
    public KeyCode upKey = KeyCode.Joystick1Button5;       
    public KeyCode downKey = KeyCode.Joystick1Button11;    
    public KeyCode leftKey = KeyCode.Joystick1Button8;     
    public KeyCode rightKey = KeyCode.Joystick1Button3;    
    public KeyCode actionConfirmKey = KeyCode.Joystick1Button0; 

    [Header("6. การตั้งค่า Double-Tap เพื่อยืนยัน")]
    public float doubleTapThreshold = 0.5f;
    private float lastUpPressTime = -1f;

    private int currentIndex = 0;
    private bool isPopupActive = false;

    void OnEnable()
    {
        isPopupActive = true;
        UpdateHighlightPosition();
    }

    void Update()
    {
        if (!isPopupActive) return;

        HandleInputNavigation();
        HandlePulseAnimation();
    }

    public void DisplayStageClear(float totalCalories, int steps, int digs, int flags, int attacks)
    {
        gameObject.SetActive(true);
        isPopupActive = true;
        Time.timeScale = 0f;

        // 1. เวลาที่ใช้
        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(Time.timeSinceLevelLoad / 60);
            int seconds = Mathf.FloorToInt(Time.timeSinceLevelLoad % 60);
            timeText.text = $"{minutes:00}:{seconds:00}";
        }

        // 2. จำนวนกับดัก (จำกัดไม่ให้เกิน totalTrapsInStage และไม่ต่ำกว่า 0)
        if (trapText != null)
        {
            int clearedTraps = Mathf.Clamp(flags, 0, totalTrapsInStage);
            float accuracy = totalTrapsInStage > 0 
                ? ((float)clearedTraps / totalTrapsInStage) * 100f 
                : 0f;

            trapText.text = $"{clearedTraps} / {totalTrapsInStage}\n(Acc: {accuracy:F0}%)";
        }

        // 3. แคลอรี่
        if (caloryText != null)
        {
            caloryText.text = $"{totalCalories:F1}kcal";
        }

        currentIndex = 0;
        UpdateHighlightPosition();
    }

    void HandleInputNavigation()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(leftKey))
        {
            MoveIndex(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(rightKey))
        {
            MoveIndex(1);
        }

        if (Input.GetKeyDown(upKey) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            float timeSinceLastTap = Time.unscaledTime - lastUpPressTime;

            if (timeSinceLastTap <= doubleTapThreshold)
            {
                ExecuteSelectedButton();
                lastUpPressTime = -1f;
                return;
            }

            lastUpPressTime = Time.unscaledTime;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(actionConfirmKey))
        {
            ExecuteSelectedButton();
        }
    }

    void MoveIndex(int step)
    {
        if (buttons == null || buttons.Length == 0) return;

        currentIndex = (currentIndex + step + buttons.Length) % buttons.Length;
        UpdateHighlightPosition();
    }

    void UpdateHighlightPosition()
    {
        if (highlightFrame == null || buttons == null || buttons.Length == 0) return;

        if (currentIndex >= 0 && currentIndex < buttons.Length && buttons[currentIndex] != null)
        {
            Transform targetBtn = buttons[currentIndex];

            highlightFrame.transform.SetParent(targetBtn, false);

            RectTransform rect = highlightFrame.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = highlightSize;
                rect.anchoredPosition = highlightOffset;
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
            }

            highlightFrame.transform.SetAsLastSibling();
            highlightFrame.SetActive(true);
        }
    }

    void HandlePulseAnimation()
    {
        if (!enablePulse || highlightFrame == null || !highlightFrame.activeInHierarchy) return;

        float scaleFactor = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f);
        highlightFrame.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
    }

    void ExecuteSelectedButton()
    {
        if (buttons == null || currentIndex < 0 || currentIndex >= buttons.Length) return;

        string btnName = buttons[currentIndex].name.ToLower();

        if (btnName.Contains("retry") || btnName.Contains("restart"))
        {
            OnClickRestartButton();
        }
        else if (btnName.Contains("main") || btnName.Contains("home") || btnName.Contains("menu"))
        {
            OnClickHomeButton();
        }
        else
        {
            if (currentIndex == 0) OnClickHomeButton();
            else OnClickRestartButton();
        }
    }

    public void OnClickRestartButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnClickHomeButton()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}