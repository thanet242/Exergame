using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// หน้าหยุดเกมชั่วคราวของโหมดวิ่งและโหมดถ้ำ ใช้แผ่นเหยียบได้ทั้งหมด
/// ซ้าย/ขวา = เลือกปุ่ม, ขึ้น หรือ Enter = ยืนยัน, Esc = เล่นต่อ
/// เปิดโดย ExhibitMode (เหยียบค้างตามที่แต่ละเกมกำหนด) หรือปุ่ม Esc
/// </summary>
public class PadPauseMenu : MonoBehaviour
{
    public static bool IsOpen => current != null;
    private static PadPauseMenu current;

    private const float InputDelay = 0.6f;
    private Image[] buttons;
    private int selected = 1;           // เริ่มที่ "เล่นต่อ"
    private float shownAt;
    private float savedTimeScale;
    private bool savedAudioPause;

    public static void Show(string howToResume)
    {
        if (current != null) return;
        RectTransform root = ExhibitUI.NewCanvas("PadPauseMenu", 950);
        current = root.gameObject.AddComponent<PadPauseMenu>();
        current.Build(root, howToResume);
    }

    public static void Close()
    {
        if (current != null) current.Resume();
    }

    private void Build(RectTransform root, string howToResume)
    {
        savedTimeScale = Time.timeScale;
        savedAudioPause = AudioListener.pause;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        shownAt = Time.unscaledTime;

        Image dim = ExhibitUI.NewImage("Dim", root, null, new Color(0f, 0f, 0f, 0.65f));
        ExhibitUI.Stretch(dim.rectTransform);
        dim.raycastTarget = true;

        Image panel = ExhibitUI.NewPanel("Panel", root, ExhibitUI.Panel, ExhibitUI.Line);
        ExhibitUI.Place(panel.rectTransform, 0, 0, 900, 470);
        Text title = ExhibitUI.NewText("Title", panel.transform, "หยุดชั่วคราว", 72, ExhibitUI.Gold);
        ExhibitUI.Place(title.rectTransform, 0, 150, 860, 100);
        Text desc = ExhibitUI.NewText("Desc", panel.transform, "เวลาหยุดเดินแล้ว พักได้ตามสบาย", 34, ExhibitUI.Ink, TextAnchor.MiddleCenter, FontStyle.Normal);
        ExhibitUI.Place(desc.rectTransform, 0, 70, 860, 50);

        buttons = new Image[2];
        buttons[0] = NewButton(panel.transform, "ออกจากเกม", -200, 0);
        buttons[1] = NewButton(panel.transform, "เล่นต่อ", 200, 1);

        Text hint = ExhibitUI.NewText("Hint", panel.transform, $"ซ้าย / ขวา = เลือก      ขึ้น = ยืนยัน\n<size=26>{howToResume}</size>", 30, ExhibitUI.Muted, TextAnchor.MiddleCenter, FontStyle.Normal);
        ExhibitUI.Place(hint.rectTransform, 0, -165, 860, 90);
    }

    private Image NewButton(Transform parent, string label, float x, int index)
    {
        Image b = ExhibitUI.NewPanel("Button", parent, Color.white, Color.clear);
        ExhibitUI.Place(b.rectTransform, x, -50, 340, 96);
        b.raycastTarget = true;
        Text t = ExhibitUI.NewText("Label", b.transform, label, 40, Color.white);
        ExhibitUI.Stretch(t.rectTransform);
        Button btn = b.gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() => { selected = index; Confirm(); });
        return b;
    }

    private void Update()
    {
        Time.timeScale = 0f;
        float pulse = 1.06f + 0.03f * Mathf.Sin(Time.unscaledTime * 6f);
        for (int i = 0; i < buttons.Length; i++)
        {
            bool sel = i == selected;
            buttons[i].color = sel ? ExhibitUI.Gold : new Color(1f, 1f, 1f, 0.12f);
            buttons[i].transform.localScale = Vector3.one * (sel ? pulse : 1f);
            buttons[i].GetComponentInChildren<Text>().color = sel ? new Color(0.2f, 0.1f, 0.02f) : ExhibitUI.Ink;
        }

        if (Time.unscaledTime - shownAt < InputDelay) return;

        if (Pressed(GameInputBindings.PadLeft, KeyCode.LeftArrow, KeyCode.A)) selected = 0;
        else if (Pressed(GameInputBindings.PadRight, KeyCode.RightArrow, KeyCode.D)) selected = 1;
        else if (Pressed(GameInputBindings.PadUp, KeyCode.UpArrow, KeyCode.W) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) Confirm();
        else if (Input.GetKeyDown(KeyCode.Escape)) Resume();
    }

    private static bool Pressed(KeyCode pad, KeyCode a, KeyCode b) => Input.GetKeyDown(pad) || Input.GetKeyDown(a) || Input.GetKeyDown(b);

    private void Confirm()
    {
        if (selected == 1) { Resume(); return; }
        // ออกไปเลือกเกมอื่นที่หน้า Quest Board
        CloseWindow();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene("QuestBoard");
    }

    private void Resume()
    {
        CloseWindow();
        // คืนค่าเดิม (ช่วงสอนเล่นของโหมดวิ่งหยุดเวลาไว้รอเหยียบ ต้องคืนเป็น 0 เหมือนเดิม)
        Time.timeScale = savedTimeScale;
        AudioListener.pause = savedAudioPause;
    }

    private void CloseWindow()
    {
        // ปุ่มที่กดปิดหน้านี้ ไม่ให้เกมด้านหลังรับซ้ำในเฟรมเดียวกัน
        ExhibitMode.BlockInputThisFrame();
        if (current == this) current = null;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
    }
}
