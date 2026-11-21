using WhatsLAN.Core.Security;
using Xunit;

namespace WhatsLAN.Tests;

public class KeyManagerTests
{
    [Fact]
    public void GenerateEcdhKeyPair_ProducesValidBase64Keys()
    {
        var keys = KeyManager.GenerateEcdhKeyPair();

        Assert.False(string.IsNullOrWhiteSpace(keys.PublicKeyBase64));
        Assert.False(string.IsNullOrWhiteSpace(keys.PrivateKeyBase64));

        using var priv = KeyManager.LoadPrivateKey(keys.PrivateKeyBase64);
        var pub = KeyManager.LoadPublicKey(keys.PublicKeyBase64);

        Assert.NotNull(priv);
        Assert.NotNull(pub);
    }

    [Fact]
    public void DeriveSharedKey_AliceAndBobDeriveSameSecret()
    {
        var aliceKeys = KeyManager.GenerateEcdhKeyPair();
        var bobKeys = KeyManager.GenerateEcdhKeyPair();

        var aliceShared = KeyManager.DeriveSharedKey(aliceKeys.PrivateKeyBase64, bobKeys.PublicKeyBase64);
        var bobShared = KeyManager.DeriveSharedKey(bobKeys.PrivateKeyBase64, aliceKeys.PublicKeyBase64);

        Assert.NotNull(aliceShared);
        Assert.NotNull(bobShared);
        Assert.Equal(32, aliceShared.Length); // 256 bits
        Assert.Equal(aliceShared, bobShared);
    }
}
