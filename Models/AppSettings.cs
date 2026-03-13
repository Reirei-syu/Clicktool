using System.Text.Json.Serialization;

namespace ClickTool.Models;

public class AppSettings
{
    [JsonPropertyName("opacity")]
    public double Opacity { get; set; } = 0.85;

    [JsonPropertyName("window_left")]
    public double WindowLeft { get; set; } = 100;

    [JsonPropertyName("window_top")]
    public double WindowTop { get; set; } = 100;

    [JsonPropertyName("record_mouse_move")]
    public bool RecordMouseMove { get; set; } = true;

    [JsonPropertyName("move_sample_interval_ms")]
    public int MoveSampleIntervalMs { get; set; } = 50;

    [JsonPropertyName("hotkey_editor_window")]
    public uint HotkeyEditorWindow { get; set; } = 0x77; // F8

    [JsonPropertyName("hotkey_record")]
    public uint HotkeyRecord { get; set; } = 0x78; // F9

    [JsonPropertyName("hotkey_playall")]
    public uint HotkeyPlayAll { get; set; } = 0x79; // F10

    [JsonPropertyName("hotkey_step")]
    public uint HotkeyStep { get; set; } = 0x7A; // F11

    [JsonPropertyName("hotkey_stop")]
    public uint HotkeyStop { get; set; } = 0x7B; // F12
}
