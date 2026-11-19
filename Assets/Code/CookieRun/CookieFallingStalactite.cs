using UnityEngine;

namespace CookieRun
{
    /// <summary>
    /// หินย้อยที่ร่วงลงมาเมื่อผู้เล่นเข้าใกล้
    /// - สั่นเตือนก่อนร่วง แล้วร่วงเร็วปักลงพื้น กลายเป็นสิ่งกีดขวางที่ต้องกระโดดข้าม
    /// - ถ้าผู้เล่นอยู่ใต้ตอนกำลังร่วง (หรือกระโดดเข้าไปชน) จะโดนทันที
    /// - เริ่มร่วงตาม "เวลา" ก่อนผู้เล่นมาถึง ยิ่งวิ่งเร็วก็ยิ่งเห็นการร่วงไกลขึ้น แต่มีเวลาตัดสินใจเท่าเดิม
    /// </summary>
    [RequireComponent(typeof(CookieObstacle))]
    public class CookieFallingStalactite : MonoBehaviour, ICookiePoolable
    {
        private enum State { Hanging, Shaking, Falling, Landed }

        [Header("Trigger")]
        [Tooltip("เริ่มร่วงก่อนผู้เล่นมาถึงกี่วินาที")]
        [SerializeField] private float triggerLeadTime = 1.3f;
        [Tooltip("ระยะไกลสุดที่ยอมให้เริ่มร่วง (ไม่ให้ร่วงนอกจอ)")]
        [SerializeField] private float maxTriggerDistance = 11f;

        [Header("Fall")]
        [SerializeField] private float shakeDuration = 0.3f;
        [SerializeField] private float shakeAmount = 0.05f;
        [SerializeField] private float fallGravity = 45f;
        [Tooltip("ความลึกที่ปลายแหลมปักลงพื้น (ยูนิต)")]
        [SerializeField] private float embedDepth = 0.3f;
        [Tooltip("ระดับผิวพื้นในพิกัดของ Chunk")]
        [SerializeField] private float groundLocalY = 0f;

        [Header("Effects")]
        [SerializeField] private Color debrisColor = new Color(0.7f, 0.72f, 0.8f, 0.9f);

        private State state;
        private Vector3 initialLocalPosition;
        private float landedLocalY;
        private float timer;
        private float fallVelocity;
        private ParticleSystem debris;

        private void Awake()
        {
            initialLocalPosition = transform.localPosition;

            // คำนวณตำแหน่งตอนปักพื้น: ปลายล่างของรูปจมลงพื้นตาม embedDepth
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            float bottomOffset = sr != null && sr.sprite != null ? sr.sprite.bounds.min.y * transform.localScale.y : 0f;
            landedLocalY = groundLocalY - embedDepth - bottomOffset;

            BuildDebris(sr);
        }

        public void OnSpawnFromPool()
        {
            state = State.Hanging;
            timer = 0f;
            fallVelocity = 0f;
            transform.localPosition = initialLocalPosition;
        }

        private void Update()
        {
            switch (state)
            {
                case State.Hanging:
                    CheckTrigger();
                    break;

                case State.Shaking:
                    timer += Time.deltaTime;
                    float shake = Mathf.Sin(timer * 70f) * shakeAmount;
                    transform.localPosition = initialLocalPosition + new Vector3(shake, 0f, 0f);
                    if (timer >= shakeDuration)
                    {
                        transform.localPosition = initialLocalPosition;
                        state = State.Falling;
                    }
                    break;

                case State.Falling:
                    fallVelocity += fallGravity * Time.deltaTime;
                    Vector3 pos = transform.localPosition;
                    pos.y -= fallVelocity * Time.deltaTime;
                    if (pos.y <= landedLocalY)
                    {
                        pos.y = landedLocalY;
                        state = State.Landed;
                        EmitDebris(14, 4f);
                        CookieAudio.Play(CookieSfx.StalactiteCrash);
                    }
                    transform.localPosition = pos;
                    break;
            }
        }

        private void CheckTrigger()
        {
            CookiePlayerController player = CookiePlayerController.Instance;
            if (player == null) return;

            float dist = transform.position.x - player.transform.position.x;
            float triggerDistance = Mathf.Min(maxTriggerDistance, player.CurrentSpeed * triggerLeadTime);
            if (dist > 0f && dist <= triggerDistance)
            {
                state = State.Shaking;
                timer = 0f;
                EmitDebris(4, 1f); // เศษหินร่วงเตือน
                CookieAudio.Play(CookieSfx.StalactiteShake, 0.6f);
            }
        }

        private void BuildDebris(SpriteRenderer sr)
        {
            GameObject obj = new GameObject("StalactiteDebris");
            obj.transform.SetParent(transform, false);

            debris = obj.AddComponent<ParticleSystem>();
            debris.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = debris.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startColor = debrisColor;
            main.gravityModifier = 2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = debris.emission;
            emission.rateOverTime = 0f;

            var shape = debris.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.2f;

            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = CookieFxTextures.EffectMaterial;
            renderer.sortingOrder = sr != null ? sr.sortingOrder + 1 : 5;
        }

        private void EmitDebris(int count, float speed)
        {
            if (debris == null) return;

            // ตอนแขวนอยู่ เศษร่วงจากโคน / ตอนปักพื้น เศษกระเด็นจากจุดที่กระแทก
            Bounds b = GetComponent<SpriteRenderer>().bounds;
            float groundWorldY = transform.parent != null ? transform.parent.TransformPoint(0f, groundLocalY, 0f).y : groundLocalY;
            float y = state == State.Landed ? groundWorldY : b.max.y;
            debris.transform.position = new Vector3(b.center.x, y, 0f);

            var main = debris.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            debris.Emit(count);
        }
    }
}
