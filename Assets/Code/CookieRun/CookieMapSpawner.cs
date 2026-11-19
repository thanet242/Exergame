using System.Collections.Generic;
using UnityEngine;

namespace CookieRun
{
    /// <summary>
    /// ระบบสร้างฉากแบบสุ่มต่อเนื่อง (Chunk Spawner) สำหรับเกมวิ่งไม่รู้จบแบบ Cookie Run
    /// </summary>
    public class CookieMapSpawner : MonoBehaviour
    {
        [Header("Player Tracking")]
        [SerializeField] private Transform playerTransform;

        [Header("Chunk Prefabs")]
        [Tooltip("Prefab ชิ้นส่วนฉากเริ่มต้น (เป็นพื้นเรียบ ปลอดภัย ไม่มีกับดัก มีเฉพาะเหรียญ)")]
        [SerializeField] private GameObject startChunkPrefab;

        [Tooltip("จำนวน Chunk เริ่มต้นที่ปลอดภัย (Chunk_Start) เพื่อให้ผู้เล่นวิ่งเก็บเหรียญและตั้งตัวก่อนเจอสิ่งกีดขวาง")]
        [SerializeField] private int safeStartChunkCount = 2;

        [Tooltip("Prefab เหรียญ 2D สำหรับสร้างบน InitialGround ช่วงเริ่มเกม")]
        [SerializeField] private GameObject starterCoinPrefab;

        [Tooltip("Prefab ชิ้นส่วนฉากสุ่มระหว่างเล่น (มีพื้น เหรียญ เยลลี่ สิ่งกีดขวาง)")]
        [SerializeField] private GameObject[] chunkPrefabs;

        [System.Serializable]
        public class LevelChunk
        {
            public GameObject prefab;
            [Tooltip("ปลดล็อกเมื่อถึงระดับความยากนี้ (ดูใน CookieGameManager)")]
            public int unlockLevel = 2;
        }

        [Tooltip("Chunk ที่ปลดล็อกตามระดับความยาก (ครั้งแรกที่ปลดล็อกจะถูกส่งออกมาทันที เพื่อแนะนำสิ่งกีดขวางใหม่)")]
        [SerializeField] private LevelChunk[] levelChunks;

        [System.Serializable]
        public class GenObstacle
        {
            public GameObject prefab;
            [Tooltip("true = สิ่งกีดขวางด้านบน (สไลด์ลอด), false = บนพื้น (กระโดดข้าม)")]
            public bool overhead;
            [Tooltip("เริ่มโผล่ในฉากสุ่มตั้งแต่ระดับนี้")]
            public int unlockLevel = 1;
        }

        [Header("Generated Chunks (ฉากสุ่มสร้าง: หลุม + สิ่งกีดขวาง)")]
        [Tooltip("เริ่มมีฉากสุ่มสร้างตั้งแต่ระดับนี้")]
        [SerializeField] private int generatedUnlockLevel = 2;
        [Tooltip("สิ่งกีดขวางที่วางบนพื้นยาวระหว่างหลุม")]
        [SerializeField] private GenObstacle[] genObstacles;
        [Tooltip("ความกว้างหลุม (เมตร) ห้ามกว้างเกิน 3 แผ่นเหยียบจะกระโดดไม่ถึง")]
        [SerializeField] private Vector2 pitWidthRange = new Vector2(2.5f, 3.0f);
        [Tooltip("พื้นสั้นสุดระหว่างหลุม (เมตร) ให้ผู้เล่นลงพื้นแล้วมีเวลาเหยียบครั้งต่อไปทัน")]
        [SerializeField] private float minPlatform = 5.5f;
        [Tooltip("ด่านหลังๆ โอกาสเจอฉากที่มีหลุมเพิ่มขึ้นระดับละเท่านี้ (เทียบกับฉากปกติ 1 เท่า)")]
        [SerializeField] private float pitWeightPerLevel = 0.6f;

