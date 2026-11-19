using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeightInputPopup : MonoBehaviour
{
    [Header("1. UI Elements")]
    [Tooltip("ช่องพิมพ์ตัวเลขน้ำหนัก (TMP_InputField)")]
    public TMP_InputField weightInputField;

    [Tooltip("ข้อความแสดงน้ำหนัก (TMP_Text) กรณีไม่ใช้ InputField")]
    public TMP_Text weightDisplayText;

    [Tooltip("ปุ่มกดยืนยันเริ่มเกม")]
    public Button confirmButton;

    [Header("2. Weight Settings")]
    public float defaultWeight = 60.0f;
    public float minWeight = 20.0f;
    public float maxWeight = 200.0f;
    public float stepAmount = 1.0f; // ปรับเพิ่ม-ลดทีละกี่ กก.

    [Header("3. Controller & Foot-Pad Keys")]
    public KeyCode leftKey = KeyCode.Joystick1Button8;
    public KeyCode rightKey = KeyCode.Joystick1Button3;
    public KeyCode confirmKey = KeyCode.Joystick1Button0;

    private float currentWeight;

    void Awake()
    {
        // โหลดค่าน้ำหนักล่าสุดที่เคยบันทึกไว้ ถ้าไม่มีให้ใช้ค่าเริ่มต้น
        currentWeight = PlayerPrefs.GetFloat("UserWeightKg", defaultWeight);
    }

    void Start()
    {
        // หยุดเวลาเกมไว้ชั่วคราวเพื่อรอใส่น้ำหนัก
        Time.timeScale = 0f;

        UpdateWeightDisplay();

        if (weightInputField != null)
        {
            weightInputField.onValueChanged.AddListener(OnInputFieldValueChanged);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmWeight);
        }
    }

    void Update()
    {
        // รองรับการปรับน้ำหนักด้วยคีย์บอร์ดหรือ Foot-Pad
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(leftKey))
        {
            AdjustWeight(-stepAmount);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(rightKey))
        {
            AdjustWeight(stepAmount);
        }

        // กดยืนยันผ่าน Enter, Spacebar หรือปุ่ม Action
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || 
            Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(confirmKey))
        {
            OnConfirmWeight();
        }
    }

    // ปุ่มกดบวก (+)
    public void OnClickIncrease()
    {
        AdjustWeight(stepAmount);
    }

    // ปุ่มกดลบ (-)
    public void OnClickDecrease()
    {
        AdjustWeight(-stepAmount);
    }

    public void AdjustWeight(float amount)
    {
        currentWeight = Mathf.Clamp(currentWeight + amount, minWeight, maxWeight);
        UpdateWeightDisplay();
    }

    void OnInputFieldValueChanged(string value)
    {
        if (float.TryParse(value, out float parsedWeight))
        {
            currentWeight = Mathf.Clamp(parsedWeight, minWeight, maxWeight);
        }
    }

    void UpdateWeightDisplay()
    {
        if (weightInputField != null)
        {
            weightInputField.text = currentWeight.ToString("F1");
        }

        if (weightDisplayText != null)
        {
            weightDisplayText.text = $"{currentWeight:F1} kg";
        }
    }

    // ฟังก์ชันยืนยันน้ำหนักและเริ่มเล่นเกม
    public void OnConfirmWeight()
    {
        // 1. บันทึกค่าน้ำหนักลงเครื่อง
        PlayerPrefs.SetFloat("UserWeightKg", currentWeight);
        PlayerPrefs.Save();

        // 2. ปล่อยเวลาให้เกมเดินตามปกติ
        Time.timeScale = 1f;

        // 3. ปิดหน้าต่าง Popup
        gameObject.SetActive(false);
    }
}