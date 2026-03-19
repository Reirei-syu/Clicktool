using ClickTool.Models;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ClickTool;

public partial class RecordEditorWindow : Window
{
    private readonly MainWindow _owner;
    private readonly Dictionary<int, bool> _groupExpansionStates = new();
    private readonly Dictionary<int, FrameworkElement> _actionRowElements = new();
    private List<RecordedActionGroup> _groups = new();
    private int? _selectionAnchorIndex;
    private bool _isDragSelecting;
    private bool _dragSelectionValue;
    private bool _isBoxSelecting;
    private Point _selectionStartPoint;

    public RecordEditorWindow(MainWindow owner)
    {
        InitializeComponent();
        _owner = owner;
    }

    public void RefreshFromOwner()
    {
        _actionRowElements.Clear();
        _groups = _owner.GetEditorGroups(_groupExpansionStates).ToList();
        SyncSelectionAnchor();
        _isDragSelecting = false;
        _isBoxSelecting = false;
        HideSelectionRectangle();
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
        var item = GetActionListItem(sender);
        if (item == null)
        {
            return;
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) &&
            _selectionAnchorIndex.HasValue &&
            _selectionAnchorIndex.Value != item.Index)
        {
            ApplySelectionRange(_selectionAnchorIndex.Value, item.Index, item.IsSelected);
        }

        _selectionAnchorIndex = item.Index;
        UpdateButtonStates();
    }

    private void ActionSelectionCheckBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var item = GetActionListItem(sender);
        if (item == null)
        {
            return;
        }

        _isDragSelecting = true;
        _dragSelectionValue = !item.IsSelected;
    }

    private void ActionSelectionCheckBox_MouseEnter(object sender, MouseEventArgs e)
    {
        HandleSelectionDrag(sender, e);
    }

    private void SelectionSurface_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_groups.Count == 0 || e.OriginalSource is not DependencyObject source)
        {
            return;
        }

        if (IsSelectionBlockedByInteractiveElement(source))
        {
            return;
        }

        _isBoxSelecting = true;
        _selectionStartPoint = e.GetPosition(SelectionSurface);
        ShowSelectionRectangle(_selectionStartPoint, _selectionStartPoint);
        SelectionSurface.CaptureMouse();
        e.Handled = true;
    }

    private void SelectionSurface_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isBoxSelecting)
        {
            return;
        }

        ShowSelectionRectangle(_selectionStartPoint, e.GetPosition(SelectionSurface));
        e.Handled = true;
    }

    private void SelectionSurface_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isBoxSelecting)
        {
            return;
        }

        var selectionRect = CreateSelectionRect(_selectionStartPoint, e.GetPosition(SelectionSurface));
        CompleteBoxSelection(selectionRect);
        e.Handled = true;
    }

    private void SelectionSurface_LostMouseCapture(object sender, MouseEventArgs e)
    {
        if (!_isBoxSelecting)
        {
            return;
        }

        _isBoxSelecting = false;
        HideSelectionRectangle();
    }

    private void ActionSelectionRow_MouseEnter(object sender, MouseEventArgs e)
    {
        HandleSelectionDrag(sender, e);
    }

    private void ActionSelectionRow_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element &&
            element.Tag is int index)
        {
            _actionRowElements[index] = element;
        }
    }

    private void ActionSelectionRow_Unloaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element &&
            element.Tag is int index)
        {
            _actionRowElements.Remove(index);
        }
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

    private void Window_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragSelecting = false;
    }

    private RecordedActionListItem? GetActionListItem(object sender)
    {
        return sender is FrameworkElement { DataContext: RecordedActionListItem item }
            ? item
            : null;
    }

    private void ApplySelectionRange(int startIndex, int endIndex, bool isSelected)
    {
        var rangeStart = Math.Min(startIndex, endIndex);
        var rangeEnd = Math.Max(startIndex, endIndex);

        foreach (var item in _groups
                     .SelectMany(group => group.Actions)
                     .Where(action => action.Index >= rangeStart && action.Index <= rangeEnd))
        {
            item.IsSelected = isSelected;
        }
    }

    private void CompleteBoxSelection(Rect selectionRect)
    {
        _isBoxSelecting = false;

        if (SelectionSurface.IsMouseCaptured)
        {
            SelectionSurface.ReleaseMouseCapture();
        }

        HideSelectionRectangle();

        if (selectionRect.Width < 4 && selectionRect.Height < 4)
        {
            return;
        }

        var intersectedIndices = _actionRowElements
            .Where(pair => DoesRowIntersectSelection(pair.Value, selectionRect))
            .Select(pair => pair.Key)
            .ToHashSet();

        foreach (var item in _groups.SelectMany(group => group.Actions))
        {
            item.IsSelected = intersectedIndices.Contains(item.Index);
        }

        UpdateButtonStates();
    }

    private bool DoesRowIntersectSelection(FrameworkElement element, Rect selectionRect)
    {
        if (!element.IsLoaded || element.ActualWidth <= 0 || element.ActualHeight <= 0)
        {
            return false;
        }

        try
        {
            var topLeft = element.TranslatePoint(new Point(0, 0), SelectionSurface);
            var rowRect = new Rect(topLeft, new Size(element.ActualWidth, element.ActualHeight));
            return rowRect.IntersectsWith(selectionRect);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private bool IsSelectionBlockedByInteractiveElement(DependencyObject source)
    {
        for (DependencyObject? current = source; current != null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is ButtonBase or TextBox or CheckBox or ScrollBar)
            {
                return true;
            }
        }

        return false;
    }

    private void ShowSelectionRectangle(Point startPoint, Point endPoint)
    {
        var rect = CreateSelectionRect(startPoint, endPoint);
        Canvas.SetLeft(SelectionRectangle, rect.X);
        Canvas.SetTop(SelectionRectangle, rect.Y);
        SelectionRectangle.Width = rect.Width;
        SelectionRectangle.Height = rect.Height;
        SelectionRectangle.Visibility = Visibility.Visible;
    }

    private void HideSelectionRectangle()
    {
        SelectionRectangle.Visibility = Visibility.Collapsed;
        SelectionRectangle.Width = 0;
        SelectionRectangle.Height = 0;
    }

    private static Rect CreateSelectionRect(Point startPoint, Point endPoint)
    {
        return new Rect(
            Math.Min(startPoint.X, endPoint.X),
            Math.Min(startPoint.Y, endPoint.Y),
            Math.Abs(endPoint.X - startPoint.X),
            Math.Abs(endPoint.Y - startPoint.Y));
    }

    private void HandleSelectionDrag(object sender, MouseEventArgs e)
    {
        if (!_isDragSelecting)
        {
            return;
        }

        if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
        {
            _isDragSelecting = false;
            return;
        }

        var item = GetActionListItem(sender);
        if (item == null || item.IsSelected == _dragSelectionValue)
        {
            return;
        }

        item.IsSelected = _dragSelectionValue;
        UpdateButtonStates();
    }

    private void SyncSelectionAnchor()
    {
        if (!_selectionAnchorIndex.HasValue)
        {
            return;
        }

        var anchorExists = _groups
            .SelectMany(group => group.Actions)
            .Any(item => item.Index == _selectionAnchorIndex.Value);

        if (!anchorExists)
        {
            _selectionAnchorIndex = null;
        }
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
