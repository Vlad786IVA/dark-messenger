using System.Windows;
using DARK_Messenger_WPF.Services;

namespace DARK_Messenger_WPF.Views;

public partial class ProfileWindow : Window
{
    public ProfileWindow()
    {
        InitializeComponent();
        var s = SettingsService.Load();
        UsernameText.Text = s.Username;
        DisplayNameBox.Text = s.DisplayName;
        var name = !string.IsNullOrEmpty(s.DisplayName) ? s.DisplayName : s.Username;
        AvatarText.Text = name.Length > 0 ? name[0].ToString().ToUpper() : "?";
    }

    private void Drag_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) => DragMove();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = DisplayNameBox.Text.Trim();
        if (string.IsNullOrEmpty(name)) { ShowStatus("Введите имя"); return; }
        var result = await ApiClient.UpdateProfile(name);
        if (result != null) { SettingsService.DisplayName = name; ShowStatus("Сохранено"); }
        else ShowStatus("Ошибка");
    }

    private async void ChangeAvatar_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var d = new Microsoft.Win32.OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png;*.gif;*.bmp", Title = "Аватар" };
        if (d.ShowDialog() == true)
        {
            var url = await ApiClient.UploadAvatar(d.FileName);
            if (url != null) { SettingsService.AvatarUrl = url; AvatarText.Text = "..."; ShowStatus("Аватар обновлён"); }
        }
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        SettingsService.Token = "";
        SettingsService.UserId = 0;
        ApiClient.Token = null;
        _ = WebSocketService.Disconnect();
        var chatWindows = Application.Current.Windows.OfType<ChatWindow>().ToList();
        foreach (var cw in chatWindows) { cw.Tag = "forceClose"; cw.Close(); }
        new LoginWindow().Show();
        Close();
    }

    private void SwitchTheme_Click(object sender, RoutedEventArgs e)
    {
        App.SwitchTheme(!App.IsDarkTheme);
        ShowStatus(App.IsDarkTheme ? "Тёмная тема" : "Светлая тема");
    }

    private void SwitchLang_Click(object sender, RoutedEventArgs e)
    {
        var newLang = App.Language == "ru" ? "en" : "ru";
        App.SwitchLanguage(newLang);
        SettingsService.Language = newLang;
        ShowStatus(newLang == "ru" ? "Русский" : "English");
    }

    private void ShowStatus(string msg) { StatusText.Text = msg; StatusText.Visibility = Visibility.Visible; }
}
