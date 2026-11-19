using UnityEngine;
using UnityEngine.UI;         
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement; 
using System.Collections;
using System.Collections.Generic; 

public class PlayerController : MonoBehaviour
{
    [Header("Grid Size Settings")]
    [SerializeField] private float gridSizeX = 1.0f; 
    [SerializeField] private float gridSizeY = 0.6f; 
    [SerializeField] private float moveDuration = 0.2f; 
    private bool isMoving = false;
    private Rigidbody2D rb;
    private Animator anim;
    private Transform characterSprite; 
    private bool isDead = false;

    [Header("Interaction & Facing Settings")]
    [SerializeField] private LayerMask interactableLayer; 
    private Vector2 facingDirection = Vector2.down; 

    [Header("Tilemap Reference Settings")]
    public Tilemap walkTilemap;      
    public Tilemap collisionTilemap; 
    public Tilemap map2Tilemap;      
    public Tilemap bombTilemap;        
    public GameObject numberPrefab;    

    [Header("Minesweeper Settings")]
    public TileBase bombTile;          
    public int totalBombs = 10;      
    public Vector2Int mapMinBound = new Vector2Int(-10, -10); 
    public Vector2Int mapMaxBound = new Vector2Int(10, 10);   

    [Tooltip("ระยะช่องในการขุดปกติและปักธงรอบสองเป็นต้นไป")]
    [Range(1, 5)]
    public int digRange = 1; 

    [Tooltip("ระยะรัศมีสี่เหลี่ยมรอบตัวที่จะเปิดพื้นที่ขุดกระจายในครั้งแรก (First Click)")]
    [Range(1, 5)]
    public int firstClickRevealRange = 1; 

    [Header("Difficulty Balance")]
    [Range(1, 8)]
    public int maxAllowedAdjacentBombs = 3;

    [Tooltip("ระยะห่างขั้นต่ำระหว่างระเบิดแต่ละลูก")]
    [Range(1, 4)]
    public int minBombDistance = 2;

    [Tooltip("ลากไฟล์รูปภาพระเบิดที่เป็น Sprite (2D) มาใส่ช่องนี้")]
    public Sprite bombSpriteVisual;

    [Header("Flag Settings (กดปุ่มค้างแล้วปล่อยเพื่อปักธง)")]
    [Tooltip("ลากไฟล์รูปภาพธง หรือวัตถุแจ้งเตือนระเบิดมาใส่ช่องนี้")]
    public Sprite flagSpriteVisual;
    [Tooltip("ระยะเวลา (วินาที) ที่ต้องกดปุ่มค้างไว้เพื่อเข้าสู่โหมดเตรียมปักธง")]
    public float requiredHoldTimeForFlagMode = 0.6f;
    private float flagHoldTimer = 0f;
    private KeyCode currentFlagHoldingKey = KeyCode.None;
    private bool isInFlagReadyMode = false;

    [Header("Empty Space Visual")]
    [Tooltip("ลากรูปภาพบล็อกดินโล่งๆ หรือพื้นหลังเปล่า (Sprite 2D) ที่ไม่มีตัวเลขมาใส่ช่องนี้")]
    public Sprite emptySpaceSprite;

    [Header("Text & Sprite Alignment")]
    public float spawnOffsetY = 0.2f;

    [Header("VFX Effects")]
    public GameObject rockBreakFXPrefab;
    public GameObject mineExplosionFXPrefab;

    [Header("Portal Unlock Settings")]
    public bool unlockPortalOnAllClear = true;
    public int customRequiredClearsToUnlockPortal = 5;

    [Header("Next Level Portal Settings")]
    public GameObject portalVFXPrefab;
    public Sprite portalSpriteVisual;

    [Header("Number Sprites Settings")]
    public Sprite[] numberSprites; 

    [Header("Platform Protection")]
    public List<string> protectedTileNames = new List<string> { "ชื่อTileแท่นหินของคุณ" };

    [Header("Animation Settings")]
    public string deathAnimationTrigger = "4_Death"; 

    // [Header("Audio Settings")] // [cite: 1] ลบ Local Audio ใน Player ออกให้ใช้ Manager กลาง
    // public AudioClip digSFX;
    // public AudioClip explosionSFX;
    // private AudioSource audioSource;

    [Header("Special Skill Settings (Q)")]
    public KeyCode specialSkillKey = KeyCode.Q;
    public bool requireFlagsClearedToUnlock = true;
    public int requiredCorrectFlagsToUnlock = 3;
    [Range(1, 3)]
    public int specialSkillCascadeRange = 1;

    [Header("Monster Spawning Settings")]
    public GameObject monsterPrefab;
    public int totalMonsters = 5;
    public float monsterChaseSpeed = 2.5f;

    [Header("Player Combat Settings")]
    public float playerAttackDamage = 10f;
    public float playerAttackRange = 1f;
    public float attackCooldown = 0.5f;
    private float nextAttackTime = 0f;

    [Header("Auto Attack (ตีมอนสเตอร์ที่อยู่ช่องข้างๆ อัตโนมัติ)")]
    public bool autoAttackMonsters = true;
    [Tooltip("ตีอัตโนมัติทุกกี่วินาที")]
    public float autoAttackInterval = 0.6f;
    [Tooltip("ดาเมจที่ระเบิดทำกับมอนสเตอร์ในรัศมี 1 ช่อง")]
    public float bombDamageToMonsters = 999f;
    private float nextAutoAttackTime = 0f;
    private Vector3 moveTargetPos;

    [Header("Double Tap Settings")]
    [Range(0.1f, 0.8f)]
    public float doubleTapTimeWindow = 0.4f;
    [Tooltip("ค่า METs ของการเล่นด่านถ้ำ (เดิน/ขุด/คิด สลับกัน) ใช้คำนวณแคลอรี่เมื่อด่านไม่มี StageConfig")]
    public float caveExerciseMETs = 3.5f;
    private float lastTapTime = -1f;
    private KeyCode lastTappedKey = KeyCode.None;

    [Header("Player Health System (UI Setup)")]
    public float maxHealth = 100f;
    private float currentHealth;
    public Image healthBarFillImage; 
    public HealthBarManager healthBarManager;

    [Header("UI Popups")]
    [Tooltip("ลาก GameObject หน้าต่าง SummaryPopup มาใส่ช่องนี้")]
    public SummaryPopup summaryPopup;
    [Tooltip("ลาก GameObject หน้าต่าง DeathPopup มาใส่ช่องนี้")]
    public DeathPopup deathPopup;

    private StageConfig stageConfig;
    private Vector3 initialPlayerSpawnPos;
    private Vector3Int initialPlayerSpawnCell;

    private int stepCount = 0;
    private int digCount = 0;
    private int flagCount = 0;
    private int attackCount = 0;

    private Vector2 lastMoveDirection = Vector2.down; 
    private bool isFirstClick = true;
    private HashSet<Vector3Int> revealedCells = new HashSet<Vector3Int>();
    private Dictionary<Vector3Int, GameObject> flaggedCells = new Dictionary<Vector3Int, GameObject>();
    private HashSet<Vector3Int> monsterCells = new HashSet<Vector3Int>(); 
    private HashSet<Vector3Int> mineCells = new HashSet<Vector3Int>();

    private List<GameObject> tutorialSpawnedObjects = new List<GameObject>();

