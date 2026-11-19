using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pulse Steps: Arrow Memory (พอร์ตจากเวอร์ชันเว็บ index.html)
/// เกมความจำ: ดูลูกศรที่สว่างทีละอัน แล้วเหยียบตามลำดับ ยิ่งผ่านมาก ลำดับยิ่งยาว
/// - 3 ระดับ (ง่าย/ปกติ/ยาก) เริ่มด้วย 3 หัวใจ ผิด = เสียหัวใจและดูลำดับใหม่
/// - ผ่านทุก 3 รอบ ได้หัวใจ +1 และเวลา +30 วินาที / หมดเวลาหรือหมดหัวใจ = จบเกม
/// - เสียงพูดบอกทิศทาง เลือกได้ 3 แบบ: ปิดเสียง / เสียง AI (Windows TTS) / เสียงคนจริง (ไฟล์ที่อัดไว้)
/// - ใช้ปุ่มเดิน 4 ทิศของทั้งเกม (GameInputBindings: แผ่นเหยียบ + คีย์บอร์ด ตั้งค่าได้ในหน้า Option)
/// - ทุกปุ่มใช้แผ่นเหยียบได้: หน้าเมนู ลง = เลื่อนแถว, ซ้าย/ขวา = เปลี่ยนค่า, ขึ้น = กดปุ่ม / ระหว่างเล่น เหยียบลงค้างไว้ = หยุดเกม
/// UI ทั้งหมดสร้างด้วยโค้ด ไม่ต้องจัดใน Scene
/// </summary>
public class ArrowMemoryGame : MonoBehaviour
{
    private enum Dir { Up, Left, Down, Right }
    private enum Phase { Menu, Starting, Preview, Retry, Input, Clear, Finished }

    private class Difficulty
    {
        public string label, labelEn, hint;
        public float seconds;
        public int startLength, add, points;
        public float show, gap; // วินาที
    }

    private static readonly Difficulty[] Difficulties =
    {
        new Difficulty { label = "ง่าย", labelEn = "EASY", hint = "จังหวะสบาย", seconds = 75, startLength = 2, add = 1, show = 0.72f, gap = 0.36f, points = 100 },
        new Difficulty { label = "ปกติ", labelEn = "NORMAL", hint = "ท้าทายกำลังดี", seconds = 65, startLength = 3, add = 1, show = 0.50f, gap = 0.21f, points = 150 },
        new Difficulty { label = "ยาก", labelEn = "HARD", hint = "เร็วและยาวกว่า", seconds = 55, startLength = 4, add = 2, show = 0.31f, gap = 0.115f, points = 220 },
    };

    private const int StartLives = 3, RoundsPerHeart = 3;
    private const float BonusSeconds = 30f, KcalPerStep = 0.04f;

    [Header("Scene")]
    [Tooltip("ฉากที่กลับไปเมื่อออกจากเกม (ปุ่ม \"หน้าเลือกเกม\")")]
    [SerializeField] private string exitSceneName = "QuestBoard";

    [Header("Voice: เสียงคนจริง (ถ้าว่าง จะโหลด ArrowVoice_* จาก Resources/ArrowMemory)")]
    [SerializeField] private AudioClip voiceStart;
    [SerializeField] private AudioClip voiceUp;
    [SerializeField] private AudioClip voiceDown;
    [SerializeField] private AudioClip voiceLeft;
    [SerializeField] private AudioClip voiceRight;

    [Header("Voice: เสียง AI (ถ้าว่าง จะโหลด ArrowVoiceAI_* จาก Resources/ArrowMemory)")]
    [SerializeField] private AudioClip aiStart;
    [SerializeField] private AudioClip aiUp;
    [SerializeField] private AudioClip aiDown;
    [SerializeField] private AudioClip aiLeft;
    [SerializeField] private AudioClip aiRight;
    [Range(0f, 1f)] [SerializeField] private float voiceVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float toneVolume = 0.35f;

    [Header("Pad Menu")]
    [Tooltip("หน่วงเวลาก่อนรับปุ่มในหน้าเมนู/สรุปผล กันเท้าที่ยังเหยียบอยู่")]
    [SerializeField] private float menuInputDelay = 0.6f;
    [Tooltip("ระหว่างเล่น เหยียบลงค้างไว้นานเท่านี้ (วินาที) = หยุดเกม")]
    [SerializeField] private float holdToPause = 1.2f;

    // ---------- สี (ตามเวอร์ชันเว็บ) ----------
    private static readonly Color Ink = Hex(0xedf7ff), Muted = Hex(0x9db1c8);
    private static readonly Color Cyan = Hex(0x3ee8ff), Pink = Hex(0xff67c5), Lime = Hex(0xc5ff61), Gold = Hex(0xffd166);
    private static readonly Color PanelColor = new Color(13 / 255f, 25 / 255f, 48 / 255f, 0.92f);
    private static readonly Color LineColor = new Color(181 / 255f, 221 / 255f, 255 / 255f, 0.17f);
    private static readonly Color DeepBg = Hex(0x07101f);

    private static Color DirColor(Dir d) => d == Dir.Up ? Cyan : d == Dir.Left ? Pink : d == Dir.Down ? Lime : Gold;
    private static float DirAngle(Dir d) => d == Dir.Up ? 0f : d == Dir.Left ? 90f : d == Dir.Down ? 180f : -90f;
    private static readonly Dir[] AllDirs = { Dir.Up, Dir.Left, Dir.Down, Dir.Right };

    // ---------- สถานะเกม ----------
    private int difficulty = 1;
    private Phase phase = Phase.Menu;
    private bool running;
    private readonly List<Dir> sequence = new List<Dir>();
    private int inputIndex, score, round, steps, lives = StartLives, maxLives = StartLives;
    private float startedAt, bonusTime;
    private Coroutine flowRoutine;
    private enum VoiceMode { Off, AI, Human }
    private VoiceMode voiceMode = VoiceMode.AI;
    private bool VoiceOn => voiceMode != VoiceMode.Off;
    private bool paused;
    private float downHeldSince = -1f, downPenaltyAt = -1f;
    private float menuShownAt;
    private bool showingResults;
    private bool warnedReload;
    private bool countingDown;
    private bool startVoicePlayed;

    private Difficulty Settings => Difficulties[difficulty];
    private float SecondsLeft => Settings.seconds + bonusTime - (Time.time - startedAt);

    // ---------- UI ----------
    private Font font;
    private Sprite roundSprite, arrowSprite, heartSprite, glowSprite;
    private Sprite personIcon, wavesIcon, slashIcon, homeIcon;
    private readonly Dictionary<int, Sprite> ringSprites = new Dictionary<int, Sprite>();
    private RectTransform canvasRoot;
    private Text scoreText, roundText, timeText, roundLabel, phaseText, doneText, totalText, progressText, feedbackText, stepsText, caloriesText, heartHint;
    private Image signalBox, signalArrow, signalGlow, seqPanel, voiceButtonImage, homeButtonImage, holdFill;
    private Text voiceBadge;
    private Text signalQuestion;
    private RectTransform slotGrid, heartsRow, timeStat;
    private readonly List<Image> slots = new List<Image>();
    private readonly Dictionary<Dir, Image> padButtons = new Dictionary<Dir, Image>();
    private readonly Dictionary<Dir, float> padFlashUntil = new Dictionary<Dir, float>();
    private readonly List<Image> hearts = new List<Image>();
    private GameObject overlay;
    private RectTransform modal;
    private readonly List<Image> difficultyTiles = new List<Image>();
    private readonly List<Image> voiceTiles = new List<Image>();
    private float feedbackUntil, heartsGainUntil, timeGainUntil, doneBumpUntil, signalLitScale;
    private int wrongSlot = -1;
    private float wrongSlotUntil;

    // ---------- เสียง ----------
    private AudioSource voiceSource, toneSource;
    private readonly Dictionary<string, AudioClip> toneCache = new Dictionary<string, AudioClip>();

    #region Unity
    private void Awake()
    {
        Time.timeScale = 1f;
        // ค่าเก่า ArrowMemoryVoice (เปิด/ปิด) แปลงเป็น เสียง AI / ปิดเสียง
        int legacy = PlayerPrefs.GetInt("ArrowMemoryVoice", 1) == 1 ? (int)VoiceMode.AI : (int)VoiceMode.Off;
        voiceMode = (VoiceMode)Mathf.Clamp(PlayerPrefs.GetInt("ArrowMemoryVoiceMode", legacy), 0, 2);
        difficulty = Mathf.Clamp(PlayerPrefs.GetInt("ArrowMemoryDifficulty", 1), 0, Difficulties.Length - 1);

        LoadVoiceClips();
        voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        toneSource = gameObject.AddComponent<AudioSource>();
        toneSource.playOnAwake = false;

        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = DeepBg;
        }

