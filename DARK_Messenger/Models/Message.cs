namespace DARK_Messenger.Models;

public enum MediaType
{
    Photo,
    Video
}

public enum MessageType
{
    Text,
    Image,
    Video,
    File,
    Voice,
    VideoMessage
}

public enum MessageStatus
{
    Sending,
    Sent,
    Delivered,
    Read,
    Failed
}

public class Message
{
    public string Id { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string? SenderAvatar { get; set; }
    public MessageType Type { get; set; } = MessageType.Text;
    public string Content { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? Duration { get; set; }
    public MessageStatus Status { get; set; } = MessageStatus.Sent;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAt { get; set; }
    public string? ReplyToId { get; set; }

    public bool IsOutgoing => Status == MessageStatus.Sent || Status == MessageStatus.Delivered || Status == MessageStatus.Read;
    public bool IsMedia => Type is MessageType.Image or MessageType.Video or MessageType.File;
}