        [Header("Spawn Settings")]
        [Tooltip("ความยาวเฉลี่ยของ Chunk แต่ละชิ้น (เมตร/ยูนิต)")]
        [SerializeField] private float defaultChunkWidth = 26f;

        [Tooltip("ระยะเผื่อล่วงหน้าที่ต้องสร้างฉากดักหน้าผู้เล่น")]
        [SerializeField] private float spawnAheadDistance = 35f;

        [Tooltip("ระยะด้านหลังผู้เล่นที่ชิ้นส่วนฉากจะถูกทำลาย")]
        [SerializeField] private float destroyBehindDistance = 25f;

        [Tooltip("จำนวน Chunk ที่ให้เกิดตอนเริ่มต้นเกม")]
        [SerializeField] private int initialChunkCount = 4;

        [Header("Heart Pickup (หัวใจฟื้นฟู)")]
        [Tooltip("ไอเทมหัวใจ (Item_Potion) จะโผล่เฉพาะตอนที่หัวใจไม่เต็ม")]
        [SerializeField] private GameObject heartPickupPrefab;
        [Tooltip("ระยะห่างขั้นต่ำระหว่างหัวใจแต่ละดวง (เมตร)")]
        [SerializeField] private float heartSpawnInterval = 180f;
        [Tooltip("ห่างจากสิ่งกีดขวางอย่างน้อยกี่เมตร")]
        [SerializeField] private float heartObstacleClearance = 2.5f;

        [Header("Pre-Game Tutorial")]
        [Tooltip("เปิดช่วงสอนเล่นก่อนเริ่มเกม (จะไม่มีเหรียญให้เก็บบนพื้นเริ่มต้น)")]
        [SerializeField] private bool hasPreGameTutorial = true;

        private float nextSpawnX = 0f;
        private bool hasSpawnedFirstObstacleChunk = false;
        private readonly List<GameObject> activeChunks = new List<GameObject>();
        private readonly Dictionary<GameObject, float> chunkWidthCache = new Dictionary<GameObject, float>();
        private readonly List<GameObject> spawnCandidates = new List<GameObject>();
        private readonly Queue<GameObject> introQueue = new Queue<GameObject>();
        private int introducedLevel = 1;
        private CookieHealth playerHealth;
        private float lastHeartSpawnX;

        private void Start()
        {
            if (playerTransform == null)
            {
                CookiePlayerController player = FindObjectOfType<CookiePlayerController>();
                if (player != null) playerTransform = player.transform;
            }
            if (playerTransform != null) playerHealth = playerTransform.GetComponent<CookieHealth>();

            // ตรวจสอบและดึง Prefab อัตโนมัติหากยังไม่ได้ Assign ใน Inspector
            ResolveMissingPrefabs();
            PrepareGroundTemplate();

            // คำนวณจุดเริ่มต้นการสร้างฉากอย่างปลอดภัย
            // ตรวจหา InitialGround ใน Scene เพื่อให้การต่อฉากปลอดภัย 100% ไม่ทับซ้อนจุดเกิดผู้เล่น
            float groundRightEdge = 0f;
            GameObject initialGround = GameObject.Find("InitialGround");
            if (initialGround != null)
            {
                BoxCollider2D col = initialGround.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    groundRightEdge = initialGround.transform.position.x + (col.size.x * 0.5f);
                }
                else
                {
                    groundRightEdge = initialGround.transform.position.x + 15f;
                }
            }

            // จุดเริ่มสร้างฉากต่อ ต้องอยู่หลัง InitialGround เสมอ (อย่างน้อย 30 เมตร)
            nextSpawnX = Mathf.Max(transform.position.x, groundRightEdge);
            if (nextSpawnX < 30f)
            {
                nextSpawnX = 30f;
            }

            // วางเหรียญบน InitialGround (หากเปิดโหมดสอนเล่น จะไม่มีเหรียญบนพื้นเริ่มต้นตามที่กำหนด)
            if (!hasPreGameTutorial)
            {
                SpawnStarterCoinsOnInitialGround();
            }

