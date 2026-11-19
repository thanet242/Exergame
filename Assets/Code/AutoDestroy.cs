using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    [Tooltip("ระยะเวลาเป็นวินาทีก่อนลบ GameObject ทิ้ง (ให้เท่ากับความยาวของเอฟเฟคหรืออนิเมชัน)")]
    public float lifetime = 1.0f; 

    void Start()
    {
        // สั่งทำลายวัตถุตนเองทิ้งหลังจากเวลาผ่านไปตามค่า lifetime
        Destroy(gameObject, lifetime);
    }
}