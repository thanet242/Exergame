using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// มอนสเตอร์ที่ซ่อนอยู่ใต้หินในโหมดเหมือง (ขุดเจอแล้วโผล่ออกมาไล่ผู้เล่น)
/// - โผล่มาแล้วมึนงงสักครู่ ให้ผู้เล่นตั้งตัว
/// - เดินทีละช่องตามตารางเหมือนผู้เล่น ไม่ทะลุหิน/ธง
/// - โจมตีเมื่ออยู่ติดผู้เล่น โดยง้างก่อน (เปลี่ยนสี) ผู้เล่นก้าวหนีทันจะไม่โดน
/// - มีหลอดเลือดแถบบนจอ (MonsterHealthHud) กระพริบเมื่อโดนตี ตายแล้วนับเป็นการเคลียร์ 1 จุด
/// </summary>
public class MonsterChaserAI : MonoBehaviour
{
    public static readonly List<MonsterChaserAI> Active = new List<MonsterChaserAI>();

    [Header("Stats")]
    public float maxHealth = 40f;
    public float monsterDamage = 10f;
    [Tooltip("เวลาพักระหว่างการโจมตีแต่ละครั้ง (วินาที)")]
    public float attackCooldown = 1.4f;
    [Tooltip("เวลาง้างก่อนโจมตี ผู้เล่นก้าวออกจากช่องข้างๆ ทันจะไม่โดน")]
    public float attackWindup = 0.4f;
    [Tooltip("เวลามึนงงหลังโผล่จากหิน")]
    public float spawnStunDuration = 1.0f;

    private PlayerController playerScript;
    private Animator monsterAnim;
    private Transform visualRoot;
    private SpriteRenderer[] renderers;
    private Color[] baseColors;

    private float currentHealth;
    private float stepInterval = 0.65f;
    private float stepDuration = 0.22f;
    private float stunTimer;
    private float nextStepTime;
    private float nextAttackTime;
    private float flashTimer;
    private bool isMoving;
    private bool isAttacking;
    private bool isDead;
    private Vector3Int cell;

    // หลอดเลือดอยู่แถบบนจอ (ไม่วางบนหัวมอนสเตอร์ เพราะจะไปบังหินช่องด้านบน)
    private MonsterHealthHud.Row hpRow;

    public Vector3Int Cell => cell;

    public bool IsMonsterDead()
    {
        return isDead;
    }

    /// <summary>ช่องนี้มีมอนสเตอร์ที่ยังมีชีวิตยืนอยู่หรือไม่</summary>
    public static bool IsCellOccupied(Vector3Int target, MonsterChaserAI except = null)
    {
        foreach (MonsterChaserAI m in Active)
        {
            if (m != null && m != except && !m.isDead && m.cell == target) return true;
        }
        return false;
    }

    /// <summary>ทำดาเมจมอนสเตอร์ทุกตัวในรัศมี (เช่น แรงระเบิด)</summary>
    public static void DamageInRadius(Vector3 center, float radius, float damage)
    {
        for (int i = Active.Count - 1; i >= 0; i--)
        {
            MonsterChaserAI m = Active[i];
            if (m != null && !m.isDead && Vector2.Distance(m.transform.position, center) <= radius) m.TakeDamage(damage);
        }
    }

