using System.IO;
using Newtonsoft.Json;

namespace DARK_Messenger_WPF.Services;

public class SettingsData
{
    public string Token { get; set; } = "";
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public string ServerUrl { get; set; } = System.Environment.GetEnvironmentVariable("DARK_SERVER_URL") ?? "http://localhost:8080";
    public bool IsDarkTheme { get; set; } = true;
    public string Language { get; set; } = "ru";
}

public static class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DARK_Messenger", "settings.json");

    private static SettingsData? _settings;
    private static readonly object _lock = new();
    private static DateTime _lastSave = DateTime.MinValue;

    public static SettingsData Load()
    {
        if (_settings != null) return _settings;
        lock (_lock)
        {
            if (_settings != null) return _settings;
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    _settings = JsonConvert.DeserializeObject<SettingsData>(json) ?? new SettingsData();
                }
                else _settings = new SettingsData();
            }
            catch { _settings = new SettingsData(); }
        }
        return _settings;
    }

    private static void Save()
    {
        var s = _settings;
        if (s == null) return;
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            if ((now - _lastSave).TotalMilliseconds < 300) return;
            _lastSave = now;
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath);
                if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(s, Formatting.Indented));
            }
            catch { }
        }
    }

    public static string Token { get => Load().Token; set { Load().Token = value; Save(); } }
    public static int UserId { get => Load().UserId; set { Load().UserId = value; Save(); } }
    public static string Username { get => Load().Username; set { Load().Username = value; Save(); } }
    public static string DisplayName { get => Load().DisplayName; set { Load().DisplayName = value; Save(); } }
    public static string AvatarUrl { get => Load().AvatarUrl; set { Load().AvatarUrl = value; Save(); } }
    public static string ServerUrl { get => Load().ServerUrl; set { Load().ServerUrl = value; ApiClient.BaseUrl = value; Save(); } }
    public static bool IsDarkTheme { get => Load().IsDarkTheme; set { Load().IsDarkTheme = value; Save(); } }
    public static string Language { get => Load().Language; set { Load().Language = value; Save(); } }
}
