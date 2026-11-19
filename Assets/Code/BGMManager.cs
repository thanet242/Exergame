using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [Header("Default Music")]
    [SerializeField] private AudioClip defaultBGM;

    [Header("Settings")]
    [Range(0f, 1f)] [SerializeField] private float volume = 1f;

    private AudioSource audioSource;

    void Awake()
    {
        // ยิง Error สีแดงทันทีเพื่อทดสอบว่าสคริปต์ถูกโหลดเข้าเกมจริงหรือไม่
        Debug.LogError(">>> ทดสอบ: สคริปต์ BGMManager เริ่มทำงานแล้ว! <<<");

        // ระบบ Singleton แบบอยู่ข้ามฉาก
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // ดึง AudioSource และตั้งค่าเริ่มต้นให้เป็น 2D Sound ทันที
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f; // 2D ได้ยินชัดเจนทั้งจอ
        audioSource.volume = volume;
    }

    void Start()
    {
        // ถ้ามีเพลงตั้งไว้ล่วงหน้า ให้เล่นทันทีเมื่อเริ่มเกม
        if (defaultBGM != null)
        {
            PlayBGM(defaultBGM);
        }
        else
        {
            Debug.LogError(">>> [BGMManager] ไม่มีไฟล์เพลงในช่อง Default BGM! <<<");
        }
    }

    /// <summary>
    /// สั่งเล่นเพลง (ถ้าเป็นเพลงเดิมที่กำลังเล่นอยู่ จะไม่เริ่มเล่นใหม่)
    /// </summary>
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogError(">>> [BGMManager] ไม่มีไฟล์เพลงที่ส่งเข้ามาเล่น! <<<");
            return;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // ถ้าเพลงเดิมเล่นอยู่แล้ว ไม่ต้องเริ่มใหม่
        if (audioSource.clip == clip && audioSource.isPlaying)
        {
            return;
        }

        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
        Debug.LogError($">>> [BGMManager] กำลังสั่งเล่นเพลง: {clip.name} <<<");
    }

    /// <summary>
    /// หยุดเล่นเพลง
    /// </summary>
    public void StopBGM()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    /// <summary>
    /// ปรับระดับเสียง (0.0 ถึง 1.0)
    /// </summary>
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }
}