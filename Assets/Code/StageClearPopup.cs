using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class StageClearPopup : MonoBehaviour
{
    [Header("UI Text References")]
    public TMP_Text timeText;
    public TMP_Text trapsText;
    public TMP_Text kcalText;

    [Header("Scene Navigation Settings")]
    [SerializeField] private string targetMainMenuSceneName = "MainMenu";
    [SerializeField] private bool useManualNextScene = false;
    [SerializeField] private string targetNextSceneName = "";

    public void DisplayStageClearData(float timePassed, int clearedTraps, int totalTraps, float totalCalories)
    {
        Debug.Log($"<color=green>[StageClear] ข้อมูลส่งมาถึงแล้ว: เวลา={timePassed:F1}s, กับดัก={clearedTraps}/{totalTraps}, แคลอรี={totalCalories:F1}</color>");

        // 1. Time (MM:SS)
        int minutes = Mathf.FloorToInt(timePassed / 60f);
        int seconds = Mathf.FloorToInt(timePassed % 60f);
        if (timeText != null)
        {
            timeText.text = $"{minutes:00}:{seconds:00}";
        }
        else
        {
            Debug.LogWarning("[StageClear] timeText ยังไม่ได้ลากใส่ใน Inspector!");
        }

        // 2. Traps & Accuracy
        float accuracy = totalTraps > 0 ? ((float)clearedTraps / totalTraps) * 100f : 100f;
        if (trapsText != null)
        {
            trapsText.text = $"Trap clear\n{clearedTraps} / {totalTraps}\n(Acc: {accuracy:0}%)";
        }
        else
        {
            Debug.LogWarning("[StageClear] trapsText ยังไม่ได้ลากใส่ใน Inspector!");
        }

        // 3. Calories
        if (kcalText != null)
        {
            kcalText.text = $"Burn cal\n{totalCalories:F1}kcal";
        }
        else
        {
            Debug.LogWarning("[StageClear] kcalText ยังไม่ได้ลากใส่ใน Inspector!");
        }
    }

    public void OnClickRestartButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnClickNextSceneButton()
    {
        Time.timeScale = 1f;

        if (useManualNextScene && !string.IsNullOrEmpty(targetNextSceneName))
        {
            SceneManager.LoadScene(targetNextSceneName);
        }
        else
        {
            int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextSceneIndex);
            }
            else
            {
                SceneManager.LoadScene(0);
            }
        }
    }

    public void OnClickMainMenuButton()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(targetMainMenuSceneName))
        {
            SceneManager.LoadScene(targetMainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(0); 
        }
    }
}