    private int bombsClearedCount = 0; 
    private bool isPortalOpened = false;
    private bool isSkillQLocked = true; 
    
    private Vector3Int exitCellCoord; 
    private Vector3 portalWorldPos;
    private GameObject spawnedPortalObj; 
    private PortalTrigger currentPortalTrigger;

    public bool IsDead()
    {
        return isDead || isStageFinished;
    }

    private bool isStageFinished = false;
    private int monstersKilled = 0;
    private int explodedBombCount = 0;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        
        // [cite: 1] ลบ Setup Local Audio ออก
        /*
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        */

        if (transform.childCount > 0)
        {
            characterSprite = transform.GetChild(0);
        }

        if (walkTilemap == null) walkTilemap = GameObject.Find("map walk")?.GetComponent<Tilemap>();
        if (collisionTilemap == null) collisionTilemap = GameObject.Find("Collision")?.GetComponent<Tilemap>();
        if (collisionTilemap == null) collisionTilemap = GameObject.Find("Collission")?.GetComponent<Tilemap>();
        if (map2Tilemap == null) map2Tilemap = GameObject.Find("map 2")?.GetComponent<Tilemap>();
        if (bombTilemap == null) bombTilemap = GameObject.Find("BombMap")?.GetComponent<Tilemap>();

        if (bombTilemap != null) bombTilemap.ClearAllTiles();

        initialPlayerSpawnPos = transform.position;
        Tilemap activeMap = walkTilemap != null ? walkTilemap : (map2Tilemap != null ? map2Tilemap : collisionTilemap);
        if (activeMap != null)
        {
            initialPlayerSpawnCell = activeMap.WorldToCell(initialPlayerSpawnPos);
        }
        else
        {
            initialPlayerSpawnCell = new Vector3Int(Mathf.RoundToInt(initialPlayerSpawnPos.x / gridSizeX), Mathf.RoundToInt(initialPlayerSpawnPos.y / gridSizeY), 0);
        }

        if (summaryPopup == null)
        {
            summaryPopup = FindObjectOfType<SummaryPopup>(true);
        }
        if (deathPopup == null)
        {
            deathPopup = FindObjectOfType<DeathPopup>(true);
        }

        stageConfig = FindObjectOfType<StageConfig>();
        if (stageConfig != null)
        {
            totalBombs = stageConfig.totalTrapsInStage;
        }

