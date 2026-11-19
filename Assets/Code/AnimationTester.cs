using UnityEngine;

public class AnimationTester : MonoBehaviour
{
    private Animator anim;
    private Rigidbody2D rb;
    public float moveSpeed = 3f;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        
        // ถ้ายังไม่มี Rigidbody2D ให้เพิ่มให้อัตโนมัติ
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; // เกม 2D มุมมองด้านบนหรือแบบ RPG มักจะไม่ใช้แรงโน้มถ่วง
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    void Update()
    {
        if (anim == null) return;

        // ทดสอบท่าโจมตีด้วยปุ่ม Spacebar
        if (Input.GetKeyDown(KeyCode.Space))
        {
            anim.SetTrigger("2_Attack");
            Debug.Log("ทดสอบอนิเมชัน: Attack");
        }

        // ทดสอบท่าเดินด้วยปุ่มลูกศร หรือปุ่ม W, A, S, D
        float moveX = 0f;
        float moveY = 0f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveY = 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveY = -1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveX = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveX = 1f;

        Vector2 moveDir = new Vector2(moveX, moveY).normalized;
        
        if (rb != null)
        {
            rb.velocity = moveDir * moveSpeed;
        }

        // สั่งเปิด/ปิดอนิเมชันเดิน
        if (moveDir != Vector2.zero)
        {
            anim.SetBool("1_Move", true);
            
            // กลับด้าน Sprite ตามทิศทางซ้าย-ขวา
            Transform spriteChild = transform.childCount > 0 ? transform.GetChild(0) : null;
            if (spriteChild != null && moveX != 0)
            {
                float scaleX = moveX > 0 ? -1f : 1f; // ปรับตามสเกลเดิมของ SPUM
                spriteChild.localScale = new Vector3(scaleX, 1f, 1f);
            }
        }
        else
        {
            anim.SetBool("1_Move", false);
        }

        // ทดสอบท่าตายด้วยปุ่ม K (เปลี่ยนจากปุ่ม D เพื่อไม่ให้ชนกับการเดินขวา)
        if (Input.GetKeyDown(KeyCode.K))
        {
            anim.SetBool("isDeath", true);
            anim.SetTrigger("4_Death");
            if (rb != null) rb.velocity = Vector2.zero;
            Debug.Log("ทดสอบอนิเมชัน: ตาย");
        }
    }
}