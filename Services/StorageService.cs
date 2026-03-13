using ClickTool.Models;
using System.IO;
using System.Text.Json;

namespace ClickTool.Services;

public static class StorageService
{
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
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载设置失败: {ex.Message}");
        }

        return new AppSettings();
    }

    public static void SaveSettings(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存设置失败: {ex.Message}");
        }
    }

    public static string SaveRecording(RecordingSession session)
    {
        var filePath = session.FilePath;
        if (string.IsNullOrWhiteSpace(filePath))
        {
            var fileName = $"{session.CreatedAt:yyyyMMdd_HHmmss}_{SanitizeFileName(session.Name)}.json";
            filePath = Path.Combine(RecordingsDirectory, fileName);
            session.FilePath = filePath;
        }

        var json = JsonSerializer.Serialize(session, JsonOptions);
        File.WriteAllText(filePath, json);

        return filePath;
    }

    public static RecordingSession? LoadRecording(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var session = JsonSerializer.Deserialize<RecordingSession>(json, JsonOptions);
                if (session != null)
                {
                    session.FilePath = filePath;
                }

                return session;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载录制失败: {ex.Message}");
        }

        return null;
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
            if (!File.Exists(filePath))
            {
                return false;
            }

            File.Delete(filePath);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"删除录制失败: {ex.Message}");
            return false;
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalidCharacter, '_');
        }

        return name;
    }
}
