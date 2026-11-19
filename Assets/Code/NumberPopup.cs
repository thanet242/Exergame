using UnityEngine;

public class NumberPopup : MonoBehaviour
{
    public float moveSpeed = 0.8f;      // ความเร็วในการลอยขึ้นฟ้า
    public float destroyTime = 1.5f;    // เวลา (วินาที) ก่อนที่ตัวเลขจะถูกทำลายทิ้ง

    void Start()
    {
        // สั่งให้ทำลายตัวเองทิ้งอัตโนมัติเมื่อเวลาผ่านไปตามที่ตั้งไว้
        Destroy(gameObject, destroyTime);
    }

    void Update()
    {
        // สั่งให้ตัวเลขขยับตัวลอยขึ้นไปด้านบน (แกน Y) ช้าๆ ในทุกๆ เฟรม
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);
    }
}