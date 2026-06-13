using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DARK_Messenger_WPF.Services;

namespace DARK_Messenger_WPF.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            SetPlaceholderText(UsernameBox);
            SetPlaceholderText(RegUsernameBox);
        };
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var source = e.OriginalSource as DependencyObject;
        while (source != null)
        {
            if (source is Button) return;
            source = VisualTreeHelper.GetParent(source);
        }
        DragMove();
    }
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private bool _passwordVisible = false;
    private void TogglePassword_Click(object sender, RoutedEventArgs e)
    {
        _passwordVisible = !_passwordVisible;
        if (_passwordVisible)
        {
            PasswordVisible.Text = PasswordHidden.Password;
            PasswordHidden.Visibility = Visibility.Collapsed;
            PasswordVisible.Visibility = Visibility.Visible;
            TogglePasswordBtn.Content = "🔓";
        }
        else
        {
            PasswordHidden.Password = PasswordVisible.Text;
            PasswordHidden.Visibility = Visibility.Visible;
            PasswordVisible.Visibility = Visibility.Collapsed;
            TogglePasswordBtn.Content = "👁";
        }
    }

    private void SetPlaceholderText(TextBox box)
    {
        if (string.IsNullOrEmpty(box.Text))
        {
            box.Text = box.Tag?.ToString() ?? "";
            box.Foreground = (Brush)FindResource("TextSecondaryBrush");
        }
    }

    private void RemovePlaceholder(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox box && box.Text == box.Tag?.ToString())
        {
            box.Text = "";
            box.Foreground = (Brush)FindResource("TextPrimaryBrush");
        }
    }

    private void SetPlaceholder(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox box && string.IsNullOrEmpty(box.Text))
        {
            box.Text = box.Tag?.ToString() ?? "";
            box.Foreground = (Brush)FindResource("TextSecondaryBrush");
        }
    }

    private async void SignIn_Click(object sender, RoutedEventArgs e)
    {
        var username = UsernameBox.Text.Trim();
        if (username == UsernameBox.Tag?.ToString()) username = "";
        var password = _passwordVisible ? PasswordVisible.Text : PasswordHidden.Password;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowStatus("Заполните все поля");
            return;
        }

        ShowLoading(true);

        var (ok, error, token, userId, displayName) = await ApiClient.Login(username, password);

        if (ok && token != null)
        {
            ApiClient.Token = token;
            SettingsService.Token = token;
            SettingsService.UserId = userId;
            SettingsService.Username = username;
            if (!string.IsNullOrEmpty(displayName)) SettingsService.DisplayName = displayName;
            OpenChatWindow();
        }
        else
        {
            ShowStatus(error ?? "Ошибка входа");
        }

        ShowLoading(false);
    }

    private async void Register_Click(object sender, RoutedEventArgs e)
    {
        var username = RegUsernameBox.Text.Trim();
        if (username == RegUsernameBox.Tag?.ToString()) username = "";
        var password = _regPasswordVisible ? RegPasswordVisible.Text : RegPasswordHidden.Password;
        var confirm = _regConfirmVisible ? RegConfirmVisible.Text : RegConfirmHidden.Password;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowStatus("Заполните все поля");
            return;
        }
        if (password != confirm) { ShowStatus("Пароли не совпадают"); return; }
        if (password.Length < 4) { ShowStatus("Пароль должен быть минимум 4 символа"); return; }

        ShowLoading(true);

        var (ok, error, token) = await ApiClient.Register(username, username, password);

        if (ok && token != null)
        {
            ApiClient.Token = token;
            SettingsService.Token = token;
            SettingsService.Username = username;

            // Auto-login after register
            var (loginOk, loginError, loginToken, userId, displayName) = await ApiClient.Login(username, password);
            if (loginOk && loginToken != null)
            {
                ApiClient.Token = loginToken;
                SettingsService.Token = loginToken;
                SettingsService.UserId = userId;
                if (!string.IsNullOrEmpty(displayName)) SettingsService.DisplayName = displayName;
                OpenChatWindow();
            }
            else
            {
                ShowStatus("Аккаунт создан, но не удалось войти");
            }
        }
        else
        {
            ShowStatus(error ?? "Ошибка регистрации");
        }

        ShowLoading(false);
    }

    private bool _regPasswordVisible = false;
    private void ToggleRegPassword_Click(object sender, RoutedEventArgs e)
    {
        _regPasswordVisible = !_regPasswordVisible;
        if (_regPasswordVisible)
        {
            RegPasswordVisible.Text = RegPasswordHidden.Password;
            RegPasswordHidden.Visibility = Visibility.Collapsed;
            RegPasswordVisible.Visibility = Visibility.Visible;
            ToggleRegPasswordBtn.Content = "🔓";
        }
        else
        {
            RegPasswordHidden.Password = RegPasswordVisible.Text;
            RegPasswordHidden.Visibility = Visibility.Visible;
            RegPasswordVisible.Visibility = Visibility.Collapsed;
            ToggleRegPasswordBtn.Content = "👁";
        }
    }

    private bool _regConfirmVisible = false;
    private void ToggleRegConfirm_Click(object sender, RoutedEventArgs e)
    {
        _regConfirmVisible = !_regConfirmVisible;
        if (_regConfirmVisible)
        {
            RegConfirmVisible.Text = RegConfirmHidden.Password;
            RegConfirmHidden.Visibility = Visibility.Collapsed;
            RegConfirmVisible.Visibility = Visibility.Visible;
            ToggleRegConfirmBtn.Content = "🔓";
        }
        else
        {
            RegConfirmHidden.Password = RegConfirmVisible.Text;
            RegConfirmHidden.Visibility = Visibility.Visible;
            RegConfirmVisible.Visibility = Visibility.Collapsed;
            ToggleRegConfirmBtn.Content = "👁";
        }
    }

    private void CreateAccount_Click(object sender, RoutedEventArgs e)
    {
        LoginPanel.Visibility = Visibility.Collapsed;
        RegisterPanel.Visibility = Visibility.Visible;
        StatusText.Visibility = Visibility.Collapsed;
    }

    private void BackToLogin_Click(object sender, RoutedEventArgs e)
    {
        RegisterPanel.Visibility = Visibility.Collapsed;
        LoginPanel.Visibility = Visibility.Visible;
        StatusText.Visibility = Visibility.Collapsed;
    }

    private void OpenChatWindow()
    {
        var chatWindow = new ChatWindow();
        Application.Current.MainWindow = chatWindow;
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
    }
}