        BuildSprites();
        BuildUI();
        ShowHome();
    }

    private void Update()
    {
        // Unity โหลดสคริปต์ใหม่ระหว่างกด Play (แก้โค้ดตอนเล่นอยู่): ข้อมูล UI ที่สร้างตอน Awake หายไป และ Awake ไม่ถูกเรียกซ้ำ
        // หยุดทำงานจนกว่าจะกด Play ใหม่ แทนการขึ้น error ทุกเฟรม
        if (padButtons.Count == 0 || overlay == null)
        {
            if (!warnedReload) { Debug.LogWarning("[ArrowMemory] สคริปต์ถูกโหลดใหม่ระหว่างเล่น กรุณากดหยุดแล้วกด Play ใหม่"); warnedReload = true; }
            return;
        }

        HandleInput();
        UpdateHudTimer();
        AnimateUI();
    }
    #endregion

    #region Input
    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.V)) CycleVoice(1);

        Dir? pressed = ReadDirection();

        if (overlay != null && overlay.activeSelf)
        {
            HandleMenuInput(pressed);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape)) { OpenPause(); return; }
        if (pressed.HasValue) AcceptDirection(pressed.Value);
        UpdateHoldToPause();
    }

    // ระหว่างเล่น: เหยียบลงค้างไว้ = หยุดเกม (แทนการเอาเมาส์กดปุ่มด้านบน) มีวงแหวนบนปุ่มบ้านบอกความคืบหน้า
    private static bool IsDownHeld() =>
        GameInputBindings.GetHeld(GameAction.MoveDown) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

    private void UpdateHoldToPause()
    {
        bool held = IsDownHeld();
        if (!held || !running)
        {
            downHeldSince = -1f;
            holdFill.fillAmount = 0f;
            return;
        }
        if (downHeldSince < 0f) downHeldSince = Time.unscaledTime;

        // เหยียบปกติไม่ถึง 0.25 วินาที ไม่ต้องโชว์อะไร
        const float showAfter = 0.25f;
        float held01 = Mathf.Clamp01((Time.unscaledTime - downHeldSince - showAfter) / Mathf.Max(0.1f, holdToPause - showAfter));
        holdFill.fillAmount = held01;
        if (held01 > 0f) Flash("ค้างไว้เพื่อหยุดเกม", Muted);
        if (held01 >= 1f)
        {
            RefundHoldPress();
            OpenPause();
        }
    }

    // การเหยียบลงที่เริ่มค้างเพื่อหยุดเกม ถูกนับเป็นคำตอบผิดไปแล้ว: คืนหัวใจและจำนวนก้าว ไม่ให้เสียอะไรจากการหยุดเกม
    private void RefundHoldPress()
    {
        bool sameStep = downPenaltyAt >= 0f && downHeldSince >= 0f && downPenaltyAt >= downHeldSince - 0.05f;
        downPenaltyAt = -1f;
        if (!sameStep) return;

        lives = Mathf.Min(lives + 1, maxLives);
        steps = Mathf.Max(0, steps - 1);
        UpdateHud();
        UpdateHeartsColors();
        if (phase == Phase.Finished)
        {
            // หัวใจดวงสุดท้าย: ยกเลิกการจบเกม แล้วโชว์ลำดับเดิมให้ดูใหม่หลังกดเล่นต่อ
            StopFlow();
            phase = Phase.Retry;
            progressText.text = "จะแสดงลำดับให้ดูอีกครั้ง";
            flowRoutine = StartCoroutine(After(0.4f, () => { if (running) PlaySequence(); }));
        }
    }

    // ทิศที่กด: แผ่นเหยียบ / ปุ่มที่ตั้งในหน้า Option + W A S D + ปุ่มลูกศร ใช้ได้เสมอ
    private static Dir? ReadDirection()
    {
        if (GameInputBindings.GetDown(GameAction.MoveUp) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) return Dir.Up;
        if (GameInputBindings.GetDown(GameAction.MoveDown) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) return Dir.Down;
        if (GameInputBindings.GetDown(GameAction.MoveLeft) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) return Dir.Left;
        if (GameInputBindings.GetDown(GameAction.MoveRight) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) return Dir.Right;
        return null;
    }

    // หน้าเลือกระดับ / หน้าหยุดเกม / หน้าสรุปผล: แผ่นเหยียบ + คีย์บอร์ด + เมาส์
    private void HandleMenuInput(Dir? pressed)
    {
        bool confirmKey = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter);
        if (Time.unscaledTime - menuShownAt < menuInputDelay) return;

        if (showingResults)
        {
            // ซ้าย/ขวา = เลื่อนเลือกปุ่ม / ขึ้น หรือ Enter = กดปุ่มที่เลือก / Esc = หน้าเลือกเกม
            if (pressed == Dir.Left) SetFocus(focusIndex - 1, true);
            else if (pressed == Dir.Right) SetFocus(focusIndex + 1, true);
            else if ((pressed == Dir.Up || confirmKey) && focusIndex >= 0 && focusIndex < focusActions.Count) focusActions[focusIndex]();
            else if (Input.GetKeyDown(KeyCode.Escape)) ExitGame();
            return;
        }

        // หน้าแรก / หน้าหยุดเกม เป็นแถว: ลง = เลื่อนแถว (กรอบสีฟ้า) / ซ้าย-ขวา = เปลี่ยนค่าในแถวนั้น
        // ขึ้น หรือ Enter = กดปุ่มที่สว่างอยู่ (ค่าเริ่มต้น "เริ่ม" / "เล่นต่อ") ไม่ว่ากรอบอยู่แถวไหน
        if (pressed == Dir.Down) SetMenuRow(menuRow + 1, true);
        else if (pressed == Dir.Left || pressed == Dir.Right)
        {
            int step = pressed == Dir.Left ? -1 : 1;
            switch (CurrentRow)
            {
                case MenuRow.Difficulty: SelectDifficulty(difficulty + step); break;
                case MenuRow.Voice: CycleVoice(step); break;
                case MenuRow.Buttons: SetFocus(focusIndex + step, true); break;
            }
        }
        else if ((pressed == Dir.Up || confirmKey) && focusIndex >= 0 && focusIndex < focusActions.Count) focusActions[focusIndex]();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (paused) ResumeGame();
            else ExitGame();
        }
    }

    // ---------- แถวในหน้าแรก / หน้าหยุดเกม ----------
    private enum MenuRow { Difficulty, Voice, Buttons }
    private readonly List<MenuRow> menuRows = new List<MenuRow>();
    private readonly List<Image> menuRowFrames = new List<Image>();
    private int menuRow;
    private MenuRow CurrentRow => menuRows.Count > 0 ? menuRows[Mathf.Clamp(menuRow, 0, menuRows.Count - 1)] : MenuRow.Buttons;

    // กรอบของแถว สร้างก่อนเนื้อหาในแถว จะได้อยู่ข้างหลัง / เอาเมาส์ชี้ = เลือกแถวนั้น
    private void AddMenuRow(MenuRow kind, float y, float height)
    {
        int index = menuRows.Count;
        Image frame = NewPanel("RowFrame", modal, Color.clear, Color.clear, 18, 2);
        Place(frame.rectTransform, 0, y, 790, height);
        menuRows.Add(kind);
        menuRowFrames.Add(frame);
        frame.raycastTarget = true;
        AddHover(frame.gameObject, () => SetMenuRow(index, false));
    }

    private void SetMenuRow(int index, bool sound)
    {
        if (menuRows.Count == 0) return;
        int next = (index + menuRows.Count) % menuRows.Count;
        if (next != menuRow && sound) PlayTone(560f, 0.05f, Wave.Triangle);
        menuRow = next;
    }

    private void ClearMenuRows()
    {
        menuRows.Clear();
        menuRowFrames.Clear();
        voiceTiles.Clear();
        menuRow = 0;
    }

    // ---------- ปุ่มที่เลื่อนเลือกได้ในหน้าสรุปผล ----------
    private readonly List<Image> focusButtons = new List<Image>();
    private readonly List<System.Action> focusActions = new List<System.Action>();
    private int focusIndex = -1;

    private void AddFocusButton(Image button, System.Action action)
    {
        int index = focusButtons.Count;
        focusButtons.Add(button);
        focusActions.Add(action);
        AddHover(button.gameObject, () => SetFocus(index, false));
    }

    private void SetFocus(int index, bool sound)
    {
        if (focusButtons.Count == 0) return;
        int next = (index + focusButtons.Count) % focusButtons.Count;
        if (next != focusIndex && sound) PlayTone(620f, 0.05f, Wave.Triangle);
        focusIndex = next;
    }

    // ปุ่มที่เมาส์กำลังชี้อยู่ (ใช้ทำสีตอนชี้)
    private readonly HashSet<Image> hovered = new HashSet<Image>();

    private void TrackHover(Image img)
    {
        EventTrigger trigger = img.GetComponent<EventTrigger>();
        if (trigger == null) trigger = img.gameObject.AddComponent<EventTrigger>();
        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => hovered.Add(img));
        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => hovered.Remove(img));
        trigger.triggers.Add(enter);
        trigger.triggers.Add(exit);
    }

    // เอาเมาส์ชี้ = เลือก (เหมือนเลื่อนด้วยแผ่นเหยียบ)
    private static void AddHover(GameObject go, System.Action onEnter)
    {
        EventTrigger trigger = go.GetComponent<EventTrigger>();
        if (trigger == null) trigger = go.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener(_ => onEnter());
        trigger.triggers.Add(entry);
    }

    #endregion

    #region Game Flow
    private void StartGame()
    {
        StopFlow();
        running = true;
        showingResults = false;
        phase = Phase.Starting;
        sequence.Clear();
        inputIndex = score = round = steps = 0;
        lives = maxLives = StartLives;
        bonusTime = 0f;
        startedAt = Time.time;
        startVoicePlayed = false;
        overlay.SetActive(false);
        roundLabel.text = $"{Settings.labelEn} MODE";
        RenderSeq("ready");
        RebuildHearts();
        UpdateHud();
        flowRoutine = StartCoroutine(CountdownRoutine());
    }

    // นับถอยหลัง 3-2-1 ครั้งเดียวหลังกดเริ่ม (ไม่มีเสียงพูด มีแค่เสียงติ๊ก) เวลาเกมยังไม่เดินระหว่างนับ
    private IEnumerator CountdownRoutine()
    {
        countingDown = true;
        voiceSource.Stop();
        roundLabel.text = "GET READY";
        for (int n = 3; n >= 1; n--)
        {
            progressText.text = $"เตรียมตัว... {n}";
            ShowCountdownNumber(n);
            PlayTone(n == 1 ? 880f : 660f, 0.12f, Wave.Triangle);
            yield return new WaitForSeconds(1f);
        }
        SetSignal(null);
        countingDown = false;
        startedAt = Time.time; // เริ่มนับเวลาเกมหลังนับถอยหลังจบ
        roundLabel.text = $"{Settings.labelEn} MODE";
        flowRoutine = null;
        StartRound();
    }

    private void ShowCountdownNumber(int n)
    {
        signalArrow.enabled = false;
        signalQuestion.enabled = true;
        signalQuestion.text = n.ToString();
        signalQuestion.fontSize = 220;
        signalQuestion.color = Cyan;
        signalBox.color = new Color(34 / 255f, 72 / 255f, 130 / 255f, 0.75f);
        SetBorder(signalBox, Cyan);
        signalGlow.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.45f);
        // เด้งขยายแล้วหดกลับทุกตัวเลข
        signalBox.transform.localScale = Vector3.one * 1.18f;
        signalLitScale = 1f;
    }

    private void StartRound()
    {
        if (!running) return;
        round++;
        AppendDirections(round == 1 ? Settings.startLength : Settings.add);
        UpdateHud();
        PlaySequence();
    }

    private void AppendDirections(int count)
    {
        for (int i = 0; i < count; i++) sequence.Add(AllDirs[Random.Range(0, AllDirs.Length)]);
    }

    private void PlaySequence()
    {
        StopFlow();
        flowRoutine = StartCoroutine(PreviewRoutine());
    }

    private IEnumerator PreviewRoutine()
    {
        phase = Phase.Preview;
        inputIndex = 0;
        roundLabel.text = "MEMORIZE";
        progressText.text = $"ดูลูกศรแล้วจำ {sequence.Count} จังหวะ";
        float show = Settings.show, gap = Settings.gap;
        // เร่งเสียงพูดนิดเดียวพอ เร่งมากเสียงจะแหลมจนฟังไม่ออก
        float voicePitch = show < 0.4f ? 1.1f : 1f;

        for (int i = 0; i < sequence.Count; i++)
        {
            Dir d = sequence[i];
            AudioClip clip = VoiceFor(d);
            // เปิดเสียงพูดอยู่: ยืดเวลาโชว์ให้พอพูดจบ ไม่โดนลูกศรถัดไปตัดกลางคำ
            float stepShow = show;
            if (VoiceOn && clip != null)
                stepShow = Mathf.Max(show, clip.length / voicePitch - gap + 0.03f);
            RenderSeq("preview", i);
            SetSignal(d);
            PulsePad(d, stepShow - 0.04f);
            ArrowTone(d, true);
            PlayVoice(clip, voicePitch);
            yield return new WaitForSeconds(stepShow);
            SetSignal(null);
            yield return new WaitForSeconds(gap);
        }

        RenderSeq("preview", sequence.Count);
        SetSignal(null);
        yield return new WaitForSeconds(gap);
        if (!running) yield break;

        phase = Phase.Input;
        roundLabel.text = "YOUR TURN";
        progressText.text = "เหยียบลูกศรตามลำดับที่จำไว้";
        RenderSeq("input");
        Flash("เริ่มได้!", Cyan);
        // เสียง "เริ่ม" พูดครั้งเดียวต่อเกม: หลังลูกศรรอบแรกโชว์ครบ ถึงตาผู้เล่นเหยียบครั้งแรก
        if (!startVoicePlayed)
        {
            PlayVoice(StartClip, 1f);
            startVoicePlayed = true;
        }
        flowRoutine = null;
    }

    private void AcceptDirection(Dir d)
    {
        if (!running || phase != Phase.Input) return;
        PulsePad(d, 0.14f);
        steps++;
        bool correct = sequence[inputIndex] == d;

        if (!correct)
        {
            lives--;
            // จำไว้ว่าเสียหัวใจจากการเหยียบลง: ถ้าเป็นจังหวะเริ่มเหยียบค้างเพื่อหยุดเกม จะคืนหัวใจให้
            downPenaltyAt = d == Dir.Down ? Time.unscaledTime : -1f;
            UpdateHud();
            PlayTone(150f, 0.16f, Wave.Saw);
            Flash("ลองใหม่", Pink);
            RenderSeq("bad", 0, inputIndex);
            progressText.text = lives > 0 ? "จะแสดงลำดับให้ดูอีกครั้ง" : "หมดชีวิตแล้ว";
            if (lives <= 0) { phase = Phase.Finished; flowRoutine = StartCoroutine(FinishUnlessHolding()); return; }
            phase = Phase.Retry;
            flowRoutine = StartCoroutine(After(0.7f, () => { if (running) PlaySequence(); }));
            return;
        }

        ArrowTone(d, false);
        score += Settings.points + inputIndex * 20;
        inputIndex++;
        UpdateHud();

        if (inputIndex < sequence.Count)
        {
            RenderSeq("input");
            Flash("ถูกต้อง", Lime);
            return;
        }

        // ผ่านรอบ
        phase = Phase.Clear;
        RenderSeq("clear");
        progressText.text = "เตรียมรอบถัดไป ลำดับจะยาวขึ้น";
        score += Settings.points * sequence.Count;
        bool earned = round % RoundsPerHeart == 0;
        if (earned)
        {
            lives++;
            bonusTime += BonusSeconds;
            heartsGainUntil = timeGainUntil = Time.time + 0.7f;
            RebuildHearts();
        }
        UpdateHud();
        Flash(earned ? $"+1 หัวใจ  +{BonusSeconds:0} วินาที" : "ยอดเยี่ยม!", Lime);
        PlayTone(earned ? 1046f : 880f, 0.12f, Wave.Triangle);
        if (earned) StartCoroutine(After(0.12f, () => PlayTone(1318f, 0.14f, Wave.Triangle)));
        flowRoutine = StartCoroutine(After(0.68f, StartRound));
    }

    private void Finish()
    {
        if (!running) return;
        running = false;
        StopFlow();
        voiceSource.Stop();
        SetSignal(null);
        PlayTone(880f, 0.18f, Wave.Triangle);
        phase = Phase.Finished;
        ShowResults();
    }

    private void StopFlow()
    {
        if (flowRoutine != null) StopCoroutine(flowRoutine);
        flowRoutine = null;
    }

    // หัวใจหมดจากการเหยียบลง: ถ้ายังเหยียบค้างอยู่ อาจกำลังจะหยุดเกม รอให้ค้างจนครบหรือปล่อยเท้าก่อนค่อยจบเกม
    private IEnumerator FinishUnlessHolding()
    {
        yield return new WaitForSeconds(0.65f);
        while (running && downHeldSince >= 0f && IsDownHeld()) yield return null;
        Finish();
    }

    private IEnumerator After(float seconds, System.Action action)
    {
        yield return new WaitForSeconds(seconds);
        action();
    }

    private void ExitGame()
    {
        StopFlow();
        running = false;
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded(exitSceneName)) SceneManager.LoadScene(exitSceneName);
        else SceneManager.LoadScene(0);
    }
    #endregion

    #region Audio
    private void LoadVoiceClips()
    {
        if (voiceStart == null) voiceStart = Resources.Load<AudioClip>("ArrowMemory/ArrowVoice_Start");
        if (voiceUp == null) voiceUp = Resources.Load<AudioClip>("ArrowMemory/ArrowVoice_Up");
        if (voiceDown == null) voiceDown = Resources.Load<AudioClip>("ArrowMemory/ArrowVoice_Down");
        if (voiceLeft == null) voiceLeft = Resources.Load<AudioClip>("ArrowMemory/ArrowVoice_Left");
        if (voiceRight == null) voiceRight = Resources.Load<AudioClip>("ArrowMemory/ArrowVoice_Right");
        if (aiStart == null) aiStart = Resources.Load<AudioClip>("ArrowMemory/ArrowVoiceAI_Start");
        if (aiUp == null) aiUp = Resources.Load<AudioClip>("ArrowMemory/ArrowVoiceAI_Up");
        if (aiDown == null) aiDown = Resources.Load<AudioClip>("ArrowMemory/ArrowVoiceAI_Down");
        if (aiLeft == null) aiLeft = Resources.Load<AudioClip>("ArrowMemory/ArrowVoiceAI_Left");
        if (aiRight == null) aiRight = Resources.Load<AudioClip>("ArrowMemory/ArrowVoiceAI_Right");
    }

    private AudioClip StartClip => voiceMode == VoiceMode.Human ? voiceStart : aiStart;

    private AudioClip VoiceFor(Dir d)
    {
        bool human = voiceMode == VoiceMode.Human;
        if (d == Dir.Up) return human ? voiceUp : aiUp;
        if (d == Dir.Left) return human ? voiceLeft : aiLeft;
        if (d == Dir.Down) return human ? voiceDown : aiDown;
        return human ? voiceRight : aiRight;
    }

    private static string VoiceLabel(VoiceMode mode) => mode == VoiceMode.Off ? "ปิดเสียง" : mode == VoiceMode.AI ? "เสียง AI" : "เสียงคนจริง";

    private void PlayVoice(AudioClip clip, float pitch)
    {
        if (!VoiceOn || clip == null) return;
        // ตัดเสียงเก่าก่อนพูดคำใหม่ (เหมือน speechSynthesis.cancel() ในเวอร์ชันเว็บ)
        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.pitch = pitch;
        voiceSource.volume = voiceVolume;
        voiceSource.Play();
    }

    // เปลี่ยนเสียงพูด ปิดเสียง → เสียง AI → เสียงคนจริง (ปุ่มด้านบน / ปุ่ม V / แถวเสียงพูดในเมนู)
    private void CycleVoice(int step)
    {
        voiceMode = (VoiceMode)(((int)voiceMode + step + 3) % 3);
        PlayerPrefs.SetInt("ArrowMemoryVoiceMode", (int)voiceMode);
        RenderVoiceButton();
        // อยู่ในเมนู/หยุดเกม: พูด "เริ่ม" ให้ฟังตัวอย่าง / กำลังเล่น: ไม่พูดแทรก แค่ขึ้นข้อความ
        bool inMenu = overlay != null && overlay.activeSelf;
        if (VoiceOn && (inMenu || !running)) PlayVoice(StartClip, 1f);
        else voiceSource.Stop();
        if (!inMenu) Flash(VoiceLabel(voiceMode), Cyan);
        PlayTone(600f + (int)voiceMode * 110f, 0.06f, Wave.Triangle);
    }

    private enum Wave { Sine, Triangle, Saw }

    private void ArrowTone(Dir d, bool preview)
    {
        float f = d == Dir.Up ? 660f : d == Dir.Left ? 440f : d == Dir.Down ? 330f : 550f;
        PlayTone(f, preview ? 0.12f : 0.07f, preview ? Wave.Sine : Wave.Triangle);
    }

    // สร้างเสียงสัญญาณด้วยโค้ด (แทน Web Audio oscillator) แล้วเก็บไว้ใช้ซ้ำ
    private void PlayTone(float frequency, float duration, Wave wave)
    {
        string key = $"{frequency}_{duration}_{wave}";
        if (!toneCache.TryGetValue(key, out AudioClip clip))
        {
            const int rate = 44100;
            int n = Mathf.CeilToInt(rate * duration);
            float[] data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float phaseT = frequency * t % 1f;
                float s = wave == Wave.Sine ? Mathf.Sin(2f * Mathf.PI * phaseT)
                        : wave == Wave.Triangle ? 1f - 4f * Mathf.Abs(phaseT - 0.5f)
                        : 2f * phaseT - 1f;
                // ลดเสียงแบบ exponential เหมือน gain.exponentialRampToValueAtTime
                float env = Mathf.Pow(0.001f / 1f, t / duration);
                float attack = Mathf.Clamp01(i / (rate * 0.004f));
                data[i] = s * env * attack;
            }
            clip = AudioClip.Create("Tone_" + key, n, 1, rate, false);
            clip.SetData(data, 0);
            toneCache[key] = clip;
        }
        toneSource.PlayOneShot(clip, toneVolume);
    }
    #endregion

    #region HUD / Render
    private void UpdateHud()
    {
        scoreText.text = score.ToString("N0");
        roundText.text = round.ToString();
        stepsText.text = steps.ToString();
        caloriesText.text = $"{steps * KcalPerStep:0.00} kcal โดยประมาณ";
        maxLives = Mathf.Max(maxLives, lives);
        if (hearts.Count != maxLives) RebuildHearts();
        for (int i = 0; i < hearts.Count; i++)
        {
            hearts[i].color = i < lives ? Pink : new Color(Pink.r, Pink.g, Pink.b, 0.22f);
        }
        int cleared = Mathf.Max(0, round - (phase == Phase.Clear ? 0 : 1));
        int left = RoundsPerHeart - cleared % RoundsPerHeart;
        heartHint.text = $"อีก {left} รอบ ได้หัวใจ +1 และเวลา +{BonusSeconds:0} วิ";
        if (!running) timeText.text = Mathf.CeilToInt(Settings.seconds).ToString();
    }

    private void UpdateHudTimer()
    {
        if (!running) return;
        if (countingDown) { timeText.text = Mathf.CeilToInt(Settings.seconds).ToString(); return; }
        float left = SecondsLeft;
        timeText.text = Mathf.Max(0, Mathf.CeilToInt(left)).ToString();
        if (left <= 0f) Finish();
    }

    private void RenderSeq(string mode, int index = 0, int wrongAt = -1)
    {
        string label = mode == "ready" ? "พร้อมเริ่ม" : mode == "preview" ? "จำลำดับ" : mode == "input" ? "ทำตามลำดับ" : mode == "clear" ? "ผ่านแล้ว!" : "ผิด ดูใหม่อีกครั้ง";
        Color accent = mode == "preview" ? Gold : mode == "input" ? Cyan : mode == "clear" ? Lime : mode == "bad" ? Pink : Muted;
        phaseText.text = label;
        phaseText.color = accent;
        SetBorder(seqPanel, mode == "ready" ? new Color(1, 1, 1, 0.14f) : new Color(accent.r, accent.g, accent.b, 0.45f));

        int total = sequence.Count;
        totalText.text = total.ToString();
        int done = mode == "input" || mode == "bad" ? inputIndex : mode == "clear" ? total : mode == "preview" ? Mathf.Min(index + 1, total) : 0;
        if (doneText.text != done.ToString()) { doneText.text = done.ToString(); doneBumpUntil = Time.time + 0.15f; }

        while (slots.Count < total) slots.Add(CreateSlot());
        for (int i = 0; i < slots.Count; i++)
        {
            Image slot = slots[i];
            slot.gameObject.SetActive(i < total);
            if (i >= total) continue;
            Image arrow = ChildImage(slot, "Arrow");
            Color border = new Color(1, 1, 1, 0.18f);
            slot.rectTransform.localEulerAngles = Vector3.zero;
            arrow.enabled = false;
            slot.color = new Color(1, 1, 1, 0.06f);

            if (mode == "preview")
            {
                if (i < index) { slot.color = new Color(1, 1, 1, 0.16f); border = new Color(1, 1, 1, 0.45f); }
                else if (i == index) border = Cyan;
            }
            else if (mode == "input" || mode == "bad" || mode == "clear")
            {
                if (i < inputIndex || mode == "clear")
                {
                    Color c = DirColor(sequence[i]);
                    slot.color = c; border = c;
                    arrow.enabled = true;
                    arrow.color = DeepBg;
                    arrow.rectTransform.localEulerAngles = new Vector3(0, 0, DirAngle(sequence[i]));
                }
                else if (i == wrongAt) { slot.color = new Color(Pink.r, Pink.g, Pink.b, 0.25f); border = Pink; }
                else if (i == inputIndex && mode == "input") border = Cyan;
            }
            SetBorder(slot, border);
        }
        wrongSlot = wrongAt;
        wrongSlotUntil = wrongAt >= 0 ? Time.time + 0.3f : 0f;
    }

    private void SetSignal(Dir? d)
    {
        if (!d.HasValue)
        {
            signalArrow.enabled = false;
            signalQuestion.enabled = true;
            signalQuestion.text = "?";
            signalQuestion.fontSize = 120;
            signalQuestion.color = new Color(1, 1, 1, 0.18f);
            signalBox.color = new Color(6 / 255f, 17 / 255f, 37 / 255f, 0.65f);
            SetBorder(signalBox, new Color(1, 1, 1, 0.22f));
            signalGlow.color = Color.clear;
            signalLitScale = 1f;
            return;
        }
        Color c = DirColor(d.Value);
        signalQuestion.enabled = false;
        signalArrow.enabled = true;
        signalArrow.color = c;
        signalArrow.rectTransform.localEulerAngles = new Vector3(0, 0, DirAngle(d.Value));
        signalBox.color = new Color(34 / 255f, 72 / 255f, 130 / 255f, 0.75f);
        SetBorder(signalBox, c);
        signalGlow.color = new Color(c.r, c.g, c.b, 0.55f);
        signalLitScale = 1.06f;
    }

    private void PulsePad(Dir d, float duration) => padFlashUntil[d] = Time.time + duration;

    private void Flash(string text, Color color)
    {
        feedbackText.text = text;
        feedbackText.color = color;
        feedbackUntil = Time.time + 0.5f;
    }

    // ปุ่มรูปคนพูด: เปิด = สีฟ้า มีคลื่นเสียง + ป้าย AI / คน / ปิด = สีเทา มีเส้นขีดทับ (เหมือนเวอร์ชันเว็บ)
    private void RenderVoiceButton()
    {
        Color c = VoiceOn ? Cyan : Muted;
        SetBorder(voiceButtonImage, VoiceOn ? Cyan : LineColor);
        voiceButtonImage.color = VoiceOn ? new Color(Cyan.r, Cyan.g, Cyan.b, 0.12f) : new Color(1, 1, 1, 0.03f);
        ChildImage(voiceButtonImage, "Person").color = c;
        Image waves = ChildImage(voiceButtonImage, "Waves");
        waves.color = c;
        waves.enabled = VoiceOn;
        Image slash = ChildImage(voiceButtonImage, "Slash");
        slash.color = c;
        slash.enabled = !VoiceOn;
        voiceBadge.text = voiceMode == VoiceMode.AI ? "AI" : voiceMode == VoiceMode.Human ? "คน" : "";
        voiceBadge.color = c;
    }

    private void AnimateUI()
    {
        float now = Time.time;

        // แผ่นลูกศรด้านล่างสว่างตอนแสดง/เหยียบ
        foreach (Dir d in AllDirs)
        {
            Image pad = padButtons[d];
            bool lit = padFlashUntil.TryGetValue(d, out float until) && now < until;
            bool hover = !lit && hovered.Contains(pad) && phase == Phase.Input;
            Color c = DirColor(d);
            pad.color = lit ? c : hover ? new Color(c.r, c.g, c.b, 0.3f) : new Color(7 / 255f, 18 / 255f, 40 / 255f, 0.9f);
            SetBorder(pad, lit || hover ? c : new Color(226 / 255f, 245 / 255f, 1f, 0.42f));
            ChildImage(pad, "Arrow").color = lit ? DeepBg : hover ? c : Ink;
            pad.transform.localScale = Vector3.one * (lit ? 1.05f : hover ? 1.03f : 1f);
        }

        // ปุ่มเสียงพูด / ปุ่มบ้าน: เอาเมาส์ชี้แล้วมีสีขึ้น
        bool voiceHover = hovered.Contains(voiceButtonImage);
        Color voiceAccent = VoiceOn ? Cyan : Muted;
        voiceButtonImage.color = new Color(voiceAccent.r, voiceAccent.g, voiceAccent.b, voiceHover ? 0.35f : VoiceOn ? 0.12f : 0.03f);
        voiceButtonImage.transform.localScale = Vector3.one * (voiceHover ? 1.06f : 1f);
        bool homeHover = hovered.Contains(homeButtonImage);
        homeButtonImage.color = homeHover ? new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f) : new Color(1, 1, 1, 0.03f);
        SetBorder(homeButtonImage, homeHover ? Cyan : LineColor);
        ChildImage(homeButtonImage, "Icon").color = homeHover ? Cyan : Muted;
        homeButtonImage.transform.localScale = Vector3.one * (homeHover ? 1.06f : 1f);

        // ลูกศรใหญ่ตรงกลาง
        signalBox.transform.localScale = Vector3.Lerp(signalBox.transform.localScale, Vector3.one * signalLitScale, Time.deltaTime * 18f);

        // ข้อความตอบสนอง
        float fb = Mathf.Clamp01((feedbackUntil - now) / 0.16f);
        feedbackText.color = new Color(feedbackText.color.r, feedbackText.color.g, feedbackText.color.b, fb);
        feedbackText.rectTransform.anchoredPosition = new Vector2(feedbackText.rectTransform.anchoredPosition.x, -640f + (1f - fb) * -15f);

        doneText.transform.localScale = Vector3.one * (now < doneBumpUntil ? 1.18f : 1f);
        heartsRow.localScale = Vector3.one * (now < heartsGainUntil ? 1.25f : 1f);
        timeStat.localScale = Vector3.one * (now < timeGainUntil ? 1.3f : 1f);
        timeText.color = now < timeGainUntil ? Lime : Ink;

        // ช่องที่เหยียบผิด สั่น (หมุนส่าย เพราะตำแหน่งถูก GridLayout ควบคุมอยู่)
        if (wrongSlot >= 0 && wrongSlot < slots.Count)
        {
            float k = Mathf.Clamp01((wrongSlotUntil - now) / 0.3f);
            slots[wrongSlot].rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Sin(now * 60f) * 10f * k);
        }

        // เมนูใช้เวลาจริง (unscaled) เพราะตอนหยุดเกม timeScale = 0
        float ut = Time.unscaledTime, udt = Time.unscaledDeltaTime;

        // ปุ่มในหน้าแรก/หน้าหยุดเกม/หน้าสรุปผล: ปุ่มที่เลือกอยู่ = พื้นสีฟ้าเต็ม ตัวอักษรเข้ม ขยาย-หดเป็นจังหวะ / ปุ่มอื่น = เส้นขอบอย่างเดียว
        bool menuOpen = overlay != null && overlay.activeSelf;
        for (int i = 0; i < focusButtons.Count; i++)
        {
            Image b = focusButtons[i];
            if (b == null) continue;
            bool sel = menuOpen && i == focusIndex;
            float glow = 0.85f + 0.15f * Mathf.Sin(ut * 6f);
            b.color = sel ? new Color(Cyan.r * glow, Cyan.g * glow, Cyan.b * glow, 1f) : new Color(1f, 1f, 1f, 0.04f);
            SetBorder(b, sel ? Color.white : Hex(0x7086ad));
            Text label = b.GetComponentInChildren<Text>();
            if (label != null) label.color = sel ? DeepBg : Ink;
            float target = sel ? 1.08f + 0.03f * Mathf.Sin(ut * 6f) : 1f;
            b.transform.localScale = Vector3.Lerp(b.transform.localScale, Vector3.one * target, udt * 14f);
        }

        // ระดับที่เลือก / เสียงที่เลือก: พื้นสีฟ้าชัดขึ้น + ขยายเล็กน้อย
        for (int i = 0; i < difficultyTiles.Count; i++) AnimateChoiceTile(difficultyTiles[i], i == difficulty, ut, udt);
        for (int i = 0; i < voiceTiles.Count; i++) AnimateChoiceTile(voiceTiles[i], i == (int)voiceMode, ut, udt);

        // แถวที่เลือกอยู่ (ซ้าย/ขวาจะเปลี่ยนค่าแถวนี้): กรอบสีฟ้า
        for (int i = 0; i < menuRowFrames.Count; i++)
        {
            Image frame = menuRowFrames[i];
            if (frame == null) continue;
            bool sel = menuOpen && i == menuRow;
            frame.color = sel ? new Color(Cyan.r, Cyan.g, Cyan.b, 0.07f) : Color.clear;
            SetBorder(frame, sel ? new Color(Cyan.r, Cyan.g, Cyan.b, 0.6f + 0.3f * Mathf.Sin(ut * 5f)) : Color.clear);
        }
    }

    private static void AnimateChoiceTile(Image tile, bool sel, float ut, float udt)
    {
        if (tile == null) return;
        tile.color = sel ? new Color(Cyan.r, Cyan.g, Cyan.b, 0.32f + 0.08f * Mathf.Sin(ut * 5f)) : Hex(0x10203d);
        SetBorder(tile, sel ? Cyan : Hex(0x48648f));
        float target = sel ? 1.05f : 1f;
        tile.transform.localScale = Vector3.Lerp(tile.transform.localScale, Vector3.one * target, udt * 14f);
    }

    private void RebuildHearts()
    {
        foreach (Image h in hearts) Destroy(h.gameObject);
        hearts.Clear();
        for (int i = 0; i < maxLives; i++)
        {
            Image h = NewImage("Heart", heartsRow, heartSprite, Pink);
            h.rectTransform.sizeDelta = new Vector2(34, 30);
            hearts.Add(h);
        }
        UpdateHeartsColors();
    }

    private void UpdateHeartsColors()
    {
        for (int i = 0; i < hearts.Count; i++) hearts[i].color = i < lives ? Pink : new Color(Pink.r, Pink.g, Pink.b, 0.22f);
    }
    #endregion

    #region Menu / Results overlays
    private void ShowHome()
    {
        StopFlow();
        paused = false;
        Time.timeScale = 1f;
        downHeldSince = -1f;
        holdFill.fillAmount = 0f;
        running = false;
        countingDown = false;
        showingResults = false;
        phase = Phase.Menu;
        voiceSource.Stop();
        SetSignal(null);
        roundLabel.text = "ARROW MEMORY";
        progressText.text = "เลือกความยาก แล้วเริ่มได้เลย";
        sequence.Clear();
        RenderSeq("ready");
        UpdateHud();

        ClearModal();
        difficultyTiles.Clear();
        ClearMenuRows();
        modal.sizeDelta = new Vector2(820, 770);
        Text title = NewText("Title", modal, "Arrow <color=#3EE8FF>Memory</color>", 76, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
        Place(title.rectTransform, 0, -40, 800, 100);
        Text desc = NewText("Desc", modal, "จำลูกศรที่ปรากฏ แล้วเหยียบตามลำดับให้ถูกต้อง\nยิ่งไปได้ไกล ลำดับจะยิ่งยาวและเร็วขึ้น", 28, Hex(0xcbd9e8), TextAnchor.MiddleCenter);
        Place(desc.rectTransform, 0, -150, 780, 90);
        BuildArrowLine(modal, -250);

        // แถว 1: ระดับความยาก / แถว 2: เสียงพูด / แถว 3: ปุ่ม
        AddMenuRow(MenuRow.Difficulty, -370, 118);
        AddMenuRow(MenuRow.Voice, -498, 86);
        AddMenuRow(MenuRow.Buttons, -604, 90);

        for (int i = 0; i < Difficulties.Length; i++)
        {
            int idx = i;
            Image tile = NewPanel("Difficulty", modal, Hex(0x10203d), Hex(0x48648f), 14);
            Place(tile.rectTransform, (i - 1) * 250, -370, 230, 96);
            Text t = NewText("Label", tile.rectTransform, $"{Difficulties[i].label}\n<size=18><color=#9DB1C8>{Difficulties[i].hint}</color></size>", 32, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(t.rectTransform);
            AddClick(tile.gameObject, () => SelectDifficulty(idx));
            AddHover(tile.gameObject, () => { SelectDifficulty(idx); SetMenuRow(0, false); });
            difficultyTiles.Add(tile);
        }

        BuildVoiceRow(-498, 1);

        // ปุ่มหน้าแรก: เริ่มที่ "เริ่ม" (เหยียบขึ้น/Enter = กดปุ่มที่เลือก) แถวปุ่ม ซ้าย/ขวา เพื่อเลือก "หน้าเลือกเกม"
        focusButtons.Clear(); focusActions.Clear();
        Image back = NewButton(modal, "หน้าเลือกเกม", false, () => ExitGame());
        Place(back.rectTransform, -150, -604, 260, 64);
        AddFocusButton(back, ExitGame);
        Image start = NewButton(modal, "เริ่ม", true, StartGame);
        Place(start.rectTransform, 150, -604, 260, 64);
        AddFocusButton(start, StartGame);
        focusIndex = 1;

        Text hint = NewText("PadHint", modal, "ลง (S) = เลื่อนแถว     ซ้าย/ขวา (A/D) = เปลี่ยนค่าในแถว     ขึ้น (W) หรือ Enter = กดปุ่มที่สว่าง\nระหว่างเล่น: เหยียบลงค้างไว้ = หยุดเกม     คลิกเมาส์ได้", 19, Muted, TextAnchor.MiddleCenter);
        Place(hint.rectTransform, 0, -700, 800, 60);

        OpenOverlay();
    }

    // แถวเลือกเสียงพูด: ปิดเสียง / เสียง AI / เสียงคนจริง (ซ้าย/ขวา หรือคลิก)
    private void BuildVoiceRow(float y, int rowIndex)
    {
        Text label = NewText("VoiceLabel", modal, "เสียงพูด", 26, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
        Place(label.rectTransform, -292, y, 170, 50);
        voiceTiles.Clear();
        for (int i = 0; i < 3; i++)
        {
            VoiceMode mode = (VoiceMode)i;
            Image tile = NewPanel("VoiceOption", modal, Hex(0x10203d), Hex(0x48648f), 14);
            Place(tile.rectTransform, -95 + i * 200, y, 184, 62);
            Text t = NewText("Label", tile.rectTransform, VoiceLabel(mode), 25, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(t.rectTransform);
            AddClick(tile.gameObject, () => { if (voiceMode != mode) CycleVoice((int)mode - (int)voiceMode); });
            AddHover(tile.gameObject, () => SetMenuRow(rowIndex, false));
            voiceTiles.Add(tile);
        }
    }

    // หยุดเกมชั่วคราว (เหยียบลงค้าง / Esc): เปลี่ยนเสียงพูด เล่นต่อ หรือกลับหน้าแรก ด้วยแผ่นเหยียบได้ทั้งหมด
    private void OpenPause()
    {
        if (!running || paused || overlay.activeSelf) return;
        paused = true;
        Time.timeScale = 0f;
        voiceSource.Pause();
        downHeldSince = -1f;
        holdFill.fillAmount = 0f;

        ClearModal();
        difficultyTiles.Clear();
        ClearMenuRows();
        modal.sizeDelta = new Vector2(820, 520);
        Text title = NewText("Title", modal, "หยุดชั่วคราว", 64, Cyan, TextAnchor.MiddleCenter, FontStyle.Bold);
        Place(title.rectTransform, 0, -60, 800, 90);
        Text desc = NewText("Desc", modal, "เวลาหยุดเดินแล้ว เล่นต่อได้ทุกเมื่อ", 26, Hex(0xcbd9e8), TextAnchor.MiddleCenter);
        Place(desc.rectTransform, 0, -130, 780, 40);

        AddMenuRow(MenuRow.Voice, -230, 86);
        AddMenuRow(MenuRow.Buttons, -340, 90);
        BuildVoiceRow(-230, 0);

        // ออกได้ทันทีโดยไม่ต้องรอหัวใจหมด: ซ้าย = กลับหน้าแรก (เลือกระดับ) / ซ้ายสุด = ออกไปหน้าเลือกเกม
        focusButtons.Clear(); focusActions.Clear();
        Image exit = NewButton(modal, "ออกจากเกม", false, ExitGame);
        Place(exit.rectTransform, -260, -340, 240, 64);
        AddFocusButton(exit, ExitGame);
        Image home = NewButton(modal, "กลับหน้าแรก", false, ShowHome);
        Place(home.rectTransform, 0, -340, 240, 64);
        AddFocusButton(home, ShowHome);
        Image resume = NewButton(modal, "เล่นต่อ", true, ResumeGame);
        Place(resume.rectTransform, 260, -340, 240, 64);
        AddFocusButton(resume, ResumeGame);
        focusIndex = 2;

        Text hint = NewText("PadHint", modal, "ลง = เลื่อนแถว     ซ้าย/ขวา = เปลี่ยนค่าในแถว     ขึ้น หรือ Enter = กดปุ่มที่สว่าง     Esc = เล่นต่อ", 19, Muted, TextAnchor.MiddleCenter);
        Place(hint.rectTransform, 0, -450, 800, 40);

        OpenOverlay();
    }

    private void ResumeGame()
    {
        if (!paused) return;
        paused = false;
        overlay.SetActive(false);
        Time.timeScale = 1f;
        voiceSource.UnPause();
        ClearMenuRows();
    }

    private void ShowResults()
    {
        showingResults = true;
        ClearModal();
        difficultyTiles.Clear();
        ClearMenuRows();
        modal.sizeDelta = new Vector2(860, 640);

        int passed = Mathf.Max(0, round - 1);
        string titleText = round >= 8 ? "MEMORY MASTER" : round >= 4 ? "GREAT RHYTHM" : "NICE START";
        float played = Mathf.Max(0f, Time.time - startedAt);
        Text title = NewText("Title", modal, titleText, 70, Cyan, TextAnchor.MiddleCenter, FontStyle.Bold);
        Place(title.rectTransform, 0, -40, 820, 100);

        // สรุปผลและการเคลื่อนไหวทั้งหมดของรอบนี้
        string[,] stats =
        {
            { "ผ่าน", $"{passed} รอบ" },
            { "คะแนน", score.ToString("N0") },
            { "เหยียบ", $"{steps} ครั้ง" },
            { "พลังงาน", $"{steps * KcalPerStep:0.00} kcal" },
            { "เวลา", $"{Mathf.FloorToInt(played / 60f):00}:{Mathf.FloorToInt(played % 60f):00}" },
            { "ระดับ", Settings.label },
        };
        for (int i = 0; i < 6; i++)
        {
            int col = i % 3, row = i / 3;
            Image card = NewPanel("Stat", modal, Hex(0x122343), Hex(0x47648e), 14);
            Place(card.rectTransform, (col - 1) * 260, -175 - row * 120, 240, 104);
            Text v = NewText("Value", card.rectTransform, stats[i, 1], 34, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(v.rectTransform, 0, -14, 230, 50);
            Text k = NewText("Key", card.rectTransform, stats[i, 0], 20, Muted, TextAnchor.MiddleCenter);
            Place(k.rectTransform, 0, -62, 230, 30);
        }

        BuildArrowLine(modal, -415);

        // ปุ่ม 3 ปุ่ม เลื่อนเลือกด้วยซ้าย/ขวา (แผ่นเหยียบ / A D / ลูกศร / ชี้เมาส์) แล้วกดยืนยัน
        focusButtons.Clear(); focusActions.Clear();
        Image choose = NewButton(modal, "เลือกระดับความยาก", false, ShowHome);
        Place(choose.rectTransform, -275, -500, 250, 64);
        AddFocusButton(choose, ShowHome);
        Image again = NewButton(modal, "เล่นอีกรอบ", true, StartGame);
        Place(again.rectTransform, 0, -500, 250, 64);
        AddFocusButton(again, StartGame);
        Image questBoard = NewButton(modal, "หน้าเลือกเกม", false, ExitGame);
        Place(questBoard.rectTransform, 275, -500, 250, 64);
        AddFocusButton(questBoard, ExitGame);
        focusIndex = 1; // เริ่มที่ "เล่นอีกรอบ"

        Text hint = NewText("PadHint", modal, "ซ้าย/ขวา (A/D) = เลือกปุ่ม     ขึ้น (W) หรือ Enter = ยืนยัน     Esc = หน้าเลือกเกม     คลิกเมาส์ได้", 20, Muted, TextAnchor.MiddleCenter);
        Place(hint.rectTransform, 0, -585, 840, 40);

        OpenOverlay();
    }

    private void SelectDifficulty(int index)
    {
        int next = (index + Difficulties.Length) % Difficulties.Length;
        if (next == difficulty) return;
        difficulty = next;
        PlayerPrefs.SetInt("ArrowMemoryDifficulty", difficulty);
        PlayTone(520f + difficulty * 120f, 0.06f, Wave.Triangle);
        UpdateHud();
    }

    private void OpenOverlay()
    {
        overlay.SetActive(true);
        menuShownAt = Time.unscaledTime;
    }

    private void ClearModal()
    {
        for (int i = modal.childCount - 1; i >= 0; i--) Destroy(modal.GetChild(i).gameObject);
    }

    private void BuildArrowLine(RectTransform parent, float y)
    {
        for (int i = 0; i < 4; i++)
        {
            Image box = NewPanel("ArrowTile", parent, Hex(0x132242), Hex(0x5e7ead), 12);
            Place(box.rectTransform, (i - 1.5f) * 66, y, 54, 54);
            Image a = NewImage("Arrow", box.rectTransform, arrowSprite, Ink);
            a.rectTransform.sizeDelta = new Vector2(30, 30);
            a.rectTransform.localEulerAngles = new Vector3(0, 0, DirAngle(AllDirs[i]));
        }
    }
    #endregion

    #region Build UI
    private void BuildUI()
    {
        font = Font.CreateDynamicFontFromOSFont(new[] { "Leelawadee UI", "Leelawadee", "Tahoma", "Segoe UI", "Arial" }, 32);

        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            es.transform.SetParent(transform, false);
        }

        var canvasGo = new GameObject("ArrowMemoryCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRoot = (RectTransform)canvasGo.transform;

        // พื้นหลังไล่สีแบบเวอร์ชันเว็บ
        RawImage bg = new GameObject("Background", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        bg.transform.SetParent(canvasRoot, false);
        bg.texture = BuildBackgroundTexture();
        bg.raycastTarget = false;
        Stretch(bg.rectTransform);

        // ---------- กรอบเกมหลัก ----------
        Image shell = NewPanel("GameShell", canvasRoot, PanelColor, LineColor, 24);
        TopLeft(shell.rectTransform, 30, -30, 1520, 1020);

        Text brand = NewText("Brand", shell.rectTransform, "PULSE <color=#3EE8FF>STEPS</color>", 34, Ink, TextAnchor.UpperLeft, FontStyle.Bold);
        TopLeft(brand.rectTransform, 32, -18, 500, 46);
        roundLabel = NewText("RoundLabel", shell.rectTransform, "ARROW MEMORY", 18, Muted, TextAnchor.UpperLeft);
        TopLeft(roundLabel.rectTransform, 34, -62, 500, 26);

        scoreText = NewStat(shell.rectTransform, "SCORE", 1520 - 540, out _);
        roundText = NewStat(shell.rectTransform, "ROUND", 1520 - 400, out _);
        timeText = NewStat(shell.rectTransform, "SECONDS", 1520 - 260, out timeStat);

        // ปุ่มรูปคนพูด: กดเพื่อเปลี่ยนเสียงพูด ปิดเสียง → เสียง AI → เสียงคนจริง (ป้ายมุมล่างบอกว่าใช้เสียงไหน)
        voiceButtonImage = NewPanel("VoiceToggle", shell.rectTransform, Color.clear, LineColor, 12, 2);
        TopLeft(voiceButtonImage.rectTransform, 1520 - 176, -24, 64, 56);
        foreach (string part in new[] { "Person", "Waves", "Slash" })
        {
            Image icon = NewImage(part, voiceButtonImage.rectTransform, part == "Person" ? personIcon : part == "Waves" ? wavesIcon : slashIcon, Cyan);
            icon.rectTransform.sizeDelta = new Vector2(38, 38);
        }
        voiceBadge = NewText("Badge", voiceButtonImage.rectTransform, "", 14, Cyan, TextAnchor.LowerRight, FontStyle.Bold);
        RectTransform badgeRt = voiceBadge.rectTransform;
        badgeRt.anchorMin = badgeRt.anchorMax = badgeRt.pivot = new Vector2(1f, 0f);
        badgeRt.anchoredPosition = new Vector2(-4f, 1f);
        badgeRt.sizeDelta = new Vector2(40f, 18f);
        AddClick(voiceButtonImage.gameObject, () => CycleVoice(1));
        TrackHover(voiceButtonImage);
        RenderVoiceButton();

        // ปุ่มบ้าน: ระหว่างเล่น = หยุดเกม (เลือกเล่นต่อ/กลับหน้าแรกได้) / นอกเกม = หน้าแรก
        // แผ่นเหยียบ: เหยียบลงค้างไว้ วงแหวนบนปุ่มนี้จะเต็มแล้วหยุดเกม
        homeButtonImage = NewPanel("HomeButton", shell.rectTransform, new Color(1, 1, 1, 0.03f), LineColor, 12, 2);
        TopLeft(homeButtonImage.rectTransform, 1520 - 98, -24, 64, 56);
        holdFill = NewImage("HoldFill", homeButtonImage.rectTransform, roundSprite, new Color(Cyan.r, Cyan.g, Cyan.b, 0.45f));
        holdFill.type = Image.Type.Filled;
        holdFill.fillMethod = Image.FillMethod.Radial360;
        holdFill.fillOrigin = (int)Image.Origin360.Top;
        holdFill.fillClockwise = true;
        holdFill.fillAmount = 0f;
        Stretch(holdFill.rectTransform);
        Image homeIconImage = NewImage("Icon", homeButtonImage.rectTransform, homeIcon, Muted);
        homeIconImage.rectTransform.sizeDelta = new Vector2(34, 34);
        AddClick(homeButtonImage.gameObject, () => { if (running) OpenPause(); else ShowHome(); });
        TrackHover(homeButtonImage);

        Image divider = NewImage("Divider", shell.rectTransform, null, LineColor);
        TopLeft(divider.rectTransform, 0, -100, 1520, 2);

        // ---------- ลูกศรใหญ่ตรงกลาง ----------
        signalGlow = NewImage("SignalGlow", shell.rectTransform, glowSprite, Color.clear);
        Place(signalGlow.rectTransform, -150, -370, 640, 640);
        signalBox = NewPanel("Signal", shell.rectTransform, Color.clear, Color.clear, 40, 4);
        Place(signalBox.rectTransform, -150, -370, 400, 400);
        signalArrow = NewImage("Arrow", signalBox.rectTransform, arrowSprite, Cyan);
        signalArrow.rectTransform.sizeDelta = new Vector2(250, 250);
        signalQuestion = NewText("Question", signalBox.rectTransform, "?", 120, new Color(1, 1, 1, 0.18f), TextAnchor.MiddleCenter, FontStyle.Bold);
        Stretch(signalQuestion.rectTransform);

        // ---------- แผงลำดับ ----------
        seqPanel = NewPanel("SeqPanel", shell.rectTransform, new Color(6 / 255f, 17 / 255f, 37 / 255f, 0.45f), new Color(1, 1, 1, 0.14f), 22);
        Place(seqPanel.rectTransform, 300, -370, 330, 400);
        phaseText = NewText("Phase", seqPanel.rectTransform, "พร้อมเริ่ม", 26, Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
        Place(phaseText.rectTransform, 0, -40, 310, 40);
        doneText = NewText("Done", seqPanel.rectTransform, "0", 60, Ink, TextAnchor.MiddleRight, FontStyle.Bold);
        Place(doneText.rectTransform, -50, -100, 120, 70);
        Text slash = NewText("Slash", seqPanel.rectTransform, "/", 36, Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
        Place(slash.rectTransform, 22, -104, 30, 60);
        totalText = NewText("Total", seqPanel.rectTransform, "0", 36, Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
        Place(totalText.rectTransform, 80, -104, 90, 60);

        slotGrid = new GameObject("Slots", typeof(RectTransform), typeof(GridLayoutGroup)).GetComponent<RectTransform>();
        slotGrid.SetParent(seqPanel.rectTransform, false);
        Place(slotGrid, 0, -230, 300, 180);
        GridLayoutGroup grid = slotGrid.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(38, 38);
        grid.spacing = new Vector2(8, 8);
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 6;

        progressText = NewText("Progress", seqPanel.rectTransform, "", 20, Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
        Place(progressText.rectTransform, 0, -350, 310, 70);

        feedbackText = NewText("Feedback", shell.rectTransform, "", 40, Cyan, TextAnchor.MiddleCenter, FontStyle.Bold);
        Place(feedbackText.rectTransform, -150, -640, 1000, 60);
        feedbackText.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0, -3);

        // ---------- แผงลูกศร 4 ทิศ ----------
        BuildPad(Dir.Up, 0, -745);
        BuildPad(Dir.Left, -170, -875);
        BuildPad(Dir.Down, 0, -875);
        BuildPad(Dir.Right, 170, -875);

        // ---------- แถบข้าง ----------
        Image moveCard = NewPanel("Movement", canvasRoot, PanelColor, LineColor, 24);
        TopLeft(moveCard.rectTransform, 1580, -30, 310, 190);
        CardTitle(moveCard.rectTransform, "MOVEMENT");
        stepsText = NewText("Steps", moveCard.rectTransform, "0", 52, Cyan, TextAnchor.UpperLeft, FontStyle.Bold);
        TopLeft(stepsText.rectTransform, 22, -58, 160, 64);
        Text stepsLabel = NewText("StepsLabel", moveCard.rectTransform, "STEPS", 18, Muted, TextAnchor.LowerLeft, FontStyle.Bold);
        TopLeft(stepsLabel.rectTransform, 130, -76, 120, 40);
        caloriesText = NewText("Calories", moveCard.rectTransform, "0.00 kcal โดยประมาณ", 20, Muted, TextAnchor.UpperLeft);
        TopLeft(caloriesText.rectTransform, 22, -132, 280, 40);

        Image livesCard = NewPanel("Lives", canvasRoot, PanelColor, LineColor, 24);
        TopLeft(livesCard.rectTransform, 1580, -240, 310, 200);
        CardTitle(livesCard.rectTransform, "LIVES");
        heartsRow = new GameObject("Hearts", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
        heartsRow.SetParent(livesCard.rectTransform, false);
        TopLeft(heartsRow, 22, -62, 270, 36);
        heartsRow.pivot = new Vector2(0f, 0.5f);
        HorizontalLayoutGroup hl = heartsRow.GetComponent<HorizontalLayoutGroup>();
        hl.spacing = 10; hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childControlWidth = hl.childControlHeight = false; hl.childForceExpandWidth = hl.childForceExpandHeight = false;
        heartHint = NewText("HeartHint", livesCard.rectTransform, "", 20, Muted, TextAnchor.UpperLeft);
        TopLeft(heartHint.rectTransform, 22, -112, 276, 76);

        Image howCard = NewPanel("HowToPlay", canvasRoot, PanelColor, LineColor, 24);
        TopLeft(howCard.rectTransform, 1580, -460, 310, 590);
        CardTitle(howCard.rectTransform, "HOW TO PLAY");
        Text tips = NewText("Tips", howCard.rectTransform,
            "1. ยืนให้มั่นคง หน้าจออยู่ระดับสายตา\n\n2. จำลูกศรที่สว่างทีละอัน\n\n3. เหยียบตามลำดับจากความจำ\n\n4. เหยียบผิด เสียหัวใจ 1 ดวง\n   แล้วดูลำดับใหม่อีกครั้ง\n\n5. ผ่านทุก 3 รอบ ได้หัวใจ +1\n   และเวลา +30 วินาที\n\n6. กด V เปิด/ปิดเสียงพูด\n\n7. ตั้งค่าปุ่มได้ที่หน้า Option\n   (ปุ่มเดิน 4 ทิศ)",
            20, Muted, TextAnchor.UpperLeft);
        TopLeft(tips.rectTransform, 22, -62, 276, 520);

        // ---------- หน้าเมนู/สรุปผล (ทับทั้งจอ) ----------
        Image dim = NewImage("Overlay", canvasRoot, null, new Color(4 / 255f, 10 / 255f, 23 / 255f, 0.82f));
        Stretch(dim.rectTransform);
        dim.raycastTarget = true;
        overlay = dim.gameObject;
        Image modalPanel = NewPanel("Modal", dim.rectTransform, Hex(0x0c1931), Hex(0x5777aa), 26);
        modal = modalPanel.rectTransform;
        modal.anchorMin = modal.anchorMax = new Vector2(0.5f, 0.5f);
        modal.pivot = new Vector2(0.5f, 0.5f);
        modal.anchoredPosition = new Vector2(-150, 0);
    }

    private void BuildPad(Dir d, float x, float y)
    {
        Image pad = NewPanel("Pad_" + d, (RectTransform)signalBox.transform.parent, Color.clear, Color.clear, 17, 2);
        Place(pad.rectTransform, x - 150, y, 150, 110);
        Image arrow = NewImage("Arrow", pad.rectTransform, arrowSprite, Ink);
        arrow.rectTransform.sizeDelta = new Vector2(64, 64);
        arrow.rectTransform.localEulerAngles = new Vector3(0, 0, DirAngle(d));
        AddClick(pad.gameObject, () => AcceptDirection(d));
        TrackHover(pad);
        padButtons[d] = pad;
    }

    private Image CreateSlot()
    {
        Image slot = NewPanel("Slot", slotGrid, new Color(1, 1, 1, 0.06f), new Color(1, 1, 1, 0.18f), 12, 2);
        Image arrow = NewImage("Arrow", slot.rectTransform, arrowSprite, DeepBg);
        arrow.rectTransform.sizeDelta = new Vector2(22, 22);
        arrow.enabled = false;
        return slot;
    }

    private Text NewStat(RectTransform parent, string label, float rightX, out RectTransform root)
    {
        root = new GameObject("Stat_" + label, typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(parent, false);
        TopLeft(root, rightX - 130, -20, 130, 70);
        root.pivot = new Vector2(1f, 1f);
        root.anchoredPosition = new Vector2(rightX, -20);
        Text value = NewText("Value", root, "0", 40, Ink, TextAnchor.UpperRight, FontStyle.Bold);
        TopLeft(value.rectTransform, 0, 0, 130, 46);
        Text small = NewText("Label", root, label, 15, Muted, TextAnchor.UpperRight);
        TopLeft(small.rectTransform, 0, -46, 130, 22);
        return value;
    }

    private void CardTitle(RectTransform card, string title)
    {
        Text t = NewText("Title", card, title, 18, Ink, TextAnchor.UpperLeft, FontStyle.Bold);
        TopLeft(t.rectTransform, 22, -20, 270, 30);
    }

    private Image NewButton(RectTransform parent, string label, bool primary, System.Action onClick)
    {
        Image b = NewPanel("Button", parent, primary ? Cyan : new Color(0, 0, 0, 0), primary ? Cyan : Hex(0x7086ad), 32, 2);
        Text t = NewText("Label", b.rectTransform, label, 26, primary ? DeepBg : Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
        Stretch(t.rectTransform);
        AddClick(b.gameObject, () => onClick());
        return b;
    }

    private static void AddClick(GameObject go, System.Action action)
    {
        Graphic g = go.GetComponent<Graphic>();
        if (g != null) g.raycastTarget = true;
        Button btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        Navigation nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;
        btn.onClick.AddListener(() => action());
    }

    private Image NewPanel(string name, RectTransform parent, Color fill, Color border, float radius, float borderWidth = 1.5f)
    {
        Image img = NewImage(name, parent, roundSprite, fill);
        img.type = Image.Type.Sliced;
        // สไปรต์ขอบมนรัศมี 32px: ปรับตัวคูณให้ได้รัศมีตามต้องการ
        float multiplier = 32f / Mathf.Max(1f, radius);
        img.pixelsPerUnitMultiplier = multiplier;

        // เส้นขอบบางๆ เป็นภาพวงแหวนขอบมนแยกต่างหาก (ลูกตัวแรก วาดทับพื้นแต่อยู่ใต้เนื้อหา)
        Image ring = NewImage("Border", img.rectTransform, RingSprite(Mathf.RoundToInt(borderWidth * multiplier)), border);
        ring.type = Image.Type.Sliced;
        ring.pixelsPerUnitMultiplier = multiplier;
        Stretch(ring.rectTransform);
        return img;
    }

    private static void SetBorder(Image panel, Color color)
    {
        Transform ring = panel.transform.Find("Border");
        if (ring != null) ring.GetComponent<Image>().color = color;
    }

    private static Image ChildImage(Component parent, string name)
    {
        Transform t = parent.transform.Find(name);
        return t != null ? t.GetComponent<Image>() : null;
    }

    private Image NewImage(string name, RectTransform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private Text NewText(string name, RectTransform parent, string text, int size, Color color, TextAnchor anchor, FontStyle style = FontStyle.Normal)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text t = go.GetComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = anchor;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    // วางตามมุมซ้ายบนของพ่อ (x ไปขวา, y ลงล่างเป็นค่าลบ)
    private static void TopLeft(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    // วางจุดกึ่งกลางที่ (x จากกึ่งกลางพ่อ, y จากขอบบนพ่อ)
    private static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
    #endregion

    #region Procedural sprites
    private void BuildSprites()
    {
        roundSprite = MakeSprite(BuildRounded(96, 32), 32);
        arrowSprite = MakeSprite(BuildArrow(128), 0);
        heartSprite = MakeSprite(BuildHeart(96), 0);
        glowSprite = MakeSprite(BuildGlow(128), 0);

        // ไอคอนตามเวอร์ชันเว็บ (พิกัดในกรอบ 24x24, y นับจากบนลงล่าง)
        personIcon = MakeSprite(BuildIcon(96, (x, y) =>
            Ring(x, y, 9f, 7f, 4f, 1f) ||                                         // หัว
            ShoulderStroke(x, y)), 0);                                             // ไหล่
        wavesIcon = MakeSprite(BuildIcon(96, (x, y) =>
            (x > 16f && Ring(x, y, 14f, 7f, 3.2f, 1f)) ||                         // คลื่นเสียงเล็ก
            (x > 17.5f && Ring(x, y, 13.5f, 7f, 6.8f, 1f))), 0);                  // คลื่นเสียงใหญ่
        slashIcon = MakeSprite(BuildIcon(96, (x, y) => SegmentDistance(x, y, 3f, 3f, 21f, 21f) <= 1.1f), 0);
        homeIcon = MakeSprite(BuildIcon(96, (x, y) =>
        {
            bool roof = y >= 3f && y <= 11.5f && Mathf.Abs(x - 12f) <= (y - 3f) * 1.1f + 0.5f;
            bool body = x >= 5.5f && x <= 18.5f && y >= 11f && y <= 21f;
            bool door = x >= 10f && x <= 14f && y >= 14.5f && y <= 21f;
            return (roof || body) && !door;
        }), 0);
    }

    private Sprite RingSprite(int stroke)
    {
        stroke = Mathf.Clamp(stroke, 1, 16);
        if (!ringSprites.TryGetValue(stroke, out Sprite s))
        {
            s = MakeSprite(BuildRoundedRing(96, 32, stroke), 32);
            ringSprites[stroke] = s;
        }
        return s;
    }

    // ขอบมนแบบเส้น (ด้านในโปร่งใส) ใช้เป็นเส้นขอบของกล่อง
    private static Texture2D BuildRoundedRing(int size, int radius, int stroke)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius), cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                float outer = Mathf.Clamp01(radius - d + 0.5f);
                float inner = Mathf.Clamp01(radius - stroke - d + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(outer - inner) * 255));
            }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return tex;
    }

    // วาดไอคอนจากฟังก์ชันรูปทรงในพิกัด 24x24 (anti-alias ด้วยการสุ่มหลายจุดต่อพิกเซล)
    private static Texture2D BuildIcon(int size, System.Func<float, float, bool> inside)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[size * size];
        const int ss = 4;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int hit = 0;
                for (int sy = 0; sy < ss; sy++)
                    for (int sx = 0; sx < ss; sx++)
                    {
                        float u = (x + (sx + 0.5f) / ss) / size * 24f;
                        float v = (1f - (y + (sy + 0.5f) / ss) / size) * 24f; // Texture นับจากล่าง แปลงเป็นนับจากบน
                        if (inside(u, v)) hit++;
                    }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(hit * 255 / (ss * ss)));
            }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return tex;
    }

    private static bool Ring(float x, float y, float cx, float cy, float r, float halfWidth)
    {
        float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
        return Mathf.Abs(d - r) <= halfWidth;
    }

    // เส้นไหล่: ด้านข้างตั้งขึ้นจาก y=21 แล้วโค้งมนด้านบนแบบเส้น path ของ SVG เดิม
    private static bool ShoulderStroke(float x, float y)
    {
        const float w = 1f;
        if (y > 21f + w || y < 14f - w) return false;
        // เส้นแนวนอนด้านบน (ระหว่างมุมโค้ง)
        if (Mathf.Abs(y - 14f) <= w && x >= 8f && x <= 10f) return true;
        // มุมโค้งซ้าย/ขวา รัศมี 6
        if (x < 8f && y < 20f && Ring(x, y, 8f, 20f, 6f, w)) return true;
        if (x > 10f && y < 20f && Ring(x, y, 10f, 20f, 6f, w)) return true;
        // ขาสองข้างลงไปถึงขอบล่าง
        if (y >= 20f && (Mathf.Abs(x - 2f) <= w || Mathf.Abs(x - 16f) <= w)) return true;
        return false;
    }

    private static float SegmentDistance(float x, float y, float ax, float ay, float bx, float by)
    {
        Vector2 p = new Vector2(x, y), a = new Vector2(ax, ay), b = new Vector2(bx, by);
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    private static Sprite MakeSprite(Texture2D tex, int border)
    {
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
    }

    // สี่เหลี่ยมขอบมน (ขอบเรียบด้วย anti-alias)
    private static Texture2D BuildRounded(int size, int radius)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius), cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                byte a = (byte)(Mathf.Clamp01(radius - d + 0.5f) * 255);
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return tex;
    }

    // ลูกศรชี้ขึ้น (หมุนเพื่อทิศอื่น)
    private static Texture2D BuildArrow(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[size * size];
        const int ss = 4;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int hit = 0;
                for (int sy = 0; sy < ss; sy++)
                    for (int sx = 0; sx < ss; sx++)
                    {
                        float u = (x + (sx + 0.5f) / ss) / size, v = (y + (sy + 0.5f) / ss) / size; // v = 0 ล่าง
                        bool head = v >= 0.48f && v <= 0.94f && Mathf.Abs(u - 0.5f) <= (0.94f - v) * 0.92f;
                        bool shaft = v >= 0.08f && v < 0.52f && Mathf.Abs(u - 0.5f) <= 0.12f;
                        if (head || shaft) hit++;
                    }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(hit * 255 / (ss * ss)));
            }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return tex;
    }

    // หัวใจจากสมการ (x^2 + y^2 - 1)^3 - x^2 y^3 <= 0
    private static Texture2D BuildHeart(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[size * size];
        const int ss = 4;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int hit = 0;
                for (int sy = 0; sy < ss; sy++)
                    for (int sx = 0; sx < ss; sx++)
                    {
                        float u = ((x + (sx + 0.5f) / ss) / size - 0.5f) * 2.6f;
                        float v = ((y + (sy + 0.5f) / ss) / size - 0.45f) * 2.6f;
                        float a = u * u + v * v - 1f;
                        if (a * a * a - u * u * v * v * v <= 0f) hit++;
                    }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(hit * 255 / (ss * ss)));
            }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return tex;
    }

    // แสงฟุ้งรอบลูกศรตอนสว่าง
    private static Texture2D BuildGlow(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                float a = Mathf.Clamp01(1f - d);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
            }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return tex;
    }

    // พื้นหลังมืด + แสงฟ้าซ้ายบน + แสงม่วงขวาล่าง (ตาม radial-gradient ของเวอร์ชันเว็บ)
    private static Texture2D BuildBackgroundTexture()
    {
        const int w = 192, h = 108;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color[w * h];
        Color baseC = DeepBg, blue = Hex(0x283b8c), purple = Hex(0x62226f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)w, v = 1f - y / (float)h; // v = 0 บน
                float d1 = Vector2.Distance(new Vector2(u * 1.78f, v), new Vector2(0.15f * 1.78f, 0.05f));
                float d2 = Vector2.Distance(new Vector2(u * 1.78f, v), new Vector2(0.9f * 1.78f, 1f));
                Color c = baseC;
                c = Color.Lerp(c, blue, Mathf.Clamp01(1f - d1 / 0.55f));
                c = Color.Lerp(c, purple, Mathf.Clamp01(1f - d2 / 0.68f));
                px[y * w + x] = c;
            }
        tex.SetPixels(px);
        tex.Apply(false, true);
        return tex;
    }

    private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
    #endregion
}
