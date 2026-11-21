using System.Security.Cryptography;

namespace WhatsLAN.Core.Security;

public class KeyPairInfo
{
    public string PublicKeyBase64 { get; set; } = string.Empty;
    public string PrivateKeyBase64 { get; set; } = string.Empty;
}

public static class KeyManager
{
    public static KeyPairInfo GenerateEcdhKeyPair()
    {
        using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var pubBytes = ecdh.PublicKey.ExportSubjectPublicKeyInfo();
        var privBytes = ecdh.ExportPkcs8PrivateKey();

        return new KeyPairInfo
        {
            PublicKeyBase64 = Convert.ToBase64String(pubBytes),
            PrivateKeyBase64 = Convert.ToBase64String(privBytes)
        };
    }

    public static ECDiffieHellman LoadPrivateKey(string privateKeyBase64)
    {
        var ecdh = ECDiffieHellman.Create();
        var bytes = Convert.FromBase64String(privateKeyBase64);
        ecdh.ImportPkcs8PrivateKey(bytes, out _);
        return ecdh;
    }

    public static ECDiffieHellmanPublicKey LoadPublicKey(string publicKeyBase64)
    {
        using var temp = ECDiffieHellman.Create();
        var bytes = Convert.FromBase64String(publicKeyBase64);
        temp.ImportSubjectPublicKeyInfo(bytes, out _);
        return temp.PublicKey;
    }

    public static byte[] DeriveSharedKey(string myPrivateKeyBase64, string otherPublicKeyBase64)
    {
        using var myEcdh = LoadPrivateKey(myPrivateKeyBase64);
        using var otherPub = ECDiffieHellman.Create();
        otherPub.ImportSubjectPublicKeyInfo(Convert.FromBase64String(otherPublicKeyBase64), out _);

        // Dérivation HKDF-SHA256 comme spécifié au §4.2
        return myEcdh.DeriveKeyFromHash(otherPub.PublicKey, HashAlgorithmName.SHA256);
    }
}