        isSkillQLocked = requireFlagsClearedToUnlock;
        currentHealth = maxHealth;
        UpdateHealthBarUI();
    }

    void Update()
    {
        if (isDead || isStageFinished) return;

        // การ์ดสอนเล่น / หน้าหยุดเกมของงานแสดงเปิดอยู่: ไม่รับปุ่ม และยกเลิกการเหยียบค้างที่ค้างอยู่ (ไม่ให้ปักธงตอนปล่อยเท้า)
        if (ExhibitMode.InputBlocked)
        {
            isInFlagReadyMode = false;
            currentFlagHoldingKey = KeyCode.None;
            flagHoldTimer = 0f;
            lastTappedKey = KeyCode.None;
            return;
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            if (!isPortalOpened)
            {
                Debug.Log("<color=yellow>[Cheat] กดปุ่ม N เสกประตูทางออกสำเร็จ!</color>");
                bombsClearedCount = totalBombs + totalMonsters; 
                CheckPortalUnlockCondition();   
                CheckSpecialSkillUnlock();
            }
            return; 
        }

        // ปุ่มเดิน 4 ทิศ ตั้งค่าได้จากหน้า Option (GameInputBindings)
        if (GameInputBindings.GetDown(GameAction.MoveUp))
            facingDirection = Vector2.up;
        else if (GameInputBindings.GetDown(GameAction.MoveDown))
            facingDirection = Vector2.down;
        else if (GameInputBindings.GetDown(GameAction.MoveLeft))
            facingDirection = Vector2.left;
        else if (GameInputBindings.GetDown(GameAction.MoveRight))
            facingDirection = Vector2.right;

        if (Input.GetKeyDown(KeyCode.E))
        {
            InteractInFront();
        }

        if (isMoving) return;

        Vector2 input = Vector2.zero;
        if (GameInputBindings.GetDown(GameAction.MoveUp))
            input = Vector2.up;
        else if (GameInputBindings.GetDown(GameAction.MoveDown))
            input = Vector2.down;
        else if (GameInputBindings.GetDown(GameAction.MoveLeft))
            input = Vector2.left;
        else if (GameInputBindings.GetDown(GameAction.MoveRight))
            input = Vector2.right;

        if (input != Vector2.zero)
        {
            lastMoveDirection = input;
            facingDirection = input;

            Vector3 step = new Vector3(input.x * gridSizeX, input.y * gridSizeY, 0f);
            Vector3 targetPosition = transform.position + step;

            if (CanMove(targetPosition))
            {
                StartCoroutine(MoveRoutine(targetPosition));
            }
        }

        if (anim != null)
        {
            anim.SetBool("1_Move", isMoving);
        }

        CheckAndFaceNearestEnemy();
        TryAutoAttack();
        CheckHoldAndPressFlag();
        CheckDirectionDoubleTap();

        if (Input.GetKeyDown(specialSkillKey)) UseSpecialCascadeSkill();
    }

    public void TeleportToCell(Vector3Int cell, Vector2? faceDir = null)
    {
        if (walkTilemap != null)
        {
            transform.position = walkTilemap.GetCellCenterWorld(cell);
        }
        else
        {
            transform.position = new Vector3(cell.x * gridSizeX, cell.y * gridSizeY, 0f);
        }

        if (faceDir.HasValue)
        {
            facingDirection = faceDir.Value;
            lastMoveDirection = faceDir.Value;
            HandleNormalSpriteFacing();
        }
    }

    public void SpawnTutorialBombWithNumbers(Vector3Int bombCell, TileBase rockTileToSpawn)
    {
        revealedCells.Remove(bombCell);

        if (flaggedCells.ContainsKey(bombCell))
        {
            if (flaggedCells[bombCell] != null) Destroy(flaggedCells[bombCell]);
            flaggedCells.Remove(bombCell);
        }

        foreach (var obj in tutorialSpawnedObjects)
        {
            if (obj != null) Destroy(obj);
        }
        tutorialSpawnedObjects.Clear();

        if (!mineCells.Contains(bombCell)) mineCells.Add(bombCell);

        if (bombTilemap != null && bombTile != null)
            bombTilemap.SetTile(bombCell, bombTile);

        if (map2Tilemap != null && rockTileToSpawn != null)
            map2Tilemap.SetTile(bombCell, rockTileToSpawn);

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                
                Vector3Int neighbor = new Vector3Int(bombCell.x + x, bombCell.y + y, 0);

                if (map2Tilemap != null) map2Tilemap.SetTile(neighbor, null);
                if (collisionTilemap != null) collisionTilemap.SetTile(neighbor, null);

                if (numberPrefab != null && numberSprites != null && numberSprites.Length > 0)
                {
                    Tilemap activeMap = walkTilemap != null ? walkTilemap : map2Tilemap;
                    Vector3 worldPos = activeMap != null ? activeMap.GetCellCenterWorld(neighbor) : new Vector3(neighbor.x, neighbor.y, 0f);
                    worldPos.y += spawnOffsetY;

                    GameObject numObj = Instantiate(numberPrefab, worldPos, Quaternion.identity);
                    tutorialSpawnedObjects.Add(numObj);

                    SpriteRenderer sr = numObj.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sprite = numberSprites[0]; 
                        sr.color = Color.cyan;
                        sr.sortingOrder = 25; 
                    }
                }
            }
        }
    }

    private void InteractInFront()
    {
        Vector3 targetPos = transform.position + new Vector3(facingDirection.x * gridSizeX, facingDirection.y * gridSizeY, 0f);
        Vector2 boxSize = new Vector2(gridSizeX * 0.6f, gridSizeY * 0.6f);
        Collider2D hit = Physics2D.OverlapBox(targetPos, boxSize, 0f, interactableLayer);
        if (hit != null)
        {
        }
    }

    private bool CanMove(Vector3 targetPos)
    {
        if (walkTilemap != null)
        {
            Vector3Int targetCell = walkTilemap.WorldToCell(targetPos);

            bool hasWalkTile = walkTilemap.HasTile(targetCell);
            bool hasMap2Tile = (map2Tilemap != null && map2Tilemap.HasTile(targetCell));
            bool hasCollisionTile = (collisionTilemap != null && collisionTilemap.HasTile(targetCell));
            bool hasFlag = flaggedCells.ContainsKey(targetCell);

            bool isExitPortalTile = isPortalOpened && (targetCell == exitCellCoord);

            if ((!hasWalkTile && !isExitPortalTile) || hasMap2Tile || hasCollisionTile || hasFlag)
                return false;
        }

        // เดินทับช่องที่มีมอนสเตอร์ยืนอยู่ไม่ได้
        if (MonsterChaserAI.IsCellOccupied(WorldToGridCell(targetPos))) return false;

        Vector2 boxSize = new Vector2(gridSizeX * 0.6f, gridSizeY * 0.6f);
        Collider2D obstacleHit = Physics2D.OverlapBox(targetPos, boxSize, 0f, interactableLayer);
        
        if (obstacleHit != null && (obstacleHit.GetComponent<PortalTrigger>() != null || obstacleHit.GetComponentInParent<PortalTrigger>() != null))
        {
            return true;
        }

        return obstacleHit == null;
    }

    private IEnumerator MoveRoutine(Vector3 targetPos)
    {
        isMoving = true;
        moveTargetPos = targetPos;
        Vector3 startPos = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float percent = Mathf.Clamp01(elapsedTime / moveDuration);
            transform.position = Vector3.Lerp(startPos, targetPos, percent);
            yield return null;
        }

        transform.position = targetPos;

        // [cite: 1] เล่นเสียงเดินก้าวเท้า
        if (SoundManager.Instance != null) SoundManager.Instance.PlayWalkSFX();

        isMoving = false;
        
        stepCount++;
        if (stageConfig != null) stageConfig.RecordStep();

        CheckPortalStepTrigger();
    }

    private void CheckPortalStepTrigger()
    {
        if (!isPortalOpened) return;
        if (currentPortalTrigger != null && !currentPortalTrigger.IsArmed()) return;

        Tilemap activeMap = walkTilemap != null ? walkTilemap : (map2Tilemap != null ? map2Tilemap : collisionTilemap);
        if (activeMap != null)
        {
            Vector3Int currentCell = activeMap.WorldToCell(transform.position);
            if (currentCell == exitCellCoord)
            {
                ShowSummaryPopup();
                return;
            }
        }

        if (Vector3.Distance(transform.position, portalWorldPos) < Mathf.Max(gridSizeX, gridSizeY) * 0.5f)
        {
            ShowSummaryPopup();
        }
    }

    void CheckAndFaceNearestEnemy()
    {
        if (MonsterChaserAI.Active.Count == 0)
        {
            HandleNormalSpriteFacing();
            return;
        }

        float shortestDist = Mathf.Infinity;
        Transform nearestMonster = null;

        foreach (var m in MonsterChaserAI.Active)
        {
            if (m == null || m.IsMonsterDead()) continue;

            float dist = Vector2.Distance(transform.position, m.transform.position);
            if (dist < shortestDist && dist <= 5f) 
            {
                shortestDist = dist;
                nearestMonster = m.transform;
            }
        }

        if (nearestMonster == null)
        {
            HandleNormalSpriteFacing();
            return;
        }

        if (characterSprite != null)
        {
            if (!isMoving)
            {
                if (nearestMonster.position.x < transform.position.x)
                {
                    characterSprite.localScale = new Vector3(1, 1, 1);   
                }
                else
                {
                    characterSprite.localScale = new Vector3(-1, 1, 1);  
                }
            }
            else
            {
                HandleNormalSpriteFacing();
            }
        }
    }

    public void HandleNormalSpriteFacing()
    {
        if (characterSprite == null) return;

        if (lastMoveDirection.x > 0) 
        {
            characterSprite.localScale = new Vector3(-1, 1, 1); 
        }
        else if (lastMoveDirection.x < 0) 
        {
            characterSprite.localScale = new Vector3(1, 1, 1);  
        }
    }

    /// <summary>
    /// ช่วงเวลาเหยียบซ้ำเพื่อขุด/ตี: งานแสดงขยายเป็นอย่างน้อย 0.7 วินาที ให้เด็กและผู้สูงอายุเหยียบทัน
    /// (เหยียบซ้ำตอนไม่มีหินหรือมอนสเตอร์ข้างหน้าจะไม่มีผลอะไร จึงไม่กระทบการเดิน)
    /// </summary>
    public float DoubleTapWindow => ExhibitMode.On ? Mathf.Max(doubleTapTimeWindow, 0.7f) : doubleTapTimeWindow;

    void CheckDirectionDoubleTap()
    {
        KeyCode[] directionalKeys = GameInputBindings.MoveKeys;

        foreach (KeyCode key in directionalKeys)
        {
            if (Input.GetKeyDown(key))
            {
                if (isInFlagReadyMode) return;

                float timeSinceLastTap = Time.time - lastTapTime;

                if (key == lastTappedKey && timeSinceLastTap <= DoubleTapWindow)
                {
                    ExecuteActionAttackAndDig();
                    lastTapTime = -1f;
                    lastTappedKey = KeyCode.None;
                }
                else
                {
                    lastTappedKey = key;
                    lastTapTime = Time.time;
                }
                break;
            }
        }
    }

    void CheckHoldAndPressFlag()
    {
        KeyCode[] directionalKeys = GameInputBindings.MoveKeys;

        foreach (KeyCode key in directionalKeys)
        {
            if (Input.GetKey(key))
            {
                if (currentFlagHoldingKey == KeyCode.None || currentFlagHoldingKey == key)
                {
                    currentFlagHoldingKey = key;
                    flagHoldTimer += Time.deltaTime;

                    if (flagHoldTimer >= requiredHoldTimeForFlagMode)
                    {
                        isInFlagReadyMode = true;
                    }
                }
            }
            
            if (Input.GetKeyUp(key) && currentFlagHoldingKey == key)
            {
                if (isInFlagReadyMode)
                {
                    ToggleFlagInFront();
                    isInFlagReadyMode = false;
                }

                currentFlagHoldingKey = KeyCode.None;
                flagHoldTimer = 0f;
            }
        }

        if (!Input.anyKey)
        {
            if (!isInFlagReadyMode)
            {
                currentFlagHoldingKey = KeyCode.None;
                flagHoldTimer = 0f;
            }
        }
    }

    private bool TryGetDiggableRockInFront(out Vector3Int foundCell)
    {
        foundCell = Vector3Int.zero;
        Tilemap activeMap = map2Tilemap != null ? map2Tilemap : collisionTilemap;
        if (activeMap == null) return false;

        Vector3Int playerCell = activeMap.WorldToCell(transform.position);
        Vector3 roundedPlayerPos = activeMap.GetCellCenterWorld(playerCell);

        for (int r = 1; r <= digRange; r++)
        {
            Vector3 checkPosition = roundedPlayerPos + new Vector3(facingDirection.x * gridSizeX * r, facingDirection.y * gridSizeY * r, 0f);
            Vector3Int gridCell = activeMap.WorldToCell(checkPosition);

            bool hasTile = (map2Tilemap != null && map2Tilemap.HasTile(gridCell)) ||       
                           (collisionTilemap != null && collisionTilemap.HasTile(gridCell));

            if (hasTile)
            {
                if (IsTileProtected(gridCell) || flaggedCells.ContainsKey(gridCell))
                {
                    return false;
                }

                foundCell = gridCell;
                return true;
            }
        }

        return false;
    }

    private bool CheckEnemyInFront()
    {
        Tilemap activeMap = walkTilemap != null ? walkTilemap : (map2Tilemap != null ? map2Tilemap : collisionTilemap);
        if (activeMap == null) return false;

        Vector3Int playerCell = activeMap.WorldToCell(transform.position);
        Vector3 roundedPlayerPos = activeMap.GetCellCenterWorld(playerCell);
        Vector3 attackPosition = roundedPlayerPos + (Vector3)facingDirection * playerAttackRange;

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(attackPosition, 0.7f);
        foreach (var col in hitColliders)
        {
            if (col.CompareTag("Enemy") || col.GetComponentInParent<MonsterChaserAI>() != null || col.GetComponent<MonsterChaserAI>() != null)
            {
                return true;
            }
        }
        return false;
    }

    void ExecuteActionAttackAndDig()
    {
        if (Time.time < nextAttackTime) return;

        bool hasRock = TryGetDiggableRockInFront(out Vector3Int rockCell);
        bool hasEnemy = CheckEnemyInFront();

        if (!hasRock && !hasEnemy)
        {
            return;
        }

        if (anim != null) anim.SetTrigger("2_Attack");
        nextAttackTime = Time.time + attackCooldown; 

        if (hasEnemy)
        {
            PerformMeleeAttack();
        }

        if (hasRock)
        {
            ExecuteDigAt(rockCell);
        }
    }

    private void ExecuteDigAt(Vector3Int gridCell)
    {
        Tilemap activeMap = map2Tilemap != null ? map2Tilemap : collisionTilemap;
        if (activeMap == null) return;

        // [cite: 1] เล่นเสียงขุดหิน
        if (SoundManager.Instance != null) SoundManager.Instance.PlayDigSFX();

        digCount++; 
        if (stageConfig != null) stageConfig.RecordDig();

        Vector3Int bombGridCell = gridCell;
        if (bombTilemap != null)
        {
            bombGridCell = bombTilemap.WorldToCell(activeMap.GetCellCenterWorld(gridCell));
        }

        if (isFirstClick)
        {
            isFirstClick = false; 
            GenerateRandomBombs(bombGridCell);
            RevealCascade(gridCell);
        }
        else
        {
            RevealSingleCell(gridCell);
        }
    }

    void PerformMeleeAttack()
    {
        Tilemap activeMap = walkTilemap != null ? walkTilemap : (map2Tilemap != null ? map2Tilemap : collisionTilemap);
        if (activeMap == null) return;

        attackCount++; 
        if (stageConfig != null) stageConfig.RecordAttack();

        Vector3Int playerCell = activeMap.WorldToCell(transform.position);
        Vector3 roundedPlayerPos = activeMap.GetCellCenterWorld(playerCell);
        Vector3 attackPosition = roundedPlayerPos + (Vector3)lastMoveDirection * playerAttackRange;

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(attackPosition, 0.7f); 
        foreach (var collider in hitColliders)
        {
            MonsterChaserAI monster = collider.GetComponentInParent<MonsterChaserAI>(); 
            if (monster == null) monster = collider.GetComponent<MonsterChaserAI>();

            if (monster != null)
            {
                monster.TakeDamage(playerAttackDamage);
            }
        }
    }

    void GenerateRandomBombs(Vector3Int startCell)
    {
        if (map2Tilemap == null) return;

        if (bombTilemap != null) bombTilemap.ClearAllTiles();
        mineCells.Clear();
        monsterCells.Clear(); 

        List<Vector3Int> validRockCells = new List<Vector3Int>();
        BoundsInt bounds = map2Tilemap.cellBounds;

        foreach (var pos in bounds.allPositionsWithin)
        {
            if (map2Tilemap.HasTile(pos))
            {
                if (IsTileProtected(pos)) continue;

                if (Mathf.Abs(pos.x - startCell.x) <= 1 && Mathf.Abs(pos.y - startCell.y) <= 1)
                {
                    continue;
                }

                if (!validRockCells.Contains(pos))
                {
                    validRockCells.Add(pos);
                }
            }
        }

        int bombsPlaced = 0;
        int safetyNet = 0;
        List<Vector3Int> placedBombCells = new List<Vector3Int>();

        while (bombsPlaced < totalBombs && validRockCells.Count > 0 && safetyNet < 5000)
        {
            safetyNet++;
            int selectedIndex = Random.Range(0, validRockCells.Count);
            Vector3Int randomCell = validRockCells[selectedIndex];

            bool tooCloseToOtherBomb = false;
            foreach (Vector3Int placedBomb in placedBombCells)
            {
                if (Mathf.Abs(randomCell.x - placedBomb.x) < minBombDistance && 
                    Mathf.Abs(randomCell.y - placedBomb.y) < minBombDistance)
                {
                    tooCloseToOtherBomb = true;
                    break;
                }
            }

            if (tooCloseToOtherBomb)
            {
                validRockCells.RemoveAt(selectedIndex);
                continue; 
            }

            if (bombTilemap != null && bombTile != null)
            {
                bombTilemap.SetTile(randomCell, bombTile); 
            }
            mineCells.Add(randomCell); 
            
            bool isDensityValid = true;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector3Int checkCell = new Vector3Int(randomCell.x + x, randomCell.y + y, 0);
                    if (CountBombsAtGrid(checkCell) > maxAllowedAdjacentBombs)
                    {
                        isDensityValid = false;
                        break;
                    }
                }
                if (!isDensityValid) break;
            }

            if (isDensityValid)
            {
                bombsPlaced++;
                placedBombCells.Add(randomCell);
                validRockCells.RemoveAt(selectedIndex);
            }
            else
            {
                if (bombTilemap != null) bombTilemap.SetTile(randomCell, null);
                mineCells.Remove(randomCell);
                validRockCells.RemoveAt(selectedIndex);
            }
        }
        totalBombs = bombsPlaced;

        // มอนสเตอร์ใช้กติกาเดียวกับระเบิด: ตัวเลขนับรวมทั้งสองอย่าง จึงต้องไม่ชิดกันเกินและไม่ให้ตัวเลขเกินรูปที่มี
        int maxNumber = numberSprites != null && numberSprites.Length > 0 ? Mathf.Min(maxAllowedAdjacentBombs + 1, numberSprites.Length) : maxAllowedAdjacentBombs + 1;
        int monstersPlaced = 0;
        int monsterSafety = 0;
        while (monstersPlaced < totalMonsters && validRockCells.Count > 0 && monsterSafety < 5000)
        {
            monsterSafety++;
            int selectedIndex = Random.Range(0, validRockCells.Count);
            Vector3Int targetMonsterCell = validRockCells[selectedIndex];
            validRockCells.RemoveAt(selectedIndex);

            // ไม่ติดกับระเบิด/มอนสเตอร์ตัวอื่นโดยตรง (ระเบิดจะได้ไม่ระเบิดใส่ตอนมอนสเตอร์เพิ่งโผล่)
            if (CountBombsAtGrid(targetMonsterCell) > 0) continue;

            monsterCells.Add(targetMonsterCell);
            bool densityOk = true;
            for (int x = -1; x <= 1 && densityOk; x++)
                for (int y = -1; y <= 1; y++)
                {
                    if (CountBombsAtGrid(new Vector3Int(targetMonsterCell.x + x, targetMonsterCell.y + y, 0)) > maxNumber) { densityOk = false; break; }
                }

            if (densityOk) monstersPlaced++;
            else monsterCells.Remove(targetMonsterCell);
        }
        totalMonsters = monstersPlaced;
    }

    void UseSpecialCascadeSkill()
    {
        if (isSkillQLocked) return;

        Tilemap activeMap = map2Tilemap != null ? map2Tilemap : collisionTilemap;
        if (activeMap == null) return;

        Vector3Int playerCell = activeMap.WorldToCell(transform.position);
        Vector3 roundedPlayerPos = activeMap.GetCellCenterWorld(playerCell);
        
        Vector3 targetPosition = roundedPlayerPos + new Vector3(facingDirection.x * gridSizeX, facingDirection.y * gridSizeY, 0f);
        Vector3Int gridCell = activeMap.WorldToCell(targetPosition);

        bool hasTile = (map2Tilemap != null && map2Tilemap.HasTile(gridCell)) ||       
                       (collisionTilemap != null && collisionTilemap.HasTile(gridCell));

        if (hasTile)
        {
            if (IsTileProtected(gridCell)) return;
            if (flaggedCells.ContainsKey(gridCell)) return;

            if (isFirstClick)
            {
                isFirstClick = false;
                Vector3Int bombGridCell = bombTilemap != null ? bombTilemap.WorldToCell(activeMap.GetCellCenterWorld(gridCell)) : gridCell;
                GenerateRandomBombs(bombGridCell);
            }

            if (anim != null) anim.SetTrigger("2_Attack"); 

            // [cite: 1] เล่นเสียงขุดหินเมื่อใช้สกิล (เพราะขุดทีเดียวหลายช่อง)
            if (SoundManager.Instance != null) SoundManager.Instance.PlayDigSFX();

            for (int x = -specialSkillCascadeRange; x <= specialSkillCascadeRange; x++)
            {
                for (int y = -specialSkillCascadeRange; y <= specialSkillCascadeRange; y++)
                {
                    Vector3Int targetCell = new Vector3Int(gridCell.x + x, gridCell.y + y, gridCell.z);
                    if (IsTileProtected(targetCell)) continue;
                    if (flaggedCells.ContainsKey(targetCell)) continue; // ช่องที่ปักธงไว้ (ผนึกระเบิด/มอนสเตอร์) ไม่ถูกขุด

                    if ((collisionTilemap != null && collisionTilemap.HasTile(targetCell)) || 
                        (map2Tilemap != null && map2Tilemap.HasTile(targetCell)))
                    {
                        RevealSingleCell(targetCell);
                    }
                }
            }
        }
    }

    void RevealCascade(Vector3Int startCell)
    {
        Queue<Vector3Int> cellsToProcess = new Queue<Vector3Int>();
        cellsToProcess.Enqueue(startCell);

        while (cellsToProcess.Count > 0)
        {
            Vector3Int currentCell = cellsToProcess.Dequeue();

            if (revealedCells.Contains(currentCell) || IsTileProtected(currentCell)) continue;

            bool hasTile = (collisionTilemap != null && collisionTilemap.HasTile(currentCell)) ||       
                           (map2Tilemap != null && map2Tilemap.HasTile(currentCell));
            if (!hasTile) continue;

            RevealSingleCell(currentCell);

            int adjacentBombs = CountBombsAround(currentCell);

            if (adjacentBombs == 0)
            {
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0) continue;
                        Vector3Int neighbor = new Vector3Int(currentCell.x + x, currentCell.y + y, currentCell.z);
                        if (!revealedCells.Contains(neighbor))
                        {
                            cellsToProcess.Enqueue(neighbor);
                        }
                    }
                }
            }
        }
    }

    void ToggleFlagInFront()
    {
        Tilemap activeMap = map2Tilemap != null ? map2Tilemap : collisionTilemap;
        if (activeMap == null) return;

        Vector3Int playerCell = activeMap.WorldToCell(transform.position);
        Vector3 roundedPlayerPos = activeMap.GetCellCenterWorld(playerCell);
        
        Vector3 targetPosition = roundedPlayerPos + new Vector3(facingDirection.x * gridSizeX, facingDirection.y * gridSizeY, 0f);
        Vector3Int gridCell = activeMap.WorldToCell(targetPosition);
        bool hasTile = (map2Tilemap != null && map2Tilemap.HasTile(gridCell)) ||       
                       (collisionTilemap != null && collisionTilemap.HasTile(gridCell));

        if (hasTile && !revealedCells.Contains(gridCell))
        {
            Vector3 spawnPos = activeMap.GetCellCenterWorld(gridCell);
            bool hasTrapActual = mineCells.Contains(gridCell) || monsterCells.Contains(gridCell);

            if (flaggedCells.ContainsKey(gridCell))
            {
                GameObject flagObj = flaggedCells[gridCell];
                Destroy(flagObj); 
                flaggedCells.Remove(gridCell); 

                if (hasTrapActual && bombsClearedCount > 0) bombsClearedCount--;
            }
            else
            {
                if (numberPrefab != null && flagSpriteVisual != null)
                {
                    Vector3 adjustedFlagPos = spawnPos + new Vector3(0f, spawnOffsetY, 0f);
                    GameObject spawnedFlag = Instantiate(numberPrefab, adjustedFlagPos, Quaternion.identity);
                    SpriteRenderer spriteRender = spawnedFlag.GetComponent<SpriteRenderer>();

                    if (spriteRender != null)
                    {
                        spriteRender.sprite = flagSpriteVisual; 
                        spriteRender.sortingOrder = 26; 
                    }

                    // [cite: 1] เล่นเสียงปุ่มเมื่อปักธง
                    if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClickSFX();

                    flaggedCells.Add(gridCell, spawnedFlag);
                    
                    flagCount++; 
                    if (stageConfig != null) stageConfig.RecordFlag();

                    if (TutorialManager.Instance != null)
                    {
                        TutorialManager.Instance.OnFlagPlaced();
                    }

                    if (hasTrapActual)
                    {
                        bombsClearedCount++;
                        CheckSpecialSkillUnlock();
                        CheckPortalUnlockCondition(); 
                    }
                }
            }
        }
    }

    void RevealSingleCell(Vector3Int cell)
    {
        if (isDead || IsTileProtected(cell)) return;

        if (flaggedCells.ContainsKey(cell))
        {
            Destroy(flaggedCells[cell]);
            flaggedCells.Remove(cell);
        }

        bool alreadyRevealed = revealedCells.Contains(cell);
        if (!alreadyRevealed)
        {
            revealedCells.Add(cell);
        }

        Tilemap activeMap = map2Tilemap != null ? map2Tilemap : collisionTilemap;
        Vector3 spawnPos = activeMap != null ? activeMap.GetCellCenterWorld(cell) : transform.position;

        Vector3Int collisionGridCell = cell;
        Vector3Int map2GridCell = cell;
        Vector3Int bombGridCell = cell;

        if (collisionTilemap != null) collisionGridCell = collisionTilemap.WorldToCell(spawnPos);
        if (map2Tilemap != null) map2GridCell = map2Tilemap.WorldToCell(spawnPos);
        if (bombTilemap != null) bombGridCell = bombTilemap.WorldToCell(spawnPos);

        bool hasBomb = mineCells.Contains(cell);
        Vector3 adjustedSpawnPos = spawnPos + new Vector3(0f, spawnOffsetY, 0f);

        if (hasBomb)
        {
            TakeDamage(maxHealth * 0.5f); 

            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.OnTutorialBombDug(cell);
            }

            // [cite: 1] เล่นเสียงระเบิดผ่าน SoundManager กลาง (แก้ local call เดิม)
            if (SoundManager.Instance != null) SoundManager.Instance.PlayExplosionSFX();

            if (mineExplosionFXPrefab != null)
            {
                GameObject explodeFX = Instantiate(mineExplosionFXPrefab, spawnPos, Quaternion.identity);
                Destroy(explodeFX, 3.0f);
            }

            // แรงระเบิดกำจัดมอนสเตอร์ที่อยู่ในรัศมี 1 ช่อง (ล่อมอนสเตอร์มาโดนระเบิดได้)
            MonsterChaserAI.DamageInRadius(spawnPos, Mathf.Max(gridSizeX, gridSizeY) * 1.6f, bombDamageToMonsters);

            if (numberPrefab != null)
            {
                GameObject spawnedBomb = Instantiate(numberPrefab, adjustedSpawnPos, Quaternion.identity);
                SpriteRenderer spriteRender = spawnedBomb.GetComponent<SpriteRenderer>();
                if (spriteRender != null && bombSpriteVisual != null)
                {
                    spriteRender.sprite = bombSpriteVisual;
                    spriteRender.sortingOrder = 25; 
                }
            }

            if (collisionTilemap != null) collisionTilemap.SetTile(collisionGridCell, null);
            if (map2Tilemap != null) map2Tilemap.SetTile(map2GridCell, null);

            // ขุดโดนระเบิดไม่นับเป็นการเคลียร์ (นับเฉพาะธงที่ปักถูกและมอนสเตอร์ที่กำจัดได้)
            // แต่ระเบิดลูกนี้หายไปแล้ว จึงลดจำนวนที่ต้องเคลียร์ลง ไม่ให้ด่านค้างจนเปิดประตูไม่ได้
            explodedBombCount++;
            CheckPortalUnlockCondition();
            return;
        }
        else if (monsterCells.Contains(cell))
        {
            monsterCells.Remove(cell); 
            
            // ต้องเอาหินออกก่อน มอนสเตอร์จึงยืนบนช่องนี้ได้
            if (collisionTilemap != null) collisionTilemap.SetTile(collisionGridCell, null);
            if (map2Tilemap != null) map2Tilemap.SetTile(map2GridCell, null);

            if (rockBreakFXPrefab != null)
            {
                GameObject fx = Instantiate(rockBreakFXPrefab, spawnPos, Quaternion.identity);
                Destroy(fx, 3.0f);
            }

            if (monsterPrefab != null)
            {
                GameObject spawnedMonster = Instantiate(monsterPrefab, spawnPos, Quaternion.identity);
                var chaserAI = spawnedMonster.AddComponent<MonsterChaserAI>();
                chaserAI.Setup(this, monsterChaseSpeed);
            }
            else
            {
                // ไม่มี Prefab มอนสเตอร์: นับเป็นเคลียร์ทันทีเพื่อไม่ให้ด่านค้าง
                OnMonsterKilled(null);
            }

            // นับเป็นการเคลียร์ตอนกำจัดมอนสเตอร์ได้ (OnMonsterKilled) ไม่ใช่ตอนขุดเจอ
            return;
        }

        if (collisionTilemap != null) collisionTilemap.SetTile(collisionGridCell, null);
        if (map2Tilemap != null) map2Tilemap.SetTile(map2GridCell, null);

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnRockDestroyed();
        }

        int trapCount = CountBombsAround(map2GridCell);

        if (numberPrefab != null && !alreadyRevealed)
        {
            GameObject spawnedNumber = Instantiate(numberPrefab, adjustedSpawnPos, Quaternion.identity);
            SpriteRenderer spriteRender = spawnedNumber.GetComponent<SpriteRenderer>();
            
            if (spriteRender != null)
            {
                spriteRender.sortingOrder = 25; 
                
                if (trapCount == 0)
                {
                    if (emptySpaceSprite != null) spriteRender.sprite = emptySpaceSprite;
                    else spriteRender.sprite = null;
                }
                else 
                {
                    int spriteIndex = trapCount - 1; 
                    if (numberSprites != null && spriteIndex >= 0 && spriteIndex < numberSprites.Length)
                    {
                        spriteRender.sprite = numberSprites[spriteIndex];

                        if (trapCount == 1) spriteRender.color = Color.cyan;         
                        else if (trapCount == 2) spriteRender.color = Color.green;     
                        else if (trapCount == 3) spriteRender.color = Color.red;       
                        else spriteRender.color = new Color(0.6f, 0f, 0.8f);         
                    }
                }
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        // ระดับ "สบาย ๆ" (เลือกที่การ์ดสอนเล่น): โดนกับดัก/มอนสเตอร์เสียเลือดครึ่งเดียว
        if (ExhibitMode.Relaxed) amount *= 0.5f;
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateHealthBarUI();
        if (currentHealth > 0f) StartCoroutine(HurtFlashRoutine());

        if (currentHealth <= 0f)
        {
            PlayerDeath();
        }
    }

    void UpdateHealthBarUI()
    {
        if (healthBarFillImage != null)
        {
            healthBarFillImage.fillAmount = currentHealth / maxHealth;
        }

        if (healthBarManager != null)
        {
            healthBarManager.UpdateHealthBar(currentHealth, maxHealth);
        }
    }

    void DisableSPUM(GameObject rootObj)
    {
        MonoBehaviour[] allScripts = rootObj.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in allScripts)
        {
            if (script != null && script.GetType().Name.Contains("SPUM"))
            {
                Destroy(script); 
            }
        }
    }

    void PlayerDeath()
    {
        isDead = true;

        if (anim != null)
        {
            DisableSPUM(this.gameObject); 

            anim.SetBool("1_Move", false);
            anim.SetBool("isDeath", true); 
            
            if (!string.IsNullOrEmpty(deathAnimationTrigger))
            {
                anim.SetTrigger(deathAnimationTrigger); 
            }
            anim.speed = 1f; 

            Invoke("LockAnimator", 1.0f); 
        }

        // [cite: 1] หยุดเพลง BGM เมื่อตาย (แก้ local call เดิม)
        if (SoundManager.Instance != null) SoundManager.Instance.StopBGM();

        if (rb != null) 
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true; 
        }
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        StartCoroutine(ShowDeathPopupRoutine());
    }

    private IEnumerator ShowDeathPopupRoutine()
    {
        yield return new WaitForSeconds(1.2f);

        // ทุกด่านถ้ำใช้หน้าสรุปผลม้วนกระดาษชุดเดียวกัน พร้อมสรุปการเคลื่อนไหวทั้งหมด
        ResultPopupUI.Data data = BuildResultData();
        data.stars = 1; // เล่นจนจบได้ 1 ดาวเสมอ ให้กำลังใจ
        ResultPopupUI.Show(ResultPopupUI.Kind.GameOver, data,
            () => SceneManager.LoadScene("Mainmenu"),
            () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
    }

    /// <summary>รวบรวมข้อมูลของรอบนี้สำหรับหน้าสรุปผล (เวลา / เคลียร์ได้ / แคลอรี่ / การเคลื่อนไหวทั้งหมด)</summary>
    private ResultPopupUI.Data BuildResultData()
    {
        // สูตรเดียวกับทุกเกม: METs x เวลา x น้ำหนัก (ด่านที่มี StageConfig ใช้ค่าของด่านนั้น)
        float calories = stageConfig != null
            ? stageConfig.CalculateCalories()
            : ResultHelpers.CaloriesFromMets(caveExerciseMETs, Time.timeSinceLevelLoad);
        int totalTraps = Mathf.Max(0, totalBombs + totalMonsters);

        string stats = $"STEPS <color=#FFD36B>{stepCount}</color>    DIGS <color=#FFD36B>{digCount}</color>    " +
                       $"FLAGS <color=#FFD36B>{flagCount}</color>    ATTACKS <color=#FFD36B>{attackCount}</color>";
        if (totalMonsters > 0) stats += $"    MONSTERS <color=#FFD36B>{monstersKilled}/{totalMonsters}</color>";

        return new ResultPopupUI.Data
        {
            seconds = Time.timeSinceLevelLoad,
            middleValue = $"{Mathf.Min(bombsClearedCount, totalTraps)} / {totalTraps}",
            calories = calories,
            statsLine = stats
        };
    }

    void LockAnimator()
    {
        if (anim != null)
        {
            anim.speed = 0f; 
            anim.enabled = false; 
        }
    }

    void CheckSpecialSkillUnlock()
    {
        if (isSkillQLocked && requireFlagsClearedToUnlock)
        {
            if (bombsClearedCount >= requiredCorrectFlagsToUnlock)
            {
                isSkillQLocked = false;
            }
        }
    }

    void CheckPortalUnlockCondition()
    {
        if (isPortalOpened) return;

        // ด่านฝึกสอน: ไม่มีประตูทางออก/หน้าสรุปผล ใช้ WarpZone + ลูกศรนำทางที่ TutorialManager เปิดให้ตอนสอนเสร็จแทน
        if (TutorialManager.Instance != null) return;

        int maxClearable = Mathf.Max(0, totalBombs + totalMonsters - explodedBombCount);
        int targetRequirement = unlockPortalOnAllClear ? maxClearable : Mathf.Min(customRequiredClearsToUnlockPortal, maxClearable);
        
        if (bombsClearedCount >= targetRequirement)
        {
            isPortalOpened = true;

            exitCellCoord = initialPlayerSpawnCell;
            portalWorldPos = initialPlayerSpawnPos;

            if (collisionTilemap != null) collisionTilemap.SetTile(exitCellCoord, null);
            if (map2Tilemap != null) map2Tilemap.SetTile(exitCellCoord, null);
            
            GameObject portalObj = null;

            if (portalVFXPrefab != null)
            {
                portalObj = Instantiate(portalVFXPrefab, portalWorldPos, Quaternion.identity);
            }
            else if (numberPrefab != null && portalSpriteVisual != null)
            {
                portalObj = Instantiate(numberPrefab, portalWorldPos, Quaternion.identity);
                SpriteRenderer spriteRender = portalObj.GetComponent<SpriteRenderer>();
                if (spriteRender != null) 
                { 
                    spriteRender.sprite = portalSpriteVisual; 
                    spriteRender.sortingOrder = 50; 
                }
            }
            else
            {
                portalObj = new GameObject("ExitPortal");
                portalObj.transform.position = portalWorldPos;
            }

            spawnedPortalObj = portalObj;

            Renderer[] allRenderers = portalObj.GetComponentsInChildren<Renderer>();
            foreach (var r in allRenderers)
            {
                r.sortingOrder = 100;
            }

            BoxCollider2D portalCollider = portalObj.GetComponent<BoxCollider2D>();
            if (portalCollider == null) portalCollider = portalObj.AddComponent<BoxCollider2D>();
            portalCollider.isTrigger = true;
            portalCollider.size = new Vector2(gridSizeX * 0.7f, gridSizeY * 0.7f);

            PortalTrigger portalTrigger = portalObj.GetComponent<PortalTrigger>();
            if (portalTrigger == null) portalTrigger = portalObj.AddComponent<PortalTrigger>();
            portalTrigger.playerController = this;
            portalTrigger.ArmPortalWithDelay(0.6f);
            currentPortalTrigger = portalTrigger;

            Debug.Log("<color=green>[PlayerController] ประตูทางออกเสกตรงจุดเกิดแรกเริ่มที่: " + portalWorldPos + "</color>");
        }
    }

    public void ShowSummaryPopup()
    {
        if (isStageFinished || isDead) return;
        isStageFinished = true; // หยุดผู้เล่นและมอนสเตอร์ระหว่างหน้าสรุปผล

        // [cite: 1] เล่นเสียงปุ่มเมื่อจบด่าน (ขึ้น Popup)
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClickSFX();
        if (anim != null) anim.SetBool("1_Move", false);

        // มีด่านถ้ำถัดไป -> ปุ่มขวาเป็น Next / ด่านสุดท้าย -> Restart
        System.Action onNext = HasNextCaveStage() ? (System.Action)(() => OnClickNextButton()) : null;
        ResultPopupUI.Data data = BuildResultData();
        // ผ่านด่าน = 2 ดาว / เลือดเหลือ 70% ขึ้นไป = 3 ดาว
        data.stars = currentHealth >= maxHealth * 0.7f ? 3 : 2;
        ResultPopupUI.Show(ResultPopupUI.Kind.StageClear, data,
            () => SceneManager.LoadScene("Mainmenu"),
            () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex),
            onNext);
    }

    private bool HasNextCaveStage()
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next >= SceneManager.sceneCountInBuildSettings) return false;
        string path = SceneUtility.GetScenePathByBuildIndex(next);
        return path.Contains("/Cave");
    }

    public void OnClickNextButton()
    {
        Time.timeScale = 1f; 
        LoadNextSceneDirect();
    }

    private void LoadNextSceneDirect()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings) 
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else 
        {
            SceneManager.LoadScene(0); 
        }
    }

    #region Grid API & Monster Combat
    // ตารางหลักที่ใช้เดิน (ผู้เล่นและมอนสเตอร์ใช้พิกัดชุดเดียวกัน)
    private Tilemap GridMap => walkTilemap != null ? walkTilemap : (map2Tilemap != null ? map2Tilemap : collisionTilemap);

    /// <summary>ช่องที่ผู้เล่นยืนอยู่ (ระหว่างก้าวเดินนับเป็นช่องปลายทาง)</summary>
    public Vector3Int CurrentCell => WorldToGridCell(isMoving ? moveTargetPos : transform.position);

    public Vector3Int WorldToGridCell(Vector3 worldPos)
    {
        Tilemap map = GridMap;
        if (map != null) return map.WorldToCell(worldPos);
        return new Vector3Int(Mathf.RoundToInt(worldPos.x / gridSizeX), Mathf.RoundToInt(worldPos.y / gridSizeY), 0);
    }

    public Vector3 GridCellToWorld(Vector3Int cell)
    {
        Tilemap map = GridMap;
        if (map != null) return map.GetCellCenterWorld(cell);
        return new Vector3(cell.x * gridSizeX, cell.y * gridSizeY, 0f);
    }

    /// <summary>มอนสเตอร์เดินเข้าช่องนี้ได้หรือไม่ (ต้องเป็นพื้นทางเดิน ไม่มีหิน/ธง/ประตูทางออก)</summary>
    public bool IsCellOpenForMonster(Vector3Int cell)
    {
        Vector3 world = GridCellToWorld(cell);
        if (walkTilemap != null && !walkTilemap.HasTile(walkTilemap.WorldToCell(world))) return false;
        if (map2Tilemap != null && map2Tilemap.HasTile(map2Tilemap.WorldToCell(world))) return false;
        if (collisionTilemap != null && collisionTilemap.HasTile(collisionTilemap.WorldToCell(world))) return false;

        Tilemap rockMap = map2Tilemap != null ? map2Tilemap : collisionTilemap;
        if (rockMap != null && flaggedCells.ContainsKey(rockMap.WorldToCell(world))) return false;
        if (isPortalOpened && cell == WorldToGridCell(portalWorldPos)) return false;
        return true;
    }

    /// <summary>
    /// ตีมอนสเตอร์ที่อยู่ช่องข้างๆ (รวมแนวทแยง) โดยอัตโนมัติ ผู้เล่นมีหน้าที่ก้าวหลบ/หาตำแหน่ง ไม่ต้องกดโจมตีเอง
    /// </summary>
    void TryAutoAttack()
    {
        if (!autoAttackMonsters || isMoving || Time.time < nextAutoAttackTime) return;
        if (MonsterChaserAI.Active.Count == 0) return;

        Vector3Int me = CurrentCell;
        MonsterChaserAI target = null;
        float best = float.MaxValue;
        foreach (MonsterChaserAI m in MonsterChaserAI.Active)
        {
            if (m == null || m.IsMonsterDead()) continue;
            Vector3Int c = m.Cell;
            if (c == me || Mathf.Abs(c.x - me.x) > 1 || Mathf.Abs(c.y - me.y) > 1) continue;
            float d = (m.transform.position - transform.position).sqrMagnitude;
            if (d < best) { best = d; target = m; }
        }
        if (target == null) return;

        nextAutoAttackTime = Time.time + autoAttackInterval;

        if (characterSprite != null && Mathf.Abs(target.transform.position.x - transform.position.x) > 0.05f)
        {
            characterSprite.localScale = new Vector3(target.transform.position.x < transform.position.x ? 1 : -1, 1, 1);
        }
        if (anim != null) anim.SetTrigger("2_Attack");
        if (SoundManager.Instance != null) SoundManager.Instance.PlayDigSFX();

        attackCount++;
        if (stageConfig != null) stageConfig.RecordAttack();

        target.TakeDamage(playerAttackDamage);
    }

    /// <summary>มอนสเตอร์ถูกกำจัด: นับเป็นการเคลียร์ 1 จุด</summary>
    public void OnMonsterKilled(MonsterChaserAI monster)
    {
        if (isDead) return;
        monstersKilled++;
        bombsClearedCount++;
        CheckSpecialSkillUnlock();
        CheckPortalUnlockCondition();
    }

    private bool isHurtFlashing;

    private IEnumerator HurtFlashRoutine()
    {
        if (isHurtFlashing) yield break;
        isHurtFlashing = true;
        SpriteRenderer[] srs = GetComponentsInChildren<SpriteRenderer>();
        Color[] original = new Color[srs.Length];
        for (int i = 0; i < srs.Length; i++) original[i] = srs[i].color;
        for (int i = 0; i < srs.Length; i++) srs[i].color = original[i] * new Color(1f, 0.4f, 0.4f, 1f);
        yield return new WaitForSeconds(0.15f);
        for (int i = 0; i < srs.Length; i++) if (srs[i] != null) srs[i].color = original[i];
        isHurtFlashing = false;
    }
    #endregion

    int CountBombsAround(Vector3Int targetCell)
    {
        int count = 0;
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue; 
                Vector3Int checkCell = new Vector3Int(targetCell.x + x, targetCell.y + y, targetCell.z);
                if (mineCells.Contains(checkCell) || monsterCells.Contains(checkCell)) count++;
            }
        }
        return count;
    }

    private int CountBombsAtGrid(Vector3Int bombGridCell)
    {
        int count = 0; 
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                Vector3Int target = new Vector3Int(bombGridCell.x + x, bombGridCell.y + y, 0);
                if (mineCells.Contains(target) || monsterCells.Contains(target)) count++;
            }
        }
        return count;
    }

    private bool IsProtectedTile(Tilemap tilemap, Vector3Int cell)
    {
        if (tilemap == null) return false;
        TileBase tile = tilemap.GetTile(cell); if (tile == null) return false;
        string tileName = tile.name.ToLower();
        foreach (string protectedName in protectedTileNames)
        {
            if (tileName.Contains(protectedName.ToLower())) return true;
        }
        return false;
    }

    private bool IsTileProtected(Vector3Int cell) { return IsProtectedTile(collisionTilemap, cell) || IsProtectedTile(map2Tilemap, cell); }
}

public class PortalTrigger : MonoBehaviour
{
    [HideInInspector]
    public PlayerController playerController;
    private bool isArmed = false;

    public bool IsArmed()
    {
        return isArmed;
    }

    public void ArmPortalWithDelay(float delay)
    {
        isArmed = false;
        StartCoroutine(ArmRoutine(delay));
    }

    private IEnumerator ArmRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        isArmed = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isArmed) return;
        TriggerPortalAction(collision.gameObject);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!isArmed) return;
        TriggerPortalAction(collision.gameObject);
    }

    private void TriggerPortalAction(GameObject targetObj)
    {
        if (targetObj.CompareTag("Player") || targetObj.GetComponentInParent<PlayerController>() != null || targetObj.GetComponent<PlayerController>() != null)
        {
            if (playerController != null)
            {
                playerController.ShowSummaryPopup();
            }
            else
            {
                PlayerController pc = FindObjectOfType<PlayerController>();
                if (pc != null) pc.ShowSummaryPopup();
            }
        }
    }
}
