using UnityEngine;

namespace CookieRun
{
    /// <summary>
    /// เอฟเฟกต์แสดงสถานะบัฟบนตัวผู้เล่น
    /// - Shield: ฟองโล่เรืองแสงกะพริบรอบตัว + ประกายวนรอบโล่ ตลอดเวลาที่โล่ยังทำงาน และแตกกระจายเมื่อโล่ถูกใช้
    /// - Blast: หางแสงด้านหลัง + เส้นความเร็ว + แสงเรืองรอบตัว ตลอดเวลาที่เร่งความเร็ว
    /// ถ้าไม่ได้ใส่ Prefab จะสร้างเอฟเฟกต์ด้วยโค้ดอัตโนมัติ (ไม่ต้องใช้ Asset เพิ่ม)
    /// </summary>
    [RequireComponent(typeof(CookiePlayerController))]
    public class CookieBuffVisuals : MonoBehaviour
    {
        [Header("Shield (โล่)")]
        [Tooltip("Prefab เอฟเฟกต์โล่แบบวนลูป (ไม่บังคับ ถ้าไม่ใส่จะสร้างฟองโล่อัตโนมัติ)")]
        [SerializeField] private GameObject shieldLoopPrefab;
        [SerializeField] private Color shieldColor = new Color(0.35f, 0.9f, 1f, 0.6f);
        [Tooltip("ขนาดฟองโล่เทียบกับความสูงตัวละคร")]
        [SerializeField] private float shieldSizeMultiplier = 1.35f;
        [SerializeField] private float shieldPulseSpeed = 4f;

        [Header("Blast (เร่งความเร็ว)")]
        [Tooltip("Prefab เอฟเฟกต์เร่งความเร็วแบบวนลูป (ไม่บังคับ ถ้าไม่ใส่จะสร้างหางแสงและเส้นความเร็วอัตโนมัติ)")]
        [SerializeField] private GameObject blastLoopPrefab;
        [SerializeField] private Color blastColor = new Color(1f, 0.55f, 0.1f, 0.9f);
        [Tooltip("ความยาวหางแสง (วินาที)")]
        [SerializeField] private float trailTime = 0.3f;

        [Header("Sorting (ภายใน SortingGroup ของผู้เล่น)")]
        [SerializeField] private int frontSortingOrder = 100;
        [SerializeField] private int backSortingOrder = -100;

        private CookiePlayerController player;
        private BoxCollider2D body;
        private Material effectMaterial;
        private float bodyHeight = 1.6f;

        // Shield
        private GameObject shieldRoot;
        private SpriteRenderer shieldBubble;
        private ParticleSystem shieldSparkles;
        private Vector3 shieldBaseScale;
        private bool shieldActive;

        // Blast
        private GameObject blastRoot;
        private TrailRenderer blastTrail;
        private ParticleSystem speedLines;
        private SpriteRenderer blastGlow;
        private bool blastActive;

        // One-shot bursts (เก็บโล่ / โล่แตก / เริ่มเร่งความเร็ว)
        private ParticleSystem burstParticles;

        private static Texture2D bubbleTexture;
        private static Sprite bubbleSprite;

        private void Awake()
        {
            player = GetComponent<CookiePlayerController>();
            body = GetComponent<BoxCollider2D>();
            if (body != null) bodyHeight = body.size.y;

            effectMaterial = CookieFxTextures.EffectMaterial;

            BuildShieldVisual();
            BuildBlastVisual();
            BuildBurstParticles();
        }

        private void LateUpdate()
        {
            if (player == null) return;

            bool hasShield = player.HasShield;
            if (hasShield != shieldActive)
            {
                shieldActive = hasShield;
                shieldRoot.SetActive(hasShield);
                if (hasShield)
                {
                    if (shieldSparkles != null) shieldSparkles.Play();
                    EmitBurst(shieldColor, 18, 2.5f, 0.6f);
                }
                else
                {
                    // โล่แตก
                    EmitBurst(shieldColor, 32, 6f, 0.45f);
                }
            }

            bool isBlast = player.IsBlast;
            if (isBlast != blastActive)
            {
                blastActive = isBlast;
                SetBlastVisible(isBlast);
                if (isBlast) EmitBurst(blastColor, 24, 5f, 0.4f);
            }

            if (shieldActive) AnimateShield();
            if (blastActive) AnimateBlast();
        }

