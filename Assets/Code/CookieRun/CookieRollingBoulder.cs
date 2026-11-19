using UnityEngine;

namespace CookieRun
{
    /// <summary>
    /// บอลหินกลิ้งเข้าหาผู้เล่น (ต้องกระโดดข้าม สไลด์หลบไม่ได้)
    /// เริ่มกลิ้งเมื่อผู้เล่นเข้าใกล้ ความเร็วเข้าหากันจึงสูงกว่าหินปกติ ต้องกะจังหวะกระโดดเร็วขึ้น
    /// </summary>
    [RequireComponent(typeof(CookieObstacle))]
    public class CookieRollingBoulder : MonoBehaviour, ICookiePoolable
    {
        [Tooltip("ความเร็วกลิ้งเข้าหาผู้เล่น (m/s)")]
        [SerializeField] private float rollSpeed = 3.5f;

        [Tooltip("ระยะห่างจากผู้เล่นที่จะเริ่มกลิ้ง (เมตร)")]
        [SerializeField] private float activationDistance = 16f;

        [Header("Dust")]
        [SerializeField] private Color dustColor = new Color(0.75f, 0.72f, 0.68f, 0.6f);

        private Vector3 initialLocalPosition;
        private float radius = 0.5f;
        private bool isRolling;

        // เงา/ฝุ่นต้องไม่หมุนตามลูกหิน
        private Transform shadow;
        private Vector3 shadowOffset;
        private ParticleSystem dust;

        private void Awake()
        {
            initialLocalPosition = transform.localPosition;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                radius = sr.sprite.bounds.extents.y * transform.localScale.y;
            }

            BuildDust();
        }

        private void Start()
        {
            // CookieRockVisual สร้างเงาไว้ใน Awake แล้ว
            shadow = transform.Find("RockShadow");
            if (shadow != null) shadowOffset = shadow.localPosition;
        }

        public void OnSpawnFromPool()
        {
            isRolling = false;
            transform.localPosition = initialLocalPosition;
            transform.localRotation = Quaternion.identity;
            if (dust != null) dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Update()
        {
            if (!isRolling)
            {
                CookiePlayerController player = CookiePlayerController.Instance;
                if (player == null) return;

                float dist = transform.position.x - player.transform.position.x;
                if (dist > 0f && dist <= activationDistance)
                {
                    isRolling = true;
                    if (dust != null) dust.Play();
                    CookieAudio.Play(CookieSfx.BoulderRoll, 0.7f);
                }
                return;
            }

            float step = rollSpeed * Time.deltaTime;
            transform.position += Vector3.left * step;

            // หมุนตามระยะที่กลิ้ง (กลิ้งไปทางซ้าย = หมุนทวนเข็มนาฬิกา)
            transform.Rotate(0f, 0f, step / radius * Mathf.Rad2Deg);
        }

        private void LateUpdate()
        {
            Vector3 basePos = transform.position;

            if (shadow != null)
            {
                shadow.SetPositionAndRotation(basePos + shadowOffset, Quaternion.identity);
            }

            if (dust != null)
            {
                dust.transform.SetPositionAndRotation(basePos + new Vector3(radius * 0.5f, -radius * 0.9f, 0f), Quaternion.identity);
            }
        }

        private void BuildDust()
        {
            GameObject dustObj = new GameObject("BoulderDust");
            dustObj.transform.SetParent(transform, false);

            dust = dustObj.AddComponent<ParticleSystem>();
            dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = dust.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = 0.45f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
            main.startColor = dustColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = dust.emission;
            emission.rateOverTime = 22f;

            var shape = dust.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.1f;
            shape.arc = 60f;
            shape.rotation = new Vector3(0f, 0f, 20f); // ฝุ่นฟุ้งขึ้นไปด้านหลังลูกหิน (ทางขวา)

            var sizeOverLifetime = dust.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));

            var colorOverLifetime = dust.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = fade;

            var renderer = dustObj.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = CookieFxTextures.EffectMaterial;
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            renderer.sortingOrder = sr != null ? sr.sortingOrder + 1 : 5;
        }
    }
}
