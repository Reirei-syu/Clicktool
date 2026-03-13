using ClickTool.Services;
using System.Windows.Input;
using Xunit;

namespace ClickTool.Tests;

public class HotkeyServiceTests
{
    [Fact]
    public void TryCreate_AllowsSingleModifierPlusMainKey()
    {
        var result = HotkeyService.TryCreate(Key.R, ModifierKeys.Control, out var binding, out var error);

        Assert.True(result);
        Assert.Equal(string.Empty, error);
        Assert.Equal((uint)KeyInterop.VirtualKeyFromKey(Key.R), binding.VirtualKey);
        Assert.Equal(HotkeyService.ModControl, binding.Modifiers);
        Assert.Equal("Ctrl+R", HotkeyService.ToDisplay(binding));
    }

    [Fact]
    public void TryCreate_RejectsMoreThanOneModifier()
    {
        var result = HotkeyService.TryCreate(
            Key.R,
            ModifierKeys.Control | ModifierKeys.Shift,
            out var binding,
            out var error);

        Assert.False(result);
        Assert.True(binding.IsEmpty);
        Assert.Equal("仅支持单键或一个修饰键加一个主键。", error);
    }
}
