using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// การกระทำในเกมที่ผู้เล่นเปลี่ยนปุ่มได้จากหน้า Option
/// </summary>
public enum GameAction
{
    RunJump,    // โหมดวิ่ง: กระโดด
    RunSlide,   // โหมดวิ่ง: สไลด์
    MoveUp,     // โหมดถ้ำ: เดินขึ้น
    MoveDown,   // โหมดถ้ำ: เดินลง
    MoveLeft,   // โหมดถ้ำ: เดินซ้าย
    MoveRight   // โหมดถ้ำ: เดินขวา
}

/// <summary>
/// ระบบปุ่มกลางของทั้งเกม: แต่ละการกระทำมีปุ่มแผ่นเหยียบ 1 ปุ่ม + ปุ่มคีย์บอร์ด 1 ปุ่ม (เปลี่ยนได้และบันทึกใน PlayerPrefs)
/// และมีปุ่มลูกศรบนคีย์บอร์ดเป็นปุ่มสำรองถาวรเสมอ
/// ปุ่มสำหรับเลือกเมนู (ซ้าย/ขวา/ยืนยัน) ไม่ได้อยู่ในระบบนี้ เพื่อไม่ให้ผู้เล่นตั้งค่าผิดจนกดเมนูไม่ได้
/// </summary>
public static class GameInputBindings
{
    // ทิศของแผ่นเหยียบ 4 ทิศ
    public const KeyCode PadUp = KeyCode.Joystick1Button5;
    public const KeyCode PadDown = KeyCode.Joystick1Button11;
    public const KeyCode PadLeft = KeyCode.Joystick1Button8;
    public const KeyCode PadRight = KeyCode.Joystick1Button3;

    private struct Defaults
    {
        public KeyCode pad, key, fallback;
        public Defaults(KeyCode pad, KeyCode key, KeyCode fallback) { this.pad = pad; this.key = key; this.fallback = fallback; }
    }

    private static readonly Dictionary<GameAction, Defaults> defaults = new Dictionary<GameAction, Defaults>
    {
        { GameAction.RunJump,   new Defaults(PadUp,    KeyCode.Space, KeyCode.UpArrow) },
        { GameAction.RunSlide,  new Defaults(PadDown,  KeyCode.S,     KeyCode.DownArrow) },
        { GameAction.MoveUp,    new Defaults(PadUp,    KeyCode.W,     KeyCode.UpArrow) },
        { GameAction.MoveDown,  new Defaults(PadDown,  KeyCode.S,     KeyCode.DownArrow) },
        { GameAction.MoveLeft,  new Defaults(PadLeft,  KeyCode.A,     KeyCode.LeftArrow) },
        { GameAction.MoveRight, new Defaults(PadRight, KeyCode.D,     KeyCode.RightArrow) },
    };

    // กลุ่มการกระทำที่ใช้พร้อมกันในโหมดเดียวกัน (ห้ามใช้ปุ่มซ้ำกันในกลุ่มเดียวกัน)
    private static readonly GameAction[] runnerGroup = { GameAction.RunJump, GameAction.RunSlide };
    private static readonly GameAction[] caveGroup = { GameAction.MoveUp, GameAction.MoveDown, GameAction.MoveLeft, GameAction.MoveRight };

    private static readonly Dictionary<GameAction, KeyCode> padBinding = new Dictionary<GameAction, KeyCode>();
    private static readonly Dictionary<GameAction, KeyCode> keyBinding = new Dictionary<GameAction, KeyCode>();
    private static bool loaded;
    private static KeyCode[] moveKeysCache;

    /// <summary>แจ้งเมื่อมีการเปลี่ยนปุ่ม (เช่น ให้ UI อัปเดตข้อความ)</summary>
    public static event Action OnBindingsChanged;

