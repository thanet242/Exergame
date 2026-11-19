using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookieRun
{
    /// <summary>
    /// ควบคุมการแสดงผล UI ทั้งหมด: หลอดเลือด Energy, คะแนน, เหรียญ, ระยะทาง, ปุ่ม Jump/Slide และหน้าต่าง Game Over
    /// </summary>
    public class CookieUIManager : MonoBehaviour
    {
        [Header("Health / Energy Bar")]
        [Tooltip("เปิด/ปิดการแสดงหลอดเลือด (ตั้งเป็น false หากต้องการซ่อนระบบเลือด)")]
        [SerializeField] private bool showHealthBar = false;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Gradient healthGradient;
        [SerializeField] private float sliderSmoothSpeed = 10f;

        [Header("Item / Buff Indicators")]
        [SerializeField] private GameObject shieldIndicator;
        [SerializeField] private TextMeshProUGUI activeBuffText;

        [Header("HUD Text (TextMeshPro)")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private TextMeshProUGUI distanceText;

        [Header("Level Up Banner")]
        [Tooltip("ข้อความแจ้งขึ้นระดับ (ถ้าไม่ใส่ จะสร้างให้อัตโนมัติกลางจอด้านบน)")]
        [SerializeField] private TextMeshProUGUI levelBannerText;
        [SerializeField] private float levelBannerDuration = 2.2f;

        [Header("Touch / On-Screen Buttons")]
        [Tooltip("เปิดใช้งานปุ่มกดกระโดดและสไลด์บนหน้าจอ (ปิดเมื่อใช้แผ่นเหยียบเท้าหรือคีย์บอร์ด)")]
        [SerializeField] private bool showOnScreenButtons = false;
        [SerializeField] private CookieTouchButton jumpTouchButton;
        [SerializeField] private CookieTouchButton slideTouchButton;

        [Header("Game Over Popup")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI finalCoinText;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private string mainMenuSceneName = "Mainmenu";
        [Tooltip("ใช้หน้าสรุปผลแบบม้วนกระดาษเหมือนฉากถ้ำ (Time / Traps Cleared / Calories + ปุ่ม Home / Restart)")]
        [SerializeField] private bool useResultPopup = true;
        [Tooltip("หน่วงเวลาให้เห็นตัวละครล้มก่อนขึ้นหน้าสรุปผล (วินาที)")]
        [SerializeField] private float gameOverPopupDelay = 1.2f;

        [Header("Styled HUD (ป้ายไม้ด้านบนจอ)")]
        [Tooltip("สร้าง HUD แบบป้ายไม้: คะแนน / ระยะทาง ที่มุมซ้ายบน")]
        [SerializeField] private bool useStyledHud = true;

        private CookiePlayerController player;
        private CookieHealth playerHealth;
        private float targetHealthPercent = 1f;

        // แคชค่าที่แสดงล่าสุด เพื่ออัปเดต TMP เฉพาะตอนค่าเปลี่ยน (ลด GC และการ Rebuild Mesh ทุกเฟรม)
        private int lastBuffMask = -1;
        private int lastDistanceMeters = -1;

        private void Start()
        {
            player = FindObjectOfType<CookiePlayerController>();
            if (player != null)
            {
                playerHealth = player.GetComponent<CookieHealth>();

                // ผูกปุ่ม Touch ให้ทำงานกับตัวละคร
                if (jumpTouchButton != null)
                {
                    jumpTouchButton.onPointerDown.AddListener(player.OnJumpPressed);
                    jumpTouchButton.onPointerUp.AddListener(player.OnJumpReleased);
                }

                if (slideTouchButton != null)
                {
                    slideTouchButton.onPointerDown.AddListener(player.OnSlideStart);
                    slideTouchButton.onPointerUp.AddListener(player.OnSlideEnd);
                }
            }

            // จัดการการแสดงผลปุ่ม Touch บนหน้าจอ (ซ่อนเมื่อเล่นด้วยแผ่นเหยียบเท้าหรือคีย์บอร์ด)
            if (!showOnScreenButtons)
            {
                if (jumpTouchButton != null) jumpTouchButton.gameObject.SetActive(false);
                if (slideTouchButton != null) slideTouchButton.gameObject.SetActive(false);
            }

            // สมัครรับ Event จาก Health
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateHealthUI;
            }

            // สมัครรับ Event จาก GameManager
            if (CookieGameManager.Instance != null)
            {
                CookieGameManager.Instance.OnScoreChanged += UpdateScoreUI;
                CookieGameManager.Instance.OnCoinsChanged += UpdateCoinUI;
                CookieGameManager.Instance.OnDistanceChanged += UpdateDistanceUI;
                CookieGameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                CookieGameManager.Instance.OnLevelChanged += HandleLevelChanged;
            }

            if (levelBannerText == null) levelBannerText = CreateLevelBanner();
            if (levelBannerText != null) levelBannerText.gameObject.SetActive(false);

            if (useStyledHud) BuildStyledHud();

            // แถบหัวใจใต้ป้ายคะแนนมุมซ้ายบน
            Canvas hudCanvas = GetComponentInParent<Canvas>();
            if (hudCanvas == null) hudCanvas = FindObjectOfType<Canvas>();
            if (hudCanvas != null && playerHealth != null)
                CookieHeartsHud.Create((RectTransform)hudCanvas.transform, playerHealth, player, new Vector2(52f, -146f));

            // ตั้งค่าปุ่มใน Game Over Panel
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(() =>
                {
                    if (CookieGameManager.Instance != null)
                        CookieGameManager.Instance.RestartGame();
                });
            }

            if (menuButton != null)
            {
                menuButton.onClick.AddListener(() =>
                {
                    if (CookieGameManager.Instance != null)
                        CookieGameManager.Instance.LoadScene(mainMenuSceneName);
                });
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            // จัดการการแสดงผลหลอดเลือด
            if (!showHealthBar && healthSlider != null)
            {
                healthSlider.gameObject.SetActive(false);
            }

            if (player != null)
            {
                player.OnShieldChanged += HandleShieldChanged;
                HandleShieldChanged(player.HasShield);
            }
        }

        private void OnDestroy()
        {
            if (player != null)
            {
                player.OnShieldChanged -= HandleShieldChanged;
            }

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateHealthUI;
            }

            if (CookieGameManager.Instance != null)
            {
                CookieGameManager.Instance.OnScoreChanged -= UpdateScoreUI;
                CookieGameManager.Instance.OnCoinsChanged -= UpdateCoinUI;
                CookieGameManager.Instance.OnDistanceChanged -= UpdateDistanceUI;
                CookieGameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
                CookieGameManager.Instance.OnLevelChanged -= HandleLevelChanged;
            }
        }

        private void HandleLevelChanged(int level, string announcement)
        {
            if (levelBannerText == null) return;

            string title = $"<color=#FFD700>LEVEL {level}</color>  <size=70%>SPEED UP!</size>";
            levelBannerText.text = string.IsNullOrEmpty(announcement)
                ? title
                : $"{title}\n<size=60%><color=#7FE8FF>{announcement}</color></size>";

            if (levelBannerRoutine != null) StopCoroutine(levelBannerRoutine);
            levelBannerRoutine = StartCoroutine(LevelBannerRoutine());
        }

        private Coroutine levelBannerRoutine;

        private System.Collections.IEnumerator LevelBannerRoutine()
        {
            GameObject banner = levelBannerText.gameObject;
            Transform t = banner.transform;
            banner.SetActive(true);

            float elapsed = 0f;
            while (elapsed < levelBannerDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                // เด้งขยายตอนเข้า แล้วจางหายตอนท้าย
                float pop = Mathf.Clamp01(elapsed / 0.18f);
                t.localScale = Vector3.one * Mathf.LerpUnclamped(1.6f, 1f, 1f - (1f - pop) * (1f - pop));
                float fadeStart = levelBannerDuration - 0.5f;
                levelBannerText.alpha = elapsed < fadeStart ? 1f : Mathf.Clamp01((levelBannerDuration - elapsed) / 0.5f);

                yield return null;
            }

            banner.SetActive(false);
            levelBannerRoutine = null;
        }

        private TextMeshProUGUI CreateLevelBanner()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return null;

            GameObject obj = new GameObject("LevelUpBanner", typeof(RectTransform));
            obj.transform.SetParent(canvas.transform, false);

            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -150f);
            rect.sizeDelta = new Vector2(900f, 160f);

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();

            // ใช้ฟอนต์เดียวกับ HUD เดิมถ้ามี
            if (scoreText != null) tmp.font = scoreText.font;

            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 64f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            return tmp;
        }

        private void Update()
        {
            // เลื่อนหลอดเลือดอย่างนุ่มนวล (Smooth Lerp) เฉพาะเมื่อเปิดใช้งาน
            if (showHealthBar && healthSlider != null && healthSlider.gameObject.activeSelf)
            {
                healthSlider.value = Mathf.Lerp(healthSlider.value, targetHealthPercent, Time.deltaTime * sliderSmoothSpeed);

                if (healthFillImage != null && healthGradient != null)
                {
                    healthFillImage.color = healthGradient.Evaluate(healthSlider.value);
                }
            }

            UpdateBuffIndicators();
        }

        /// <summary>
        /// HUD ป้ายไม้ (สไตล์เดียวกับ UI ฉากอื่น): คะแนน / ระยะทาง ที่มุมซ้ายบน
        /// ใช้ป้ายจาก UIArtLibrary และย้ายการแสดงคะแนน/ระยะทางมาที่ป้ายใหม่
        /// </summary>
        private void BuildStyledHud()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            Sprite plaque = UIArtLibrary.Instance != null ? UIArtLibrary.Instance.plaqueSprite : null;
            RectTransform root = (RectTransform)canvas.transform;

            // ระหว่างวิ่งแสดงแค่คะแนนกับระยะทาง (เวลา/แคลอรี่ ดูได้ที่หน้าสรุปผล)
            TextMeshProUGUI newScore = CreateHudPlaque(root, plaque, new Vector2(0f, 1f), new Vector2(40f, -30f));
            TextMeshProUGUI newDistance = CreateHudPlaque(root, plaque, new Vector2(0f, 1f), new Vector2(390f, -30f));

            // ซ่อนข้อความ HUD เดิม แล้วให้ระบบเดิมเขียนลงป้ายใหม่แทน
            if (scoreText != null) scoreText.gameObject.SetActive(false);
            if (distanceText != null) distanceText.gameObject.SetActive(false);
            scoreText = newScore;
            distanceText = newDistance;

            CookieGameManager gm = CookieGameManager.Instance;
            UpdateScoreUI(gm != null ? gm.CurrentScore : 0);
            lastDistanceMeters = -1;
            UpdateDistanceUI(gm != null ? gm.DistanceTraveled : 0f);
        }

        private static TextMeshProUGUI CreateHudPlaque(RectTransform root, Sprite plaque, Vector2 anchor, Vector2 position)
        {
            GameObject bg = new GameObject("HudPlaque", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(root, false);
            RectTransform br = (RectTransform)bg.transform;
            br.anchorMin = br.anchorMax = anchor;
            br.pivot = new Vector2(anchor.x, 1f);
            br.sizeDelta = new Vector2(330f, 102f);
            br.anchoredPosition = position;
            Image img = bg.GetComponent<Image>();
            img.sprite = plaque;
            img.color = plaque != null ? Color.white : new Color(0.25f, 0.13f, 0.05f, 0.85f);
            img.raycastTarget = false;

            GameObject textObj = new GameObject("Value", typeof(RectTransform));
            textObj.transform.SetParent(br, false);
            RectTransform tr = (RectTransform)textObj.transform;
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(44f, 10f); tr.offsetMax = new Vector2(-44f, -8f);
            TextMeshProUGUI t = textObj.AddComponent<TextMeshProUGUI>();
            t.alignment = TextAlignmentOptions.Center;
            t.fontStyle = FontStyles.Bold;
            t.color = new Color(1f, 0.93f, 0.78f, 1f);
            t.enableAutoSizing = true;
            t.fontSizeMin = 16f;
            t.fontSizeMax = 38f;
            t.lineSpacing = -20f;
            t.raycastTarget = false;
            return t;
        }

        private void HandleShieldChanged(bool hasShield)
        {
            if (shieldIndicator != null)
            {
                shieldIndicator.SetActive(hasShield);
            }
        }

        private void UpdateBuffIndicators()
        {
            if (activeBuffText != null && player != null)
            {
                int mask = (player.HasShield ? 1 : 0)
                         | (player.IsMagnetActive ? 2 : 0)
                         | (player.IsBlast ? 4 : 0)
                         | (player.IsGiant ? 8 : 0);
                if (mask == lastBuffMask) return;
                lastBuffMask = mask;

                string buffs = "";
                if (player.HasShield) buffs += "<color=#00e5ff>🛡️ SHIELD</color>  ";
                if (player.IsMagnetActive) buffs += "<color=#ffd700>🧲 MAGNET</color>  ";
                if (player.IsBlast) buffs += "<color=#ff5722>🚀 BLAST</color>  ";
                if (player.IsGiant) buffs += "<color=#e040fb>🦾 GIANT</color>  ";
                activeBuffText.text = buffs;
            }
        }

        private void UpdateHealthUI(float current, float max)
        {
            targetHealthPercent = max > 0f ? (current / max) : 0f;
        }

        private void UpdateScoreUI(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = useStyledHud ? $"<size=60%>SCORE</size>\n{score:N0}" : score.ToString("N0");
            }
        }

        private void UpdateCoinUI(int coins)
        {
            if (coinText != null)
            {
                coinText.text = coins.ToString("N0");
            }
        }

        private void UpdateDistanceUI(float distance)
        {
            int meters = Mathf.FloorToInt(distance);
            if (distanceText != null && meters != lastDistanceMeters)
            {
                lastDistanceMeters = meters;
                distanceText.text = useStyledHud ? $"<size=60%>DISTANCE</size>\n{meters} m" : $"{meters}m";
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.GameOver)
            {
                ShowGameOverPanel();
            }
        }

        private void ShowGameOverPanel()
        {
            if (!useResultPopup)
            {
                if (gameOverPanel != null) gameOverPanel.SetActive(true);
                CookieGameManager gm0 = CookieGameManager.Instance;
                if (gm0 != null)
                {
                    if (finalScoreText != null) finalScoreText.text = gm0.CurrentScore.ToString("N0");
                    if (finalCoinText != null) finalCoinText.text = gm0.CurrentCoins.ToString("N0");
                    if (highScoreText != null) highScoreText.text = $"BEST: {gm0.HighScore:N0}";
                }
                return;
            }

            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            StartCoroutine(ShowResultPopupRoutine());
        }

        // หน้าสรุปผลแบบเดียวกับฉากถ้ำ: Time / Traps Cleared (สิ่งกีดขวางที่ผ่าน) / Calories Burned
        private System.Collections.IEnumerator ShowResultPopupRoutine()
        {
            yield return new WaitForSecondsRealtime(gameOverPopupDelay);

            CookieGameManager gm = CookieGameManager.Instance;
            if (gm == null) yield break;

            bool newBest = gm.CurrentScore > 0 && gm.CurrentScore >= gm.HighScore;
            var data = new ResultPopupUI.Data
            {
                seconds = gm.PlayTimeSeconds,
                // ช่องกลางของโหมดวิ่งแสดงระยะทางที่วิ่งได้ (ภาพหัวข้อ "Distance")
                middleValue = $"{Mathf.FloorToInt(gm.DistanceTraveled)}<size=55%> m</size>",
                // หน้าสรุปผล (หัวข้อ RESULTS) แทนหน้า GAME OVER: โหมดวิ่งไม่รู้จบ จบเมื่อไหร่ก็คือสรุปผลการวิ่งรอบนั้น
                panelOverride = UIArtLibrary.Instance != null
                    ? (UIArtLibrary.Instance.resultDistanceSprite != null ? UIArtLibrary.Instance.resultDistanceSprite : UIArtLibrary.Instance.gameOverDistanceSprite)
                    : null,
                calories = gm.CaloriesBurned,
                extraLine = $"SCORE {gm.CurrentScore:N0}   |   COINS {gm.CurrentCoins:N0}   |   " +
                            (newBest ? "<color=#C0392B>NEW BEST!</color>" : $"BEST {gm.HighScore:N0}"),
                // สรุปการเคลื่อนไหวทั้งหมดของรอบนี้
                statsLine = $"JUMPS <color=#FFD36B>{gm.JumpCount}</color>    SLIDES <color=#FFD36B>{gm.SlideCount}</color>    " +
                            $"OBSTACLES PASSED <color=#FFD36B>{gm.ObstaclesCleared}</color>    LEVEL <color=#FFD36B>{gm.CurrentLevel}</color>"
            };

            ResultPopupUI.Show(ResultPopupUI.Kind.StageClear, data,
                () => gm.LoadScene(mainMenuSceneName),
                () => gm.RestartGame());
        }
    }
}
