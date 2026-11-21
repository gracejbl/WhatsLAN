namespace WhatsLAN.Core.Models;

public class Profile
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Disponible";
    public string? AvatarPath { get; set; }
    public string? AvatarHash { get; set; }
    public string PrivateKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
