using System.Windows.Input;
using DARK_Messenger.Models;
using DARK_Messenger.Services;

namespace DARK_Messenger.ViewModels;

public class RegisterViewModel : BaseViewModel
{
    private ApiClient? _api;
    private SettingsService? _settings;
    private SocketService? _socket;
    private ChatService? _chatService;

    private string _username = "";
    public string Username { get => _username; set => SetProperty(ref _username, value); }

    private string _displayName = "";
    public string DisplayName { get => _displayName; set => SetProperty(ref _displayName, value); }

    private string _password = "";
    public string Password { get => _password; set => SetProperty(ref _password, value); }

    private string _passwordConfirm = "";
    public string PasswordConfirm { get => _passwordConfirm; set => SetProperty(ref _passwordConfirm, value); }

    private bool _isPasswordHidden = true;
    public bool IsPasswordHidden { get => _isPasswordHidden; set => SetProperty(ref _isPasswordHidden, value); }

    private string _passwordEyeIcon = "👁";
    public string PasswordEyeIcon { get => _passwordEyeIcon; set => SetProperty(ref _passwordEyeIcon, value); }

    public event Action<string>? OnError;
    public event Action? OnRegisterSuccess;
    public event Action? OnGoToLogin;

    public RegisterViewModel()
    {
        _api = AppServices.Get<ApiClient>() ?? new ApiClient();
        _settings = AppServices.Get<SettingsService>() ?? new SettingsService();
        _socket = AppServices.Get<SocketService>() ?? new SocketService();
        _chatService = AppServices.Get<ChatService>();
    }

    public void TogglePassword()
    {
        IsPasswordHidden = !IsPasswordHidden;
        PasswordEyeIcon = IsPasswordHidden ? "👁" : "🙈";
    }

    public void GoToLogin()
    {
        OnGoToLogin?.Invoke();
    }

    public async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            OnError?.Invoke("Введите логин");
            return;
        }
        if (string.IsNullOrWhiteSpace(Password))
        {
            OnError?.Invoke("Введите пароль");
            return;
        }
        if (Password != PasswordConfirm)
        {
            OnError?.Invoke("Пароли не совпадают");
            return;
        }
        if (_api == null || _settings == null || _socket == null || _chatService == null)
        {
            OnError?.Invoke("Ошибка инициализации");
            return;
        }

        IsBusy = true;
        Error = null;

        try
        {
            var result = await _api.RegisterAsync(Username, DisplayName, Password);
            if (result != null)
            {
                _settings.Token = result.Token;
                _api.SetToken(result.Token);
                _settings.CurrentUser = new User
                {
                    Id = result.UserId,
                    Username = Username,
                    DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? Username : DisplayName
                };
                _socket.Connect(result.Token);
                await _chatService.LoadChatsAsync();
                OnRegisterSuccess?.Invoke();
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
