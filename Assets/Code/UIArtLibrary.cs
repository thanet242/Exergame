using UnityEngine;

/// <summary>
/// รวมภาพ/เสียง UI ที่ใช้ร่วมกันทุกฉาก (โหลดจาก Resources/UIArtLibrary)
/// ใช้กับ UI ที่สร้างด้วยโค้ด เช่น หน้าสรุปผล Game Over / Stage Clear และ HUD ของโหมดวิ่ง
/// </summary>
[CreateAssetMenu(fileName = "UIArtLibrary", menuName = "Game/UI Art Library")]
public class UIArtLibrary : ScriptableObject
{
    [Header("Result Popups")]
    public Sprite gameOverSprite;
    public Sprite stageClearSprite;
    [Tooltip("STAGE CLEAR ที่ปุ่มขวาเป็น Next (ใช้เมื่อยังมีด่านต่อไป)")]
    public Sprite stageClearNextSprite;
    [Tooltip("GAME OVER ของโหมดวิ่ง: ช่องกลางเป็น Distance แทน Traps Cleared")]
    public Sprite gameOverDistanceSprite;
    [Tooltip("หน้าสรุปผลของโหมดวิ่ง (หัวข้อ RESULTS ช่องกลางเป็น Distance)")]
    public Sprite resultDistanceSprite;

    [Header("Shared Pieces")]
    [Tooltip("ป้ายไม้ขอบทอง (ใช้กับ HUD และปุ่ม)")]
    public Sprite plaqueSprite;

    [Header("Sounds")]
    public AudioClip selectSound;
    public AudioClip confirmSound;

    private static UIArtLibrary cached;

    public static UIArtLibrary Instance
    {
        get
        {
            if (cached == null) cached = Resources.Load<UIArtLibrary>("UIArtLibrary");
            return cached;
        }
    }
}
