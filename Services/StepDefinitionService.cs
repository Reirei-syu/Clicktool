using ClickTool.Models;

namespace ClickTool.Services;

public static class StepDefinitionService
{
    public static void Normalize(RecordingSession? session)
    {
        _ = session;
    }

    public static int GetStepCount(RecordingSession? session)
    {
        return session == null ? 0 : GetStepCount(session.Actions);
    }

    public static int GetStepCount(IReadOnlyList<MouseAction> actions)
    {
        return BuildStepRanges(actions).Count;
    }

    public static int GetCompletedStepCount(IReadOnlyList<MouseAction> actions, int nextActionIndex)
    {
        if (actions.Count == 0 || nextActionIndex <= 0)
        {
            return 0;
        }

        var upperBoundExclusive = Math.Min(nextActionIndex, actions.Count);
        return BuildStepRanges(actions).Count(range => range.End < upperBoundExclusive);
    }

    public static int GetStepNumber(IReadOnlyList<MouseAction> actions, int actionIndex)
    {
        if (actions.Count == 0)
        {
            return 0;
        }

        var ranges = BuildStepRanges(actions);
        var normalizedIndex = Math.Max(0, Math.Min(actionIndex, actions.Count - 1));

        for (var stepIndex = 0; stepIndex < ranges.Count; stepIndex++)
        {
            var range = ranges[stepIndex];
            if (normalizedIndex >= range.Start && normalizedIndex <= range.End)
            {
                return stepIndex + 1;
            }
        }

        return ranges.Count;
    }

    public static bool IsEndOfStep(IReadOnlyList<MouseAction> actions, int actionIndex)
    {
        if (actionIndex < 0 || actionIndex >= actions.Count)
        {
            return false;
        }

        return BuildStepRanges(actions).Any(range => range.End == actionIndex);
    }

    public static List<RecordedActionListItem> BuildListItems(RecordingSession? session)
    {
        var items = new List<RecordedActionListItem>();
        if (session == null || session.Actions.Count == 0)
        {
            return items;
        }

        var ranges = BuildStepRanges(session.Actions);
        for (var stepIndex = 0; stepIndex < ranges.Count; stepIndex++)
        {
            var range = ranges[stepIndex];
            for (var index = range.Start; index <= range.End; index++)
            {
                items.Add(new RecordedActionListItem
                {
                    Index = index,
                    StepNumber = stepIndex + 1,
                    Action = session.Actions[index]
                });
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
        var groupedItems = items.GroupBy(item => item.StepNumber).ToList();
        var hasSingleGroup = groupedItems.Count == 1;

        foreach (var groupItems in groupedItems)
        {
            var actionItems = groupItems.ToList();
            var defaultExpanded = hasSingleGroup || actionItems.Count == 1;
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

        session.Actions[index].IsStepEnd = !session.Actions[index].IsStepEnd;
        session.ResetPlayback();
    }

    private static List<StepRange> BuildStepRanges(IReadOnlyList<MouseAction> actions)
    {
        var ranges = new List<StepRange>();
        if (actions.Count == 0)
        {
            return ranges;
        }

        var index = 0;
        while (index < actions.Count)
        {
            var explicitBoundary = FindNextExplicitBoundary(actions, index);
            if (explicitBoundary < 0)
            {
                ranges.Add(new StepRange(index, actions.Count - 1));
                break;
            }

            ranges.Add(new StepRange(index, explicitBoundary));
            index = explicitBoundary + 1;
        }

        return ranges;
    }

    private static int FindNextExplicitBoundary(IReadOnlyList<MouseAction> actions, int startIndex)
    {
        for (var index = startIndex; index < actions.Count; index++)
        {
            if (actions[index].IsStepEnd)
            {
                return index;
            }
        }

        return -1;
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

    private readonly record struct StepRange(int Start, int End);
}
