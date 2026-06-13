namespace DARK_Messenger.Models;

public enum ChatType
{
    Private,
    Group
}

public class Chat
{
    public string Id { get; set; } = string.Empty;
    public ChatType Type { get; set; } = ChatType.Private;
    public string Title { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? LastMessage { get; set; }
    public DateTime? LastMessageTime { get; set; }
    public int UnreadCount { get; set; }
    public bool IsPinned { get; set; }
    public bool IsMuted { get; set; }
    public List<string> ParticipantIds { get; set; } = new();
    public Message? LastMessageObj { get; set; }
}
