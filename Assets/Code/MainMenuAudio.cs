using UnityEngine;

public class MainMenuAudio : MonoBehaviour
{
    [Header("Menu Background Music")]
    [Tooltip("ใส่ไฟล์เพลงที่ต้องการให้เล่นในหน้า MainMenu ที่นี่")]
    [SerializeField] private AudioClip menuBGMClip;

    [Header("Volume Override (Optional)")]
    [Tooltip("ติ๊กถูกถ้าต้องการปรับระดับเสียงเฉพาะหน้าเมนู")]
    [SerializeField] private bool overrideVolume = false;
    [Range(0f, 1f)] [SerializeField] private float menuVolume = 0.5f;

    void Start()
    {
        // ตรวจสอบว่ามี SoundManagerMain อยู่ในเกมหรือไม่
        if (SoundManagerMain.Instance != null)
        {
            // ปรับระดับเสียง (ถ้าเลือก override)
            if (overrideVolume)
            {
                SoundManagerMain.Instance.SetBGMVolume(menuVolume);
            }

            // สั่งเล่นเพลงเมนู
            if (menuBGMClip != null)
            {
                SoundManagerMain.Instance.PlayBGM(menuBGMClip);
            }
            else
            {
                Debug.LogWarning("[MainMenuAudio] ยังไม่ได้ใส่ menuBGMClip ใน Inspector");
            }
        }
        else
        {
            Debug.LogError("[MainMenuAudio] ไม่พบ SoundManagerMain ใน Scene! กรุณาวาง GameObject ที่มี SoundManagerMain ไว้ใน Scene ด้วย");
        }
    }
}