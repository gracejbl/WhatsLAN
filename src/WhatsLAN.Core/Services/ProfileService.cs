using WhatsLAN.Core.Models;
using WhatsLAN.Core.Security;
using WhatsLAN.Core.Storage;

namespace WhatsLAN.Core.Services;

public class ProfileService
{
    private readonly ProfileRepository _repository;
    private Profile? _currentProfile;

    public Profile? CurrentProfile => _currentProfile;

    public ProfileService(ProfileRepository repository)
    {
        _repository = repository;
        _currentProfile = _repository.GetProfile();
    }

    public bool HasProfile() => _repository.HasProfile();

    public Profile CreateProfile(string name, string? status, string? avatarPath)
    {
        var trimmedName = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName) || trimmedName.Length > 25)
        {
            throw new ArgumentException("Le pseudo doit contenir entre 1 et 25 caractères.", nameof(name));
        }

        var trimmedStatus = string.IsNullOrWhiteSpace(status) ? "Disponible" : status.Trim();
        if (trimmedStatus.Length > 139)
        {
            trimmedStatus = trimmedStatus[..139];
        }

        // Génération GUID permanent & clés ECDH
        var userId = Guid.NewGuid().ToString();
        var keyPair = KeyManager.GenerateEcdhKeyPair();

        var profile = new Profile
        {
            UserId = userId,
            Name = trimmedName,
            Status = trimmedStatus,
            AvatarPath = avatarPath,
            PrivateKey = keyPair.PrivateKeyBase64,
            PublicKey = keyPair.PublicKeyBase64,
            CreatedAtUtc = DateTime.UtcNow
        };

        _repository.SaveProfile(profile);
        _currentProfile = profile;
        return profile;
    }

    public void UpdateProfile(string name, string status, string? avatarPath)
    {
        if (_currentProfile == null)
        {
            throw new InvalidOperationException("Aucun profil existant à mettre à jour.");
        }

        var trimmedName = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName) || trimmedName.Length > 25)
        {
            throw new ArgumentException("Le pseudo doit contenir entre 1 et 25 caractères.", nameof(name));
        }

        _currentProfile.Name = trimmedName;
        _currentProfile.Status = string.IsNullOrWhiteSpace(status) ? "Disponible" : status.Trim();
        _currentProfile.AvatarPath = avatarPath;

        _repository.SaveProfile(_currentProfile);
    }
}
