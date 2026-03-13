using ClickTool.Models;
using ClickTool.Services;
using System.Linq;

namespace ClickTool;

public partial class MainWindow
{
    private void RefreshActionList()
    {
        _actionGroups = StepDefinitionService.BuildActionGroups(_currentSession, _actionGroupExpansionStates);

        var actionCount = _currentSession?.Actions.Count ?? 0;
        var stepCount = StepDefinitionService.GetStepCount(_currentSession);

        TxtActionCount.Text = $"操作 {actionCount} 条";
        TxtStepCount.Text = $"步骤 {stepCount} 组";
        TxtSchemeSummary.Text = BuildSchemeSummaryText(actionCount, stepCount);

        UpdateHintText();
        RefreshRecordEditorWindow();
        RefreshSchemeManagerWindow();
    }

    private string BuildSchemeSummaryText(int actionCount, int stepCount)
    {
        if (_currentSession == null || actionCount == 0)
        {
            return "当前没有可执行的录制内容。点击“录制”后即可创建新方案。";
        }

        var createdAt = _currentSession.CreatedAt.ToString("yyyy-MM-dd HH:mm");
        return $"当前方案：{_currentSession.Name}\n操作总数：{actionCount} 条\n步骤数量：{stepCount} 组\n创建时间：{createdAt}";
    }

    private IReadOnlyList<int> GetSelectedActionIndices()
    {
        return _actionGroups
            .SelectMany(group => group.Actions)
            .Where(item => item.IsSelected)
            .Select(item => item.Index)
            .OrderBy(index => index)
            .ToList();
    }

    private void LoadFallbackSchemeIfAvailable()
    {
        var nextScheme = GetSchemeSummaries().FirstOrDefault();
        if (nextScheme != null)
        {
            TrySelectScheme(nextScheme.FilePath, out _);
            return;
        }

        _currentSession = null;
        RefreshActionList();
        UpdateButtonStates();
    }

    internal bool TryDeleteActions(IReadOnlyCollection<int> indices, out string message)
    {
        if (_currentSession == null)
        {
            message = "没有可编辑的录制。";
            return false;
        }

        if (indices.Count == 0)
        {
            message = "请先选择要删除的操作。";
            return false;
        }

        StepDefinitionService.DeleteActions(_currentSession, indices);
        _actionGroupExpansionStates.Clear();

        if (_currentSession.Actions.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(_currentSession.FilePath))
            {
                StorageService.DeleteRecording(_currentSession.FilePath);
            }

            message = "选中的操作已删除，当前方案为空。";
            LoadFallbackSchemeIfAvailable();
            SetStatus(message);
            return true;
        }

        SaveCurrentSession();
        message = $"已删除 {indices.Count} 条操作。";
        RefreshActionList();
        UpdateButtonStates();
        SetStatus(message);
        return true;
    }

    internal bool TryDeleteAllActions(out string message)
    {
        if (_currentSession == null || _currentSession.Actions.Count == 0)
        {
            message = "当前没有可清空的方案。";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(_currentSession.FilePath))
        {
            StorageService.DeleteRecording(_currentSession.FilePath);
        }

        _actionGroupExpansionStates.Clear();
        message = "已清空当前方案。";
        LoadFallbackSchemeIfAvailable();
        SetStatus(message);
        return true;
    }

    internal bool TryMergeActions(IReadOnlyCollection<int> indices, out string message)
    {
        if (_currentSession == null)
        {
            message = "没有可编辑的录制。";
            return false;
        }

        if (!StepDefinitionService.TryMergeSelectionIntoSingleStep(_currentSession, indices, out message))
        {
            SetStatus(message);
            return false;
        }

        _actionGroupExpansionStates.Clear();
        SaveCurrentSession();
        RefreshActionList();
        UpdateButtonStates();
        SetStatus(message);
        return true;
    }

    internal bool TrySplitActions(IReadOnlyCollection<int> indices, out string message)
    {
        if (_currentSession == null)
        {
            message = "没有可编辑的录制。";
            return false;
        }

        if (!StepDefinitionService.TrySplitSelectionIntoIndividualSteps(_currentSession, indices, out message))
        {
            SetStatus(message);
            return false;
        }

        _actionGroupExpansionStates.Clear();
        SaveCurrentSession();
        RefreshActionList();
        UpdateButtonStates();
        SetStatus(message);
        return true;
    }

    internal bool TryToggleStepBoundary(int index, out string message)
    {
        if (_currentSession == null)
        {
            message = "没有可编辑的录制。";
            return false;
        }

        StepDefinitionService.ToggleStepBoundary(_currentSession, index);
        _actionGroupExpansionStates.Clear();
        SaveCurrentSession();
        RefreshActionList();
        UpdateButtonStates();
        message = "已更新步尾定义。";
        SetStatus(message);
        return true;
    }

    internal bool TrySaveActionEdit(int actionIndex, int x, int y, long delayMs, out string message)
    {
        if (!RecordingEditService.TryUpdateAction(_currentSession, actionIndex, x, y, delayMs, out message))
        {
            SetStatus(message);
            return false;
        }

        SaveCurrentSession();
        RefreshActionList();
        UpdateButtonStates();
        SetStatus(message);
        return true;
    }

    internal bool TryDuplicateStep(int stepNumber, out string message)
    {
        if (!RecordingEditService.TryDuplicateStep(_currentSession, stepNumber, out message))
        {
            SetStatus(message);
            return false;
        }

        _actionGroupExpansionStates[stepNumber] = true;
        _actionGroupExpansionStates[stepNumber + 1] = true;
        SaveCurrentSession();
        RefreshActionList();
        UpdateButtonStates();
        SetStatus(message);
        return true;
    }
}
