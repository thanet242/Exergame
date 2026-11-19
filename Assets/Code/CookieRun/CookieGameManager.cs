using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CookieRun
{
    public enum GameState
    {
        Ready,
        Playing,
        Paused,
        GameOver
    }

    /// <summary>
    /// ระดับความยาก (Level) ที่ปลดล็อกตามระยะทางที่วิ่งได้
    /// </summary>
    [Serializable]
    public class DifficultyLevel
    {
        [Tooltip("ระยะทาง (เมตร) ที่จะขึ้นระดับนี้")]
        public float startDistance;
        [Tooltip("ข้อความแจ้งเตือนตอนขึ้นระดับ เช่น ชื่อสิ่งกีดขวางใหม่")]
        public string announcement;
    }

    /// <summary>
    /// ผู้จัดการหลักของเกม Cookie Run จัดการคะแนน เหรียญ สถานะเกม และ High Score
    /// </summary>
    public class CookieGameManager : MonoBehaviour
    {
        public static CookieGameManager Instance { get; private set; }

        [Header("Difficulty (ความยากเพิ่มขึ้นตามระยะทาง)")]
        [Tooltip("ความเร็ววิ่งเพิ่มขึ้นต่อระยะทาง 1 เมตร (0.0008 = เร็วขึ้น 8% ทุก 100 เมตร)")]
        [SerializeField] private float speedRampPerMeter = 0.0008f;
        [Tooltip("ตัวคูณความเร็วสูงสุด (1.4 = เร็วกว่าตอนเริ่ม 40%) แผ่นเหยียบไม่มีราวจับ ถ้าเร็วกว่านี้จะเหยียบต่อเนื่องไม่ทัน")]
        [SerializeField] private float maxSpeedMultiplier = 1.4f;
        [Tooltip("ระดับความยาก เรียงตามระยะทาง (ระดับแรกควรเริ่มที่ 0 เมตร)")]
        [SerializeField] private DifficultyLevel[] difficultyLevels =
        {
            new DifficultyLevel { startDistance = 0f,   announcement = "" },
            new DifficultyLevel { startDistance = 150f, announcement = "ROLLING BOULDERS!" },
            new DifficultyLevel { startDistance = 300f, announcement = "FALLING STALACTITES!" },
            new DifficultyLevel { startDistance = 450f, announcement = "SPIKE TRAPS!" },
            new DifficultyLevel { startDistance = 650f, announcement = "MAX SPEED!" },
            new DifficultyLevel { startDistance = 850f, announcement = "GIANT MUSHROOMS!" },
            new DifficultyLevel { startDistance = 1050f, announcement = "HANGING ROOTS!" },
            new DifficultyLevel { startDistance = 1250f, announcement = "CRYSTAL SPIRES!" },
        };

        private int currentLevel = 1;

        [Header("Exercise Stats (สถิติการออกกำลังกาย)")]
        [Tooltip("ค่า METs ของการวิ่งเหยียบแผ่น (ใช้คำนวณแคลอรี่ เหมือนด่านอื่นในโปรเจกต์)")]
        [SerializeField] private float exerciseMETs = 5.0f;
        [Tooltip("น้ำหนักเริ่มต้น (กก.) ถ้าผู้เล่นยังไม่ได้กรอกน้ำหนักจากหน้าเมนู")]
        [SerializeField] private float defaultWeightKg = 60f;

        private float playTimeSeconds;
        private int jumpCount;
        private int slideCount;

        /// <summary>เวลาที่เล่นจริง (วินาที) ไม่นับช่วงที่เกมหยุด</summary>
        public float PlayTimeSeconds => playTimeSeconds;
        public int JumpCount => jumpCount;
        public int SlideCount => slideCount;

        private int obstaclesCleared;

        /// <summary>จำนวนสิ่งกีดขวางที่ผ่านพ้นได้ (แสดงในช่อง Traps Cleared ของหน้าสรุปผล)</summary>
        public int ObstaclesCleared => obstaclesCleared;

        public void RecordObstacleCleared() { if (IsPlaying && !isTutorialActive) obstaclesCleared++; }

        public void RecordJump() { if (IsPlaying) jumpCount++; }
        public void RecordSlide() { if (IsPlaying) slideCount++; }

        /// <summary>
        /// แคลอรี่ที่เผาผลาญ: (METs x 3.5 x น้ำหนัก) / 200 x นาที
        /// ใช้น้ำหนักที่ผู้เล่นกรอกไว้ (PlayerPrefs "UserWeightKg") เหมือนด่านถ้ำ
        /// </summary>
        public float CaloriesBurned
        {
            get
            {
                float weight = PlayerPrefs.GetFloat("UserWeightKg", defaultWeightKg);
                return (exerciseMETs * 3.5f * weight / 200f) * (playTimeSeconds / 60f);
            }
        }

        /// <summary>
        /// ตัวคูณความเร็ววิ่งจากความยาก (1 = ปกติ) ไม่เพิ่มระหว่างช่วงสอนเล่น
        /// ระดับ "สบาย ๆ": วิ่งความเร็วเริ่มต้นตลอด (ไม่ลดต่ำกว่า 1 เพราะระยะกระโดดออกแบบไว้ที่ความเร็วนี้)
        /// </summary>
        public float SpeedMultiplier => isTutorialActive || ExhibitMode.Relaxed ? 1f : Mathf.Min(maxSpeedMultiplier, 1f + distanceTraveled * speedRampPerMeter);

        // เวลาวิ่งจริงหลังจบช่วงสอนเล่น (ใช้จำกัดเวลาต่อรอบในงานแสดง)
        private float runSeconds;

        /// <summary>งานแสดง: เวลาที่เหลือของรอบนี้ (วินาที) / -1 = ไม่จำกัด</summary>
        public float ExhibitTimeLeft => ExhibitMode.On ? Mathf.Max(0f, ExhibitMode.RunnerTimeLimit - runSeconds) : -1f;

        /// <summary>ระดับความยากปัจจุบัน (เริ่มที่ 1)</summary>
        public int CurrentLevel => currentLevel;

        /// <summary>แจ้งเมื่อขึ้นระดับใหม่ (ระดับ, ข้อความแจ้งเตือน)</summary>
        public event Action<int, string> OnLevelChanged;

        [Header("Distance Score")]
        [Tooltip("คะแนนที่ได้รับต่อระยะทางวิ่ง 1 เมตร (ตั้งเป็น 0 เพื่อให้ได้คะแนนเฉพาะเมื่อเก็บเหรียญเท่านั้น)")]
        [SerializeField] private float scorePerMeter = 0f;

        [Header("State & Stats")]
        [SerializeField] private GameState currentState = GameState.Playing;
        [SerializeField] private int currentScore = 0;
        [SerializeField] private int currentCoins = 0;
        [SerializeField] private float distanceTraveled = 0f;
        [SerializeField] private int highScore = 0;

        private Transform playerTransform;
        private float startPlayerX;
        private float distanceScoreAccumulator = 0f;

        // Events
        public event Action<int> OnScoreChanged;
        public event Action<int> OnCoinsChanged;
        public event Action<float> OnDistanceChanged;
        public event Action<GameState> OnGameStateChanged;

        public GameState CurrentState => currentState;
        public bool IsPlaying => currentState == GameState.Playing;
        public int CurrentScore => currentScore;
        public int CurrentCoins => currentCoins;
        public float DistanceTraveled => distanceTraveled;
        public int HighScore => highScore;

        private const string PREF_HIGH_SCORE = "COOKIE_RUN_HIGH_SCORE";
        private const string PREF_TOTAL_COINS = "COOKIE_RUN_TOTAL_COINS";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadSavedData();
        }

        private void Start()
        {
            CookiePlayerController player = FindObjectOfType<CookiePlayerController>();
            if (player != null)
            {
                playerTransform = player.transform;
                startPlayerX = playerTransform.position.x;
            }

            // Ensure CookieTutorialManager is present if pre-game tutorial is needed
            if (FindObjectOfType<CookieTutorialManager>() == null && player != null)
            {
                GameObject tutObj = new GameObject("TutorialManager");
                tutObj.AddComponent<CookieTutorialManager>();
            }

            currentState = GameState.Playing;
            OnGameStateChanged?.Invoke(currentState);
            OnScoreChanged?.Invoke(currentScore);
            OnCoinsChanged?.Invoke(currentCoins);
        }

        [Header("Tutorial State")]
        [Tooltip("อยู่ในช่วงฝึกสอนก่อนเริ่มเกมหรือไม่ (จะไม่นับระยะทางจนกว่าจะเริ่มเกมจริง)")]
        [SerializeField] private bool isTutorialActive = true;
        public bool IsTutorialActive { get => isTutorialActive; set => isTutorialActive = value; }

        public void ResetDistanceTracking(float newStartX)
        {
            startPlayerX = newStartX;
            distanceTraveled = 0f;
            distanceScoreAccumulator = 0f;
            isTutorialActive = false;
            OnDistanceChanged?.Invoke(0f);
        }

        private void Update()
        {
            if (currentState != GameState.Playing) return;

            // นับเวลาออกกำลังกายรวมช่วงสอนเล่นด้วย (deltaTime เป็น 0 ตอนเกมหยุดรอกดปุ่ม จึงไม่ถูกนับ)
            playTimeSeconds += Time.deltaTime;

            if (isTutorialActive) return;

            // งานแสดง: จำกัดเวลาต่อรอบ ให้คิวเดินเร็ว (หมดเวลา = สรุปผล)
            runSeconds += Time.deltaTime;
            if (ExhibitMode.On && runSeconds >= ExhibitMode.RunnerTimeLimit)
            {
                TriggerGameOver();
                return;
            }

            // คำนวณระยะทางที่วิ่งได้
            if (playerTransform != null)
            {
                float newDistance = Mathf.Max(0f, playerTransform.position.x - startPlayerX);
                float deltaDist = newDistance - distanceTraveled;

                if (deltaDist > 0f)
                {
                    distanceTraveled = newDistance;
                    OnDistanceChanged?.Invoke(distanceTraveled);
                    UpdateDifficultyLevel();

                    // เพิ่มคะแนนตามระยะทาง (เฉพาะเมื่อเปิดใช้งาน scorePerMeter > 0)
                    if (scorePerMeter > 0f)
                    {
                        distanceScoreAccumulator += deltaDist * scorePerMeter;
                        if (distanceScoreAccumulator >= 1f)
                        {
                            int addInt = Mathf.FloorToInt(distanceScoreAccumulator);
                            distanceScoreAccumulator -= addInt;
                            AddScore(addInt);
                        }
                    }
                }
            }
        }

        private void UpdateDifficultyLevel()
        {
            if (difficultyLevels == null) return;

            // ระดับถัดไปอยู่ที่ index = currentLevel (เพราะระดับเริ่มนับจาก 1)
            while (currentLevel < difficultyLevels.Length && distanceTraveled >= difficultyLevels[currentLevel].startDistance)
            {
                currentLevel++;
                OnLevelChanged?.Invoke(currentLevel, difficultyLevels[currentLevel - 1].announcement);
            }
        }

        public void AddScore(int amount)
        {
            if (amount <= 0 || currentState == GameState.GameOver) return;

            currentScore += amount;
            OnScoreChanged?.Invoke(currentScore);
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0 || currentState == GameState.GameOver) return;

            currentCoins += amount;
            OnCoinsChanged?.Invoke(currentCoins);
        }

        public void TriggerGameOver()
        {
            if (currentState == GameState.GameOver) return;

            currentState = GameState.GameOver;
            OnGameStateChanged?.Invoke(currentState);

            // ตรวจสอบและบันทึก High Score
            if (currentScore > highScore)
            {
                highScore = currentScore;
                PlayerPrefs.SetInt(PREF_HIGH_SCORE, highScore);
            }

            int totalCoins = PlayerPrefs.GetInt(PREF_TOTAL_COINS, 0) + currentCoins;
            PlayerPrefs.SetInt(PREF_TOTAL_COINS, totalCoins);
            PlayerPrefs.Save();
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            // เล่นใหม่ทันทีโดยไม่ต้องผ่านช่วงสอนเล่นซ้ำ (กลับเมนูแล้วเข้าใหม่ยังมีสอนเล่นตามปกติ สำหรับผู้เล่นคนใหม่)
            CookieTutorialManager.SkipNextTutorial = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadScene(string sceneName)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        private void LoadSavedData()
        {
            highScore = PlayerPrefs.GetInt(PREF_HIGH_SCORE, 0);
        }
    }
}
