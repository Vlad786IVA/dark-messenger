using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DARK_Messenger_WPF.Models;

public class Message : INotifyPropertyChanged
{
    private int _id;
    private int _chatId;
    private int? _groupId;
    private int _senderId;
    private string _content = "";
    private string? _mediaUrl;
    private string? _mediaType;
    private int? _replyToId;
    private DateTime _sentAt;
    private bool _isRead;
    private bool _isEdited;
    private bool _isDeleted;
    private bool _isMine;

    public int Id { get => _id; set => SetField(ref _id, value); }
    public int ChatId { get => _chatId; set => SetField(ref _chatId, value); }
    public int? GroupId { get => _groupId; set => SetField(ref _groupId, value); }
    public int SenderId { get => _senderId; set => SetField(ref _senderId, value); }
    public string Content { get => _content; set => SetField(ref _content, value); }
    public string? MediaUrl { get => _mediaUrl; set => SetField(ref _mediaUrl, value); }
    public string? MediaType { get => _mediaType; set => SetField(ref _mediaType, value); }
    public int? ReplyToId { get => _replyToId; set => SetField(ref _replyToId, value); }
    public DateTime SentAt { get => _sentAt; set => SetField(ref _sentAt, value); }
    public bool IsRead { get => _isRead; set => SetField(ref _isRead, value); }
    public bool IsEdited { get => _isEdited; set => SetField(ref _isEdited, value); }
    public bool IsDeleted { get => _isDeleted; set => SetField(ref _isDeleted, value); }
    public bool IsMine { get => _isMine; set => SetField(ref _isMine, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
