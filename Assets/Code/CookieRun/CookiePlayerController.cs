using System.Collections;
using UnityEngine;

namespace CookieRun
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(CookieHealth))]
    public class CookiePlayerController : MonoBehaviour
    {
        [Header("Running Movement")]
        [Tooltip("ความเร็วในการวิ่งพื้นฐาน")]
        [SerializeField] private float baseRunSpeed = 7f;
        [Tooltip("ตัวคูณความเร็ว (ใช้ตอนเก็บไอเทม Blast / Speed Up)")]
        [SerializeField] private float speedMultiplier = 1f;

        [Header("Jumping Settings")]
        [Tooltip("แรงกระโดดครั้งแรก (11 = สูงราว 1.75 ม. ข้ามหลุมกว้าง 3.2 ม. ได้ที่ความเร็ว 7)")]
        [SerializeField] private float jumpForce = 11f;
        [Tooltip("แรงกระโดดครั้งที่สอง (Double Jump) รวมสองจังหวะสูงราว 3.3 ม. พอข้ามเสาหินสูง")]
        [SerializeField] private float doubleJumpForce = 10.5f;
        [Tooltip("จำนวนครั้งที่กระโดดได้")]
        [SerializeField] private int maxJumps = 2;
        [Tooltip("Coyote Time: เวลาผ่อนผันหลังตกจากขอบยังกดโดดได้ (วินาที)")]
        [SerializeField] private float coyoteTime = 0.12f;
        [Tooltip("Jump Buffer Time: จำการกดกระโดดล่วงหน้าก่อนถึงพื้น (วินาที) แผ่นเหยียบไม่มีราวจับ ควรเผื่อไว้นาน")]
        [SerializeField] private float jumpBufferTime = 0.3f;
        [Tooltip("ร่วงลงมาต่ำกว่าความสูงนี้ (เมตรจากพื้น) แล้วกดกระโดด = จำไว้กระโดดตอนแตะพื้น แทน Double Jump")]
        [SerializeField] private float landingBufferHeight = 0.9f;
        [Tooltip("ปิดไว้ = กระโดดสูงเท่ากันทุกครั้ง ไม่ว่าจะเหยียบแผ่นค้างหรือแตะสั้นๆ (เหมาะกับแผ่นเหยียบ)")]
        [SerializeField] private bool variableJumpHeight = false;
        [Tooltip("ตัดความสูงการกระโดดเมื่อปล่อยปุ่ม (ใช้เฉพาะเมื่อเปิด Variable Jump Height)")]
        [SerializeField] private float jumpCutMultiplier = 0.5f;
        private int lastJumpInputFrame = -1;

        [Header("Snappy Gravity & Fall")]
        [Tooltip("แรงโน้มถ่วงพื้นฐาน (3.5 ช่วยให้ไม่ลอยช้าแบบอวกาศ)")]
        [SerializeField] private float baseGravityScale = 3.5f;
        [Tooltip("ตัวคูณแรงโน้มถ่วงตอนตก (ช่วยให้ร่วงลงพื้นเร็วขึ้น กระชับฉับไว)")]
        [SerializeField] private float fallGravityMultiplier = 1.4f;
        [Tooltip("จำกัดความเร็วสูงสุดในการตก")]
        [SerializeField] private float maxFallSpeed = -20f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.15f);
        [SerializeField] private LayerMask groundLayer;

        [Header("Slide Settings (ปรับขนาด Collider)")]
        [Tooltip("ขนาดของ Collider ขณะยืน/วิ่งปกติ")]
        [SerializeField] private Vector2 standColliderSize = new Vector2(0.8f, 1.6f);
        [Tooltip("Offset ของ Collider ขณะยืน/วิ่งปกติ")]
        [SerializeField] private Vector2 standColliderOffset = new Vector2(0f, 0f);
        [Tooltip("ขนาดของ Collider ขณะสไลด์")]
        [SerializeField] private Vector2 slideColliderSize = new Vector2(1.2f, 0.7f);
        [Tooltip("Offset ของ Collider ขณะสไลด์")]
        [SerializeField] private Vector2 slideColliderOffset = new Vector2(0f, -0.45f);

        [Header("Slide Juice & Dynamic Pose")]
        [Tooltip("ตัวคูณความเร็วสไลด์ชั่วขณะ (Cookie Run Slide Boost)")]
        [SerializeField] private float slideBoostMultiplier = 1.15f;
        [Tooltip("ระยะเวลาบูสต์สไลด์ (วินาที)")]
        [SerializeField] private float slideBoostDuration = 0.22f;
        private float slideBoostTimer = 0f;

        [Tooltip("มุมเอนหลังของโมเดลขณะสไลด์ (องศา, บวก = เอนไปด้านหลัง ท่าไถลเท้านำ)")]
        [SerializeField] private float slideTiltAngle = 68f;
        [Tooltip("สเกลโมเดลขณะสไลด์ (สัดส่วนเท่าเดิม ไม่บีบตัวละคร)")]
        [SerializeField] private Vector3 slideModelScale = new Vector3(-0.95f, 0.95f, 1f);
        [Tooltip("ตำแหน่งโมเดลขณะสไลด์ (เลื่อนไปข้างหน้าให้ตัวที่นอนอยู่ตรงกับ Hitbox)")]
        [SerializeField] private Vector3 slideModelOffset = new Vector3(0.55f, 0.02f, 0f);
        [Tooltip("กดสไลด์ตอนลอยอยู่ = ดิ่งลงพื้นเร็วด้วยความเร็วนี้ แล้วสไลด์ทันทีที่แตะพื้น")]
        [SerializeField] private float fastFallSpeed = 14f;

        // ปุ่มกระโดด/สไลด์ ตั้งค่าได้จากหน้า Option ในเมนูหลัก (ดู GameInputBindings)

        [Header("Invincibility / Hurt")]
        [Tooltip("ระยะเวลาอมตะชั่วขณะหลังโดนชน (วินาที)")]
        [SerializeField] private float hurtInvincibleDuration = 1.5f;
        [Tooltip("ความเร็วในการกะพริบตัวละคร")]
        [SerializeField] private float blinkInterval = 0.1f;

        [Header("Fall Death")]
        [Tooltip("ระดับแกน Y ที่ต่ำกว่านี้ถือว่าตกเหวตาย (-0.8f อยู่ต่ำกว่าระดับพื้นดิน 0.0f พอดี เมื่อตกหลุมตัวละครจะตายทันที)")]
        [SerializeField] private float deathYThreshold = -0.8f;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip jumpSound;
        [SerializeField] private AudioClip doubleJumpSound;
        [SerializeField] private AudioClip slideSound;
        [SerializeField] private AudioClip hurtSound;

        [Header("Visual References")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private ParticleSystem slideDustParticles;

        // Internal State
        private Rigidbody2D rb;
        private BoxCollider2D boxCollider;
        private CookieHealth health;

        private bool isGrounded = false;
        private int jumpsRemaining = 2;
        private bool isSliding = false;
        private bool isSlideInputHeld = false; // จำการกดปุ่มสไลด์ค้าง (รองรับกดค้างกลางอากาศ พอแตะพื้นสไลด์ทันที)
        private float slideBufferTimer = 0f;
        [SerializeField] private float slideBufferTime = 0.35f;
        private float minSlideTimer = 0f;
        [Tooltip("เหยียบสไลด์ครั้งเดียว = สไลด์อย่างน้อยเท่านี้ (วินาที) เหยียบค้างไว้จะสไลด์ต่อ")]
        [SerializeField] private float minSlideDuration = 0.7f;
        private bool isInvincible = false;

        // Coyote and Buffer Timers
        private float coyoteTimer = 0f;
        private float jumpBufferTimer = 0f;

        // Buff Timers
        private bool isMagnetActive = false;
        private float magnetRadius = 6f;
        private bool isGiant = false;
        private bool isBlast = false;
        private Vector3 originalScale;
        private Coroutine blastCoroutine;
        private Coroutine giantCoroutine;
        private Coroutine magnetCoroutine;

        [Header("Shield Power-up")]
        [SerializeField] private bool hasShield = false;
        [SerializeField] private GameObject shieldVisualObject;
        [SerializeField] private AudioClip shieldBreakSound;
        [Tooltip("โล่อยู่ได้นานกี่วินาที (รับการชนได้ 1 ครั้งภายในเวลานี้)")]
        [SerializeField] private float shieldDuration = 10f;
        [Tooltip("เหลือเวลาโล่น้อยกว่านี้ (วินาที) จะกะพริบเตือน")]
        [SerializeField] private float shieldWarningTime = 3f;
        private float shieldTimeLeft;
        private Coroutine shieldCoroutine;

        [Header("Blast Power-up")]
        [Tooltip("หมดความเร็ว (Blast) แล้วอมตะกะพริบต่ออีกกี่วินาที")]
        [SerializeField] private float postBlastInvincibleDuration = 1.5f;

        public event System.Action<bool> OnShieldChanged;

        // Animator Hashes
        private static readonly int AnimGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int AnimSliding = Animator.StringToHash("IsSliding");
        private static readonly int AnimJump = Animator.StringToHash("Jump");
        private static readonly int AnimDoubleJump = Animator.StringToHash("DoubleJump");
        private static readonly int AnimHurt = Animator.StringToHash("Hurt");
        private static readonly int AnimDead = Animator.StringToHash("IsDead");
        private static readonly int SpumMove = Animator.StringToHash("1_Move");
        private static readonly int SpumIsDeath = Animator.StringToHash("isDeath");
        private static readonly int SpumDeathTrigger = Animator.StringToHash("4_Death");
        private static readonly int SpumDamaged = Animator.StringToHash("3_Damaged");

        // แคช Transform ของโมเดล SPUM ไว้ ไม่ต้อง transform.Find ทุกเฟรม
        private Transform spumModel;
        private static readonly Vector3 SpumDefaultScale = new Vector3(-1f, 1f, 1f);

        // Animator Parameter Existence Flags
        private bool hasAnimGrounded = false;
        private bool hasAnimSliding = false;
        private bool hasAnimJump = false;
        private bool hasAnimDoubleJump = false;
        private bool hasAnimHurt = false;
        private bool hasAnimDead = false;

        // SPUM Animation Support
        private bool hasSpumMove = false;
        private bool hasSpumDeath = false;
        private bool hasSpumDamaged = false;

        public static CookiePlayerController Instance { get; private set; }

        public bool IsSliding => isSliding;
        public bool IsGrounded => isGrounded;
        public bool IsInvincible => isInvincible || isGiant || isBlast;
        public bool IsMagnetActive => isMagnetActive;
        public float MagnetRadius => magnetRadius;
        public bool IsGiant => isGiant;
        public bool IsBlast => isBlast;
        public bool HasShield => hasShield;
        public float CurrentSpeed => baseRunSpeed * speedMultiplier * DifficultySpeedMultiplier;

        // ความเร็วที่เพิ่มขึ้นเรื่อยๆ ตามระยะทาง (จาก CookieGameManager)
        private static float DifficultySpeedMultiplier =>
            CookieGameManager.Instance != null ? CookieGameManager.Instance.SpeedMultiplier : 1f;
        /// <summary>เวลาจากกระโดดจนถึงจุดสูงสุด (วินาที) ใช้คำนวณจุดกระโดดที่ปลอดภัยในช่วงสอนเล่น</summary>
        public float JumpApexTime => jumpForce / (Mathf.Abs(Physics2D.gravity.y) * baseGravityScale);

        public bool CheckJumpInput() => GameInputBindings.GetDown(GameAction.RunJump);

        public bool CheckSlideInput() => GameInputBindings.GetDown(GameAction.RunSlide);

        private void Awake()
        {
            Instance = this;
            rb = GetComponent<Rigidbody2D>();
            boxCollider = GetComponent<BoxCollider2D>();
            health = GetComponent<CookieHealth>();

            rb.gravityScale = baseGravityScale;

            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();

            if (animator != null)
            {
                foreach (var p in animator.parameters)
                {
                    if (p.name == "IsGrounded") hasAnimGrounded = true;
                    if (p.name == "IsSliding") hasAnimSliding = true;
                    if (p.name == "Jump") hasAnimJump = true;
                    if (p.name == "DoubleJump") hasAnimDoubleJump = true;
                    if (p.name == "Hurt") hasAnimHurt = true;
                    if (p.name == "IsDead") hasAnimDead = true;
                    if (p.name == "1_Move") hasSpumMove = true;
                    if (p.name == "isDeath") hasSpumDeath = true;
                    if (p.name == "3_Damaged") hasSpumDamaged = true;
                }
            }

            originalScale = transform.localScale;

            // กันตัวละครติดรอยต่อระหว่างพื้นแต่ละท่อน (ghost collision):
            // ทำมุม Collider ให้มน (ขนาดรวมเท่าเดิม) และไม่มีแรงเสียดทานกับพื้น
            const float edgeRadius = 0.1f;
            if (boxCollider.edgeRadius < edgeRadius)
            {
                boxCollider.size = new Vector2(Mathf.Max(0.1f, boxCollider.size.x - edgeRadius * 2f), Mathf.Max(0.1f, boxCollider.size.y - edgeRadius * 2f));
                boxCollider.edgeRadius = edgeRadius;
            }
            var noFriction = new PhysicsMaterial2D("PlayerNoFriction") { friction = 0f, bounciness = 0f };
            boxCollider.sharedMaterial = noFriction;
            rb.sharedMaterial = noFriction;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // กล้องตามนุ่มขึ้น ไม่สั่น

            // ตั้งค่า Collider เริ่มต้น
            standColliderSize = boxCollider.size;
            standColliderOffset = boxCollider.offset;
            slideColliderSize = new Vector2(standColliderSize.x * 1.3f, standColliderSize.y * 0.5f);
            slideColliderOffset = new Vector2(standColliderOffset.x, standColliderOffset.y - (standColliderSize.y * 0.25f));

            // ตั้งค่า SortingGroup ให้ตัวละครและชิ้นส่วน SPUM ทั้งหมดแสดงอยู่ด้านหน้าฉากหลังเสมอ
            var sortingGroup = GetComponent<UnityEngine.Rendering.SortingGroup>();
            if (sortingGroup == null)
            {
                sortingGroup = gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
            }
            sortingGroup.sortingOrder = 10;

            // เอฟเฟกต์แสดงสถานะบัฟ (โล่ / เร่งความเร็ว) เพิ่มให้อัตโนมัติหากยังไม่มีในตัวผู้เล่น
            if (GetComponent<CookieBuffVisuals>() == null)
            {
                gameObject.AddComponent<CookieBuffVisuals>();
            }

            // ตรวจสอบและรีเซ็ตตำแหน่ง/สเกลของโมเดล SPUM ให้ถูกต้อง
            spumModel = transform.Find("SPUM_Model");
            if (spumModel != null)
            {
                spumModel.localPosition = Vector3.zero;
                spumModel.localScale = SpumDefaultScale;
                spumModel.localRotation = Quaternion.identity;
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnDeath += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDeath -= HandleDeath;
            }
        }

        private void Update()
        {
            if (health.IsDead) return;
            if (CookieGameManager.Instance != null && !CookieGameManager.Instance.IsPlaying) return;

            // เกมหยุดชั่วคราว (เช่น ช่วงสอนเล่น) ไม่รับปุ่ม เพื่อไม่ให้กระโดดซ้อนกับคำสั่งของ Tutorial
            if (Time.timeScale <= 0f) return;
            // เฟรมที่เพิ่งปิดการ์ดสอนเล่น/หน้าหยุดเกม: ปุ่มที่กดปิดไม่นับเป็นกระโดด/สไลด์
            if (ExhibitMode.InputBlocked) return;

            CheckGrounded();
            HandleInput();
            HandleJumpBuffer();
            HandleSlideTimers();
            CheckFallDeath();
            UpdateAnimations();
            UpdateJumpVisuals();
        }

        private void FixedUpdate()
        {
            if (health != null && health.IsDead)
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
                return;
            }

            if (CookieGameManager.Instance != null && !CookieGameManager.Instance.IsPlaying)
            {
                rb.velocity = Vector2.zero;
                return;
            }

            CheckFallDeath();

            // เคลื่อนที่ไปข้างหน้าอัตโนมัติ (แกน X) พร้อม Slide Boost
            float targetSpeed = CurrentSpeed;
            if (slideBoostTimer > 0f)
            {
                slideBoostTimer -= Time.fixedDeltaTime;
                float boostFactor = Mathf.Lerp(1f, slideBoostMultiplier, slideBoostTimer / slideBoostDuration);
                targetSpeed *= boostFactor;
            }

            if (isBlast)
            {
                // โหมด Blast (วิ่งเร่งความเร็วสไตล์ Cookie Run):
                // สามารถกระโดดได้ตามปกติ และบินลอยข้ามหลุมได้ ไม่ตกหลุมตาย
                float floorY = 0.1f;

                // เมื่อตัวละครอยู่ที่ระดับพื้น และไม่ได้กำลังกระโดดพุ่งขึ้น (rb.velocity.y <= 0.01f)
                if (transform.position.y <= floorY && rb.velocity.y <= 0.01f)
                {
                    transform.position = new Vector3(transform.position.x, floorY, transform.position.z);
                    rb.velocity = new Vector2(targetSpeed, 0f);
                    rb.gravityScale = 0f;
                    isGrounded = true;
                    jumpsRemaining = maxJumps;
                }
                else
                {
                    // กำลังกระโดดขึ้น หรืออยู่กลางอากาศ ให้กระโดดและลอยตัวได้อย่างอิสระตามแรงกระโดด
                    rb.velocity = new Vector2(targetSpeed, rb.velocity.y);
                    rb.gravityScale = baseGravityScale;
                }
            }
            else
            {
                rb.velocity = new Vector2(targetSpeed, rb.velocity.y);

                // ปรับแต่งแรงโน้มถ่วงแบบ Snappy Fall (ตกไว ไม่ลอยอวกาศ)
                if (rb.velocity.y < -0.1f)
                {
                    rb.gravityScale = baseGravityScale * fallGravityMultiplier;
                    if (rb.velocity.y < maxFallSpeed)
                    {
                        rb.velocity = new Vector2(rb.velocity.x, maxFallSpeed);
                    }
                }
                else
                {
                    rb.gravityScale = baseGravityScale;
                }
            }
        }

        #region Ground Check & Logic
        private void CheckGrounded()
        {
            Vector2 checkPos = groundCheckPoint != null ? (Vector2)groundCheckPoint.position : (Vector2)transform.position + new Vector2(0f, -boxCollider.size.y / 2f);
            bool wasGrounded = isGrounded;

            // ถ้ากำลังลอยพุ่งขึ้น ไม่นับว่าอยู่บนพื้น เพื่อให้ Double Jump กดได้ทันที
            if (rb.velocity.y > 0.1f)
            {
                isGrounded = false;
            }
            else if (isBlast && transform.position.y <= 0.12f)
            {
                // ในโหมด Blast หากอยู่ที่ระดับพื้น/เหาะข้ามหลุม ให้นับว่าอยู่บนพื้นเพื่อให้กดกระโดดได้ทันที
                isGrounded = true;
            }
            else
            {
                isGrounded = Physics2D.OverlapBox(checkPos, groundCheckSize, 0f, groundLayer);
            }

            if (isGrounded)
            {
                jumpsRemaining = maxJumps;
                coyoteTimer = coyoteTime;

                // เมื่อเท้าแตะพื้น ให้รีเซ็ตท่าทางย้อนกลับมาวิ่งทันที
                if (!wasGrounded)
                {
                    OnLanded();
                }

                // ถ้ากดปุ่มย่อค้างไว้ หรือบัฟเฟอร์การย่อไว้ตอนอยู่กลางอากาศ พอลงมาจะย่อทันที!
                if ((isSlideInputHeld || slideBufferTimer > 0f) && !isSliding)
                {
                    slideBufferTimer = 0f;
                    StartSlide();
                }
            }
            else
            {
                coyoteTimer -= Time.deltaTime;
                // ถ้าสไลด์อยู่แล้วตกจากขอบ ให้ยกเลิกสไลด์อัตโนมัติ
                if (isSliding)
                {
                    EndSlide();
                }
            }
        }

        private void OnLanded()
        {
            if (spumModel != null)
            {
                spumModel.localRotation = Quaternion.identity;
            }

            // ถ้ากดปุ่มย่อค้างไว้ หรือบัฟเฟอร์การย่อไว้ตอนอยู่กลางอากาศ พอลงมาจะย่อทันที!
            if ((isSlideInputHeld || slideBufferTimer > 0f) && !isSliding)
            {
                slideBufferTimer = 0f;
                StartSlide();
            }
            else
            {
                if (animator != null && hasSpumMove)
                {
                    animator.SetBool(SpumMove, !health.IsDead && !isSliding);
                }
            }
        }

        private void UpdateJumpVisuals()
        {
            if (health.IsDead) return;

            Transform spumChild = spumModel;
            if (spumChild == null) return;

            // จัดการท่าทางสไลด์แบบนุ่มนวลและเอียงตัวสมจริง
            if (isSliding)
            {
                spumChild.localPosition = Vector3.Lerp(spumChild.localPosition, slideModelOffset, Time.deltaTime * 24f);
                spumChild.localScale = Vector3.Lerp(spumChild.localScale, slideModelScale, Time.deltaTime * 24f);
                spumChild.localRotation = Quaternion.Slerp(spumChild.localRotation, Quaternion.Euler(0f, 0f, slideTiltAngle), Time.deltaTime * 24f);
            }
            else
            {
                spumChild.localPosition = Vector3.Lerp(spumChild.localPosition, Vector3.zero, Time.deltaTime * 20f);
                spumChild.localScale = Vector3.Lerp(spumChild.localScale, SpumDefaultScale, Time.deltaTime * 20f);

                if (!isGrounded)
                {
                    // ขณะลอยในอากาศ: ลอยขึ้นเอียงไปข้างหน้า ตกลงเอียงลงเล็กน้อย
                    float targetAngle = (rb.velocity.y > 0.5f) ? 8f : ((rb.velocity.y < -0.5f) ? -6f : 0f);
                    spumChild.localRotation = Quaternion.Slerp(spumChild.localRotation, Quaternion.Euler(0f, 0f, targetAngle), Time.deltaTime * 12f);
                }
                else
                {
                    // เมื่ออยู่บนพื้น คืนมุมหมุนกลับสู่ 0 ทันที
                    spumChild.localRotation = Quaternion.Slerp(spumChild.localRotation, Quaternion.identity, Time.deltaTime * 20f);
                }
            }
        }
        #endregion

        #region Input Handling (Keyboard + Touch/UI)
        private void HandleInput()
        {
            // รับปุ่มจากแผ่นเหยียบและคีย์บอร์ด ตามที่ตั้งไว้ในหน้า Option (GameInputBindings)
            if (CheckJumpInput())
            {
                OnJumpPressed();
            }

            if (GameInputBindings.GetUp(GameAction.RunJump))
            {
                OnJumpReleased();
            }

            if (CheckSlideInput())
            {
                OnSlideStart();
            }

            // ปล่อยสไลด์เมื่อไม่มีปุ่มสไลด์ปุ่มไหนถูกกดค้างอยู่แล้ว
            if (GameInputBindings.GetUp(GameAction.RunSlide) && !GameInputBindings.GetHeld(GameAction.RunSlide))
            {
                OnSlideEnd();
            }
        }

        private void HandleJumpBuffer()
        {
            if (jumpBufferTimer > 0f)
            {
                jumpBufferTimer -= Time.deltaTime;

                if (isGrounded || coyoteTimer > 0f)
                {
                    PerformJump(false);
                    jumpBufferTimer = 0f;
                }
            }
        }

        private void HandleSlideTimers()
        {
            if (slideBufferTimer > 0f)
            {
                slideBufferTimer -= Time.deltaTime;
            }

            if (minSlideTimer > 0f)
            {
                minSlideTimer -= Time.deltaTime;
                if (minSlideTimer <= 0f && !isSlideInputHeld && isSliding)
                {
                    TryEndSlide();
                }
            }
            else if (isSliding && !isSlideInputHeld)
            {
                // ยังอยู่ใต้สิ่งกีดขวาง: รอจนพ้นแล้วค่อยลุก
                TryEndSlide();
            }
        }

        // ลุกขึ้นจากสไลด์เฉพาะเมื่อไม่มีสิ่งกีดขวางด้านบนอยู่ (ไม่ให้ลุกชนหินย้อยทั้งที่ยังสไลด์ผ่านไม่พ้น)
        private void TryEndSlide()
        {
            if (IsUnderOverheadObstacle()) return;
            EndSlide();
        }

        private static readonly Collider2D[] OverlapBuffer = new Collider2D[8];
        private static readonly RaycastHit2D[] GroundRayBuffer = new RaycastHit2D[4];

        // มีพื้นอยู่ใต้เท้าในระยะนี้หรือไม่ (ไม่นับเมื่ออยู่เหนือหลุม จึงยังใช้ Double Jump ข้ามหลุมได้ตามปกติ)
        private bool IsNearGround(float height)
        {
            Vector2 feet = (Vector2)boxCollider.bounds.center + Vector2.down * boxCollider.bounds.extents.y;
            int count = Physics2D.RaycastNonAlloc(feet, Vector2.down, GroundRayBuffer, height, groundLayer);
            for (int i = 0; i < count; i++)
            {
                if (!GroundRayBuffer[i].collider.isTrigger) return true;
            }
            return false;
        }

        private bool IsUnderOverheadObstacle()
        {
            Vector2 center = (Vector2)transform.position + standColliderOffset * (Vector2)transform.localScale;
            Vector2 size = (standColliderSize + Vector2.one * (boxCollider.edgeRadius * 2f)) * (Vector2)transform.localScale;
            int count = Physics2D.OverlapBoxNonAlloc(center, size, 0f, OverlapBuffer);
            for (int i = 0; i < count; i++)
            {
                CookieObstacle obstacle = OverlapBuffer[i].GetComponent<CookieObstacle>();
                if (obstacle != null && obstacle.Kind == ObstacleType.OverheadObstacle && obstacle.IsArmed) return true;
            }
            return false;
        }

        /// <summary>
        /// เรียกเมื่อกดปุ่มกระโดด (สามารถผูกกับ UI Button / Mobile OnScreen Button ได้)
        /// </summary>
        public void OnJumpPressed()
        {
            if (health.IsDead) return;

            // กันการกดครั้งเดียวถูกนับ 2 ครั้งในเฟรมเดียว (เช่น Tutorial กับ Input ของผู้เล่นพร้อมกัน) จนกลายเป็น Double Jump
            if (lastJumpInputFrame == Time.frameCount) return;
            lastJumpInputFrame = Time.frameCount;

            // หากกดโดดตอนสไลด์ ให้ยกเลิกสไลด์ทันที
            if (isSliding)
            {
                EndSlide();
            }

            // เคลียร์สถานะการสไลด์เมื่อกดกระโดด
            isSlideInputHeld = false;
            slideBufferTimer = 0f;
            minSlideTimer = 0f;

            // ตรวจสอบกระโดดครั้งแรก (เมื่ออยู่บนพื้นหรือยังอยู่ใน Coyote Time)
            if (isGrounded || coyoteTimer > 0f)
            {
                PerformJump(false);
            }
            // กำลังร่วงใกล้ถึงพื้น: ผู้เล่นแผ่นเหยียบมักเหยียบล่วงหน้าเพื่อเตรียมกระโดดข้ามสิ่งถัดไป
            // จำไว้แล้วกระโดดเต็มแรงทันทีที่แตะพื้น แทนการเสียสิทธิ์ Double Jump ไปกลางอากาศ
            else if (rb.velocity.y < 0f && IsNearGround(landingBufferHeight))
            {
                jumpBufferTimer = jumpBufferTime;
            }
            // ตรวจสอบ Double Jump กลางอากาศ (ถ้ายังมีสิทธิ์กระโดดเหลือ)
            else if (jumpsRemaining > 0)
            {
                PerformJump(true);
            }
            else
            {
                // บันทึก Jump Buffer ล่วงหน้า
                jumpBufferTimer = jumpBufferTime;
            }
        }

        /// <summary>
        /// เรียกเมื่อปล่อยปุ่มกระโดด (Variable Jump)
        /// </summary>
        public void OnJumpReleased()
        {
            if (!variableJumpHeight) return;

            if (rb.velocity.y > 0f)
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpCutMultiplier);
            }
        }

        private void PerformJump(bool isDoubleJump)
        {
            float force = isDoubleJump ? doubleJumpForce : jumpForce;
            rb.velocity = new Vector2(rb.velocity.x, force);

            isGrounded = false;
            coyoteTimer = 0f;

            if (CookieGameManager.Instance != null) CookieGameManager.Instance.RecordJump();

            if (isDoubleJump)
            {
                jumpsRemaining = 0;
                if (animator != null && hasAnimDoubleJump) animator.SetTrigger(AnimDoubleJump);
                PlaySound(doubleJumpSound, CookieSfx.DoubleJump);
            }
            else
            {
                jumpsRemaining = maxJumps - 1;
                if (animator != null && hasAnimJump) animator.SetTrigger(AnimJump);
                PlaySound(jumpSound, CookieSfx.Jump);
            }

            // รักษาสถานะวิ่งไว้ต่อเนื่อง
            if (animator != null && hasSpumMove)
            {
                animator.SetBool(SpumMove, true);
            }
        }

        /// <summary>
        /// เรียกเมื่อเริ่มกดปุ่มสไลด์ค้าง (รองรับกดค้างกลางอากาศ พอแตะพื้นสไลด์ทันที)
        /// </summary>
        public void OnSlideStart()
        {
            if (health.IsDead) return;

            isSlideInputHeld = true;
            slideBufferTimer = slideBufferTime;

            if (isGrounded && !isSliding)
            {
                StartSlide();
            }
            else if (!isGrounded && !isBlast && rb.velocity.y > -fastFallSpeed)
            {
                // กดสไลด์กลางอากาศ: ดิ่งลงพื้นทันที แล้วสไลด์ต่อเมื่อแตะพื้น (ระบบ slide buffer)
                rb.velocity = new Vector2(rb.velocity.x, -fastFallSpeed);
            }
        }

        /// <summary>
        /// เรียกเมื่อปล่อยปุ่มสไลด์
        /// </summary>
        public void OnSlideEnd()
        {
            isSlideInputHeld = false;
            if (isSliding && minSlideTimer <= 0f)
            {
                TryEndSlide();
            }
        }

        private void StartSlide()
        {
            if (health.IsDead || isSliding || !isGrounded) return;

            isSliding = true;
            minSlideTimer = minSlideDuration;
            if (CookieGameManager.Instance != null) CookieGameManager.Instance.RecordSlide();
            slideBoostTimer = slideBoostDuration;
            boxCollider.size = slideColliderSize;
            boxCollider.offset = slideColliderOffset;

            EnsureSlideDustParticles();
            if (slideDustParticles != null) slideDustParticles.Play();
            PlaySound(slideSound, CookieSfx.Slide);

            if (animator != null && hasSpumMove)
            {
                animator.SetBool(SpumMove, false);
            }
        }

        private void EndSlide()
        {
            if (!isSliding) return;

            isSliding = false;
            minSlideTimer = 0f;
            slideBoostTimer = 0f;
            boxCollider.size = standColliderSize;
            boxCollider.offset = standColliderOffset;

            if (slideDustParticles != null) slideDustParticles.Stop();

            if (animator != null && hasSpumMove)
            {
                animator.SetBool(SpumMove, !health.IsDead);
            }
        }

        private void EnsureSlideDustParticles()
        {
            if (slideDustParticles != null) return;

            Transform existing = transform.Find("SlideDustFX");
            if (existing != null)
            {
                slideDustParticles = existing.GetComponent<ParticleSystem>();
                return;
            }

            GameObject dustObj = new GameObject("SlideDustFX");
            dustObj.transform.parent = transform;
            dustObj.transform.localPosition = new Vector3(-0.35f, 0.05f, 0f);

            slideDustParticles = dustObj.AddComponent<ParticleSystem>();
            var main = slideDustParticles.main;
            main.playOnAwake = false;
            main.startLifetime = 0.3f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 2.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
            main.startColor = new Color(0.85f, 0.82f, 0.78f, 0.5f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = slideDustParticles.emission;
            emission.rateOverTime = 30f;

            var shape = slideDustParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.15f;
            shape.rotation = new Vector3(0f, 0f, 160f);

            // ต้องมี Material ไม่อย่างนั้นฝุ่นจะไม่แสดง (หรือขึ้นเป็นสีชมพู)
            dustObj.GetComponent<ParticleSystemRenderer>().sharedMaterial = CookieFxTextures.EffectMaterial;

            var sizeOverLife = slideDustParticles.sizeOverLifetime;
            sizeOverLife.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.4f);
            curve.AddKey(0.3f, 1f);
            curve.AddKey(1f, 0f);
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var renderer = dustObj.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 12;
        }
        #endregion

        #region Damage & Obstacle Hit
        /// <summary>
        /// โดนสิ่งกีดขวาง
        /// </summary>
        public void HitObstacle(float damage)
        {
            // ช่วงสอนเล่นผู้เล่นเป็นอมตะ (IsImmune) ชนสิ่งกีดขวางแล้วไม่ตาย
            if (IsInvincible || health.IsDead || health.IsImmune) return;

            // หากมีเกราะป้องกัน (Shield) -> โล่จะดูดซับความเสียหาย 1 ครั้ง ไม่ Game Over
            if (hasShield)
            {
                ClearShield();
                PlaySound(shieldBreakSound, CookieSfx.ShieldBreak);

                if (animator != null && hasAnimHurt) animator.SetTrigger(AnimHurt);
                if (animator != null && hasSpumDamaged) animator.SetTrigger(SpumDamaged);

                StartInvincibility(hurtInvincibleDuration);
                return;
            }

            // ไม่มีโล่ -> เสียหัวใจ 1 ดวง แล้วกระพริบอมตะสั้นๆ ให้ผู้เล่นตั้งตัว (หัวใจหมด = Game Over)
            PlaySound(hurtSound, CookieSfx.Hurt);

            if (animator != null && hasAnimHurt) animator.SetTrigger(AnimHurt);
            if (animator != null && hasSpumDamaged) animator.SetTrigger(SpumDamaged);

            health.LoseHeart();
            if (!health.IsDead) StartInvincibility(hurtInvincibleDuration);
        }

        /// <summary>
        /// สั่งให้ผู้เล่นเสียชีวิตทันที (Game Over) เช่น ตกเหว หรือวิ่งชนมอนสเตอร์
        /// </summary>
        public void KillInstant()
        {
            if (health.IsDead || health.IsImmune) return;

            ClearShield();

            PlaySound(hurtSound, CookieSfx.Hurt);

            if (animator != null && hasAnimHurt) animator.SetTrigger(AnimHurt);
            if (animator != null && hasSpumDamaged) animator.SetTrigger(SpumDamaged);
            if (animator != null && hasAnimDead) animator.SetBool(AnimDead, true);
            if (animator != null && hasSpumDeath)
            {
                animator.SetBool(SpumIsDeath, true);
                animator.SetTrigger(SpumDeathTrigger);
            }

            rb.velocity = Vector2.zero;
            EndSlide();

            health.Die();
        }

        private Coroutine invincibleCoroutine;
        private SpriteRenderer[] blinkRenderers;

        /// <summary>อมตะชั่วคราวพร้อมกะพริบตัว (ถ้ากำลังกะพริบอยู่ ให้เริ่มนับใหม่ตามเวลาที่ยาวกว่า)</summary>
        private void StartInvincibility(float duration)
        {
            if (invincibleCoroutine != null) StopCoroutine(invincibleCoroutine);
            invincibleCoroutine = StartCoroutine(InvincibilityRoutine(duration));
        }

        private IEnumerator InvincibilityRoutine(float duration)
        {
            isInvincible = true;
            float elapsed = 0f;
            // กะพริบเฉพาะตัวละคร ไม่ยุ่งกับเอฟเฟกต์บัฟ (ฟองโล่/แสง Blast จัดการความโปร่งใสเอง)
            if (blinkRenderers == null) blinkRenderers = spumModel != null ? spumModel.GetComponentsInChildren<SpriteRenderer>(true) : GetComponentsInChildren<SpriteRenderer>(true);

            while (elapsed < duration)
            {
                bool fade = (Mathf.FloorToInt(elapsed / blinkInterval) % 2) == 0;
                SetBlinkAlpha(fade ? 0.3f : 1f);
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
            }

            SetBlinkAlpha(1f);
            isInvincible = false;
            invincibleCoroutine = null;
        }

        private void SetBlinkAlpha(float alpha)
        {
            foreach (var sr in blinkRenderers)
            {
                if (sr == null) continue;
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
        }

        private void CheckFallDeath()
        {
            // ในโหมด Blast ตัวละครจะเหาะ/พุ่งข้ามหลุมได้ ไม่ Game Over จากการตกหลุม
            if (isBlast) return;

            if (transform.position.y < deathYThreshold && health != null && !health.IsDead)
            {
                health.Die();
            }
        }

        private void HandleDeath()
        {
            if (animator != null && hasAnimDead) animator.SetBool(AnimDead, true);
            if (animator != null && hasSpumDeath)
            {
                animator.SetBool(SpumIsDeath, true);
                animator.SetTrigger(SpumDeathTrigger);
            }
            if (animator != null) animator.speed = 1f;
            rb.velocity = Vector2.zero;
            EndSlide();
            CookieAudio.Play(CookieSfx.Death);
        }
        #endregion

        #region Buff Systems (Shield, Magnet, Giant, Blast)
        public void ActivateShield()
        {
            hasShield = true;
            shieldTimeLeft = shieldDuration;
            if (shieldVisualObject != null) shieldVisualObject.SetActive(true);
            if (shieldCoroutine != null) StopCoroutine(shieldCoroutine);
            shieldCoroutine = StartCoroutine(ShieldTimerRoutine());
            OnShieldChanged?.Invoke(true);
        }

        // โล่มีเวลาจำกัด: ใกล้หมดเวลาจะกะพริบเตือน (CookieBuffVisuals / หัวใจสีฟ้าบน HUD) หมดเวลาแล้วหายไปเอง
        private IEnumerator ShieldTimerRoutine()
        {
            while (shieldTimeLeft > 0f)
            {
                // ไม่นับเวลาตอนเกมหยุด (เช่น ช่วงสอนเล่น)
                if (CookieGameManager.Instance == null || CookieGameManager.Instance.IsPlaying) shieldTimeLeft -= Time.deltaTime;
                if (shieldVisualObject != null && shieldTimeLeft < shieldWarningTime)
                {
                    shieldVisualObject.SetActive(Mathf.Repeat(Time.time, ShieldBlinkPeriod) < ShieldBlinkPeriod * 0.6f);
                }
                yield return null;
            }
            shieldCoroutine = null;
            if (!hasShield) yield break;
            ClearShield();
            CookieAudio.Play(CookieSfx.ShieldBreak, 0.5f);
        }

        private void ClearShield()
        {
            if (shieldCoroutine != null) { StopCoroutine(shieldCoroutine); shieldCoroutine = null; }
            bool had = hasShield;
            hasShield = false;
            shieldTimeLeft = 0f;
            if (shieldVisualObject != null) shieldVisualObject.SetActive(false);
            if (had) OnShieldChanged?.Invoke(false);
        }

        /// <summary>กะพริบเร็วขึ้นเมื่อเวลาโล่ใกล้หมด (ใช้ร่วมกับ CookieBuffVisuals และ HUD)</summary>
        public float ShieldBlinkPeriod => shieldTimeLeft < shieldWarningTime * 0.4f ? 0.16f : 0.32f;
        public bool IsShieldExpiring => hasShield && shieldTimeLeft < shieldWarningTime;
        public float ShieldTimeLeft => shieldTimeLeft;

        public void ActivateMagnet(float duration, float radius = 6f)
        {
            if (magnetCoroutine != null) StopCoroutine(magnetCoroutine);
            magnetCoroutine = StartCoroutine(MagnetRoutine(duration, radius));
        }

        private IEnumerator MagnetRoutine(float duration, float radius)
        {
            isMagnetActive = true;
            magnetRadius = radius;
            yield return new WaitForSeconds(duration);
            isMagnetActive = false;
            magnetCoroutine = null;
        }

        public void ActivateGiant(float duration, float scaleFactor = 1.6f)
        {
            if (giantCoroutine != null) StopCoroutine(giantCoroutine);
            giantCoroutine = StartCoroutine(GiantRoutine(duration, scaleFactor));
        }

        private IEnumerator GiantRoutine(float duration, float scaleFactor)
        {
            isGiant = true;
            transform.localScale = originalScale * scaleFactor;
            yield return new WaitForSeconds(duration);
            transform.localScale = originalScale;
            isGiant = false;
            giantCoroutine = null;
        }

        public void ActivateBlast(float duration, float speedBoost = 1.8f)
        {
            if (blastCoroutine != null) StopCoroutine(blastCoroutine);
            blastCoroutine = StartCoroutine(BlastRoutine(duration, speedBoost));
        }

        // มีพื้นรองรับใต้ตัวละครทั้งด้านหน้าและด้านหลัง (ไม่ได้อยู่เหนือหลุมหรือคาบขอบหลุม)
        private bool HasGroundBelow()
        {
            float halfWidth = standColliderSize.x * 0.5f * Mathf.Abs(transform.localScale.x);
            Vector2 pos = transform.position;
            return Physics2D.Raycast(new Vector2(pos.x - halfWidth, pos.y + 0.5f), Vector2.down, 4f, groundLayer)
                && Physics2D.Raycast(new Vector2(pos.x + halfWidth, pos.y + 0.5f), Vector2.down, 4f, groundLayer);
        }

        private IEnumerator BlastRoutine(float duration, float speedBoost)
        {
            isBlast = true;
            speedMultiplier = speedBoost;
            yield return new WaitForSeconds(duration);

            // หมดเวลาตอนลอยอยู่เหนือหลุม (หรือกระโดดค้างกลางอากาศเหนือหลุม): ยังลอยต่อจนกว่าใต้เท้าจะมีพื้น ไม่ให้ตกหลุม
            while (!health.IsDead && !HasGroundBelow())
            {
                yield return new WaitForFixedUpdate();
            }

            speedMultiplier = 1f;
            isBlast = false;
            rb.gravityScale = baseGravityScale;
            blastCoroutine = null;

            // หมดความเร็วแล้วอาจอยู่กลางสิ่งกีดขวางที่เพิ่งพุ่งชน: อมตะกะพริบสั้นๆ ให้ตั้งตัวก่อน
            if (!health.IsDead && postBlastInvincibleDuration > 0f) StartInvincibility(postBlastInvincibleDuration);
        }
        #endregion

        #region Animations & Sound
        private void UpdateAnimations()
        {
            if (animator == null) return;
            if (hasAnimGrounded) animator.SetBool(AnimGrounded, isGrounded);
            if (hasAnimSliding) animator.SetBool(AnimSliding, isSliding);

            if (hasSpumMove)
            {
                // ตัวละครในเกมวิ่ง จะรักษาสถานะวิ่งต่อเนื่องตลอดเวลาที่ยังมีชีวิตอยู่และไม่ได้สไลด์
                animator.SetBool(SpumMove, !health.IsDead && !isSliding);
            }

            // ก้าวขาเร็วขึ้นตามความเร็ววิ่งที่เพิ่มขึ้น (ไม่ให้ดูเหมือนตัวไถลไปบนพื้น)
            animator.speed = 1f + (DifficultySpeedMultiplier - 1f) * 0.8f;
        }

        // เล่นเสียงที่ตั้งไว้ที่ตัวผู้เล่น ถ้าไม่ได้ตั้ง ใช้เสียงกลางจาก CookieAudio
        private void PlaySound(AudioClip clip, CookieSfx fallback)
        {
            if (clip != null && audioSource != null)
            {
                // ใช้ความดังเสียงเอฟเฟกต์เดียวกับ CookieAudio ไม่ให้เสียงตัวละครดังกว่าเสียงอื่น
                audioSource.PlayOneShot(clip, CookieAudio.Instance != null ? CookieAudio.Instance.sfxVolume : 1f);
            }
            else
            {
                CookieAudio.Play(fallback);
            }
        }
        #endregion

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Vector2 checkPos = groundCheckPoint != null ? (Vector2)groundCheckPoint.position : (Vector2)transform.position + new Vector2(0f, -standColliderSize.y / 2f);
            Gizmos.DrawWireCube(checkPos, groundCheckSize);

            if (isMagnetActive)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, magnetRadius);
            }
        }
    }
}
