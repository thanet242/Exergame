using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("ลากหน้าต่าง UI Panel ของ Option มาใส่ในช่องนี้")]
    public GameObject optionsPanel; 

    void Start()
    {
        // ซ่อนหน้าต่าง Option ไว้ก่อนตั้งแต่ตอนเริ่มเกม (เผื่อเราลืมปิดใน Editor)
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }
    }

    // 🎮 ฟังก์ชันสำหรับปุ่ม [PLAY]
    public void PlayGame()
    {
        // โหลด Scene ถัดไปตามลำดับใน Build Settings
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // ⚙️ ฟังก์ชันสำหรับปุ่ม [OPTION] (เปิดหน้าตั้งค่า)
    public void OpenOptions()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true); // สั่งให้หน้าต่าง Option แสดงขึ้นมา
            Debug.Log("เปิดหน้าต่าง Option");
        }
        else
        {
            Debug.LogWarning("ยังไม่ได้ใส่ Options Panel ในช่อง Inspector!");
        }
    }

    // ❌ ฟังก์ชันสำหรับปุ่มปิดหน้า Option (ใช้กับปุ่ม X กากบาทในหน้าตั้งค่า)
    public void CloseOptions()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false); // สั่งซ่อนหน้าต่าง Option
            Debug.Log("ปิดหน้าต่าง Option");
        }
    }

    // 🚪 ฟังก์ชันสำหรับปุ่ม [EXIT GAME]
    public void QuitGame()
    {
        Debug.Log("ปิดเกมแล้ว!"); 
        Application.Quit(); 
    }
}