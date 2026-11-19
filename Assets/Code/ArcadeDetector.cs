using UnityEngine;

public class ArcadeDetector : MonoBehaviour
{
    private string lastInputText = "รอการเหยียบแผ่นฟอยล์...";

    void OnGUI()
    {
        // แก้ไขชนิดข้อมูลเป็น GUIStyle ให้ถูกต้อง
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 18;
        style.normal.textColor = Color.yellow;

        GUI.Label(new Rect(20, 20, 800, 40), "--- [Arcade Scanner] ---", style);
        
        style.normal.textColor = Color.cyan;
        GUI.Label(new Rect(20, 60, 800, 40), lastInputText, style);
    }

    void Update()
    {
        // 1. ตรวจจับการกดปุ่มสวิตช์ทุกปุ่ม
        foreach (KeyCode kcode in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(kcode))
            {
                lastInputText = $"[พบปุ่มกด!] ปุ่มที่เหยียบคือ: KeyCode.{kcode}";
                Debug.Log(lastInputText);
            }
        }

        // 2. ตรวจจับทิศทาง D-Pad / Axis มาตรฐาน
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (h > 0.5f) { lastInputText = "[พบทิศทาง!] เหยียบทิศทาง: RIGHT (ขวา)"; Debug.Log(lastInputText); }
        else if (h < -0.5f) { lastInputText = "[พบทิศทาง!] เหยียบทิศทาง: LEFT (ซ้าย)"; Debug.Log(lastInputText); }
        else if (v > 0.5f) { lastInputText = "[พบทิศทาง!] เหยียบทิศทาง: UP (ขึ้น)"; Debug.Log(lastInputText); }
        else if (v < -0.5f) { lastInputText = "[พบทิศทาง!] เหยียบทิศทาง: DOWN (ลง)"; Debug.Log(lastInputText); }
    }
}