using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookieRun
{
    /// <summary>
    /// Interactive Pre-Game Tutorial
    /// - Zero coins on initial ground during practice
    /// - Automatically freezes/pauses game in front of obstacles
    /// - Displays clean, high-visibility English instructions
    /// - Resumes ONLY when designated Jump/Slide button/pad is pressed
    /// - Smoothly transitions into main endless runner upon completion
    /// </summary>
    public class CookieTutorialManager : MonoBehaviour
    {
        public static CookieTutorialManager Instance { get; private set; }

        public enum TutorialStep
        {
            ApproachingJump,
            PausedForJump,
            ApproachingSlide,
            PausedForSlide,
            ApproachingFinish,
            Completed
        }

        [Header("Tutorial Settings")]
        [Tooltip("Enable or disable pre-game tutorial")]
        [SerializeField] private bool enableTutorial = true;

        [Tooltip("X position of rock obstacle for jump tutorial")]
        [SerializeField] private float jumpObstacleX = 14f;

        [Tooltip("Distance in front of rock to freeze game (used only when Auto Jump Distance is off)")]
        [SerializeField] private float jumpDistance = 3.2f;

        [Tooltip("คำนวณจุดหยุดหน้าหินอัตโนมัติจากความเร็ววิ่งและแรงกระโดด ให้กระโดดจากจุดนั้นแล้วจุดสูงสุดอยู่เหนือหินพอดี ไม่ตกลงไปโดนหิน")]
        [SerializeField] private bool autoJumpDistance = true;

        [Tooltip("ข้ามช่วงสอนเล่นเมื่อกดเล่นใหม่ (Restart) หลัง Game Over")]
        [SerializeField] private bool skipOnRestart = true;

        /// <summary>ตั้งเป็น true ก่อนโหลด Scene ใหม่จากการกด Restart เพื่อข้ามช่วงสอนเล่นรอบถัดไป</summary>
        public static bool SkipNextTutorial { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            SkipNextTutorial = false;
        }

        [Tooltip("X position of overhead obstacle for slide tutorial")]
        [SerializeField] private float slideObstacleX = 28f;

        [Tooltip("Distance in front of overhead obstacle to freeze game (3.5m gives safe slide entry)")]
        [SerializeField] private float slideDistance = 3.5f;

        [Tooltip("X position where tutorial finishes and endless runner begins")]
        [SerializeField] private float tutorialFinishX = 35f;

        [Header("Obstacle Prefabs (Auto-loaded if null)")]
        [SerializeField] private GameObject rockPrefab;
        [SerializeField] private GameObject stalactitePrefab;

        private CookiePlayerController player;
        private CookieHealth playerHealth;
        private TutorialStep currentStep = TutorialStep.ApproachingJump;
        private float pauseRealtime = 0f;

        // UI References
        private Canvas targetCanvas;
        private GameObject promptPanel;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI subtitleText;
        private TextMeshProUGUI keyPromptText;
        private TextMeshProUGUI hintText;

        private GameObject completeBannerObj;
        private TextMeshProUGUI completeBannerText;

        public bool IsTutorialActive => enableTutorial && currentStep != TutorialStep.Completed;
        public TutorialStep CurrentStep => currentStep;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (FindObjectOfType<CookieTutorialManager>() == null && FindObjectOfType<CookiePlayerController>() != null)
            {
                GameObject obj = new GameObject("TutorialManager");
                obj.AddComponent<CookieTutorialManager>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            bool skipThisRun = skipOnRestart && SkipNextTutorial;
            SkipNextTutorial = false;

            if (!enableTutorial || skipThisRun)
            {
                currentStep = TutorialStep.Completed;
                if (CookieGameManager.Instance != null)
                {
                    CookieGameManager.Instance.IsTutorialActive = false;
                }
                return;
            }

            currentStep = TutorialStep.ApproachingJump;

            player = FindObjectOfType<CookiePlayerController>();
            if (player != null)
            {
                playerHealth = player.GetComponent<CookieHealth>();
                // Grant immunity during tutorial practice so player cannot die
                if (playerHealth != null)
                {
                    playerHealth.IsImmune = true;
                }
            }

            if (CookieGameManager.Instance != null)
            {
                CookieGameManager.Instance.IsTutorialActive = true;
            }

            LoadObstaclePrefabs();
            SpawnTutorialObstacles();
            CreateTutorialUI();
        }

        private void LoadObstaclePrefabs()
        {
#if UNITY_EDITOR
            if (rockPrefab == null)
            {
                rockPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CookieRunGame/Prefabs/Obstacle_Rock.prefab");
            }
            if (stalactitePrefab == null)
            {
                stalactitePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CookieRunGame/Prefabs/Obstacle_Stalactite.prefab");
            }
#endif
        }

        private void SpawnTutorialObstacles()
        {
            // 1. Rock for Jump Tutorial (x = 14, y = 0.20)
            if (GameObject.Find("Tutorial_Obstacle_Rock") == null && rockPrefab != null)
            {
                GameObject rock = Instantiate(rockPrefab, new Vector3(jumpObstacleX, 0.20f, 0f), Quaternion.identity);
                rock.name = "Tutorial_Obstacle_Rock";
            }

            // 2. Stalactite for Slide Tutorial (x = 28, y = 2.05)
            if (GameObject.Find("Tutorial_Obstacle_Stalactite") == null && stalactitePrefab != null)
            {
                GameObject stalactite = Instantiate(stalactitePrefab, new Vector3(slideObstacleX, 2.05f, 0f), Quaternion.identity);
                stalactite.name = "Tutorial_Obstacle_Stalactite";
            }
        }

        private void Update()
        {
            if (!enableTutorial || currentStep == TutorialStep.Completed) return;
            if (player == null)
            {
                player = FindObjectOfType<CookiePlayerController>();
                if (player == null) return;
            }

            float playerX = player.transform.position.x;

            // Allow skipping tutorial with ESC or ENTER
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return))
            {
                SkipTutorial();
                return;
            }

            switch (currentStep)
            {
                case TutorialStep.ApproachingJump:
                    if (playerX >= (jumpObstacleX - GetJumpPauseDistance()))
                    {
                        PauseForJump();
                    }
                    break;

                case TutorialStep.PausedForJump:
                    // Debounce so player has at least 0.2s to see prompt before accepting input
                    if (Time.realtimeSinceStartup - pauseRealtime >= 0.20f)
                    {
                        if (IsJumpInputDetected())
                        {
                            ResumeFromJump();
                        }
                    }
                    break;

                case TutorialStep.ApproachingSlide:
                    if (playerX >= (slideObstacleX - slideDistance))
                    {
                        PauseForSlide();
                    }
                    break;

                case TutorialStep.PausedForSlide:
                    if (Time.realtimeSinceStartup - pauseRealtime >= 0.20f)
                    {
                        if (IsSlideInputDetected())
                        {
                            ResumeFromSlide();
                        }
                    }
                    break;

                case TutorialStep.ApproachingFinish:
                    if (playerX >= tutorialFinishX)
                    {
                        CompleteTutorial();
                    }
                    break;
            }
        }

        /// <summary>
        /// ระยะหยุดหน้าหิน: ระยะที่วิ่งได้ระหว่างกระโดดขึ้นถึงจุดสูงสุด
        /// กระโดดจากจุดนี้แล้วตัวละครจะอยู่สูงสุดตรงเหนือหินพอดี ข้ามได้ปลอดภัยทั้งขาขึ้นและขาลง
        /// </summary>
        private float GetJumpPauseDistance()
        {
            if (!autoJumpDistance || player == null) return jumpDistance;
            return Mathf.Max(1.2f, player.CurrentSpeed * player.JumpApexTime);
        }

        // ใช้ปุ่มตามที่ผู้เล่นตั้งไว้ในหน้า Option
        private bool IsJumpInputDetected() => GameInputBindings.GetDown(GameAction.RunJump);

        private bool IsSlideInputDetected() => GameInputBindings.GetDown(GameAction.RunSlide);

        private static string ControlsText(GameAction action) =>
            $"{GameInputBindings.DisplayName(GameInputBindings.GetPad(action))} / {GameInputBindings.DisplayName(GameInputBindings.GetKeyboard(action))} / {GameInputBindings.DisplayName(GameInputBindings.GetFallback(action))}";

        private void PauseForJump()
        {
            currentStep = TutorialStep.PausedForJump;
            pauseRealtime = Time.realtimeSinceStartup;
            Time.timeScale = 0f;

            ShowPrompt(
                "--- PRE-GAME TUTORIAL (1/2) ---",
                "[JUMP OVER OBSTACLE]",
                "Rock obstacle ahead!",
                $"> Press [{GameInputBindings.DisplayName(GameInputBindings.GetPad(GameAction.RunJump))}] to Jump\n<size=72%><color=#F3F4F6>(Controls: {ControlsText(GameAction.RunJump)})</color></size>",
                new Color(0f, 0.9f, 1f, 1f)
            );
        }

        private void ResumeFromJump()
        {
            HidePrompt();
            Time.timeScale = 1f;

            if (player != null)
            {
                player.OnJumpPressed();
            }
            currentStep = TutorialStep.ApproachingSlide;
        }

        private void PauseForSlide()
        {
            currentStep = TutorialStep.PausedForSlide;
            pauseRealtime = Time.realtimeSinceStartup;
            Time.timeScale = 0f;

            ShowPrompt(
                "--- PRE-GAME TUTORIAL (2/2) ---",
                "[SLIDE UNDER OBSTACLE]",
                "Overhead obstacle ahead!",
                $"> Hold [{GameInputBindings.DisplayName(GameInputBindings.GetPad(GameAction.RunSlide))}] to Slide\n<size=72%><color=#F3F4F6>(Controls: {ControlsText(GameAction.RunSlide)})</color></size>",
                new Color(1f, 0.6f, 0.1f, 1f)
            );
        }

        private void ResumeFromSlide()
        {
            HidePrompt();
            Time.timeScale = 1f;

            if (player != null)
            {
                player.OnSlideStart();
            }
            currentStep = TutorialStep.ApproachingFinish;
            StartCoroutine(WaitForSlideAndFinish());
        }

        private IEnumerator WaitForSlideAndFinish()
        {
            while (player != null && player.transform.position.x < tutorialFinishX)
            {
                yield return null;
            }
            CompleteTutorial();
        }

        private void CompleteTutorial()
        {
            if (currentStep == TutorialStep.Completed) return;
            currentStep = TutorialStep.Completed;

            Time.timeScale = 1f;
            HidePrompt();

            if (playerHealth != null)
            {
                playerHealth.IsImmune = false;
            }

            if (CookieGameManager.Instance != null && player != null)
            {
                CookieGameManager.Instance.ResetDistanceTracking(player.transform.position.x);
            }

            CookieAudio.Play(CookieSfx.ReadyGo);
            StartCoroutine(ShowCompleteBannerRoutine());
        }

        private IEnumerator ShowCompleteBannerRoutine()
        {
            if (completeBannerObj != null)
            {
                completeBannerObj.SetActive(true);
                if (completeBannerText != null)
                {
                    completeBannerText.text = "<color=#00FF66><b>TUTORIAL COMPLETED!</b></color>\n<size=85%><color=#FFD700><b>READY... GO!</b></color></size>";
                }

                yield return new WaitForSeconds(2.0f);

                completeBannerObj.SetActive(false);
            }
        }

        private void SkipTutorial()
        {
            Time.timeScale = 1f;
            HidePrompt();
            CompleteTutorial();
        }

        #region Dynamic UI Generation
        // ตัวอักษรใหญ่ขึ้น สว่างขึ้น และมีขอบเข้ม อ่านชัดจากระยะยืนบนแผ่นเหยียบ
        // (ใช้ทั้งกับ UI ที่สร้างใหม่และ UI ที่บันทึกไว้ในฉากแล้ว)
        private void ApplyReadableStyle()
        {
            if (promptPanel == null) return;

            RectTransform panelRect = (RectTransform)promptPanel.transform;
            panelRect.sizeDelta = new Vector2(900f, 360f);
            Image panelBg = promptPanel.GetComponent<Image>();
            if (panelBg != null) panelBg.color = new Color(0.07f, 0.06f, 0.05f, 0.96f);

            Transform box = promptPanel.transform.Find("KeyPromptBox");
            if (box != null)
            {
                RectTransform boxRect = (RectTransform)box;
                boxRect.anchorMin = new Vector2(0.04f, 0.2f);
                boxRect.anchorMax = new Vector2(0.96f, 0.68f);
                Image boxBg = box.GetComponent<Image>();
                if (boxBg != null) boxBg.color = new Color(0.13f, 0.14f, 0.19f, 1f);
            }

            Style(subtitleText, 24f, new Color(1f, 0.82f, 0.25f, 1f), FontStyles.Bold, -14f, 36f);
            Style(titleText, 40f, Color.white, FontStyles.Bold, -52f, 56f);
            Style(keyPromptText, 34f, new Color(1f, 0.93f, 0.35f, 1f), FontStyles.Bold, 0f, 0f);
            Style(hintText, 21f, new Color(0.92f, 0.94f, 0.98f, 1f), FontStyles.Bold, 12f, 34f);
        }

        private static void Style(TextMeshProUGUI text, float size, Color color, FontStyles style, float y, float height)
        {
            if (text == null) return;
            text.fontSize = size;
            text.enableAutoSizing = false;
            text.color = color;
            text.fontStyle = style;
            // ขอบดำบางๆ รอบตัวอักษร ให้ตัดกับพื้นหลังชัดขึ้น
            text.outlineWidth = 0.18f;
            text.outlineColor = new Color32(0, 0, 0, 255);
            if (height > 0f)
            {
                RectTransform r = text.rectTransform;
                r.anchoredPosition = new Vector2(r.anchoredPosition.x, y);
                r.sizeDelta = new Vector2(r.sizeDelta.x, height);
            }
        }

        private void CreateTutorialUI()
        {
            targetCanvas = FindObjectOfType<Canvas>();
            if (targetCanvas == null)
            {
                GameObject canvasObj = new GameObject("Tutorial_Canvas");
                targetCanvas = canvasObj.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Check if UI already exists
            Transform existingPrompt = targetCanvas.transform.Find("Tutorial_PromptPanel");
            if (existingPrompt != null)
            {
                promptPanel = existingPrompt.gameObject;
                subtitleText = promptPanel.transform.Find("BadgeText")?.GetComponent<TextMeshProUGUI>();
                titleText = promptPanel.transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
                keyPromptText = promptPanel.transform.Find("KeyPromptBox/PromptText")?.GetComponent<TextMeshProUGUI>();
                hintText = promptPanel.transform.Find("HintText")?.GetComponent<TextMeshProUGUI>();
                promptPanel.SetActive(false);

                Transform existingBanner = targetCanvas.transform.Find("Tutorial_CompleteBanner");
                if (existingBanner != null)
                {
                    completeBannerObj = existingBanner.gameObject;
                    completeBannerText = completeBannerObj.transform.Find("BannerText")?.GetComponent<TextMeshProUGUI>();
                    completeBannerObj.SetActive(false);
                }
                ApplyReadableStyle();
                return;
            }

            // 1. Prompt Panel
            promptPanel = new GameObject("Tutorial_PromptPanel");
            promptPanel.transform.SetParent(targetCanvas.transform, false);

            RectTransform panelRect = promptPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(820f, 320f);
            panelRect.anchoredPosition = new Vector2(0f, 30f);

            Image panelBg = promptPanel.AddComponent<Image>();
            panelBg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

            Outline outline = promptPanel.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.84f, 0f, 0.8f);
            outline.effectDistance = new Vector2(3f, -3f);

            TMP_FontAsset fontAsset = null;
            TextMeshProUGUI anyTmp = targetCanvas.GetComponentInChildren<TextMeshProUGUI>();
            if (anyTmp != null) fontAsset = anyTmp.font;

            // 1.1 Badge Text
            GameObject badgeObj = new GameObject("BadgeText");
            badgeObj.transform.SetParent(promptPanel.transform, false);
            RectTransform badgeRect = badgeObj.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0f, 1f);
            badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(0.5f, 1f);
            badgeRect.anchoredPosition = new Vector2(0f, -15f);
            badgeRect.sizeDelta = new Vector2(-40f, 35f);

            subtitleText = badgeObj.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) subtitleText.font = fontAsset;
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.fontSize = 20;
            subtitleText.color = new Color(1f, 0.84f, 0f, 1f);

            // 1.2 Action Title
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(promptPanel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -55f);
            titleRect.sizeDelta = new Vector2(-40f, 55f);

            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) titleText.font = fontAsset;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontSize = 32;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = Color.white;

            // 1.3 Key Prompt Box
            GameObject promptBoxObj = new GameObject("KeyPromptBox");
            promptBoxObj.transform.SetParent(promptPanel.transform, false);
            RectTransform promptBoxRect = promptBoxObj.AddComponent<RectTransform>();
            promptBoxRect.anchorMin = new Vector2(0.05f, 0.18f);
            promptBoxRect.anchorMax = new Vector2(0.95f, 0.70f);
            promptBoxRect.offsetMin = Vector2.zero;
            promptBoxRect.offsetMax = Vector2.zero;

            Image boxBg = promptBoxObj.AddComponent<Image>();
            boxBg.color = new Color(0.15f, 0.16f, 0.22f, 0.9f);

            GameObject promptTextObj = new GameObject("PromptText");
            promptTextObj.transform.SetParent(promptBoxObj.transform, false);
            RectTransform promptTextRect = promptTextObj.AddComponent<RectTransform>();
            promptTextRect.anchorMin = Vector2.zero;
            promptTextRect.anchorMax = Vector2.one;
            promptTextRect.offsetMin = new Vector2(15f, 10f);
            promptTextRect.offsetMax = new Vector2(-15f, -10f);

            keyPromptText = promptTextObj.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) keyPromptText.font = fontAsset;
            keyPromptText.alignment = TextAlignmentOptions.Center;
            keyPromptText.fontSize = 24;
            keyPromptText.color = new Color(1f, 1f, 0.4f, 1f);

            // 1.4 Footer Hint Text
            GameObject hintObj = new GameObject("HintText");
            hintObj.transform.SetParent(promptPanel.transform, false);
            RectTransform hintRect = hintObj.AddComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 10f);
            hintRect.sizeDelta = new Vector2(-40f, 30f);

            hintText = hintObj.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) hintText.font = fontAsset;
            hintText.alignment = TextAlignmentOptions.Center;
            hintText.fontSize = 16;
            hintText.color = new Color(0.6f, 0.65f, 0.75f, 0.9f);
            hintText.text = "NOTE: Press designated key or step on pad | Press [ESC] or [ENTER] to Skip";

            ApplyReadableStyle();
            promptPanel.SetActive(false);

            // 2. Complete Banner
            completeBannerObj = new GameObject("Tutorial_CompleteBanner");
            completeBannerObj.transform.SetParent(targetCanvas.transform, false);

            RectTransform bannerRect = completeBannerObj.AddComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 0.65f);
            bannerRect.anchorMax = new Vector2(0.5f, 0.65f);
            bannerRect.pivot = new Vector2(0.5f, 0.5f);
            bannerRect.sizeDelta = new Vector2(650f, 130f);

            Image bannerBg = completeBannerObj.AddComponent<Image>();
            bannerBg.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);

            Outline bannerOutline = completeBannerObj.AddComponent<Outline>();
            bannerOutline.effectColor = new Color(0f, 1f, 0.5f, 0.9f);
            bannerOutline.effectDistance = new Vector2(2f, -2f);

            GameObject bannerTextObj = new GameObject("BannerText");
            bannerTextObj.transform.SetParent(completeBannerObj.transform, false);
            RectTransform bTextRect = bannerTextObj.AddComponent<RectTransform>();
            bTextRect.anchorMin = Vector2.zero;
            bTextRect.anchorMax = Vector2.one;
            bTextRect.offsetMin = Vector2.zero;
            bTextRect.offsetMax = Vector2.zero;

            completeBannerText = bannerTextObj.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) completeBannerText.font = fontAsset;
            completeBannerText.alignment = TextAlignmentOptions.Center;
            completeBannerText.fontSize = 32;

            completeBannerObj.SetActive(false);
        }

        private void ShowPrompt(string badge, string actionTitle, string subtitle, string keyPrompt, Color accentColor)
        {
            CookieAudio.Play(CookieSfx.TutorialPrompt);
            if (promptPanel == null)
            {
                CreateTutorialUI();
            }

            if (promptPanel == null) return;

            if (subtitleText != null) subtitleText.text = badge;
            if (titleText != null)
            {
                titleText.text = actionTitle;
                titleText.color = accentColor;
            }
            if (keyPromptText != null) keyPromptText.text = keyPrompt;

            promptPanel.SetActive(true);
        }

        private void HidePrompt()
        {
            if (promptPanel != null)
            {
                promptPanel.SetActive(false);
            }
        }
        #endregion
    }
}
