using System.Text.Json;
using WhatsLAN.Core.Models;
using WhatsLAN.Core.Network;
using WhatsLAN.Core.Storage;
using Xunit;

namespace WhatsLAN.Tests;

public class MessageActionsTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly Database _database;
    private readonly MessageRepository _messageRepository;
    private readonly ReactionRepository _reactionRepository;

    public MessageActionsTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"whatslan_actions_test_{Guid.NewGuid():N}.db");
        _database = new Database(_testDbPath);
        _messageRepository = new MessageRepository(_database);
        _reactionRepository = new ReactionRepository(_database);
    }

    [Fact]
    public void Packets_SerializeAndDeserialize_Correctly()
    {
        var edit = new EditPacket { MessageId = "m1", NewText = "Texte modifié", EditedAtUtc = DateTime.UtcNow };
        var jsonEdit = JsonSerializer.Serialize(edit);
        var desEdit = JsonSerializer.Deserialize<EditPacket>(jsonEdit);
        Assert.NotNull(desEdit);
        Assert.Equal("m1", desEdit.MessageId);
        Assert.Equal("Texte modifié", desEdit.NewText);

        var del = new DeletePacket { MessageId = "m2", Mode = "forEveryone" };
        var jsonDel = JsonSerializer.Serialize(del);
        var desDel = JsonSerializer.Deserialize<DeletePacket>(jsonDel);
        Assert.NotNull(desDel);
        Assert.Equal("m2", desDel.MessageId);
        Assert.Equal("forEveryone", desDel.Mode);

        var react = new ReactionPacket { MessageId = "m3", Emoji = "❤️" };
        var jsonReact = JsonSerializer.Serialize(react);
        var desReact = JsonSerializer.Deserialize<ReactionPacket>(jsonReact);
        Assert.NotNull(desReact);
        Assert.Equal("m3", desReact.MessageId);
        Assert.Equal("❤️", desReact.Emoji);
    }

    [Fact]
    public void EditMessage_UpdatesTextAndTimestamp()
    {
        var msg = new Message
        {
            Id = "edit-1",
            ConversationId = "conv-1",
            SenderId = "user-alice",
            Body = "Texte initial",
            CreatedUtc = DateTime.UtcNow
        };
        _messageRepository.Insert(msg);

        var editTime = DateTime.UtcNow;
        _messageRepository.UpdateText("edit-1", "Nouveau texte corrigé", editTime);

        var updated = _messageRepository.GetById("edit-1");
        Assert.NotNull(updated);
        Assert.Equal("Nouveau texte corrigé", updated.Body);
        Assert.NotNull(updated.EditedUtc);
    }

    [Fact]
    public void DeleteMessage_ForEveryone_MarksDeleted()
    {
        var msg = new Message
        {
            Id = "del-1",
            ConversationId = "conv-1",
            SenderId = "user-alice",
            Body = "Message à supprimer",
            CreatedUtc = DateTime.UtcNow
        };
        _messageRepository.Insert(msg);

        _messageRepository.MarkDeleted("del-1");

        var deleted = _messageRepository.GetById("del-1");
        Assert.NotNull(deleted);
        Assert.True(deleted.IsDeleted);
        Assert.Equal("Ce message a été supprimé", deleted.Body);
    }

    [Fact]
    public void DeleteMessage_ForMe_RemovesFromDatabase()
    {
        var msg = new Message
        {
            Id = "del-for-me",
            ConversationId = "conv-1",
            SenderId = "user-alice",
            Body = "Message privé",
            CreatedUtc = DateTime.UtcNow
        };
        _messageRepository.Insert(msg);

        _messageRepository.DeleteForMe("del-for-me");

        var found = _messageRepository.GetById("del-for-me");
        Assert.Null(found);
    }

    [Fact]
    public void ReactionRepository_AddUpdateRemove_WorksProperly()
    {
        var msgId = "react-msg-1";
        var userAlice = "alice-guid";
        var userBob = "bob-guid";

        // Alice réagit avec 👍
        _reactionRepository.SetReaction(msgId, userAlice, "👍");
        var reactions1 = _reactionRepository.GetReactionsByMessageId(msgId);
        Assert.Single(reactions1);
        Assert.Equal("👍", reactions1[0].Emoji);

        // Alice change pour ❤️
        _reactionRepository.SetReaction(msgId, userAlice, "❤️");
        var reactions2 = _reactionRepository.GetReactionsByMessageId(msgId);
        Assert.Single(reactions2);
        Assert.Equal("❤️", reactions2[0].Emoji);

        // Bob réagit avec 😂
        _reactionRepository.SetReaction(msgId, userBob, "😂");
        var reactions3 = _reactionRepository.GetReactionsByMessageId(msgId);
        Assert.Equal(2, reactions3.Count);

        // Alice retire sa réaction
        _reactionRepository.RemoveReaction(msgId, userAlice);
        var reactions4 = _reactionRepository.GetReactionsByMessageId(msgId);
        Assert.Single(reactions4);
        Assert.Equal("😂", reactions4[0].Emoji);
    }

    [Fact]
    public void ToggleStarred_TogglesBooleanState()
    {
        var msg = new Message
        {
            Id = "star-1",
            ConversationId = "conv-1",
            SenderId = "user-alice",
            Body = "Message important",
            CreatedUtc = DateTime.UtcNow
        };
        _messageRepository.Insert(msg);

        var firstToggle = _messageRepository.ToggleStarred("star-1");
        Assert.True(firstToggle);

        var secondToggle = _messageRepository.ToggleStarred("star-1");
        Assert.False(secondToggle);
    }

    [Fact]
    public void SearchMessages_FindsMatchingKeywords()
    {
        var convId = "conv-search";
        _messageRepository.Insert(new Message
        {
            Id = "s-1",
            ConversationId = convId,
            SenderId = "alice",
            Body = "Rendez-vous demain à 14h",
            CreatedUtc = DateTime.UtcNow.AddMinutes(-5)
        });

        _messageRepository.Insert(new Message
        {
            Id = "s-2",
            ConversationId = convId,
            SenderId = "bob",
            Body = "D'accord pour demain",
            CreatedUtc = DateTime.UtcNow.AddMinutes(-4)
        });

        _messageRepository.Insert(new Message
        {
            Id = "s-3",
            ConversationId = convId,
            SenderId = "alice",
            Body = "Autre sujet totalement différent",
            CreatedUtc = DateTime.UtcNow.AddMinutes(-3)
        });

        var results = _messageRepository.SearchMessages(convId, "demain");
        Assert.Equal(2, results.Count);
        Assert.Equal("s-1", results[0].Id);
        Assert.Equal("s-2", results[1].Id);
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
