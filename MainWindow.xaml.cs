using ClickTool.Models;
using ClickTool.Services;
using System.Windows;
using System.Windows.Input;

namespace ClickTool;

public partial class MainWindow : Window
{
    private const double CompactWindowSize = 92;
    private const double ExpandedWindowWidth = 468;
    private const double ExpandedWindowHeight = 860;

    private readonly RecordingService _recordingService;
    private readonly PlaybackService _playbackService;
    private AppSettings _settings;
    private RecordingSession? _currentSession;
    private RecordEditorWindow? _recordEditorWindow;
    private SchemeManagerWindow? _schemeManagerWindow;
    private List<RecordedActionGroup> _actionGroups = new();
    private readonly Dictionary<int, bool> _actionGroupExpansionStates = new();

    private bool _compactClickCandidate;
    private Point _compactPointerStart;
    private bool _preserveIdlePlaybackStatus;
    private bool _isBottomLayer;
    private string _loopPlaybackLabel = "无限循环";

    public MainWindow()
    {
        InitializeComponent();

        _settings = StorageService.LoadSettings();
        _recordingService = new RecordingService
        {
            RecordMouseMove = _settings.RecordMouseMove,
            MoveSampleIntervalMs = _settings.MoveSampleIntervalMs
        };
        _playbackService = new PlaybackService();

        _recordingService.OnActionRecorded += OnActionRecorded;
        _recordingService.OnRecordingStateChanged += OnRecordingStateChanged;
        _playbackService.OnPlaybackStateChanged += OnPlaybackStateChanged;
        _playbackService.OnActionExecuting += OnActionExecuting;
        _playbackService.OnPlaybackCompleted += OnPlaybackCompleted;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Left = _settings.WindowLeft;
        Top = _settings.WindowTop;
        Opacity = _settings.Opacity;
        SldOpacity.Value = _settings.Opacity;
        TxtLoopCount.Text = "*";

        SetWindowMode(expanded: false);
        EnsureWindowOnScreen();
        UpdateHotkeyTextBoxes();
        InitializeSchemes();
        RegisterHotKeys(showFailureStatus: true);
        UpdateButtonStates();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _settings.WindowLeft = Left;
        _settings.WindowTop = Top;
        _settings.Opacity = Opacity;
        StorageService.SaveSettings(_settings);

        UnregisterHotKeys();
        _recordingService.Dispose();
        _playbackService.Stop();
    }

    private void InitializeSchemes()
    {
        var schemes = GetSchemeSummaries();
        if (schemes.Count > 0)
        {
            TrySelectScheme(schemes[0].FilePath, out _);
            return;
        }

        _currentSession = null;
        RefreshActionList();
        SetStatus("准备就绪。");
    }

    private void SetWindowMode(bool expanded)
    {
        Width = expanded ? ExpandedWindowWidth : CompactWindowSize;
        Height = expanded ? ExpandedWindowHeight : CompactWindowSize;
        CompactView.Visibility = expanded ? Visibility.Collapsed : Visibility.Visible;
        ExpandedView.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
        EnsureWindowOnScreen();
    }

    private void EnsureWindowOnScreen()
    {
        if (Left < SystemParameters.VirtualScreenLeft)
        {
            Left = SystemParameters.VirtualScreenLeft;
        }

        if (Top < SystemParameters.VirtualScreenTop)
        {
            Top = SystemParameters.VirtualScreenTop;
        }

        var maxLeft = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Width;
        var maxTop = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - Height;

        if (Left > maxLeft)
        {
            Left = maxLeft;
        }

        if (Top > maxTop)
        {
            Top = maxTop;
        }
    }

    private void SaveCurrentSession()
    {
        if (_currentSession == null)
        {
            return;
        }

        StorageService.SaveRecording(_currentSession);
        RefreshSchemeManagerWindow();
    }

    internal IReadOnlyList<RecordedActionGroup> GetEditorGroups(IReadOnlyDictionary<int, bool>? expandedStates)
    {
        return StepDefinitionService.BuildActionGroups(_currentSession, expandedStates);
    }

    internal IReadOnlyList<RecordingSchemeSummary> GetSchemeSummaries()
    {
        var schemes = StorageService.GetRecordingSchemes();
        var currentFilePath = _currentSession?.FilePath;

        foreach (var scheme in schemes)
        {
            scheme.IsCurrent = !string.IsNullOrWhiteSpace(currentFilePath) && scheme.FilePath == currentFilePath;
        }

        return schemes;
    }

