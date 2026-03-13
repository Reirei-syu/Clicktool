using ClickTool.Models;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClickTool;

public partial class SchemeManagerWindow : Window
{
    private readonly MainWindow _owner;
    private List<RecordingSchemeSummary> _schemes = new();

    public SchemeManagerWindow(MainWindow owner)
    {
        InitializeComponent();
        _owner = owner;
    }

    public void RefreshFromOwner()
    {
        _schemes = _owner.GetSchemeSummaries().ToList();
        LstSchemes.ItemsSource = _schemes;
        TxtEmptyState.Visibility = _schemes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        TxtCounts.Text = $"共 {_schemes.Count} 个方案";
        TxtStatus.Text = _owner.CurrentStatus;
        TxtSchemeName.Text = _owner.CurrentSchemeName == "未选择方案" ? string.Empty : _owner.CurrentSchemeName;
        UpdateButtonStates();
    }

    private void SchemeCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox || checkBox.DataContext is not RecordingSchemeSummary scheme)
        {
            return;
        }

        if (checkBox.IsChecked != true)
        {
            RefreshFromOwner();
            return;
        }

        _owner.TrySelectScheme(scheme.FilePath, out var message);
        TxtStatus.Text = message;
        RefreshFromOwner();
    }

    private void BtnRename_Click(object sender, RoutedEventArgs e)
    {
        _owner.TryRenameCurrentScheme(TxtSchemeName.Text, out var message);
        TxtStatus.Text = message;
        RefreshFromOwner();
    }

    private void BtnSaveAs_Click(object sender, RoutedEventArgs e)
    {
        _owner.TrySaveAsCurrentScheme(TxtSchemeName.Text, out var message);
        TxtStatus.Text = message;
        RefreshFromOwner();
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        var currentName = _owner.CurrentSchemeName;
        if (currentName == "未选择方案")
        {
            TxtStatus.Text = "没有可删除的方案。";
            return;
        }

        var result = MessageBox.Show(
            $"确定要删除方案“{currentName}”吗？",
            "删除方案",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        _owner.TryDeleteCurrentScheme(out var message);
        TxtStatus.Text = message;
        RefreshFromOwner();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void UpdateButtonStates()
    {
        var hasScheme = _schemes.Count > 0;
        BtnRename.IsEnabled = hasScheme;
        BtnSaveAs.IsEnabled = hasScheme;
        BtnDelete.IsEnabled = hasScheme;
    }
}