    public void Setup(PlayerController player, float speed)
    {
        playerScript = player;
        // ความเร็วเดิมเป็นหน่วย/วินาที แปลงเป็นเวลาต่อหนึ่งก้าว (2.5 -> ประมาณ 0.65 วินาที)
        stepInterval = Mathf.Clamp(1.6f / Mathf.Max(0.1f, speed), 0.4f, 1.2f);

        monsterAnim = GetComponentInChildren<Animator>();
        visualRoot = transform.childCount > 0 ? transform.GetChild(0) : transform;

        // เดินด้วยตาราง ไม่ใช้ฟิสิกส์ผลักกับผู้เล่น
        Rigidbody2D rb2d = GetComponent<Rigidbody2D>();
        if (rb2d == null) rb2d = gameObject.AddComponent<Rigidbody2D>();
        rb2d.isKinematic = true;
        rb2d.gravityScale = 0f;
        rb2d.velocity = Vector2.zero;
        rb2d.constraints = RigidbodyConstraints2D.FreezeRotation;
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.isTrigger = true;

        currentHealth = maxHealth;
        cell = playerScript.WorldToGridCell(transform.position);
        transform.position = playerScript.GridCellToWorld(cell);

        // Prefab ตั้งลำดับวาดไว้ 5 แต่ Tilemap พื้น/หินของด่านอยู่ที่ 9-10 มอนสเตอร์จะถูกพื้นทับจนมองไม่เห็น
        var sortingGroup = GetComponentInChildren<UnityEngine.Rendering.SortingGroup>();
        if (sortingGroup == null) sortingGroup = gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
        sortingGroup.sortingOrder = Mathf.Max(sortingGroup.sortingOrder, 20);

        renderers = GetComponentsInChildren<SpriteRenderer>();
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;

        BuildHpBar();
        stunTimer = spawnStunDuration;
        nextAttackTime = Time.time + spawnStunDuration;
        Active.Add(this);
    }

    private void OnDestroy()
    {
        Active.Remove(this);
        if (hpRow != null) hpRow.Remove();
    }