            // สร้าง Chunk เริ่มต้นที่ปลอดภัย (มีแต่เหรียญ ไม่มีสิ่งกีดขวาง) ตามจำนวนที่กำหนด (เช่น 2 Chunks = 50 เมตร)
            for (int i = 0; i < safeStartChunkCount; i++)
            {
                SpawnStartChunk();
            }

            // สร้าง Chunk สุ่มล่วงหน้า
            for (int i = 0; i < initialChunkCount; i++)
            {
                SpawnRandomChunk();
            }
        }

        private void ResolveMissingPrefabs()
        {
#if UNITY_EDITOR
            if (startChunkPrefab == null)
            {
                startChunkPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CookieRunGame/Prefabs/Chunk_Start.prefab");
            }
            if (starterCoinPrefab == null)
            {
                starterCoinPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CookieRunGame/Prefabs/Item_Coin2D.prefab");
            }
            if (heartPickupPrefab == null)
            {
                heartPickupPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CookieRunGame/Prefabs/Item_Potion.prefab");
            }
            if (genObstacles == null || genObstacles.Length == 0)
            {
                GenObstacle Load(string name, bool overhead, int unlock) => new GenObstacle
                {
                    prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/CookieRunGame/Prefabs/{name}.prefab"),
                    overhead = overhead,
                    unlockLevel = unlock
                };
                genObstacles = new[]
                {
                    Load("Obstacle_Rock", false, 1), Load("Obstacle_Stalagmites", false, 1), Load("Obstacle_Stalactite", true, 1),
                    Load("Obstacle_Mushroom", false, 6), Load("Obstacle_HangingRoots", true, 7), Load("Obstacle_CrystalSpire", false, 8),
                };
            }
#endif
        }

        private void SpawnStarterCoinsOnInitialGround()
        {
            if (starterCoinPrefab == null) return;

            // วางแนวเหรียญตั้งแต่ x = 5m ถึง x = 27m ห่างกันทุก 2m บนพื้น InitialGround
            for (float x = 5f; x <= 27f; x += 2f)
            {
                GameObject coin = CookiePool.Spawn(starterCoinPrefab, new Vector3(x, 0.8f, 0f), Quaternion.identity);
                activeChunks.Add(coin);
            }
        }

        private void Update()
        {
            if (playerTransform == null) return;

            // ตรวจสอบว่าต้องสร้าง Chunk ใหม่ข้างหน้าหรือยัง
            if (nextSpawnX - playerTransform.position.x < spawnAheadDistance)
            {
                SpawnRandomChunk();
            }

            // ทำลาย Chunk ที่อยู่ข้างหลังผู้เล่นไกลเกินไป
            RemoveOldChunks();
        }

        private void SpawnStartChunk()
        {
            GameObject prefab = startChunkPrefab;
            if (prefab == null)
            {
                // หากไม่พบ Prefab เริ่มต้น ให้ขยับระยะปลอดภัยไปข้างหน้า ห้ามนำ Chunk ที่มีสิ่งกีดขวางมาวางเด็ดขาด
                nextSpawnX += defaultChunkWidth;
                return;
            }

            GameObject chunk = CookiePool.Spawn(prefab, new Vector3(nextSpawnX, transform.position.y, 0f), Quaternion.identity);
            activeChunks.Add(chunk);
            CookieZoneManager.RegisterChunk(chunk); // ย้อมสีพื้นตามโซนปัจจุบัน

            nextSpawnX += GetChunkWidth(prefab, chunk);
        }

        public static CookieMapSpawner Instance { get; private set; }

        private System.Action<float, float> pendingSafeZone;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// ขอช่วงพื้นเรียบปลอดภัย (ไม่มีหลุม/สิ่งกีดขวาง) เป็นฉากถัดไป สำหรับวางประตูเปลี่ยนโซน
        /// onReady(ตำแหน่ง X ของประตู, ระดับพื้น) ถูกเรียกตอนฉากนั้นถูกสร้าง / คืนค่า false ถ้าไม่มีฉากปลอดภัยให้ใช้
        /// </summary>
        public bool RequestSafeZone(System.Action<float, float> onReady)
        {
            if (startChunkPrefab == null) return false;
            pendingSafeZone = onReady;
            return true;
        }

