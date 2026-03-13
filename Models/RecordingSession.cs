using System.Text.Json.Serialization;

namespace ClickTool.Models;

public class RecordingSession
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "未命名录制";

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonPropertyName("actions")]
    public List<MouseAction> Actions { get; set; } = new();

    [JsonIgnore]
    public int CurrentStepIndex { get; set; }

    [JsonIgnore]
    public string? FilePath { get; set; }

    [JsonIgnore]
    public bool IsCompleted => CurrentStepIndex >= Actions.Count;

    public void ResetPlayback()
    {
        CurrentStepIndex = 0;
    }
}