    internal bool TrySelectScheme(string filePath, out string message)
    {
        var session = StorageService.LoadRecording(filePath);
        if (session == null)
        {
            message = "方案加载失败。";
            return false;
        }

        _currentSession = session;
        _actionGroupExpansionStates.Clear();
        StepDefinitionService.Normalize(_currentSession);
        StorageService.SaveRecording(_currentSession);
        RefreshActionList();
        UpdateButtonStates();
        message = $"已切换到方案“{_currentSession.Name}”。";
        SetStatus(message);
        return true;
    }

    internal bool TryRenameCurrentScheme(string newName, out string message)
    {
        if (_currentSession == null)
        {
            message = "没有可重命名的方案。";
            return false;
        }

        var trimmedName = newName.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            message = "方案名称不能为空。";
            return false;
        }

        _currentSession.Name = trimmedName;
        SaveCurrentSession();
        RefreshActionList();
        UpdateButtonStates();
        message = $"方案已重命名为“{trimmedName}”。";
        SetStatus(message);
        return true;
    }

    internal bool TrySaveAsCurrentScheme(string newName, out string message)
    {
        if (_currentSession == null || _currentSession.Actions.Count == 0)
        {
            message = "没有可另存的方案内容。";
            return false;
        }

        var trimmedName = string.IsNullOrWhiteSpace(newName)
            ? $"方案_{DateTime.Now:yyyyMMdd_HHmmss}"
            : newName.Trim();

        _currentSession = StorageService.SaveRecordingAsNewScheme(_currentSession, trimmedName);
        _actionGroupExpansionStates.Clear();
        RefreshActionList();
        UpdateButtonStates();
        message = $"已另存为方案“{trimmedName}”。";
        SetStatus(message);
        return true;
    }

    internal bool TryDeleteCurrentScheme(out string message)
    {
        if (_currentSession == null || string.IsNullOrWhiteSpace(_currentSession.FilePath))
        {
            message = "没有可删除的方案。";
            return false;
        }

        var deletedName = _currentSession.Name;
        StorageService.DeleteRecording(_currentSession.FilePath);
        _actionGroupExpansionStates.Clear();

        var nextScheme = GetSchemeSummaries().FirstOrDefault();
        if (nextScheme != null)
        {
            TrySelectScheme(nextScheme.FilePath, out _);
        }
        else
        {
            _currentSession = null;
            RefreshActionList();
            UpdateButtonStates();
        }

        message = $"已删除方案“{deletedName}”。";
        SetStatus(message);
        return true;
    }

    internal string CurrentSchemeName => _currentSession?.Name ?? "未选择方案";

    internal int CurrentActionCount => _currentSession?.Actions.Count ?? 0;

    internal int CurrentStepCount => StepDefinitionService.GetStepCount(_currentSession);

    internal string CurrentStatus => TxtStatus.Text;

    private void OpenRecordEditorWindow()
    {
        if (_recordEditorWindow == null || !_recordEditorWindow.IsLoaded)
        {
            _recordEditorWindow = new RecordEditorWindow(this)
            {
                Owner = this
            };
            _recordEditorWindow.Closed += (_, _) => _recordEditorWindow = null;
            _recordEditorWindow.Show();
        }
        else
        {
            _recordEditorWindow.Activate();
        }

        _recordEditorWindow.RefreshFromOwner();
    }

    private void OpenSchemeManagerWindow()
    {
        if (_schemeManagerWindow == null || !_schemeManagerWindow.IsLoaded)
        {
            _schemeManagerWindow = new SchemeManagerWindow(this)
            {
                Owner = this
            };
            _schemeManagerWindow.Closed += (_, _) => _schemeManagerWindow = null;
            _schemeManagerWindow.Show();
        }
        else
        {
            _schemeManagerWindow.Activate();
        }

        _schemeManagerWindow.RefreshFromOwner();
    }

    private void RefreshRecordEditorWindow()
    {
        _recordEditorWindow?.RefreshFromOwner();
    }

    private void RefreshSchemeManagerWindow()
    {
        _schemeManagerWindow?.RefreshFromOwner();
    }

    private void SetStatus(string message)
    {
        TxtStatus.Text = message;
        RefreshRecordEditorWindow();
        RefreshSchemeManagerWindow();
    }

    private void UpdateHintText()
    {
        if (_recordingService.IsRecording)
        {
            TxtHint.Text = "正在录制。建议使用热键停止，避免把面板操作一起录进去。";
            return;
        }

        if (_currentSession?.Actions.Count > 0)
        {
            TxtHint.Text = "方案切换、重命名、另存和删除请在“打开方案列表”窗口中完成。";
            return;
        }

        TxtHint.Text = "先录制一段鼠标操作，再按需管理方案、打开步骤列表和设置热键。";
    }
}
