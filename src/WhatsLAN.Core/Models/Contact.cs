namespace WhatsLAN.Core.Models;

public class Contact
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Disponible";
    public string? AvatarPath { get; set; }
    public string? AvatarHash { get; set; }
    public string? LastIp { get; set; }
    public int LastPort { get; set; }
    public DateTime? LastSeenUtc { get; set; }
    public string? PublicKey { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsOnline { get; set; }
}
