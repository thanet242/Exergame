using UnityEngine;

namespace CookieRun
{
    /// <summary>เสียงเอฟเฟกต์ในโหมดวิ่ง</summary>
    public enum CookieSfx
    {
        Jump, DoubleJump, Slide, Coin, PowerUp, ShieldGet, ShieldBreak, Blast, Hurt, Death,
        LevelUp, Smash, MonsterKick, BoulderRoll, StalactiteShake, StalactiteCrash, SpikePop,
        TutorialPrompt, ReadyGo
    }

    /// <summary>
    /// ระบบเสียงของโหมดวิ่ง: เพลงประกอบ (BGM), เสียงเอฟเฟกต์ทุกเหตุการณ์ และเสียงฝีเท้าตอนวิ่ง
    /// เปลี่ยนเสียงแต่ละตัวได้ใน Inspector (ช่องที่ว่างจะไม่มีเสียง)
    /// ความดังรวมขึ้นกับ Volume ในหน้า Option (AudioListener.volume)
    /// </summary>
    public class CookieAudio : MonoBehaviour
    {
        public static CookieAudio Instance { get; private set; }

        [Header("Music")]
        public AudioClip backgroundMusic;
        [Range(0f, 1f)] public float musicVolume = 0.45f;
        public AudioClip gameOverJingle;

        [Header("Player")]
        public AudioClip jump;
        public AudioClip doubleJump;
        public AudioClip slide;
        public AudioClip hurt;
        public AudioClip death;
        [Tooltip("เสียงฝีเท้าแบบวนลูป เล่นตอนวิ่งอยู่บนพื้น")]
        public AudioClip footstepLoop;
        [Range(0f, 1f)] public float footstepVolume = 0.2f;

        [Header("Items")]
        public AudioClip coin;
        public AudioClip powerUp;
        public AudioClip shieldGet;
        public AudioClip shieldBreak;
        public AudioClip blast;

        [Header("Obstacles")]
        public AudioClip smash;
        public AudioClip monsterKick;
        public AudioClip boulderRoll;
        public AudioClip stalactiteShake;
        public AudioClip stalactiteCrash;
        public AudioClip spikePop;

        [Header("Game Flow")]
        public AudioClip levelUp;
        public AudioClip tutorialPrompt;
        public AudioClip readyGo;

        [Header("Mix")]
        [Range(0f, 1f)] public float sfxVolume = 0.07f;
        [Tooltip("สุ่มระดับเสียงสูงต่ำเล็กน้อย ไม่ให้เสียงซ้ำจำเจ (เช่น เก็บเหรียญติดกัน)")]
        [Range(0f, 0.2f)] public float pitchVariation = 0.06f;

        private AudioSource musicSource;
        private AudioSource footstepSource;
        private AudioSource[] sfxSources;
        private int nextSfx;
        private CookiePlayerController player;

        private void Awake()
        {
            Instance = this;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = musicVolume;

            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.loop = true;
            footstepSource.playOnAwake = false;
            footstepSource.clip = footstepLoop;
            footstepSource.volume = footstepVolume;

            // หลาย AudioSource เพื่อให้แต่ละเสียงปรับ pitch ได้อิสระและเล่นซ้อนกันได้
            sfxSources = new AudioSource[8];
            for (int i = 0; i < sfxSources.Length; i++)
            {
                sfxSources[i] = gameObject.AddComponent<AudioSource>();
                sfxSources[i].playOnAwake = false;
            }
        }

