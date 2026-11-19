using System.Collections.Generic;
using UnityEngine;

namespace CookieRun
{
    /// <summary>
    /// ระบบฉากหลังแบบ Parallax Scrolling และ Infinite Looping สำหรับเกมวิ่ง Cookie Run
    /// รองรับทั้งการเลื่อนหลายเลเยอร์ (Multi-layer) เลื่อนตามความเร็วกล้องอย่างนุ่มนวล และวนลูปไร้รอยต่อ
    /// </summary>
    [ExecuteAlways]
    public class CookieParallaxBackground : MonoBehaviour
    {
        [System.Serializable]
        public class ParallaxLayer
        {
            [Tooltip("ชื่อเลเยอร์")]
            public string layerName = "Background";

            [Tooltip("รูปภาพ Sprite ของเลเยอร์นี้")]
            public Sprite sprite;

            [Tooltip("อัตราส่วนการเลื่อน Parallax (สำหรับโหมดหลายแผ่น)")]
            [Range(0f, 1f)]
            public float parallaxSpeed = 0.35f;

            [Tooltip("ความเร็วขยับฉากหลังเทียบกับกล้องในโหมดผืนเดียว (0.05 = ช้า 5% ของความเร็ววิ่ง ให้ความรู้สึกอยู่ลึกเข้าไปในถ้ำไกลๆ)")]
            [Range(0.01f, 0.3f)]
            public float slowDriftRatio = 0.05f;

            [Tooltip("ความสูงแกน Y")]
            public float yOffset = 2.0f;

            [Tooltip("ขนาดสเกลของภาพ")]
            public Vector2 scale = new Vector2(2.5f, 2.5f);

            [Tooltip("Sorting Order (ติดลบเพื่อให้วาดอยู่หลังสุด เช่น -500)")]
            public int sortingOrder = -500;

            [Tooltip("สีและค่าความโปร่งแสง")]
            public Color tintColor = Color.white;

            [Tooltip("จำนวนแผ่นภาพ (1 = ฉากหลังเดี่ยวอันเดียวระยะไกล ขยับช้าๆ, 2-5 = โหมดหลายแผ่นวนลูป)")]
            [Range(1, 5)]
            public int sliceCount = 1;

            // Runtime data
            [HideInInspector] public GameObject container;
            [HideInInspector] public GameObject[] slices;
            [HideInInspector] public float sliceWidth;
        }

        [Header("Camera Reference")]
        [Tooltip("กล้องหลักที่ฉากหลังจะเลื่อนตาม")]
        [SerializeField] private Camera targetCamera;

        [Header("Layers")]
        [Tooltip("รายการเลเยอร์ฉากหลัง (รองรับหลายชั้น)")]
        [SerializeField] private List<ParallaxLayer> layers = new List<ParallaxLayer>();

        [Header("Underground Void (พื้นหลังใต้พื้นดิน)")]
        [Tooltip("เปิดใช้งานพื้นหลังใต้พื้นดินสีดำ (ความมืดมิดใต้หลุมและใต้ดิน)")]
        [SerializeField] private bool enableUndergroundVoid = true;

        [Tooltip("ระดับความสูงขอบบนของพื้นหลังสีดำ (ระดับพื้นดิน ปกติคือ 0)")]
        [SerializeField] private float undergroundVoidTopY = 0f;

        [Tooltip("ความลึกของพื้นหลังสีดำ")]
        [SerializeField] private float undergroundVoidHeight = 20f;

        [Tooltip("สีของพื้นหลังใต้ดิน")]
        [SerializeField] private Color undergroundColor = Color.black;

        private GameObject undergroundVoidObj;
        private Sprite cachedWhiteSprite;

        private float lastCameraX;

        public List<ParallaxLayer> Layers => layers;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void OnEnable()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
            if (targetCamera != null)
            {
                lastCameraX = targetCamera.transform.position.x;
            }
            InitializeLayers();
        }

        private void OnDisable()
        {
            CleanupLayers();
        }

