using UnityEngine;
using System.Collections.Generic;

public class ObjectiveArrow : MonoBehaviour
{
    public enum ArrowTargetMode
    {
        Nearest,        // ชี้หาแท่นที่ใกล้ตัวผู้เล่นที่สุดอัตโนมัติ
        Sequential,     // ชี้เรียงตามลำดับ (1 -> 2 -> 3) เมื่อเดินไปถึงจุดปัจจุบัน
        SingleTarget    // ชี้เป้าหมายเดี่ยวที่กำหนดไว้
    }

    [Header("1. โหมดการทำงาน")]
    public ArrowTargetMode targetMode = ArrowTargetMode.Nearest;

    [Header("2. รายการแท่นวาร์ป / จุดหมาย (หลายจุด)")]
    [Tooltip("ลากแท่นวาร์ปทั้งหมดในฉากมาใส่ที่นี่")]
    public List<Transform> warpPoints = new List<Transform>();

    [Tooltip("เป้าหมายเดี่ยว (เช่น ประตูทางออก หรือจุดเควสต์เฉพาะ)")]
    public Transform singleTarget;

    [Tooltip("ตัวละครผู้เล่น (ถ้าเว้นว่างไว้จะค้นหาจาก Tag 'Player' ให้อัตโนมัติ)")]
    public Transform player;

    [Header("3. ตำแหน่งและการโคจรรอบตัว")]
    [Tooltip("ระยะห่างจากศูนย์กลางตัวผู้เล่น")]
    public float orbitRadius = 1.3f;

    [Header("4. การย่อ-ขยายตามระยะทาง")]
    public float minDistance = 2.0f;
    public float maxDistance = 15.0f;
    public float minScale = 0.35f;
    public float maxScale = 1.0f;

    [Header("5. การซ่อนลูกศรเมื่อถึงจุดหมาย")]
    public bool hideWhenArrived = true;
    public float arrivalDistance = 1.0f;

    [Header("6. การปรับองศารูปภาพ (Sprite Offset)")]
    public float rotationOffset = 0f;

    private SpriteRenderer spriteRenderer;
    private Transform currentTarget;
    private int sequentialIndex = 0;

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
        UpdateCurrentTarget();

        if (currentTarget == null || player == null)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            return;
        }

        Vector2 playerPos = player.position;
        Vector2 targetPos = currentTarget.position;
        Vector2 direction = targetPos - playerPos;
        float distance = direction.magnitude;

        // ตรวจสอบเงื่อนไขเมื่อเดินถึงจุดหมาย
        if (distance <= arrivalDistance)
        {
            if (targetMode == ArrowTargetMode.Sequential)
            {
                // เลื่อนไปชี้แท่นวาร์ปถัดไปในลิสต์
                if (sequentialIndex < warpPoints.Count - 1)
                {
                    sequentialIndex++;
                }
                else if (hideWhenArrived)
                {
                    if (spriteRenderer != null) spriteRenderer.enabled = false;
                    return;
                }
            }
            else if (hideWhenArrived)
            {
                if (spriteRenderer != null) spriteRenderer.enabled = false;
                return;
            }
        }
        else
        {
            if (spriteRenderer != null) spriteRenderer.enabled = true;
        }

        // 1. หมุนลูกศรให้ชี้ตรงไปหาจุดหมาย
        direction.Normalize();
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);

        // 2. จัดตำแหน่งลูกศรให้วนรอบตัวผู้เล่น
        transform.position = playerPos + (direction * orbitRadius);

        // 3. ปรับขนาดสเกลตามระยะทาง (ยิ่งใกล้ = สเกลยิ่งเล็ก)
        float t = Mathf.InverseLerp(minDistance, maxDistance, distance);
        float currentScale = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = new Vector3(currentScale, currentScale, 1f);
    }

    void UpdateCurrentTarget()
    {
        switch (targetMode)
        {
            case ArrowTargetMode.SingleTarget:
                currentTarget = singleTarget;
                break;

            case ArrowTargetMode.Sequential:
                if (warpPoints != null && warpPoints.Count > 0 && sequentialIndex < warpPoints.Count)
                {
                    currentTarget = warpPoints[sequentialIndex];
                }
                break;

            case ArrowTargetMode.Nearest:
                currentTarget = GetNearestWarpPoint();
                break;
        }
    }

    Transform GetNearestWarpPoint()
    {
        if (warpPoints == null || warpPoints.Count == 0 || player == null) return null;

        Transform nearest = null;
        float shortestDistance = Mathf.Infinity;
        Vector2 playerPos = player.position;

        foreach (Transform wp in warpPoints)
        {
            if (wp == null || !wp.gameObject.activeInHierarchy) continue;

            float dist = Vector2.Distance(playerPos, wp.position);
            if (dist < shortestDistance)
            {
                shortestDistance = dist;
                nearest = wp;
            }
        }

        return nearest;
    }

    // ฟังก์ชันสำหรับกำหนดเป้าหมายเดี่ยวจากภายนอก (เช่น ประตูทางออกตอนจบด่าน)
    public void SetTarget(Transform newTarget)
    {
        singleTarget = newTarget;
        targetMode = ArrowTargetMode.SingleTarget;
    }
}