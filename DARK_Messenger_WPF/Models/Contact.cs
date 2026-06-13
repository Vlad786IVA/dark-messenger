using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DARK_Messenger_WPF.Models;

public class Contact : INotifyPropertyChanged
{
    private int _userId;
    private string _username = "";
    private string _displayName = "";
    private bool _isOnline;
    private DateTime _lastSeen;
    private string? _avatarUrl;

    public int UserId { get => _userId; set => SetField(ref _userId, value); }
    public string Username { get => _username; set => SetField(ref _username, value); }
    public string DisplayName { get => _displayName; set => SetField(ref _displayName, value); }
    public bool IsOnline { get => _isOnline; set => SetField(ref _isOnline, value); }
    public DateTime LastSeen { get => _lastSeen; set => SetField(ref _lastSeen, value); }
    public string? AvatarUrl { get => _avatarUrl; set => SetField(ref _avatarUrl, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
