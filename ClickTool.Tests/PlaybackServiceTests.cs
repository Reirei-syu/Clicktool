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
}
