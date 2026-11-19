using System.Collections.Generic;
using UnityEngine;

namespace CookieRun
{
    /// <summary>
    /// เอฟเฟกต์สิ่งกีดขวางแตกกระจายตอนโดนชนด้วย Giant / Blast
    /// - หั่น Sprite ของสิ่งกีดขวางเป็นชิ้นๆ ปลิวไปข้างหน้าพร้อมหมุนและร่วงลง
    /// - ฝุ่นและประกายไฟกระจาย + วงแสงแฟลชขยายออก
    /// สร้างด้วยโค้ดทั้งหมด ไม่ต้องตั้งค่าใน Inspector (เศษใช้ซ้ำจาก Pool)
    /// </summary>
    public class CookieSmashFx : MonoBehaviour
    {
        private const int Grid = 3;
        private const float ShardLife = 0.9f;
        private const float FlashLife = 0.25f;

        private class Shard
        {
            public SpriteRenderer sr;
            public Vector3 velocity;
            public float spin, age;
            public Color color;
        }

        private class Flash
        {
            public SpriteRenderer sr;
            public float age, size;
        }

        private static CookieSmashFx instance;
        private readonly List<Shard> activeShards = new List<Shard>();
        private readonly Stack<Shard> freeShards = new Stack<Shard>();
        private readonly List<Flash> activeFlashes = new List<Flash>();
        private readonly Stack<Flash> freeFlashes = new Stack<Flash>();
        private readonly Dictionary<Sprite, Sprite[]> pieceCache = new Dictionary<Sprite, Sprite[]>();
        private ParticleSystem burst;

