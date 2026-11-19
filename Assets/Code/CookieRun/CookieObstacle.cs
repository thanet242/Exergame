using UnityEngine;

namespace CookieRun
{
    public enum ObstacleType
    {
        GroundObstacle,   // สิ่งกีดขวางบนพื้น (ต้องกระโดดข้าม)
        OverheadObstacle, // สิ่งกีดขวางด้านบน (ต้องสไลด์หลบ)
        SmashableOnly,    // สิ่งกีดขวางที่ทำลายได้ด้วย Giant/Blast เท่านั้น
        Monster           // มอนสเตอร์เดินเข้าหาผู้เล่น (สไลด์เตะล้ม หรือกระโดดข้าม)
    }

    [RequireComponent(typeof(Collider2D))]
    public class CookieObstacle : MonoBehaviour, ICookiePoolable
    {
        [Header("Obstacle Settings")]
        [Tooltip("ประเภทของสิ่งกีดขวาง")]
        [SerializeField] private ObstacleType obstacleType = ObstacleType.GroundObstacle;

        [Tooltip("ดาเมจที่ทำต่อพลังชีวิต (Energy) ของตัวละคร")]
        [SerializeField] private float damage = 15f;

        [Header("Effects & Audio")]
        [SerializeField] private GameObject smashEffectPrefab;
        [SerializeField] private AudioClip smashSound;

        private bool hasCollided = false;
        private Collider2D obstacleCollider;

        private void Awake()
        {
            // CookieSpriteHitbox อาจตั้ง Hitbox ให้ก่อนแล้ว (ขึ้นกับลำดับ Awake)
            if (obstacleCollider == null) obstacleCollider = GetComponent<Collider2D>();
        }

        /// <summary>
        /// กำหนด Collider ที่ใช้เป็น Hitbox จริง (เรียกโดย CookieSpriteHitbox)
        /// </summary>
        public void SetHitbox(Collider2D hitbox)
        {
            obstacleCollider = hitbox;
        }

        /// <summary>
        /// สิ่งกีดขวางทำอันตรายได้หรือไม่ (เช่น กับดักหนามที่หดลงพื้นจะไม่ทำอันตราย)
        /// </summary>
        public bool IsArmed { get; set; } = true;

        /// <summary>ชนิดของสิ่งกีดขวาง (ใช้ตรวจว่ามีสิ่งกีดขวางด้านบนตอนกำลังสไลด์)</summary>
        public ObstacleType Kind => obstacleType;

        // นับสิ่งกีดขวางที่ผู้เล่นผ่านพ้นได้ (ไม่นับถ้าชนจนเสียโล่/ตาย)
        private bool countedPass;
        private bool hitPlayer;

        private void Update()
        {
            if (countedPass) return;
            CookiePlayerController player = CookiePlayerController.Instance;
            if (player == null || player.transform.position.x < transform.position.x + 0.6f) return;

            countedPass = true;
            if (!hitPlayer && CookieGameManager.Instance != null) CookieGameManager.Instance.RecordObstacleCleared();
        }

        public void OnSpawnFromPool()
        {
            hasCollided = false;
            countedPass = false;
            hitPlayer = false;
            IsArmed = true;
            if (obstacleCollider != null) obstacleCollider.enabled = true;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            CookiePlayerController player = collision.GetComponentInParent<CookiePlayerController>();
            if (player != null)
            {
                CheckAndDamagePlayer(player);
            }
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            // ชนไปแล้ว ไม่ต้องค้นหา Component ซ้ำทุก Physics Step
            if (hasCollided) return;

            CookiePlayerController player = collision.GetComponentInParent<CookiePlayerController>();
            if (player != null)
            {
                CheckAndDamagePlayer(player);
            }
        }

