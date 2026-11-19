using UnityEngine;

public class HighlightPulse : MonoBehaviour
{
    [Header("Pulse Settings")]
    public float pulseSpeed = 4f;       // ความเร็วในการเต้น/กระพริบ
    public float minScale = 0.95f;      // ขนาดเล็กสุด
    public float maxScale = 1.08f;      // ขนาดใหญ่สุด

    private Vector3 initialScale;

    void Awake()
    {
        initialScale = transform.localScale;
    }

    void OnEnable()
    {
        transform.localScale = initialScale;
    }

    void Update()
    {
        // คำนวณคลื่น Sine เพื่อย่อ-ขยายกรอบแบบนุ่มนวล
        float scaleFactor = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
        transform.localScale = initialScale * scaleFactor;
    }
}