        private void SpawnRandomChunk()
        {
            if (chunkPrefabs == null || chunkPrefabs.Length == 0) return;

            if (pendingSafeZone != null)
            {
                // ประตูอยู่ค่อนไปทางท้ายพื้นเรียบ: ก่อนถึงประตูมีพื้นโล่งยาว ผู้เล่นวิ่งผ่านได้สบาย
                float safeStart = nextSpawnX;
                SpawnStartChunk();
                System.Action<float, float> onReady = pendingSafeZone;
                pendingSafeZone = null;
                onReady(safeStart + (nextSpawnX - safeStart) * 0.6f, transform.position.y);
                return;
            }

            GameObject prefab;
            QueueNewlyUnlockedChunks();

            if (!hasSpawnedFirstObstacleChunk)
            {
                // Chunk แรกหลังจากช่วงปลอดภัย แนะนำให้เป็น Chunk_Jumps (กระโดดข้ามหินระดับง่าย) เพื่อความนุ่มนวล
                prefab = chunkPrefabs[0] != null ? chunkPrefabs[0] : chunkPrefabs[Random.Range(0, chunkPrefabs.Length)];
                hasSpawnedFirstObstacleChunk = true;
            }
            else if (introQueue.Count > 0)
            {
                // เพิ่งขึ้นระดับใหม่: ส่ง Chunk ที่มีสิ่งกีดขวางแบบใหม่ออกมาก่อน
                prefab = introQueue.Dequeue();
            }
            else
            {
                prefab = PickRandomUnlockedChunk();
            }

            // ไม่มี prefab = สุ่มสร้างฉากหลุมขึ้นเอง
            if (prefab == null && groundTemplate != null)
            {
                SpawnGeneratedChunk();
                return;
            }
            if (prefab == null) prefab = System.Array.Find(chunkPrefabs, p => p != null);
            if (prefab == null) return;

            GameObject chunk = CookiePool.Spawn(prefab, new Vector3(nextSpawnX, transform.position.y, 0f), Quaternion.identity);
            activeChunks.Add(chunk);
            CookieZoneManager.RegisterChunk(chunk); // ย้อมสีพื้นตามโซนปัจจุบัน

            float width = GetChunkWidth(prefab, chunk);
            TrySpawnHeart(chunk, nextSpawnX, width);
            nextSpawnX += width;
        }

        /// <summary>
        /// วางหัวใจฟื้นฟูบนพื้นที่ปลอดภัยของ Chunk (มีพื้นรองรับ และห่างจากสิ่งกีดขวาง) เฉพาะตอนหัวใจไม่เต็ม
        /// </summary>
        private void TrySpawnHeart(GameObject chunk, float startX, float width)
        {
            if (heartPickupPrefab == null || playerHealth == null) return;
            if (playerHealth.IsDead || playerHealth.Hearts >= playerHealth.MaxHearts) return;
            if (startX - lastHeartSpawnX < heartSpawnInterval) return;

            CookieObstacle[] obstacles = chunk.GetComponentsInChildren<CookieObstacle>();
            BoxCollider2D[] colliders = chunk.GetComponentsInChildren<BoxCollider2D>();

            for (int attempt = 0; attempt < 12; attempt++)
            {
                float x = startX + Random.Range(3f, Mathf.Max(3.5f, width - 3f));
                if (!TryGetGroundTop(colliders, x, out float groundTop)) continue;

                bool clear = true;
                foreach (CookieObstacle o in obstacles)
                {
                    if (Mathf.Abs(o.transform.position.x - x) < heartObstacleClearance) { clear = false; break; }
                }
                if (!clear) continue;

                GameObject heart = CookiePool.Spawn(heartPickupPrefab, new Vector3(x, groundTop + 1f, 0f), Quaternion.identity);
                activeChunks.Add(heart);
                lastHeartSpawnX = x;
                return;
            }
        }