        private void CheckAndDamagePlayer(CookiePlayerController player)
        {
            if (hasCollided) return;

            // 1. ถ้าผู้เล่นอยู่ในสถานะ Giant (ยักษ์) หรือ Blast (วิ่งทะลวง) -> ทำลายสิ่งกีดขวางทันที
            if (player.IsGiant || player.IsBlast)
            {
                Smash(player);
                return;
            }

            // กับดักที่ยังไม่ทำงาน (เช่น หนามหดอยู่ใต้พื้น) วิ่งผ่านได้
            if (!IsArmed) return;

            // 2. ถ้าเป็น Overhead Obstacle และผู้เล่นกำลังสไลด์อยู่ -> รอด! ไม่โดนดาเมจ
            if (obstacleType == ObstacleType.OverheadObstacle && player.IsSliding)
            {
                return;
            }

            // 3. ถ้าเป็น Monster และผู้เล่นกำลังสไลด์อยู่ -> สไลด์เตะมอนสเตอร์ล้มกระเด็นทันที! (Slide-Tackle)
            if (obstacleType == ObstacleType.Monster && player.IsSliding)
            {
                Smash(player);
                return;
            }

            // 4. ถ้าเป็น Monster และไม่ได้สไลด์ (วิ่งชนหรือโดดชน) -> มอนสเตอร์ตีใส่ เสียหัวใจ 1 ดวง
            if (obstacleType == ObstacleType.Monster)
            {
                if (player.IsInvincible) return;

                hasCollided = true;
                hitPlayer = true;

                // สั่งให้มอนสเตอร์เล่นแอนิเมชันโจมตีใส่ผู้เล่น
                CookieMonsterWalker walker = GetComponent<CookieMonsterWalker>();
                if (walker != null)
                {
                    walker.OnAttackPlayer();
                }

                player.HitObstacle(damage);
                return;
            }

            // 5. ถ้าผู้เล่นไม่ได้อมตะ -> โดนความเสียหาย
            if (!player.IsInvincible)
            {
                hasCollided = true;
                hitPlayer = true;
                player.HitObstacle(damage);
            }
        }

        /// <summary>
        /// ทำลายสิ่งกีดขวางเมื่อโดนชนด้วยสถานะพิเศษ หรือสไลด์เตะมอนสเตอร์
        /// </summary>
        public void Smash(CookiePlayerController player)
        {
            if (hasCollided) return;
            hasCollided = true;

            // ปิด Collider ทันทีเพื่อป้องกันการตรวจจับซ้ำในเฟรมเดียวกัน
            if (obstacleCollider != null) obstacleCollider.enabled = false;

            // ตรวจสอบเอฟเฟกต์การระเบิด / แตกกระจายอย่างปลอดภัย ป้องกัน InvalidCastException
            if (smashEffectPrefab != null)
            {
                try
                {
                    CookiePool.SpawnEffect(smashEffectPrefab, transform.position);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[CookieObstacle] Smash effect warning: {ex.Message}");
                }
            }

            // ชนด้วยยักษ์/วิ่งทะลวง: สิ่งกีดขวางแตกกระจาย (มอนสเตอร์ประกอบจากหลายชิ้น ใช้แค่ฝุ่นและแฟลช)
            if (player != null && (player.IsGiant || player.IsBlast))
            {
                CookieSmashFx.Play(gameObject, player.IsGiant, obstacleType != ObstacleType.Monster);
            }

            if (smashSound != null)
            {
                AudioSource.PlayClipAtPoint(smashSound, transform.position);
            }
            else
            {
                CookieAudio.Play(obstacleType == ObstacleType.Monster ? CookieSfx.MonsterKick : CookieSfx.Smash);
            }

            // ทุบทำลายได้ = ผ่านพ้นสิ่งกีดขวางนี้
            if (!countedPass)
            {
                countedPass = true;
                if (CookieGameManager.Instance != null) CookieGameManager.Instance.RecordObstacleCleared();
            }

            CookiePool.Release(gameObject);
        }
    }
}
