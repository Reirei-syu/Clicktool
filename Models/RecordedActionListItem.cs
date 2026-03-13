namespace ClickTool.Models;

public sealed class RecordedActionListItem
{
    public required int Index { get; init; }

    public required int StepNumber { get; init; }

    public required MouseAction Action { get; init; }

    public string IndexLabel => $"#{Index + 1}";

    public string StepLabel => $"步骤 {StepNumber}";

    public string Summary => Action.ToDisplaySummary();

    public string DelayLabel => Action.ToDelayLabel();

    public bool IsStepEnd => Action.IsStepEnd;

    public bool IsSelected { get; set; }

    public string BoundaryLabel => Action.IsStepEnd ? "步尾" : "步骤中";
}
