using System.Text.Json.Serialization;

namespace WhatsLAN.Core.Network;

public class HelloPacket
{
    [JsonPropertyName("t")]
    public string Type { get; set; } = "HELLO";

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("tcpPort")]
    public int TcpPort { get; set; }

    [JsonPropertyName("avatarHash")]
    public string? AvatarHash { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Disponible";

    [JsonPropertyName("pubKey")]
    public string PublicKey { get; set; } = string.Empty;
}

public class ByePacket
{
    [JsonPropertyName("t")]
    public string Type { get; set; } = "BYE";

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;
}

public class HandshakePacket
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("pubKey")]
    public string PublicKey { get; set; } = string.Empty;
}

public class MessagePacket
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("conversationId")]
    public string ConversationId { get; set; } = string.Empty;

    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("replyToId")]
    public string? ReplyToId { get; set; }

    [JsonPropertyName("timestampUtc")]
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "text";
}

public class AckDeliveredPacket
{
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = string.Empty;

    [JsonPropertyName("timestampUtc")]
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

public class AckReadPacket
{
    [JsonPropertyName("conversationId")]
    public string ConversationId { get; set; } = string.Empty;

    [JsonPropertyName("messageIds")]
    public List<string> MessageIds { get; set; } = [];

    [JsonPropertyName("timestampUtc")]
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

public class TypingPacket
{
    [JsonPropertyName("conversationId")]
    public string ConversationId { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = "start"; // "start", "stop", "recording"
}

public class EditPacket
{
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = string.Empty;

    [JsonPropertyName("newText")]
    public string NewText { get; set; } = string.Empty;

    [JsonPropertyName("editedAtUtc")]
    public DateTime EditedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DeletePacket
{
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = string.Empty;

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "forEveryone"; // "forEveryone" ou "forMe"
}

public class ReactionPacket
{
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = string.Empty;

    [JsonPropertyName("emoji")]
    public string Emoji { get; set; } = string.Empty; // Vide = retrait
}
