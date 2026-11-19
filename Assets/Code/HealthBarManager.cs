using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarManager : MonoBehaviour
{
    [Header("UI Image Reference")]
    public Image healthBarImage;

    [Header("Health Sprites (เรียงจากเลือดเต็มไปจนหมด)")]
    public Sprite[] healthSprites;

    [Header("Top Bar (หลอดเลือดแนวนอนในแถบด้านบนจอ)")]
    [Tooltip("เปิด = ซ่อนหลอดเลือดแนวตั้งเดิม แล้วแสดงหลอดเลือดแนวนอนที่มุมบนซ้าย (แถบเดียวกับหลอดเลือดมอนสเตอร์)")]
    public bool useTopBar = false;
    [Tooltip("ขยายมุมกล้องขึ้นด้านบน ให้มีที่ว่างเหนือแผนที่สำหรับแถบ HUD ไม่บังหิน")]
    public bool makeRoomAboveMap = true;

    private Image topFill, topGhost, topFrame;
    private TextMeshProUGUI topText;
    private RectTransform topRoot;
    private float target = 1f, shown = 1f, ghost = 1f, ghostHold, flash;

    private static readonly Color FillFull = new Color(0.36f, 0.86f, 0.42f, 1f);
    private static readonly Color FillMid = new Color(1f, 0.8f, 0.2f, 1f);
    private static readonly Color FillLow = new Color(0.95f, 0.26f, 0.26f, 1f);
    private static readonly Color FrameColor = new Color(0.1f, 0.08f, 0.07f, 0.95f);

    private System.Collections.IEnumerator Start()
    {
        if (!useTopBar) yield break;
        BuildTopBar();
        // รอให้กล้องจัดตำแหน่งเฟรมแรกก่อน แล้วค่อยเว้นที่ด้านบน
        yield return null;
        yield return null;
        if (makeRoomAboveMap) CaveHudCameraRoom.Apply(MonsterHealthHud.BandHeight / 1080f);
    }

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (useTopBar)
        {
            BuildTopBar();
            float pct = Mathf.Clamp01(currentHealth / Mathf.Max(1f, maxHealth));
            if (pct < target) { flash = 1f; ghostHold = 0.45f; }
            target = pct;
            topText.text = $"{Mathf.CeilToInt(Mathf.Max(0f, currentHealth))} / {Mathf.CeilToInt(maxHealth)}";
            return;
        }

        if (healthBarImage == null || healthSprites == null || healthSprites.Length == 0) return;

        float healthPercentage = currentHealth / maxHealth;

        int spriteIndex = Mathf.Clamp(
            Mathf.FloorToInt((1f - healthPercentage) * healthSprites.Length),
            0,
            healthSprites.Length - 1
        );

        healthBarImage.sprite = healthSprites[spriteIndex];
    }

    private void BuildTopBar()
    {
        if (topRoot != null) return;

        // ซ่อนหลอดเลือดแนวตั้งเดิม
        if (healthBarImage != null) healthBarImage.enabled = false;

        RectTransform band = MonsterHealthHud.Band;
        topRoot = MonsterHealthHud.NewRect("PlayerHealth", band);
        topRoot.anchorMin = topRoot.anchorMax = new Vector2(0f, 0.5f);
        topRoot.pivot = new Vector2(0f, 0.5f);
        topRoot.anchoredPosition = new Vector2(28f, 0f);
        topRoot.sizeDelta = new Vector2(560f, 62f);

        topFrame = MonsterHealthHud.AddImage(topRoot, "Frame", FrameColor);
        MonsterHealthHud.Stretch(topFrame.rectTransform, 0f, 0f, 0f, 0f);

        // ไอคอนหัวใจ
        Image heart = MonsterHealthHud.NewRect("Heart", topRoot).gameObject.AddComponent<Image>();
        heart.sprite = HeartSprite();
        heart.color = new Color(1f, 0.33f, 0.4f, 1f);
        heart.raycastTarget = false;
        heart.preserveAspect = true;
        MonsterHealthHud.Anchor(heart.rectTransform, 0f, 0f, 0f, 1f, new Vector2(12f, 9f), new Vector2(58f, -9f));

        var label = MonsterHealthHud.AddText(topRoot, "Label", "HP", 22f, new Color(1f, 0.88f, 0.7f, 1f), TextAlignmentOptions.MidlineLeft);
        MonsterHealthHud.Anchor(label.rectTransform, 0f, 0f, 0f, 1f, new Vector2(64f, 0f), new Vector2(104f, 0f));

        Image track = MonsterHealthHud.AddImage(topRoot, "Track", new Color(0.2f, 0.14f, 0.13f, 1f));
        MonsterHealthHud.Anchor(track.rectTransform, 0f, 0f, 1f, 1f, new Vector2(106f, 14f), new Vector2(-14f, -14f));
        topGhost = MonsterHealthHud.AddFilled(track.rectTransform, "Ghost", new Color(1f, 0.93f, 0.7f, 0.9f));
        topFill = MonsterHealthHud.AddFilled(track.rectTransform, "Fill", FillFull);
        Image shine = MonsterHealthHud.AddImage(topFill.rectTransform, "Shine", new Color(1f, 1f, 1f, 0.22f));
        MonsterHealthHud.Anchor(shine.rectTransform, 0f, 0.55f, 1f, 1f, new Vector2(2f, 0f), new Vector2(-2f, -2f));

        topText = MonsterHealthHud.AddText(track.rectTransform, "Value", "", 20f, Color.white, TextAlignmentOptions.Center);
        MonsterHealthHud.Stretch(topText.rectTransform, 0f, 0f, 0f, 0f);
    }

    private void Update()
    {
        if (topRoot == null) return;
        float dt = Time.deltaTime;

        shown = Mathf.MoveTowards(shown, target, dt * 2f);
        if (ghostHold > 0f) ghostHold -= dt;
        else ghost = Mathf.MoveTowards(ghost, shown, dt * 0.8f);
        flash = Mathf.Max(0f, flash - dt * 3.5f);

        topFill.fillAmount = shown;
        topGhost.fillAmount = Mathf.Max(ghost, shown);
        // เขียว -> เหลือง -> แดง ตามเลือดที่เหลือ และกะพริบเมื่อเลือดน้อย
        Color c = shown > 0.5f ? Color.Lerp(FillMid, FillFull, (shown - 0.5f) * 2f) : Color.Lerp(FillLow, FillMid, shown * 2f);
        if (shown < 0.25f) c = Color.Lerp(c, Color.white, 0.25f + 0.25f * Mathf.Sin(Time.time * 10f));
        topFill.color = Color.Lerp(c, Color.white, flash * 0.5f);
        topFrame.color = Color.Lerp(FrameColor, new Color(0.6f, 0.1f, 0.1f, 0.95f), flash);
        topRoot.localScale = Vector3.one * (1f + flash * 0.03f);
    }

    private static Sprite heartSprite;

    // หัวใจทรงพิกเซล 11x10 ขยายแบบคมชัด
    private static Sprite HeartSprite()
    {
        if (heartSprite != null) return heartSprite;
        string[] rows =
        {
            "..XXX.XXX..",
            ".XXXXXXXXX.",
            "XXWWXXXXXXX",
            "XXWXXXXXXXX",
            "XXXXXXXXXXX",
            ".XXXXXXXXX.",
            "..XXXXXXX..",
            "...XXXXX...",
            "....XXX....",
            ".....X.....",
        };
        int w = rows[0].Length, h = rows.Length;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                char ch = rows[h - 1 - y][x];
                tex.SetPixel(x, y, ch == 'X' ? Color.white : ch == 'W' ? new Color(1f, 1f, 1f, 1f) : Color.clear);
            }
        tex.Apply();
        heartSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 10f);
        return heartSprite;
    }
}

