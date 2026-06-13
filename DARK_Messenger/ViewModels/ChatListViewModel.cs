using System.Windows.Input;
using DARK_Messenger.Models;
using DARK_Messenger.Services;

namespace DARK_Messenger.ViewModels;

public class ChatListViewModel : BaseViewModel
{
    private ChatService? _chatService;
    private SettingsService? _settings;
    private ApiClient? _api;

    public ICommand OpenChatCommand { get; }
    public ICommand NewChatCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand RefreshCommand { get; }

    public event Action<Chat>? OnOpenChat;
    public event Action? OnNewChat;
    public event Action? OnLogout;

    public ChatListViewModel()
    {
        InitServices();
        OpenChatCommand = new Command<Chat>(c => OnOpenChat?.Invoke(c));
        NewChatCommand = new Command(() => OnNewChat?.Invoke());
        LogoutCommand = new Command(async () => await LogoutAsync());
        RefreshCommand = new Command(async () => await LoadChatsAsync());
    }

    public ChatListViewModel(ChatService chatService, SettingsService settings, ApiClient api)
    {
        _chatService = chatService;
        _settings = settings;
        _api = api;

        OpenChatCommand = new Command<Chat>(c => OnOpenChat?.Invoke(c));
        NewChatCommand = new Command(() => OnNewChat?.Invoke());
        LogoutCommand = new Command(async () => await LogoutAsync());
        RefreshCommand = new Command(async () => await LoadChatsAsync());
    }

    private void InitServices()
    {
        try
        {
            var handler = AppServices.Provider;
            if (handler != null)
            {
                _chatService = AppServices.Get<ChatService>();
                _settings = AppServices.Get<SettingsService>();
                _api = AppServices.Get<ApiClient>();
            }
        }
        catch { }
    }

    public async Task LoadChatsAsync()
    {
        if (_chatService == null) InitServices();
        if (_chatService == null) return;

        IsBusy = true;
        Error = null;
        try
        {
            await _chatService.LoadChatsAsync();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LogoutAsync()
    {
        if (_chatService == null || _settings == null || _api == null) return;
        _chatService.CurrentChat = null;
        _chatService.Chats.Clear();
        _settings.Clear();
        _api.ClearToken();
        OnLogout?.Invoke();
    }

    public System.Collections.ObjectModel.ObservableCollection<Chat> Chats => _chatService?.Chats ?? new();
}
