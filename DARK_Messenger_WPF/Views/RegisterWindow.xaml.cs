using System.Windows;
using System.Windows.Input;
using DARK_Messenger_WPF.Services;

namespace DARK_Messenger_WPF.Views;

public partial class RegisterWindow : Window
{
    public RegisterWindow()
    {
        InitializeComponent();
    }

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private async void Register_Click(object sender, RoutedEventArgs e)
    {
        var username = UsernameBox.Text.Trim();
        var displayName = DisplayNameBox.Text.Trim();
        var password = PasswordBox.Password;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(displayName) || string.IsNullOrEmpty(password))
        {
            ShowStatus("Заполните все поля");
            return;
        }

        ShowLoading(true);

        var (ok, error, token) = await ApiClient.Register(username, displayName, password);

        if (ok && token != null)
        {
            ApiClient.Token = token;
            SettingsService.Token = token;
            OpenChatWindow();
        }
        else
        {
            ShowStatus(error);
        }

        ShowLoading(false);
    }

    private void BackToLogin_Click(object sender, RoutedEventArgs e)
    {
        var loginWindow = new LoginWindow();
        loginWindow.Show();
        Close();
    }

    private void OpenChatWindow()
    {
        var chatWindow = new ChatWindow();
        chatWindow.Show();
        Close();
    }

    private void ShowStatus(string message)
    {
        StatusText.Text = message;
        StatusText.Visibility = Visibility.Visible;
    }

    private void ShowLoading(bool loading)
    {
        LoadingBar.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
        RegisterButton.IsEnabled = !loading;
    }
}