        private static bool TryGetGroundTop(BoxCollider2D[] colliders, float x, out float top)
        {
            top = float.MinValue;
            foreach (BoxCollider2D b in colliders)
            {
                if (b.isTrigger || !b.gameObject.name.Contains("Ground")) continue;
                Vector2 center = b.transform.TransformPoint(b.offset);
                Vector2 half = Vector2.Scale(b.size, b.transform.lossyScale) * 0.5f;
                // ต้องมีพื้นยื่นเลยจุดวางทั้งสองข้าง (ไม่วางตรงขอบหลุม)
                if (Mathf.Abs(x - center.x) <= Mathf.Abs(half.x) - 1f) top = Mathf.Max(top, center.y + Mathf.Abs(half.y));
            }
            return top > float.MinValue;
        }

        private int CurrentLevel => CookieGameManager.Instance != null ? CookieGameManager.Instance.CurrentLevel : 1;

        private void QueueNewlyUnlockedChunks()
        {
            int level = CurrentLevel;
            if (level <= introducedLevel || levelChunks == null) return;

            foreach (LevelChunk entry in levelChunks)
            {
                if (entry.prefab != null && entry.unlockLevel > introducedLevel && entry.unlockLevel <= level)
                {
                    introQueue.Enqueue(entry.prefab);
                }
            }
            introducedLevel = level;
        }

        // สุ่มแบบถ่วงน้ำหนัก: ด่านหลังๆ ฉากที่มีหลุม (รวมฉากสุ่มสร้าง) โผล่บ่อยขึ้น / คืนค่า null = ใช้ฉากสุ่มสร้าง
        private GameObject PickRandomUnlockedChunk()
        {
            spawnCandidates.Clear();
            spawnCandidates.AddRange(chunkPrefabs);

            int level = CurrentLevel;
            if (levelChunks != null)
            {
                foreach (LevelChunk entry in levelChunks)
                {
                    if (entry.prefab != null && entry.unlockLevel <= level) spawnCandidates.Add(entry.prefab);
                }
            }

            // ระดับ "สบาย ๆ": หลุมเพิ่มขึ้นช้ากว่าปกติมาก
            float pitWeight = 1f + pitWeightPerLevel * (ExhibitMode.Relaxed ? 0.25f : 1f) * (level - 1);
            float total = 0f;
            foreach (GameObject c in spawnCandidates) total += ChunkWeight(c, pitWeight);
            bool generated = groundTemplate != null && level >= generatedUnlockLevel;
            // ฉากสุ่มสร้างนับเป็นฉากหลุม 2 แบบ (มีหลายหลุมและสิ่งกีดขวางใหม่)
            float genWeight = generated ? pitWeight * 2f : 0f;

            float r = Random.value * (total + genWeight);
            foreach (GameObject c in spawnCandidates)
            {
                r -= ChunkWeight(c, pitWeight);
                if (r <= 0f) return c;
            }
            return generated ? null : spawnCandidates[spawnCandidates.Count - 1];
        }

        private static float ChunkWeight(GameObject prefab, float pitWeight)
        {
            if (prefab == null) return 0f;
            return prefab.name.Contains("Pit") ? pitWeight : 1f;
        }

        #region Generated Chunks
        private GameObject groundTemplate;
        private readonly Stack<GameObject> groundPool = new Stack<GameObject>();
        private readonly HashSet<GameObject> generatedGround = new HashSet<GameObject>();
        private readonly List<GameObject> obstacleChoices = new List<GameObject>();

        // ใช้ GroundBlock จาก Chunk ที่มีอยู่เป็นแม่แบบพื้น (รูป สี Layer และ Collider เหมือนกันทุกอย่าง)
        private void PrepareGroundTemplate()
        {
            if (chunkPrefabs == null) return;
            foreach (GameObject chunk in chunkPrefabs)
            {
                if (chunk == null) continue;
                foreach (Transform child in chunk.transform)
                {
                    if (!child.name.StartsWith("GroundBlock")) continue;
                    SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                    if (sr == null || sr.drawMode == SpriteDrawMode.Simple || child.GetComponent<BoxCollider2D>() == null) continue;

                    groundTemplate = Instantiate(child.gameObject, transform);
                    groundTemplate.name = "GroundBlock";
                    groundTemplate.SetActive(false);
                    return;
                }
            }
        }

