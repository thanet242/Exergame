using UnityEngine;

namespace CookieRun
{
    /// <summary>
    /// Texture / Sprite / Material ที่สร้างด้วยโค้ด ใช้ร่วมกันระหว่างเอฟเฟกต์ต่างๆ (สร้างครั้งเดียวแล้วแคชไว้)
    /// </summary>
    public static class CookieFxTextures
    {
        private static Texture2D softCircleTexture;
        private static Sprite softCircleSprite;
        private static Material effectMaterial;

        /// <summary>วงกลมขาวขอบจางนุ่ม ขนาด 1x1 ยูนิต</summary>
        public static Texture2D SoftCircleTexture
        {
            get
            {
                if (softCircleTexture == null)
                {
                    softCircleTexture = GenerateCircleTexture(64, r => Mathf.Clamp01(1f - r) * Mathf.Clamp01(1f - r));
                }
                return softCircleTexture;
            }
        }

        public static Sprite SoftCircleSprite
        {
            get
            {
                if (softCircleSprite == null)
                {
                    Texture2D tex = SoftCircleTexture;
                    softCircleSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
                }
                return softCircleSprite;
            }
        }

        /// <summary>
        /// Material สำหรับ Particle / Trail (Sprites/Default อยู่ใน Always Included Shaders ใช้ได้ทั้ง Editor และ Build)
        /// </summary>
        public static Material EffectMaterial
        {
            get
            {
                if (effectMaterial == null)
                {
                    effectMaterial = new Material(Shader.Find("Sprites/Default"));
                    effectMaterial.mainTexture = SoftCircleTexture;
                }
                return effectMaterial;
            }
        }

        public static Texture2D GenerateCircleTexture(int size, System.Func<float, float> alphaByRadius)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color32[] pixels = new Color32[size * size];
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    byte a = (byte)(Mathf.Clamp01(alphaByRadius(r)) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }
    }
}