        #region Shield
        private void BuildShieldVisual()
        {
            if (shieldLoopPrefab != null)
            {
                shieldRoot = Instantiate(shieldLoopPrefab, transform);
                shieldRoot.transform.localPosition = BodyCenter();
                shieldRoot.SetActive(false);
                return;
            }

            shieldRoot = new GameObject("Buff_ShieldBubble");
            shieldRoot.transform.SetParent(transform, false);
            shieldRoot.transform.localPosition = BodyCenter();

            // ฟองโล่ (ขอบสว่าง ตรงกลางใส)
            GameObject bubbleObj = new GameObject("Bubble");
            bubbleObj.transform.SetParent(shieldRoot.transform, false);
            shieldBubble = bubbleObj.AddComponent<SpriteRenderer>();
            shieldBubble.sprite = GetBubbleSprite();
            shieldBubble.color = shieldColor;
            shieldBubble.sortingOrder = frontSortingOrder;

            float diameter = bodyHeight * shieldSizeMultiplier;
            shieldBaseScale = new Vector3(diameter, diameter, 1f);
            bubbleObj.transform.localScale = shieldBaseScale;

            // ประกายเล็กๆ วนรอบขอบโล่
            ParticleSystem sparkles = CreateParticleSystem("Sparkles", shieldRoot.transform, frontSortingOrder + 1);
            shieldSparkles = sparkles;
            var main = sparkles.main;
            main.loop = true;
            main.startLifetime = 0.6f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = new Color(0.85f, 1f, 1f, 0.9f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = sparkles.emission;
            emission.rateOverTime = 14f;

            var shape = sparkles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = diameter * 0.45f;
            shape.radiusThickness = 0f;

            var rotation = sparkles.velocityOverLifetime;
            rotation.enabled = true;
            rotation.space = ParticleSystemSimulationSpace.Local;
            rotation.orbitalZ = 2.5f;

            FadeOutOverLifetime(sparkles);

            shieldRoot.SetActive(false);
        }

        private void AnimateShield()
        {
            if (shieldBubble == null) return;

            // ฟองโล่ขยับตาม Collider (ตอนสไลด์จะเลื่อนลงตามตัว)
            shieldRoot.transform.localPosition = BodyCenter();

            float t = Time.time * shieldPulseSpeed;
            shieldBubble.transform.localScale = shieldBaseScale * (1f + Mathf.Sin(t) * 0.04f);

            Color c = shieldColor;
            c.a = shieldColor.a * (0.75f + 0.25f * Mathf.Sin(t * 1.3f));

            // เวลาโล่ใกล้หมด: กะพริบเตือน ยิ่งใกล้หมดยิ่งถี่
            if (player.IsShieldExpiring)
            {
                float period = player.ShieldBlinkPeriod;
                bool on = Mathf.Repeat(Time.time, period) < period * 0.6f;
                c.a = on ? shieldColor.a : shieldColor.a * 0.12f;
                if (shieldSparkles != null)
                {
                    var emission = shieldSparkles.emission;
                    emission.enabled = on;
                }
            }
            else if (shieldSparkles != null && !shieldSparkles.emission.enabled)
            {
                var emission = shieldSparkles.emission;
                emission.enabled = true;
            }
            shieldBubble.color = c;
        }
        #endregion

        #region Blast
        private void BuildBlastVisual()
        {
            if (blastLoopPrefab != null)
            {
                blastRoot = Instantiate(blastLoopPrefab, transform);
                blastRoot.transform.localPosition = BodyCenter();
                blastRoot.SetActive(false);
                return;
            }

            blastRoot = new GameObject("Buff_Blast");
            blastRoot.transform.SetParent(transform, false);
            blastRoot.transform.localPosition = BodyCenter();

            // หางแสงด้านหลังตัวละคร
            blastTrail = blastRoot.AddComponent<TrailRenderer>();
            blastTrail.sharedMaterial = effectMaterial;
            blastTrail.time = trailTime;
            blastTrail.minVertexDistance = 0.1f;
            blastTrail.widthMultiplier = bodyHeight * 0.7f;
            blastTrail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            blastTrail.sortingOrder = backSortingOrder;
            blastTrail.emitting = false;

            Gradient trailGradient = new Gradient();
            trailGradient.SetKeys(
                new[] { new GradientColorKey(Color.Lerp(blastColor, Color.white, 0.5f), 0f), new GradientColorKey(blastColor, 1f) },
                new[] { new GradientAlphaKey(blastColor.a * 0.8f, 0f), new GradientAlphaKey(0f, 1f) });
            blastTrail.colorGradient = trailGradient;

            // แสงเรืองรอบตัว
            GameObject glowObj = new GameObject("Glow");
            glowObj.transform.SetParent(blastRoot.transform, false);
            glowObj.transform.localScale = new Vector3(bodyHeight * 1.6f, bodyHeight * 1.3f, 1f);
            blastGlow = glowObj.AddComponent<SpriteRenderer>();
            blastGlow.sprite = CookieFxTextures.SoftCircleSprite;
            blastGlow.color = new Color(blastColor.r, blastColor.g, blastColor.b, 0.35f);
            blastGlow.sortingOrder = backSortingOrder + 1;

            // เส้นความเร็ว พุ่งไปด้านหลัง
            speedLines = CreateParticleSystem("SpeedLines", blastRoot.transform, frontSortingOrder - 1);
            var main = speedLines.main;
            main.loop = true;
            main.startLifetime = 0.25f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 16f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.09f);
            main.startColor = new Color(1f, 0.9f, 0.6f, 0.9f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = speedLines.emission;
            emission.rateOverTime = 40f;

            var shape = speedLines.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.1f, bodyHeight * 1.2f, 0f);
            shape.rotation = new Vector3(0f, -90f, 0f); // ยิงไปทาง -X (ด้านหลังตัวละคร)

            var renderer = speedLines.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.06f;
            renderer.lengthScale = 2f;

            FadeOutOverLifetime(speedLines);

            blastRoot.SetActive(false);
        }