/// <summary>
/// ขยายมุมกล้อง (Cinemachine) ขึ้นด้านบน ให้เหนือแผนที่มีที่ว่างเท่ากับแถบ HUD
/// แผนที่ด่านถ้ำอยู่เต็มจอพอดี ถ้าไม่เว้นที่ แถบ HUD จะบังหินแถวบนสุด
/// </summary>
public static class CaveHudCameraRoom
{
    private static Camera appliedTo;

    /// <summary>เรียกหลังกล้องจัดตำแหน่งแล้วอย่างน้อย 1 เฟรม (ใช้ตำแหน่งกล้องปัจจุบันเป็นฐาน)</summary>
    public static void Apply(float bandFraction)
    {
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic || appliedTo == cam) return;
        appliedTo = cam;

        // ขยายภาพทั้งจอ 1/(1-แถบ) เท่า แล้วเลื่อนกล้องขึ้นเท่าที่ขยาย:
        // ขอบล่างของภาพอยู่ที่เดิม ส่วนที่ได้เพิ่มมาอยู่เหนือแผนที่ทั้งหมดพอดีกับแถบ HUD
        var vcam = Object.FindObjectOfType<Cinemachine.CinemachineVirtualCamera>();
        float oldSize = vcam != null ? vcam.m_Lens.OrthographicSize : cam.orthographicSize;
        float newSize = oldSize / (1f - bandFraction);
        Vector3 target = cam.transform.position + new Vector3(0f, newSize - oldSize, 0f);

        if (vcam != null)
        {
            // แผนที่ทั้งด่านอยู่ในจออยู่แล้ว (กล้องไม่ได้เลื่อนตามผู้เล่นจริง) จึงล็อกกล้องไว้ที่ตำแหน่งใหม่
            foreach (var ext in vcam.GetComponents<Cinemachine.CinemachineExtension>())
                if (ext.GetType().Name.Contains("Confiner")) ext.enabled = false;
            vcam.Follow = null;
            vcam.m_Lens.OrthographicSize = newSize;
            vcam.ForceCameraPosition(target, vcam.transform.rotation);
        }
        else
        {
            cam.orthographicSize = newSize;
            cam.transform.position = target;
        }
    }
}
