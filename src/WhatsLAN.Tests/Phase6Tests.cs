using WhatsLAN.Core.Models;
using WhatsLAN.Core.Services;
using WhatsLAN.Core.Storage;
using Xunit;

namespace WhatsLAN.Tests;

public class Phase6Tests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly Database _database;
    private readonly ConversationRepository _convRepo;

    public Phase6Tests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"whatslan_test_p6_{Guid.NewGuid():N}.db");
        _database = new Database(_tempDbPath);
        _convRepo = new ConversationRepository(_database);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_tempDbPath))
            {
                File.Delete(_tempDbPath);
            }
        }
        catch
        {
        }
    }

    [Fact]
    public void DraftText_ShouldBePersistedAndRetrieved()
    {
        var conv = new Conversation
        {
            Id = "user_alpha",
            Type = "direct",
            PeerUserId = "Alpha",
            UpdatedUtc = DateTime.UtcNow
        };
        _convRepo.Save(conv);

        // Mise à jour du brouillon
        _convRepo.UpdateDraftText("user_alpha", "Bonjour, ceci est un brouillon...");
        var loaded = _convRepo.GetById("user_alpha");
        Assert.NotNull(loaded);
        Assert.Equal("Bonjour, ceci est un brouillon...", loaded.DraftText);

        // Effacement du brouillon
        _convRepo.UpdateDraftText("user_alpha", null);
        var cleared = _convRepo.GetById("user_alpha");
        Assert.NotNull(cleared);
        Assert.Null(cleared.DraftText);
    }

    [Fact]
    public void PinnedConversations_ShouldEnforceMaxThreeLimit()
    {
        // Créer 4 conversations
        for (int i = 1; i <= 4; i++)
        {
            _convRepo.Save(new Conversation
            {
                Id = $"user_{i}",
                Type = "direct",
                PeerUserId = $"User {i}",
                Pinned = false,
                UpdatedUtc = DateTime.UtcNow
            });
        }

        // Épingler 3 conversations : doit réussir
        Assert.True(_convRepo.SetPinned("user_1", true));
        Assert.True(_convRepo.SetPinned("user_2", true));
        Assert.True(_convRepo.SetPinned("user_3", true));
        Assert.Equal(3, _convRepo.GetPinnedCount());

        // Tenter d'épingler la 4e : doit être refusé (max 3 selon §7.4)
        Assert.False(_convRepo.SetPinned("user_4", true));
        Assert.Equal(3, _convRepo.GetPinnedCount());

        // Désépingler une et réépingler la 4e : doit réussir
        Assert.True(_convRepo.SetPinned("user_1", false));
        Assert.Equal(2, _convRepo.GetPinnedCount());
        Assert.True(_convRepo.SetPinned("user_4", true));
        Assert.Equal(3, _convRepo.GetPinnedCount());
    }

    [Fact]
    public void MutedAndArchived_ShouldToggleCorrectly()
    {
        var conv = new Conversation
        {
            Id = "user_beta",
            Type = "direct",
            PeerUserId = "Beta",
            Muted = false,
            Archived = false,
            UpdatedUtc = DateTime.UtcNow
        };
        _convRepo.Save(conv);

        // Mute
        _convRepo.SetMuted("user_beta", true);
        var afterMute = _convRepo.GetById("user_beta");
        Assert.NotNull(afterMute);
        Assert.True(afterMute.Muted);

        // Unmute
        _convRepo.SetMuted("user_beta", false);
        var afterUnmute = _convRepo.GetById("user_beta");
        Assert.NotNull(afterUnmute);
        Assert.False(afterUnmute.Muted);

        // Archive
        _convRepo.SetArchived("user_beta", true);
        var afterArchive = _convRepo.GetById("user_beta");
        Assert.NotNull(afterArchive);
        Assert.True(afterArchive.Archived);

        // Unarchive
        _convRepo.SetArchived("user_beta", false);
        var afterUnarchive = _convRepo.GetById("user_beta");
        Assert.NotNull(afterUnarchive);
        Assert.False(afterUnarchive.Archived);
    }

    [Fact]
    public void EmojiService_CategoriesAndSearch_ShouldWorkProperly()
    {
        var service = EmojiService.Instance;

        // Catégories
        Assert.Contains("Smileys", service.Categories);
        Assert.Contains("Personnes", service.Categories);
        Assert.Contains("Animaux", service.Categories);

        var smileys = service.GetEmojisByCategory("Smileys");
        Assert.NotEmpty(smileys);
        Assert.Contains(smileys, e => e.Emoji == "😀");

        // Recherche par mot clé
        var searchHeart = service.Search("coeur");
        Assert.NotEmpty(searchHeart);
        Assert.Contains(searchHeart, e => e.Emoji == "❤️");

        var searchJoy = service.Search("rire");
        Assert.NotEmpty(searchJoy);
        Assert.Contains(searchJoy, e => e.Emoji == "😂");

        var searchThumb = service.Search("pouce");
        Assert.NotEmpty(searchThumb);
        Assert.Contains(searchThumb, e => e.Emoji == "👍");
    }

    [Fact]
    public void EmojiService_Recents_ShouldTrackAndDeduplicate()
    {
        var service = EmojiService.Instance;

        service.AddRecent("🚀");
        service.AddRecent("🎉");
        service.AddRecent("🚀"); // Doublon : doit remonter en premier sans doubler

        var recents = service.GetEmojisByCategory("Récents");
        Assert.NotEmpty(recents);
        Assert.Equal("🚀", recents[0].Emoji);
        Assert.Equal(1, recents.Count(e => e.Emoji == "🚀"));
    }
}
