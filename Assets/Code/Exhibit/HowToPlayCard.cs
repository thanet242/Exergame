using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// การ์ดสอนเล่นแบบดูรูปก่อนเริ่มแต่ละเกม (งานแสดง): ผังแผ่นเหยียบ + 3 ขั้นตอนสั้นๆ + เสียง AI อ่านให้ฟัง
/// โหมดวิ่ง/ถ้ำ เลือกระดับ "ปกติ" หรือ "สบาย ๆ" ได้ที่การ์ดนี้ (ซ้าย/ขวา) แล้วเหยียบขึ้นเพื่อเริ่ม
/// ระหว่างเปิดการ์ด เวลาเกมหยุด และเกมด้านหลังไม่รับปุ่ม (ExhibitMode.InputBlocked)
/// </summary>
public class HowToPlayCard : MonoBehaviour
{
    public static bool IsOpen => current != null;
    private static HowToPlayCard current;

    private class Content
    {
        public string title, voice, exitTip;
        public string up, down, left, right;   // ข้อความบนแผ่นเหยียบ (null = ไม่ใช้)
        public string[] steps;
        public bool difficultyChoice;
    }

    private static Content For(string game)
    {
        switch (game)
        {
            case "Arrow":
                return new Content
                {
                    title = "เกมจำลูกศร",
                    voice = "HowTo_Arrow",
                    up = "", down = "", left = "", right = "",
                    steps = new[]
                    {
                        "ดูลูกศรที่สว่างทีละตัว แล้วจำลำดับไว้",
                        "ถึงตาเรา เหยียบแผ่นตามลำดับที่จำไว้",
                        "เหยียบผิด เสียหัวใจ 1 ดวง  ยิ่งผ่านมาก ลำดับยิ่งยาว",
                    },
                    exitTip = "อยากหยุดระหว่างเล่น: เหยียบลงค้างไว้ 1 วินาที",
                };
            case "Runner":
                return new Content
                {
                    title = "วิ่งผจญภัยในถ้ำ",
                    voice = "HowTo_Runner",
                    up = "กระโดด", down = "สไลด์",
                    steps = new[]
                    {
                        "ตัวละครวิ่งไปข้างหน้าเอง",
                        "เหยียบขึ้น = กระโดดข้ามหิน   เหยียบลงค้าง = สไลด์ลอดใต้",
                        "เก็บเหรียญและไอเทม  ชนสิ่งกีดขวาง = เสียหัวใจ",
                    },
                    exitTip = "อยากหยุดระหว่างเล่น: เหยียบซ้ายหรือขวาค้างไว้ 1 วินาที",
                    difficultyChoice = true,
                };
            default:
                return new Content
                {
                    title = "ถ้ำกู้ระเบิด",
                    voice = "HowTo_Cave",
                    up = "เดิน", down = "เดิน", left = "เดิน", right = "เดิน",
                    steps = new[]
                    {
                        "เหยียบทิศ = เดิน   เหยียบซ้ำทิศเดิม = ขุดหิน / ตีมอนสเตอร์",
                        "ตัวเลขบนพื้น = จำนวนกับดักที่อยู่รอบช่องนั้น",
                        "เหยียบค้าง = ปักธงบนช่องที่คิดว่ามีกับดัก",
                    },
                    exitTip = "อยากหยุดระหว่างเล่น: เหยียบซ้ายและขวาพร้อมกันค้างไว้ 1 วินาที",
                    difficultyChoice = true,
                };
        }
    }

    private const float InputDelay = 0.8f;
    private Content content;
    private Image[] difficultyTiles;
    private Image startButton;
    private float shownAt;
    private float savedTimeScale;
    private bool savedAudioPause;
    private AudioSource voice;

    /// <summary>game: "Arrow" / "Runner" / "Cave"</summary>
    public static void Show(string game)
    {
        if (current != null) return;
        RectTransform root = ExhibitUI.NewCanvas("HowToPlayCard", 940);
        current = root.gameObject.AddComponent<HowToPlayCard>();
        current.Build(root, For(game));
    }

