using System.Windows;
using System.Windows.Media;
using DARK_Messenger_WPF.Services;
using DARK_Messenger_WPF.Views;

namespace DARK_Messenger_WPF;

public partial class App : Application
{
    public static bool IsDarkTheme { get; private set; } = true;
    public static string Language { get; private set; } = "ru";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var settings = SettingsService.Load();
        ApiClient.BaseUrl = settings.ServerUrl;
        IsDarkTheme = settings.IsDarkTheme;
        Language = settings.Language;
        ApplyTheme(settings.IsDarkTheme);

        if (!string.IsNullOrEmpty(settings.Token))
        {
            ApiClient.Token = settings.Token;
            var chat = new Views.ChatWindow();
            MainWindow = chat;
            chat.Show();
        }
        else
        {
            var login = new LoginWindow();
            MainWindow = login;
            login.Show();
        }
    }

    public static void SwitchTheme(bool dark)
    {
        IsDarkTheme = dark;
        SettingsService.IsDarkTheme = dark;
        ApplyTheme(dark);
    }

    private static void ApplyTheme(bool dark)
    {
        var uri = dark ? "Themes/Dark.xaml" : "Themes/Light.xaml";
        var themeDict = new ResourceDictionary { Source = new Uri(uri, UriKind.Relative) };

        (string brushKey, string colorKey)[] pairs = [
            ("BackgroundBrush", "BackgroundColor"),
            ("SurfaceBrush", "SurfaceColor"),
            ("PrimaryBrush", "PrimaryColor"),
            ("AccentBrush", "AccentColor"),
            ("TextPrimaryBrush", "TextPrimaryColor"),
            ("TextSecondaryBrush", "TextSecondaryColor"),
            ("MessageSentBrush", "MessageSentColor"),
            ("MessageReceivedBrush", "MessageReceivedColor"),
            ("BorderBrush", "BorderColor"),
            ("InputBgBrush", "InputBgColor"),
            ("OnlineBrush", "OnlineColor"),
            ("ToastBgBrush", "ToastBgColor"),
        ];

        foreach (var (brushKey, colorKey) in pairs)
        {
            if (themeDict[colorKey] is Color color)
                Current.Resources[brushKey] = new SolidColorBrush(color);
        }
    }

    public static void SwitchLanguage(string lang)
    {
        Language = lang;
        SettingsService.Language = lang;
    }
}
