using UnityEngine;

public class EndlessBackground : MonoBehaviour
{
    [Header("Speed Settings")]
    [Tooltip("ความเร็วในการเลื่อนฉากไปทางซ้าย")]
    public float scrollSpeed = 5f;

    [Header("Sprite References")]
    [Tooltip("ลาก GameObject แผ่นที่ 1 มาใส่")]
    public Transform backgroundA;
    [Tooltip("ลาก GameObject แผ่นที่ 2 มาใส่")]
    public Transform backgroundB;

    private float spriteWidth;

    void Start()
    {
        // คำนวณความกว้างของภาพอัตโนมัติจาก SpriteRenderer
        if (backgroundA != null)
        {
            SpriteRenderer sr = backgroundA.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                spriteWidth = sr.bounds.size.x;
            }
            else
            {
                Debug.LogError("[EndlessBackground] ไม่พบ SpriteRenderer บน Background A");
                return;
            }
        }

        // จัดวางตำแหน่งเริ่มต้นให้อยู่ต่อกันพอดี
        if (backgroundA != null && backgroundB != null)
        {
            backgroundA.position = new Vector3(0f, backgroundA.position.y, backgroundA.position.z);
            backgroundB.position = new Vector3(spriteWidth, backgroundB.position.y, backgroundB.position.z);
        }
    }

    void Update()
    {
        if (backgroundA == null || backgroundB == null) return;

        // 1. เลื่อนทั้ง 2 แผ่นไปทางซ้ายพร้อมกัน
        float moveDistance = scrollSpeed * Time.deltaTime;
        backgroundA.position += Vector3.left * moveDistance;
        backgroundB.position += Vector3.left * moveDistance;

        // 2. ตรวจสอบแผ่น A: ถ้าหลุดขอบซ้าย ให้ย้ายไปต่อท้ายแผ่น B ทางขวา
        if (backgroundA.position.x <= -spriteWidth)
        {
            backgroundA.position = new Vector3(backgroundB.position.x + spriteWidth, backgroundA.position.y, backgroundA.position.z);
        }

        // 3. ตรวจสอบแผ่น B: ถ้าหลุดขอบซ้าย ให้ย้ายไปต่อท้ายแผ่น A ทางขวา
        if (backgroundB.position.x <= -spriteWidth)
        {
            backgroundB.position = new Vector3(backgroundA.position.x + spriteWidth, backgroundB.position.y, backgroundB.position.z);
        }
    }

    // ฟังก์ชันสำหรับปรับความเร็วจากสคริปต์อื่น (เช่น หยุดตอนเจอหิน หรือเร่งสปีด)
    public void SetSpeed(float newSpeed)
    {
        scrollSpeed = newSpeed;
    }
}