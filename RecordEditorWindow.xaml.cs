using ClickTool.Models;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ClickTool;

public partial class RecordEditorWindow : Window
{
    private readonly MainWindow _owner;
    private readonly Dictionary<int, bool> _groupExpansionStates = new();
    private List<RecordedActionGroup> _groups = new();

    public RecordEditorWindow(MainWindow owner)
    {
        InitializeComponent();
        _owner = owner;
    }

    public void RefreshFromOwner()
    {
        _groups = _owner.GetEditorGroups(_groupExpansionStates).ToList();
        LstActionGroups.ItemsSource = _groups;
        TxtEmptyState.Visibility = _groups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        TxtCounts.Text = $"操作 {_owner.CurrentActionCount} 条 · 步骤 {_owner.CurrentStepCount} 组";
        TxtStatus.Text = _owner.CurrentStatus;
        UpdateButtonStates();
    }

    private IReadOnlyList<int> GetSelectedIndices()
    {
        return _groups
            .SelectMany(group => group.Actions)
            .Where(item => item.IsSelected)
            .Select(item => item.Index)
            .OrderBy(index => index)
            .ToList();
    }

    private IReadOnlyList<int> GetSelectedStepNumbers()
    {
        return _groups
            .Where(group => group.Actions.Any(item => item.IsSelected))
            .Select(group => group.StepNumber)
            .OrderBy(stepNumber => stepNumber)
            .ToList();
    }

    private void ActionSelectionCheckBox_Click(object sender, RoutedEventArgs e)
    {
        UpdateButtonStates();
    }

    private void EditorGroup_Expanded(object sender, RoutedEventArgs e)
    {
        if (sender is Expander { DataContext: RecordedActionGroup group })
        {
            _groupExpansionStates[group.StepNumber] = true;
        }
    }

    private void EditorGroup_Collapsed(object sender, RoutedEventArgs e)
    {
        if (sender is Expander { DataContext: RecordedActionGroup group })
        {
            _groupExpansionStates[group.StepNumber] = false;
        }
    }

    private void BtnCopyStep_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not int stepNumber)
        {
            return;
        }

        _owner.TryDuplicateStep(stepNumber, out var message);
        TxtStatus.Text = message;
        RefreshFromOwner();
    }

    private void BtnCopySelectedSteps_Click(object sender, RoutedEventArgs e)
    {
        _owner.TryDuplicateSteps(GetSelectedStepNumbers(), out var message);
        TxtStatus.Text = message;
        RefreshFromOwner();
    }

    private void BtnApplyBatchDelay_Click(object sender, RoutedEventArgs e)
    {
        if (!long.TryParse(TxtBatchDelayMs.Text?.Trim(), out var delayMs) || delayMs < 0)
        {
            TxtStatus.Text = "批量等待时间只能输入 0 以上的整数。";
            return;
        }

        _owner.TryApplyDelayToActions(GetSelectedIndices(), delayMs, out var message);
        TxtStatus.Text = message;
        RefreshFromOwner();
    }

    private void BtnSaveActionEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not int actionIndex)
        {
            return;
        }

        var item = _groups
            .SelectMany(group => group.Actions)
            .FirstOrDefault(action => action.Index == actionIndex);

        if (item == null)
        {
            return;
        }

        _owner.TrySaveActionEdit(actionIndex, item.Action.X, item.Action.Y, item.Action.DelayMs, out var message);
        TxtStatus.Text = message;
        RefreshFromOwner();
    }

    private void BtnMerge_Click(object sender, RoutedEventArgs e)
    {
        _owner.TryMergeActions(GetSelectedIndices(), out var message);
        TxtStatus.Text = message;
        RefreshFromOwner();
    }

    private void BtnSplit_Click(object sender, RoutedEventArgs e)
    {
        _owner.TrySplitActions(GetSelectedIndices(), out var message);
        TxtStatus.Text = message;
        RefreshFromOwner();
    }

    private void BtnDeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        _owner.TryDeleteActions(GetSelectedIndices(), out var message);
        TxtStatus.Text = message;
        RefreshFromOwner();
    }

    private void BtnDeleteAll_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "确定要删除当前录制中的全部操作吗？",
            "清空当前录制",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        _owner.TryDeleteAllActions(out var message);
        TxtStatus.Text = message;
        RefreshFromOwner();
    }

    private void ActionDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not int index)
        {
            return;
        }

        _owner.TryDeleteActions(new[] { index }, out var message);
        TxtStatus.Text = message;
        RefreshFromOwner();
    }

    private void ActionStepBoundary_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not int index)
        {
            return;
        }

        _owner.TryToggleStepBoundary(index, out var message);
        TxtStatus.Text = message;
        RefreshFromOwner();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void UpdateButtonStates()
    {
        var hasItems = _owner.CurrentActionCount > 0;
        var hasSelection = GetSelectedIndices().Count > 0;
        var hasSelectedSteps = GetSelectedStepNumbers().Count > 0;

        BtnMerge.IsEnabled = hasSelection;
        BtnSplit.IsEnabled = hasSelection;
        BtnCopySelectedSteps.IsEnabled = hasSelectedSteps;
        BtnApplyBatchDelay.IsEnabled = hasSelection;
        BtnDeleteSelected.IsEnabled = hasSelection;
        BtnDeleteAll.IsEnabled = hasItems;
    }
}
