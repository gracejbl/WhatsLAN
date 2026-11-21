using System.Text;
using WhatsLAN.Core.Network;
using WhatsLAN.Core.Security;
using Xunit;

namespace WhatsLAN.Tests;

public class PacketFramingTests
{
    [Fact]
    public async Task EncodeAndReadFrame_Plaintext_Succeeds()
    {
        var originalPayload = Encoding.UTF8.GetBytes("Test Payload Non Chiffré");
        var frame = PacketFraming.Encode(PacketType.Handshake, originalPayload, null);

        using var ms = new MemoryStream(frame);
        var (type, payload) = await PacketFraming.ReadFrameAsync(ms, null);

        Assert.Equal(PacketType.Handshake, type);
        Assert.Equal("Test Payload Non Chiffré", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task EncodeAndReadFrame_Encrypted_Succeeds()
    {
        var key = CryptoService.DeriveKey(Encoding.UTF8.GetBytes("shared_secret_32_bytes_long_ok!"));
        var originalPayload = Encoding.UTF8.GetBytes("Message ultra secret WhatsLAN");

        var frame = PacketFraming.Encode(PacketType.Msg, originalPayload, key);

        using var ms = new MemoryStream(frame);
        var (type, payload) = await PacketFraming.ReadFrameAsync(ms, key);

        Assert.Equal(PacketType.Msg, type);
        Assert.Equal("Message ultra secret WhatsLAN", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task ReadFrame_EncryptedWithoutKey_ThrowsInvalidOperationException()
    {
        var key = CryptoService.DeriveKey(Encoding.UTF8.GetBytes("shared_secret_32_bytes_long_ok!"));
        var originalPayload = Encoding.UTF8.GetBytes("Secret");

        var frame = PacketFraming.Encode(PacketType.Msg, originalPayload, key);

        using var ms = new MemoryStream(frame);
        await Assert.ThrowsAsync<InvalidOperationException>(() => PacketFraming.ReadFrameAsync(ms, null));
    }
}
