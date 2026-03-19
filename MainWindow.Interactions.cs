using ClickTool.Services;
using System.Windows;
using System.Windows.Input;
using RecordedMouseAction = ClickTool.Models.MouseAction;

namespace ClickTool;

public partial class MainWindow
{
    private void CompactView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _compactClickCandidate = true;
        _compactPointerStart = e.GetPosition(this);
        CompactView.CaptureMouse();
    }

    private void CompactView_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_compactClickCandidate || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var currentPosition = e.GetPosition(this);
        var delta = currentPosition - _compactPointerStart;
        if (Math.Abs(delta.X) < 6 && Math.Abs(delta.Y) < 6)
        {
            return;
        }

        _compactClickCandidate = false;
        CompactView.ReleaseMouseCapture();
        DragMove();
    }

    private void CompactView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (CompactView.IsMouseCaptured)
        {
            CompactView.ReleaseMouseCapture();
        }

        if (!_compactClickCandidate)
        {
            return;
        }

        _compactClickCandidate = false;
        _ = PlayAllAsync(fromCompactOrb: true);
    }

    private void CompactView_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        SetWindowMode(expanded: true);
    }

    private void HeaderDragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void BtnCollapse_Click(object sender, RoutedEventArgs e)
    {
        SetWindowMode(expanded: false);
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnOpenRecordEditor_Click(object sender, RoutedEventArgs e)
    {
        OpenRecordEditorWindow();
    }

    private void BtnOpenSchemeManager_Click(object sender, RoutedEventArgs e)
    {
        OpenSchemeManagerWindow();
    }

    private void SldOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded)
        {
            return;
        }

        Opacity = SldOpacity.Value;
    }

    private void BtnRecord_Click(object sender, RoutedEventArgs e)
    {
        ToggleRecording();
    }

    private async void BtnPlayAll_Click(object sender, RoutedEventArgs e)
    {
        await PlayAllAsync(fromCompactOrb: false);
    }

    private async void BtnLoopPlay_Click(object sender, RoutedEventArgs e)
    {
        await PlayLoopAsync();
    }

    private async Task PlayAllAsync(bool fromCompactOrb)
    {
        if (_currentSession == null || _currentSession.Actions.Count == 0)
        {
            if (!fromCompactOrb)
            {
                SetStatus("没有可播放的录制。");
            }

            return;
        }

        if (_recordingService.IsRecording || _playbackService.IsPlaying)
        {
            return;
        }

        _playbackService.LoadSession(_currentSession);
        await _playbackService.PlayAllAsync();
    }

    private async Task PlayLoopAsync()
    {
        if (_currentSession == null || _currentSession.Actions.Count == 0)
        {
            SetStatus("没有可循环播放的录制。");
            return;
        }

        if (_recordingService.IsRecording || _playbackService.IsPlaying)
        {
            return;
        }

        if (!TryParseLoopCount(out var loopCount, out var loopLabel, out var errorMessage))
        {
            SetStatus(errorMessage);
            return;
        }

        _loopPlaybackLabel = loopLabel;
        _playbackService.LoadSession(_currentSession);
        await _playbackService.PlayLoopAsync(loopCount);
    }

    private async void BtnStepPlay_Click(object sender, RoutedEventArgs e)
    {
        if (_currentSession == null || _currentSession.Actions.Count == 0)
        {
            SetStatus("没有可按步播放的录制。");
            return;
        }

        if (_recordingService.IsRecording || _playbackService.IsPlaying)
        {
            return;
        }

        if (_playbackService.TotalActions == 0 || _playbackService.CurrentStepIndex >= _playbackService.TotalActions)
        {
            _playbackService.LoadSession(_currentSession);
        }

        await _playbackService.StepNextAsync();
        UpdateStepStatus();
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e)
    {
        if (_recordingService.IsRecording)
        {
            StopRecording();
            return;
        }

        if (!_playbackService.IsPlaying)
        {
            return;
        }

        _preserveIdlePlaybackStatus = true;
        _playbackService.Stop();
        SetStatus("已停止播放。");
        UpdateButtonStates();
    }

    private void ToggleRecording()
    {
        if (_recordingService.IsRecording)
        {
            StopRecording();
            return;
        }

        StartRecording();
    }

    private void StartRecording()
    {
        _currentSession = _recordingService.StartRecording();
        RefreshActionList();
        UpdateButtonStates();
        SetStatus("开始录制。完成后会自动保存为当前方案。");
    }

    private void StopRecording()
    {
        _currentSession = _recordingService.StopRecording();
        if (_currentSession != null && _currentSession.Actions.Count > 0)
        {
            StepDefinitionService.Normalize(_currentSession);
            SaveCurrentSession();
            SetStatus($"录制完成，已保存 {_currentSession.Actions.Count} 条操作。");
        }
        else
        {
            SetStatus("录制已停止，但没有捕获到操作。");
        }

        RefreshActionList();
        UpdateButtonStates();
    }

    private void OnActionRecorded(RecordedMouseAction action)
    {
        _ = Dispatcher.InvokeAsync(() =>
        {
            RefreshActionList();
            UpdateButtonStates();
        });
    }

    private void OnRecordingStateChanged(bool isRecording)
    {
        _ = Dispatcher.InvokeAsync(() =>
        {
            BtnRecord.Style = (Style)FindResource(isRecording
                ? "RecordActionButtonActive"
                : "PrimaryActionButton");

            if (isRecording)
            {
                SetStatus("正在录制鼠标操作。");
            }

            UpdateButtonStates();
        });
    }

    private void OnPlaybackStateChanged(bool isPlaying)
    {
        _ = Dispatcher.InvokeAsync(() =>
        {
            BtnPlayAll.Style = (Style)FindResource(isPlaying
                ? "PlayActionButtonActive"
                : "PrimaryActionButton");
            BtnLoopPlay.Style = (Style)FindResource(isPlaying && _playbackService.IsLooping
                ? "PlayActionButtonActive"
                : "PrimaryActionButton");

            if (isPlaying)
            {
                _preserveIdlePlaybackStatus = false;
                SetStatus(_playbackService.IsLooping
                    ? $"开始循环播放当前方案（{_loopPlaybackLabel}）。"
                    : "开始执行录制内容。");
            }
            else if (!_preserveIdlePlaybackStatus)
            {
                UpdateStepStatus();
            }

            _preserveIdlePlaybackStatus = false;
            UpdateButtonStates();
        });
    }

    private void OnActionExecuting(int index, RecordedMouseAction action)
    {
        _ = Dispatcher.InvokeAsync(() =>
        {
            if (_currentSession == null)
            {
                return;
            }

            var stepNumber = StepDefinitionService.GetStepNumber(_currentSession.Actions, index);
            SetStatus($"正在执行第 {stepNumber}/{_playbackService.TotalSteps} 步：{action.ToDisplaySummary()}");
        });
    }

    private void OnPlaybackCompleted()
    {
        _ = Dispatcher.InvokeAsync(() =>
        {
            _preserveIdlePlaybackStatus = true;
            SetStatus(_playbackService.IsLooping
                ? $"循环播放完成（{_loopPlaybackLabel}）。"
                : "播放完成。");
            UpdateButtonStates();
        });
    }

    private void UpdateButtonStates()
    {
        var hasSession = _currentSession != null && _currentSession.Actions.Count > 0;
        var isRecording = _recordingService.IsRecording;
        var isPlaying = _playbackService.IsPlaying;

        BtnRecord.IsEnabled = !isPlaying;
        BtnPlayAll.IsEnabled = hasSession && !isRecording && !isPlaying;
        BtnLoopPlay.IsEnabled = hasSession && !isRecording && !isPlaying;
        BtnStepPlay.IsEnabled = hasSession && !isRecording && !isPlaying;
        BtnStop.IsEnabled = isRecording || isPlaying;

        BtnOpenRecordEditor.IsEnabled = hasSession;
        BtnOpenSchemeManager.IsEnabled = true;

        UpdateHintText();
    }

    private void UpdateStepStatus()
    {
        if (_currentSession == null || _currentSession.Actions.Count == 0)
        {
            SetStatus("准备就绪。");
            return;
        }

        var totalSteps = _playbackService.TotalSteps > 0
            ? _playbackService.TotalSteps
            : StepDefinitionService.GetStepCount(_currentSession);
        var completedSteps = _playbackService.CompletedSteps;

        SetStatus(completedSteps >= totalSteps
            ? "按步播放已完成。"
            : $"按步播放就绪，当前进度 {completedSteps}/{totalSteps} 步。");
    }

    private bool TryParseLoopCount(out int? loopCount, out string loopLabel, out string errorMessage)
    {
        var rawText = TxtLoopCount.Text?.Trim() ?? "*";
        if (string.IsNullOrWhiteSpace(rawText) || rawText == "*")
        {
            loopCount = null;
            loopLabel = "无限循环";
            errorMessage = string.Empty;
            return true;
        }

        if (!int.TryParse(rawText, out var parsedCount) || parsedCount < 1)
        {
            loopCount = null;
            loopLabel = string.Empty;
            errorMessage = "循环次数只能输入 1 以上的整数，或输入 * 表示无限循环。";
            return false;
        }

        loopCount = parsedCount;
        loopLabel = $"共 {parsedCount} 次";
        errorMessage = string.Empty;
        return true;
    }
}
