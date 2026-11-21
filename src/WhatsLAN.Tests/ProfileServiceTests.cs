using WhatsLAN.Core.Services;
using WhatsLAN.Core.Storage;
using Xunit;

namespace WhatsLAN.Tests;

public class ProfileServiceTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly Database _database;
    private readonly ProfileRepository _repository;
    private readonly ProfileService _service;

    public ProfileServiceTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"whatslan_prof_test_{Guid.NewGuid()}.db");
        _database = new Database(_testDbPath);
        _repository = new ProfileRepository(_database);
        _service = new ProfileService(_repository);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("CePseudoEstBeaucoupTropLongPourEtreValideEtDoitEtreRejete")]
    public void CreateProfile_WithInvalidName_ThrowsArgumentException(string invalidName)
    {
        Assert.Throws<ArgumentException>(() => _service.CreateProfile(invalidName, "Statut", null));
    }

    [Fact]
    public void CreateProfile_WithValidName_CreatesProfileWithGuidAndKeys()
    {
        var profile = _service.CreateProfile("MonPseudo", "En réunion", null);

        Assert.NotNull(profile);
        Assert.False(string.IsNullOrWhiteSpace(profile.UserId));
        Assert.True(Guid.TryParse(profile.UserId, out _));
        Assert.Equal("MonPseudo", profile.Name);
        Assert.Equal("En réunion", profile.Status);
        Assert.False(string.IsNullOrWhiteSpace(profile.PublicKey));
        Assert.False(string.IsNullOrWhiteSpace(profile.PrivateKey));

        Assert.True(_service.HasProfile());
        Assert.NotNull(_service.CurrentProfile);
        Assert.Equal(profile.UserId, _service.CurrentProfile.UserId);
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
