using UnityEngine;

public class StageConfig : MonoBehaviour
{
    [Header("1. ข้อมูลประจำด่าน")]
    [Tooltip("จำนวนกับดักทั้งหมดในด่านนี้")]
    public int totalTrapsInStage = 10;

    [Header("2. การคำนวณแคลอรี่ตามสูตร METs")]
    [Tooltip("ค่าความเข้มข้นของการออกกำลังกาย (METs): เดินสบายๆ = 3.0, เคลื่อนไหวต่อเนื่อง = 4.0 - 5.0, เหนื่อยหนัก = 6.0+")]
    public float stageMETs = 4.0f;

    [Tooltip("น้ำหนักตัวผู้เล่น (กก.) - ใช้เป็นค่าเริ่มต้นถ้ายังไม่มีการบันทึกโปรไฟล์")]
    public float defaultWeightKg = 60.0f;

    [Header("3. สถิติที่บันทึกได้ในฉากนี้")]
    [SerializeField] private int stepCount = 0;
    [SerializeField] private int digCount = 0;
    [SerializeField] private int flagCount = 0;
    [SerializeField] private int attackCount = 0;

    public void RecordStep() => stepCount++;
    public void RecordDig() => digCount++;
    public void RecordAttack() => attackCount++;
    public void RecordFlag() => flagCount++;

    // คำนวณแคลอรี่ตามสูตร: (METs * 3.5 * น้ำหนัก) / 200 * เวลา (นาที)
    public float CalculateCalories()
    {
        // 1. ดึงน้ำหนักตัว (ถ้ามีระบบเซฟ PlayerPrefs จะดึงค่านั้นมาใช้ก่อน ถ้าไม่มีจะใช้น้ำหนักเริ่มต้น)
        float userWeight = PlayerPrefs.GetFloat("UserWeightKg", defaultWeightKg);

        // 2. ดึงเวลาที่เล่นจริงในด่านนี้ (แปลงจากวินาทีเป็นนาที)
        float playTimeInMinutes = Time.timeSinceLevelLoad / 60f;

        // 3. คำนวณตามสูตร
        float caloriesBurned = ((stageMETs * 3.5f * userWeight) / 200f) * playTimeInMinutes;

        return caloriesBurned;
    }

    public void SendSummaryData(SummaryPopup popup)
    {
        if (popup == null) return;

        float totalCalories = CalculateCalories();
        popup.totalTrapsInStage = totalTrapsInStage;
        popup.DisplayStageClear(totalCalories, stepCount, digCount, flagCount, attackCount);
    }

    public void SendDeathData(DeathPopup popup)
    {
        if (popup == null) return;

        float totalCalories = CalculateCalories();
        popup.totalTrapsInStage = totalTrapsInStage;
        popup.DisplayDeathCalories(totalCalories, stepCount, digCount, flagCount, attackCount);
    }
}