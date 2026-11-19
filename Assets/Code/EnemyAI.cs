using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("ความเร็วในการเดิน")]
    public float moveSpeed = 2.5f;

    [Tooltip("ระยะห่างที่ถือว่าถึงจุด Waypoint แล้ว")]
    public float waypointTolerance = 0.2f;

    [Header("Waypoint Settings")]
    [Tooltip("เป้าหมายจุดนำทาง (สามารถลากใส่ใน Inspector ได้ หรือระบบจะค้นหาให้อัตโนมัติ)")]
    public Transform targetWaypoint;

    [Tooltip("ชื่อ GameObject ของจุด Waypoint ในฉาก")]
    public string waypointName = "CaveExit_Waypoint";

    [Header("Target Player")]
    [Tooltip("เป้าหมายผู้เล่น (หากปล่อยว่างระบบจะค้นหาจาก Tag Player หรือ PlayerController ให้อัตโนมัติ)")]
    public Transform playerTransform;

    [Header("State")]
    [Tooltip("สถานะว่าเดินถึงจุด Waypoint แล้วหรือยัง")]
    public bool hasReachedWaypoint = false;

    private Rigidbody2D rb;
    private MonsterChaserAI chaserAI;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        chaserAI = GetComponent<MonsterChaserAI>();
    }

    void Start()
    {
        // 1. ค้นหาผู้เล่น
        FindPlayerTarget();

        // 2. ค้นหา Waypoint ในฉากแบบยืดหยุ่น (รองรับทุกฉาก Cave1, Cave2, Cave3 ฯลฯ)
        FindWaypointTarget();
    }

    void Update()
    {
        // ตรวจสอบ MonsterChaserAI กรณีถูกเพิ่มเข้ามาทีหลังหลัง Instantiate
        if (chaserAI == null)
        {
            chaserAI = GetComponent<MonsterChaserAI>();
        }

        // หากมอนสเตอร์ตายแล้ว ให้หยุดเคลื่อนไหวทันที
        if (chaserAI != null && chaserAI.IsMonsterDead())
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        // ค้นหาเป้าหมายผู้เล่นซ้ำอีกครั้งหากยังไม่พบ
        if (playerTransform == null)
        {
            FindPlayerTarget();
        }

        // --- โหมดที่ 1: เดินไปหา Waypoint ก่อน ---
        if (!hasReachedWaypoint && targetWaypoint != null)
        {
            MoveTowardsTarget(targetWaypoint.position);

            // ตรวจสอบว่าถึงจุดหมายหรือยัง
            if (Vector2.Distance(transform.position, targetWaypoint.position) <= waypointTolerance)
            {
                hasReachedWaypoint = true;
                if (rb != null) rb.velocity = Vector2.zero;
            }
        }
        // --- โหมดที่ 2: ถึงจุด Waypoint แล้ว (หรือฉากไม่มี Waypoint) ให้ไล่ตามผู้เล่น ---
        else
        {
            // หากมี MonsterChaserAI ติดตั้งอยู่ ให้ปล่อยให้ MonsterChaserAI ควบคุมการเดินและโจมตี
            // เพื่อไม่ให้ velocity ของทั้งสองสคริปต์ตีกัน (Conflict)
            if (chaserAI != null)
            {
                // MonsterChaserAI จัดการการไล่ล่าและระยะหยุดโจมตีใน FixedUpdate
            }
            else if (playerTransform != null)
            {
                MoveTowardsTarget(playerTransform.position);
            }
        }

        // จัดการการหันหน้า (Sprite Flipping)
        FlipSprite();
    }

    /// <summary>
    /// ค้นหาจุด Waypoint ในฉากอย่างชาญฉลาดตามลำดับความสำคัญ
    /// </summary>
    private void FindWaypointTarget()
    {
        // หากผู้ใช้กำหนด targetWaypoint ไว้ใน Inspector แล้ว ให้ใช้ค่านั้น
        if (targetWaypoint != null)
        {
            hasReachedWaypoint = false;
            return;
        }

        // 1. ค้นหาจากชื่อที่กำหนด (waypointName เช่น CaveExit_Waypoint)
        if (!string.IsNullOrEmpty(waypointName))
        {
            GameObject wpObj = GameObject.Find(waypointName);
            if (wpObj != null)
            {
                targetWaypoint = wpObj.transform;
                hasReachedWaypoint = false;
                return;
            }
        }

        // 2. ค้นหาจากชื่อ CaveExit_Waypoint
        GameObject caveExit = GameObject.Find("CaveExit_Waypoint");
        if (caveExit != null)
        {
            targetWaypoint = caveExit.transform;
            hasReachedWaypoint = false;
            return;
        }

        // 3. ค้นหาจากจุดที่มีคอมโพเนนต์ WarpZone
        WarpZone warpZone = FindObjectOfType<WarpZone>();
        if (warpZone != null)
        {
            targetWaypoint = warpZone.transform;
            hasReachedWaypoint = false;
            return;
        }

        // 4. ค้นหาจากชื่อจุดทางออกทั่วไป (WarpPoint, Exit, ExitPoint, Waypoint)
        string[] commonWaypointNames = new string[] { "WarpPoint", "Exit", "ExitPoint", "Waypoint", "ExitPortal" };
        foreach (string name in commonWaypointNames)
        {
            GameObject commonObj = GameObject.Find(name);
            if (commonObj != null)
            {
                targetWaypoint = commonObj.transform;
                hasReachedWaypoint = false;
                return;
            }
        }

        // 5. หากฉากนี้ไม่มีจุด Waypoint หรือทางออก ให้ถือว่าถึงจุดหมายแล้ว และเริ่มไล่ตามผู้เล่นได้ทันที
        // ไม่ต้องแสดง Warning รบกวนหน้าต่าง Console
        hasReachedWaypoint = true;
    }

    /// <summary>
    /// ค้นหาเป้าหมายผู้เล่นในฉาก
    /// </summary>
    private void FindPlayerTarget()
    {
        if (playerTransform != null) return;

        // 1. ค้นหาจาก Tag "Player"
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            return;
        }

        // 2. ค้นหาจาก PlayerController (Minesweeper Adventure)
        PlayerController pc = FindObjectOfType<PlayerController>();
        if (pc != null)
        {
            playerTransform = pc.transform;
            return;
        }

        // 3. ค้นหาจาก CookiePlayerController (CookieRun)
        CookieRun.CookiePlayerController cookiePc = FindObjectOfType<CookieRun.CookiePlayerController>();
        if (cookiePc != null)
        {
            playerTransform = cookiePc.transform;
            return;
        }
    }

    // ฟังก์ชันช่วยในการเคลื่อนที่
    public void MoveTowardsTarget(Vector2 targetPos)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 direction = (targetPos - (Vector2)transform.position).normalized;
            rb.velocity = direction * moveSpeed;
        }
    }

    // ฟังก์ชันจัดการการหันหน้าของ Sprite
    public void FlipSprite()
    {
        Transform spriteChild = transform.childCount > 0 ? transform.GetChild(0) : null;
        if (spriteChild == null) return;

        Vector2 targetPos = (!hasReachedWaypoint && targetWaypoint != null)
            ? (Vector2)targetWaypoint.position
            : (playerTransform != null ? (Vector2)playerTransform.position : (Vector2)transform.position);

        if (targetPos.x < transform.position.x - 0.05f)
        {
            spriteChild.localScale = new Vector3(-1f, 1f, 1f); // หันซ้าย
        }
        else if (targetPos.x > transform.position.x + 0.05f)
        {
            spriteChild.localScale = new Vector3(1f, 1f, 1f);  // หันขวา
        }
    }
}