    private void Update()
    {
        if (isDead || playerScript == null) return;
        UpdateFlash();
        if (playerScript.IsDead()) { SetMoveAnim(false); return; }

        if (stunTimer > 0f)
        {
            // เด้งตัวขึ้นจากหิน + โยกไปมาตอนมึนงง
            stunTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(stunTimer / spawnStunDuration);
            float pop = t < 0.25f ? Mathf.Lerp(0.4f, 1.1f, t / 0.25f) : Mathf.Lerp(1.1f, 1f, (t - 0.25f) / 0.75f);
            transform.localScale = Vector3.one * pop;
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 18f) * 8f * (1f - t));
            if (stunTimer <= 0f) { transform.localScale = Vector3.one; visualRoot.localRotation = Quaternion.identity; }
            return;
        }

        if (isMoving || isAttacking) return;

        Vector3Int playerCell = playerScript.CurrentCell;
        FaceTowards(playerScript.transform.position);

        if (IsAdjacent(playerCell))
        {
            SetMoveAnim(false);
            if (Time.time >= nextAttackTime) StartCoroutine(AttackRoutine());
            return;
        }

        if (Time.time >= nextStepTime)
        {
            nextStepTime = Time.time + stepInterval;
            Vector3Int next;
            if (TryPickStep(playerCell, out next)) StartCoroutine(StepRoutine(next));
            else SetMoveAnim(false);
        }
    }

    private bool IsAdjacent(Vector3Int target)
    {
        return Mathf.Abs(target.x - cell.x) <= 1 && Mathf.Abs(target.y - cell.y) <= 1 && target != cell;
    }

    private bool TryPickStep(Vector3Int target, out Vector3Int next)
    {
        int dx = target.x - cell.x, dy = target.y - cell.y;
        Vector3Int horizontal = new Vector3Int(cell.x + (int)Mathf.Sign(dx), cell.y, cell.z);
        Vector3Int vertical = new Vector3Int(cell.x, cell.y + (int)Mathf.Sign(dy), cell.z);

        // เดินแกนที่ห่างกว่าก่อน ถ้าติดหินค่อยลองอีกแกน
        bool preferHorizontal = Mathf.Abs(dx) >= Mathf.Abs(dy);
        Vector3Int first = preferHorizontal ? horizontal : vertical;
        Vector3Int second = preferHorizontal ? vertical : horizontal;
        bool firstValid = preferHorizontal ? dx != 0 : dy != 0;
        bool secondValid = preferHorizontal ? dy != 0 : dx != 0;

        if (firstValid && CanEnter(first, target)) { next = first; return true; }
        if (secondValid && CanEnter(second, target)) { next = second; return true; }
        next = cell;
        return false;
    }

    private bool CanEnter(Vector3Int c, Vector3Int playerCell)
    {
        return c != playerCell && playerScript.IsCellOpenForMonster(c) && !IsCellOccupied(c, this);
    }

    private IEnumerator StepRoutine(Vector3Int next)
    {
        isMoving = true;
        cell = next; // จองช่องทันที กันมอนสเตอร์ตัวอื่น/ผู้เล่นเดินเข้ามาซ้อน
        SetMoveAnim(true);

        Vector3 start = transform.position, end = playerScript.GridCellToWorld(next);
        float t = 0f;
        while (t < stepDuration && !isDead)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(t / stepDuration));
            yield return null;
        }
        if (!isDead) transform.position = end;
        isMoving = false;
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;
        if (monsterAnim != null) monsterAnim.SetTrigger("2_Attack");

        // ง้าง: ตัวแดงขึ้นเรื่อยๆ ให้ผู้เล่นเห็นว่ากำลังจะตี
        float t = 0f;
        while (t < attackWindup && !isDead)
        {
            t += Time.deltaTime;
            Tint(Color.Lerp(Color.white, new Color(1f, 0.45f, 0.35f), t / attackWindup));
            yield return null;
        }
        Tint(Color.white);

        if (!isDead && playerScript != null && !playerScript.IsDead() && IsAdjacent(playerScript.CurrentCell))
        {
            playerScript.TakeDamage(monsterDamage);
        }
        isAttacking = false;
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        flashTimer = 0.15f;
        UpdateHpBar();

        if (currentHealth <= 0f) MonsterDeath();
    }

    private void UpdateFlash()
    {
        if (flashTimer <= 0f) return;
        flashTimer -= Time.deltaTime;
        // กระพริบขาว + สั่นเล็กน้อยตอนโดนตี
        Tint(flashTimer > 0f ? new Color(1f, 1f, 1f, 0.35f) : Color.white);
        visualRoot.localPosition = flashTimer > 0f ? new Vector3(Mathf.Sin(Time.time * 90f) * 0.06f, 0f, 0f) : Vector3.zero;
    }

    private void Tint(Color multiplier)
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].color = baseColors[i] * multiplier;
        }
    }

    private void FaceTowards(Vector3 target)
    {
        if (visualRoot == null || visualRoot == transform) return;
        // ตัวละคร SPUM หันซ้ายเป็นค่าเริ่มต้น
        float sx = target.x < transform.position.x ? 1f : -1f;
        if (Mathf.Abs(target.x - transform.position.x) < 0.05f) return;
        Vector3 s = visualRoot.localScale;
        visualRoot.localScale = new Vector3(Mathf.Abs(s.x) * sx, s.y, s.z);
    }

    private void SetMoveAnim(bool moving)
    {
        if (monsterAnim != null) monsterAnim.SetBool("1_Move", moving);
    }

    private void BuildHpBar()
    {
        hpRow = MonsterHealthHud.Register(currentHealth, maxHealth);
    }

    private void UpdateHpBar()
    {
        if (hpRow != null) hpRow.Set(currentHealth, maxHealth);
    }

    private void DisableSPUM(GameObject rootObj)
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

    private void MonsterDeath()
    {
        isDead = true;
        Active.Remove(this);
        StopAllCoroutines();
        Tint(Color.white);
        visualRoot.localPosition = Vector3.zero;
        if (hpRow != null) { hpRow.Set(0f, maxHealth); hpRow.Remove(); }

        foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = false;

        if (monsterAnim != null)
        {
            DisableSPUM(gameObject);
            monsterAnim.SetBool("1_Move", false);
            monsterAnim.SetBool("isDeath", true);
            monsterAnim.SetTrigger("4_Death");
            Invoke(nameof(LockMonsterAnimator), 1.0f);
        }

        if (playerScript != null) playerScript.OnMonsterKilled(this);
        Destroy(gameObject, 2.5f);
    }

    private void LockMonsterAnimator()
    {
        if (monsterAnim != null)
        {
            monsterAnim.speed = 0f;
            monsterAnim.enabled = false;
        }
    }
}
