using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class UIHighlightLayerFixer : MonoBehaviour
{
    [Header("Layer & Sorting Settings")]
    [Tooltip("ชื่อ Layer ที่ต้องการบังคับใช้")]
    public string targetLayerName = "UI";

    [Tooltip("ลำดับความลึกของการวาด (ยิ่งค่าเยอะ ยิ่งอยู่หน้าสุด แนะนำ 500 - 1000)")]
    public int sortingOrder = 500;

    [Header("Auto Fix Properties")]
    [Tooltip("ดึงแกน Z กลับมาที่ 0 เพื่อไม่ให้ภาพจมลงไปหลังปุ่ม")]
    public bool fixZDepth = true;

    [Tooltip("ป้องกัน Scale กลายเป็น 0 (รีเซ็ตกลับมาเป็น 1 ทันทีที่เปิดใช้งาน)")]
    public bool fixZeroScale = true;

    [Tooltip("ป้องกันสี Alpha เป็น 0 (ปรับให้มองเห็นภาพทึบ 100%)")]
    public bool fixTransparentAlpha = true;

    private Canvas overrideCanvas;
    private RectTransform rectTransform;
    private Image uiImage;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        uiImage = GetComponent<Image>();
        ApplyLayerAndSortingFix();
    }

    void OnEnable()
    {
        ApplyLayerAndSortingFix();
    }

    void LateUpdate()
    {
        // ตรวจสอบและดึงให้อยู่หน้าสุดของ Hierarchy เสมอ
        transform.SetAsLastSibling();

        // ป้องกันสคริปต์อื่นแอบมาดึง Scale กลายเป็น 0
        if (fixZeroScale && (transform.localScale.x <= 0.001f || transform.localScale.y <= 0.001f))
        {
            transform.localScale = Vector3.one;
        }

        // ป้องกันแกน Z เด้งไปค่าบวก (จมลงหลังจอ)
        if (fixZDepth && transform.localPosition.z != 0f)
        {
            Vector3 pos = transform.localPosition;
            pos.z = 0f;
            transform.localPosition = pos;
        }
    }

    [ContextMenu("Force Apply Fix")]
    public void ApplyLayerAndSortingFix()
    {
        // 1. ปรับ Layer ให้เป็น UI
        int layerID = LayerMask.NameToLayer(targetLayerName);
        if (layerID != -1)
        {
            gameObject.layer = layerID;
        }

        // 2. ติดตั้ง Canvas แยกและดัน Sorting Order ให้อยู่หน้าสุด
        overrideCanvas = GetComponent<Canvas>();
        if (overrideCanvas == null)
        {
            overrideCanvas = gameObject.AddComponent<Canvas>();
        }
        overrideCanvas.overrideSorting = true;
        overrideCanvas.sortingOrder = sortingOrder;

        // ไม่ให้ตัวไฮไลต์ไปบล็อกการกดคลิกปุ่มด้านหลัง
        if (uiImage != null)
        {
            uiImage.raycastTarget = false;

            if (fixTransparentAlpha)
            {
                Color c = uiImage.color;
                if (c.a <= 0.01f)
                {
                    c.a = 1f;
                    uiImage.color = c;
                }
            }
        }

        // 3. ปรับ Scale และแกน Z
        if (fixZeroScale && (transform.localScale.x <= 0.001f || transform.localScale.y <= 0.001f))
        {
            transform.localScale = Vector3.one;
        }

        if (fixZDepth)
        {
            Vector3 pos = transform.localPosition;
            pos.z = 0f;
            transform.localPosition = pos;
        }

        transform.SetAsLastSibling();
    }
}