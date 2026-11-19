using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookieRun
{
    public enum ZoneAmbient { Sparkles, Spores, Dust, Embers, Snow }

    /// <summary>ข้อมูลบรรยากาศของแต่ละโซน</summary>
    [System.Serializable]
    public class CookieZone
    {
        public string zoneName = "CRYSTAL CAVERN";
        [Tooltip("ภาพฉากหลังของโซน (ว่าง = ใช้ภาพเดิมต่อ)")]
        public Sprite background;
        [Tooltip("สีย้อมฉากหลัง (คูณกับสีเดิม, ขาว = ไม่ย้อม)")]
        public Color backgroundTint = Color.white;
        [Tooltip("สีย้อมพื้นทางวิ่ง")]
        public Color groundTint = Color.white;
        [Tooltip("สีย้อมหินกีดขวาง")]
        public Color rockTint = Color.white;
        [Tooltip("หมอกบางๆ ทับฉากหลัง (ค่า Alpha = ความหนาของหมอก)")]
        public Color fogColor = new Color(0.1f, 0.6f, 0.7f, 0.06f);
        public ZoneAmbient ambient = ZoneAmbient.Sparkles;
        public Color ambientColor = new Color(0.5f, 1f, 1f, 0.8f);
        [Tooltip("เพลงประจำโซน (ว่าง = ใช้เพลงเดิมต่อ)")]
        public AudioClip music;
    }

    /// <summary>
    /// ฉากเปลี่ยนโซนตามระดับความยาก (ระดับ 1-8 = โซน 1-8)
    /// - ขึ้นระดับใหม่: ประตูถ้ำโผล่ข้างหน้า
    /// - วิ่งผ่านประตู: จอมืดลงชั่วครู่ แล้วฉากหลัง/พื้น/หิน ค่อยๆ เปลี่ยนสี หมอก ละอองบรรยากาศ และเพลงเปลี่ยนตามโซน พร้อมป้ายชื่อโซน
    /// </summary>
    public class CookieZoneManager : MonoBehaviour
    {
        public static CookieZoneManager Instance { get; private set; }

        [Header("Zones (ระดับละ 1 โซน: ระดับ 1 = โซนแรก)")]
        [SerializeField] private CookieZone[] zones =
        {
            new CookieZone { zoneName = "CRYSTAL CAVERN", backgroundTint = Color.white, groundTint = Color.white, rockTint = Color.white,
                fogColor = new Color(0.1f, 0.6f, 0.7f, 0.06f), ambient = ZoneAmbient.Sparkles, ambientColor = new Color(0.55f, 1f, 1f, 0.85f) },
            new CookieZone { zoneName = "MUSHROOM GROTTO", backgroundTint = new Color(0.86f, 0.66f, 1f), groundTint = new Color(0.86f, 0.76f, 0.96f), rockTint = new Color(0.9f, 0.8f, 1f),
                fogColor = new Color(0.6f, 0.2f, 0.8f, 0.12f), ambient = ZoneAmbient.Spores, ambientColor = new Color(1f, 0.55f, 0.95f, 0.8f) },
            new CookieZone { zoneName = "ABANDONED MINE", backgroundTint = new Color(1f, 0.78f, 0.55f), groundTint = new Color(1f, 0.86f, 0.7f), rockTint = new Color(1f, 0.88f, 0.75f),
                fogColor = new Color(0.9f, 0.55f, 0.2f, 0.1f), ambient = ZoneAmbient.Dust, ambientColor = new Color(1f, 0.85f, 0.55f, 0.6f) },
            new CookieZone { zoneName = "LAVA DEPTHS", backgroundTint = new Color(1f, 0.5f, 0.36f), groundTint = new Color(1f, 0.62f, 0.5f), rockTint = new Color(1f, 0.72f, 0.62f),
                fogColor = new Color(1f, 0.3f, 0.05f, 0.16f), ambient = ZoneAmbient.Embers, ambientColor = new Color(1f, 0.55f, 0.15f, 0.95f) },
            new CookieZone { zoneName = "FROZEN ABYSS", backgroundTint = new Color(0.62f, 0.8f, 1f), groundTint = new Color(0.8f, 0.9f, 1f), rockTint = new Color(0.86f, 0.95f, 1f),
                fogColor = new Color(0.7f, 0.85f, 1f, 0.14f), ambient = ZoneAmbient.Snow, ambientColor = new Color(0.95f, 0.98f, 1f, 0.9f) },
        };

        [Header("Gate (ประตูถ้ำคั่นโซน)")]
        [SerializeField] private Sprite gateSprite;
        [SerializeField] private Color gateTint = new Color(0.78f, 0.82f, 0.95f, 1f);
        [Tooltip("ระยะข้างหน้าผู้เล่นที่ประตูโผล่ขึ้นตอนขึ้นระดับ (เมตร)")]
        [SerializeField] private float gateDistance = 24f;

        [Header("Transition")]
        [SerializeField] private float colorBlendDuration = 1.5f;
        [Tooltip("ความมืดของจอตอนวิ่งผ่านประตู (0-1)")]
        [Range(0f, 1f)] [SerializeField] private float tunnelDarkness = 0.6f;
        [SerializeField] private float musicCrossfade = 2f;

        private int currentZone;
        private int pendingZone;
        private Transform gate;
        private bool transitioning;

        private Color bgTint = Color.white, groundTint = Color.white, rockTint = Color.white, fogColor;
        private readonly List<SpriteRenderer> bgRenderers = new List<SpriteRenderer>();
        private readonly List<Color> bgBaseColors = new List<Color>();
        private readonly List<SpriteRenderer> groundRenderers = new List<SpriteRenderer>();
        private readonly List<Color> groundBaseColors = new List<Color>();
        private readonly HashSet<SpriteRenderer> groundSet = new HashSet<SpriteRenderer>();

        private SpriteRenderer fog;
        private ParticleSystem ambientParticles;
        private Image tunnelOverlay;
        private TextMeshProUGUI zoneBanner;

        /// <summary>สีย้อมหินของโซนปัจจุบัน (CookieRockVisual ใช้ตอนสร้าง/นำหินกลับมาใช้)</summary>
        public static Color CurrentRockTint => Instance != null ? Instance.rockTint : Color.white;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (CookieGameManager.Instance != null) CookieGameManager.Instance.OnLevelChanged -= HandleLevelChanged;
        }

        private void Start()
        {
            if (CookieGameManager.Instance != null) CookieGameManager.Instance.OnLevelChanged += HandleLevelChanged;

            BuildAtmosphere();

            GameObject initialGround = GameObject.Find("InitialGround");
            if (initialGround != null) RegisterChunk(initialGround);

            ApplyZoneInstant(0);
        }

        #region Public API
        /// <summary>ให้ Spawner เรียกหลังสร้าง/นำ Chunk กลับมาใช้ เพื่อย้อมสีพื้นตามโซนปัจจุบัน</summary>
        public static void RegisterChunk(GameObject chunk)
        {
            if (Instance == null || chunk == null) return;
            foreach (SpriteRenderer sr in chunk.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!sr.gameObject.name.Contains("Ground")) continue;
                if (Instance.groundSet.Add(sr))
                {
                    Instance.groundRenderers.Add(sr);
                    Instance.groundBaseColors.Add(sr.color);
                }
                int idx = Instance.groundRenderers.IndexOf(sr);
                sr.color = Instance.groundBaseColors[idx] * Instance.groundTint;
            }
        }
        #endregion

        #region Level -> Gate -> Zone
        private void HandleLevelChanged(int level, string announcement)
        {
            int target = Mathf.Clamp(level - 1, 0, zones.Length - 1);
            if (target <= pendingZone) return;
            pendingZone = target;

            // ประตูวางบนพื้นเรียบช่วงถัดไปที่ Spawner สร้างให้ (รอบประตูไม่มีหลุมหรือสิ่งกีดขวาง)
            // ลบประตูเก่าที่วิ่งผ่านไปแล้ว ไม่ให้เปลี่ยนโซนทันทีก่อนถึงประตูใหม่
            if (gate != null) { Destroy(gate.gameObject); gate = null; }
            CookieMapSpawner spawner = CookieMapSpawner.Instance;
            if (spawner == null || !spawner.RequestSafeZone(SpawnGateAt)) SpawnGate();
        }

        private void SpawnGateAt(float x, float groundY)
        {
            if (gate != null) Destroy(gate.gameObject);

            var go = new GameObject("ZoneGate");
            go.transform.position = new Vector3(x, groundY, 0f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = gateSprite;
            sr.color = gateTint * rockTint;
            sr.sortingOrder = 2; // อยู่หลังผู้เล่นและสิ่งกีดขวาง แต่อยู่หน้าพื้น
            gate = go.transform;
        }

        // สำรองกรณีไม่มี Spawner: วางประตูข้างหน้าผู้เล่นตรงจุดที่มีพื้น
        private void SpawnGate()
        {
            CookiePlayerController player = CookiePlayerController.Instance;
            if (player == null) return;

            // หาตำแหน่งที่มีพื้นรองรับ (ไม่ให้ประตูลอยอยู่เหนือหลุม)
            float x = player.transform.position.x + gateDistance;
            float groundY = 0f;
            var filter = new ContactFilter2D { useTriggers = false };
            var hits = new RaycastHit2D[4];
            for (int i = 0; i < 12; i++)
            {
                int count = Physics2D.Raycast(new Vector2(x, 6f), Vector2.down, filter, hits, 12f);
                bool found = false;
                for (int h = 0; h < count; h++)
                {
                    if (hits[h].collider.GetComponentInParent<CookiePlayerController>() != null) continue;
                    groundY = hits[h].point.y; found = true; break;
                }
                if (found) break;
                x += 1.5f;
            }

            SpawnGateAt(x, groundY);
        }

        private void Update()
        {
            CookiePlayerController player = CookiePlayerController.Instance;
            if (gate == null || player == null) return;

            float dx = player.transform.position.x - gate.position.x;
            if (!transitioning && pendingZone != currentZone && dx >= 0f)
            {
                StartCoroutine(TransitionRoutine(pendingZone));
            }

            // ประตูที่วิ่งผ่านไปไกลแล้ว ลบทิ้ง
            if (dx > 30f)
            {
                Destroy(gate.gameObject);
                gate = null;
            }
        }

        private IEnumerator TransitionRoutine(int target)
        {
            transitioning = true;

            CookieZone zone = zones[target];
            CookieParallaxBackground parallax = FindObjectOfType<CookieParallaxBackground>();
            bool swapBackground = zone.background != null && parallax != null && parallax.GetLayerSprite(0) != zone.background;

            // วิ่งเข้าอุโมงค์: จอมืดลง (เปลี่ยนภาพฉากหลัง = มืดสนิทชั่วครู่ ไม่ให้เห็นภาพสลับ)
            float darkness = swapBackground ? 0.95f : tunnelDarkness;
            yield return FadeOverlay(0f, darkness, 0.25f);

            if (swapBackground)
            {
                parallax.SetLayerSprite(0, zone.background);
                yield return null; // รอให้ภาพใหม่ถูกสร้างก่อนเก็บรายการ SpriteRenderer
                bgRenderers.Clear();
                bgBaseColors.Clear();
                PushColors();
                yield return new WaitForSecondsRealtime(0.15f);
            }

            currentZone = target;
            SetAmbient(zone);
            ShowZoneBanner(target, zone);
            if (zone.music != null && CookieAudio.Instance != null) CookieAudio.Instance.CrossfadeMusic(zone.music, musicCrossfade);

            // ออกจากอุโมงค์: จอสว่างขึ้นพร้อมสีโซนใหม่ค่อยๆ ซึมเข้ามา
            StartCoroutine(BlendColors(zone, colorBlendDuration));
            yield return FadeOverlay(darkness, 0f, 0.5f);

            transitioning = false;
        }
        #endregion

        #region Colors
        private void ApplyZoneInstant(int index)
        {
            CookieZone zone = zones[Mathf.Clamp(index, 0, zones.Length - 1)];
            currentZone = pendingZone = index;
            bgTint = zone.backgroundTint; groundTint = zone.groundTint; rockTint = zone.rockTint; fogColor = zone.fogColor;
            PushColors();
            SetAmbient(zone);
        }

        private IEnumerator BlendColors(CookieZone zone, float duration)
        {
            Color bg0 = bgTint, gr0 = groundTint, rk0 = rockTint, fg0 = fogColor;
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / Mathf.Max(0.01f, duration));
                float e = t * t * (3f - 2f * t);
                bgTint = Color.Lerp(bg0, zone.backgroundTint, e);
                groundTint = Color.Lerp(gr0, zone.groundTint, e);
                rockTint = Color.Lerp(rk0, zone.rockTint, e);
                fogColor = Color.Lerp(fg0, zone.fogColor, e);
                PushColors();
                yield return null;
            }
        }

        private void PushColors()
        {
            CollectBackground();
            for (int i = 0; i < bgRenderers.Count; i++)
            {
                if (bgRenderers[i] != null) bgRenderers[i].color = bgBaseColors[i] * bgTint;
            }
            for (int i = 0; i < groundRenderers.Count; i++)
            {
                if (groundRenderers[i] != null) groundRenderers[i].color = groundBaseColors[i] * groundTint;
            }
            if (fog != null) fog.color = fogColor;
        }

        // ฉากหลัง Parallax สร้าง SpriteRenderer เองตอนเริ่ม จึงเก็บรายการแบบหน่วงเวลา
        private void CollectBackground()
        {
            bool stale = bgRenderers.Count == 0;
            foreach (SpriteRenderer sr in bgRenderers) if (sr == null) { stale = true; break; }
            if (!stale) return;

            bgRenderers.Clear(); bgBaseColors.Clear();
            CookieParallaxBackground parallax = FindObjectOfType<CookieParallaxBackground>();
            if (parallax == null) return;
            foreach (SpriteRenderer sr in parallax.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.gameObject.name.Contains("Void")) continue; // พื้นดำใต้ดินไม่ต้องย้อม
                bgRenderers.Add(sr);
                bgBaseColors.Add(sr.color);
            }
        }
        #endregion

        #region Atmosphere (fog + particles + overlay + banner)
        private void BuildAtmosphere()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            float camH = cam.orthographicSize * 2f;
            float camW = camH * cam.aspect;

            // หมอกบางๆ ทับฉากหลัง (อยู่หลังพื้นและตัวละคร)
            var fogGo = new GameObject("ZoneFog");
            fogGo.transform.SetParent(cam.transform, false);
            fogGo.transform.localPosition = new Vector3(0f, 0f, 10f);
            fog = fogGo.AddComponent<SpriteRenderer>();
            Texture2D white = Texture2D.whiteTexture;
            fog.sprite = Sprite.Create(white, new Rect(0, 0, white.width, white.height), new Vector2(0.5f, 0.5f), white.width);
            fog.sortingOrder = -450;
            fogGo.transform.localScale = new Vector3(camW * 1.4f, camH * 1.4f, 1f);

            // ละอองบรรยากาศ (ติดกล้อง แต่จำลองในโลก จึงเลื่อนผ่านเหมือนอยู่ในฉาก)
            var pGo = new GameObject("ZoneAmbient");
            pGo.transform.SetParent(cam.transform, false);
            pGo.transform.localPosition = new Vector3(camW * 0.15f, 0f, 10f);
            ambientParticles = pGo.AddComponent<ParticleSystem>();
            ambientParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ambientParticles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;
            var shape = ambientParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(camW * 1.5f, camH * 1.2f, 0.1f);
            var col = ambientParticles.colorOverLifetime;
            col.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = fade;
            var renderer = pGo.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = CookieFxTextures.EffectMaterial;
            renderer.sortingOrder = -300;

            // จอมืดตอนวิ่งผ่านประตู + ป้ายชื่อโซน
            var canvasGo = new GameObject("ZoneOverlay", typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var ov = new GameObject("Tunnel", typeof(RectTransform), typeof(Image));
            ov.transform.SetParent(canvasGo.transform, false);
            RectTransform or = (RectTransform)ov.transform;
            or.anchorMin = Vector2.zero; or.anchorMax = Vector2.one; or.offsetMin = or.offsetMax = Vector2.zero;
            tunnelOverlay = ov.GetComponent<Image>();
            tunnelOverlay.color = new Color(0f, 0f, 0f, 0f);
            tunnelOverlay.raycastTarget = false;

            var bn = new GameObject("ZoneBanner", typeof(RectTransform));
            bn.transform.SetParent(canvasGo.transform, false);
            RectTransform br = (RectTransform)bn.transform;
            br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.5f);
            br.sizeDelta = new Vector2(1200f, 200f);
            br.anchoredPosition = new Vector2(0f, 170f);
            zoneBanner = bn.AddComponent<TextMeshProUGUI>();
            zoneBanner.alignment = TextAlignmentOptions.Center;
            zoneBanner.fontStyle = FontStyles.Bold;
            zoneBanner.fontSize = 70f;
            zoneBanner.characterSpacing = 6f;
            zoneBanner.raycastTarget = false;
            zoneBanner.gameObject.SetActive(false);
        }

        private void SetAmbient(CookieZone zone)
        {
            if (ambientParticles == null) return;

            var main = ambientParticles.main;
            var emission = ambientParticles.emission;
            var vel = ambientParticles.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            Color c = zone.ambientColor;
            main.startColor = new ParticleSystem.MinMaxGradient(c, new Color(c.r, c.g, c.b, c.a * 0.5f));

            switch (zone.ambient)
            {
                case ZoneAmbient.Sparkles: // ประกายคริสตัลลอยช้าๆ
                    Configure(main, emission, vel, 2f, 3.5f, 0.05f, 0.12f, 16f, new Vector2(-0.15f, 0.15f), new Vector2(-0.1f, 0.2f));
                    break;
                case ZoneAmbient.Spores: // สปอร์เห็ดลอยขึ้น
                    Configure(main, emission, vel, 3.5f, 5f, 0.07f, 0.17f, 14f, new Vector2(-0.2f, 0.2f), new Vector2(0.25f, 0.6f));
                    break;
                case ZoneAmbient.Dust: // ฝุ่นในแสงตะเกียง
                    Configure(main, emission, vel, 3f, 4.5f, 0.03f, 0.09f, 26f, new Vector2(-0.3f, 0.1f), new Vector2(-0.1f, 0.1f));
                    break;
                case ZoneAmbient.Embers: // สะเก็ดไฟลอยขึ้นเร็ว
                    Configure(main, emission, vel, 1.5f, 2.5f, 0.05f, 0.12f, 30f, new Vector2(-0.3f, 0.3f), new Vector2(1f, 2.5f));
                    break;
                case ZoneAmbient.Snow: // เกล็ดน้ำแข็งตก
                    Configure(main, emission, vel, 4f, 6f, 0.06f, 0.14f, 38f, new Vector2(-0.5f, 0.1f), new Vector2(-1.2f, -0.5f));
                    break;
            }

            ambientParticles.Play();
        }

        private static void Configure(ParticleSystem.MainModule main, ParticleSystem.EmissionModule emission, ParticleSystem.VelocityOverLifetimeModule vel,
            float lifeMin, float lifeMax, float sizeMin, float sizeMax, float rate, Vector2 velX, Vector2 velY)
        {
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startSpeed = 0f;
            emission.rateOverTime = rate;
            // ทุกแกนต้องใช้โหมดเดียวกัน (สุ่มระหว่างสองค่า)
            vel.x = new ParticleSystem.MinMaxCurve(velX.x, velX.y);
            vel.y = new ParticleSystem.MinMaxCurve(velY.x, velY.y);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        }

        private IEnumerator FadeOverlay(float from, float to, float duration)
        {
            if (tunnelOverlay == null) yield break;
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.unscaledDeltaTime / Mathf.Max(0.01f, duration));
                tunnelOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t));
                yield return null;
            }
        }

        private void ShowZoneBanner(int index, CookieZone zone)
        {
            if (zoneBanner == null) return;
            string hex = ColorUtility.ToHtmlStringRGB(Color.Lerp(zone.ambientColor, Color.white, 0.35f));
            zoneBanner.text = $"<size=55%>ZONE {index + 1}</size>\n<color=#{hex}>{zone.zoneName}</color>";
            StopCoroutine(nameof(BannerRoutine));
            StartCoroutine(nameof(BannerRoutine));
        }

        private IEnumerator BannerRoutine()
        {
            zoneBanner.gameObject.SetActive(true);
            float t = 0f, duration = 2.4f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = t < 0.3f ? t / 0.3f : (t > duration - 0.6f ? (duration - t) / 0.6f : 1f);
                zoneBanner.alpha = Mathf.Clamp01(a);
                zoneBanner.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, Mathf.Clamp01(t / 0.3f));
                yield return null;
            }
            zoneBanner.gameObject.SetActive(false);
        }
        #endregion
    }
}
