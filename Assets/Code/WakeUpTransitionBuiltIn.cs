using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WakeUpTransitionBuiltIn : MonoBehaviour
{
    private RectTransform topEyelid;
    private RectTransform bottomEyelid;
    private Image flashOverlay; // เลเยอร์แสงจ้าสีขาว

    void Awake()
    {
        SetupRuntimeUI();
    }

    void SetupRuntimeUI()
    {
        // 1. สร้าง Canvas รองรับทุกขนาดหน้าจอ
        GameObject canvasObj = new GameObject("TransitionCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasObj.AddComponent<CanvasScaler>();

        // 2. สร้างเปลือกตาบนและล่าง (สีดำ)
        topEyelid = CreateOverlay(canvasObj.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), Color.black);
        bottomEyelid = CreateOverlay(canvasObj.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0f), Color.black);

        // 3. สร้างเลเยอร์แสงวาบตา (สีขาวโปร่งแสง)
        RectTransform flashRect = CreateOverlay(canvasObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Color(1, 1, 1, 0));
        flashRect.sizeDelta = Vector2.zero;
        flashOverlay = flashRect.GetComponent<Image>();
    }

    RectTransform CreateOverlay(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Color color)
    {
        GameObject obj = new GameObject("OverlayItem", typeof(Image));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().color = color;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = new Vector2(0, Screen.height / 2f + 50f);
        return rect;
    }

    public void PlayTransition()
    {
        StartCoroutine(WakeUpSequence());
    }

    IEnumerator WakeUpSequence()
    {
        float screenH = Screen.height / 2f + 50f;

        // เริ่มต้น: หลับตาสนิท
        SetEyelidHeight(screenH);
        flashOverlay.color = new Color(1, 1, 1, 0.7f); // แสงขาวจำลองความสว่างจ้าตอนเริ่ม

        yield return new WaitForSeconds(0.6f);

        // แง้มตาครั้งที่ 1 (เปิด 30% แล้วหลับลง)
        yield return MoveEyes(screenH * 0.7f, 0.35f);
        yield return MoveEyes(screenH, 0.25f);
        yield return new WaitForSeconds(0.2f);

        // ลืมตาขึ้นทั้งหมด
        StartCoroutine(MoveEyes(0f, 1.2f));

        // ค่อยๆ ปรับแสงสีขาวให้จางหายไป (เลียนแบบสายตาปรับแสง)
        float elapsed = 0f;
        float duration = 1.2f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0.7f, 0f, elapsed / duration);
            flashOverlay.color = new Color(1, 1, 1, alpha);
            yield return null;
        }
        flashOverlay.color = new Color(1, 1, 1, 0);
    }

    IEnumerator MoveEyes(float targetHeight, float duration)
    {
        float time = 0;
        float startHeight = topEyelid.sizeDelta.y;

        while (time < duration)
        {
            time += Time.deltaTime;
            float h = Mathf.Lerp(startHeight, targetHeight, time / duration);
            SetEyelidHeight(h);
            yield return null;
        }
        SetEyelidHeight(targetHeight);
    }

    void SetEyelidHeight(float height)
    {
        topEyelid.sizeDelta = new Vector2(topEyelid.sizeDelta.x, height);
        bottomEyelid.sizeDelta = new Vector2(bottomEyelid.sizeDelta.x, height);
    }
    void Start()
    {
    PlayTransition(); // สั่งให้เล่นแอนิเมชันลืมตาทันทีเมื่อฉากนี้ถูกโหลดขึ้นมา
    }
}