    private static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        foreach (var pair in defaults)
        {
            padBinding[pair.Key] = (KeyCode)PlayerPrefs.GetInt(PrefKey(pair.Key, "Pad"), (int)pair.Value.pad);
            keyBinding[pair.Key] = (KeyCode)PlayerPrefs.GetInt(PrefKey(pair.Key, "Key"), (int)pair.Value.key);
        }
        moveKeysCache = null;
    }

    private static string PrefKey(GameAction action, string slot) => $"Bind_{action}_{slot}";

    public static KeyCode GetPad(GameAction action) { EnsureLoaded(); return padBinding[action]; }
    public static KeyCode GetKeyboard(GameAction action) { EnsureLoaded(); return keyBinding[action]; }
    public static KeyCode GetFallback(GameAction action) => defaults[action].fallback;

    public static bool GetDown(GameAction action)
    {
        EnsureLoaded();
        return Input.GetKeyDown(padBinding[action]) || Input.GetKeyDown(keyBinding[action]) || Input.GetKeyDown(defaults[action].fallback);
    }

    public static bool GetHeld(GameAction action)
    {
        EnsureLoaded();
        return Input.GetKey(padBinding[action]) || Input.GetKey(keyBinding[action]) || Input.GetKey(defaults[action].fallback);
    }

    public static bool GetUp(GameAction action)
    {
        EnsureLoaded();
        return Input.GetKeyUp(padBinding[action]) || Input.GetKeyUp(keyBinding[action]) || Input.GetKeyUp(defaults[action].fallback);
    }

    /// <summary>ปุ่มทั้งหมดของการเดิน 4 ทิศ (ใช้กับระบบที่ต้องเช็กปุ่มทีละปุ่ม เช่น เหยียบ 2 ครั้ง/กดค้าง)</summary>
    public static KeyCode[] MoveKeys
    {
        get
        {
            EnsureLoaded();
            if (moveKeysCache == null)
            {
                var list = new List<KeyCode>();
                foreach (GameAction a in caveGroup)
                {
                    foreach (KeyCode k in new[] { padBinding[a], keyBinding[a], defaults[a].fallback })
                    {
                        if (k != KeyCode.None && !list.Contains(k)) list.Add(k);
                    }
                }
                moveKeysCache = list.ToArray();
            }
            return moveKeysCache;
        }
    }

    /// <summary>
    /// ตั้งปุ่มใหม่: ปุ่มจอย/แผ่นเหยียบไปที่ช่องแผ่นเหยียบ ปุ่มคีย์บอร์ดไปที่ช่องคีย์บอร์ด
    /// ถ้าปุ่มนี้ถูกใช้โดยการกระทำอื่นในโหมดเดียวกัน จะสลับปุ่มให้กันอัตโนมัติ
    /// </summary>
    public static void SetBinding(GameAction action, KeyCode newKey)
    {
        EnsureLoaded();
        bool isPad = IsJoystickKey(newKey);
        Dictionary<GameAction, KeyCode> slot = isPad ? padBinding : keyBinding;

        KeyCode oldKey = slot[action];
        foreach (GameAction other in GroupOf(action))
        {
            if (other != action && slot[other] == newKey)
            {
                slot[other] = oldKey;
                PlayerPrefs.SetInt(PrefKey(other, isPad ? "Pad" : "Key"), (int)oldKey);
            }
        }

        slot[action] = newKey;
        PlayerPrefs.SetInt(PrefKey(action, isPad ? "Pad" : "Key"), (int)newKey);
        PlayerPrefs.Save();
        moveKeysCache = null;
        OnBindingsChanged?.Invoke();
    }

    public static void ResetToDefaults()
    {
        foreach (var pair in defaults)
        {
            padBinding[pair.Key] = pair.Value.pad;
            keyBinding[pair.Key] = pair.Value.key;
            PlayerPrefs.DeleteKey(PrefKey(pair.Key, "Pad"));
            PlayerPrefs.DeleteKey(PrefKey(pair.Key, "Key"));
        }
        loaded = true;
        PlayerPrefs.Save();
        moveKeysCache = null;
        OnBindingsChanged?.Invoke();
    }

    private static GameAction[] GroupOf(GameAction action) =>
        (action == GameAction.RunJump || action == GameAction.RunSlide) ? runnerGroup : caveGroup;

    public static bool IsJoystickKey(KeyCode key) => key >= KeyCode.JoystickButton0 && key <= KeyCode.Joystick8Button19;

    /// <summary>ชื่อปุ่มสำหรับแสดงผล เช่น "PAD UP", "SPACE"</summary>
    public static string DisplayName(KeyCode key)
    {
        switch (key)
        {
            case PadUp: return "PAD UP";
            case PadDown: return "PAD DOWN";
            case PadLeft: return "PAD LEFT";
            case PadRight: return "PAD RIGHT";
            case KeyCode.None: return "-";
            case KeyCode.UpArrow: return "UP ARROW";
            case KeyCode.DownArrow: return "DOWN ARROW";
            case KeyCode.LeftArrow: return "LEFT ARROW";
            case KeyCode.RightArrow: return "RIGHT ARROW";
            case KeyCode.LeftControl: return "L-CTRL";
            case KeyCode.RightControl: return "R-CTRL";
            case KeyCode.LeftShift: return "L-SHIFT";
            case KeyCode.RightShift: return "R-SHIFT";
            case KeyCode.Return: return "ENTER";
        }

        if (IsJoystickKey(key))
        {
            // ปุ่มจอยอื่นๆ เช่น Joystick1Button7 -> "PAD BTN 7"
            string name = key.ToString();
            int idx = name.LastIndexOf("Button", StringComparison.Ordinal);
            return idx >= 0 ? "PAD BTN " + name.Substring(idx + 6) : name.ToUpperInvariant();
        }

        string s = key.ToString();
        if (s.StartsWith("Alpha", StringComparison.Ordinal)) return s.Substring(5);
        if (s.StartsWith("Keypad", StringComparison.Ordinal)) return "NUM " + s.Substring(6).ToUpperInvariant();
        return s.ToUpperInvariant();
    }

    /// <summary>ข้อความรวมของการกระทำ เช่น "PAD UP / SPACE"</summary>
    public static string DescribeAction(GameAction action) =>
        $"{DisplayName(GetPad(action))} / {DisplayName(GetKeyboard(action))}";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        loaded = false;
        moveKeysCache = null;
        padBinding.Clear();
        keyBinding.Clear();
        OnBindingsChanged = null;
    }
}
