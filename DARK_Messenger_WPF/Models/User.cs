using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DARK_Messenger_WPF.Models;

public class User : INotifyPropertyChanged
{
    private int _id;
    private string _username = "";
    private string _displayName = "";
    private string _avatarUrl = "";
    private bool _isOnline;
    private DateTime _lastSeen;

    public int Id { get => _id; set => SetField(ref _id, value); }
    public string Username { get => _username; set => SetField(ref _username, value); }
    public string DisplayName { get => _displayName; set => SetField(ref _displayName, value); }
    public string AvatarUrl { get => _avatarUrl; set => SetField(ref _avatarUrl, value); }
    public bool IsOnline { get => _isOnline; set => SetField(ref _isOnline, value); }
    public DateTime LastSeen { get => _lastSeen; set => SetField(ref _lastSeen, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
