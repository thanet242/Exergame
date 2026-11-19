using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("1. รายการ Scene ทั้งหมดที่ต้องการใช้งาน")]
    [Tooltip("ใส่รายชื่อ Scene ทั้งหมดที่โปรเจกต์นี้จะโหลด")]
    public string[] sceneNames;

    [Header("2. ชื่อ Scene เริ่มต้น / ค่าหลัก")]
    public string defaultSceneName = "MainMenu";

    // ==========================================
    // ฟังก์ชันโหลด Scene ตามรูปแบบต่างๆ
    // ==========================================

    // 1. โหลดตามชื่อ Scene ที่ส่งเข้ามาตรงๆ (แนะนำสำหรับใส่ใน OnClick ของปุ่ม UI)
    public void LoadSceneByName(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            ResetTimeScale();
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("[SceneLoader] ชื่อ Scene ที่ส่งมาว่างเปล่า!");
        }
    }

    // 2. โหลดตามลำดับ Index ในรายการ sceneNames (เช่น 0 = ด่าน 1, 1 = ด่าน 2)
    public void LoadSceneByIndex(int index)
    {
        if (sceneNames != null && index >= 0 && index < sceneNames.Length)
        {
            string targetName = sceneNames[index];
            if (!string.IsNullOrEmpty(targetName))
            {
                ResetTimeScale();
                SceneManager.LoadScene(targetName);
            }
        }
        else
        {
            Debug.LogWarning($"[SceneLoader] Index {index} อยู่นอกเหนือช่วงของรายการ Scene Names!");
        }
    }

    // 3. โหลด Scene ค่าเริ่มต้น (Default)
    public void LoadDefaultScene()
    {
        LoadSceneByName(defaultSceneName);
    }

    // 4. โหลดฉากถัดไปตามลำดับใน Build Settings (Next Scene)
    public void LoadNextBuildScene()
    {
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            ResetTimeScale();
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            Debug.LogWarning("[SceneLoader] ไม่มี Scene ถัดไปใน Build Settings (ถึงฉากสุดท้ายแล้ว)");
        }
    }

    // 5. โหลดฉากปัจจุบันซ้ำใหม่ (Restart Level)
    public void RestartCurrentScene()
    {
        ResetTimeScale();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ==========================================
    // ฟังก์ชันเสริม (Utility)
    // ==========================================

    private void ResetTimeScale()
    {
        Time.timeScale = 1f; // คืนค่าเวลาเกมให้เป็นปกติเผื่อเกมถูก Pause ไว้
    }

    // ฟังก์ชันปิดหน้าต่าง Pop-up
    public void ClosePopup(GameObject popupObject)
    {
        if (popupObject != null)
        {
            popupObject.SetActive(false);
        }
    }
}