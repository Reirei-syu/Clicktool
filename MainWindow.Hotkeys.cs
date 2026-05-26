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
    private const int HotkeyNextSchemeId = 9005;
    private const int HotkeyPrevSchemeId = 9006;
    private const int WmHotkey = 0x0312;

    // Listening mode state for improved hotkey UX (Phase 1 usability)
    private bool _isHotkeyListening;
    private TextBox? _listeningBox;
    private System.Windows.Threading.DispatcherTimer? _hotkeyListenTimeoutTimer;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private void HotkeyBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        SetStatus("直接按键设置热键，支持单键或一个修饰键加一个主键，按 Esc 清空。");
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
            SetHotkeyBinding(textBox, default);
            RegisterHotKeys(showFailureStatus: false);
            StorageService.SaveSettings(_settings);
            UpdateHotkeyTextBoxes();
            SetStatus($"已清空{GetHotkeyLabel(textBox)}热键。");
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (HotkeyService.IsModifierOnlyKey(key))
        {
            SetStatus("请再按一个主键完成热键设置。");
            return;
        }

        if (!HotkeyService.TryCreate(key, Keyboard.Modifiers, out var binding, out var error))
        {
            SetStatus(error);
            return;
        }

        if (IsHotkeyDuplicate(textBox, binding))
        {
            SetStatus("热键不能重复，请换一个组合。");
            return;
        }

        var snapshot = CaptureHotkeyBindings();
        SetHotkeyBinding(textBox, binding);

        if (!RegisterHotKeys(showFailureStatus: true))
        {
            RestoreHotkeyBindings(snapshot);
            RegisterHotKeys(showFailureStatus: false);
            UpdateHotkeyTextBoxes();
            SetStatus("热键注册失败，可能被系统或其他软件占用。");
            return;
        }

        StorageService.SaveSettings(_settings);
        UpdateHotkeyTextBoxes();
        SetStatus($"已更新{GetHotkeyLabel(textBox)}热键为 {HotkeyService.ToDisplay(binding)}。");
    }

    // === Hotkey Listening Mode (major UX improvement) ===
    private void StartHotkeyListening(TextBox targetBox)
    {
        if (_isHotkeyListening)
        {
            CancelHotkeyListening();
        }

        _isHotkeyListening = true;
        _listeningBox = targetBox;

        targetBox.Style = (Style)FindResource("HotkeyBoxListeningStyle");
        targetBox.Text = "请按键... (Esc 取消)";

        SetStatus("正在监听热键输入。按任意键（含修饰键）完成设置，或按 Esc 取消。");

        // Optional 12s timeout for safety
        _hotkeyListenTimeoutTimer?.Stop();
        _hotkeyListenTimeoutTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(12)
        };
        _hotkeyListenTimeoutTimer.Tick += (_, _) =>
        {
            CancelHotkeyListening();
            SetStatus("热键监听已超时自动取消。");
        };
        _hotkeyListenTimeoutTimer.Start();

        // Use PreviewKeyDown on the window so we capture even if focus is elsewhere
        PreviewKeyDown -= HotkeyListening_PreviewKeyDown;
        PreviewKeyDown += HotkeyListening_PreviewKeyDown;
    }

    private void CancelHotkeyListening()
    {
        if (!_isHotkeyListening || _listeningBox == null)
        {
            return;
        }

        _hotkeyListenTimeoutTimer?.Stop();
        _hotkeyListenTimeoutTimer = null;

        PreviewKeyDown -= HotkeyListening_PreviewKeyDown;

        // Restore original style and display text
        _listeningBox.Style = (Style)FindResource("HotkeyBoxStyle");
        UpdateHotkeyTextBoxes(); // restores correct "Ctrl+F12" or "未设置"

        _isHotkeyListening = false;
        _listeningBox = null;
    }

    private void HotkeyListening_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_isHotkeyListening || _listeningBox == null)
        {
            return;
        }

        e.Handled = true;

        if (e.Key == Key.Escape)
        {
            CancelHotkeyListening();
            SetStatus("已取消热键设置。");
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (HotkeyService.IsModifierOnlyKey(key))
        {
            SetStatus("请再按一个主键完成热键设置。");
            return;
        }

        if (!HotkeyService.TryCreate(key, Keyboard.Modifiers, out var binding, out var error))
        {
            SetStatus(error);
            CancelHotkeyListening();
            return;
        }

        if (IsHotkeyDuplicate(_listeningBox, binding))
        {
            SetStatus("热键不能重复，请换一个组合。");
            CancelHotkeyListening();
            return;
        }

        var snapshot = CaptureHotkeyBindings();
        SetHotkeyBinding(_listeningBox, binding);

        if (!RegisterHotKeys(showFailureStatus: true))
        {
            RestoreHotkeyBindings(snapshot);
            RegisterHotKeys(showFailureStatus: false);
            UpdateHotkeyTextBoxes();
            SetStatus("热键注册失败，可能被系统或其他软件占用。");
            CancelHotkeyListening();
            return;
        }

        StorageService.SaveSettings(_settings);
        UpdateHotkeyTextBoxes();

        var label = GetHotkeyLabel(_listeningBox);
        SetStatus($"已更新{label}热键为 {HotkeyService.ToDisplay(binding)}。");

        CancelHotkeyListening();
    }

    private void HotkeyRecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not TextBox target)
        {
            return;
        }

        StartHotkeyListening(target);
    }

    private void UpdateHotkeyTextBoxes()
    {
        TxtHotkeyEditorWindow.Text = HotkeyService.ToDisplay(GetHotkeyBinding(TxtHotkeyEditorWindow));
        TxtHotkeyRecord.Text = HotkeyService.ToDisplay(GetHotkeyBinding(TxtHotkeyRecord));
        TxtHotkeyPlay.Text = HotkeyService.ToDisplay(GetHotkeyBinding(TxtHotkeyPlay));
        TxtHotkeyStep.Text = HotkeyService.ToDisplay(GetHotkeyBinding(TxtHotkeyStep));
        TxtHotkeyStop.Text = HotkeyService.ToDisplay(GetHotkeyBinding(TxtHotkeyStop));
    }

    private bool RegisterHotKeys(bool showFailureStatus)
    {
        var handle = new WindowInteropHelper(this).Handle;
        UnregisterHotKeys();

        var registrations = new (int Id, HotkeyBinding Binding, string Label)[]
        {
            (HotkeyEditorWindowId, GetHotkeyBinding(TxtHotkeyEditorWindow), "窗口查看"),
            (HotkeyRecordId, GetHotkeyBinding(TxtHotkeyRecord), "录制"),
            (HotkeyPlayId, GetHotkeyBinding(TxtHotkeyPlay), "播放"),
            (HotkeyStepId, GetHotkeyBinding(TxtHotkeyStep), "按步播放"),
            (HotkeyStopId, GetHotkeyBinding(TxtHotkeyStop), "停止")
        };

        foreach (var registration in registrations)
        {
            if (registration.Binding.IsEmpty)
            {
                continue;
            }

            if (RegisterHotKey(handle, registration.Id, registration.Binding.Modifiers, registration.Binding.VirtualKey))
            {
                continue;
            }

            UnregisterHotKeys();
            if (showFailureStatus)
            {
                SetStatus($"{registration.Label}热键注册失败，请换一个组合。");
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

    private bool IsHotkeyDuplicate(TextBox currentTextBox, HotkeyBinding binding)
    {
        if (binding.IsEmpty)
        {
            return false;
        }

        foreach (var pair in new[]
                 {
                     (TextBox: TxtHotkeyEditorWindow, Binding: GetHotkeyBinding(TxtHotkeyEditorWindow)),
                     (TextBox: TxtHotkeyRecord, Binding: GetHotkeyBinding(TxtHotkeyRecord)),
                     (TextBox: TxtHotkeyPlay, Binding: GetHotkeyBinding(TxtHotkeyPlay)),
                     (TextBox: TxtHotkeyStep, Binding: GetHotkeyBinding(TxtHotkeyStep)),
                     (TextBox: TxtHotkeyStop, Binding: GetHotkeyBinding(TxtHotkeyStop))
                 })
        {
            if (ReferenceEquals(pair.TextBox, currentTextBox))
            {
                continue;
            }

            if (pair.Binding == binding)
            {
                return true;
            }
        }

        return false;
    }

    private HotkeyBinding GetHotkeyBinding(TextBox textBox)
    {
        if (ReferenceEquals(textBox, TxtHotkeyEditorWindow))
        {
            return new HotkeyBinding(_settings.HotkeyEditorWindow, _settings.HotkeyEditorWindowModifiers);
        }

        if (ReferenceEquals(textBox, TxtHotkeyRecord))
        {
            return new HotkeyBinding(_settings.HotkeyRecord, _settings.HotkeyRecordModifiers);
        }

        if (ReferenceEquals(textBox, TxtHotkeyPlay))
        {
            return new HotkeyBinding(_settings.HotkeyPlayAll, _settings.HotkeyPlayAllModifiers);
        }

        if (ReferenceEquals(textBox, TxtHotkeyStep))
        {
            return new HotkeyBinding(_settings.HotkeyStep, _settings.HotkeyStepModifiers);
        }

        return new HotkeyBinding(_settings.HotkeyStop, _settings.HotkeyStopModifiers);
    }

    private void SetHotkeyBinding(TextBox textBox, HotkeyBinding binding)
    {
        if (ReferenceEquals(textBox, TxtHotkeyEditorWindow))
        {
            _settings.HotkeyEditorWindow = binding.VirtualKey;
            _settings.HotkeyEditorWindowModifiers = binding.Modifiers;
            return;
        }

        if (ReferenceEquals(textBox, TxtHotkeyRecord))
        {
            _settings.HotkeyRecord = binding.VirtualKey;
            _settings.HotkeyRecordModifiers = binding.Modifiers;
            return;
        }

        if (ReferenceEquals(textBox, TxtHotkeyPlay))
        {
            _settings.HotkeyPlayAll = binding.VirtualKey;
            _settings.HotkeyPlayAllModifiers = binding.Modifiers;
            return;
        }

        if (ReferenceEquals(textBox, TxtHotkeyStep))
        {
            _settings.HotkeyStep = binding.VirtualKey;
            _settings.HotkeyStepModifiers = binding.Modifiers;
            return;
        }

        _settings.HotkeyStop = binding.VirtualKey;
        _settings.HotkeyStopModifiers = binding.Modifiers;
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

    private Dictionary<TextBox, HotkeyBinding> CaptureHotkeyBindings()
    {
        return new Dictionary<TextBox, HotkeyBinding>
        {
            [TxtHotkeyEditorWindow] = GetHotkeyBinding(TxtHotkeyEditorWindow),
            [TxtHotkeyRecord] = GetHotkeyBinding(TxtHotkeyRecord),
            [TxtHotkeyPlay] = GetHotkeyBinding(TxtHotkeyPlay),
            [TxtHotkeyStep] = GetHotkeyBinding(TxtHotkeyStep),
            [TxtHotkeyStop] = GetHotkeyBinding(TxtHotkeyStop)
        };
    }

    private void RestoreHotkeyBindings(IReadOnlyDictionary<TextBox, HotkeyBinding> snapshot)
    {
        foreach (var pair in snapshot)
        {
            SetHotkeyBinding(pair.Key, pair.Value);
        }
    }
}
