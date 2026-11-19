using UnityEngine;

public class ObjectiveArrow : MonoBehaviour
{
    [Header("1. วัตถุอ้างอิง (Target & Player)")]
    [Tooltip("จุดหมายปลายทาง (เช่น Portal ทางออก หรือตำแหน่งเควสต์)")]
    public Transform targetDestination;

    [Tooltip("ตัวละครผู้เล่น (ถ้าเว้นว่างไว้ ระบบจะค้นหาจาก Tag 'Player' ให้อัตโนมัติ)")]
    public Transform player;

    [Header("2. ตำแหน่งและการโคจรรอบตัวผู้เล่น")]
    [Tooltip("ระยะห่างจากศูนย์กลางตัวผู้เล่น")]
    public float orbitRadius = 1.2f;

    [Header("3. การย่อ-ขยายตามระยะทาง")]
    [Tooltip("ระยะห่างที่ถือว่าใกล้มาก (ลูกศรจะเล็กที่สุด)")]
    public float minDistance = 2.0f;

    [Tooltip("ระยะห่างที่ถือว่าไกล (ลูกศรจะใหญ่ที่สุด)")]
    public float maxDistance = 15.0f;

    [Tooltip("ขนาดเล็กสุด (เมื่ออยู่ใกล้จุดหมาย)")]
    public float minScale = 0.35f;

    [Tooltip("ขนาดใหญ่สุด (เมื่ออยู่ไกลจากจุดหมาย)")]
    public float maxScale = 1.0f;

    [Header("4. การซ่อนลูกศรเมื่อถึงจุดหมาย")]
    public bool hideWhenArrived = true;
    public float arrivalDistance = 1.0f;

    [Header("5. การปรับองศารูปภาพ (Sprite Offset)")]
    [Tooltip("ถ้าภาพลูกศรเดิมชี้ขึ้นด้านบน ให้ปรับเป็น -90 (ถ้าภาพเดิมชี้ไปทางขวาอยู่แล้วให้ใส่ 0)")]
    public float rotationOffset = 0f;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    void LateUpdate()
    {
        // หากยังไม่มีเป้าหมาย ให้ซ่อนลูกศร
        if (targetDestination == null || player == null)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            return;
        }

        Vector2 playerPos = player.position;
        Vector2 targetPos = targetDestination.position;
        Vector2 direction = targetPos - playerPos;
        float distance = direction.magnitude;

        // เมื่อเดินถึงจุดหมาย ให้ซ่อนลูกศร
        if (hideWhenArrived && distance <= arrivalDistance)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            return;
        }
        else
        {
            if (spriteRenderer != null) spriteRenderer.enabled = true;
        }

        // 1. หมุนลูกศรให้ชี้ตรงไปหาจุดหมาย
        direction.Normalize();
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);

        // 2. จัดตำแหน่งลูกศรให้ลอยวนรอบตัวผู้เล่นตามทิศทางที่ชี้ไป
        transform.position = playerPos + (direction * orbitRadius);

        // 3. ปรับขนาดสเกลตามระยะทาง (ยิ่งใกล้ = สเกลยิ่งเล็ก)
        float t = Mathf.InverseLerp(minDistance, maxDistance, distance);
        float currentScale = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = new Vector3(currentScale, currentScale, 1f);
    }

    // ฟังก์ชันสั่งเปลี่ยนเป้าหมายใหม่ผ่านโค้ด
    public void SetTarget(Transform newTarget)
    {
        targetDestination = newTarget;
    }
}