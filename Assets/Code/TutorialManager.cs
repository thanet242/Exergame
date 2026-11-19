using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Warp Point Reference")]
    [Tooltip("ลาก GameObject WarpPoint ที่มีสคริปต์ WarpZone มาใส่ช่องนี้")]
    public WarpZone stageWarpZone;

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI tutorialText;
    private CanvasGroup canvasGroup;
    private Camera mainCam;

    [Header("Follow Player Settings")]
    [SerializeField] private Vector3 offsetAboveHead = new Vector3(0f, 1.2f, 0f);

    [Header("Player & Target Teleport Settings")]
    [SerializeField] private PlayerController playerController;
    [Tooltip("พิกัด Grid สำหรับส่งตัวละครไปหลังสอนเดินเสร็จ (Step 1)")]
    [SerializeField] private Vector3Int targetCell = new Vector3Int(0, 0, 0);
    [SerializeField] private float delayBeforeTeleport = 1.5f;

    [Header("Rock & Bomb Spawn Settings")]
    [SerializeField] private TileBase rockTile;
    [Tooltip("พิกัดหินสำหรับซ้อมขุด (Step 2)")]
    [SerializeField] private Vector3Int firstRockSpawnCell = new Vector3Int(0, 1, 0);
    [Tooltip("พิกัดหินระเบิดพร้อมตัวเลขรอบๆ (Step 3)")]
    [SerializeField] private Vector3Int bombRockSpawnCell = new Vector3Int(0, 1, 0);
    [Tooltip("จุดที่ส่งผู้เล่นไปกรณีเผลอขุดระเบิดใน Step 3")]
    [SerializeField] private Vector3Int retryPlayerCell = new Vector3Int(0, 0, 0);

    [Header("Exit Loop Effect Settings")]
    [Tooltip("ลาก Prefab เอฟเฟกต์ทางออกที่ต้องการให้เล่นวนลูปมาใส่ช่องนี้")]
    [SerializeField] private GameObject exitEffectPrefab;
    [Tooltip("ลาก GameObject WarpPoint มาใส่ (หากปล่อยว่าง ระบบจะดึงจาก stageWarpZone หรือค้นหาในฉากให้อัตโนมัติ)")]
    [SerializeField] private Transform exitPoint;
    [Tooltip("ปรับขนาดเอฟเฟกต์ เช่น (0.5, 0.5, 1) คือย่อลงครึ่งหนึ่ง")]
    [SerializeField] private Vector3 exitEffectScale = new Vector3(0.5f, 0.5f, 1f);
    private bool isExitEffectSpawned = false;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.8f;
    [SerializeField] private float delayBeforeNextStep = 0.4f;

    public enum TutorialTriggerType
    {
        Move,          
        DoubleTapDig,  
        FlagHold,      
        AnyKey,
        TimeOnly,
        RockDestroyed  
    }

    [System.Serializable]
    public class TutorialStep
    {
        [TextArea(2, 3)]
        public string message;
        public TutorialTriggerType triggerToDismiss;
        [Tooltip("จำนวนวินาทีก่อนข้อความจะหายไปเอง (ใส่ 0 หากต้องการให้รอการกดปุ่ม)")]
        public float autoDismissDuration = 0f;
    }

    [Header("Tutorial Sequence")]
    [SerializeField] private List<TutorialStep> tutorialSteps = new List<TutorialStep>();
    
    private int currentStepIndex = 0;
    private bool isFading = false;
    private bool isWaitingForAction = false;
    private Coroutine autoDismissCoroutine;

    private float lastTapTime = -1f;
    private KeyCode lastTappedKey = KeyCode.None;
    private float holdTimer = 0f;
    private bool isTutorialCompleted = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        mainCam = Camera.main;

        if (tutorialText != null)
        {
            canvasGroup = tutorialText.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = tutorialText.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }

        // ค้นหา WarpZone อัตโนมัติหากไม่ได้ลากใส่ไว้ใน Inspector
        if (stageWarpZone == null)
        {
            stageWarpZone = FindObjectOfType<WarpZone>();
        }

        if (exitPoint == null)
        {
            if (stageWarpZone != null)
            {
                exitPoint = stageWarpZone.transform;
            }
            else
            {
                GameObject warpObj = GameObject.Find("WarpPoint");
                if (warpObj != null) exitPoint = warpObj.transform;
            }
        }
    }

    private void Start()
    {
        if (tutorialSteps.Count > 0)
        {
            StartCoroutine(ShowStepRoutine(0));
        }
    }

    private void LateUpdate()
    {
        if (tutorialText == null || playerController == null) return;

        if (mainCam == null)
        {
            mainCam = Camera.main;
            if (mainCam == null) return;
        }

        Vector3 targetWorldPos = playerController.transform.position + offsetAboveHead;
        Vector3 screenPos = mainCam.WorldToScreenPoint(targetWorldPos);
        screenPos.z = 0f;

        Canvas parentCanvas = tutorialText.canvas;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform, screenPos, parentCanvas.worldCamera, out Vector2 localPoint))
            {
                tutorialText.rectTransform.anchoredPosition = localPoint;
            }
        }
        else
        {
            tutorialText.transform.position = screenPos;
        }
    }

    private void Update()
    {
        if (!isWaitingForAction || isFading || currentStepIndex >= tutorialSteps.Count) return;

        TutorialTriggerType targetAction = tutorialSteps[currentStepIndex].triggerToDismiss;

        if (currentStepIndex == 1 || targetAction == TutorialTriggerType.RockDestroyed || targetAction == TutorialTriggerType.TimeOnly)
        {
            return;
        }

        if (CheckPlayerAction(targetAction))
        {
            CompleteCurrentStep();
        }
    }

    private bool CheckPlayerAction(TutorialTriggerType actionType)
    {
        // ปุ่มเดินตามที่ตั้งไว้ในหน้า Option (รวมแผ่นเหยียบด้วย)
        KeyCode[] moveKeys = GameInputBindings.MoveKeys;

        switch (actionType)
        {
            case TutorialTriggerType.Move:
                foreach (KeyCode key in moveKeys)
                {
                    if (Input.GetKeyDown(key)) return true;
                }
                break;

            case TutorialTriggerType.DoubleTapDig:
                foreach (KeyCode key in moveKeys)
                {
                    if (Input.GetKeyDown(key))
                    {
                        if (key == lastTappedKey && (Time.time - lastTapTime) <= 0.4f)
                        {
                            lastTapTime = -1f;
                            lastTappedKey = KeyCode.None;
                            return true;
                        }
                        lastTappedKey = key;
                        lastTapTime = Time.time;
                    }
                }
                break;

            case TutorialTriggerType.FlagHold:
                foreach (KeyCode key in moveKeys)
                {
                    if (Input.GetKey(key))
                    {
                        holdTimer += Time.deltaTime;
                        if (holdTimer >= 0.6f) return true;
                    }
                    if (Input.GetKeyUp(key))
                    {
                        holdTimer = 0f;
                    }
                }
                break;

            case TutorialTriggerType.AnyKey:
                if (Input.anyKeyDown) return true;
                break;
        }

        return false;
    }

    public void OnRockDestroyed()
    {
        if (currentStepIndex < tutorialSteps.Count)
        {
            if (currentStepIndex == 1 || tutorialSteps[currentStepIndex].triggerToDismiss == TutorialTriggerType.RockDestroyed)
            {
                CompleteCurrentStep();
            }
        }
    }

    public void OnFlagPlaced()
    {
        if (currentStepIndex < tutorialSteps.Count)
        {
            if (tutorialSteps[currentStepIndex].triggerToDismiss == TutorialTriggerType.FlagHold)
            {
                CompleteCurrentStep();
            }
        }
    }

    public void OnTutorialBombDug(Vector3Int cell)
    {
        if (currentStepIndex == 2 && cell == bombRockSpawnCell)
        {
            if (autoDismissCoroutine != null)
            {
                StopCoroutine(autoDismissCoroutine);
            }
            StartCoroutine(RespawnBombAndAdvanceRoutine());
        }
    }

    private IEnumerator RespawnBombAndAdvanceRoutine()
    {
        yield return new WaitForSeconds(0.8f);

        if (playerController != null)
        {
            playerController.TeleportToCell(retryPlayerCell, Vector2.up);
            playerController.TakeDamage(-playerController.maxHealth);
            playerController.SpawnTutorialBombWithNumbers(bombRockSpawnCell, rockTile);
        }

        CompleteCurrentStep();
    }

    public void CompleteCurrentStep()
    {
        if (isFading) return;
        if (autoDismissCoroutine != null)
        {
            StopCoroutine(autoDismissCoroutine);
            autoDismissCoroutine = null;
        }
        StartCoroutine(FadeOutAndNextRoutine());
    }

    private IEnumerator ShowStepRoutine(int index)
    {
        if (index >= tutorialSteps.Count) yield break;

        currentStepIndex = index;
        tutorialText.text = tutorialSteps[currentStepIndex].message;
        holdTimer = 0f;

        if (currentStepIndex == 2 && playerController != null)
        {
            playerController.SpawnTutorialBombWithNumbers(bombRockSpawnCell, rockTile);
        }

        // เมื่อถึง Element สุดท้าย ให้เสกเอฟเฟกต์ที่ทางออกทันที
        if (currentStepIndex == tutorialSteps.Count - 1)
        {
            SpawnExitLoopEffect();
        }

        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / 0.3f);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        isWaitingForAction = true;

        float autoTime = tutorialSteps[currentStepIndex].autoDismissDuration;
        if (autoTime > 0f)
        {
            autoDismissCoroutine = StartCoroutine(AutoDismissTimerRoutine(autoTime));
        }
    }

    private IEnumerator AutoDismissTimerRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        CompleteCurrentStep();
    }

    private IEnumerator FadeOutAndNextRoutine()
    {
        isWaitingForAction = false;
        isFading = true;

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        isFading = false;

        if (currentStepIndex == 0)
        {
            yield return StartCoroutine(TeleportAndSpawnRoutine());
        }

        yield return new WaitForSeconds(delayBeforeNextStep);

        if (currentStepIndex + 1 < tutorialSteps.Count)
        {
            StartCoroutine(ShowStepRoutine(currentStepIndex + 1));
        }
        else
        {
            SpawnExitLoopEffect();
            tutorialText.gameObject.SetActive(false);
            
            // เรียกฟังก์ชันจบฝึกสอนเมื่อครบทุกขั้นตอน
            CompleteTutorial();
        }
    }

    // ฟังก์ชันตอนที่ผู้เล่นทำเควสต์ฝึกสอนครบทุกข้อแล้ว
    public void CompleteTutorial()
    {
        if (isTutorialCompleted) return;
        isTutorialCompleted = true;

        // สั่งปลดล็อกจุดวาร์ปและเปิดลูกศรนำทาง
        if (stageWarpZone != null)
        {
            stageWarpZone.UnlockWarp();
        }
        else
        {
            Debug.LogWarning("[TutorialManager] ยังไม่ได้ใส่ Stage Warp Zone!");
        }

        Debug.Log("<color=green>[TutorialManager]</color> ฝึกสอนเสร็จสิ้น! จุดวาร์ปปลดล็อกแล้ว");
    }

    private void SpawnExitLoopEffect()
    {
        if (isExitEffectSpawned) return;

        if (exitPoint == null)
        {
            if (stageWarpZone != null)
            {
                exitPoint = stageWarpZone.transform;
            }
            else
            {
                GameObject warpObj = GameObject.Find("WarpPoint");
                if (warpObj != null) exitPoint = warpObj.transform;
            }
        }

        if (exitPoint == null || exitEffectPrefab == null) return;

        isExitEffectSpawned = true;

        Vector3 spawnPos = exitPoint.position;
        spawnPos.z = -1f;

        GameObject fx = Instantiate(exitEffectPrefab, spawnPos, Quaternion.identity);
        fx.transform.localScale = exitEffectScale;

        ParticleSystem[] allPS = fx.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in allPS)
        {
            var main = ps.main;
            main.loop = true;
            main.stopAction = ParticleSystemStopAction.None;
            if (!ps.isPlaying) ps.Play();
        }

        Renderer[] allRenderers = fx.GetComponentsInChildren<Renderer>();
        foreach (var r in allRenderers)
        {
            r.sortingOrder = 100;
        }
    }

    private IEnumerator TeleportAndSpawnRoutine()
    {
        yield return new WaitForSeconds(delayBeforeTeleport);

        if (playerController != null)
        {
            playerController.TeleportToCell(targetCell);

            if (playerController.map2Tilemap != null && rockTile != null)
            {
                playerController.map2Tilemap.SetTile(firstRockSpawnCell, rockTile);
            }
        }
    }
}