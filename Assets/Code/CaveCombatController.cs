using UnityEngine;

public class CaveCombatController : MonoBehaviour
{
    private Animator anim;
    private Rigidbody2D rb;
    
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Combat & Target Detection")]
    public GameObject magicEffectPrefab; 
    public float attackRange = 10f;      
    public float attackInterval = 1.2f;  
    private float attackTimer = 0f;
    
    private Transform currentTarget;     

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        float moveX = 0f;
        float moveY = 0f;

        // เดินตามปุ่มที่ตั้งไว้ในหน้า Option (แผ่นเหยียบ / คีย์บอร์ด / ลูกศร)
        if (GameInputBindings.GetHeld(GameAction.MoveUp)) moveY = 1f;
        if (GameInputBindings.GetHeld(GameAction.MoveDown)) moveY = -1f;
        if (GameInputBindings.GetHeld(GameAction.MoveLeft)) moveX = -1f;
        if (GameInputBindings.GetHeld(GameAction.MoveRight)) moveX = 1f;

        Vector2 moveDir = new Vector2(moveX, moveY).normalized;
        rb.velocity = moveDir * moveSpeed;

        if (moveDir != Vector2.zero)
        {
            anim.SetBool("1_Move", true);

            Transform spriteChild = transform.childCount > 0 ? transform.GetChild(0) : null;
            if (spriteChild != null && moveX != 0)
            {
                float scaleX = moveX > 0 ? -1f : 1f; 
                spriteChild.localScale = new Vector3(scaleX, 1f, 1f);
            }
        }
        else
        {
            anim.SetBool("1_Move", false);
        }

        FindNearestEnemy();

        attackTimer += Time.deltaTime;
        if (currentTarget != null && attackTimer >= attackInterval)
        {
            CastTargetedMagic();
            attackTimer = 0f;
        }
    }

    void FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (enemies == null || enemies.Length == 0)
        {
            currentTarget = null;
            return;
        }

        float shortestDistance = Mathf.Infinity;
        Transform nearestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;

            float distanceToEnemy = Vector2.Distance(transform.position, enemy.transform.position);
            if (distanceToEnemy < shortestDistance && distanceToEnemy <= attackRange)
            {
                shortestDistance = distanceToEnemy;
                nearestEnemy = enemy.transform;
            }
        }

        currentTarget = nearestEnemy;
    }

    void CastTargetedMagic()
    {
        if (currentTarget == null || magicEffectPrefab == null) return;

        if (anim != null)
        {
            anim.SetTrigger("2_Attack");
        }

        Vector3 spawnPos = transform.position;
        GameObject magic = Instantiate(magicEffectPrefab, spawnPos, Quaternion.identity);
        
        // ค้นหา SpriteRenderer จากลูกๆ ทั้งหมดข้างใน Prefab (แก้ปัญหาหาไม่เจอ)
        SpriteRenderer sr = magic.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 100; 
        }

        // คำนวณทิศทางพุ่งไปหามอนสเตอร์
        Vector2 direction = ((Vector2)currentTarget.position - (Vector2)transform.position).normalized;
        float magicSpeed = 10f; 

        Rigidbody2D rbMagic = magic.GetComponent<Rigidbody2D>();
        if (rbMagic == null)
        {
            rbMagic = magic.AddComponent<Rigidbody2D>();
        }
        
        rbMagic.gravityScale = 0f;
        rbMagic.velocity = direction * magicSpeed;

        // คำนวณองศาการพุ่ง
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // หมุนตัววัตถุหลัก (Thunder) ให้พุ่งไปหาเป้าหมาย โดยที่ข้างในมีลูกที่นอนแนวนอนอยู่แล้ว จะทำให้หัวสายฟ้าพุ่งตรงเป้าพอดี
        magic.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}