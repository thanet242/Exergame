using UnityEngine;
using TMPro;

public class KeyRebinder : MonoBehaviour
{
    [Header("UI Text บนปุ่มสำหรับแสดงชื่อปุ่มปัจจุบัน")]
    public TextMeshProUGUI upKeyText;
    public TextMeshProUGUI downKeyText;
    public TextMeshProUGUI leftKeyText;
    public TextMeshProUGUI rightKeyText;

    // เก็บค่าปุ่มปัจจุบัน (ค่าเริ่มต้น)
    public static KeyCode upKey = KeyCode.W;
    public static KeyCode downKey = KeyCode.S;
    public static KeyCode leftKey = KeyCode.A;
    public static KeyCode rightKey = KeyCode.D;

    private string waitingForKey = ""; 

    void Start()
    {
        UpdateAllButtonTexts();
    }

    void OnGUI()
    {
        // เมื่อผู้เล่นกดปุ่มในหน้า Option เพื่อรอเปลี่ยน แล้วกดแป้นพิมพ์ตัวใหม่
        if (waitingForKey != "")
        {
            Event e = Event.current;
            if (e.isKey && e.type == EventType.KeyDown)
            {
                KeyCode newKey = e.keyCode;
                if (newKey == KeyCode.None || newKey == KeyCode.Escape) 
                {
                    waitingForKey = ""; 
                    return;
                }

                // บันทึกปุ่มใหม่ตามทิศทางที่เลือก
                if (waitingForKey == "UP") upKey = newKey;
                else if (waitingForKey == "DOWN") downKey = newKey;
                else if (waitingForKey == "LEFT") leftKey = newKey;
                else if (waitingForKey == "RIGHT") rightKey = newKey;

                waitingForKey = ""; // ปิดสถานะรอรับค่า
                UpdateAllButtonTexts(); // อัปเดตข้อความบนปุ่ม UI ทันที
                e.Use();
            }
        }
    }

    public void StartRebind(string direction)
    {
        waitingForKey = direction;
        Debug.Log($"<color=yellow>กำลังรอรับปุ่มใหม่สำหรับทิศทาง: {direction} (กดปุ่มบนคีย์บอร์ดที่ต้องการได้เลย)</color>");
    }

    void UpdateAllButtonTexts()
    {
        if (upKeyText) upKeyText.text = upKey.ToString();
        if (downKeyText) downKeyText.text = downKey.ToString();
        if (leftKeyText) leftKeyText.text = leftKey.ToString();
        if (rightKeyText) rightKeyText.text = rightKey.ToString();
    }

    // ฟังก์ชันเชื่อมโยงกับปุ่ม UI แต่ละอันใน Inspector
    public void RebindUp() { StartRebind("UP"); }
    public void RebindDown() { StartRebind("DOWN"); }
    public void RebindLeft() { StartRebind("LEFT"); }
    public void RebindRight() { StartRebind("RIGHT"); }
}