        private void Start()
        {
            player = CookiePlayerController.Instance;

            if (backgroundMusic != null)
            {
                musicSource.clip = backgroundMusic;
                musicSource.Play();
            }

            if (CookieGameManager.Instance != null)
            {
                CookieGameManager.Instance.OnLevelChanged += HandleLevelChanged;
                CookieGameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (CookieGameManager.Instance != null)
            {
                CookieGameManager.Instance.OnLevelChanged -= HandleLevelChanged;
                CookieGameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void Update()
        {
            UpdateFootsteps();
        }

        // เสียงฝีเท้า: ดังเมื่อวิ่งบนพื้น (ไม่ได้สไลด์/ลอยอยู่/เกมหยุด) และเร็วขึ้นตามความเร็ววิ่ง
        private void UpdateFootsteps()
        {
            if (footstepSource.clip == null) return;
            if (player == null) player = CookiePlayerController.Instance;

            CookieGameManager gm = CookieGameManager.Instance;
            bool running = player != null && player.IsGrounded && !player.IsSliding && Time.timeScale > 0f
                           && (gm == null || gm.IsPlaying);

            if (running)
            {
                float speedFactor = gm != null ? gm.SpeedMultiplier : 1f;
                footstepSource.pitch = 0.95f + (speedFactor - 1f) * 0.6f;
                if (!footstepSource.isPlaying) footstepSource.Play();
            }
            else if (footstepSource.isPlaying)
            {
                footstepSource.Pause();
            }
        }

        private void HandleLevelChanged(int level, string announcement) => PlayInternal(CookieSfx.LevelUp, 1f);

        private void HandleGameStateChanged(GameState state)
        {
            if (state != GameState.GameOver) return;

            StopCoroutine(nameof(CrossfadeRoutine));
            musicSource.volume = musicVolume;
            musicSource.Stop();
            footstepSource.Stop();
            if (gameOverJingle != null)
            {
                musicSource.loop = false;
                musicSource.clip = gameOverJingle;
                musicSource.PlayDelayed(0.6f);
            }
        }

        /// <summary>ค่อยๆ เปลี่ยนเพลงประกอบ (ใช้ตอนเปลี่ยนโซน)</summary>
        public void CrossfadeMusic(AudioClip clip, float duration)
        {
            if (clip == null || musicSource.clip == clip) return;
            StopCoroutine(nameof(CrossfadeRoutine));
            pendingMusic = clip;
            crossfadeDuration = duration;
            StartCoroutine(nameof(CrossfadeRoutine));
        }

        private AudioClip pendingMusic;
        private float crossfadeDuration = 2f;

        private System.Collections.IEnumerator CrossfadeRoutine()
        {
            float half = Mathf.Max(0.05f, crossfadeDuration * 0.5f);
            float start = musicSource.volume;
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                musicSource.volume = Mathf.Lerp(start, 0f, t / half);
                yield return null;
            }

            // เกมจบระหว่างเปลี่ยนเพลง: ไม่เล่นเพลงโซนทับเพลง Game Over
            if (CookieGameManager.Instance != null && !CookieGameManager.Instance.IsPlaying) yield break;

            musicSource.clip = pendingMusic;
            musicSource.loop = true;
            musicSource.Play();
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                musicSource.volume = Mathf.Lerp(0f, musicVolume, t / half);
                yield return null;
            }
            musicSource.volume = musicVolume;
        }

        /// <summary>เล่นเสียงเอฟเฟกต์ (ไม่มี CookieAudio ในฉาก = เงียบ)</summary>
        public static void Play(CookieSfx sfx, float volumeScale = 1f)
        {
            if (Instance != null) Instance.PlayInternal(sfx, volumeScale);
        }

        private void PlayInternal(CookieSfx sfx, float volumeScale)
        {
            AudioClip clip = ClipFor(sfx);
            if (clip == null) return;

            AudioSource src = sfxSources[nextSfx];
            nextSfx = (nextSfx + 1) % sfxSources.Length;

            src.pitch = sfx == CookieSfx.Coin ? NextCoinPitch() : 1f + Random.Range(-pitchVariation, pitchVariation);
            src.PlayOneShot(clip, sfxVolume * volumeScale);
        }

        [Header("Coin Combo")]
        [Tooltip("เก็บเหรียญต่อเนื่องภายในเวลานี้ (วินาที) เสียงจะสูงขึ้นทีละครึ่งเสียง")]
        public float coinComboWindow = 0.45f;
        [Tooltip("จำนวนขั้นสูงสุดที่เสียงเหรียญจะไล่ขึ้น")]
        public int coinComboMaxSteps = 7;

        private int coinCombo;
        private float lastCoinTime = -10f;

        // เสียงเหรียญไล่โน้ตขึ้นเมื่อเก็บติดกัน (ไม่สุ่ม เพื่อให้ฟังเป็นทำนอง)
        private float NextCoinPitch()
        {
            coinCombo = Time.unscaledTime - lastCoinTime <= coinComboWindow ? Mathf.Min(coinCombo + 1, coinComboMaxSteps) : 0;
            lastCoinTime = Time.unscaledTime;
            return Mathf.Pow(2f, coinCombo / 12f);
        }

        private AudioClip ClipFor(CookieSfx sfx)
        {
            switch (sfx)
            {
                case CookieSfx.Jump: return jump;
                case CookieSfx.DoubleJump: return doubleJump != null ? doubleJump : jump;
                case CookieSfx.Slide: return slide;
                case CookieSfx.Coin: return coin;
                case CookieSfx.PowerUp: return powerUp;
                case CookieSfx.ShieldGet: return shieldGet != null ? shieldGet : powerUp;
                case CookieSfx.ShieldBreak: return shieldBreak;
                case CookieSfx.Blast: return blast != null ? blast : powerUp;
                case CookieSfx.Hurt: return hurt;
                case CookieSfx.Death: return death;
                case CookieSfx.LevelUp: return levelUp;
                case CookieSfx.Smash: return smash;
                case CookieSfx.MonsterKick: return monsterKick != null ? monsterKick : smash;
                case CookieSfx.BoulderRoll: return boulderRoll;
                case CookieSfx.StalactiteShake: return stalactiteShake;
                case CookieSfx.StalactiteCrash: return stalactiteCrash != null ? stalactiteCrash : smash;
                case CookieSfx.SpikePop: return spikePop;
                case CookieSfx.TutorialPrompt: return tutorialPrompt;
                case CookieSfx.ReadyGo: return readyGo;
                default: return null;
            }
        }
    }
}
