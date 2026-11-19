using UnityEngine;

namespace CookieRun
{
    public enum ItemType
    {
        Jelly,          // เยลลี่คะแนน
        Coin,           // เหรียญเงิน / ทอง
        HealthPotion,   // ขวดฟื้นฟูเลือด / Energy (legacy)
        Magnet,         // แม่เหล็กดูดเหรียญ
        Giant,          // ตัวยักษ์ วิ่งชนสิ่งกีดขวางพัง
        Blast,          // สปีดบูสต์ พุ่งชนเร็ว อมตะ
        Shield          // โล่ป้องกันการชนหิน 1 ครั้ง
    }

    [RequireComponent(typeof(Collider2D))]
    public class CookieItem : MonoBehaviour, ICookiePoolable
    {
        [Header("Item Config")]
        [SerializeField] private ItemType itemType = ItemType.Jelly;
        [SerializeField] private int scoreValue = 100;
        [SerializeField] private int coinValue = 1;
        [SerializeField] private float buffDuration = 5f;

        [Header("Magnet Behavior")]
        [SerializeField] private bool canBeMagnetized = true;
        [SerializeField] private float magnetFlySpeed = 16f;

        [Header("Effects & Sound")]
        [SerializeField] private GameObject pickupEffectPrefab;
        [SerializeField] private AudioClip pickupSound;

        private bool isCollected = false;
        private Transform targetPlayerTransform;

        // ตำแหน่งเดิมใน Chunk (แม่เหล็กจะดึงไอเทมออกจากที่ ต้องคืนตำแหน่งตอน Chunk ถูกใช้ซ้ำ)
        private Vector3 initialLocalPosition;
        private bool hasInitialPose = false;

        private void Awake()
        {
            initialLocalPosition = transform.localPosition;
            hasInitialPose = true;
        }

        public void OnSpawnFromPool()
        {
            isCollected = false;
            targetPlayerTransform = null;

            // ถ้าไอเทมเป็นตัวหลักของ Pool เอง (เช่น เหรียญเริ่มเกม) Pool จะจัดตำแหน่งและเปิดใช้งานให้
            if (GetComponent<CookiePooledObject>() != null) return;

            if (hasInitialPose) transform.localPosition = initialLocalPosition;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }

        private void Update()
        {
            if (isCollected) return;

            // ตรวจสอบระบบแม่เหล็ก (Magnet)
            if (canBeMagnetized && (itemType == ItemType.Jelly || itemType == ItemType.Coin))
            {
                if (targetPlayerTransform == null)
                {
                    CookiePlayerController player = CookiePlayerController.Instance;
                    if (player != null && player.IsMagnetActive)
                    {
                        // ใช้ระยะยกกำลังสอง เลี่ยงการถอดรากที่สองของไอเทมทุกชิ้นทุกเฟรม
                        float radius = player.MagnetRadius;
                        Vector2 offset = player.transform.position - transform.position;
                        if (offset.sqrMagnitude <= radius * radius)
                        {
                            targetPlayerTransform = player.transform;
                        }
                    }
                }
                else
                {
                    // ลอยพุ่งเข้าหาผู้เล่น
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        targetPlayerTransform.position,
                        magnetFlySpeed * Time.deltaTime
                    );
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isCollected) return;

            CookiePlayerController player = collision.GetComponentInParent<CookiePlayerController>();
            if (player == null) return;

            CookieHealth health = player.GetComponent<CookieHealth>();

            Collect(player, health);
        }

        private static CookieSfx PickupSfx(ItemType type)
        {
            switch (type)
            {
                case ItemType.Coin:
                case ItemType.Jelly: return CookieSfx.Coin;
                case ItemType.Shield: return CookieSfx.ShieldGet;
                case ItemType.Blast: return CookieSfx.Blast;
                default: return CookieSfx.PowerUp;
            }
        }

        private void Collect(CookiePlayerController player, CookieHealth health)
        {
            isCollected = true;

            // ประมวลผลตามประเภทไอเทม
            switch (itemType)
            {
                case ItemType.Jelly:
                    // คะแนนจะได้รับเฉพาะจากการเก็บเหรียญ (Coin) เท่านั้น
                    break;

                case ItemType.Coin:
                    if (CookieGameManager.Instance != null)
                    {
                        CookieGameManager.Instance.AddScore(scoreValue);
                        CookieGameManager.Instance.AddCoins(coinValue);
                    }
                    break;

                case ItemType.HealthPotion:
                    // คืนหัวใจ 1 ดวง ถ้าหัวใจเต็มอยู่แล้วได้คะแนนโบนัสแทน
                    if ((health == null || !health.Heal()) && CookieGameManager.Instance != null)
                        CookieGameManager.Instance.AddScore(scoreValue * 10);
                    break;

                case ItemType.Magnet:
                    player.ActivateMagnet(buffDuration);
                    break;

                case ItemType.Giant:
                    player.ActivateGiant(buffDuration);
                    break;

                case ItemType.Blast:
                    player.ActivateBlast(buffDuration);
                    break;

                case ItemType.Shield:
                    player.ActivateShield();
                    break;
            }

            // แสดงเอฟเฟกต์และเล่นเสียง
            if (pickupEffectPrefab != null)
            {
                CookiePool.SpawnEffect(pickupEffectPrefab, transform.position);
            }

            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }
            else
            {
                CookieAudio.Play(PickupSfx(itemType), itemType == ItemType.Coin || itemType == ItemType.Jelly ? 0.6f : 1f);
            }

            CookiePool.Release(gameObject);
        }
    }
}
