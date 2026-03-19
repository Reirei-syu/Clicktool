namespace ClickTool.Models;

public sealed class RecordedActionGroup
{
    public required int StepNumber { get; init; }

    public required List<RecordedActionListItem> Actions { get; init; }

    public bool IsExpanded { get; set; }

    public string StepLabel => $"步骤 {StepNumber}";

    public string CountLabel => $"{Actions.Count} 条操作";

    public string Summary =>
        Actions.Count == 1
            ? Actions[0].Summary
            : $"{Actions[0].Summary} 等 {Actions.Count} 条操作";
}
