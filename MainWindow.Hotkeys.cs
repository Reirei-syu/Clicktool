using ClickTool.Services;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace ClickTool;

public partial class MainWindow
{
    private const int HotkeyEditorWindowId = 9000;
    private const int HotkeyRecordId = 9001;
    private const int HotkeyPlayId = 9002;
    private const int HotkeyStepId = 9003;
    private const int HotkeyStopId = 9004;
    private const int WmHotkey = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private void HotkeyBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        SetStatus("直接按下你想设置的按键，按 Esc 表示不设置热键。");
    }

    private void HotkeyBox_KeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        if (sender is not TextBox textBox)
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            ApplyHotkey(textBox, 0);
            RegisterHotKeys(showFailureStatus: false);
            StorageService.SaveSettings(_settings);
            UpdateHotkeyTextBoxes();
            SetStatus($"已清空{GetHotkeyLabel(textBox)}热键。");
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (IsModifierOnlyKey(key))
        {
            return;
        }

        var virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);
        if (IsHotkeyDuplicate(textBox, virtualKey))
        {
            SetStatus("热键不能重复，请换一个键。");
            return;
        }

        var previousEditorWindow = _settings.HotkeyEditorWindow;
        var previousRecord = _settings.HotkeyRecord;
        var previousPlay = _settings.HotkeyPlayAll;
        var previousStep = _settings.HotkeyStep;
        var previousStop = _settings.HotkeyStop;

        ApplyHotkey(textBox, virtualKey);
        if (!RegisterHotKeys(showFailureStatus: true))
        {
            _settings.HotkeyEditorWindow = previousEditorWindow;
            _settings.HotkeyRecord = previousRecord;
            _settings.HotkeyPlayAll = previousPlay;
            _settings.HotkeyStep = previousStep;
            _settings.HotkeyStop = previousStop;
            RegisterHotKeys(showFailureStatus: false);
            UpdateHotkeyTextBoxes();
            SetStatus("热键注册失败，可能被系统或其他软件占用。");
            return;
        }

        StorageService.SaveSettings(_settings);
        UpdateHotkeyTextBoxes();
        SetStatus($"已更新{GetHotkeyLabel(textBox)}热键。");
    }

    private void UpdateHotkeyTextBoxes()
    {
        TxtHotkeyEditorWindow.Text = GetHotkeyDisplay(_settings.HotkeyEditorWindow);
        TxtHotkeyRecord.Text = GetHotkeyDisplay(_settings.HotkeyRecord);
        TxtHotkeyPlay.Text = GetHotkeyDisplay(_settings.HotkeyPlayAll);
        TxtHotkeyStep.Text = GetHotkeyDisplay(_settings.HotkeyStep);
        TxtHotkeyStop.Text = GetHotkeyDisplay(_settings.HotkeyStop);
    }

    private bool RegisterHotKeys(bool showFailureStatus)
    {
        var handle = new WindowInteropHelper(this).Handle;
        UnregisterHotKeys();

        var registrations = new (int Id, uint VirtualKey, string Label)[]
        {
            (HotkeyEditorWindowId, _settings.HotkeyEditorWindow, "窗口查看"),
            (HotkeyRecordId, _settings.HotkeyRecord, "录制"),
            (HotkeyPlayId, _settings.HotkeyPlayAll, "播放"),
            (HotkeyStepId, _settings.HotkeyStep, "按步播放"),
            (HotkeyStopId, _settings.HotkeyStop, "停止")
        };

        foreach (var registration in registrations)
        {
            if (registration.VirtualKey == 0)
            {
                continue;
            }

            if (RegisterHotKey(handle, registration.Id, 0, registration.VirtualKey))
            {
                continue;
            }

            UnregisterHotKeys();
            if (showFailureStatus)
            {
                SetStatus($"{registration.Label}热键注册失败，请换一个键。");
            }

            return false;
        }

        var source = HwndSource.FromHwnd(handle);
        source?.RemoveHook(WndProc);
        source?.AddHook(WndProc);
        return true;
    }

    private void UnregisterHotKeys()
    {
        var handle = new WindowInteropHelper(this).Handle;
        UnregisterHotKey(handle, HotkeyEditorWindowId);
        UnregisterHotKey(handle, HotkeyRecordId);
        UnregisterHotKey(handle, HotkeyPlayId);
        UnregisterHotKey(handle, HotkeyStepId);
        UnregisterHotKey(handle, HotkeyStopId);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmHotkey)
        {
            return IntPtr.Zero;
        }

        switch (wParam.ToInt32())
        {
            case HotkeyEditorWindowId:
                OpenRecordEditorWindow();
                handled = true;
                break;
            case HotkeyRecordId:
                ToggleRecording();
                handled = true;
                break;
            case HotkeyPlayId:
                if (BtnPlayAll.IsEnabled)
                {
                    BtnPlayAll_Click(this, new RoutedEventArgs());
                }

                handled = true;
                break;
            case HotkeyStepId:
                if (BtnStepPlay.IsEnabled)
                {
                    BtnStepPlay_Click(this, new RoutedEventArgs());
                }

                handled = true;
                break;
            case HotkeyStopId:
                if (BtnStop.IsEnabled)
                {
                    BtnStop_Click(this, new RoutedEventArgs());
                }

                handled = true;
                break;
        }

        return IntPtr.Zero;
    }

    private bool IsHotkeyDuplicate(TextBox currentTextBox, uint virtualKey)
    {
        if (virtualKey == 0)
        {
            return false;
        }

        foreach (var pair in new[]
                 {
                     (TextBox: TxtHotkeyEditorWindow, VirtualKey: _settings.HotkeyEditorWindow),
                     (TextBox: TxtHotkeyRecord, VirtualKey: _settings.HotkeyRecord),
                     (TextBox: TxtHotkeyPlay, VirtualKey: _settings.HotkeyPlayAll),
                     (TextBox: TxtHotkeyStep, VirtualKey: _settings.HotkeyStep),
                     (TextBox: TxtHotkeyStop, VirtualKey: _settings.HotkeyStop)
                 })
        {
            if (ReferenceEquals(pair.TextBox, currentTextBox))
            {
                continue;
            }

            if (pair.VirtualKey == virtualKey)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyHotkey(TextBox textBox, uint virtualKey)
    {
        if (ReferenceEquals(textBox, TxtHotkeyEditorWindow))
        {
            _settings.HotkeyEditorWindow = virtualKey;
            return;
        }

        if (ReferenceEquals(textBox, TxtHotkeyRecord))
        {
            _settings.HotkeyRecord = virtualKey;
            return;
        }

        if (ReferenceEquals(textBox, TxtHotkeyPlay))
        {
            _settings.HotkeyPlayAll = virtualKey;
            return;
        }

        if (ReferenceEquals(textBox, TxtHotkeyStep))
        {
            _settings.HotkeyStep = virtualKey;
            return;
        }

        _settings.HotkeyStop = virtualKey;
    }

    private string GetHotkeyLabel(TextBox textBox)
    {
        if (ReferenceEquals(textBox, TxtHotkeyEditorWindow))
        {
            return "窗口查看";
        }

        if (ReferenceEquals(textBox, TxtHotkeyRecord))
        {
            return "录制";
        }

        if (ReferenceEquals(textBox, TxtHotkeyPlay))
        {
            return "播放";
        }

        if (ReferenceEquals(textBox, TxtHotkeyStep))
        {
            return "按步播放";
        }

        return "停止";
    }

    private static string GetHotkeyDisplay(uint virtualKey)
    {
        if (virtualKey == 0)
        {
            return "未设置";
        }

        var key = KeyInterop.KeyFromVirtualKey((int)virtualKey);
        return key == Key.None ? "未设置" : key.ToString();
    }

    private static bool IsModifierOnlyKey(Key key)
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
}
