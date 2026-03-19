using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ClickTool.Models;

public sealed class RecordedActionListItem : INotifyPropertyChanged
{
    private bool _isSelected;

    public required int Index { get; init; }

    public required int StepNumber { get; init; }

    public required MouseAction Action { get; init; }

    public string IndexLabel => $"#{Index + 1}";

    public string StepLabel => $"步骤 {StepNumber}";

    public string Summary => Action.ToDisplaySummary();

    public string DelayLabel => Action.ToDelayLabel();

    public bool IsStepEnd => Action.IsStepEnd;

    public bool UsesWarmSummaryColor => Action.Action != MouseActionType.Move;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public string BoundaryLabel => Action.IsStepEnd ? "步尾" : "步骤中";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
