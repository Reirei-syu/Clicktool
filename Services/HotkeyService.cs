using System.Windows.Input;

namespace ClickTool.Services;

public readonly record struct HotkeyBinding(uint VirtualKey, uint Modifiers)
{
    public bool IsEmpty => VirtualKey == 0;
}

public static class HotkeyService
{
    public const uint ModAlt = 0x0001;
    public const uint ModControl = 0x0002;
    public const uint ModShift = 0x0004;
    public const uint ModWin = 0x0008;

    public static bool TryCreate(
        Key key,
        ModifierKeys modifiers,
        out HotkeyBinding binding,
        out string error)
    {
        binding = default;
        error = string.Empty;

        if (key == Key.None || IsModifierOnlyKey(key))
        {
            error = "请选择一个主键。";
            return false;
        }

        var normalizedModifiers = NormalizeModifiers(modifiers);
        if (normalizedModifiers == uint.MaxValue)
        {
            error = "仅支持单键或一个修饰键加一个主键。";
            return false;
        }

        binding = new HotkeyBinding((uint)KeyInterop.VirtualKeyFromKey(key), normalizedModifiers);
        return true;
    }

    public static string ToDisplay(HotkeyBinding binding)
    {
        if (binding.IsEmpty)
        {
            return "未设置";
        }

        var parts = new List<string>();
        if ((binding.Modifiers & ModControl) != 0)
        {
            parts.Add("Ctrl");
        }

        if ((binding.Modifiers & ModAlt) != 0)
        {
            parts.Add("Alt");
        }

        if ((binding.Modifiers & ModShift) != 0)
        {
            parts.Add("Shift");
        }

        if ((binding.Modifiers & ModWin) != 0)
        {
            parts.Add("Win");
        }

        var key = KeyInterop.KeyFromVirtualKey((int)binding.VirtualKey);
        parts.Add(key == Key.None ? binding.VirtualKey.ToString() : key.ToString());
        return string.Join("+", parts);
    }

    public static bool IsModifierOnlyKey(Key key)
    {
        return key is Key.LeftCtrl
            or Key.RightCtrl
            or Key.LeftAlt
            or Key.RightAlt
            or Key.LeftShift
            or Key.RightShift
            or Key.LWin
            or Key.RWin;
    }

    private static uint NormalizeModifiers(ModifierKeys modifiers)
    {
        if (modifiers == ModifierKeys.None)
        {
            return 0;
        }

        var supportedModifiers = new[]
        {
            (Key: ModifierKeys.Control, Value: ModControl),
            (Key: ModifierKeys.Alt, Value: ModAlt),
            (Key: ModifierKeys.Shift, Value: ModShift),
            (Key: ModifierKeys.Windows, Value: ModWin)
        };

        var modifierCount = 0;
        uint result = 0;

        foreach (var modifier in supportedModifiers)
        {
            if (!modifiers.HasFlag(modifier.Key))
            {
                continue;
            }

            modifierCount++;
            result |= modifier.Value;
        }

        return modifierCount <= 1 ? result : uint.MaxValue;
    }
}