        /// <summary>
        /// สร้างฉากหลุมแบบสุ่ม: พื้นสลับหลุมหลายช่อง พื้นยาวมีสิ่งกีดขวาง เหรียญโค้งข้ามหลุม
        /// จำนวนหลุมเพิ่มตามระดับ แต่หลุมกว้างไม่เกินที่กำหนดและมีพื้นพักระหว่างหลุมเสมอ
        /// </summary>
        private void SpawnGeneratedChunk()
        {
            int level = CurrentLevel;
            float startX = nextSpawnX;
            float length = Random.Range(26f, 32f);
            int pits = Mathf.Clamp(1 + (level - generatedUnlockLevel + 1) / 2 + Random.Range(0, 2), 1, 4);

            float x = 0f;
            float firstPlatform = Random.Range(4f, 6f);
            AddGround(startX, x, firstPlatform);
            AddCoinLine(startX, x + 1f, x + firstPlatform - 1f, float.NaN);
            x += firstPlatform;

            for (int i = 0; i < pits && length - x > minPlatform + pitWidthRange.y; i++)
            {
                float gap = Random.Range(pitWidthRange.x, pitWidthRange.y);
                AddCoinArc(startX, x, gap);
                x += gap;

                // พื้นถัดไป: ถ้ายาวพอ (ด่านหลังๆ) วางสิ่งกีดขวางตรงกลาง ห่างขอบหลุมทั้งสองด้าน
                bool last = i == pits - 1;
                float remaining = length - x;
                float platform = last ? remaining : Mathf.Min(remaining - minPlatform - pitWidthRange.y, Random.Range(minPlatform, minPlatform + 5f));
                platform = Mathf.Max(platform, minPlatform);

                float obstacleX = float.NaN;
                if (platform >= 9f && level >= generatedUnlockLevel + 1 && Random.value < 0.75f)
                {
                    obstacleX = x + platform * 0.5f;
                    SpawnGenObstacle(startX + obstacleX, level);
                }

                AddGround(startX, x, platform);
                AddCoinLine(startX, x + 1.2f, x + platform - 1.2f, obstacleX);
                x += platform;
            }

            if (x < length)
            {
                AddGround(startX, x, length - x);
                x = length;
            }

            nextSpawnX = startX + x;
        }

        private void AddGround(float chunkX, float localX, float width)
        {
            if (groundTemplate == null || width <= 0.01f) return;

            GameObject g = groundPool.Count > 0 ? groundPool.Pop() : Instantiate(groundTemplate, transform);
            generatedGround.Add(g);
            g.transform.position = new Vector3(chunkX + localX + width * 0.5f, transform.position.y - 1f, 0f);

            SpriteRenderer sr = g.GetComponent<SpriteRenderer>();
            BoxCollider2D col = g.GetComponent<BoxCollider2D>();
            sr.size = new Vector2(width, sr.size.y);
            col.size = new Vector2(width, col.size.y);
            g.SetActive(true);

            activeChunks.Add(g);
            CookieZoneManager.RegisterChunk(g);
        }

