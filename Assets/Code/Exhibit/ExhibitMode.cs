using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// โหมดงานแสดง (เปิดอยู่เป็นค่าเริ่มต้น สร้างตัวเองอัตโนมัติ ไม่ต้องวางใน Scene)
/// - ไม่มีการเหยียบนาน: เตือนนับถอยหลัง แล้วกลับหน้าแรกเอง + ล้างค่าของผู้เล่นคนก่อน
/// - หน้าแรกว่างอยู่: ขึ้นป้าย "เหยียบแผ่นไหนก็ได้เพื่อเริ่มเล่น" + สถิติวันนี้
/// - การ์ดสอนเล่นก่อนเริ่มแต่ละเกม / หยุดเกมด้วยการเหยียบค้าง (โหมดวิ่ง/ถ้ำ)
/// - แผ่นเหยียบเล็กๆ มุมจอสว่างตามแผ่นที่เหยียบ / เตือนเมื่อแผ่นเหยียบหลุด
/// - ซ่อนเมาส์ เต็มจอ กันปิดโปรแกรม
/// ปุ่มลับสำหรับทีมงาน (คีย์บอร์ด): F12 = เปิด/ปิดโหมดงานแสดง, F9 / F10 = เสียงเบาลง / ดังขึ้น
/// </summary>
public class ExhibitMode : MonoBehaviour
{
    public const string MainMenuScene = "Mainmenu";
    public const string QuestBoardScene = "QuestBoard";
    private const string PrefOn = "ExhibitMode";

    [Tooltip("ไม่มีการเหยียบนานเท่านี้ (วินาที) = กลับหน้าแรก")]
    public float idleToHome = 35f;
    [Tooltip("เตือนนับถอยหลังก่อนกลับหน้าแรก (วินาที)")]
    public float idleWarning = 10f;
    [Tooltip("หน้าแรกว่างนานเท่านี้ = ขึ้นป้ายเชิญเล่น")]
    public float attractAfter = 20f;
    [Tooltip("เหยียบค้างนานเท่านี้ = หยุดเกม (โหมดวิ่ง/ถ้ำ)")]
    public float holdToPause = 1.0f;

    /// <summary>โหมดวิ่ง: จำกัดเวลาต่อรอบ (วินาที) ให้คิวเดินเร็ว</summary>
    public const float RunnerTimeLimit = 180f;

    public static ExhibitMode Instance { get; private set; }
    public static bool On { get; private set; } = true;

    /// <summary>ระดับ "สบาย ๆ" ที่ผู้เล่นคนปัจจุบันเลือก (ล้างเมื่อกลับหน้าแรก)</summary>
    public static bool Relaxed { get; set; }

    /// <summary>มีหน้าต่างของงานแสดงเปิดทับเกมอยู่ (การ์ดสอนเล่น / หยุดเกม) เกมด้านหลังไม่ควรรับปุ่ม</summary>
    public static bool InputBlocked => HowToPlayCard.IsOpen || PadPauseMenu.IsOpen || Time.frameCount <= blockedUntilFrame;
    private static int blockedUntilFrame = -1;
    public static void BlockInputThisFrame() => blockedUntilFrame = Time.frameCount + 1;

    private float lastInputAt;
    private Vector3 lastMouse;
    private float lastMouseMoveAt;
    private string previousScene;
    private float holdStartedAt = -1f;
    private float nextPadCheckAt;
    private bool padConnected = true;
    private bool attractShown;

