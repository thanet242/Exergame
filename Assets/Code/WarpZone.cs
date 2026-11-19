using UnityEngine;
using UnityEngine.SceneManagement;

public class WarpZone : MonoBehaviour
{
    [Header("Target Scene")]
    [Tooltip("ชื่อฉากที่ต้องการให้วาร์ปไป")]
    public string targetSceneName = "Cave1";

    [Header("Tutorial & Lock Settings")]
    [Tooltip("สถานะปลดล็อก (จะถูกเปิดใช้งานเมื่อฝึกสอนจบ)")]
    public bool isUnlocked = false;

    [Header("Navigation Arrow Reference")]
    [Tooltip("ลาก GameObject ObjectiveArrow มาใส่ช่องนี้")]
    public ObjectiveArrow navigationArrow;

    private Collider2D warpCollider;

    void Awake()
    {
        warpCollider = GetComponent<Collider2D>();

        // 1. ถ้ายังไม่ปลดล็อก ให้ปิด Collider ไว้ก่อนเพื่อไม่ให้ผู้เล่นเหยียบวาร์ปได้
        if (warpCollider != null)
        {
            warpCollider.enabled = isUnlocked;
        }

        // 2. ซ่อนลูกศรนำทางไว้ก่อนตั้งแต่เริ่มเกม
        if (navigationArrow != null && !isUnlocked)
        {
            navigationArrow.gameObject.SetActive(false);
        }
    }

    // ฟังก์ชันนี้จะถูกเรียกเมื่อฝึกสอนเสร็จสิ้น (Tutorial Complete)
    public void UnlockWarp()
    {
        isUnlocked = true;

        // เปิด Collider ให้ใช้งานแท่นวาร์ปได้
        if (warpCollider != null)
        {
            warpCollider.enabled = true;
        }

        // เปิดแสดงลูกศร และสั่งให้ชี้ตรงมาที่ WarpPoint นี้
        if (navigationArrow != null)
        {
            navigationArrow.gameObject.SetActive(true);
            navigationArrow.SetTarget(this.transform);
        }

        Debug.Log("<color=green>[WarpZone]</color> ปลดล็อกจุดวาร์ปและเปิดลูกศรนำทางเรียบร้อยแล้ว!");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isUnlocked) return;

        if (collision.CompareTag("Player") || collision.GetComponentInParent<PlayerController>() != null)
        {
            Time.timeScale = 1f;

            if (!string.IsNullOrEmpty(targetSceneName))
            {
                // ตรวจสอบว่ามีระบบ ScreenSlideTransition หรือไม่
                if (ScreenSlideTransition.Instance != null)
                {
                    // เลื่อนสไลด์หน้าจอไปทางซ้ายอย่างรวดเร็วแล้วโหลดฉากใหม่
                    ScreenSlideTransition.Instance.LoadSceneWithSlide(targetSceneName);
                }
                else
                {
                    // โหลดฉากแบบปกติสำรอง (กรณีไม่ได้ใส่ TransitionCanvas ไว้ในฉาก)
                    SceneManager.LoadScene(targetSceneName);
                }
            }
        }
    }
}