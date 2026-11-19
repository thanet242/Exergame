using UnityEngine;

namespace CookieRun
{
    /// <summary>
    /// ระบบฉากหลังแบบมีมิติ (Parallax Scrolling) เลื่อนช้า-เร็วตามระยะกล้อง
    /// </summary>
    public class CookieParallax : MonoBehaviour
    {
        [Tooltip("สัดส่วนความเร็วในการเลื่อนตามกล้อง (0 = นิ่งสนิท, 0.8 = เลื่อนช้ามาก(ฉากไกล), 0.2 = เลื่อนเร็ว(ฉากใกล้))")]
        [SerializeField] private float parallaxEffect = 0.5f;

        [Tooltip("กล้องหลัก")]
        [SerializeField] private Camera targetCamera;

        private float startPosX;
        private float spriteLength;

        private void Start()
        {
            if (targetCamera == null) targetCamera = Camera.main;

            startPosX = transform.position.x;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                spriteLength = sr.bounds.size.x;
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null) return;

            float cameraX = targetCamera.transform.position.x;
            float temp = cameraX * (1f - parallaxEffect);
            float dist = cameraX * parallaxEffect;

            transform.position = new Vector3(startPosX + dist, transform.position.y, transform.position.z);

            // วนซ้ำภาพพื้นหลังแบบไร้รอยต่อ (Seamless Loop)
            if (spriteLength > 0f)
            {
                if (temp > startPosX + spriteLength)
                {
                    startPosX += spriteLength;
                }
                else if (temp < startPosX - spriteLength)
                {
                    startPosX -= spriteLength;
                }
            }
        }
    }
}
