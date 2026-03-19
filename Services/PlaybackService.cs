using ClickTool.Models;

namespace ClickTool.Services;

public class PlaybackService
{
    private RecordingSession? _session;
    private CancellationTokenSource? _cts;
    private bool _isPlaying;
    private bool _isLooping;

    public bool IsPlaying => _isPlaying;

    public bool IsLooping => _isLooping;

    public int CurrentStepIndex => _session?.CurrentStepIndex ?? 0;

    public int TotalActions => _session?.Actions.Count ?? 0;

    public int TotalSteps => StepDefinitionService.GetStepCount(_session);

    public int CompletedSteps =>
        _session == null
            ? 0
            : StepDefinitionService.GetCompletedStepCount(_session.Actions, _session.CurrentStepIndex);

    public Action<MouseAction> ActionExecutor { get; set; } = MouseSimulator.ExecuteAction;

    public event Action<bool>? OnPlaybackStateChanged;

    public event Action<int, MouseAction>? OnActionExecuting;

    public event Action? OnPlaybackCompleted;

    public void LoadSession(RecordingSession session)
    {
        _session = session;
        StepDefinitionService.Normalize(_session);
        _session.ResetPlayback();
    }

    public async Task PlayAllAsync()
    {
        if (_session == null || _session.Actions.Count == 0 || _isPlaying)
        {
            return;
        }

        _isLooping = false;
        await ExecutePlaybackAsync(loop: false);
    }

    public async Task PlayLoopAsync(int? loopCount = null)
    {
        if (_session == null || _session.Actions.Count == 0 || _isPlaying)
        {
            return;
        }

        if (loopCount.HasValue && loopCount.Value < 1)
        {
            return;
        }

        _isLooping = true;
        await ExecutePlaybackAsync(loop: true, loopCount);
    }

    public async Task<bool> StepNextAsync()
    {
        if (_session == null || _session.IsCompleted || _isPlaying)
        {
            return false;
        }

        _isPlaying = true;
        _isLooping = false;
        StepDefinitionService.Normalize(_session);
        _cts = new CancellationTokenSource();
        OnPlaybackStateChanged?.Invoke(true);

        try
        {
            while (_session.CurrentStepIndex < _session.Actions.Count)
            {
                _cts.Token.ThrowIfCancellationRequested();

                var index = _session.CurrentStepIndex;
                var action = _session.Actions[index];

                OnActionExecuting?.Invoke(index, action);

                if (action.DelayMs > 0)
                {
                    await Task.Delay((int)action.DelayMs, _cts.Token);
                }

                ActionExecutor(action);
                _session.CurrentStepIndex++;

                if (StepDefinitionService.IsEndOfStep(_session.Actions, index))
                {
                    break;
                }
            }

            if (_session.IsCompleted)
            {
                OnPlaybackCompleted?.Invoke();
            }

            return !_session.IsCompleted;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        finally
        {
            _isPlaying = false;
            OnPlaybackStateChanged?.Invoke(false);
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        _isLooping = false;
        _isPlaying = false;
    }

    public void Reset()
    {
        Stop();
        _session?.ResetPlayback();
    }

    private async Task ExecutePlaybackAsync(bool loop, int? loopCount = null)
    {
        _isPlaying = true;
        StepDefinitionService.Normalize(_session);
        _session!.ResetPlayback();
        _cts = new CancellationTokenSource();
        OnPlaybackStateChanged?.Invoke(true);
        var completedLoops = 0;

        try
        {
            do
            {
                _session.ResetPlayback();

                for (var index = 0; index < _session.Actions.Count; index++)
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    var action = _session.Actions[index];
                    _session.CurrentStepIndex = index;
                    OnActionExecuting?.Invoke(index, action);

                    if (action.DelayMs > 0)
                    {
                        await Task.Delay((int)action.DelayMs, _cts.Token);
                    }

                    ActionExecutor(action);
                }

                _session.CurrentStepIndex = _session.Actions.Count;
                completedLoops++;

                var hasReachedLoopTarget = loop && loopCount.HasValue && completedLoops >= loopCount.Value;
                if (!loop || hasReachedLoopTarget)
                {
                    OnPlaybackCompleted?.Invoke();
                    break;
                }
            }
            while (loop && !_cts.Token.IsCancellationRequested);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _isPlaying = false;
            _isLooping = false;
            OnPlaybackStateChanged?.Invoke(false);
        }
    }
}
