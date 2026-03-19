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

    public static bool TryUpdateDelayForActions(
        RecordingSession? session,
        IReadOnlyCollection<int> rawIndices,
        long delayMs,
        out string message)
    {
        if (session == null)
        {
            message = "没有可编辑的录制。";
            return false;
        }

        if (delayMs < 0)
        {
            message = "等待时间不能小于 0。";
            return false;
        }

        var indices = rawIndices
            .Distinct()
            .Where(index => index >= 0 && index < session.Actions.Count)
            .OrderBy(index => index)
            .ToArray();

        if (indices.Length == 0)
        {
            message = "请先选择要修改等待时间的记录。";
            return false;
        }

        foreach (var index in indices)
        {
            session.Actions[index].DelayMs = delayMs;
        }

        message = $"已将 {indices.Length} 条记录的等待时间改为 {delayMs} ms。";
        return true;
    }

    public static bool TryDuplicateStep(
        RecordingSession? session,
        int stepNumber,
        out string message)
    {
        return TryDuplicateSteps(session, new[] { stepNumber }, out message);
    }

    public static bool TryDuplicateSteps(
        RecordingSession? session,
        IReadOnlyCollection<int> rawStepNumbers,
        out string message)
    {
        if (session == null || session.Actions.Count == 0)
        {
            message = "没有可复制的步骤。";
            return false;
        }

        var stepNumbers = rawStepNumbers.Distinct().OrderBy(number => number).ToArray();
        if (stepNumbers.Length == 0)
        {
            message = "请先选择要复制的步骤。";
            return false;
        }

        var groups = StepDefinitionService.BuildActionGroups(session);
        var selectedGroups = groups
            .Where(group => stepNumbers.Contains(group.StepNumber))
            .OrderBy(group => group.StepNumber)
            .ToList();

        if (selectedGroups.Count == 0)
        {
            message = "未找到要复制的步骤。";
            return false;
        }

        var offset = 0;
        foreach (var group in selectedGroups)
        {
            var insertAt = group.Actions.Max(item => item.Index) + 1 + offset;
            var clonedActions = group.Actions
                .Select(item => CloneAction(item.Action))
                .ToList();

            session.Actions.InsertRange(insertAt, clonedActions);
            offset += clonedActions.Count;
        }

        message = selectedGroups.Count == 1
            ? $"已复制步骤 {selectedGroups[0].StepNumber}。"
            : $"已批量复制 {selectedGroups.Count} 个步骤。";
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
