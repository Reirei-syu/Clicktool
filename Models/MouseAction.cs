using System.Text.Json.Serialization;

namespace ClickTool.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MouseActionType
{
    Move,
    Click
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MouseButton
{
    Left,
    Right,
    Middle
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MouseButtonState
{
    Down,
    Up
}

public class MouseAction
{
    [JsonPropertyName("action")]
    public MouseActionType Action { get; set; }

    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("delay_ms")]
    public long DelayMs { get; set; }

    [JsonPropertyName("button")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MouseButton? Button { get; set; }

    [JsonPropertyName("state")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MouseButtonState? State { get; set; }

    [JsonPropertyName("is_step_end")]
    public bool IsStepEnd { get; set; }

    [JsonIgnore]
    public long TimestampMs { get; set; }

    [JsonIgnore]
    public bool IsClickRelease =>
        Action == MouseActionType.Click && State == MouseButtonState.Up;

    public string ToDisplaySummary()
    {
        return Action switch
        {
            MouseActionType.Move => $"移动到 ({X}, {Y})",
            MouseActionType.Click => $"{GetButtonLabel()}{GetStateLabel()} ({X}, {Y})",
            _ => $"未知动作 ({X}, {Y})"
        };
    }

    public string ToDelayLabel()
    {
        return DelayMs <= 0 ? "立即执行" : $"等待 {DelayMs} ms";
    }

    public override string ToString()
    {
        var stepMark = IsStepEnd ? " [步尾]" : string.Empty;
        return $"{ToDisplaySummary()} · {ToDelayLabel()}{stepMark}";
    }

    private string GetButtonLabel()
    {
        return Button switch
        {
            MouseButton.Left => "左键",
            MouseButton.Right => "右键",
            MouseButton.Middle => "中键",
            _ => "按键"
        };
    }

    private string GetStateLabel()
    {
        return State switch
        {
            MouseButtonState.Down => "按下",
            MouseButtonState.Up => "抬起",
            _ => "动作"
        };
    }
}
