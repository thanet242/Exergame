using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CookieRun.Editor
{
    /// <summary>
    /// สคริปต์อัตโนมัติสำหรับสร้าง Prefabs, Sprites และ Scene ของเกมวิ่ง Cookie Run
    /// ผูกกับตัวละคร SPUM Human (SPUM_20240911215638389) และเหรียญ 2DCoin แบบแอนิเมชัน
    /// </summary>
    public static class CookieRunAutoSetup
    {
        private const string SCENE_PATH = "Assets/Scenes/CookieRunner.unity";
        private const string PREFAB_DIR = "Assets/CookieRunGame/Prefabs";
        private const string SPRITE_DIR = "Assets/CookieRunGame/Sprites";
        private const string SPUM_PREFAB_PATH = "Assets/SPUM/Resources/Addons/BasicPack/2_Prefab/Human/SPUM_20240911215638389.prefab";
        private const string COIN_SPRITESHEET_PATH = "Assets/2DCoin/Sprites/coin-animation.png";
        private const string CAVE_BG_PATH = "Assets/map/Run/6e60b201-9afc-4930-9114-7bdee686e9f9.png";
        private const string CAVE_CRYSTAL_BG_PATH = "Assets/CookieRunGame/Sprites/Cave_Crystal_Background.png";
        private const string GROUND_ROCK_PATH = "Assets/CookieRunGame/Sprites/Ground_Rock.png";
        private const string GROUND_DIRT_PATH = "Assets/OONY TEAM/2D Classic Sprite - Adventure/Ground2.png";
        private const string STALACTITE_PATH = "Assets/CookieRunGame/Sprites/Obstacle_Stalactite.png";
        private const string STALACTITE_CLUSTER_PATH = "Assets/CookieRunGame/Sprites/Obstacle_StalactiteCluster.png";
        private const string MEGA_CRYSTAL_PATH = "Assets/CookieRunGame/Sprites/Obstacle_MegaCrystal.png";
        private const string ROCK_PATH = "Assets/CookieRunGame/Sprites/Obstacle_Rock.png";
        private const string SHIELD_PREFAB_PATH = "Assets/CookieRunGame/Prefabs/Item_Shield.prefab";
        private const string MAGNET_PREFAB_PATH = "Assets/CookieRunGame/Prefabs/Item_Magnet.prefab";
        private const string BLAST_PREFAB_PATH = "Assets/CookieRunGame/Prefabs/Item_Blast.prefab";
        private const string GIANT_PREFAB_PATH = "Assets/CookieRunGame/Prefabs/Item_Giant.prefab";
        private const string STALAGMITES_PREFAB_PATH = "Assets/CookieRunGame/Prefabs/Obstacle_Stalagmites.prefab";
        private const string CAVE_BAT_PREFAB_PATH = "Assets/CookieRunGame/Prefabs/Obstacle_CaveBat.prefab";
        private const string MONSTER_PREFAB_PATH = "Assets/CookieRunGame/Prefabs/Obstacle_Monster.prefab";
        private const string ROCK_WALL_PREFAB_PATH = "Assets/CookieRunGame/Prefabs/Obstacle_RockWall.prefab";

        [MenuItem("Cookie Run/🌟 Setup Complete Runner Game (Auto)", false, 0)]
        public static void BuildCompleteGameMenu()
        {
            BuildCompleteGame(true);
        }

        [MenuItem("Cookie Run/⛰️ Rebuild Chunks (Stalactites + Double Jump Obstacles)", false, 1)]
        public static void RebuildAllChunksMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            RebuildChunksAndObstacles();
        }

        [MenuItem("Cookie Run/🌄 Apply Cave Theme (Cave BG + Rock Ground)", false, 2)]
        public static void ApplyCaveThemeMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            ApplyCaveBackground(CAVE_BG_PATH, "Crystal_Cave", new Vector2(2.35f, 2.35f), 2.0f);
            ApplyRockGround(GROUND_ROCK_PATH);
        }

        [MenuItem("Cookie Run/🌄 Cave Variations/1. Crystal Cave (Default)", false, 10)]
        public static void SetStoneCavernBackgroundMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            ApplyCaveBackground(CAVE_BG_PATH, "Crystal_Cave", new Vector2(2.35f, 2.35f), 2.0f);
        }

        [MenuItem("Cookie Run/🌄 Cave Variations/2. Mystic Crystal Cave", false, 11)]
        public static void SetCrystalCaveBackgroundMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            ApplyCaveBackground(CAVE_CRYSTAL_BG_PATH, "Mystic_Crystal_Cave", new Vector2(2.2f, 2.2f), 2.0f);
        }

        [MenuItem("Cookie Run/🌱 Apply Matching Rock Ground", false, 20)]
        public static void ApplyRockGroundMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            ApplyRockGround(GROUND_ROCK_PATH);
        }

        [MenuItem("Cookie Run/🌱 Apply Dirt Ground (Ground2)", false, 21)]
        public static void ApplyDirtGroundMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            ApplyRockGround(GROUND_DIRT_PATH);
        }

        public static void ApplyCaveBackground(string bgPath, string layerName, Vector2 scale, float yOffset)
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != SCENE_PATH && File.Exists(SCENE_PATH))
            {
                activeScene = EditorSceneManager.OpenScene(SCENE_PATH);
            }

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.backgroundColor = new Color(0.06f, 0.05f, 0.08f);
            }

            GameObject bgObj = GameObject.Find("ParallaxBackground");
            if (bgObj == null)
            {
                bgObj = new GameObject("ParallaxBackground");
                Undo.RegisterCreatedObjectUndo(bgObj, "Create ParallaxBackground");
            }

            CookieParallaxBackground parallaxBg = bgObj.GetComponent<CookieParallaxBackground>();
            if (parallaxBg == null)
            {
                parallaxBg = bgObj.AddComponent<CookieParallaxBackground>();
            }

            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgPath);
            if (bgSprite == null)
            {
                bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CookieRunGame/Sprites/Cave_Background.png");
            }
            if (bgSprite == null)
            {
                bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/map/Cave3.1/Cave 3.1.jpg");
            }

            if (bgSprite != null)
            {
                parallaxBg.Layers.Clear();
                parallaxBg.AddLayer(layerName, bgSprite, 0.05f, yOffset, scale, -500, Color.white, sliceCount: 1, slowDriftRatio: 0.05f);
                parallaxBg.InitializeLayers();
                EditorUtility.SetDirty(bgObj);
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                Debug.Log($"<color=green>[CookieRun] ติดตั้งฉากหลังถ้ำ ({layerName}) เรียบร้อย!</color>");
            }
            else
            {
                Debug.LogWarning("[CookieRun] ไม่พบรูปภาพ Sprite สำหรับทำฉากหลังถ้ำ: " + bgPath);
            }
        }

        public static void ApplyRockGround(string groundSpritePath)
        {
            Sprite rockSprite = AssetDatabase.LoadAssetAtPath<Sprite>(groundSpritePath);
            if (rockSprite == null)
            {
                rockSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/OONY TEAM/2D Classic Sprite - Adventure/Floor3.png");
            }

            if (rockSprite == null)
            {
                Debug.LogError($"[CookieRun] ไม่พบรูปภาพพื้นหินที่: {groundSpritePath}");
                return;
            }

            // 1. อัปเดต InitialGround ใน Scene
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != SCENE_PATH && File.Exists(SCENE_PATH))
            {
                activeScene = EditorSceneManager.OpenScene(SCENE_PATH);
            }

            GameObject initialGround = GameObject.Find("InitialGround");
            if (initialGround != null)
            {
                initialGround.transform.localScale = Vector3.one;
                initialGround.transform.position = new Vector3(25f, -1f, 0f);
                SpriteRenderer sr = initialGround.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = rockSprite;
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.size = new Vector2(50f, 2f);
                    sr.sortingOrder = 1;
                }
                BoxCollider2D col = initialGround.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    col.size = new Vector2(50f, 2f);
                    col.offset = Vector2.zero;
                }
                EditorUtility.SetDirty(initialGround);
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                Debug.Log("<color=green>[CookieRun] อัปเดต InitialGround เป็นพื้นหินใน Scene เรียบร้อย!</color>");
            }

            // 2. อัปเดต Chunk Prefabs ทั้งหมด
            UpdateChunkPrefabGround("Chunk_Start.prefab", rockSprite);
            UpdateChunkPrefabGround("Chunk_Jumps.prefab", rockSprite);
            UpdateChunkPrefabGround("Chunk_Slides.prefab", rockSprite);
            UpdateChunkPrefabGround("Chunk_Mixed.prefab", rockSprite);
            UpdateChunkPrefabGround("Chunk_Pit_Single.prefab", rockSprite);
            UpdateChunkPrefabGround("Chunk_Pit_Double.prefab", rockSprite);
            UpdateChunkPrefabGround("Chunk_TallPillars.prefab", rockSprite);
            UpdateChunkPrefabGround("Chunk_Pit_Combo.prefab", rockSprite);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[CookieRun] ติดตั้งพื้นหินถ้ำให้กับทุก Chunk สำเร็จสมบูรณ์!</color>");
        }

        private static void UpdateChunkPrefabGround(string prefabName, Sprite dirtSprite)
        {
            string path = $"{PREFAB_DIR}/{prefabName}";
            if (!File.Exists(path)) return;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            foreach (Transform child in root.transform)
            {
                if (child.name.StartsWith("GroundBlock"))
                {
                    SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sprite = dirtSprite;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        [InitializeOnLoadMethod]
        private static void AutoRunOnFirstImport()
        {
            EditorApplication.update += CheckAndRun;
        }

        private static void CheckAndRun()
        {
            EditorApplication.update -= CheckAndRun;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            // ตรวจสอบและสร้าง/อัปเดต Scene และ Prefabs เฉพาะเมื่อยังไม่มีไฟล์
            if (!File.Exists(SCENE_PATH) || !File.Exists($"{PREFAB_DIR}/Chunk_Start.prefab") || !File.Exists($"{PREFAB_DIR}/Item_Coin2D.prefab"))
            {
                BuildCompleteGame(false);
            }
            else if (!File.Exists($"{PREFAB_DIR}/Chunk_Pit_Single.prefab") || !File.Exists($"{PREFAB_DIR}/Obstacle_MegaCrystal.prefab"))
            {
                RebuildChunksAndObstacles();
            }
            else
            {
                string jumpsPath = $"{PREFAB_DIR}/Chunk_Jumps.prefab";
                if (File.Exists(jumpsPath))
                {
                    string content = File.ReadAllText(jumpsPath);
                    if (!content.Contains("aaaa1111bbbb2222cccc3333dddd4401")) // GUID ของ Obstacle_Stalagmites
                    {
                        RebuildChunksAndObstacles();
                    }
                }
            }
        }

        public static void BuildCompleteGame(bool openSceneAfter = true)
        {
            Debug.Log("<color=cyan>[CookieRun] เริ่มต้นสร้าง/อัปเดตระบบเกม Cookie Run (SPUM + 2DCoin)...</color>");

            // 1. สร้างโฟลเดอร์ที่จำเป็น
            EnsureFolderExists("Assets/Scenes");
            EnsureFolderExists("Assets/CookieRunGame");
            EnsureFolderExists(PREFAB_DIR);
            EnsureFolderExists(SPRITE_DIR);

            // 2. ตรวจสอบ Layer "Ground"
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer == -1)
            {
                groundLayer = 3; // ตำแหน่ง Ground ใน TagManager
            }

            // 3. สร้างรูปภาพ Sprite สิ่งกีดขวางและไอเทมพื้นฐาน
            Sprite sprGround = AssetDatabase.LoadAssetAtPath<Sprite>(GROUND_ROCK_PATH);
            if (sprGround == null)
            {
                sprGround = AssetDatabase.LoadAssetAtPath<Sprite>(GROUND_DIRT_PATH);
            }
            if (sprGround == null)
            {
                sprGround = GetOrCreateSprite("Ground_Sprite.png", 64, 64, new Color(0.35f, 0.25f, 0.20f, 1f));
            }
            Sprite sprRock = AssetDatabase.LoadAssetAtPath<Sprite>(ROCK_PATH);
            if (sprRock == null) sprRock = GetOrCreateSprite("Rock_Obstacle.png", 64, 64, new Color(0.45f, 0.45f, 0.50f, 1f));
            Sprite sprSpike = GetOrCreateSprite("Spike_Overhead.png", 80, 50, new Color(0.85f, 0.25f, 0.20f, 1f));
            Sprite sprPotion = GetOrCreateSprite("Potion_Heart.png", 50, 50, new Color(1f, 0.25f, 0.5f, 1f));

            Sprite sprStalactite = AssetDatabase.LoadAssetAtPath<Sprite>(STALACTITE_PATH);
            if (sprStalactite == null) sprStalactite = sprSpike;

            Sprite sprStalactiteCluster = AssetDatabase.LoadAssetAtPath<Sprite>(STALACTITE_CLUSTER_PATH);
            if (sprStalactiteCluster == null) sprStalactiteCluster = sprSpike;

            Sprite sprMegaCrystal = AssetDatabase.LoadAssetAtPath<Sprite>(MEGA_CRYSTAL_PATH);
            if (sprMegaCrystal == null) sprMegaCrystal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/OONY TEAM/2D Classic Sprite - Adventure/Crystal.png");
            if (sprMegaCrystal == null) sprMegaCrystal = GetOrCreateSprite("Crystal_Obstacle.png", 64, 96, new Color(0.75f, 0.25f, 0.90f, 1f));

            Sprite sprSpikeFloor = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/OONY TEAM/2D Classic Sprite - Adventure/Spikes/Spike1.png");
            if (sprSpikeFloor == null) sprSpikeFloor = GetOrCreateSprite("Spike_Floor.png", 64, 40, new Color(0.6f, 0.5f, 0.5f, 1f));

            // 4. สร้าง Prefabs ของ สิ่งกีดขวาง (ปรับขนาดความสูงให้เหมาะกับแผ่นเหยียบเท้า Arcade Pad ไม่สูงชันเกินไป)
            GameObject pRock = CreateObstaclePrefab("Obstacle_Rock.prefab", sprRock, ObstacleType.GroundObstacle, 15f, new Vector2(0.62f, 0.35f), new Vector3(1.0f, 1.0f, 1f));
            GameObject pStalactite = CreateObstaclePrefab("Obstacle_Stalactite.prefab", sprStalactite, ObstacleType.OverheadObstacle, 15f, new Vector2(0.65f, 2.3f), new Vector3(1.0f, 1.0f, 1f));
            GameObject pStalactiteCluster = CreateObstaclePrefab("Obstacle_StalactiteCluster.prefab", sprStalactiteCluster, ObstacleType.OverheadObstacle, 15f, new Vector2(2.3f, 2.1f), new Vector3(1.05f, 1.05f, 1f));
            GameObject pMegaCrystal = CreateObstaclePrefab("Obstacle_MegaCrystal.prefab", sprMegaCrystal, ObstacleType.GroundObstacle, 20f, new Vector2(0.65f, 1.4f), new Vector3(0.85f, 0.52f, 1f));
            GameObject pSpikeFloor = CreateObstaclePrefab("Obstacle_SpikeFloor.prefab", sprSpikeFloor, ObstacleType.GroundObstacle, 15f, new Vector2(1.2f, 0.45f), new Vector3(0.55f, 0.5f, 1f));

            // 5. สร้าง Prefab ของ เหรียญ 2DCoin และไอเทม Power-up
            GameObject pCoin = Create2DCoinPrefab();
            GameObject pPotion = CreateItemPrefab("Item_Potion.prefab", sprPotion, ItemType.HealthPotion, 50, 0, 25f);
            GameObject pShield = AssetDatabase.LoadAssetAtPath<GameObject>(SHIELD_PREFAB_PATH);
            GameObject pMagnet = AssetDatabase.LoadAssetAtPath<GameObject>(MAGNET_PREFAB_PATH);
            GameObject pBlast = AssetDatabase.LoadAssetAtPath<GameObject>(BLAST_PREFAB_PATH);
            GameObject pGiant = AssetDatabase.LoadAssetAtPath<GameObject>(GIANT_PREFAB_PATH);

            GameObject pStalagmites = AssetDatabase.LoadAssetAtPath<GameObject>(STALAGMITES_PREFAB_PATH);
            GameObject pMonster = AssetDatabase.LoadAssetAtPath<GameObject>(MONSTER_PREFAB_PATH);
            if (pMonster == null) pMonster = AssetDatabase.LoadAssetAtPath<GameObject>(CAVE_BAT_PREFAB_PATH);
            GameObject pRockWall = AssetDatabase.LoadAssetAtPath<GameObject>(ROCK_WALL_PREFAB_PATH);

            // 6. สร้าง Prefabs ของ ชิ้นส่วนฉาก (Chunks หลากหลายพร้อมสิ่งกีดขวางและไอเทม Power-up)
            GameObject cStart = CreateStartChunkPrefab("Chunk_Start.prefab", sprGround, pCoin, groundLayer);
            GameObject cJumps = CreateJumpsChunkPrefab("Chunk_Jumps.prefab", sprGround, pRock, pMegaCrystal, pStalagmites, pCoin, groundLayer);
            GameObject cSlides = CreateSlidesChunkPrefab("Chunk_Slides.prefab", sprGround, pStalactite, pStalactiteCluster, pMonster, pCoin, groundLayer);
            GameObject cMixed = CreateMixedChunkPrefab("Chunk_Mixed.prefab", sprGround, pRock, pStalactite, pMegaCrystal, pMonster, pCoin, pShield != null ? pShield : pPotion, groundLayer);
            GameObject cPitSingle = CreatePitSingleChunkPrefab("Chunk_Pit_Single.prefab", sprGround, pRock, pMegaCrystal, pRockWall, pCoin, groundLayer);
            GameObject cPitDouble = CreatePitDoubleChunkPrefab("Chunk_Pit_Double.prefab", sprGround, pStalactite, pCoin, pMagnet != null ? pMagnet : pPotion, groundLayer);
            GameObject cTallPillars = CreateTallPillarsChunkPrefab("Chunk_TallPillars.prefab", sprGround, pMegaCrystal, pStalactite, pCoin, pGiant != null ? pGiant : pCoin, groundLayer);
            GameObject cPitCombo = CreatePitComboChunkPrefab("Chunk_Pit_Combo.prefab", sprGround, pStalactiteCluster, pMegaCrystal, pRockWall, pCoin, pBlast != null ? pBlast : pPotion, groundLayer);

            // 7. สร้าง Scene ใหม่
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 7.1 สร้าง Main Camera (ซูมใกล้ orthographicSize = 4.2f, fixedY = 1.35f, offset = (3.8, 1.0, -10))
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 4.2f;
            cam.backgroundColor = new Color(0.06f, 0.05f, 0.08f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camObj.transform.position = new Vector3(3.8f, 1.35f, -10f);
            camObj.AddComponent<AudioListener>();
            CookieCameraFollow camFollow = camObj.AddComponent<CookieCameraFollow>();

            // 7.1.1 สร้าง Parallax Looping Background
            GameObject bgObj = new GameObject("ParallaxBackground");
            CookieParallaxBackground parallaxBg = bgObj.AddComponent<CookieParallaxBackground>();
            Sprite caveBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CAVE_BG_PATH);
            if (caveBgSprite == null)
            {
                caveBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/map/Cave3.1/Cave 3.1.jpg");
            }
            if (caveBgSprite != null)
            {
                parallaxBg.AddLayer("Deep_Stone_Cavern", caveBgSprite, 0.05f, 2.0f, new Vector2(2.35f, 2.35f), -500, Color.white, sliceCount: 1, slowDriftRatio: 0.05f);
            }

            // 7.2 สร้าง Player โดยใช้โมเดล SPUM
            GameObject playerObj = new GameObject("Player");
            playerObj.transform.position = new Vector3(2f, 0.1f, 0f);

            // โหลดโมเดล SPUM Human มาเป็นลูก
            GameObject spumSrc = AssetDatabase.LoadAssetAtPath<GameObject>(SPUM_PREFAB_PATH);
            GameObject spumInstance = null;
            Animator spumAnim = null;

            if (spumSrc != null)
            {
                spumInstance = (GameObject)PrefabUtility.InstantiatePrefab(spumSrc, playerObj.transform);
                spumInstance.name = "SPUM_Model";
                spumInstance.transform.localPosition = Vector3.zero;
                // สลับด้าน x = -1 เพื่อให้ตัวละครหันหน้าไปทางขวาตามทิศทางการวิ่ง
                spumInstance.transform.localScale = new Vector3(-1f, 1f, 1f);
                spumAnim = spumInstance.GetComponentInChildren<Animator>();
            }
            else
            {
                Debug.LogWarning("[CookieRun] ไม่พบ SPUM prefab ที่: " + SPUM_PREFAB_PATH);
            }

            Rigidbody2D playerRb = playerObj.AddComponent<Rigidbody2D>();
            playerRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            playerRb.constraints = RigidbodyConstraints2D.FreezeRotation;
            playerRb.gravityScale = 3.5f;

            BoxCollider2D playerCol = playerObj.AddComponent<BoxCollider2D>();
            playerCol.size = new Vector2(0.8f, 1.2f);
            playerCol.offset = new Vector2(0f, 0.6f);

            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.parent = playerObj.transform;
            groundCheck.transform.localPosition = new Vector3(0f, 0f, 0f);

            AudioSource playerAudio = playerObj.AddComponent<AudioSource>();

            CookieHealth playerHealth = playerObj.AddComponent<CookieHealth>();
            SerializedObject soHealth = new SerializedObject(playerHealth);
            soHealth.FindProperty("maxHearts").intValue = 3;
            soHealth.FindProperty("decayRatePerSecond").floatValue = 0f;
            soHealth.ApplyModifiedProperties();

            CookiePlayerController playerCtrl = playerObj.AddComponent<CookiePlayerController>();
            SerializedObject soCtrl = new SerializedObject(playerCtrl);
            soCtrl.FindProperty("baseRunSpeed").floatValue = 7f;
            soCtrl.FindProperty("jumpForce").floatValue = 11f;
            soCtrl.FindProperty("doubleJumpForce").floatValue = 10.5f;
            soCtrl.FindProperty("baseGravityScale").floatValue = 3.5f;
            soCtrl.FindProperty("fallGravityMultiplier").floatValue = 1.4f;
            soCtrl.FindProperty("standColliderSize").vector2Value = new Vector2(0.8f, 1.2f);
            soCtrl.FindProperty("standColliderOffset").vector2Value = new Vector2(0f, 0.6f);
            soCtrl.FindProperty("slideColliderSize").vector2Value = new Vector2(1.2f, 0.6f);
            soCtrl.FindProperty("slideColliderOffset").vector2Value = new Vector2(0f, 0.3f);
            soCtrl.FindProperty("groundCheckPoint").objectReferenceValue = groundCheck.transform;
            soCtrl.FindProperty("groundLayer").intValue = 1 << groundLayer;
            if (spumAnim != null) soCtrl.FindProperty("animator").objectReferenceValue = spumAnim;
            soCtrl.FindProperty("audioSource").objectReferenceValue = playerAudio;
            soCtrl.FindProperty("deathYThreshold").floatValue = -0.8f;
            soCtrl.ApplyModifiedProperties();

            // เชื่อม Camera กับ Player (ซูมใกล้ fixedY = 1.35f, offset = (3.8, 1.0, -10))
            SerializedObject soCam = new SerializedObject(camFollow);
            soCam.FindProperty("target").objectReferenceValue = playerObj.transform;
            soCam.FindProperty("lockY").boolValue = true;
            soCam.FindProperty("fixedY").floatValue = 1.35f;
            soCam.FindProperty("offset").vector3Value = new Vector3(3.8f, 1.0f, -10f);
            soCam.ApplyModifiedProperties();

            // 7.3 สร้าง GameManager
            GameObject gmObj = new GameObject("GameManager");
            CookieGameManager gameManager = gmObj.AddComponent<CookieGameManager>();
            SerializedObject soGM = new SerializedObject(gameManager);
            soGM.FindProperty("scorePerMeter").floatValue = 0f;
            soGM.ApplyModifiedProperties();

            // 7.4 สร้างพื้นเริ่มต้นคงที่ใน Scene (ยาว 50 เมตร รองรับช่วงสอนเล่น) ใต้เท้าผู้เล่น
            GameObject initialGround = new GameObject("InitialGround");
            initialGround.layer = groundLayer;
            initialGround.transform.position = new Vector3(25f, -1f, 0f);
            initialGround.transform.localScale = Vector3.one;

            SpriteRenderer groundSr = initialGround.AddComponent<SpriteRenderer>();
            groundSr.sprite = sprGround;
            groundSr.drawMode = SpriteDrawMode.Tiled;
            groundSr.size = new Vector2(50f, 2f);
            groundSr.sortingOrder = 1;

            BoxCollider2D groundCol = initialGround.AddComponent<BoxCollider2D>();
            groundCol.size = new Vector2(50f, 2f);

            // 7.5 สร้าง MapSpawner (เริ่มสุ่มต่อฉากถัดไปตั้งแต่ x = 50 เมตร)
            GameObject spawnerObj = new GameObject("MapSpawner");
            spawnerObj.transform.position = new Vector3(50f, 0f, 0f);
            CookieMapSpawner spawner = spawnerObj.AddComponent<CookieMapSpawner>();
            SerializedObject soSpawn = new SerializedObject(spawner);
            soSpawn.FindProperty("playerTransform").objectReferenceValue = playerObj.transform;
            soSpawn.FindProperty("startChunkPrefab").objectReferenceValue = cStart;
            soSpawn.FindProperty("safeStartChunkCount").intValue = 2;
            soSpawn.FindProperty("starterCoinPrefab").objectReferenceValue = pCoin;
            SerializedProperty chunkArr = soSpawn.FindProperty("chunkPrefabs");
            chunkArr.arraySize = 7;
            chunkArr.GetArrayElementAtIndex(0).objectReferenceValue = cJumps;
            chunkArr.GetArrayElementAtIndex(1).objectReferenceValue = cSlides;
            chunkArr.GetArrayElementAtIndex(2).objectReferenceValue = cMixed;
            chunkArr.GetArrayElementAtIndex(3).objectReferenceValue = cPitSingle;
            chunkArr.GetArrayElementAtIndex(4).objectReferenceValue = cPitDouble;
            chunkArr.GetArrayElementAtIndex(5).objectReferenceValue = cTallPillars;
            chunkArr.GetArrayElementAtIndex(6).objectReferenceValue = cPitCombo;
            soSpawn.FindProperty("defaultChunkWidth").floatValue = 26f;
            SerializedProperty propTutorial = soSpawn.FindProperty("hasPreGameTutorial");
            if (propTutorial != null) propTutorial.boolValue = true;
            soSpawn.ApplyModifiedProperties();

            // 7.6 สร้าง Canvas UI (HUD)
            BuildUI(playerCtrl, playerHealth, gameManager);

            // 7.7 สร้าง TutorialManager (สำหรับฝึกซ้อมก่อนเริ่มเกม)
            GameObject tutObj = new GameObject("TutorialManager");
            CookieTutorialManager tutMgr = tutObj.AddComponent<CookieTutorialManager>();
            SerializedObject soTut = new SerializedObject(tutMgr);
            soTut.FindProperty("enableTutorial").boolValue = true;
            soTut.FindProperty("jumpObstacleX").floatValue = 14f;
            soTut.FindProperty("jumpDistance").floatValue = 3.2f;
            soTut.FindProperty("slideObstacleX").floatValue = 28f;
            soTut.FindProperty("slideDistance").floatValue = 3.5f;
            soTut.FindProperty("tutorialFinishX").floatValue = 35f;
            soTut.FindProperty("rockPrefab").objectReferenceValue = pRock;
            soTut.FindProperty("stalactitePrefab").objectReferenceValue = pStalactite;
            soTut.ApplyModifiedProperties();

            // บันทึก Scene
            EditorSceneManager.SaveScene(newScene, SCENE_PATH);
            AddSceneToBuildSettings(SCENE_PATH);

            Debug.Log("<color=green>[CookieRun] สร้าง Scene และ Prefabs สำเร็จ! (SPUM Character + 2DCoin เรียบร้อย)</color>");

            if (openSceneAfter)
            {
                EditorSceneManager.OpenScene(SCENE_PATH);
            }
        }

        public static void RebuildChunksAndObstacles()
        {
            EnsureFolderExists("Assets/Scenes");
            EnsureFolderExists("Assets/CookieRunGame");
            EnsureFolderExists(PREFAB_DIR);
            EnsureFolderExists(SPRITE_DIR);

            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer == -1) groundLayer = 3;

            Sprite sprGround = AssetDatabase.LoadAssetAtPath<Sprite>(GROUND_ROCK_PATH);
            if (sprGround == null) sprGround = AssetDatabase.LoadAssetAtPath<Sprite>(GROUND_DIRT_PATH);
            if (sprGround == null) sprGround = GetOrCreateSprite("Ground_Sprite.png", 64, 64, new Color(0.35f, 0.25f, 0.20f, 1f));

            Sprite sprRock = AssetDatabase.LoadAssetAtPath<Sprite>(ROCK_PATH);
            if (sprRock == null) sprRock = GetOrCreateSprite("Rock_Obstacle.png", 64, 64, new Color(0.45f, 0.45f, 0.50f, 1f));
            Sprite sprSpike = GetOrCreateSprite("Spike_Overhead.png", 80, 50, new Color(0.85f, 0.25f, 0.20f, 1f));
            Sprite sprPotion = GetOrCreateSprite("Potion_Heart.png", 50, 50, new Color(1f, 0.25f, 0.5f, 1f));

            Sprite sprStalactite = AssetDatabase.LoadAssetAtPath<Sprite>(STALACTITE_PATH);
            if (sprStalactite == null) sprStalactite = sprSpike;

            Sprite sprStalactiteCluster = AssetDatabase.LoadAssetAtPath<Sprite>(STALACTITE_CLUSTER_PATH);
            if (sprStalactiteCluster == null) sprStalactiteCluster = sprSpike;

            Sprite sprMegaCrystal = AssetDatabase.LoadAssetAtPath<Sprite>(MEGA_CRYSTAL_PATH);
            if (sprMegaCrystal == null) sprMegaCrystal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/OONY TEAM/2D Classic Sprite - Adventure/Crystal.png");
            if (sprMegaCrystal == null) sprMegaCrystal = GetOrCreateSprite("Crystal_Obstacle.png", 64, 96, new Color(0.75f, 0.25f, 0.90f, 1f));

            Sprite sprSpikeFloor = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/OONY TEAM/2D Classic Sprite - Adventure/Spikes/Spike1.png");
            if (sprSpikeFloor == null) sprSpikeFloor = GetOrCreateSprite("Spike_Floor.png", 64, 40, new Color(0.6f, 0.5f, 0.5f, 1f));

            // สร้าง Prefabs ของ สิ่งกีดขวาง (ปรับขนาดความสูงให้เหมาะกับแผ่นเหยียบเท้า Arcade Pad ไม่สูงชันเกินไป)
            GameObject pRock = CreateObstaclePrefab("Obstacle_Rock.prefab", sprRock, ObstacleType.GroundObstacle, 15f, new Vector2(0.62f, 0.35f), new Vector3(1.0f, 1.0f, 1f));
            GameObject pStalactite = CreateObstaclePrefab("Obstacle_Stalactite.prefab", sprStalactite, ObstacleType.OverheadObstacle, 15f, new Vector2(0.65f, 2.3f), new Vector3(1.0f, 1.0f, 1f));
            GameObject pStalactiteCluster = CreateObstaclePrefab("Obstacle_StalactiteCluster.prefab", sprStalactiteCluster, ObstacleType.OverheadObstacle, 15f, new Vector2(2.0f, 2.1f), new Vector3(1.05f, 1.05f, 1f));
            GameObject pMegaCrystal = CreateObstaclePrefab("Obstacle_MegaCrystal.prefab", sprMegaCrystal, ObstacleType.GroundObstacle, 20f, new Vector2(0.65f, 1.4f), new Vector3(0.85f, 0.52f, 1f));
            GameObject pSpikeFloor = CreateObstaclePrefab("Obstacle_SpikeFloor.prefab", sprSpikeFloor, ObstacleType.GroundObstacle, 15f, new Vector2(1.2f, 0.45f), new Vector3(0.55f, 0.5f, 1f));

            GameObject pCoin = Create2DCoinPrefab();
            GameObject pPotion = CreateItemPrefab("Item_Potion.prefab", sprPotion, ItemType.HealthPotion, 50, 0, 25f);
            GameObject pShield = AssetDatabase.LoadAssetAtPath<GameObject>(SHIELD_PREFAB_PATH);
            GameObject pMagnet = AssetDatabase.LoadAssetAtPath<GameObject>(MAGNET_PREFAB_PATH);
            GameObject pBlast = AssetDatabase.LoadAssetAtPath<GameObject>(BLAST_PREFAB_PATH);
            GameObject pGiant = AssetDatabase.LoadAssetAtPath<GameObject>(GIANT_PREFAB_PATH);

            GameObject pStalagmites = AssetDatabase.LoadAssetAtPath<GameObject>(STALAGMITES_PREFAB_PATH);
            GameObject pMonster = AssetDatabase.LoadAssetAtPath<GameObject>(MONSTER_PREFAB_PATH);
            if (pMonster == null) pMonster = AssetDatabase.LoadAssetAtPath<GameObject>(CAVE_BAT_PREFAB_PATH);
            GameObject pRockWall = AssetDatabase.LoadAssetAtPath<GameObject>(ROCK_WALL_PREFAB_PATH);

            // สร้าง Chunks ใหม่ทั้งหมดพร้อมกระจายสิ่งกีดขวางหลากหลายและไอเทม Power-up
            GameObject cStart = CreateStartChunkPrefab("Chunk_Start.prefab", sprGround, pCoin, groundLayer);
            GameObject cJumps = CreateJumpsChunkPrefab("Chunk_Jumps.prefab", sprGround, pRock, pMegaCrystal, pStalagmites, pCoin, groundLayer);
            GameObject cSlides = CreateSlidesChunkPrefab("Chunk_Slides.prefab", sprGround, pStalactite, pStalactiteCluster, pMonster, pCoin, groundLayer);
            GameObject cMixed = CreateMixedChunkPrefab("Chunk_Mixed.prefab", sprGround, pRock, pStalactite, pMegaCrystal, pMonster, pCoin, pShield != null ? pShield : pPotion, groundLayer);
            GameObject cPitSingle = CreatePitSingleChunkPrefab("Chunk_Pit_Single.prefab", sprGround, pRock, pMegaCrystal, pRockWall, pCoin, groundLayer);
            GameObject cPitDouble = CreatePitDoubleChunkPrefab("Chunk_Pit_Double.prefab", sprGround, pStalactite, pCoin, pMagnet != null ? pMagnet : pPotion, groundLayer);
            GameObject cTallPillars = CreateTallPillarsChunkPrefab("Chunk_TallPillars.prefab", sprGround, pMegaCrystal, pStalactite, pCoin, pGiant != null ? pGiant : pCoin, groundLayer);
            GameObject cPitCombo = CreatePitComboChunkPrefab("Chunk_Pit_Combo.prefab", sprGround, pStalactiteCluster, pMegaCrystal, pRockWall, pCoin, pBlast != null ? pBlast : pPotion, groundLayer);

            // อัปเดต MapSpawner ใน Scene
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != SCENE_PATH && File.Exists(SCENE_PATH))
            {
                activeScene = EditorSceneManager.OpenScene(SCENE_PATH);
            }

            GameObject spawnerObj = GameObject.Find("MapSpawner");
            if (spawnerObj != null)
            {
                CookieMapSpawner spawner = spawnerObj.GetComponent<CookieMapSpawner>();
                if (spawner != null)
                {
                    SerializedObject soSpawn = new SerializedObject(spawner);
                    soSpawn.FindProperty("startChunkPrefab").objectReferenceValue = cStart;
                    soSpawn.FindProperty("safeStartChunkCount").intValue = 2;
                    soSpawn.FindProperty("starterCoinPrefab").objectReferenceValue = pCoin;
                    SerializedProperty chunkArr = soSpawn.FindProperty("chunkPrefabs");
                    chunkArr.arraySize = 7;
                    chunkArr.GetArrayElementAtIndex(0).objectReferenceValue = cJumps;
                    chunkArr.GetArrayElementAtIndex(1).objectReferenceValue = cSlides;
                    chunkArr.GetArrayElementAtIndex(2).objectReferenceValue = cMixed;
                    chunkArr.GetArrayElementAtIndex(3).objectReferenceValue = cPitSingle;
                    chunkArr.GetArrayElementAtIndex(4).objectReferenceValue = cPitDouble;
                    chunkArr.GetArrayElementAtIndex(5).objectReferenceValue = cTallPillars;
                    chunkArr.GetArrayElementAtIndex(6).objectReferenceValue = cPitCombo;
                    soSpawn.FindProperty("defaultChunkWidth").floatValue = 26f;
                    soSpawn.ApplyModifiedProperties();
                    EditorUtility.SetDirty(spawnerObj);
                }
            }

            // ตรวจสอบและตั้งค่า deathYThreshold ของ Player
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                CookiePlayerController pc = playerObj.GetComponent<CookiePlayerController>();
                if (pc != null)
                {
                    SerializedObject soPc = new SerializedObject(pc);
                    soPc.FindProperty("deathYThreshold").floatValue = -0.8f;
                    soPc.ApplyModifiedProperties();
                    EditorUtility.SetDirty(playerObj);
                }

                CookieHealth h = playerObj.GetComponent<CookieHealth>();
                if (h != null)
                {
                    SerializedObject soH = new SerializedObject(h);
                    soH.FindProperty("decayRatePerSecond").floatValue = 0f;
                    soH.ApplyModifiedProperties();
                }
            }

            // อัปเดต Main Camera ใน Scene ให้ซูมเข้าใกล้
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.orthographicSize = 4.2f;
                CookieCameraFollow camFollow = mainCam.GetComponent<CookieCameraFollow>();
                if (camFollow != null)
                {
                    SerializedObject soCam = new SerializedObject(camFollow);
                    soCam.FindProperty("fixedY").floatValue = 1.35f;
                    soCam.FindProperty("offset").vector3Value = new Vector3(3.8f, 1.0f, -10f);
                    soCam.ApplyModifiedProperties();
                }
                EditorUtility.SetDirty(mainCam.gameObject);
            }

            // ซ่อน HealthSlider และเชื่อม BuffText ใน Canvas_CookieHUD
            GameObject canvasObj = GameObject.Find("Canvas_CookieHUD");
            if (canvasObj != null)
            {
                CookieUIManager ui = canvasObj.GetComponent<CookieUIManager>();
                if (ui != null)
                {
                    SerializedObject soUI = new SerializedObject(ui);
                    soUI.FindProperty("showHealthBar").boolValue = false;

                    Transform buffT = canvasObj.transform.Find("BuffText");
                    TextMeshProUGUI buffTxt = null;
                    if (buffT == null)
                    {
                        buffTxt = CreateText(canvasObj.transform, "BuffText", "", new Vector2(0.05f, 0.92f), new Vector2(0.6f, 0.98f), 30, TextAlignmentOptions.Left, Color.white);
                    }
                    else
                    {
                        buffTxt = buffT.GetComponent<TextMeshProUGUI>();
                    }

                    if (buffTxt != null)
                    {
                        soUI.FindProperty("activeBuffText").objectReferenceValue = buffTxt;
                    }
                    soUI.ApplyModifiedProperties();
                    EditorUtility.SetDirty(canvasObj);
                }

                Transform sliderT = canvasObj.transform.Find("HealthSlider");
                if (sliderT != null)
                {
                    sliderT.gameObject.SetActive(false);
                }
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[CookieRun] สร้าง Chunks ใหม่ (หลุมเดี่ยว, หลุมคู่, เสาคริสตัล, หนามพื้น) และลงทะเบียนใน MapSpawner สำเร็จ!</color>");
        }

        #region Coin & Item Prefabs
        private static GameObject Create2DCoinPrefab()
        {
            string outPath = $"{PREFAB_DIR}/Item_Coin2D.prefab";

            // โหลดเฟรม Sprite เหรียญ 2DCoin ทั้งหมดจากไฟล์ Sprite Sheet โดยตรง
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(COIN_SPRITESHEET_PATH);
            System.Collections.Generic.List<Sprite> frames = new System.Collections.Generic.List<Sprite>();
            foreach (var a in assets)
            {
                if (a is Sprite s) frames.Add(s);
            }
            frames.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

            GameObject coinObj = new GameObject("Item_Coin2D");
            coinObj.transform.position = Vector3.zero;
            coinObj.transform.localScale = new Vector3(0.28f, 0.28f, 1f); // ย่อขนาดเหรียญให้เล็กลงกำลังพอดี น่ารักสไตล์ Cookie Run

            SpriteRenderer sr = coinObj.AddComponent<SpriteRenderer>();
            if (frames.Count > 0)
            {
                sr.sprite = frames[0];
            }
            sr.sortingOrder = 4;
            sr.color = Color.white;

            // สลับเฟรมหมุนเหรียญด้วย CoinSpinner (แม่นยำ ไม่ขึ้นกับ Animator ของเดิมที่ตกหล่น)
            CoinSpinner spinner = coinObj.AddComponent<CoinSpinner>();
            spinner.frames = frames.ToArray();
            spinner.framesPerSecond = 10f;

            CircleCollider2D col = coinObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;

            CookieItem item = coinObj.AddComponent<CookieItem>();
            SerializedObject so = new SerializedObject(item);
            so.FindProperty("itemType").enumValueIndex = (int)ItemType.Coin;
            so.FindProperty("scoreValue").intValue = 200;
            so.FindProperty("coinValue").intValue = 1;
            so.FindProperty("canBeMagnetized").boolValue = true;
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(coinObj, outPath);
            Object.DestroyImmediate(coinObj);
            return prefab;
        }

        private static GameObject CreateObstaclePrefab(string fileName, Sprite sprite, ObstacleType type, float damage, Vector2 colSize, Vector3 localScale)
        {
            string path = $"{PREFAB_DIR}/{fileName}";
            GameObject go = new GameObject(Path.GetFileNameWithoutExtension(fileName));
            go.transform.localScale = localScale;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 3;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = colSize;

            CookieObstacle obs = go.AddComponent<CookieObstacle>();
            SerializedObject so = new SerializedObject(obs);
            so.FindProperty("obstacleType").enumValueIndex = (int)type;
            so.FindProperty("damage").floatValue = damage;
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateItemPrefab(string fileName, Sprite sprite, ItemType type, int score, int coin, float heal)
        {
            string path = $"{PREFAB_DIR}/{fileName}";
            GameObject go = new GameObject(Path.GetFileNameWithoutExtension(fileName));
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 4;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;

            CookieItem itm = go.AddComponent<CookieItem>();
            SerializedObject so = new SerializedObject(itm);
            so.FindProperty("itemType").enumValueIndex = (int)type;
            so.FindProperty("scoreValue").intValue = score;
            so.FindProperty("coinValue").intValue = coin;
            // ไอเทมหัวใจคืน 1 ดวงเสมอ (ไม่มีค่าปริมาณการฮีลแล้ว)
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }
        #endregion

        #region Chunk Builders
        private static GameObject CreateStartChunkPrefab(string fileName, Sprite groundSprite, GameObject coinPrefab, int groundLayer)
        {
            string path = $"{PREFAB_DIR}/{fileName}";
            GameObject chunk = new GameObject(Path.GetFileNameWithoutExtension(fileName));

            CreateGroundBlock(chunk.transform, groundSprite, 0f, 25f, groundLayer);

            // วางเหรียญ 2DCoin ให้เก็บตั้งแต่ต้น
            for (int i = 5; i < 22; i += 2)
            {
                GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, chunk.transform);
                c.transform.localPosition = new Vector3(i, 0.8f, 0f);
            }

            CreateEndPoint(chunk.transform, 25f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(chunk, path);
            Object.DestroyImmediate(chunk);
            return prefab;
        }

        private static GameObject CreateJumpsChunkPrefab(string fileName, Sprite groundSprite, GameObject rockPrefab, GameObject megaCrystalPrefab, GameObject stalagmitesPrefab, GameObject coinPrefab, int groundLayer)
        {
            string path = $"{PREFAB_DIR}/{fileName}";
            GameObject chunk = new GameObject(Path.GetFileNameWithoutExtension(fileName));

            CreateGroundBlock(chunk.transform, groundSprite, 0f, 26f, groundLayer);

            // สิ่งกีดขวางบนพื้น จุดที่ 1 (x=6, หินบนพื้น: กระโดด 1 ชั้น เหมาะกับแผ่นเหยียบเท้า)
            if (rockPrefab != null)
            {
                GameObject r1 = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, chunk.transform);
                r1.transform.localPosition = new Vector3(6f, 0.20f, 0f);
                SpawnCoinArc(chunk.transform, coinPrefab, 4f, 8f, 1.25f);
            }

            // สิ่งกีดขวางบนพื้น จุดที่ 2 (x=13, y=0.20: แผงหินงอก Stalagmites กระโดดจังหวะแม่น)
            if (stalagmitesPrefab != null)
            {
                GameObject sm = (GameObject)PrefabUtility.InstantiatePrefab(stalagmitesPrefab, chunk.transform);
                sm.transform.localPosition = new Vector3(13f, 0.20f, 0f);
                SpawnCoinArc(chunk.transform, coinPrefab, 11.5f, 14.5f, 1.25f);
            }

            // เหรียญทางวิ่งระดับพื้นระหว่างหินเล็กและหินใหญ่ (x = 16, 17)
            for (float x = 16f; x <= 17f; x += 1f)
            {
                GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, chunk.transform);
                c.transform.localPosition = new Vector3(x, 0.4f, 0f);
            }

            // สิ่งกีดขวางบนพื้น จุดที่ 3 (x=19.5, y=0.70: เสาคริสตัลหิน ความสูงลดลงเพื่อให้กระโดดบนแผ่นเหยียบเท้าได้ง่าย)
            if (megaCrystalPrefab != null)
            {
                GameObject mc = (GameObject)PrefabUtility.InstantiatePrefab(megaCrystalPrefab, chunk.transform);
                mc.transform.localPosition = new Vector3(19.5f, 0.70f, 0f);
                // โค้งเหรียญนำทางการกระโดด (ยอดโค้ง y = 2.4m)
                SpawnCoinArc(chunk.transform, coinPrefab, 17.5f, 21.5f, 2.0f);
            }

            CreateEndPoint(chunk.transform, 26f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(chunk, path);
            Object.DestroyImmediate(chunk);
            return prefab;
        }

        private static GameObject CreateSlidesChunkPrefab(string fileName, Sprite groundSprite, GameObject stalactitePrefab, GameObject stalactiteClusterPrefab, GameObject caveBatPrefab, GameObject coinPrefab, int groundLayer)
        {
            string path = $"{PREFAB_DIR}/{fileName}";
            GameObject chunk = new GameObject(Path.GetFileNameWithoutExtension(fileName));

            CreateGroundBlock(chunk.transform, groundSprite, 0f, 26f, groundLayer);

            // หินย้อยจากเพดานถ้ำ จุดที่ 1 (x=7.5, y=2.05) ปลายหินย้อยลงมาถึงระดับศีรษะ ต้องกด Slide มุดผ่าน
            if (stalactitePrefab != null)
            {
                GameObject s1 = (GameObject)PrefabUtility.InstantiatePrefab(stalactitePrefab, chunk.transform);
                s1.transform.localPosition = new Vector3(7.5f, 2.05f, 0f);

                // เหรียญเตี้ยๆ ด้านล่างให้สไลด์เก็บ
                for (float x = 6f; x <= 9f; x += 1f)
                {
                    GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, chunk.transform);
                    c.transform.localPosition = new Vector3(x, 0.35f, 0f);
                }
            }

            // มอนสเตอร์โครงกระดูกเดินเข้าหาผู้เล่น จุดที่ 2 (x=14, y=0.0) สไลด์เตะมอนสเตอร์ หรือกระโดดข้าม
            if (caveBatPrefab != null)
            {
                GameObject bat = (GameObject)PrefabUtility.InstantiatePrefab(caveBatPrefab, chunk.transform);
                bat.transform.localPosition = new Vector3(14f, 0f, 0f);

                for (float x = 12.5f; x <= 15.5f; x += 1f)
                {
                    GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, chunk.transform);
                    c.transform.localPosition = new Vector3(x, 0.35f, 0f);
                }
            }

            // กลุ่มหินย้อยจากเพดาน จุดที่ 3 (x=20.5, y=2.05) กว้างและอันตราย ต้องกด Slide ยาวลอดผ่าน
            if (stalactiteClusterPrefab != null)
            {
                GameObject s2 = (GameObject)PrefabUtility.InstantiatePrefab(stalactiteClusterPrefab, chunk.transform);
                s2.transform.localPosition = new Vector3(20.5f, 2.05f, 0f);

                for (float x = 18.5f; x <= 22.5f; x += 1f)
                {
                    GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, chunk.transform);
                    c.transform.localPosition = new Vector3(x, 0.35f, 0f);
                }
            }

            CreateEndPoint(chunk.transform, 26f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(chunk, path);
            Object.DestroyImmediate(chunk);
            return prefab;
        }

        private static GameObject CreateMixedChunkPrefab(string fileName, Sprite groundSprite, GameObject rockPrefab, GameObject stalactitePrefab, GameObject megaCrystalPrefab, GameObject caveBatPrefab, GameObject coinPrefab, GameObject potionPrefab, int groundLayer)
        {
            string path = $"{PREFAB_DIR}/{fileName}";
            GameObject chunk = new GameObject(Path.GetFileNameWithoutExtension(fileName));

            CreateGroundBlock(chunk.transform, groundSprite, 0f, 32f, groundLayer);

            // หินบนพื้น (x=6) กระโดดธรรมดา
            if (rockPrefab != null)
            {
                GameObject r = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, chunk.transform);
                r.transform.localPosition = new Vector3(6f, 0.20f, 0f);
                SpawnCoinArc(chunk.transform, coinPrefab, 4f, 8f, 1.25f);
            }

            // ขวดฟื้นเลือด / ไอเทม (x=11, y=1.2)
            if (potionPrefab != null)
            {
                GameObject pot = (GameObject)PrefabUtility.InstantiatePrefab(potionPrefab, chunk.transform);
                pot.transform.localPosition = new Vector3(11f, 1.2f, 0f);
            }

            // หินย้อยจากเพดานถ้ำ (x=15.5, y=2.05) ต้องสไลด์
            if (stalactitePrefab != null)
            {
                GameObject s = (GameObject)PrefabUtility.InstantiatePrefab(stalactitePrefab, chunk.transform);
                s.transform.localPosition = new Vector3(15.5f, 2.05f, 0f);

                for (float x = 14f; x <= 17f; x += 1f)
                {
                    GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, chunk.transform);
                    c.transform.localPosition = new Vector3(x, 0.35f, 0f);
                }
            }

            // มอนสเตอร์โครงกระดูกเดินเข้าหาผู้เล่น (x=21.5, y=0.0) สไลด์เตะมอนสเตอร์ หรือกระโดดข้าม
            if (caveBatPrefab != null)
            {
                GameObject bat = (GameObject)PrefabUtility.InstantiatePrefab(caveBatPrefab, chunk.transform);
                bat.transform.localPosition = new Vector3(21.5f, 0f, 0f);

                for (float x = 20f; x <= 23f; x += 1f)
                {
                    GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, chunk.transform);
                    c.transform.localPosition = new Vector3(x, 0.35f, 0f);
                }
            }

            // เหรียญทางวิ่งระดับพื้นระหว่างมอนสเตอร์กับเสาคริสตัล (x = 24.5)
            for (float x = 24f; x <= 24.5f; x += 1f)
            {
                GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, chunk.transform);
                c.transform.localPosition = new Vector3(x, 0.4f, 0f);
            }

            // เสาคริสตัลยักษ์ (x=27f, y=0.70)
            if (megaCrystalPrefab != null)
            {
                GameObject mc = (GameObject)PrefabUtility.InstantiatePrefab(megaCrystalPrefab, chunk.transform);
                mc.transform.localPosition = new Vector3(27f, 0.70f, 0f);
                SpawnCoinArc(chunk.transform, coinPrefab, 25f, 29f, 2.0f);
            }

            CreateEndPoint(chunk.transform, 32f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(chunk, path);
            Object.DestroyImmediate(chunk);
            return prefab;
        }

        private static GameObject CreatePitSingleChunkPrefab(string fileName, Sprite groundSprite, GameObject rockPrefab, GameObject megaCrystalPrefab, GameObject rockWallPrefab, GameObject coinPrefab, int groundLayer)
        {
            string path = $"{PREFAB_DIR}/{fileName}";
            GameObject chunk = new GameObject(Path.GetFileNameWithoutExtension(fileName));

            // แท่นพื้น 1 (ยาว 10 เมตร: x = 0 ถึง 10)
            CreateGroundBlock(chunk.transform, groundSprite, 0f, 10f, groundLayer);

            // หินบนแท่นแรก (x = 5.5, y = 0.20)
            if (rockPrefab != null)
            {
                GameObject r = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, chunk.transform);
                r.transform.localPosition = new Vector3(5.5f, 0.20f, 0f);
            }

            // หลุมที่ 1 (ช่องว่าง 3.0 เมตร: x = 10 ถึง 13)
            // โค้งเหรียญลอยข้ามหลุม นำทางการกระโดด
            SpawnCoinArc(chunk.transform, coinPrefab, 9.5f, 13.5f, 1.8f);

            // แท่นพื้น 2 (ยาว 15 เมตร: x = 13 ถึง 28)
            CreateGroundBlock(chunk.transform, groundSprite, 13f, 15f, groundLayer);

            // กำแพงหินซ้อนหนาบนแท่นที่ 2 (x = 16.5, y = 0.25) กระโดด 1 ชั้นปกติ (โค้งเหรียญเตี้ย)
            if (rockWallPrefab != null)
            {
                GameObject rw = (GameObject)PrefabUtility.InstantiatePrefab(rockWallPrefab, chunk.transform);
                rw.transform.localPosition = new Vector3(16.5f, 0.25f, 0f);
                SpawnCoinArc(chunk.transform, coinPrefab, 15f, 18f, 1.25f);
            }

            // เหรียญทางวิ่งระดับพื้นระหว่างหินเล็กและหินใหญ่ (x = 19.5, 20.5)
            for (float x = 19.5f; x <= 20.5f; x += 1f)
            {
                GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, chunk.transform);
                c.transform.localPosition = new Vector3(x, 0.4f, 0f);
            }

            // เสาคริสตัลหินบนแท่นที่ 2 (x = 23, y = 0.70)
            if (megaCrystalPrefab != null)
            {
                GameObject mc = (GameObject)PrefabUtility.InstantiatePrefab(megaCrystalPrefab, chunk.transform);
                mc.transform.localPosition = new Vector3(23f, 0.70f, 0f);
                SpawnCoinArc(chunk.transform, coinPrefab, 21f, 25f, 2.0f);
            }

            CreateEndPoint(chunk.transform, 28f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(chunk, path);
            Object.DestroyImmediate(chunk);
            return prefab;
        }

        private static GameObject CreatePitDoubleChunkPrefab(string fileName, Sprite groundSprite, GameObject stalactitePrefab, GameObject coinPrefab, GameObject potionPrefab, int groundLayer)
        {
            string path = $"{PREFAB_DIR}/{fileName}";
            GameObject chunk = new GameObject(Path.GetFileNameWithoutExtension(fileName));

            // แท่นพื้น 1 (x = 0 ถึง 7)
            CreateGroundBlock(chunk.transform, groundSprite, 0f, 7f, groundLayer);

            // หลุมที่ 1 (กว้าง 2.8 เมตร: x = 7 ถึง 9.8)
            SpawnCoinArc(chunk.transform, coinPrefab, 6.5f, 10.3f, 1.9f);

            // เกาะหินกลางเหว (x = 9.8 ถึง 15, ยาว 5.2 เมตร)
            CreateGroundBlock(chunk.transform, groundSprite, 9.8f, 5.2f, groundLayer);

            // หินย้อยจากเพดานบนเกาะหินกลางเหว (x = 12.4, y = 2.05) ต้องสไลด์ผ่านเกาะกลาง!
            if (stalactitePrefab != null)
            {
                GameObject s = (GameObject)PrefabUtility.InstantiatePrefab(stalactitePrefab, chunk.transform);
                s.transform.localPosition = new Vector3(12.4f, 2.05f, 0f);

                for (float x = 11.2f; x <= 13.6f; x += 1.2f)
                {
                    GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, chunk.transform);
                    c.transform.localPosition = new Vector3(x, 0.35f, 0f);
                }
            }

            // หลุมที่ 2 (กว้าง 3.0 เมตร: x = 15 ถึง 18)
            SpawnCoinArc(chunk.transform, coinPrefab, 14.5f, 18.5f, 1.9f);

            // แท่นพื้น 2 (x = 18 ถึง 28, ยาว 10.0 เมตร)
            CreateGroundBlock(chunk.transform, groundSprite, 18f, 10f, groundLayer);

            // ขวดเลือดเป็นรางวัลหลังข้ามหลุมคู่
            if (potionPrefab != null)
            {
                GameObject pot = (GameObject)PrefabUtility.InstantiatePrefab(potionPrefab, chunk.transform);
                pot.transform.localPosition = new Vector3(23f, 1.2f, 0f);
            }

            CreateEndPoint(chunk.transform, 28f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(chunk, path);
            Object.DestroyImmediate(chunk);
            return prefab;
        }

        private static GameObject CreateTallPillarsChunkPrefab(string fileName, Sprite groundSprite, GameObject megaCrystalPrefab, GameObject stalactitePrefab, GameObject coinPrefab, GameObject giantPrefab, int groundLayer)
        {
            string path = $"{PREFAB_DIR}/{fileName}";
            GameObject chunk = new GameObject(Path.GetFileNameWithoutExtension(fileName));

            CreateGroundBlock(chunk.transform, groundSprite, 0f, 28f, groundLayer);

            // ไอเทมร่างยักษ์ (Giant Power-up) ก่อนถึงแนวเสาหินยักษ์
            if (giantPrefab != null)
            {
                GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(giantPrefab, chunk.transform);
                g.transform.localPosition = new Vector3(3.2f, 1.2f, 0f);
            }

            // เสาคริสตัลยักษ์ จุดที่ 1 (x = 7.5, y = 0.70)
            if (megaCrystalPrefab != null)
            {
                GameObject p1 = (GameObject)PrefabUtility.InstantiatePrefab(megaCrystalPrefab, chunk.transform);
                p1.transform.localPosition = new Vector3(7.5f, 0.70f, 0f);
                // โค้งเหรียญนำทางสำหรับแผ่นเหยียบ
                SpawnCoinArc(chunk.transform, coinPrefab, 5f, 10f, 2.0f);
            }

            // หินย้อยจากเพดาน (x = 14.5, y = 2.05) ลงพื้นแล้วสไลด์มุด
            if (stalactitePrefab != null)
            {
                GameObject s = (GameObject)PrefabUtility.InstantiatePrefab(stalactitePrefab, chunk.transform);
                s.transform.localPosition = new Vector3(14.5f, 2.05f, 0f);

                for (float x = 13f; x <= 16f; x += 1f)
                {
                    GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, chunk.transform);
                    c.transform.localPosition = new Vector3(x, 0.35f, 0f);
                }
            }

            // เสาคริสตัลทรงสูง จุดที่ 2 (x = 21.0, y = 0.70)
            if (megaCrystalPrefab != null)
            {
                GameObject p2 = (GameObject)PrefabUtility.InstantiatePrefab(megaCrystalPrefab, chunk.transform);
                p2.transform.localPosition = new Vector3(21.0f, 0.70f, 0f);

                SpawnCoinArc(chunk.transform, coinPrefab, 18.5f, 23.5f, 2.0f);
            }

            CreateEndPoint(chunk.transform, 28f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(chunk, path);
            Object.DestroyImmediate(chunk);
            return prefab;
        }

        private static GameObject CreatePitComboChunkPrefab(string fileName, Sprite groundSprite, GameObject stalactiteClusterPrefab, GameObject megaCrystalPrefab, GameObject rockWallPrefab, GameObject coinPrefab, GameObject potionPrefab, int groundLayer)
        {
            string path = $"{PREFAB_DIR}/{fileName}";
            GameObject chunk = new GameObject(Path.GetFileNameWithoutExtension(fileName));

            // แท่นพื้น 1 (x = 0 ถึง 11)
            CreateGroundBlock(chunk.transform, groundSprite, 0f, 11f, groundLayer);

            // กลุ่มหินย้อยจากเพดาน (x = 6.5, y = 2.05) สไลด์มุดยาว
            if (stalactiteClusterPrefab != null)
            {
                GameObject ceil = (GameObject)PrefabUtility.InstantiatePrefab(stalactiteClusterPrefab, chunk.transform);
                ceil.transform.localPosition = new Vector3(6.5f, 2.05f, 0f);

                for (float x = 5f; x <= 8f; x += 1f)
                {
                    GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, chunk.transform);
                    c.transform.localPosition = new Vector3(x, 0.35f, 0f);
                }
            }

            // สไลด์มุดเสร็จแล้วเจอหลุมทันที! (หลุมกว้าง 3.2 เมตร: x = 11 ถึง 14.2)
            SpawnCoinArc(chunk.transform, coinPrefab, 10.5f, 14.7f, 2.2f);

            // แท่นพื้น 2 (x = 14.2 ถึง 32, ยาว 17.8 เมตร)
            CreateGroundBlock(chunk.transform, groundSprite, 14.2f, 17.8f, groundLayer);

            // กำแพงหินบนแท่นที่ 2 (x = 17.5, y = 0.25)
            if (rockWallPrefab != null)
            {
                GameObject rw = (GameObject)PrefabUtility.InstantiatePrefab(rockWallPrefab, chunk.transform);
                rw.transform.localPosition = new Vector3(17.5f, 0.25f, 0f);
                SpawnCoinArc(chunk.transform, coinPrefab, 16f, 19f, 1.25f);
            }

            // เหรียญทางวิ่งระดับพื้นระหว่างหินเล็กและหินใหญ่ (x = 20.5, 21.5)
            for (float x = 20.5f; x <= 21.5f; x += 1f)
            {
                GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, chunk.transform);
                c.transform.localPosition = new Vector3(x, 0.4f, 0f);
            }

            // เสาคริสตัลยักษ์ทรงสูง (x = 24.5, y = 0.70)
            if (megaCrystalPrefab != null)
            {
                GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(megaCrystalPrefab, chunk.transform);
                p.transform.localPosition = new Vector3(24.5f, 0.70f, 0f);
                SpawnCoinArc(chunk.transform, coinPrefab, 22.5f, 26.5f, 2.0f);
            }

            if (potionPrefab != null)
            {
                GameObject pot = (GameObject)PrefabUtility.InstantiatePrefab(potionPrefab, chunk.transform);
                pot.transform.localPosition = new Vector3(29.5f, 1.2f, 0f);
            }

            CreateEndPoint(chunk.transform, 32f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(chunk, path);
            Object.DestroyImmediate(chunk);
            return prefab;
        }

        private static void CreateGroundBlock(Transform parent, Sprite groundSprite, float startX, float length, int groundLayer)
        {
            GameObject g = new GameObject("GroundBlock");
            g.transform.parent = parent;
            g.layer = groundLayer;
            g.transform.localPosition = new Vector3(startX + (length / 2f), -1f, 0f);
            g.transform.localScale = Vector3.one;

            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = groundSprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(length, 2f);
            sr.sortingOrder = 1;

            BoxCollider2D col = g.AddComponent<BoxCollider2D>();
            col.size = new Vector2(length, 2f);
        }

        private static void CreateEndPoint(Transform parent, float xPos)
        {
            GameObject end = new GameObject("EndPoint");
            end.transform.parent = parent;
            end.transform.localPosition = new Vector3(xPos, 0f, 0f);
        }

        private static void SpawnCoinArc(Transform parent, GameObject coinPrefab, float startX, float endX, float peakY)
        {
            int count = 5;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1);
                float x = Mathf.Lerp(startX, endX, t);
                float y = Mathf.Sin(t * Mathf.PI) * peakY + 0.4f;

                GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, parent);
                c.transform.localPosition = new Vector3(x, y, 0f);
            }
        }
        #endregion

        #region UI Builder
        private static void BuildUI(CookiePlayerController player, CookieHealth health, CookieGameManager gm)
        {
            GameObject canvasObj = new GameObject("Canvas_CookieHUD");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            CookieUIManager uiManager = canvasObj.AddComponent<CookieUIManager>();

            // Health Slider
            GameObject sliderObj = new GameObject("HealthSlider");
            sliderObj.transform.SetParent(canvasObj.transform, false);
            RectTransform sRt = sliderObj.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.05f, 0.92f);
            sRt.anchorMax = new Vector2(0.35f, 0.97f);
            sRt.sizeDelta = Vector2.zero;

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            // Fill Area & Fill
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform faRt = fillArea.AddComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero;
            faRt.anchorMax = Vector2.one;
            faRt.sizeDelta = Vector2.zero;

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform, false);
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.9f, 0.3f, 1f);
            RectTransform fRt = fillObj.GetComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.sizeDelta = Vector2.zero;

            slider.fillRect = fRt;
            sliderObj.SetActive(false); // ซ่อนหลอดเลือดตามที่ผู้ใช้ต้องการนำระบบเลือดออก

            // Active Buff Text (แสดงผลเมื่อเก็บไอเทมพิเศษ เช่น SHIELD, MAGNET, BLAST, GIANT)
            TextMeshProUGUI buffTxt = CreateText(canvasObj.transform, "BuffText", "", new Vector2(0.05f, 0.92f), new Vector2(0.6f, 0.98f), 30, TextAlignmentOptions.Left, Color.white);

            // Score Text
            TextMeshProUGUI scoreTxt = CreateText(canvasObj.transform, "ScoreText", "0", new Vector2(0.7f, 0.92f), new Vector2(0.95f, 0.98f), 40, TextAlignmentOptions.Right, Color.yellow);

            // Coin Text
            TextMeshProUGUI coinTxt = CreateText(canvasObj.transform, "CoinText", "0", new Vector2(0.7f, 0.85f), new Vector2(0.95f, 0.91f), 32, TextAlignmentOptions.Right, Color.white);

            // Distance Text
            TextMeshProUGUI distTxt = CreateText(canvasObj.transform, "DistanceText", "0m", new Vector2(0.05f, 0.85f), new Vector2(0.25f, 0.91f), 32, TextAlignmentOptions.Left, Color.cyan);

            // Jump Touch Button (ล่างขวา) - ซ่อนไว้สำหรับรองรับแผ่นเหยียบเท้า/คีย์บอร์ด
            CookieTouchButton btnJump = CreateTouchButton(canvasObj.transform, "Btn_Jump", "JUMP", new Vector2(0.82f, 0.05f), new Vector2(0.96f, 0.25f), new Color(0.2f, 0.6f, 1f, 0.7f));
            btnJump.gameObject.SetActive(false);

            // Slide Touch Button (ล่างซ้าย) - ซ่อนไว้สำหรับรองรับแผ่นเหยียบเท้า/คีย์บอร์ด
            CookieTouchButton btnSlide = CreateTouchButton(canvasObj.transform, "Btn_Slide", "SLIDE", new Vector2(0.04f, 0.05f), new Vector2(0.18f, 0.25f), new Color(1f, 0.5f, 0.2f, 0.7f));
            btnSlide.gameObject.SetActive(false);

            // Game Over Panel
            GameObject goPanel = new GameObject("GameOverPanel");
            goPanel.transform.SetParent(canvasObj.transform, false);
            Image goBg = goPanel.AddComponent<Image>();
            goBg.color = new Color(0f, 0f, 0f, 0.85f);
            RectTransform goRt = goPanel.GetComponent<RectTransform>();
            goRt.anchorMin = Vector2.zero;
            goRt.anchorMax = Vector2.one;
            goRt.sizeDelta = Vector2.zero;

            CreateText(goPanel.transform, "Title", "GAME OVER", new Vector2(0.3f, 0.7f), new Vector2(0.7f, 0.85f), 64, TextAlignmentOptions.Center, Color.red);
            TextMeshProUGUI goScore = CreateText(goPanel.transform, "FinalScore", "Score: 0", new Vector2(0.3f, 0.55f), new Vector2(0.7f, 0.65f), 36, TextAlignmentOptions.Center, Color.yellow);
            TextMeshProUGUI goCoin = CreateText(goPanel.transform, "FinalCoin", "Coins: 0", new Vector2(0.3f, 0.45f), new Vector2(0.7f, 0.55f), 32, TextAlignmentOptions.Center, Color.white);
            TextMeshProUGUI goBest = CreateText(goPanel.transform, "BestScore", "BEST: 0", new Vector2(0.3f, 0.35f), new Vector2(0.7f, 0.45f), 32, TextAlignmentOptions.Center, Color.cyan);

            // Restart Button
            GameObject restartBtnObj = new GameObject("Btn_Restart");
            restartBtnObj.transform.SetParent(goPanel.transform, false);
            Image rImg = restartBtnObj.AddComponent<Image>();
            rImg.color = new Color(0.2f, 0.8f, 0.3f, 1f);
            Button restartBtn = restartBtnObj.AddComponent<Button>();
            RectTransform rRt = restartBtnObj.GetComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0.4f, 0.15f);
            rRt.anchorMax = new Vector2(0.6f, 0.28f);
            rRt.sizeDelta = Vector2.zero;
            CreateText(restartBtnObj.transform, "Txt", "RESTART", Vector2.zero, Vector2.one, 32, TextAlignmentOptions.Center, Color.black);

            goPanel.SetActive(false);

            // เชื่อมค่าเข้า CookieUIManager
            SerializedObject soUI = new SerializedObject(uiManager);
            soUI.FindProperty("showOnScreenButtons").boolValue = false;
            soUI.FindProperty("showHealthBar").boolValue = false;
            soUI.FindProperty("activeBuffText").objectReferenceValue = buffTxt;
            soUI.FindProperty("healthSlider").objectReferenceValue = slider;
            soUI.FindProperty("healthFillImage").objectReferenceValue = fillImg;
            soUI.FindProperty("scoreText").objectReferenceValue = scoreTxt;
            soUI.FindProperty("coinText").objectReferenceValue = coinTxt;
            soUI.FindProperty("distanceText").objectReferenceValue = distTxt;
            soUI.FindProperty("jumpTouchButton").objectReferenceValue = btnJump;
            soUI.FindProperty("slideTouchButton").objectReferenceValue = btnSlide;
            soUI.FindProperty("gameOverPanel").objectReferenceValue = goPanel;
            soUI.FindProperty("finalScoreText").objectReferenceValue = goScore;
            soUI.FindProperty("finalCoinText").objectReferenceValue = goCoin;
            soUI.FindProperty("highScoreText").objectReferenceValue = goBest;
            soUI.FindProperty("restartButton").objectReferenceValue = restartBtn;
            soUI.ApplyModifiedProperties();
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, float fontSize, TextAlignmentOptions align, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            return tmp;
        }

        private static CookieTouchButton CreateTouchButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Color btnColor)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = Vector2.zero;

            Image img = obj.AddComponent<Image>();
            img.color = btnColor;

            CookieTouchButton btn = obj.AddComponent<CookieTouchButton>();
            CreateText(obj.transform, "Label", label, Vector2.zero, Vector2.one, 32, TextAlignmentOptions.Center, Color.white);
            return btn;
        }
        #endregion

        #region Helpers & Texture Generation
        private static Sprite GetOrCreateSprite(string fileName, int width, int height, Color color)
        {
            string path = $"{SPRITE_DIR}/{fileName}";
            if (!File.Exists(path))
            {
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[width * height];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
                tex.SetPixels(pixels);
                tex.Apply();

                byte[] pngData = tex.EncodeToPNG();
                File.WriteAllBytes(path, pngData);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = 32;
                    importer.filterMode = FilterMode.Point;
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureFolderExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace("\\", "/");
                string folder = Path.GetFileName(path);
                if (!AssetDatabase.IsValidFolder(parent))
                {
                    EnsureFolderExists(parent);
                }
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
            {
                if (s.path == scenePath) return;
            }

            EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            for (int i = 0; i < scenes.Length; i++)
            {
                newScenes[i] = scenes[i];
            }
            newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
        }
        #endregion
    }
}
