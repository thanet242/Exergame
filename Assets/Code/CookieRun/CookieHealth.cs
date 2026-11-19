using System;
using UnityEngine;

namespace CookieRun
{
    /// <summary>
    /// ระบบพลังชีวิตแบบหัวใจ (Heart) สำหรับโหมดวิ่ง
    /// - ชนสิ่งกีดขวาง/มอนสเตอร์ เสียหัวใจ 1 ดวงต่อครั้ง (ไม่ขึ้นกับค่าดาเมจ)
    /// - เก็บหัวใจ (Potion) ฟื้นฟู 1 ดวง
    /// - หัวใจหมด หรือตกหลุม = Game Over
    /// </summary>
    public class CookieHealth : MonoBehaviour
    {
        [Header("Heart Settings")]
        [Tooltip("จำนวนหัวใจสูงสุด")]
        [SerializeField] private int maxHearts = 3;

        [Tooltip("อัตราการลดของหัวใจต่อวินาที (Time Decay - ตั้งเป็น 0 หากไม่ใช้ระบบลดตามเวลา)")]
        [SerializeField] private float decayRatePerSecond = 0f;

        [Header("Status")]
        [SerializeField] private float currentHealth;
        [SerializeField] private bool isImmune = false;
        private bool isDead = false;

        // Events สำหรับส่งข้อมูลให้ UI หรือระบบอื่น (หน่วยเป็นจำนวนหัวใจ)
        public event Action<float, float> OnHealthChanged; // (current, max)
        public event Action OnDeath;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHearts;
        public int Hearts => Mathf.Max(0, Mathf.CeilToInt(currentHealth - 0.001f));
        public int MaxHearts => maxHearts;
        public bool IsDead => isDead;
        public bool IsImmune { get => isImmune; set => isImmune = value; }

        private void Awake()
        {
            currentHealth = maxHearts;
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHearts);
        }

        private void Update()
        {
            // ถ้าเกมยังไม่เริ่ม หรือจบเกมแล้ว หรือเลือดหมด จะไม่ลดเลือด
            if (isDead) return;
            if (CookieGameManager.Instance != null && !CookieGameManager.Instance.IsPlaying) return;

            if (decayRatePerSecond > 0f)
            {
                currentHealth -= decayRatePerSecond * Time.deltaTime;
                currentHealth = Mathf.Max(0f, currentHealth);
                OnHealthChanged?.Invoke(currentHealth, maxHearts);

                if (currentHealth <= 0f)
                {
                    Die();
                }
            }
        }

        /// <summary>
        /// เสียหัวใจ 1 ดวง
        /// </summary>
        public void LoseHeart()
        {
            if (isDead || isImmune) return;

            currentHealth = Mathf.Max(0f, Hearts - 1);
            OnHealthChanged?.Invoke(currentHealth, maxHearts);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        /// <summary>
        /// โดนความเสียหาย: ระบบหัวใจนับเป็น 1 ดวงต่อครั้งเสมอ
        /// </summary>
        public void TakeDamage(float damage)
        {
            LoseHeart();
        }

        /// <summary>
        /// ฟื้นฟูหัวใจ 1 ดวง (เก็บ Potion) คืนค่า false ถ้าหัวใจเต็มอยู่แล้ว
        /// </summary>
        public bool Heal(float amount = 1f)
        {
            if (isDead || Hearts >= maxHearts) return false;

            currentHealth = Mathf.Min(Hearts + 1, maxHearts);
            OnHealthChanged?.Invoke(currentHealth, maxHearts);
            return true;
        }

        /// <summary>
        /// จัดการเมื่อตัวละครพลังชีวิตหมด
        /// </summary>
        public void Die()
        {
            if (isDead) return;

            isDead = true;
            currentHealth = 0f;
            OnHealthChanged?.Invoke(currentHealth, maxHearts);
            OnDeath?.Invoke();

            if (CookieGameManager.Instance != null)
            {
                CookieGameManager.Instance.TriggerGameOver();
            }
        }

        /// <summary>
        /// รีเซ็ตพลังชีวิตเมื่อเริ่มรอบใหม่
        /// </summary>
        public void ResetHealth()
        {
            isDead = false;
            currentHealth = maxHearts;
            OnHealthChanged?.Invoke(currentHealth, maxHearts);
        }
    }
}
