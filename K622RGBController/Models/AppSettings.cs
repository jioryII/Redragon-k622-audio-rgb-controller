using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

namespace K622RGBController.Models;

public enum AppLanguage
{
    Spanish,
    English
}

/// <summary>
/// Complete application state. Auto-saves to %APPDATA%/K622_RGB/config.json.
/// </summary>
public class AppSettings
{
    [JsonPropertyName("language")]
    public AppLanguage Language { get; set; } = AppLanguage.English;

    // ── Gradient colors (4 anchor colors) ──
    [JsonPropertyName("gradient_colors")]
    public int[][] GradientColors { get; set; } = new[]
    {
        new[] { 0, 255, 65 },
        new[] { 255, 255, 0 },
        new[] { 255, 120, 0 },
        new[] { 255, 0, 60 },
    };

    // ── Audio settings ──
    [JsonPropertyName("sensitivity")]
    public double Sensitivity { get; set; } = 1.0;

    [JsonPropertyName("smoothing")]
    public double Smoothing { get; set; } = 0.3;

    [JsonPropertyName("bar_decay")]
    public double BarDecay { get; set; } = 0.75;

    // ── Display settings ──
    [JsonPropertyName("brightness")]
    public double Brightness { get; set; } = 1.0;

    [JsonPropertyName("angle")]
    public double Angle { get; set; } = 0.0;

    [JsonPropertyName("fluidity")]
    public double Fluidity { get; set; } = 0.5;

    [JsonPropertyName("target_fps")]
    public int TargetFps { get; set; } = 30;

    // ── Idle Mode (Reposo) ──
    [JsonPropertyName("idle_enabled")]
    public bool IdleEnabled { get; set; } = true;

    [JsonPropertyName("idle_intensity")]
    public double IdleIntensity { get; set; } = 0.6;

    [JsonPropertyName("idle_speed")]
    public double IdleSpeed { get; set; } = 0.5;

    [JsonPropertyName("idle_colors")]
    public int[][] IdleColors { get; set; } = new[]
    {
        new[] { 30, 0, 150 },
        new[] { 0, 200, 255 },
        new[] { 0, 0, 0 },
        new[] { 100, 0, 200 },
    };

    // ── Notification settings ──
    [JsonPropertyName("notif_color")]
    public int[] NotifColor { get; set; } = { 255, 255, 255 };

    [JsonPropertyName("notif_duration")]
    public double NotifDuration { get; set; } = 3.0;

    [JsonPropertyName("notif_intensity")]
    public double NotifIntensity { get; set; } = 1.0;

    [JsonPropertyName("notif_enabled")]
    public bool NotifEnabled { get; set; } = true;

    // ── Mode ──
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "music";

    // ── App behavior ──
    [JsonPropertyName("start_with_windows")]
    public bool StartWithWindows { get; set; } = true;

    [JsonPropertyName("start_minimized")]
    public bool StartMinimized { get; set; } = true;

    [JsonPropertyName("auto_connect")]
    public bool AutoConnect { get; set; } = true;

    // ── Legacy colors (profile compat) ──
    [JsonPropertyName("bass_color")]
    public int[] BassColor { get; set; } = { 255, 23, 68 };

    [JsonPropertyName("mids_color")]
    public int[] MidsColor { get; set; } = { 41, 121, 255 };

    [JsonPropertyName("treble_color")]
    public int[] TrebleColor { get; set; } = { 0, 230, 118 };

    // ── File paths ──
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "K622_RGB");

    private static readonly string ConfigFile = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(ConfigFile))
            {
                var json = File.ReadAllText(ConfigFile);
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading config: {ex.Message}");
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(ConfigFile, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving config: {ex.Message}");
        }
    }
}
