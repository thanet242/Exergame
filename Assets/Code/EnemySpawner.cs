using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject[] enemyPrefabs;    // อาเรย์เก็บ Prefab มอนสเตอร์หลายประเภท
    public int spawnCountPerBatch = 5;   // จำนวนมอนสเตอร์ที่ต้องการให้ออกมาต่อครั้ง
    public float spawnInterval = 3f;     // ทุกๆ กี่วินาทีที่จะปล่อยมอนสเตอร์ชุดใหม่
    private float spawnTimer = 0f;

    [Header("Spawn Settings")]
    public Transform caveSpawnPoint;     // ตำแหน่งปากถ้ำฝั่งขวา

    void Update()
    {
        spawnTimer += Time.deltaTime;

        // เมื่อถึงเวลาที่กำหนด จะปล่อยมอนสเตอร์ออกมาตามจำนวนที่ตั้งไว้
        if (spawnTimer >= spawnInterval)
        {
            SpawnBatchOfEnemies();
            spawnTimer = 0f;
        }
    }

    void SpawnBatchOfEnemies()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0 || caveSpawnPoint == null) return;

        // วนลูปสร้างมอนสเตอร์ตามจำนวนที่กำหนดใน spawnCountPerBatch
        for (int i = 0; i < spawnCountPerBatch; i++)
        {
            // สุ่มเลือกประเภทมอนสเตอร์จากรายการ
            int randomIndex = Random.Range(0, enemyPrefabs.Length);
            GameObject selectedEnemyPrefab = enemyPrefabs[randomIndex];

            if (selectedEnemyPrefab == null) continue;

            // เพิ่มการสุ่มระยะห่างเล็กน้อย (Random Offset) เพื่อไม่ให้มอนสเตอร์เกิดทับซ้อนกันเป๊ะๆ จนเบียดกันแน่นเกินไป
            Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0f);
            Vector3 spawnPosition = caveSpawnPoint.position + randomOffset;

            // สร้างมอนสเตอร์
            GameObject newEnemy = Instantiate(selectedEnemyPrefab, spawnPosition, Quaternion.identity);

            // ตั้งค่า Rigidbody เพื่อให้สคริปต์ AI (EnemyAI) ควบคุมการเดินต่อได้
            Rigidbody2D rb = newEnemy.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = newEnemy.AddComponent<Rigidbody2D>();
            }
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }
}