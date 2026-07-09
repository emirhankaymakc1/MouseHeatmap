using System.Text.Json;
using System.Text.Json.Serialization;

namespace MouseHeatmap.Core;

public sealed class AppSettings
{
    public int MinDistancePx { get; set; } = 8;

    public int MinIntervalMs { get; set; } = 50;

    public bool StartRecordingOnLaunch { get; set; } = true;

    public bool Autostart { get; set; }

    public string Theme { get; set; } = "Dark";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public static string BaseDir { get; } = AppDomain.CurrentDomain.BaseDirectory;

    public static string DataDir => Path.Combine(BaseDir, "data");
    public static string ReportsDir => Path.Combine(BaseDir, "reports");
    public static string DatabasePath => Path.Combine(DataDir, "mouse_events.db");
    private static string SettingsPath => Path.Combine(DataDir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
                       ?? new AppSettings();
            }
        }
        catch (Exception)
        {
        }
        return new AppSettings();
    }

    public void Save()
    {
        Directory.CreateDirectory(DataDir);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOptions));
    }
}