        private void SetBlastVisible(bool visible)
        {
            if (blastTrail == null)
            {
                // ใช้ Prefab ที่ผู้ใช้ใส่มา
                blastRoot.SetActive(visible);
                return;
            }

            if (visible)
            {
                blastRoot.SetActive(true);
                blastTrail.Clear();
                blastTrail.emitting = true;
                speedLines.Play();
                blastGlow.enabled = true;
            }
            else
            {
                // ปล่อยให้หางแสงค่อยๆ หายไปเอง ไม่ตัดทิ้งทันที
                blastTrail.emitting = false;
                speedLines.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                blastGlow.enabled = false;
            }
        }

        private void AnimateBlast()
        {
            if (blastGlow == null) return;

            blastRoot.transform.localPosition = BodyCenter();

            Color c = blastGlow.color;
            c.a = 0.25f + 0.15f * Mathf.Sin(Time.time * 18f);
            blastGlow.color = c;
        }
        #endregion

        #region Bursts
        private void BuildBurstParticles()
        {
            burstParticles = CreateParticleSystem("Buff_Burst", transform, frontSortingOrder + 2);
            burstParticles.transform.localPosition = BodyCenter();

            var main = burstParticles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.22f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            var emission = burstParticles.emission;
            emission.rateOverTime = 0f;

            var shape = burstParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = bodyHeight * 0.5f;

            FadeOutOverLifetime(burstParticles);
        }

        private void EmitBurst(Color color, int count, float speed, float lifetime)
        {
            if (burstParticles == null) return;

            burstParticles.transform.localPosition = BodyCenter();

            var main = burstParticles.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);

            var emitParams = new ParticleSystem.EmitParams
            {
                startColor = new Color(color.r, color.g, color.b, 1f),
                startLifetime = lifetime,
                applyShapeToPosition = true
            };
            burstParticles.Emit(emitParams, count);
        }
        #endregion

        #region Helpers
        private Vector3 BodyCenter()
        {
            return body != null ? (Vector3)body.offset : new Vector3(0f, bodyHeight * 0.5f, 0f);
        }

        private ParticleSystem CreateParticleSystem(string name, Transform parent, int sortingOrder)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            // AddComponent จะเริ่มเล่นทันที ต้องหยุดก่อนตั้งค่า
            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;

            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = effectMaterial;
            renderer.sortingOrder = sortingOrder;

            return ps;
        }

        private static void FadeOutOverLifetime(ParticleSystem ps)
        {
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = gradient;
        }

        private static Sprite GetBubbleSprite()
        {
            if (bubbleSprite == null)
            {
                // ตรงกลางโปร่งใสเกือบหมด ขอบสว่าง และขอบนอกจางแบบนุ่ม
                bubbleTexture = CookieFxTextures.GenerateCircleTexture(128, r =>
                {
                    if (r > 1f) return 0f;
                    float rim = Mathf.Pow(r, 6f);
                    float edgeSoftness = Mathf.Clamp01((1f - r) / 0.06f);
                    return Mathf.Lerp(0.12f, 1f, rim) * edgeSoftness;
                });
                bubbleSprite = Sprite.Create(bubbleTexture, new Rect(0, 0, bubbleTexture.width, bubbleTexture.height), new Vector2(0.5f, 0.5f), bubbleTexture.width);
            }
            return bubbleSprite;
        }
        #endregion
    }
}
