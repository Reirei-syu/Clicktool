using ClickTool.Models;
using System.IO;
using System.Text.Json;

namespace ClickTool.Services;

public static class StorageService
{
    private const long MaxSupportedDelayMs = int.MaxValue;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly string DataDirectory;
    private static readonly string SettingsFilePath;
    private static readonly string RecordingsDirectory;

    static StorageService()
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        DataDirectory = Path.Combine(baseDirectory, "data");
        SettingsFilePath = Path.Combine(DataDirectory, "settings.json");
        RecordingsDirectory = Path.Combine(DataDirectory, "recordings");

        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(RecordingsDirectory);
    }

    public static AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                return SanitizeSettings(settings ?? new AppSettings());
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"鍔犺浇璁剧疆澶辫触: {ex.Message}");
        }

        return SanitizeSettings(new AppSettings());
    }

    public static void SaveSettings(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(SanitizeSettings(settings), JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"淇濆瓨璁剧疆澶辫触: {ex.Message}");
        }
    }

    public static string SaveRecording(RecordingSession session)
    {
        SanitizeSession(session);

        var filePath = ResolveRecordingPath(session.FilePath, session.Name, session.CreatedAt);
        session.FilePath = filePath;

        var json = JsonSerializer.Serialize(session, JsonOptions);
        File.WriteAllText(filePath, json);

        return filePath;
    }

    public static RecordingSession? LoadRecording(string filePath)
    {
        try
        {
            var resolvedPath = ResolveExistingRecordingPath(filePath);
            if (resolvedPath == null || !File.Exists(resolvedPath))
            {
                return null;
            }

            var json = File.ReadAllText(resolvedPath);
            var session = JsonSerializer.Deserialize<RecordingSession>(json, JsonOptions);
            if (session == null)
            {
                return null;
            }

            SanitizeSession(session);
            session.FilePath = resolvedPath;
            return session;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"鍔犺浇褰曞埗澶辫触: {ex.Message}");
            return null;
        }
    }

    public static List<string> GetRecordingFiles()
    {
        if (!Directory.Exists(RecordingsDirectory))
        {
            return new List<string>();
        }

        return Directory
            .GetFiles(RecordingsDirectory, "*.json")
            .OrderByDescending(File.GetLastWriteTime)
            .ToList();
    }

    public static List<RecordingSchemeSummary> GetRecordingSchemes()
    {
        var schemes = new List<RecordingSchemeSummary>();

        foreach (var filePath in GetRecordingFiles())
        {
            var session = LoadRecording(filePath);
            if (session == null)
            {
                continue;
            }

            StepDefinitionService.Normalize(session);
            schemes.Add(new RecordingSchemeSummary
            {
                FilePath = filePath,
                Name = session.Name,
                CreatedAt = session.CreatedAt,
                ActionCount = session.Actions.Count,
                StepCount = StepDefinitionService.GetStepCount(session)
            });
        }

        return schemes;
    }

    public static RecordingSession SaveRecordingAsNewScheme(RecordingSession source, string schemeName)
    {
        var clone = new RecordingSession
        {
            Name = schemeName,
            CreatedAt = DateTime.Now,
            Actions = source.Actions
                .Select(action => new MouseAction
                {
                    Action = action.Action,
                    X = action.X,
                    Y = action.Y,
                    DelayMs = action.DelayMs,
                    Button = action.Button,
                    State = action.State,
                    IsStepEnd = action.IsStepEnd
                })
                .ToList()
        };

        SaveRecording(clone);
        return clone;
    }

    public static bool DeleteRecording(string filePath)
    {
        try
        {
            var resolvedPath = ResolveExistingRecordingPath(filePath);
            if (resolvedPath == null || !File.Exists(resolvedPath))
            {
                return false;
            }

            File.Delete(resolvedPath);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"鍒犻櫎褰曞埗澶辫触: {ex.Message}");
            return false;
        }
    }

    private static AppSettings SanitizeSettings(AppSettings settings)
    {
        settings.Opacity = Math.Clamp(settings.Opacity, 0.05, 1.0);
        settings.MoveSampleIntervalMs = Math.Clamp(settings.MoveSampleIntervalMs, 0, 5000);
        return settings;
    }

    private static void SanitizeSession(RecordingSession session)
    {
        session.Name = string.IsNullOrWhiteSpace(session.Name)
            ? $"褰曞埗_{DateTime.Now:yyyyMMdd_HHmmss}"
            : session.Name.Trim();

        if (session.CreatedAt == default)
        {
            session.CreatedAt = DateTime.Now;
        }

        session.Actions ??= [];
        session.Actions = session.Actions
            .Where(action => action != null)
            .Select(action => SanitizeAction(action!))
            .ToList();
        session.ResetPlayback();
    }

    private static MouseAction SanitizeAction(MouseAction action)
    {
        action.DelayMs = Math.Clamp(action.DelayMs, 0, MaxSupportedDelayMs);

        if (action.Action != MouseActionType.Click)
        {
            action.Button = null;
            action.State = null;
        }

        return action;
    }

    private static string ResolveRecordingPath(string? rawFilePath, string sessionName, DateTime createdAt)
    {
        if (!string.IsNullOrWhiteSpace(rawFilePath))
        {
            var resolvedPath = ResolveExistingRecordingPath(rawFilePath);
            if (resolvedPath != null)
            {
                return resolvedPath;
            }
        }

        return Path.Combine(RecordingsDirectory, BuildRecordingFileName(sessionName, createdAt));
    }

    private static string? ResolveExistingRecordingPath(string? rawFilePath)
    {
        if (string.IsNullOrWhiteSpace(rawFilePath))
        {
            return null;
        }

        var fullPath = Path.GetFullPath(rawFilePath);
        return IsPathUnderDirectory(fullPath, RecordingsDirectory)
               && string.Equals(Path.GetExtension(fullPath), ".json", StringComparison.OrdinalIgnoreCase)
            ? fullPath
            : null;
    }

    private static bool IsPathUnderDirectory(string path, string baseDirectory)
    {
        var normalizedBaseDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(baseDirectory));
        var normalizedPath = Path.GetFullPath(path);

        return normalizedPath.StartsWith(
                   normalizedBaseDirectory + Path.DirectorySeparatorChar,
                   StringComparison.OrdinalIgnoreCase)
               || string.Equals(normalizedPath, normalizedBaseDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildRecordingFileName(string sessionName, DateTime createdAt)
    {
        return $"{createdAt:yyyyMMdd_HHmmss}_{SanitizeFileName(sessionName)}.json";
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "recording";
        }

        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalidCharacter, '_');
        }

        return string.IsNullOrWhiteSpace(name) ? "recording" : name.Trim();
    }
}
