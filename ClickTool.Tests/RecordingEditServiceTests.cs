using ClickTool.Models;
using ClickTool.Services;
using Xunit;

namespace ClickTool.Tests;

public class RecordingEditServiceTests
{
    [Fact]
    public void TryUpdateAction_UpdatesCoordinatesAndDelay()
    {
        var session = new RecordingSession
        {
            Actions =
            [
                new MouseAction
                {
                    Action = MouseActionType.Move,
                    X = 1,
                    Y = 2,
                    DelayMs = 3,
                    IsStepEnd = true
                }
            ]
        };

        var result = RecordingEditService.TryUpdateAction(session, 0, 100, 200, 300, out _);

        Assert.True(result);
        Assert.Equal(100, session.Actions[0].X);
        Assert.Equal(200, session.Actions[0].Y);
        Assert.Equal(300, session.Actions[0].DelayMs);
    }

    [Fact]
    public void TryDuplicateStep_DuplicatesWholeStepAfterSourceStep()
    {
        var session = new RecordingSession
        {
            Actions =
            [
                new MouseAction
                {
                    Action = MouseActionType.Move,
                    X = 1,
                    Y = 1,
                    DelayMs = 0,
                    IsStepEnd = false
                },
                new MouseAction
                {
                    Action = MouseActionType.Click,
                    X = 2,
                    Y = 2,
                    DelayMs = 10,
                    Button = MouseButton.Left,
                    State = MouseButtonState.Up,
                    IsStepEnd = true
                }
            ]
        };

        var result = RecordingEditService.TryDuplicateStep(session, 1, out _);

        Assert.True(result);
        Assert.Equal(4, session.Actions.Count);
        Assert.Equal(1, session.Actions[2].X);
        Assert.Equal(2, session.Actions[3].X);
        Assert.True(session.Actions[3].IsStepEnd);
    }
}