        private void SpawnGenObstacle(float worldX, int level)
        {
            if (genObstacles == null) return;

            obstacleChoices.Clear();
            GenObstacle newest = null;
            foreach (GenObstacle o in genObstacles)
            {
                if (o == null || o.prefab == null || o.unlockLevel > level) continue;
                obstacleChoices.Add(o.prefab);
                if (newest == null || o.unlockLevel > newest.unlockLevel) newest = o;
            }
            if (obstacleChoices.Count == 0) return;

            // สิ่งกีดขวางที่เพิ่งปลดล็อกโผล่บ่อยกว่า ให้ผู้เล่นได้เห็นของใหม่
            GameObject prefab = newest != null && newest.unlockLevel > 1 && Random.value < 0.4f
                ? newest.prefab
                : obstacleChoices[Random.Range(0, obstacleChoices.Count)];
            bool overhead = false;
            foreach (GenObstacle o in genObstacles) if (o != null && o.prefab == prefab) { overhead = o.overhead; break; }

            float groundTop = transform.position.y;
            GameObject obj = CookiePool.Spawn(prefab, new Vector3(worldX, groundTop + (overhead ? 2.05f : 0.2f), 0f), Quaternion.identity);
            if (obj == null) return;

            if (!overhead)
            {
                // วางฐานให้แนบพื้นพอดี (รูปแต่ละแบบสูงไม่เท่ากัน)
                SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                    obj.transform.position += new Vector3(0f, groundTop - 0.04f - sr.bounds.min.y, 0f);
            }
            activeChunks.Add(obj);
        }

        private void AddCoinLine(float chunkX, float from, float to, float skipX)
        {
            if (starterCoinPrefab == null) return;
            for (float x = from; x <= to; x += 1.5f)
            {
                if (!float.IsNaN(skipX) && Mathf.Abs(x - skipX) < 1.6f) continue;
                activeChunks.Add(CookiePool.Spawn(starterCoinPrefab, new Vector3(chunkX + x, transform.position.y + 0.4f, 0f), Quaternion.identity));
            }
        }

        // เหรียญโค้งตามแนวกระโดดข้ามหลุม (บอกจังหวะกระโดดให้ผู้เล่น)
        private void AddCoinArc(float chunkX, float pitStart, float gap)
        {
            if (starterCoinPrefab == null) return;
            const int count = 5;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                float x = pitStart - 0.6f + (gap + 1.2f) * t;
                float y = 0.5f + 1.8f * 4f * t * (1f - t);
                activeChunks.Add(CookiePool.Spawn(starterCoinPrefab, new Vector3(chunkX + x, transform.position.y + y, 0f), Quaternion.identity));
            }
        }
        #endregion

        private float GetChunkWidth(GameObject prefab, GameObject chunk)
        {
            if (chunkWidthCache.TryGetValue(prefab, out float cachedWidth))
            {
                return cachedWidth;
            }

            // ตรวจหาวัตถุลูกชื่อ "EndPoint" ถ้ามี ให้ใช้ตำแหน่งนั้นเป็นจุดต่อของ Chunk ถัดไป
            Transform endPoint = chunk.transform.Find("EndPoint");
            float width = endPoint != null ? endPoint.localPosition.x : defaultChunkWidth;

            chunkWidthCache[prefab] = width;
            return width;
        }

        private void RemoveOldChunks()
        {
            if (activeChunks.Count == 0 || playerTransform == null) return;

            for (int i = activeChunks.Count - 1; i >= 0; i--)
            {
                GameObject chunk = activeChunks[i];

                // ถูกทำลาย หรือถูกคืนเข้า Pool ไปแล้ว (เช่น เหรียญเริ่มเกมที่ถูกเก็บ)
                if (chunk == null || !chunk.activeSelf)
                {
                    activeChunks.RemoveAt(i);
                    continue;
                }

                // ถ้า Chunk อยู่ห่างด้านหลังผู้เล่นเกินกำหนด ให้คืนเข้า Pool เพื่อนำกลับมาใช้ใหม่
                if (playerTransform.position.x - chunk.transform.position.x > destroyBehindDistance + defaultChunkWidth)
                {
                    activeChunks.RemoveAt(i);
                    if (generatedGround.Contains(chunk))
                    {
                        // พื้นของฉากสุ่มสร้าง เก็บไว้ใช้ซ้ำเอง
                        chunk.SetActive(false);
                        groundPool.Push(chunk);
                    }
                    else
                    {
                        CookiePool.Release(chunk);
                    }
                }
            }
        }
    }
}
