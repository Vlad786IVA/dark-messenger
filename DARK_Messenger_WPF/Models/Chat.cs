using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DARK_Messenger_WPF.Models;

public class Chat : INotifyPropertyChanged
{
    private int _id;
    private string _name = "";
    private string? _avatarUrl;
    private string _lastMessage = "";
    private DateTime _lastMessageTime;
    private int _unreadCount;
    private bool _isGroup;
    private int? _createdBy;
    private User? _otherUser;

    public int Id { get => _id; set => SetField(ref _id, value); }
    public string Name { get => _name; set => SetField(ref _name, value); }
    public string? AvatarUrl { get => _avatarUrl; set => SetField(ref _avatarUrl, value); }
    public string LastMessage { get => _lastMessage; set => SetField(ref _lastMessage, value); }
    public DateTime LastMessageTime { get => _lastMessageTime; set => SetField(ref _lastMessageTime, value); }
    public int UnreadCount { get => _unreadCount; set => SetField(ref _unreadCount, value); }
    public bool IsGroup { get => _isGroup; set => SetField(ref _isGroup, value); }
    public int? CreatedBy { get => _createdBy; set => SetField(ref _createdBy, value); }
    public User? OtherUser { get => _otherUser; set => SetField(ref _otherUser, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
