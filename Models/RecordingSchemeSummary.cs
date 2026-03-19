namespace ClickTool.Models;

public sealed class RecordingSchemeSummary
{
    public required string FilePath { get; init; }

    public required string Name { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required int ActionCount { get; init; }

    public required int StepCount { get; init; }

    public bool IsCurrent { get; set; }

    public string DisplayName => $"{Name} · {ActionCount} 条 / {StepCount} 步";
}
