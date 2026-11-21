namespace WhatsLAN.Core.Models;

public class Attachment
{
    public string Id { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Mime { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public string? ThumbPath { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string TransferState { get; set; } = "Completed"; // "Pending", "Transferring", "Completed", "Failed"
}
