using UnityEngine;

namespace CookieRun
{
    [RequireComponent(typeof(CookieObstacle))]
    public class CookieMonsterWalker : MonoBehaviour, ICookiePoolable
    {
        [Header("Movement Settings")]
        [Tooltip("ความเร็วในการเดินเข้าหาผู้เล่น (m/s)")]
        [SerializeField] private float walkSpeed = 2.4f;

        [Tooltip("ระยะห่างจากผู้เล่นที่จะเริ่มเดินเข้าหา (เมตร)")]
        [SerializeField] private float activationDistance = 22f;

        [Tooltip("ระยะห่างขั้นต่ำจากสิ่งกีดขวางก่อนหน้า (เมตร) มอนสเตอร์จะเดินมาไม่เกินจุดนี้ ให้ผู้เล่นมีเวลาเหยียบครั้งต่อไปทัน")]
        [SerializeField] private float minGapFromPrevObstacle = 8f;

        [Header("Animation Reference")]
        [SerializeField] private Animator animator;

        private Transform playerTransform;
        private SpriteRenderer fallbackSr;
        private bool isWalking = false;
        private bool isDefeated = false;
        // ตำแหน่งเดิมใน Chunk (มอนสเตอร์เดินออกจากที่ ต้องคืนตำแหน่งตอน Chunk ถูกใช้ซ้ำ)
        private Vector3 initialLocalPosition;

        private static readonly int SpumMove = Animator.StringToHash("1_Move");
        private static readonly int SpumAttack = Animator.StringToHash("2_Attack");
        private static readonly int SpumDamaged = Animator.StringToHash("3_Damaged");

        private void Awake()
        {
            fallbackSr = GetComponent<SpriteRenderer>();
            initialLocalPosition = transform.localPosition;

            // พยายามโหลดโมเดล 3D/2D SPUM Skeleton จาก Resources
            TryLoadSpumModel();
        }

        private void OnEnable()
        {
            // Animator รีเซ็ตพารามิเตอร์ทุกครั้งที่ถูกปิด-เปิด จึงต้องตั้งท่าเดินใหม่เมื่อถูกนำกลับมาใช้
            if (animator != null && !isDefeated)
            {
                animator.SetBool(SpumMove, true);
            }
        }

        public void OnSpawnFromPool()
        {
            isWalking = false;
            isDefeated = false;
            stopX = float.MinValue;
            transform.localPosition = initialLocalPosition;
        }

        private void TryLoadSpumModel()
        {
            if (transform.Find("SPUM_Model") != null) return;

            GameObject spumPrefab = Resources.Load<GameObject>("Addons/BasicPack/2_Prefab/Skelton/SPUM_20240911222823174");
            if (spumPrefab != null)
            {
                GameObject spumObj = Instantiate(spumPrefab, transform);
                spumObj.name = "SPUM_Model";
                spumObj.transform.localPosition = Vector3.zero;
                spumObj.transform.localScale = new Vector3(1f, 1f, 1f); // หันซ้ายเข้าหาผู้เล่น
                spumObj.transform.localRotation = Quaternion.identity;

                var sg = GetComponent<UnityEngine.Rendering.SortingGroup>();
                if (sg == null) sg = gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
                sg.sortingOrder = 9;

                // เมื่อมีโมเดล SPUM ทำงานแล้ว ให้ปิด SpriteRenderer สำรอง
                if (fallbackSr != null) fallbackSr.enabled = false;

                animator = spumObj.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.SetBool(SpumMove, true);
                }
            }
        }

        private void Start()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null)
            {
                animator.SetBool(SpumMove, true);
            }

            if (CookiePlayerController.Instance != null)
            {
                playerTransform = CookiePlayerController.Instance.transform;
            }
        }

        private void Update()
        {
            if (isDefeated) return;

            if (playerTransform == null)
            {
                if (CookiePlayerController.Instance != null)
                {
                    playerTransform = CookiePlayerController.Instance.transform;
                }
                return;
            }

            float dist = transform.position.x - playerTransform.position.x;

            // เมื่อผู้เล่นเข้าใกล้ในระยะ activationDistance เริ่มเดินเข้าหาผู้เล่น
            if (!isWalking && dist > 0f && dist <= activationDistance)
            {
                isWalking = true;
                stopX = FindStopX();

                // ตั้งค่าแอนิเมชันเดินครั้งเดียวตอนเริ่มเดิน ไม่ต้องเรียกทุกเฟรม
                if (animator != null)
                {
                    animator.SetBool(SpumMove, true);
                }
            }

            if (isWalking)
            {
                // เคลื่อนที่ไปทางซ้าย (เข้าหาผู้เล่นที่วิ่งมาจากทางซ้าย)
                // ไม่เดินเข้าไปใกล้สิ่งกีดขวางก่อนหน้า (เช่น หินย้อยที่ต้องสไลด์) เกินระยะที่กำหนด
                if (transform.position.x > stopX)
                {
                    float x = Mathf.Max(stopX, transform.position.x - walkSpeed * Time.deltaTime);
                    transform.position = new Vector3(x, transform.position.y, transform.position.z);
                }

                // ถ้าใช้สไปรต์สำรอง ทำจังหวะกระตุกเดินน่ารักๆ (Step Bobbing)
                if (fallbackSr != null && fallbackSr.enabled)
                {
                    float bob = Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 0.08f;
                    Vector3 localPos = transform.localPosition;
                    localPos.y = initialLocalPosition.y + bob;
                    transform.localPosition = localPos;
                }
            }
        }

        private float stopX = float.MinValue;

        // หาสิ่งกีดขวางตัวที่อยู่ใกล้ที่สุดระหว่างผู้เล่นกับมอนสเตอร์ แล้วกำหนดจุดหยุดเดินให้ห่างจากมันพอ
        private float FindStopX()
        {
            float playerX = playerTransform.position.x;
            float myX = transform.position.x;
            float nearest = float.MinValue;
            foreach (CookieObstacle o in FindObjectsOfType<CookieObstacle>())
            {
                if (o.gameObject == gameObject) continue;
                float x = o.transform.position.x;
                if (x > playerX && x < myX && x > nearest) nearest = x;
            }
            return nearest == float.MinValue ? float.MinValue : nearest + minGapFromPrevObstacle;
        }

        public void OnAttackPlayer()
        {
            isWalking = false;
            if (animator != null)
            {
                animator.SetBool(SpumMove, false);
                animator.SetTrigger(SpumAttack);
            }
        }

        public void OnDefeated()
        {
            isDefeated = true;
            isWalking = false;
            if (animator != null)
            {
                animator.SetBool(SpumMove, false);
                animator.SetTrigger(SpumDamaged);
            }
        }
    }
}
