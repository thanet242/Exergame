using UnityEngine;

namespace CookieRun
{
    /// <summary>
    /// กับดักหนามที่ผุดขึ้น-หดลงจากพื้นเป็นจังหวะ
    /// - หดอยู่ใต้พื้น: วิ่งผ่านได้ปลอดภัย
    /// - ก่อนผุดขึ้นจะโผล่ปลายหนามพร้อมกะพริบแดงเตือน
    /// - ผุดขึ้นเต็มที่: ต้องกระโดดข้าม
    /// วางหลายอันติดกันโดยตั้ง phaseOffset ต่างกัน จะได้แนวหนามที่ต้องกะจังหวะ
    /// (หนามต้องมี Sorting Order ต่ำกว่าพื้น เพื่อให้ซ่อนอยู่หลังพื้นตอนหดลง)
    /// </summary>
    [RequireComponent(typeof(CookieObstacle))]
    public class CookieSpikeTrap : MonoBehaviour, ICookiePoolable
    {
        [Header("Rhythm (วินาที)")]
        [SerializeField] private float hiddenTime = 1.0f;
        [SerializeField] private float warningTime = 0.4f;
        [SerializeField] private float raisedTime = 0.9f;
        [Tooltip("เลื่อนจังหวะ (0-1 ของรอบ) ให้หนามแต่ละอันขึ้นไม่พร้อมกัน")]
        [Range(0f, 1f)]
        [SerializeField] private float phaseOffset = 0f;

        [Header("Motion")]
        [Tooltip("ระยะที่หนามจมลงใต้พื้นตอนหด (ยูนิต)")]
        [SerializeField] private float sinkDepth = 0.42f;
        [Tooltip("ระยะที่ปลายหนามโผล่ตอนเตือน")]
        [SerializeField] private float warningPeek = 0.1f;
        [Tooltip("เวลาผุดขึ้น/หดลง (วินาที)")]
        [SerializeField] private float moveTime = 0.08f;

        [Header("Warning")]
        [SerializeField] private Color warningColor = new Color(1f, 0.35f, 0.3f, 1f);
        [Tooltip("ต้องต่ำกว่าพื้น (พื้นใช้ 1) หนามจึงซ่อนหลังพื้นได้ตอนหดลง")]
        [SerializeField] private int sortingOrder = 0;

        private CookieObstacle obstacle;
        private SpriteRenderer spriteRenderer;
        private Color baseColor;
        private Vector3 raisedLocalPosition;
        private float currentRaise; // 0 = หดสุด, 1 = ขึ้นสุด

        private void Awake()
        {
            obstacle = GetComponent<CookieObstacle>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            baseColor = spriteRenderer.color;
            spriteRenderer.sortingOrder = sortingOrder;
            raisedLocalPosition = transform.localPosition;
        }

        public void OnSpawnFromPool()
        {
            currentRaise = 0f;
            transform.localPosition = raisedLocalPosition;
        }

        private void Update()
        {
            float cycle = hiddenTime + warningTime + raisedTime;
            float t = Mathf.Repeat(Time.time + phaseOffset * cycle, cycle);

            float targetRaise;
            bool warning = false;
            if (t < hiddenTime)
            {
                targetRaise = 0f;
            }
            else if (t < hiddenTime + warningTime)
            {
                targetRaise = warningPeek / sinkDepth;
                warning = true;
            }
            else
            {
                targetRaise = 1f;
            }

            // เสียงหนามผุด เฉพาะกับดักที่อยู่ใกล้ผู้เล่น (ไม่ให้หนามทั้งด่านดังพร้อมกัน)
            if (targetRaise >= 1f && currentRaise < 0.5f)
            {
                CookiePlayerController player = CookiePlayerController.Instance;
                if (player != null && Mathf.Abs(transform.position.x - player.transform.position.x) < 12f)
                {
                    CookieAudio.Play(CookieSfx.SpikePop, 0.5f);
                }
            }

            currentRaise = Mathf.MoveTowards(currentRaise, targetRaise, Time.deltaTime / Mathf.Max(0.01f, moveTime));
            transform.localPosition = raisedLocalPosition + new Vector3(0f, (currentRaise - 1f) * sinkDepth, 0f);

            // อันตรายเมื่อโผล่พ้นพื้นเกินครึ่ง
            obstacle.IsArmed = currentRaise > 0.5f;

            if (warning)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 30f);
                spriteRenderer.color = Color.Lerp(baseColor, warningColor, pulse);
            }
            else
            {
                spriteRenderer.color = baseColor;
            }
        }
    }
}
