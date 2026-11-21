namespace WhatsLAN.Core.Models;

public class Message
{
    public string Id { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string Kind { get; set; } = "text"; // "text", "image", "file", "voice", "system"
    public string Body { get; set; } = string.Empty;
    public string? ReplyToId { get; set; }
    public string Status { get; set; } = "Pending"; // "Pending", "Sent", "Delivered", "Read", "Failed"
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EditedUtc { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsStarred { get; set; }
    public string? AttachmentId { get; set; }
    public int? DurationMs { get; set; }
}
