using UnityEngine;

public class SoundManagerMain : MonoBehaviour
{
    public static SoundManagerMain Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Default Volume Settings (0.0 - 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultBGMVolume = 0.5f;

    [Range(0f, 1f)]
    [SerializeField] private float defaultSFXVolume = 1.0f;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip backgroundMusicClip;
    [SerializeField] private AudioClip buttonClickSFX;
    [SerializeField] private AudioClip digSFX;
    [SerializeField] private AudioClip explosionSFX;

    [Header("Walk SFX Settings")]
    [Tooltip("ลากไฟล์เสียงเดินมาใส่ที่นี่")]
    [SerializeField] private AudioClip walkSFX;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // หา AudioSource ที่มีอยู่แล้ว
        AudioSource[] sources = GetComponents<AudioSource>();

        // BGM AudioSource
        if (bgmAudioSource == null)
        {
            if (sources.Length > 0)
            {
                bgmAudioSource = sources[0];
            }
            else
            {
                bgmAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // SFX AudioSource
        if (sfxAudioSource == null)
        {
            if (sources.Length > 1)
            {
                sfxAudioSource = sources[1];
            }
            else
            {
                sfxAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // ตั้งค่า BGM AudioSource
        bgmAudioSource.playOnAwake = true;
        bgmAudioSource.loop = true;
        bgmAudioSource.volume = defaultBGMVolume;

        // ตั้งค่า SFX AudioSource
        sfxAudioSource.playOnAwake = false;
        sfxAudioSource.loop = false;
        sfxAudioSource.volume = defaultSFXVolume;

        Debug.Log("SoundManager: AudioSource initialized successfully.");
    }


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        // บังคับเปิดเสียงของ AudioListener
        AudioListener.volume = 1f;
        AudioListener.pause = false;

        Debug.Log("AudioListener Volume = " + AudioListener.volume);
        Debug.Log("AudioListener Pause = " + AudioListener.pause);

        Debug.Log("SoundManager: Start()");

        // ตรวจสอบ Background Music
        if (backgroundMusicClip == null)
        {
            Debug.LogError(
                "SoundManager: Background Music Clip ยังไม่ได้ใส่!"
            );

            return;
        }

        // เล่นเพลง
        PlayBGM(backgroundMusicClip);

        // ตั้ง Volume
        SetBGMVolume(defaultBGMVolume);
        SetSFXVolume(defaultSFXVolume);
    }


    // =========================================================
    // Update - ตรวจสอบสถานะ BGM
    // =========================================================

    private void Update()
    {
        if (bgmAudioSource != null)
        {
            if (bgmAudioSource.clip != null)
            {
                Debug.Log(
                    "BGM Playing: " +
                    bgmAudioSource.isPlaying +
                    " | Time: " +
                    bgmAudioSource.time.ToString("F2") +
                    " / " +
                    bgmAudioSource.clip.length.ToString("F2")
                );
            }
            else
            {
                Debug.Log(
                    "BGM AudioSource มีอยู่ แต่ยังไม่มี AudioClip"
                );
            }
        }
        else
        {
            Debug.Log("BGM AudioSource เป็น NULL");
        }
    }


    // =========================================================
    // BGM
    // =========================================================

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning(
                "SoundManager: PlayBGM() ได้รับ AudioClip เป็น Null"
            );

            return;
        }

        if (bgmAudioSource == null)
        {
            Debug.LogError(
                "SoundManager: BGM AudioSource เป็น Null!"
            );

            return;
        }

        // ถ้าเพลงเดียวกันกำลังเล่นอยู่แล้ว
        // ไม่ต้องเล่นซ้ำ
        if (bgmAudioSource.clip == clip &&
            bgmAudioSource.isPlaying)
        {
            return;
        }

        bgmAudioSource.clip = clip;
        bgmAudioSource.loop = true;
        bgmAudioSource.volume = defaultBGMVolume;

        bgmAudioSource.Play();

        Debug.Log(
            "SoundManager: Playing BGM -> " +
            clip.name +
            " | Volume = " +
            bgmAudioSource.volume
        );
    }


    // =========================================================
    // SFX
    // =========================================================

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning(
                "SoundManager: PlaySFX() ได้รับ AudioClip เป็น Null"
            );

            return;
        }

        if (sfxAudioSource == null)
        {
            Debug.LogError(
                "SoundManager: SFX AudioSource เป็น Null!"
            );

            return;
        }

        sfxAudioSource.PlayOneShot(
            clip,
            sfxAudioSource.volume
        );

        Debug.Log(
            "SoundManager: Playing SFX -> " +
            clip.name
        );
    }


    // =========================================================
    // Button Click
    // =========================================================

    public void PlayButtonClick()
    {
        if (buttonClickSFX != null)
        {
            PlaySFX(buttonClickSFX);
        }
    }


    // =========================================================
    // Explosion
    // =========================================================

    public void PlayExplosionSFX()
    {
        if (explosionSFX != null)
        {
            PlaySFX(explosionSFX);
        }
    }


    // =========================================================
    // Dig
    // =========================================================

    public void PlayDigSFX()
    {
        if (digSFX != null)
        {
            PlaySFX(digSFX);
        }
    }


    // =========================================================
    // Walk
    // =========================================================

    public void PlayWalkSFX()
    {
        if (walkSFX != null)
        {
            PlaySFX(walkSFX);
        }
    }


    // =========================================================
    // Volume
    // =========================================================

    public void SetBGMVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);

        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = volume;
        }
    }


    public void SetSFXVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);

        if (sfxAudioSource != null)
        {
            sfxAudioSource.volume = volume;
        }
    }


    // =========================================================
    // Stop BGM
    // =========================================================

    public void StopBGM()
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.Stop();

            Debug.Log("SoundManager: BGM Stopped");
        }
    }


    // =========================================================
    // Check BGM Status
    // =========================================================

    public bool IsBGMPlaying()
    {
        return bgmAudioSource != null &&
               bgmAudioSource.isPlaying;
    }
}