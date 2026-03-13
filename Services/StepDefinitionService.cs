using ClickTool.Models;

namespace ClickTool.Services;

public static class StepDefinitionService
{
    public static void Normalize(RecordingSession? session)
    {
        if (session == null || session.Actions.Count == 0)
        {
            return;
        }

        if (!session.Actions.Any(action => action.IsStepEnd))
        {
            foreach (var action in session.Actions)
            {
                action.IsStepEnd = action.IsClickRelease;
            }
        }

        session.Actions[^1].IsStepEnd = true;
    }

    public static int GetStepCount(RecordingSession? session)
    {
        return session == null ? 0 : GetStepCount(session.Actions);
    }

    public static int GetStepCount(IReadOnlyList<MouseAction> actions)
    {
        if (actions.Count == 0)
        {
            return 0;
        }

        return actions.Count(action => action.IsStepEnd);
    }

    public static int GetCompletedStepCount(IReadOnlyList<MouseAction> actions, int nextActionIndex)
    {
        if (actions.Count == 0 || nextActionIndex <= 0)
        {
            return 0;
        }

        var upperBound = Math.Min(nextActionIndex, actions.Count);
        var completedSteps = 0;

        for (var index = 0; index < upperBound; index++)
        {
            if (actions[index].IsStepEnd)
            {
                completedSteps++;
            }
        }

        return completedSteps;
    }

    public static int GetStepNumber(IReadOnlyList<MouseAction> actions, int actionIndex)
    {
        if (actions.Count == 0)
        {
            return 0;
        }

        if (actionIndex <= 0)
        {
            return 1;
        }

        var normalizedIndex = Math.Min(actionIndex, actions.Count - 1);
        var stepNumber = 1;

        for (var index = 0; index < normalizedIndex; index++)
        {
            if (actions[index].IsStepEnd)
            {
                stepNumber++;
            }
        }

        return stepNumber;
    }

    public static List<RecordedActionListItem> BuildListItems(RecordingSession? session)
    {
        var items = new List<RecordedActionListItem>();
        if (session == null || session.Actions.Count == 0)
        {
            return items;
        }

        Normalize(session);

        var stepNumber = 1;
        for (var index = 0; index < session.Actions.Count; index++)
        {
            var action = session.Actions[index];
            items.Add(new RecordedActionListItem
            {
                Index = index,
                StepNumber = stepNumber,
                Action = action
            });

            if (action.IsStepEnd)
            {
                stepNumber++;
            }
        }

        return items;
    }

    public static List<RecordedActionGroup> BuildActionGroups(
        RecordingSession? session,
        IReadOnlyDictionary<int, bool>? expandedStates = null)
    {
        var groups = new List<RecordedActionGroup>();
        if (session == null || session.Actions.Count == 0)
        {
            return groups;
        }

        var items = BuildListItems(session);
        foreach (var groupItems in items.GroupBy(item => item.StepNumber))
        {
            var actionItems = groupItems.ToList();
            var defaultExpanded = actionItems.Count == 1;
            var isExpanded = expandedStates != null
                && expandedStates.TryGetValue(groupItems.Key, out var savedExpanded)
                    ? savedExpanded
                    : defaultExpanded;

            groups.Add(new RecordedActionGroup
            {
                StepNumber = groupItems.Key,
                Actions = actionItems,
                IsExpanded = isExpanded
            });
        }

        return groups;
    }

    public static void DeleteActions(RecordingSession session, IReadOnlyCollection<int> indices)
    {
        foreach (var index in indices.Distinct().OrderByDescending(index => index))
        {
            if (index >= 0 && index < session.Actions.Count)
            {
                session.Actions.RemoveAt(index);
            }
        }

        session.ResetPlayback();
        Normalize(session);
    }

    public static bool TryMergeSelectionIntoSingleStep(
        RecordingSession session,
        IReadOnlyCollection<int> rawIndices,
        out string message)
    {
        var indices = rawIndices.Distinct().OrderBy(index => index).ToArray();
        if (indices.Length == 0)
        {
            message = "请先选择要合并的操作。";
            return false;
        }

        if (!IsContiguous(indices))
        {
            message = "只能将连续的操作合并成一步。";
            return false;
        }

        Normalize(session);

        var start = indices[0];
        var end = indices[^1];

        if (start > 0)
        {
            session.Actions[start - 1].IsStepEnd = true;
        }

        for (var index = start; index <= end; index++)
        {
            session.Actions[index].IsStepEnd = false;
        }

        session.Actions[end].IsStepEnd = true;
        Normalize(session);
        session.ResetPlayback();

        message = $"已将 {indices.Length} 条操作合并为一步。";
        return true;
    }

    public static bool TrySplitSelectionIntoIndividualSteps(
        RecordingSession session,
        IReadOnlyCollection<int> rawIndices,
        out string message)
    {
        var indices = rawIndices.Distinct().OrderBy(index => index).ToArray();
        if (indices.Length == 0)
        {
            message = "请先选择要拆分的操作。";
            return false;
        }

        Normalize(session);

        var start = indices[0];
        if (start > 0)
        {
            session.Actions[start - 1].IsStepEnd = true;
        }

        foreach (var index in indices)
        {
            if (index >= 0 && index < session.Actions.Count)
            {
                session.Actions[index].IsStepEnd = true;
            }
        }

        Normalize(session);
        session.ResetPlayback();

        message = $"已将 {indices.Length} 条操作拆分为独立步骤。";
        return true;
    }

    public static void ToggleStepBoundary(RecordingSession session, int index)
    {
        if (index < 0 || index >= session.Actions.Count)
        {
            return;
        }

        Normalize(session);

        if (index == session.Actions.Count - 1)
        {
            session.Actions[index].IsStepEnd = true;
            return;
        }

        session.Actions[index].IsStepEnd = !session.Actions[index].IsStepEnd;
        Normalize(session);
        session.ResetPlayback();
    }

    private static bool IsContiguous(IReadOnlyList<int> indices)
    {
        for (var index = 1; index < indices.Count; index++)
        {
            if (indices[index] != indices[index - 1] + 1)
            {
                return false;
            }
        }

        return true;
    }
}
