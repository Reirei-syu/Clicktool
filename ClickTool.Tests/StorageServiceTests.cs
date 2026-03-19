using ClickTool.Models;
using ClickTool.Services;
using Xunit;

namespace ClickTool.Tests;

public sealed class StorageServiceTests : IDisposable
{
    private readonly string _dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
    private readonly string _recordingsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "recordings");

    [Fact]
    public void SaveRecording_RelocatesPathOutsideRecordingsDirectory()
    {
        var session = new RecordingSession
        {
            Name = "path-escape",
            CreatedAt = new DateTime(2026, 3, 19, 10, 0, 0),
            FilePath = Path.Combine(Path.GetTempPath(), "outside-recording.json"),
            Actions =
            [
                new MouseAction
                {
                    Action = MouseActionType.Move,
                    X = 1,
                    Y = 2,
                    DelayMs = 0,
                    IsStepEnd = true
                }
            ]
        };

        var savedPath = StorageService.SaveRecording(session);

        Assert.StartsWith(Path.GetFullPath(_recordingsDirectory), Path.GetFullPath(savedPath), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(savedPath, session.FilePath);
        Assert.True(File.Exists(savedPath));
        Assert.False(File.Exists(Path.Combine(Path.GetTempPath(), "outside-recording.json")));
    }

    [Fact]
    public void LoadRecording_RejectsPathOutsideRecordingsDirectory()
    {
        var outsidePath = Path.Combine(Path.GetTempPath(), $"clicktool-outside-{Guid.NewGuid():N}.json");
        File.WriteAllText(outsidePath, "{}");

        var session = StorageService.LoadRecording(outsidePath);

        Assert.Null(session);
        File.Delete(outsidePath);
    }

    [Fact]
    public void LoadRecording_SanitizesMalformedRecordingData()
    {
        Directory.CreateDirectory(_recordingsDirectory);
        var recordingPath = Path.Combine(_recordingsDirectory, $"malformed-{Guid.NewGuid():N}.json");
        File.WriteAllText(
            recordingPath,
            """
            {
              "name": "   ",
              "created_at": "0001-01-01T00:00:00",
              "actions": [
                {
                  "action": "Move",
                  "x": 10,
                  "y": 20,
                  "delay_ms": 9999999999,
                  "button": "Left",
                  "state": "Down",
                  "is_step_end": false
                },
                {
                  "action": "Click",
                  "x": 30,
                  "y": 40,
                  "delay_ms": -50,
                  "button": "Right",
                  "state": "Up",
                  "is_step_end": true
                }
              ]
            }
            """);

        var session = StorageService.LoadRecording(recordingPath);

        Assert.NotNull(session);
        Assert.False(string.IsNullOrWhiteSpace(session!.Name));
        Assert.NotEqual(default, session.CreatedAt);
        Assert.Equal(2, session.Actions.Count);
        Assert.Equal(int.MaxValue, session.Actions[0].DelayMs);
        Assert.Null(session.Actions[0].Button);
        Assert.Null(session.Actions[0].State);
        Assert.Equal(0, session.Actions[1].DelayMs);
        Assert.Equal(recordingPath, session.FilePath);
    }

    public void Dispose()
    {
        if (!Directory.Exists(_dataDirectory) || !Directory.Exists(_recordingsDirectory))
        {
            return;
        }

        foreach (var filePath in Directory.GetFiles(_recordingsDirectory, "*.json"))
        {
            File.Delete(filePath);
        }
    }
}
