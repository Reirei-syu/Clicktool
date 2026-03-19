using ClickTool.Models;
using ClickTool.Services;
using Xunit;

namespace ClickTool.Tests;

public class PlaybackServiceTests
{
    [Fact]
    public async Task PlayLoopAsync_ReplaysUntilStopped()
    {
        var session = new RecordingSession
        {
            Actions =
            [
                new MouseAction
                {
                    Action = MouseActionType.Move,
                    X = 10,
                    Y = 20,
                    DelayMs = 0,
                    IsStepEnd = true
                }
            ]
        };

        var executedCount = 0;
        var playback = new PlaybackService();
        playback.ActionExecutor = action =>
        {
            executedCount++;
            if (executedCount >= 3)
            {
                playback.Stop();
            }
        };

        playback.LoadSession(session);
        await playback.PlayLoopAsync();

        Assert.True(executedCount >= 3);
        Assert.False(playback.IsLooping);
        Assert.False(playback.IsPlaying);
    }

    [Fact]
    public async Task PlayLoopAsync_WithFiniteCount_StopsAfterRequestedLoops()
    {
        var session = new RecordingSession
        {
            Actions =
            [
                new MouseAction
                {
                    Action = MouseActionType.Move,
                    X = 10,
                    Y = 20,
                    DelayMs = 0,
                    IsStepEnd = false
                }
            ]
        };

        var executedCount = 0;
        var playback = new PlaybackService
        {
            ActionExecutor = _ => executedCount++
        };

        playback.LoadSession(session);
        await playback.PlayLoopAsync(2);

        Assert.Equal(2, executedCount);
        Assert.False(playback.IsLooping);
        Assert.False(playback.IsPlaying);
    }

    [Fact]
    public async Task StepNextAsync_WithoutExplicitBoundaries_PlaysWholeDefaultStep()
    {
        var session = new RecordingSession
        {
            Actions =
            [
                new MouseAction
                {
                    Action = MouseActionType.Move,
                    X = 10,
                    Y = 20,
                    DelayMs = 0,
                    IsStepEnd = false
                },
                new MouseAction
                {
                    Action = MouseActionType.Click,
                    X = 11,
                    Y = 21,
                    DelayMs = 0,
                    Button = MouseButton.Left,
                    State = MouseButtonState.Up,
                    IsStepEnd = false
                }
            ]
        };

        var executed = new List<MouseAction>();
        var playback = new PlaybackService
        {
            ActionExecutor = action => executed.Add(action)
        };

        playback.LoadSession(session);

        var hasMore = await playback.StepNextAsync();

        Assert.False(hasMore);
        Assert.Equal(2, executed.Count);
        Assert.Equal(1, playback.CompletedSteps);
        Assert.Equal(1, playback.TotalSteps);
    }
}
