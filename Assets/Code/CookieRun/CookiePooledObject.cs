using UnityEngine;

namespace CookieRun
{
    /// <summary>
    /// ตัวกำกับวัตถุที่มาจาก CookiePool (ถูกเพิ่มให้อัตโนมัติตอนสร้าง ไม่ต้องใส่เอง)
    /// </summary>
    [DisallowMultipleComponent]
    public class CookiePooledObject : MonoBehaviour
    {
        internal GameObject Prefab { get; private set; }
        internal bool InPool { get; set; }
        internal float FallbackLifetime { get; set; }

        private CookiePool owner;
        private ICookiePoolable[] poolables;

        internal void Init(CookiePool pool, GameObject prefab)
        {
            owner = pool;
            Prefab = prefab;
            poolables = GetComponentsInChildren<ICookiePoolable>(true);
        }

        internal void ResetForReuse()
        {
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnSpawnFromPool();
            }
        }

        private void OnDisable()
        {
            // owner เป็น null เมื่อ Pool ถูกทำลายไปก่อนแล้ว (เช่น ตอนเปลี่ยน Scene)
            if (owner != null)
            {
                owner.Return(this);
            }
        }
    }
}
