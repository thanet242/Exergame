using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CookieRun
{
    /// <summary>
    /// คอมโพเนนต์ที่ต้องการรีเซ็ตสถานะเมื่อ Chunk / วัตถุถูกนำกลับมาใช้ใหม่จาก Pool
    /// (ถูกเรียกขณะวัตถุยังปิดอยู่ ก่อนจัดตำแหน่งและเปิดใช้งาน)
    /// </summary>
    public interface ICookiePoolable
    {
        void OnSpawnFromPool();
    }

    /// <summary>
    /// ระบบ Object Pooling สำหรับ Chunk, เหรียญ/ไอเทม และเอฟเฟกต์ ของเกม Cookie Run
    /// - Spawn: ดึงวัตถุจาก Pool (ถ้าไม่มีจึง Instantiate ใหม่)
    /// - Release: คืนวัตถุเข้า Pool แทน Destroy (ถ้าเป็นลูกของ Chunk ที่อยู่ใน Pool จะแค่ปิดไว้ แล้วเปิดใหม่ตอน Chunk ถูกใช้ซ้ำ)
    /// - วัตถุที่ถูกปิด (SetActive(false)) จะกลับเข้า Pool อัตโนมัติ รองรับเอฟเฟกต์ที่ปิดตัวเองเมื่อเล่นจบ
    /// Pool ผูกกับ Scene ปัจจุบัน เมื่อโหลด Scene ใหม่จะถูกทำลายพร้อมวัตถุทั้งหมด
    /// </summary>
    public class CookiePool : MonoBehaviour
    {
        [Tooltip("อายุเอฟเฟกต์สำรอง (วินาที) กรณีเอฟเฟกต์ไม่มี ParticleSystem หรือ CFXR ที่ปิดตัวเองได้")]
        [SerializeField] private float fallbackEffectLifetime = 2f;

        private static CookiePool instance;
        private static bool isQuitting;

        private readonly Dictionary<GameObject, Stack<CookiePooledObject>> pools = new Dictionary<GameObject, Stack<CookiePooledObject>>();

        private static CookiePool Instance
        {
            get
            {
                if (instance == null && !isQuitting)
                {
                    instance = new GameObject("CookiePool").AddComponent<CookiePool>();
                }
                return instance;
            }
        }

        // รีเซ็ตค่า static ทุกครั้งที่กด Play (รองรับการปิด Domain Reload ใน Enter Play Mode Options)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            isQuitting = false;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        /// <summary>
        /// ดึงวัตถุจาก Pool (หรือสร้างใหม่) แล้ววางที่ตำแหน่งที่กำหนด
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            CookiePool pool = Instance;
            if (pool == null) return Instantiate(prefab, position, rotation);

            return pool.SpawnInternal(prefab, position, rotation, false);
        }

        /// <summary>
        /// สร้างเอฟเฟกต์จาก Pool โดยตั้งให้เอฟเฟกต์ปิดตัวเอง (แทนการ Destroy) เมื่อเล่นจบ แล้วกลับเข้า Pool อัตโนมัติ
        /// </summary>
        public static GameObject SpawnEffect(GameObject prefab, Vector3 position)
        {
            if (prefab == null) return null;

            CookiePool pool = Instance;
            if (pool == null) return Instantiate(prefab, position, Quaternion.identity);

            return pool.SpawnInternal(prefab, position, Quaternion.identity, true);
        }

        /// <summary>
        /// คืนวัตถุเข้า Pool แทนการ Destroy
        /// </summary>
        public static void Release(GameObject obj)
        {
            if (obj == null) return;

            if (obj.GetComponent<CookiePooledObject>() != null)
            {
                // การปิดวัตถุจะเรียก OnDisable ของ CookiePooledObject ให้คืนเข้า Pool เอง
                obj.SetActive(false);
            }
            else if (obj.GetComponentInParent<CookiePooledObject>() != null)
            {
                // เป็นส่วนหนึ่งของ Chunk ใน Pool: ปิดไว้ก่อน แล้ว ICookiePoolable จะเปิดคืนตอน Chunk ถูกใช้ซ้ำ
                obj.SetActive(false);
            }
            else
            {
                Destroy(obj);
            }
        }

        private GameObject SpawnInternal(GameObject prefab, Vector3 position, Quaternion rotation, bool isEffect)
        {
            CookiePooledObject pooled = null;

            if (pools.TryGetValue(prefab, out Stack<CookiePooledObject> stack))
            {
                // ข้ามวัตถุที่ถูก Destroy ไปแล้วจากภายนอก
                while (pooled == null && stack.Count > 0)
                {
                    pooled = stack.Pop();
                }
            }

            if (pooled == null)
            {
                GameObject obj = Instantiate(prefab, position, rotation, transform);
                pooled = obj.AddComponent<CookiePooledObject>();
                pooled.Init(this, prefab);
                if (isEffect) ConfigureEffect(pooled);
                return obj;
            }

            pooled.InPool = false;
            pooled.ResetForReuse();
            pooled.transform.SetPositionAndRotation(position, rotation);
            pooled.gameObject.SetActive(true);

            if (pooled.FallbackLifetime > 0f)
            {
                StartCoroutine(ReleaseAfter(pooled, pooled.FallbackLifetime));
            }

            return pooled.gameObject;
        }

        internal void Return(CookiePooledObject pooled)
        {
            if (pooled.InPool) return;
            pooled.InPool = true;

            if (!pools.TryGetValue(pooled.Prefab, out Stack<CookiePooledObject> stack))
            {
                stack = new Stack<CookiePooledObject>();
                pools.Add(pooled.Prefab, stack);
            }
            stack.Push(pooled);
        }

        /// <summary>
        /// เปลี่ยนพฤติกรรมเอฟเฟกต์จาก "ทำลายตัวเอง" เป็น "ปิดตัวเอง" เพื่อให้กลับเข้า Pool ได้
        /// </summary>
        private void ConfigureEffect(CookiePooledObject pooled)
        {
            GameObject obj = pooled.gameObject;
            bool selfDisables = false;

            foreach (var cfxr in obj.GetComponentsInChildren<CartoonFX.CFXR_Effect>(true))
            {
                if (cfxr.clearBehavior != CartoonFX.CFXR_Effect.ClearBehavior.None)
                {
                    cfxr.clearBehavior = CartoonFX.CFXR_Effect.ClearBehavior.Disable;
                    if (cfxr.gameObject == obj) selfDisables = true;
                }
            }

            if (!selfDisables && obj.TryGetComponent(out ParticleSystem rootParticles))
            {
                var main = rootParticles.main;
                if (!main.loop)
                {
                    main.stopAction = ParticleSystemStopAction.Disable;
                    selfDisables = true;
                }
            }

            float lifetime = fallbackEffectLifetime;
            foreach (var autoDestroy in obj.GetComponentsInChildren<AutoDestroy>(true))
            {
                // ปิด AutoDestroy (Destroy ใน Start) แล้วใช้ lifetime ของมันคืนเข้า Pool แทน
                lifetime = autoDestroy.lifetime;
                autoDestroy.enabled = false;
                selfDisables = false;
            }

            if (!selfDisables)
            {
                pooled.FallbackLifetime = lifetime;
                StartCoroutine(ReleaseAfter(pooled, lifetime));
            }
        }

        private IEnumerator ReleaseAfter(CookiePooledObject pooled, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (pooled != null && !pooled.InPool)
            {
                pooled.gameObject.SetActive(false);
            }
        }
    }
}
