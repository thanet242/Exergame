using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("ลาก AudioSource สำหรับ BGM มาใส่ (ถ้าเว้นว่าง ระบบจะสร้างให้อัตโนมัติ)")]
    public AudioSource bgmAudioSource;
    [Tooltip("ลาก AudioSource สำหรับ SFX มาใส่ (ถ้าเว้นว่าง ระบบจะสร้างให้อัตโนมัติ)")]
    public AudioSource sfxAudioSource;

    [Header("Volume Settings (0.0 - 1.0)")]
    [Range(0f, 1f)] public float defaultBGMVolume = 0.5f;
    [Range(0f, 1f)] public float defaultSFXVolume = 1.0f;

    [Header("Audio Clips ประจำฉากนี้")]
    public AudioClip backgroundMusicClip;
    public AudioClip buttonClickSFX;
    public AudioClip digSFX;
    public AudioClip explosionSFX;
    public AudioClip walkSFX;

    void Awake()
    {
        Instance = this;
        SetupAudioSources();
    }

    void Start()
    {
        if (backgroundMusicClip != null)
        {
            PlayBGM(backgroundMusicClip);
        }
        else
        {
            StopBGM();
        }

        SetBGMVolume(defaultBGMVolume);
        SetSFXVolume(defaultSFXVolume);
    }

    void OnDestroy()
    {
        StopBGM();
        StopSFX();
    }

    private void SetupAudioSources()
    {
        // ค้นหาหรือสร้าง AudioSource ให้แยกจากกันชัดเจน 2 ตัว
        AudioSource[] sources = GetComponents<AudioSource>();

        if (bgmAudioSource == null)
        {
            bgmAudioSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
        }

        if (sfxAudioSource == null)
        {
            sfxAudioSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
        }

        // ตั้งค่า BGM: เล่นวนลูป, โหมด 2D เต็มรูปแบบ
        bgmAudioSource.loop = true;
        bgmAudioSource.playOnAwake = false;
        bgmAudioSource.spatialBlend = 0f; // 2D Sound

        // ตั้งค่า SFX: ไม่วนลูป, โหมด 2D เต็มรูปแบบ
        sfxAudioSource.loop = false;
        sfxAudioSource.playOnAwake = false;
        sfxAudioSource.spatialBlend = 0f; // 2D Sound
    }

    // ================= ระบบเล่นเพลง BGM =================
    public void PlayBGM(AudioClip clip)
    {
        if (bgmAudioSource == null || clip == null) return;

        bgmAudioSource.clip = clip;
        bgmAudioSource.loop = true;
        bgmAudioSource.volume = defaultBGMVolume;
        bgmAudioSource.Play();
    }

    public void StopBGM()
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.Stop();
            bgmAudioSource.clip = null;
        }
    }

    // ================= ระบบเล่นเสียง SFX =================
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxAudioSource == null) return;

        // ใช้ PlayOneShot โดยส่งความดัง SFX ตรงๆ ไม่คูณซ้อน
        sfxAudioSource.PlayOneShot(clip, defaultSFXVolume);
    }

    public void StopSFX()
    {
        if (sfxAudioSource != null)
        {
            sfxAudioSource.Stop();
        }
    }

    // ฟังก์ชันสำเร็จรูปสำหรับให้สคริปต์อื่นเรียกใช้ได้ทันที
    public void PlayDigSFX()
    {
        if (digSFX != null) PlaySFX(digSFX);
    }

    public void PlayButtonClickSFX()
    {
        if (buttonClickSFX != null) PlaySFX(buttonClickSFX);
    }

    public void PlayExplosionSFX()
    {
        if (explosionSFX != null) PlaySFX(explosionSFX);
    }

    public void PlayWalkSFX()
    {
        if (walkSFX != null) PlaySFX(walkSFX);
    }

    // ================= ปรับระดับเสียง =================
    public void SetBGMVolume(float volume)
    {
        defaultBGMVolume = Mathf.Clamp01(volume);
        if (bgmAudioSource != null) bgmAudioSource.volume = defaultBGMVolume;
    }

    public void SetSFXVolume(float volume)
    {
        defaultSFXVolume = Mathf.Clamp01(volume);
        if (sfxAudioSource != null) sfxAudioSource.volume = defaultSFXVolume;
    }
}