using System.Collections.ObjectModel;
using System.ComponentModel;
using DARK_Messenger.Models;
using DARK_Messenger.Services;

namespace DARK_Messenger.Services;

public class ChatService : INotifyPropertyChanged
{
    private readonly ApiClient _api;
    private readonly SocketService _socket;
    private readonly SettingsService _settings;

    public ObservableCollection<Chat> Chats { get; } = new();
    public ObservableCollection<Message> CurrentMessages { get; } = new();
    public Chat? CurrentChat { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? OnChatsChanged;
    public event Action? OnMessagesChanged;

    public ChatService(ApiClient api, SocketService socket, SettingsService settings)
    {
        _api = api;
        _socket = socket;
        _settings = settings;
        _socket.OnMessageReceived += OnSocketMessage;
    }

    private void OnSocketMessage(Message msg)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (CurrentChat?.Id == msg.ChatId)
            {
                CurrentMessages.Add(msg);
                OnMessagesChanged?.Invoke();
            }
            var chat = Chats.FirstOrDefault(c => c.Id == msg.ChatId);
            if (chat != null)
            {
                chat.LastMessage = msg.Content;
                chat.LastMessageTime = msg.Timestamp;
                chat.UnreadCount += msg.IsOutgoing ? 0 : 1;
                OnChatsChanged?.Invoke();
            }
        });
    }

    public async Task LoadChatsAsync()
    {
        var chats = await _api.GetChatsAsync();
        if (chats != null)
        {
            Chats.Clear();
            foreach (var c in chats.OrderByDescending(x => x.LastMessageTime ?? DateTime.MinValue))
                Chats.Add(c);
            OnChatsChanged?.Invoke();
        }
    }

    public async Task LoadMessagesAsync(string chatId)
    {
        CurrentChat = Chats.FirstOrDefault(c => c.Id == chatId);
        CurrentMessages.Clear();
        var messages = await _api.GetMessagesAsync(chatId);
        if (messages != null)
        {
            foreach (var m in messages.OrderBy(x => x.Timestamp))
                CurrentMessages.Add(m);
            CurrentChat!.UnreadCount = 0;
            OnMessagesChanged?.Invoke();
            OnChatsChanged?.Invoke();
        }
    }

    public async Task SendMessageAsync(Message message)
    {
        CurrentMessages.Add(message);
        OnMessagesChanged?.Invoke();

        await _api.SendMessageAsync(message.ChatId, message);
        _socket.SendMessageAsync(message);

        if (CurrentChat != null)
        {
            CurrentChat.LastMessage = message.Content;
            CurrentChat.LastMessageTime = message.Timestamp;
            OnChatsChanged?.Invoke();
        }
    }

    public async Task<string?> CreateChatAsync(string userId)
    {
        var chatId = await _api.CreateChatAsync(userId);
        if (!string.IsNullOrEmpty(chatId))
        {
            await LoadChatsAsync();
        }
        return chatId;
    }
}
