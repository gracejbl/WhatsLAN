using System.Text;
using WhatsLAN.Core.Security;
using Xunit;

namespace WhatsLAN.Tests;

public class CryptoServiceTests
{
    [Fact]
    public void DeriveKey_ProducesValid32BytesKey()
    {
        var secret = KeyManager.GenerateEcdhKeyPair();
        var secretBytes = Encoding.UTF8.GetBytes(secret.PrivateKeyBase64);

        var key = CryptoService.DeriveKey(secretBytes);

        Assert.NotNull(key);
        Assert.Equal(32, key.Length);
    }

    [Fact]
    public void EncryptAndDecrypt_ProducesOriginalPlaintext()
    {
        var key = KeyManager.GenerateEcdhKeyPair();
        var aesKey = CryptoService.DeriveKey(Encoding.UTF8.GetBytes(key.PrivateKeyBase64));
        var nonce = CryptoService.GenerateNonce();
        var plaintext = Encoding.UTF8.GetBytes("Bonjour WhatsLAN chiffré !");
        var ad = new byte[] { 0x02, 0x01, 0x03 };

        var (ciphertext, tag) = CryptoService.Encrypt(aesKey, nonce, plaintext, ad);

        Assert.NotNull(ciphertext);
        Assert.NotNull(tag);
        Assert.Equal(plaintext.Length, ciphertext.Length);
        Assert.Equal(16, tag.Length);

        var decrypted = CryptoService.Decrypt(aesKey, nonce, ciphertext, tag, ad);
        var decryptedText = Encoding.UTF8.GetString(decrypted);

        Assert.Equal("Bonjour WhatsLAN chiffré !", decryptedText);
    }

    [Fact]
    public void Decrypt_WithWrongKeyOrTag_ThrowsCryptographicException()
    {
        var key = KeyManager.GenerateEcdhKeyPair();
        var aesKey = CryptoService.DeriveKey(Encoding.UTF8.GetBytes(key.PrivateKeyBase64));
        var wrongKey = CryptoService.DeriveKey(Encoding.UTF8.GetBytes("different_key_seed_1234567890"));
        var nonce = CryptoService.GenerateNonce();
        var plaintext = Encoding.UTF8.GetBytes("Secret");

        var (ciphertext, tag) = CryptoService.Encrypt(aesKey, nonce, plaintext);

        Assert.ThrowsAny<System.Security.Cryptography.CryptographicException>(() =>
            CryptoService.Decrypt(wrongKey, nonce, ciphertext, tag));
    }
}
