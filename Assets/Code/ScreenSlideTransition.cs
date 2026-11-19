using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScreenSlideTransition : MonoBehaviour
{
    public static ScreenSlideTransition Instance { get; private set; }

    [Header("UI Reference")]
    [Tooltip("ลาก RectTransform ของแผ่น Transition Panel มาใส่")]
    public RectTransform transitionPanel;

    [Header("Slide Settings")]
    [Tooltip("ความเร็วในการเลื่อน (วินาที) ยิ่งน้อยยิ่งเร็ว เช่น 0.25 - 0.35")]
    public float slideDuration = 0.3f;

    [Tooltip("เส้นโค้งความเร่ง (Ease In-Out)")]
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Canvas canvas;
    private bool isTransitioning = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        canvas = GetComponentInParent<Canvas>();

        // ซ่อนพาเนลไว้นอกจอด้านขวาตั้งแต่เริ่ม
        if (transitionPanel != null)
        {
            float screenWidth = GetCanvasWidth();
            transitionPanel.anchoredPosition = new Vector2(screenWidth, 0f);
            transitionPanel.gameObject.SetActive(false);
        }
    }

    float GetCanvasWidth()
    {
        if (canvas != null)
        {
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null) return canvasRect.rect.width;
        }
        return Screen.width;
    }

    // ฟังก์ชันสั่งเปลี่ยนฉากพร้อมเอฟเฟกต์เลื่อนไปทางซ้าย
    public void LoadSceneWithSlide(string sceneName)
    {
        if (isTransitioning) return;
        StartCoroutine(SlideAndLoadRoutine(sceneName));
    }

    private IEnumerator SlideAndLoadRoutine(string sceneName)
    {
        isTransitioning = true;
        float screenWidth = GetCanvasWidth();

        if (transitionPanel != null)
        {
            transitionPanel.gameObject.SetActive(true);

            // 1. สไลด์จากขวาเข้ามาตรงกลางจอ (Right -> Center)
            Vector2 startPos = new Vector2(screenWidth, 0f);
            Vector2 centerPos = Vector2.zero;

            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = slideCurve.Evaluate(elapsed / slideDuration);
                transitionPanel.anchoredPosition = Vector2.Lerp(startPos, centerPos, t);
                yield return null;
            }
            transitionPanel.anchoredPosition = centerPos;
        }

        // 2. โหลดฉากใหม่
        Time.timeScale = 1f;
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // รอ 1 เฟรมเพื่อให้ฉากใหม่และกล้องพร้อมทำงาน
        yield return new WaitForEndOfFrame();

        // 3. สไลด์จากตรงกลางออกไปทางซ้าย (Center -> Left)
        if (transitionPanel != null)
        {
            Vector2 centerPos = Vector2.zero;
            Vector2 endPos = new Vector2(-screenWidth, 0f);

            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = slideCurve.Evaluate(elapsed / slideDuration);
                transitionPanel.anchoredPosition = Vector2.Lerp(centerPos, endPos, t);
                yield return null;
            }

            // ซ่อนพาเนลและรีเซ็ตกลับไปฝั่งขวาเพื่อเตรียมใช้งานรอบถัดไป
            transitionPanel.anchoredPosition = new Vector2(screenWidth, 0f);
            transitionPanel.gameObject.SetActive(false);
        }

        isTransitioning = false;
    }
}