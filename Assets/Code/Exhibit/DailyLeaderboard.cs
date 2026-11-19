using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ตารางคะแนนของวันนี้ (เก็บใน PlayerPrefs แยกตามเกมและวันที่ ขึ้นวันใหม่เริ่มนับใหม่)
/// ใช้แสดง "อันดับวันนี้" ในหน้าสรุปผล และ Top 3 ในหน้าเริ่มเล่นของงานแสดง
/// </summary>
public static class DailyLeaderboard
{
    public const string ArrowMemory = "Arrow";
    public const string Runner = "Runner";
    private const int MaxEntries = 200;

    private static string Key(string game) => $"LB_{game}_{DateTime.Now:yyyyMMdd}";

    private static List<int> Load(string game)
    {
        var list = new List<int>();
        string raw = PlayerPrefs.GetString(Key(game), "");
        foreach (string s in raw.Split(','))
            if (int.TryParse(s, out int v)) list.Add(v);
        return list;
    }

    /// <summary>บันทึกผลรอบนี้ คืนค่าอันดับ (เริ่มที่ 1) และจำนวนรอบทั้งหมดของวันนี้</summary>
    public static (int rank, int count) Submit(string game, int value)
    {
        List<int> list = Load(game);
        list.Add(value);
        list.Sort((a, b) => b.CompareTo(a));
        if (list.Count > MaxEntries) list.RemoveRange(MaxEntries, list.Count - MaxEntries);
        PlayerPrefs.SetString(Key(game), string.Join(",", list));
        PlayerPrefs.Save();
        return (list.IndexOf(value) + 1, list.Count);
    }

    public static List<int> Top(string game, int n)
    {
        List<int> list = Load(game);
        return list.GetRange(0, Mathf.Min(n, list.Count));
    }

    /// <summary>ข้อความอันดับสำหรับหน้าสรุปผล เช่น "อันดับวันนี้: ที่ 2 จาก 15 รอบ"</summary>
    public static string RankText(int rank, int count) =>
        rank == 1 ? $"อันดับวันนี้: ที่ 1 จาก {count} รอบ - สูงสุดของวัน!" : $"อันดับวันนี้: ที่ {rank} จาก {count} รอบ";
}

/// <summary>ตัวช่วยหน้าสรุปผล: แคลอรี่เทียบกับการเดิน และสูตรแคลอรี่กลาง</summary>
public static class ResultHelpers
{
    /// <summary>เดินปกติราว 0.04 kcal ต่อก้าว</summary>
    public static string KcalCompare(float kcal)
    {
        int stepsEq = Mathf.Max(1, Mathf.RoundToInt(kcal / 0.04f));
        return $"เท่ากับเดินประมาณ {stepsEq:N0} ก้าว";
    }

    /// <summary>แคลอรี่จากค่า METs: (METs x 3.5 x น้ำหนัก) / 200 x นาที (สูตรเดียวกันทุกเกม)</summary>
    public static float CaloriesFromMets(float mets, float seconds)
    {
        float weight = PlayerPrefs.GetFloat("UserWeightKg", 60f);
        return (mets * 3.5f * weight / 200f) * (seconds / 60f);
    }
}