    // UI
    private RectTransform canvas;
    private Text toastText, warningText, padWarningText, timeLeftText;
    private float toastUntil;
    private GameObject attractRoot, warningRoot, padWarningRoot, holdRoot, padHudRoot;
    private Image holdFill;
    private Text holdLabel;
    private Dictionary<int, Image> padHud;
    private Text attractScores;
    private AudioSource voice;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (Instance != null) return;
        var go = new GameObject("ExhibitMode");
        DontDestroyOnLoad(go);
        go.AddComponent<ExhibitMode>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        Relaxed = false;
        blockedUntilFrame = -1;
    }

    private void Awake()
    {
        Instance = this;
        On = PlayerPrefs.GetInt(PrefOn, 1) == 1;
        voice = gameObject.AddComponent<AudioSource>();
        voice.ignoreListenerPause = true;
        voice.playOnAwake = false;
        BuildUI();
        SceneManager.sceneLoaded += OnSceneLoaded;
        Application.wantsToQuit += () => !On || Application.isEditor;
        ApplyKiosk();
        lastInputAt = Time.unscaledTime;
        previousScene = null;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #region Scene / session
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        lastInputAt = Time.unscaledTime;
        holdStartedAt = -1f;
        HideAttract();

        // ผู้เล่นคนใหม่เริ่มที่หน้าแรก: ล้างค่าที่คนก่อนตั้งไว้
        if (scene.name == MainMenuScene) ResetSession();

        // การ์ดสอนเล่น: แสดงเมื่อเข้าเกมจากหน้าเลือกเกม (เล่นใหม่/ไปด่านต่อไป ไม่ต้องดูซ้ำ)
        string game = GameOfScene(scene.name);
        bool fromMenu = previousScene == null || previousScene == QuestBoardScene || previousScene == MainMenuScene;
        if (On && game != null && fromMenu) HowToPlayCard.Show(game);

        previousScene = scene.name;
    }

    public static string GameOfScene(string scene)
    {
        if (scene == "ArrowMemory") return "Arrow";
        if (scene == "CookieRunner") return "Runner";
        if (scene == "Tutorialmine" || scene.StartsWith("Cave")) return "Cave";
        return null;
    }

    private static bool IsCaveScene(string scene) => scene == "Tutorialmine" || (scene.StartsWith("Cave") && scene != "Cavedef");

    /// <summary>ล้างค่าของผู้เล่นคนก่อน (ระดับความยาก เสียงพูด น้ำหนัก ระดับสบาย ๆ)</summary>
    public static void ResetSession()
    {
        Relaxed = false;
        foreach (string key in new[] { "ArrowMemoryDifficulty", "ArrowMemoryVoiceMode", "ArrowMemoryVoice", "UserWeightKg" })
            PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        CookieRun.CookieTutorialManager.SkipNextTutorial = false;
    }

    /// <summary>ใช้ระดับ "สบาย ๆ" กับสิ่งที่สร้างไปแล้วในฉาก (เรียกตอนปิดการ์ดสอนเล่น)</summary>
    public static void ApplyRelaxedToScene()
    {
        foreach (CookieRun.CookieHealth h in FindObjectsOfType<CookieRun.CookieHealth>()) h.ApplyDifficulty();
    }

    private void GoHome()
    {
        PadPauseMenu.Close();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        lastInputAt = Time.unscaledTime;
        SceneManager.LoadScene(MainMenuScene);
    }
    #endregion

    private void Update()
    {
        HandleStaffKeys();

        string scene = SceneManager.GetActiveScene().name;
        bool active = AnyInput();
        if (active) lastInputAt = Time.unscaledTime;

        UpdateToast();
        if (!On)
        {
            Cursor.visible = true;
            SetActive(attractRoot, false); SetActive(warningRoot, false); SetActive(padWarningRoot, false);
            SetActive(holdRoot, false); SetActive(padHudRoot, false); SetActive(timeLeftText != null ? timeLeftText.gameObject : null, false);
            attractShown = false;
            return;
        }

        // เมาส์: ซ่อนถ้าไม่ได้ขยับ 3 วินาที
        if ((Input.mousePosition - lastMouse).sqrMagnitude > 4f) { lastMouseMoveAt = Time.unscaledTime; lastMouse = Input.mousePosition; }
        Cursor.visible = Time.unscaledTime - lastMouseMoveAt < 3f;

        UpdateIdle(scene, active);
        UpdatePadStatus();
        UpdatePadHud(scene);
        UpdatePauseHold(scene);
        UpdateRunnerTimeLeft(scene);
    }

    private static bool AnyInput()
    {
        // ยืนเหยียบค้างก็นับว่ายังมีคนเล่นอยู่
        return Input.anyKey || Input.anyKeyDown || Input.GetMouseButton(0) || Mathf.Abs(Input.GetAxis("Mouse X")) > 0.01f || Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.01f;
    }

    #region Idle / attract
    private void UpdateIdle(string scene, bool active)
    {
        float idle = Time.unscaledTime - lastInputAt;

        if (scene == MainMenuScene)
        {
            SetActive(warningRoot, false);
            if (!attractShown && idle >= attractAfter) ShowAttract();
            else if (attractShown && active) HideAttract();
            if (attractShown)
            {
                float k = 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 3f);
                attractRoot.transform.Find("Title").localScale = Vector3.one * k;
            }
            return;
        }

        // หน้าอื่น: เตือนก่อน แล้วกลับหน้าแรก
        float left = idleToHome - idle;
        bool warn = left <= idleWarning;
        SetActive(warningRoot, warn);
        if (warn) warningText.text = $"ไม่มีการเหยียบ จะกลับหน้าแรกใน {Mathf.CeilToInt(Mathf.Max(0f, left))} วินาที\n<size=34>เหยียบแผ่นไหนก็ได้เพื่อเล่นต่อ</size>";
        if (left <= 0f) GoHome();
    }

    private void ShowAttract()
    {
        attractShown = true;
        SetActive(attractRoot, true);
        attractScores.text = BuildTodayScores();
        AudioClip clip = Resources.Load<AudioClip>("Exhibit/Attract");
        if (clip != null) voice.PlayOneShot(clip);
    }

    private void HideAttract()
    {
        attractShown = false;
        SetActive(attractRoot, false);
    }

    private static string BuildTodayScores()
    {
        List<int> arrow = DailyLeaderboard.Top(DailyLeaderboard.ArrowMemory, 3);
        List<int> run = DailyLeaderboard.Top(DailyLeaderboard.Runner, 3);
        if (arrow.Count == 0 && run.Count == 0) return "ยังไม่มีใครเล่นวันนี้ มาเป็นคนแรกกัน!";
        string Line(List<int> list, string unit)
        {
            if (list.Count == 0) return "-";
            var parts = new List<string>();
            for (int i = 0; i < list.Count; i++) parts.Add($"{i + 1}. {list[i]:N0}{unit}");
            return string.Join("     ", parts);
        }
        return $"<color=#FFC74D>สถิติวันนี้</color>\nจำลูกศร (คะแนน):  {Line(arrow, "")}\nวิ่งผจญภัย (ระยะทาง):  {Line(run, " ม.")}";
    }
    #endregion

    #region Pause by holding (runner / cave)
    // โหมดวิ่ง: ซ้าย/ขวาไม่ได้ใช้เล่น -> เหยียบซ้ายหรือขวาค้าง = หยุดเกม
    // โหมดถ้ำ: ใช้ครบ 4 ทิศ และเหยียบค้าง = ปักธง -> เหยียบซ้ายและขวาพร้อมกันค้าง = หยุดเกม
    private void UpdatePauseHold(string scene)
    {
        bool runner = scene == "CookieRunner";
        bool cave = IsCaveScene(scene);
        bool canPause = (runner || cave) && !HowToPlayCard.IsOpen && !PadPauseMenu.IsOpen && !ResultPopupUI.IsOpen;
        if (runner && CookieRun.CookieGameManager.Instance != null && CookieRun.CookieGameManager.Instance.CurrentState == CookieRun.GameState.GameOver) canPause = false;

        if (canPause && Input.GetKeyDown(KeyCode.Escape) && !(runner && CookieRun.CookieGameManager.Instance != null && CookieRun.CookieGameManager.Instance.IsTutorialActive))
        {
            OpenPause(runner);
            return;
        }

        bool left = Input.GetKey(GameInputBindings.PadLeft) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
        bool right = Input.GetKey(GameInputBindings.PadRight) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
        bool holding = canPause && (runner ? (left || right) : (left && right));
        if (!holding)
        {
            holdStartedAt = -1f;
            SetActive(holdRoot, false);
            return;
        }

        if (holdStartedAt < 0f) holdStartedAt = Time.unscaledTime;
        float t = (Time.unscaledTime - holdStartedAt) / holdToPause;
        // แตะสั้นๆ ไม่ต้องโชว์อะไร
        SetActive(holdRoot, t > 0.25f);
        holdFill.fillAmount = Mathf.Clamp01(t);
        if (t >= 1f)
        {
            holdStartedAt = -1f;
            SetActive(holdRoot, false);
            OpenPause(runner);
        }
    }

    private static void OpenPause(bool runner)
    {
        PadPauseMenu.Show(runner ? "ระหว่างเล่น: เหยียบซ้ายหรือขวาค้างไว้ = หยุดเกม" : "ระหว่างเล่น: เหยียบซ้ายและขวาพร้อมกันค้างไว้ = หยุดเกม");
    }

    private void UpdateRunnerTimeLeft(string scene)
    {
        CookieRun.CookieGameManager gm = scene == "CookieRunner" ? CookieRun.CookieGameManager.Instance : null;
        float left = gm != null ? gm.ExhibitTimeLeft : -1f;
        bool show = gm != null && gm.IsPlaying && left >= 0f && left <= 60f;
        SetActive(timeLeftText.gameObject, show);
        if (show) timeLeftText.text = $"เหลือเวลา {Mathf.FloorToInt(left / 60f)}:{Mathf.FloorToInt(left % 60f):00}";
    }
    #endregion

    #region Pad status / HUD
    private void UpdatePadStatus()
    {
        if (Time.unscaledTime >= nextPadCheckAt)
        {
            nextPadCheckAt = Time.unscaledTime + 2f;
            padConnected = false;
            foreach (string n in Input.GetJoystickNames())
                if (!string.IsNullOrEmpty(n)) { padConnected = true; break; }
        }
        SetActive(padWarningRoot, !padConnected);
    }

    private void UpdatePadHud(string scene)
    {
        bool show = !attractShown && scene != MainMenuScene;
        SetActive(padHudRoot, show);
        if (!show) return;
        KeyCode[] pads = { GameInputBindings.PadUp, GameInputBindings.PadDown, GameInputBindings.PadLeft, GameInputBindings.PadRight };
        KeyCode[] keys = { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow };
        Color[] colors = { ExhibitUI.PadUpColor, ExhibitUI.PadDownColor, ExhibitUI.PadLeftColor, ExhibitUI.PadRightColor };
        for (int i = 0; i < 4; i++)
        {
            bool down = Input.GetKey(pads[i]) || Input.GetKey(keys[i]);
            Color c = colors[i];
            padHud[i].color = down ? c : new Color(c.r, c.g, c.b, 0.25f);
            padHud[i].transform.localScale = Vector3.one * (down ? 1.12f : 1f);
        }
    }
    #endregion

    #region Staff keys / kiosk
    private void HandleStaffKeys()
    {
        if (Input.GetKeyDown(KeyCode.F12))
        {
            On = !On;
            PlayerPrefs.SetInt(PrefOn, On ? 1 : 0);
            PlayerPrefs.Save();
            ApplyKiosk();
            Toast(On ? "โหมดงานแสดง: เปิด" : "โหมดงานแสดง: ปิด (ปุ่ม Option / Exit กลับมาแล้ว)");
        }
        if (Input.GetKeyDown(KeyCode.F9) || Input.GetKeyDown(KeyCode.F10))
        {
            float v = Mathf.Clamp01(Mathf.Round((AudioListener.volume + (Input.GetKeyDown(KeyCode.F10) ? 0.1f : -0.1f)) * 10f) / 10f);
            AudioListener.volume = v;
            PlayerPrefs.SetFloat(ArcadeMenuController.PREF_VOLUME, v);
            PlayerPrefs.Save();
            Toast($"ความดังเสียง {Mathf.RoundToInt(v * 100)}%");
        }
    }

    private static void ApplyKiosk()
    {
        if (On && !Application.isEditor)
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.fullScreen = true;
        }
        if (!On) Cursor.visible = true;
    }

    public static void Toast(string text)
    {
        if (Instance == null) return;
        Instance.toastText.text = text;
        Instance.toastUntil = Time.unscaledTime + 2.5f;
    }

    private void UpdateToast()
    {
        float a = Mathf.Clamp01((toastUntil - Time.unscaledTime) / 0.4f);
        toastText.transform.parent.gameObject.SetActive(a > 0f);
        toastText.color = new Color(ExhibitUI.Ink.r, ExhibitUI.Ink.g, ExhibitUI.Ink.b, a);
    }
    #endregion

    #region UI
    private void BuildUI()
    {
        canvas = ExhibitUI.NewCanvas("ExhibitCanvas", 980, transform);
        canvas.GetComponent<GraphicRaycaster>().enabled = false; // แค่แสดงผล ไม่บังการคลิก

        // ป้ายเชิญเล่นหน้าแรก
        attractRoot = new GameObject("Attract", typeof(RectTransform));
        attractRoot.transform.SetParent(canvas, false);
        ExhibitUI.Stretch((RectTransform)attractRoot.transform);
        Image dim = ExhibitUI.NewImage("Dim", attractRoot.transform, null, new Color(0f, 0f, 0f, 0.62f));
        ExhibitUI.Stretch(dim.rectTransform);
        Text title = ExhibitUI.NewText("Title", attractRoot.transform, "เหยียบแผ่นไหนก็ได้\nเพื่อเริ่มเล่น!", 110, ExhibitUI.Gold);
        ExhibitUI.Place(title.rectTransform, 0, 300, 1800, 300);
        ExhibitUI.NewPadDiagram(attractRoot.transform, 0, -20, 120, "", "", "", "");
        Text sub = ExhibitUI.NewText("Sub", attractRoot.transform, "เกมออกกำลังกายด้วยแผ่นเหยียบ 3 เกม  เล่นได้ทุกวัย", 44, ExhibitUI.Ink, TextAnchor.MiddleCenter, FontStyle.Normal);
        ExhibitUI.Place(sub.rectTransform, 0, -260, 1800, 60);
        attractScores = ExhibitUI.NewText("Scores", attractRoot.transform, "", 36, ExhibitUI.Ink, TextAnchor.MiddleCenter, FontStyle.Normal);
        ExhibitUI.Place(attractScores.rectTransform, 0, -390, 1800, 160);
        attractRoot.SetActive(false);

        // เตือนก่อนกลับหน้าแรก
        Image warnBg = ExhibitUI.NewPanel("IdleWarning", canvas, new Color(0.45f, 0.08f, 0.05f, 0.92f), ExhibitUI.Line);
        ExhibitUI.Place(warnBg.rectTransform, 0, 0, 1200, 220);
        warningRoot = warnBg.gameObject;
        warningText = ExhibitUI.NewText("Text", warnBg.transform, "", 50, ExhibitUI.Ink);
        ExhibitUI.Stretch(warningText.rectTransform);
        warningRoot.SetActive(false);

        // แผ่นเหยียบหลุด
        Image padBg = ExhibitUI.NewPanel("PadWarning", canvas, new Color(0.6f, 0.1f, 0.05f, 0.95f), ExhibitUI.Line);
        RectTransform pr = padBg.rectTransform;
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 1f);
        pr.pivot = new Vector2(0.5f, 1f);
        pr.anchoredPosition = new Vector2(0f, -16f);
        pr.sizeDelta = new Vector2(1100f, 90f);
        padWarningRoot = padBg.gameObject;
        padWarningText = ExhibitUI.NewText("Text", padBg.transform, "แผ่นเหยียบไม่ได้เชื่อมต่อ  กรุณาเรียกทีมงาน", 42, ExhibitUI.Ink);
        ExhibitUI.Stretch(padWarningText.rectTransform);
        padWarningRoot.SetActive(false);

        // แถบเหยียบค้างเพื่อหยุดเกม
        Image holdBg = ExhibitUI.NewPanel("HoldToPause", canvas, ExhibitUI.Panel, ExhibitUI.Line);
        ExhibitUI.Place(holdBg.rectTransform, 0, 330, 760, 110);
        holdRoot = holdBg.gameObject;
        holdLabel = ExhibitUI.NewText("Label", holdBg.transform, "ค้างไว้เพื่อหยุดเกม...", 40, ExhibitUI.Ink);
        ExhibitUI.Place(holdLabel.rectTransform, 0, 18, 740, 56);
        Image barBg = ExhibitUI.NewImage("BarBg", holdBg.transform, null, new Color(1f, 1f, 1f, 0.15f));
        ExhibitUI.Place(barBg.rectTransform, 0, -30, 660, 18);
        holdFill = ExhibitUI.NewImage("Fill", barBg.transform, null, ExhibitUI.Gold);
        ExhibitUI.Stretch(holdFill.rectTransform);
        holdFill.type = Image.Type.Filled;
        holdFill.fillMethod = Image.FillMethod.Horizontal;
        holdFill.sprite = ExhibitUI.RoundSprite;
        holdRoot.SetActive(false);

        // แผ่นเหยียบเล็กมุมขวาล่าง: สว่างเมื่อเหยียบ (ผู้เล่นรู้ว่าเครื่องรับการเหยียบแล้ว)
        padHudRoot = new GameObject("PadHud", typeof(RectTransform));
        padHudRoot.transform.SetParent(canvas, false);
        RectTransform hr = (RectTransform)padHudRoot.transform;
        hr.anchorMin = hr.anchorMax = new Vector2(1f, 0f);
        hr.pivot = new Vector2(1f, 0f);
        hr.anchoredPosition = new Vector2(-20f, 20f);
        hr.sizeDelta = new Vector2(130f, 130f);
        padHud = ExhibitUI.NewPadDiagram(padHudRoot.transform, 0, 0, 40, "", "", "", "");

        // เวลาที่เหลือของโหมดวิ่ง
        timeLeftText = ExhibitUI.NewText("RunnerTimeLeft", canvas, "", 44, ExhibitUI.Gold);
        RectTransform tr = timeLeftText.rectTransform;
        tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 1f);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, -120f);
        tr.sizeDelta = new Vector2(600f, 60f);
        timeLeftText.gameObject.SetActive(false);

        // ข้อความแจ้งสั้นๆ (ทีมงานกดปุ่มลับ)
        Image toastBg = ExhibitUI.NewPanel("Toast", canvas, ExhibitUI.Panel, ExhibitUI.Line);
        RectTransform tb = toastBg.rectTransform;
        tb.anchorMin = tb.anchorMax = new Vector2(0.5f, 0f);
        tb.pivot = new Vector2(0.5f, 0f);
        tb.anchoredPosition = new Vector2(0f, 120f);
        tb.sizeDelta = new Vector2(1000f, 80f);
        toastText = ExhibitUI.NewText("Text", toastBg.transform, "", 36, ExhibitUI.Ink);
        ExhibitUI.Stretch(toastText.rectTransform);
        toastBg.gameObject.SetActive(false);
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active) go.SetActive(active);
    }
    #endregion
}
