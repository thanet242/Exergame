using UnityEngine;

namespace CookieRun
{
    /// <summary>
    /// ควบคุมกล้องให้ติดตามตัวละครวิ่ง 2D สไตล์ Cookie Run
    /// - เลื่อนนำหน้าตัวละครไปทางขวาเล็กน้อย (Look Ahead) เพื่อให้มองเห็นอุปสรรคข้างหน้า
    /// - ล็อคหรือผ่อนแกน Y นุ่มนวล ไม่ให้กล้องกระตุกตามตอนกระโดด
    /// </summary>
    public class CookieCameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Offset & Ahead")]
        [Tooltip("ระยะห่างของกล้องจากตัวละคร (X เลื่อนไปข้างหน้าเพื่อให้เห็นสิ่งกีดขวางก่อน)")]
        [SerializeField] private Vector3 offset = new Vector3(3.8f, 1.0f, -10f);

        [Header("Damping & Smoothing")]
        [SerializeField] private float smoothSpeedX = 0.08f;
        [SerializeField] private float smoothSpeedY = 0.2f;

        [Header("Y Axis Control")]
        [Tooltip("ล็อคแกน Y ให้อยู่ระดับคงที่ ไม่เคลื่อนตามตอนกระโดด")]
        [SerializeField] private bool lockY = true;
        [SerializeField] private float fixedY = 1.35f;
        [SerializeField] private float minY = 0f;

        private float currentVelocityX;
        private float currentVelocityY;

        private void Start()
        {
            if (target == null)
            {
                CookiePlayerController player = FindObjectOfType<CookiePlayerController>();
                if (player != null) target = player.transform;
            }

            if (target != null)
            {
                float targetY = lockY ? fixedY : Mathf.Max(target.position.y + offset.y, minY);
                transform.position = new Vector3(target.position.x + offset.x, targetY, offset.z);
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float newX = Mathf.SmoothDamp(transform.position.x, target.position.x + offset.x, ref currentVelocityX, smoothSpeedX);
            float targetY = lockY ? fixedY : Mathf.Max(target.position.y + offset.y, minY);
            float newY = Mathf.SmoothDamp(transform.position.y, targetY, ref currentVelocityY, smoothSpeedY);

            transform.position = new Vector3(newX, newY, offset.z);
        }
    }
}
