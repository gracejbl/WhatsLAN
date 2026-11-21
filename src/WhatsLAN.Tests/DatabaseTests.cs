using WhatsLAN.Core.Models;
using WhatsLAN.Core.Storage;
using Xunit;

namespace WhatsLAN.Tests;

public class DatabaseTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly Database _database;

    public DatabaseTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"whatslan_test_{Guid.NewGuid()}.db");
        _database = new Database(_testDbPath);
    }

    [Fact]
    public void Database_InitializesTablesProperly()
    {
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";

        var tables = new List<string>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            tables.Add(reader.GetString(0));
        }

        Assert.Contains("Profile", tables);
        Assert.Contains("Contact", tables);
        Assert.Contains("Conversation", tables);
        Assert.Contains("Message", tables);
        Assert.Contains("Attachment", tables);
        Assert.Contains("Reaction", tables);
        Assert.Contains("Setting", tables);
    }

    [Fact]
    public void ProfileRepository_SavesAndRetrievesProfile()
    {
        var repo = new ProfileRepository(_database);

        Assert.False(repo.HasProfile());

        var profile = new Profile
        {
            UserId = Guid.NewGuid().ToString(),
            Name = "Utilisateur Test",
            Status = "Disponible",
            PrivateKey = "privkey",
            PublicKey = "pubkey",
            CreatedAtUtc = DateTime.UtcNow
        };

        repo.SaveProfile(profile);

        Assert.True(repo.HasProfile());

        var retrieved = repo.GetProfile();
        Assert.NotNull(retrieved);
        Assert.Equal(profile.UserId, retrieved.UserId);
        Assert.Equal("Utilisateur Test", retrieved.Name);
        Assert.Equal("Disponible", retrieved.Status);
    }

    [Fact]
    public void ConversationAndMessageRepository_StoresAndRetrieves()
    {
        var convRepo = new ConversationRepository(_database);
        var msgRepo = new MessageRepository(_database);

        var convId = Guid.NewGuid().ToString();
        convRepo.Save(new Conversation
        {
            Id = convId,
            Type = "direct",
            PeerUserId = "peer-1",
            Pinned = true,
            UnreadCount = 1,
            UpdatedUtc = DateTime.UtcNow
        });

        var convs = convRepo.GetAll();
        Assert.Single(convs);
        Assert.Equal(convId, convs[0].Id);
        Assert.True(convs[0].Pinned);

        var msgId = Guid.NewGuid().ToString();
        msgRepo.Insert(new Message
        {
            Id = msgId,
            ConversationId = convId,
            SenderId = "peer-1",
            Body = "Message test",
            Status = "Pending",
            CreatedUtc = DateTime.UtcNow
        });

        var messages = msgRepo.GetByConversationId(convId);
        Assert.Single(messages);
        Assert.Equal("Message test", messages[0].Body);
        Assert.Equal("Pending", messages[0].Status);

        msgRepo.UpdateStatus(msgId, "Delivered");
        var updated = msgRepo.GetByConversationId(convId);
        Assert.Equal("Delivered", updated[0].Status);
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
