using ClickTool.Models;
using ClickTool.Services;
using Xunit;

namespace ClickTool.Tests;

public class StepDefinitionServiceTests
{
    [Fact]
    public void GetStepCount_WithoutExplicitBoundaries_GroupsAllActionsIntoStepOne()
    {
        var session = new RecordingSession
        {
            Actions =
            [
                new MouseAction { Action = MouseActionType.Move, X = 1, Y = 1, DelayMs = 0, IsStepEnd = false },
                new MouseAction { Action = MouseActionType.Click, X = 2, Y = 2, DelayMs = 10, IsStepEnd = false },
                new MouseAction { Action = MouseActionType.Move, X = 3, Y = 3, DelayMs = 20, IsStepEnd = false }
            ]
        };

        var groups = StepDefinitionService.BuildActionGroups(session);

        Assert.Equal(1, StepDefinitionService.GetStepCount(session));
        Assert.Single(groups);
        Assert.Equal([0, 1, 2], groups[0].Actions.Select(item => item.Index).ToArray());
        Assert.All(groups[0].Actions, item => Assert.Equal(1, item.StepNumber));
        Assert.All(groups.SelectMany(group => group.Actions), item => Assert.False(item.Action.IsStepEnd));
    }

    [Fact]
    public void GetStepCount_WithExplicitBoundary_KeepsTrailingActionsInNextStep()
    {
        var session = new RecordingSession
        {
            Actions =
            [
                new MouseAction { Action = MouseActionType.Move, X = 1, Y = 1, DelayMs = 0, IsStepEnd = false },
                new MouseAction { Action = MouseActionType.Click, X = 2, Y = 2, DelayMs = 10, IsStepEnd = true },
                new MouseAction { Action = MouseActionType.Move, X = 3, Y = 3, DelayMs = 20, IsStepEnd = false },
                new MouseAction { Action = MouseActionType.Move, X = 4, Y = 4, DelayMs = 30, IsStepEnd = false }
            ]
        };

        var groups = StepDefinitionService.BuildActionGroups(session);

        Assert.Equal(2, StepDefinitionService.GetStepCount(session));
        Assert.Equal([0, 1], groups[0].Actions.Select(item => item.Index).ToArray());
        Assert.Equal([2, 3], groups[1].Actions.Select(item => item.Index).ToArray());
    }
}
