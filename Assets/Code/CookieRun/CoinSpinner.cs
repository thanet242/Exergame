using UnityEngine;

namespace CookieRun
{
    /// <summary>
    /// เล่นแอนิเมชันเหรียญหมุน 2D โดยการสลับเฟรม Sprite อัตโนมัติ
    /// ป้องกันปัญหา Animator หลุด / Root Motion เลื่อนผิดตำแหน่ง / Missing Sprite จาก Unity เวอร์ชันเก่า
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CoinSpinner : MonoBehaviour
    {
        [Header("Frames & Speed")]
        [Tooltip("เฟรมรูปเหรียญหมุนทั้งหมด")]
        public Sprite[] frames;

        [Tooltip("ความเร็วในการหมุน (เฟรมต่อวินาที)")]
        public float framesPerSecond = 10f;

        private SpriteRenderer spriteRenderer;
        private int currentFrameIndex = 0;
        private float timer = 0f;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (frames != null && frames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = frames[0];
            }
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0 || spriteRenderer == null) return;

            timer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(1f, framesPerSecond);

            if (timer >= frameDuration)
            {
                timer -= frameDuration;
                currentFrameIndex = (currentFrameIndex + 1) % frames.Length;
                spriteRenderer.sprite = frames[currentFrameIndex];
            }
        }
    }
}
