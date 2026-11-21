namespace WhatsLAN.Core.Models;

public class Conversation
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "direct"; // "direct" or "group"
    public string? PeerUserId { get; set; }
    public string? GroupId { get; set; }
    public bool Pinned { get; set; }
    public bool Muted { get; set; }
    public bool Archived { get; set; }
    public string? LastMessageId { get; set; }
    public int UnreadCount { get; set; }
    public string? DraftText { get; set; }
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
