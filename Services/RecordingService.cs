using ClickTool.Models;
using System.Diagnostics;

namespace ClickTool.Services;

public class RecordingService : IDisposable
{
    private readonly GlobalMouseHook _hook;
    private readonly Stopwatch _stopwatch = new();
    private RecordingSession? _currentSession;
    private long _lastMoveTimestamp;
    private bool _isRecording;
    private bool _disposed;

    public int MoveSampleIntervalMs { get; set; } = 50;

    public bool RecordMouseMove { get; set; } = true;

    public bool IsRecording => _isRecording;

    public event Action<MouseAction>? OnActionRecorded;

    public event Action<bool>? OnRecordingStateChanged;

    public RecordingService()
    {
        _hook = new GlobalMouseHook();
        _hook.OnMouseMove += HandleMouseMove;
        _hook.OnMouseDown += HandleMouseDown;
        _hook.OnMouseUp += HandleMouseUp;
    }

    public RecordingSession StartRecording()
    {
        if (_isRecording)
        {
            throw new InvalidOperationException("当前已经在录制。");
        }

        _currentSession = new RecordingSession
        {
            Name = $"录制_{DateTime.Now:yyyyMMdd_HHmmss}",
            CreatedAt = DateTime.Now
        };

        _stopwatch.Restart();
        _lastMoveTimestamp = 0;
        _isRecording = true;

        _hook.Install();
        OnRecordingStateChanged?.Invoke(true);

        return _currentSession;
    }

    public RecordingSession? StopRecording()
    {
        if (!_isRecording)
        {
            return null;
        }

        _hook.Uninstall();
        _stopwatch.Stop();
        _isRecording = false;

        var session = _currentSession;
        OnRecordingStateChanged?.Invoke(false);

        return session;
    }

    private void HandleMouseMove(int x, int y)
    {
        if (!_isRecording || !RecordMouseMove)
        {
            return;
        }

        var now = _stopwatch.ElapsedMilliseconds;
        if (now - _lastMoveTimestamp < MoveSampleIntervalMs)
        {
            return;
        }

        var delay = _currentSession!.Actions.Count > 0
            ? now - _currentSession.Actions[^1].TimestampMs
            : 0;

        var action = new MouseAction
        {
            Action = MouseActionType.Move,
            X = x,
            Y = y,
            DelayMs = delay,
            TimestampMs = now
        };

        _currentSession.Actions.Add(action);
        _lastMoveTimestamp = now;
        OnActionRecorded?.Invoke(action);
    }

    private void HandleMouseDown(int x, int y, MouseButton button)
    {
        if (!_isRecording)
        {
            return;
        }

        RecordClickAction(x, y, button, MouseButtonState.Down);
    }

    private void HandleMouseUp(int x, int y, MouseButton button)
    {
        if (!_isRecording)
        {
            return;
        }

        RecordClickAction(x, y, button, MouseButtonState.Up);
    }

    private void RecordClickAction(int x, int y, MouseButton button, MouseButtonState state)
    {
        var now = _stopwatch.ElapsedMilliseconds;
        var delay = _currentSession!.Actions.Count > 0
            ? now - _currentSession.Actions[^1].TimestampMs
            : 0;

        var action = new MouseAction
        {
            Action = MouseActionType.Click,
            X = x,
            Y = y,
            Button = button,
            State = state,
            IsStepEnd = false,
            DelayMs = delay,
            TimestampMs = now
        };

        _currentSession.Actions.Add(action);
        OnActionRecorded?.Invoke(action);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_isRecording)
        {
            StopRecording();
        }

        _hook.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
