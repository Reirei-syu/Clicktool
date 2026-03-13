using ClickTool.Models;

namespace ClickTool.Services;

public static class RecordingEditService
{
    public static bool TryUpdateAction(
        RecordingSession? session,
        int actionIndex,
        int x,
        int y,
        long delayMs,
        out string message)
    {
        if (session == null)
        {
            message = "没有可编辑的录制。";
            return false;
        }

        if (actionIndex < 0 || actionIndex >= session.Actions.Count)
        {
            message = "要编辑的操作不存在。";
            return false;
        }

        if (delayMs < 0)
        {
            message = "等待时间不能小于 0。";
            return false;
        }

        var action = session.Actions[actionIndex];
        action.X = x;
        action.Y = y;
        action.DelayMs = delayMs;

        message = "已保存该条记录的坐标和等待时间。";
        return true;
    }

    public static bool TryDuplicateStep(
        RecordingSession? session,
        int stepNumber,
        out string message)
    {
        if (session == null || session.Actions.Count == 0)
        {
            message = "没有可复制的步骤。";
            return false;
        }

        StepDefinitionService.Normalize(session);
        var groups = StepDefinitionService.BuildActionGroups(session);
        var targetGroup = groups.FirstOrDefault(group => group.StepNumber == stepNumber);
        if (targetGroup == null)
        {
            message = "未找到要复制的步骤。";
            return false;
        }

        var insertAt = targetGroup.Actions.Max(item => item.Index) + 1;
        var clonedActions = targetGroup.Actions
            .Select(item => CloneAction(item.Action))
            .ToList();

        session.Actions.InsertRange(insertAt, clonedActions);
        StepDefinitionService.Normalize(session);
        message = $"已复制步骤 {stepNumber}。";
        return true;
    }

    private static MouseAction CloneAction(MouseAction action)
    {
        return new MouseAction
        {
            Action = action.Action,
            X = action.X,
            Y = action.Y,
            DelayMs = action.DelayMs,
            Button = action.Button,
            State = action.State,
            IsStepEnd = action.IsStepEnd
        };
    }
}