        private void Start()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera != null)
            {
                lastCameraX = targetCamera.transform.position.x;
            }

            InitializeLayers();
        }

        public void CleanupLayers()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == null) continue;
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }

        public void InitializeLayers()
        {
            CleanupLayers();

            if (targetCamera == null) targetCamera = Camera.main;
            float camX = targetCamera != null ? targetCamera.transform.position.x : 0f;

            foreach (var layer in layers)
            {
                if (layer.sprite == null) continue;

                // สร้าง Container สำหรับแต่ละเลเยอร์ (วางที่ Z = 10 เพื่อให้อยู่ด้านหลังระนาบตัวละครที่ Z = 0 เสมอ)
                layer.container = new GameObject($"Layer_{layer.layerName}");
                layer.container.transform.SetParent(transform);
                layer.container.hideFlags = HideFlags.DontSave;
                layer.container.transform.position = new Vector3(camX, 0f, 10f);

                // คำนวณความกว้างของ 1 แผ่น
                float unitWidth = layer.sprite.rect.width / layer.sprite.pixelsPerUnit;
                float unitHeight = layer.sprite.rect.height / layer.sprite.pixelsPerUnit;
                layer.sliceWidth = unitWidth * layer.scale.x;

                if (layer.sliceCount == 1)
                {
                    // โหมดผืนเดียวอันเดียว (Single Distant Backdrop) ตามคำขอของผู้ใช้:
                    // มีเพียง 1 GameObject ไม่สร้างหลายแผ่นให้รกสายตา
                    layer.slices = new GameObject[1];
                    GameObject singleBackdrop = new GameObject("Backdrop_Distant");
                    singleBackdrop.transform.SetParent(layer.container.transform);
                    singleBackdrop.hideFlags = HideFlags.DontSave;

                    SpriteRenderer sr = singleBackdrop.AddComponent<SpriteRenderer>();
                    sr.sprite = layer.sprite;
                    sr.drawMode = SpriteDrawMode.Tiled;
                    // ขยายความกว้างแบบ Tiled ให้ครอบคลุมมุมมองจออย่างเหลือเฟือ (4 เท่าของภาพ หรือประมาณ 100 ยูนิต)
                    sr.size = new Vector2(unitWidth * 4.0f, unitHeight);
                    sr.sortingOrder = layer.sortingOrder;
                    sr.color = layer.tintColor;

                    singleBackdrop.transform.localScale = new Vector3(layer.scale.x, layer.scale.y, 1f);
                    singleBackdrop.transform.localPosition = new Vector3(0f, layer.yOffset, 0f);

                    layer.slices[0] = singleBackdrop;
                }
                else
                {
                    // โหมดหลายแผ่น (Multi-slice fallback)
                    layer.slices = new GameObject[layer.sliceCount];
                    int startOffsetIndex = -(layer.sliceCount / 2);
                    for (int i = 0; i < layer.sliceCount; i++)
                    {
                        GameObject slice = new GameObject($"Slice_{i}");
                        slice.transform.SetParent(layer.container.transform);
                        slice.hideFlags = HideFlags.DontSave;

                        float localX = (startOffsetIndex + i) * layer.sliceWidth;
                        slice.transform.localPosition = new Vector3(localX, layer.yOffset, 0f);
                        slice.transform.localScale = new Vector3(layer.scale.x, layer.scale.y, 1f);

                        SpriteRenderer sr = slice.AddComponent<SpriteRenderer>();
                        sr.sprite = layer.sprite;
                        sr.sortingOrder = layer.sortingOrder;
                        sr.color = layer.tintColor;

                        layer.slices[i] = slice;
                    }
                }
            }

            // สร้างพื้นหลังใต้ดินสีดำ (Underground Black Void)
            if (enableUndergroundVoid)
            {
                undergroundVoidObj = new GameObject("Void_Underground");
                undergroundVoidObj.transform.SetParent(transform);
                undergroundVoidObj.hideFlags = HideFlags.DontSave;

                SpriteRenderer voidSr = undergroundVoidObj.AddComponent<SpriteRenderer>();
                voidSr.sprite = GetOrCreateWhiteSprite();
                voidSr.color = undergroundColor;
                // sortingOrder = -400: อยู่หน้าฉากหลัง Cave (-500) แต่อยู่หลังพื้นหิน Ground (+1)
                // ทำให้ใต้หลุมและใต้ระดับพื้นดินมืดสนิทเป็นสีดำสนิท
                voidSr.sortingOrder = -400;

                undergroundVoidObj.transform.localScale = new Vector3(120f, undergroundVoidHeight, 1f);
                // ด้วย Pivot ขอบบนกึ่งกลาง (0.5, 1.0) ขอบบนสุดจะถูกล็อคอยู่ที่แกน Y = undergroundVoidTopY พอดีเป๊ะ
                // และขยายตัวลงล่างไปถึง Y = undergroundVoidTopY - undergroundVoidHeight (-20)
                // จะไม่ล้นขึ้นไปบังฉากหลังถ้ำด้านบน (Y > undergroundVoidTopY) เด็ดขาด
                undergroundVoidObj.transform.position = new Vector3(camX, undergroundVoidTopY, 5f);
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null) return;

            float currentCameraX = targetCamera.transform.position.x;
            float deltaCameraX = currentCameraX - lastCameraX;
            lastCameraX = currentCameraX;

            // เลื่อนพื้นหลังใต้ดินสีดำตามแกน X ของกล้อง (ล็อคขอบบนไว้ที่ undergroundVoidTopY เสมอ)
            if (undergroundVoidObj != null)
            {
                undergroundVoidObj.transform.position = new Vector3(currentCameraX, undergroundVoidTopY, 5f);
            }

            foreach (var layer in layers)
            {
                if (layer.container == null || layer.slices == null || layer.slices.Length == 0) continue;

                if (layer.sliceCount == 1)
                {
                    // โหมดฉากหลังเดี่ยวระยะไกล: ขยับช้าๆ ตามกล้อง (slow drift)
                    // เลื่อนช้าๆ เพียง slowDriftRatio (เช่น 5% ของความเร็ววิ่ง) ให้ความรู้สึกฉากหลังอยู่ลึกและไกลมาก
                    // และใช้วิธี Repeat เพื่อวนลูปอย่างไร้รอยต่อโดยที่หน้าจอไม่มีการกระตุกหรือรอยต่อใดๆ
                    float driftX = Mathf.Repeat(currentCameraX * layer.slowDriftRatio, layer.sliceWidth);
                    layer.container.transform.position = new Vector3(currentCameraX - driftX, 0f, 10f);
                }
                else
                {
                    if (Mathf.Abs(deltaCameraX) < 0.00001f) continue;

                    // เลื่อน Container ตามอัตราส่วน Parallax
                    layer.container.transform.position += new Vector3(deltaCameraX * layer.parallaxSpeed, 0f, 0f);

                    // เช็คการวนลูปของแต่ละ Slice
                    for (int i = 0; i < layer.slices.Length; i++)
                    {
                        GameObject slice = layer.slices[i];
                        if (slice == null) continue;

                        float sliceWorldX = layer.container.transform.position.x + slice.transform.localPosition.x;

                        // ถ้าแผ่นภาพหลุดไปทางซ้ายของกล้องเกิน 1.5 แผ่น ให้ย้ายไปต่อท้ายด้านขวาสุด
                        if (currentCameraX - sliceWorldX > layer.sliceWidth * 1.5f)
                        {
                            // หา slice ที่อยู่ขวาสุดในเชิง localPosition.x
                            float maxLocalX = slice.transform.localPosition.x;
                            for (int j = 0; j < layer.slices.Length; j++)
                            {
                                if (layer.slices[j] != null && layer.slices[j].transform.localPosition.x > maxLocalX)
                                {
                                    maxLocalX = layer.slices[j].transform.localPosition.x;
                                }
                            }

                            slice.transform.localPosition = new Vector3(maxLocalX + layer.sliceWidth, layer.yOffset, 0f);
                        }
                    }
                }
            }
        }

        private Sprite GetOrCreateWhiteSprite()
        {
            if (cachedWhiteSprite == null)
            {
                Texture2D tex = Texture2D.whiteTexture;
                // pixelsPerUnit = tex.width (4) เพื่อให้ขนาดของ Sprite เป็น 1.0 x 1.0 ยูนิตใน World Space พอดี
                // pivot = (0.5f, 1.0f) ให้จุด Pivot อยู่ที่ขอบบนกึ่งกลาง (Top-Center)
                // เพื่อให้ Sprite ขยายตัวลงล่างอย่างเดียว ไม่ล้นขึ้นไปบังฉากหลังถ้ำด้านบน (Y > 0)
                cachedWhiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 1.0f), tex.width);
            }
            return cachedWhiteSprite;
        }

        /// <summary>
        /// เพิ่มเลเยอร์ฉากหลังใหม่
        /// </summary>
        public void AddLayer(string name, Sprite sprite, float parallaxSpeed, float yOffset, Vector2 scale, int sortingOrder, Color tint, int sliceCount = 1, float slowDriftRatio = 0.05f)
        {
            ParallaxLayer newLayer = new ParallaxLayer
            {
                layerName = name,
                sprite = sprite,
                parallaxSpeed = parallaxSpeed,
                slowDriftRatio = slowDriftRatio,
                yOffset = yOffset,
                scale = scale,
                sortingOrder = sortingOrder,
                tintColor = tint,
                sliceCount = sliceCount
            };
            layers.Add(newLayer);
            InitializeLayers();
        }

        /// <summary>
        /// เปลี่ยนภาพของเลเยอร์ (ใช้ตอนเปลี่ยนโซน) แล้วสร้างฉากหลังใหม่
        /// </summary>
        public void SetLayerSprite(int index, Sprite sprite)
        {
            if (sprite == null || index < 0 || index >= layers.Count) return;
            if (layers[index].sprite == sprite) return;
            layers[index].sprite = sprite;
            InitializeLayers();
        }

        public Sprite GetLayerSprite(int index) => index >= 0 && index < layers.Count ? layers[index].sprite : null;
    }
}