    private void Build(RectTransform root, Content c)
    {
        content = c;
        savedTimeScale = Time.timeScale;
        savedAudioPause = AudioListener.pause;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        shownAt = Time.unscaledTime;

        Image dim = ExhibitUI.NewImage("Dim", root, null, new Color(0f, 0f, 0f, 0.78f));
        ExhibitUI.Stretch(dim.rectTransform);
        dim.raycastTarget = true;

        Image panel = ExhibitUI.NewPanel("Panel", root, ExhibitUI.Panel, ExhibitUI.Line);
        ExhibitUI.Place(panel.rectTransform, 0, 0, 1560, 940);
        Transform p = panel.transform;

        Text title = ExhibitUI.NewText("Title", p, $"วิธีเล่น: {c.title}", 66, ExhibitUI.Gold);
        ExhibitUI.Place(title.rectTransform, 0, 385, 1500, 90);

        ExhibitUI.NewPadDiagram(p, -500, 90, 150, c.up, c.down, c.left, c.right);

        for (int i = 0; i < c.steps.Length; i++)
        {
            float y = 230 - i * 135;
            Image num = ExhibitUI.NewPanel("Num", p, ExhibitUI.Gold, Color.clear);
            ExhibitUI.Place(num.rectTransform, -150, y, 76, 76);
            Text n = ExhibitUI.NewText("N", num.transform, (i + 1).ToString(), 44, new Color(0.2f, 0.1f, 0.02f));
            ExhibitUI.Stretch(n.rectTransform);
            Text s = ExhibitUI.NewText("Step", p, c.steps[i], 38, ExhibitUI.Ink, TextAnchor.MiddleLeft, FontStyle.Normal);
            ExhibitUI.Place(s.rectTransform, 340, y, 880, 120);
        }

        Text exit = ExhibitUI.NewText("ExitTip", p, c.exitTip, 30, ExhibitUI.Muted, TextAnchor.MiddleCenter, FontStyle.Normal);
        ExhibitUI.Place(exit.rectTransform, 0, -150, 1480, 50);

        if (c.difficultyChoice)
        {
            Text label = ExhibitUI.NewText("DiffLabel", p, "ระดับ", 40, ExhibitUI.Ink);
            ExhibitUI.Place(label.rectTransform, -470, -255, 200, 70);
            difficultyTiles = new Image[2];
            string[] names = { "ปกติ", "สบาย ๆ  <size=26>(เด็ก / ผู้สูงอายุ)</size>" };
            for (int i = 0; i < 2; i++)
            {
                int idx = i;
                Image t = ExhibitUI.NewPanel("Diff", p, Color.white, Color.clear);
                ExhibitUI.Place(t.rectTransform, i == 0 ? -170 : 260, -255, i == 0 ? 280 : 500, 84);
                t.raycastTarget = true;
                Text tt = ExhibitUI.NewText("Label", t.transform, names[i], 38, Color.white);
                ExhibitUI.Stretch(tt.rectTransform);
                Button b = t.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => ExhibitMode.Relaxed = idx == 1);
                difficultyTiles[i] = t;
            }
        }

        startButton = ExhibitUI.NewPanel("Start", p, ExhibitUI.Gold, Color.clear);
        ExhibitUI.Place(startButton.rectTransform, 0, -375, 640, 104);
        startButton.raycastTarget = true;
        string startText = c.difficultyChoice ? "เหยียบขึ้น = เริ่มเล่น   <size=30>(ซ้าย/ขวา = เลือกระดับ)</size>" : "เหยียบขึ้น = เริ่มเล่น";
        Text st = ExhibitUI.NewText("Label", startButton.transform, startText, 44, new Color(0.2f, 0.1f, 0.02f));
        ExhibitUI.Stretch(st.rectTransform);
        Button sb = startButton.gameObject.AddComponent<Button>();
        sb.transition = Selectable.Transition.None;
        sb.onClick.AddListener(Close);
        if (c.difficultyChoice) startButton.rectTransform.sizeDelta = new Vector2(1000, 104);

        AudioClip clip = Resources.Load<AudioClip>("Exhibit/" + c.voice);
        if (clip != null)
        {
            voice = gameObject.AddComponent<AudioSource>();
            voice.ignoreListenerPause = true;
            voice.clip = clip;
            voice.Play();
        }
    }

    private void Update()
    {
        // สคริปต์ของฉากบางตัวตั้ง timeScale = 1 ตอน Start: ย้ำให้หยุดไว้ตลอดที่การ์ดเปิด
        Time.timeScale = 0f;
        float now = Time.unscaledTime;
        startButton.transform.localScale = Vector3.one * (1f + 0.03f * Mathf.Sin(now * 5f));
        if (difficultyTiles != null)
        {
            for (int i = 0; i < difficultyTiles.Length; i++)
            {
                bool sel = (i == 1) == ExhibitMode.Relaxed;
                difficultyTiles[i].color = sel ? new Color(0.25f, 0.75f, 0.95f, 1f) : new Color(1f, 1f, 1f, 0.1f);
                difficultyTiles[i].transform.localScale = Vector3.one * (sel ? 1.06f : 1f);
            }
        }

        if (now - shownAt < InputDelay) return;

        bool left = Input.GetKeyDown(GameInputBindings.PadLeft) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
        bool right = Input.GetKeyDown(GameInputBindings.PadRight) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
        if (content.difficultyChoice && (left || right))
        {
            ExhibitMode.Relaxed = right;
            return;
        }
        if (Input.GetKeyDown(GameInputBindings.PadUp) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) ||
            Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    private void Close()
    {
        if (current != this) return;
        current = null;
        ExhibitMode.BlockInputThisFrame();
        Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;
        AudioListener.pause = savedAudioPause;
        ExhibitMode.ApplyRelaxedToScene();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (current == this)
        {
            current = null;
            Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;
            AudioListener.pause = savedAudioPause;
        }
    }
}