        private static CookieSmashFx Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameObject("CookieSmashFx").AddComponent<CookieSmashFx>();
                    instance.BuildBurst();
                }
                return instance;
            }
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            foreach (Sprite[] pieces in pieceCache.Values)
                foreach (Sprite s in pieces) if (s != null) Destroy(s);
        }

        /// <summary>เล่นเอฟเฟกต์แตกกระจายของสิ่งกีดขวาง (เรียกก่อนคืนวัตถุเข้า Pool)</summary>
        public static void Play(GameObject obstacle, bool giant, bool withShards)
        {
            if (obstacle == null) return;
            Instance.PlayInternal(obstacle, giant, withShards);
        }

        private void PlayInternal(GameObject obstacle, bool giant, bool withShards)
        {
            SpriteRenderer main = FindMainRenderer(obstacle);
            Bounds bounds = main != null ? main.bounds : new Bounds(obstacle.transform.position, Vector3.one);
            int order = main != null ? main.sortingOrder : 5;
            int layer = main != null ? main.sortingLayerID : 0;
            Color tint = main != null ? main.color : Color.white;

            // พลังของแต่ละบัฟ: ยักษ์ = เศษปลิวแรงและกว้าง, วิ่งทะลวง = พุ่งไปข้างหน้าแรง
            float power = giant ? 1.3f : 1f;
            float forward = giant ? 4f : 7f;

            if (withShards && main != null && main.sprite != null)
                SpawnShards(main, bounds, order, layer, tint, power, forward);

            Color sparkColor = giant ? new Color(0.95f, 0.5f, 1f, 1f) : new Color(1f, 0.6f, 0.2f, 1f);
            EmitBurst(bounds, order + 2, layer, sparkColor, tint, power);
            SpawnFlash(bounds, order + 3, layer, sparkColor);
        }

        // ใช้ SpriteRenderer ที่ใหญ่ที่สุดเป็นตัวแทนรูปของสิ่งกีดขวาง
        private static SpriteRenderer FindMainRenderer(GameObject obstacle)
        {
            SpriteRenderer best = null;
            float bestArea = 0f;
            foreach (SpriteRenderer sr in obstacle.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!sr.enabled || sr.sprite == null) continue;
                Vector3 size = sr.bounds.size;
                float area = size.x * size.y;
                if (area > bestArea) { bestArea = area; best = sr; }
            }
            return best;
        }

        private void SpawnShards(SpriteRenderer source, Bounds bounds, int order, int layer, Color tint, float power, float forward)
        {
            Sprite[] pieces = GetPieces(source.sprite);
            if (pieces == null) return;

            Vector3 scale = source.transform.lossyScale;
            Vector2 cell = new Vector2(bounds.size.x / Grid, bounds.size.y / Grid);

            for (int gy = 0; gy < Grid; gy++)
            {
                for (int gx = 0; gx < Grid; gx++)
                {
                    Sprite piece = pieces[gy * Grid + gx];
                    if (piece == null) continue;

                    int ix = source.flipX ? Grid - 1 - gx : gx;
                    Vector3 pos = new Vector3(bounds.min.x + cell.x * (ix + 0.5f), bounds.min.y + cell.y * (gy + 0.5f), 0f);

                    Shard s = freeShards.Count > 0 ? freeShards.Pop() : CreateShard();
                    s.sr.sprite = piece;
                    s.sr.flipX = source.flipX;
                    s.sr.sortingLayerID = layer;
                    s.sr.sortingOrder = order + 1;
                    s.color = tint;
                    s.sr.color = tint;
                    s.sr.transform.position = pos;
                    s.sr.transform.rotation = Quaternion.identity;
                    s.sr.transform.localScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), 1f);
                    s.sr.gameObject.SetActive(true);

                    // ชิ้นบนปลิวสูงกว่า ชิ้นล่างกระเด็นต่ำเรี่ยพื้น
                    float up = Mathf.Lerp(2.5f, 7f, gy / (float)(Grid - 1));
                    s.velocity = new Vector3(Random.Range(forward * 0.5f, forward * 1.1f), Random.Range(up * 0.7f, up), 0f) * power;
                    s.spin = Random.Range(-720f, 720f);
                    s.age = 0f;
                    activeShards.Add(s);
                }
            }
        }

        private Shard CreateShard()
        {
            GameObject go = new GameObject("SmashShard");
            go.transform.SetParent(transform, false);
            return new Shard { sr = go.AddComponent<SpriteRenderer>() };
        }

        // หั่น Sprite เป็นตาราง 3x3 (แคชไว้ต่อ Sprite ใช้ซ้ำได้ทุกครั้ง)
        private Sprite[] GetPieces(Sprite sprite)
        {
            if (pieceCache.TryGetValue(sprite, out Sprite[] cached)) return cached;

            Sprite[] pieces = null;
            try
            {
                // Sprite ที่ถูกแพ็กแบบ Tight หรือหมุนใน Atlas หั่นด้วย textureRect ไม่ได้
                bool usable = !sprite.packed || sprite.packingMode == SpritePackingMode.Rectangle;
                if (usable)
                {
                    Rect r = sprite.packed ? sprite.textureRect : sprite.rect;
                    float w = r.width / Grid, h = r.height / Grid;
                    pieces = new Sprite[Grid * Grid];
                    for (int gy = 0; gy < Grid; gy++)
                        for (int gx = 0; gx < Grid; gx++)
                            pieces[gy * Grid + gx] = Sprite.Create(sprite.texture,
                                new Rect(r.x + gx * w, r.y + gy * h, w, h), new Vector2(0.5f, 0.5f), sprite.pixelsPerUnit);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CookieSmashFx] หั่น Sprite {sprite.name} ไม่ได้: {ex.Message}");
                pieces = null;
            }

            pieceCache[sprite] = pieces;
            return pieces;
        }

        private void BuildBurst()
        {
            GameObject obj = new GameObject("SmashBurst");
            obj.transform.SetParent(transform, false);

            burst = obj.AddComponent<ParticleSystem>();
            burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = burst.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.gravityModifier = 1.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;

            var emission = burst.emission;
            emission.rateOverTime = 0f;

            var shape = burst.shape;
            shape.enabled = false;

            var colorOverLife = burst.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) });
            colorOverLife.color = g;

            var sizeOverLife = burst.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

            obj.GetComponent<ParticleSystemRenderer>().sharedMaterial = CookieFxTextures.EffectMaterial;
        }

        private void EmitBurst(Bounds bounds, int order, int layer, Color sparkColor, Color dustTint, float power)
        {
            var renderer = burst.GetComponent<ParticleSystemRenderer>();
            renderer.sortingLayerID = layer;
            renderer.sortingOrder = order;

            Color dust = Color.Lerp(new Color(0.75f, 0.72f, 0.7f, 0.85f), dustTint, 0.4f);
            int dustCount = Mathf.RoundToInt(18 * power), sparkCount = Mathf.RoundToInt(14 * power);

            for (int i = 0; i < dustCount + sparkCount; i++)
            {
                bool spark = i >= dustCount;
                Vector2 dir = Random.insideUnitCircle.normalized;
                dir.x = Mathf.Abs(dir.x) * 0.8f + 0.2f; // เอนไปทางที่ผู้เล่นวิ่ง
                float speed = (spark ? Random.Range(5f, 10f) : Random.Range(1.5f, 4.5f)) * power;

                var p = new ParticleSystem.EmitParams
                {
                    position = bounds.center + new Vector3(Random.Range(-bounds.extents.x, bounds.extents.x), Random.Range(-bounds.extents.y, bounds.extents.y), 0f) * 0.6f,
                    velocity = (Vector3)(dir * speed),
                    startSize = spark ? Random.Range(0.06f, 0.13f) : Random.Range(0.18f, 0.4f) * power,
                    startColor = spark ? sparkColor : dust,
                    startLifetime = spark ? Random.Range(0.25f, 0.45f) : Random.Range(0.45f, 0.8f),
                };
                burst.Emit(p, 1);
            }
        }

        private void SpawnFlash(Bounds bounds, int order, int layer, Color color)
        {
            Flash f = freeFlashes.Count > 0 ? freeFlashes.Pop() : CreateFlash();
            f.sr.sortingLayerID = layer;
            f.sr.sortingOrder = order;
            f.sr.color = new Color(Mathf.Lerp(color.r, 1f, 0.5f), Mathf.Lerp(color.g, 1f, 0.5f), Mathf.Lerp(color.b, 1f, 0.5f), 0.9f);
            f.sr.transform.position = bounds.center;
            f.size = Mathf.Max(bounds.size.x, bounds.size.y) * 1.6f;
            f.age = 0f;
            f.sr.gameObject.SetActive(true);
            activeFlashes.Add(f);
        }

        private Flash CreateFlash()
        {
            GameObject go = new GameObject("SmashFlash");
            go.transform.SetParent(transform, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CookieFxTextures.SoftCircleSprite;
            return new Flash { sr = sr };
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            for (int i = activeShards.Count - 1; i >= 0; i--)
            {
                Shard s = activeShards[i];
                s.age += dt;
                if (s.age >= ShardLife)
                {
                    s.sr.gameObject.SetActive(false);
                    activeShards.RemoveAt(i);
                    freeShards.Push(s);
                    continue;
                }

                s.velocity.y -= 22f * dt;
                Transform t = s.sr.transform;
                t.position += s.velocity * dt;
                t.Rotate(0f, 0f, s.spin * dt);

                float k = s.age / ShardLife;
                t.localScale *= 1f - dt * 0.6f;
                Color c = s.color;
                c.a *= k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
                s.sr.color = c;
            }

            for (int i = activeFlashes.Count - 1; i >= 0; i--)
            {
                Flash f = activeFlashes[i];
                f.age += dt;
                float k = f.age / FlashLife;
                if (k >= 1f)
                {
                    f.sr.gameObject.SetActive(false);
                    activeFlashes.RemoveAt(i);
                    freeFlashes.Push(f);
                    continue;
                }

                float size = f.size * Mathf.Lerp(0.4f, 1f, 1f - (1f - k) * (1f - k));
                f.sr.transform.localScale = new Vector3(size, size, 1f);
                Color c = f.sr.color;
                c.a = 0.9f * (1f - k);
                f.sr.color = c;
            }
        }
    }
}
