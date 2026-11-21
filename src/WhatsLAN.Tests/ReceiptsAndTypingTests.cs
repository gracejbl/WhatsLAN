using System.Text.Json;
using WhatsLAN.Core.Models;
using WhatsLAN.Core.Network;
using WhatsLAN.Core.Storage;
using Xunit;

namespace WhatsLAN.Tests;

public class ReceiptsAndTypingTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly Database _database;
    private readonly MessageRepository _messageRepository;

    public ReceiptsAndTypingTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"whatslan_test_{Guid.NewGuid():N}.db");
        _database = new Database(_testDbPath);
        _messageRepository = new MessageRepository(_database);
    }

    [Fact]
    public void AckReadPacket_SerializeAndDeserialize_PreservesData()
    {
        var ack = new AckReadPacket
        {
            ConversationId = "conv-123",
            MessageIds = ["msg-1", "msg-2", "msg-3"],
            TimestampUtc = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(ack);
        var deserialized = JsonSerializer.Deserialize<AckReadPacket>(json);

        Assert.NotNull(deserialized);
        Assert.Equal("conv-123", deserialized.ConversationId);
        Assert.Equal(3, deserialized.MessageIds.Count);
        Assert.Contains("msg-2", deserialized.MessageIds);
    }

    [Fact]
    public void TypingPacket_SerializeAndDeserialize_PreservesData()
    {
        var typing = new TypingPacket
        {
            ConversationId = "conv-456",
            State = "start"
        };

        var json = JsonSerializer.Serialize(typing);
        var deserialized = JsonSerializer.Deserialize<TypingPacket>(json);

        Assert.NotNull(deserialized);
        Assert.Equal("conv-456", deserialized.ConversationId);
        Assert.Equal("start", deserialized.State);
    }

    [Fact]
    public void GetPendingMessages_ReturnsOnlyPendingSorted()
    {
        var convId = "conv-offline";
        var now = DateTime.UtcNow;

        _messageRepository.Insert(new Message
        {
            Id = "msg-1",
            ConversationId = convId,
            SenderId = "me",
            Body = "Premier message en attente",
            Status = "Pending",
            CreatedUtc = now.AddMinutes(-2)
        });

        _messageRepository.Insert(new Message
        {
            Id = "msg-2",
            ConversationId = convId,
            SenderId = "me",
            Body = "Message déjà envoyé",
            Status = "Sent",
            CreatedUtc = now.AddMinutes(-1)
        });

        _messageRepository.Insert(new Message
        {
            Id = "msg-3",
            ConversationId = convId,
            SenderId = "me",
            Body = "Second message en attente",
            Status = "Pending",
            CreatedUtc = now
        });

        var pending = _messageRepository.GetPendingMessagesByConversation(convId);

        Assert.Equal(2, pending.Count);
        Assert.Equal("msg-1", pending[0].Id);
        Assert.Equal("msg-3", pending[1].Id);
    }

    [Fact]
    public void MarkMessagesAsRead_UpdatesStatusToRead()
    {
        var convId = "conv-read";
        var peerId = "peer-bob";

        _messageRepository.Insert(new Message
        {
            Id = "msg-incoming-1",
            ConversationId = convId,
            SenderId = peerId,
            Body = "Coucou",
            Status = "Delivered",
            CreatedUtc = DateTime.UtcNow
        });

        _messageRepository.Insert(new Message
        {
            Id = "msg-incoming-2",
            ConversationId = convId,
            SenderId = peerId,
            Body = "Tu es là ?",
            Status = "Delivered",
            CreatedUtc = DateTime.UtcNow
        });

        var unread = _messageRepository.GetUnreadIncomingMessageIds(convId, "my-user-id");
        Assert.Equal(2, unread.Count);

        _messageRepository.MarkMessagesAsRead(unread);

        var afterRead = _messageRepository.GetUnreadIncomingMessageIds(convId, "my-user-id");
        Assert.Empty(afterRead);

        var messages = _messageRepository.GetByConversationId(convId);
        Assert.All(messages, m => Assert.Equal("Read", m.Status));
    }

    public void Dispose()
    {
        _database.Dispose();
        if (File.Exists(_testDbPath))
        {
            try { File.Delete(_testDbPath); } catch { }
        }
    }
}
