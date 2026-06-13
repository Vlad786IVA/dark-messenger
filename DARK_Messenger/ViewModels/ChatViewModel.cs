using System.Windows.Input;
using DARK_Messenger.Models;
using DARK_Messenger.Services;

namespace DARK_Messenger.ViewModels;

public class ChatViewModel : BaseViewModel
{
    private ChatService? _chatService;
    private SettingsService? _settings;

    private string _newMessageText = "";
    public string NewMessageText { get => _newMessageText; set { SetProperty(ref _newMessageText, value); ((Command)SendMessageCommand).ChangeCanExecute(); } }

    public ICommand SendMessageCommand { get; }
    public ICommand AttachPhotoCommand { get; }
    public ICommand AttachVideoCommand { get; }
    public ICommand AttachFileCommand { get; }
    public ICommand RecordCircleCommand { get; }
    public ICommand BackCommand { get; }

    public event Action? OnBack;

    public ChatViewModel()
    {
        InitServices();
        SendMessageCommand = new Command(async () => await SendMessageAsync(), () => !string.IsNullOrWhiteSpace(NewMessageText));
        AttachPhotoCommand = new Command(async () => await PickPhotoAsync());
        AttachVideoCommand = new Command(async () => await PickVideoAsync());
        AttachFileCommand = new Command(async () => await PickFileAsync());
        RecordCircleCommand = new Command(async () => await RecordCircleAsync());
        BackCommand = new Command(() => OnBack?.Invoke());
    }

    public ChatViewModel(ChatService chatService, SettingsService settings)
    {
        _chatService = chatService;
        _settings = settings;

        SendMessageCommand = new Command(async () => await SendMessageAsync(), () => !string.IsNullOrWhiteSpace(NewMessageText));
        AttachPhotoCommand = new Command(async () => await PickPhotoAsync());
        AttachVideoCommand = new Command(async () => await PickVideoAsync());
        AttachFileCommand = new Command(async () => await PickFileAsync());
        RecordCircleCommand = new Command(async () => await RecordCircleAsync());
        BackCommand = new Command(() => OnBack?.Invoke());
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
                if (_chatService != null)
                    _chatService.OnMessagesChanged += () => OnPropertyChanged(nameof(Messages));
            }
        }
        catch { }
    }

    public async Task LoadChatAsync(string chatId)
    {
        if (_chatService == null) InitServices();
        if (_chatService != null)
            await _chatService.LoadMessagesAsync(chatId);
    }

    public System.Collections.ObjectModel.ObservableCollection<Message> Messages => _chatService?.CurrentMessages ?? new();
    public Chat? CurrentChat => _chatService?.CurrentChat;

    private async Task SendMessageAsync()
    {
        if (_chatService == null || _settings == null) return;
        if (string.IsNullOrWhiteSpace(NewMessageText) || _chatService.CurrentChat == null) return;

        var message = new Message
        {
            Id = Guid.NewGuid().ToString(),
            ChatId = _chatService.CurrentChat.Id,
            SenderId = _settings.CurrentUser?.Id ?? "",
            SenderName = _settings.CurrentUser?.DisplayNameOrPhone ?? "",
            Type = MessageType.Text,
            Content = NewMessageText,
            Timestamp = DateTime.UtcNow,
            Status = MessageStatus.Sent
        };

        NewMessageText = "";
        await _chatService.SendMessageAsync(message);
    }

    private async Task PickPhotoAsync()
    {
        if (_chatService == null || _settings == null) return;
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync();
            if (result != null && _chatService.CurrentChat != null)
            {
                var message = new Message
                {
                    Id = Guid.NewGuid().ToString(),
                    ChatId = _chatService.CurrentChat.Id,
                    SenderId = _settings.CurrentUser?.Id ?? "",
                    SenderName = _settings.CurrentUser?.DisplayNameOrPhone ?? "",
                    Type = MessageType.Image,
                    Content = "",
                    FileUrl = result.FullPath,
                    FileName = result.FileName,
                    Timestamp = DateTime.UtcNow,
                    Status = MessageStatus.Sent
                };
                await _chatService.SendMessageAsync(message);
            }
        }
        catch { }
    }

    private async Task PickVideoAsync()
    {
        if (_chatService == null || _settings == null) return;
        try
        {
            var result = await MediaPicker.Default.PickVideoAsync();
            if (result != null && _chatService.CurrentChat != null)
            {
                var message = new Message
                {
                    Id = Guid.NewGuid().ToString(),
                    ChatId = _chatService.CurrentChat.Id,
                    SenderId = _settings.CurrentUser?.Id ?? "",
                    SenderName = _settings.CurrentUser?.DisplayNameOrPhone ?? "",
                    Type = MessageType.Video,
                    Content = "",
                    FileUrl = result.FullPath,
                    FileName = result.FileName,
                    Timestamp = DateTime.UtcNow,
                    Status = MessageStatus.Sent
                };
                await _chatService.SendMessageAsync(message);
            }
        }
        catch { }
    }

    private async Task PickFileAsync()
    {
        if (_chatService == null || _settings == null) return;
        try
        {
            var result = await FilePicker.Default.PickAsync();
            if (result != null && _chatService.CurrentChat != null)
            {
                var message = new Message
                {
                    Id = Guid.NewGuid().ToString(),
                    ChatId = _chatService.CurrentChat.Id,
                    SenderId = _settings.CurrentUser?.Id ?? "",
                    SenderName = _settings.CurrentUser?.DisplayNameOrPhone ?? "",
                    Type = MessageType.File,
                    Content = "",
                    FileUrl = result.FullPath,
                    FileName = result.FileName,
                    Timestamp = DateTime.UtcNow,
                    Status = MessageStatus.Sent
                };
                await _chatService.SendMessageAsync(message);
            }
        }
        catch { }
    }

    private async Task RecordCircleAsync()
    {
        if (_chatService == null || _settings == null) return;
        try
        {
            var result = await MediaPicker.Default.CaptureVideoAsync();
            if (result != null && _chatService.CurrentChat != null)
            {
                var message = new Message
                {
                    Id = Guid.NewGuid().ToString(),
                    ChatId = _chatService.CurrentChat.Id,
                    SenderId = _settings.CurrentUser?.Id ?? "",
                    SenderName = _settings.CurrentUser?.DisplayNameOrPhone ?? "",
                    Type = MessageType.VideoMessage,
                    Content = "",
                    FileUrl = result.FullPath,
                    FileName = result.FileName,
                    Timestamp = DateTime.UtcNow,
                    Status = MessageStatus.Sent
                };
                await _chatService.SendMessageAsync(message);
            }
        }
        catch { }
    }
}
