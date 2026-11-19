using UnityEngine;

public class CaveDefenseController : MonoBehaviour
{
    private Animator anim;
    private Rigidbody2D rb;
    
    [Header("Movement Settings")]
    public float moveSpeed = 5f; // ความเร็วในการเดิน

    [Header("Magic Attack Settings")]
    public GameObject magicEffectPrefab; // ลากเอฟเฟกต์เวทมนตร์ (เช่น สายฟ้า) มาใส่ที่นี่
    public float attackInterval = 1.5f;   // ระยะเวลาเป็นวินาทีในการยิงเวทอัตโนมัติ
    private float attackTimer = 0f;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    void Update()
    {
        // 1. ระบบควบคุมการเดินอิสระ ตามปุ่มที่ตั้งไว้ในหน้า Option (แผ่นเหยียบ / คีย์บอร์ด / ลูกศร)
        float moveX = 0f;
        float moveY = 0f;

        if (GameInputBindings.GetHeld(GameAction.MoveUp)) moveY = 1f;
        if (GameInputBindings.GetHeld(GameAction.MoveDown)) moveY = -1f;
        if (GameInputBindings.GetHeld(GameAction.MoveLeft)) moveX = -1f;
        if (GameInputBindings.GetHeld(GameAction.MoveRight)) moveX = 1f;

        Vector2 moveDir = new Vector2(moveX, moveY).normalized;
        rb.velocity = moveDir * moveSpeed;

        // 2. จัดการอนิเมชันเดินและการหันหน้า (เนื่องจาก SPUM มีแค่ซ้าย/ขวา)
        if (moveDir != Vector2.zero)
        {
            anim.SetBool("1_Move", true);

            // ถ้ากดเดินไปทางซ้าย ให้หันซ้าย ถ้ากดขวาให้หันขวา
            Transform spriteChild = transform.childCount > 0 ? transform.GetChild(0) : null;
            if (spriteChild != null && moveX != 0)
            {
                float scaleX = moveX > 0 ? -1f : 1f; // ปรับกลับด้านตามสเกล SPUM
                spriteChild.localScale = new Vector3(scaleX, 1f, 1f);
            }
        }
        else
        {
            anim.SetBool("1_Move", false);
        }

        // 3. ระบบยิงเวทมนตร์โจมตีอัตโนมัติตามเวลา (Auto-Attack)
        attackTimer += Time.deltaTime;
        if (attackTimer >= attackInterval)
        {
            AutoCastMagic();
            attackTimer = 0f;
        }
    }

    void AutoCastMagic()
    {
        // เล่นอนิเมชันโจมตีอัตโนมัติ
        if (anim != null)
        {
            anim.SetTrigger("2_Attack");
        }

        // เสกเวทมนตร์พุ่งออกไปด้านหน้า (ฝั่งขวาหรือตามทิศทางที่ตัวละครหัน)
        if (magicEffectPrefab != null)
        {
            Vector3 spawnPos = transform.position + new Vector3(1.0f, 0f, 0f);
            GameObject magic = Instantiate(magicEffectPrefab, spawnPos, Quaternion.identity);

            Rigidbody2D rbMagic = magic.GetComponent<Rigidbody2D>();
            if (rbMagic != null)
            {
                rbMagic.velocity = new Vector2(12f, 0f); // พุ่งไปทางขวา
            }
        }
    }
}