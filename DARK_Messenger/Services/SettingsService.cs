using System.Text.Json;
using DARK_Messenger.Models;

namespace DARK_Messenger.Services;

public class SettingsService
{
    private readonly string _settingsPath;
    private AppSettings _settings = new();

    public SettingsService()
    {
        _settingsPath = Path.Combine(FileSystem.AppDataDirectory, "dark_settings.json");
        Load();
    }

    public AppSettings Settings => _settings;

    public string? Token
    {
        get => _settings.Token;
        set { _settings.Token = value; Save(); }
    }

    public User? CurrentUser
    {
        get => _settings.CurrentUser;
        set { _settings.CurrentUser = value; Save(); }
    }

    public void Clear()
    {
        _settings = new AppSettings();
        Save();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { _settings = new AppSettings(); }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch { }
    }
}

public class AppSettings
{
    public string? Token { get; set; }
    public User? CurrentUser { get; set; }
}
