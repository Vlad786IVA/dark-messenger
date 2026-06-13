namespace DARK_Messenger.Models;

public class User
{
    public string Id { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeen { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string DisplayNameOrPhone => DisplayName ?? Phone;
}
