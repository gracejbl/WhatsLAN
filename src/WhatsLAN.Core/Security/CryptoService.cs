using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace WhatsLAN.Core.Security;

public static class CryptoService
{
    private const string DefaultInfo = "WhatsLAN-v1";
    public const int NonceSize = 12; // 12 octets
    public const int TagSize = 16;   // 16 octets
    public const int KeySize = 32;   // 256 bits

    /// <summary>
    /// Dérivation d'une clé AES-256 à partir du secret partagé ECDH via HKDF-SHA256 (§4.2, §17).
    /// </summary>
    public static byte[] DeriveKey(byte[] sharedSecret, string info = DefaultInfo, byte[]? salt = null)
    {
        var infoBytes = Encoding.UTF8.GetBytes(info);
        return HKDF.DeriveKey(HashAlgorithmName.SHA256, sharedSecret, KeySize, salt, infoBytes);
    }

    /// <summary>
    /// Chiffrement AES-256-GCM d'un payload (§4.3, §17).
    /// </summary>
    public static (byte[] Ciphertext, byte[] Tag) Encrypt(byte[] key, byte[] nonce, byte[] plaintext, byte[]? associatedData = null)
    {
        if (key.Length != KeySize) throw new ArgumentException($"La clé doit faire {KeySize} octets.", nameof(key));
        if (nonce.Length != NonceSize) throw new ArgumentException($"Le nonce doit faire {NonceSize} octets.", nameof(nonce));

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aesGcm = new AesGcm(key, TagSize);
        aesGcm.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

        return (ciphertext, tag);
    }

    /// <summary>
    /// Déchiffrement AES-256-GCM d'un payload (§4.3, §17).
    /// </summary>
    public static byte[] Decrypt(byte[] key, byte[] nonce, byte[] ciphertext, byte[] tag, byte[]? associatedData = null)
    {
        if (key.Length != KeySize) throw new ArgumentException($"La clé doit faire {KeySize} octets.", nameof(key));
        if (nonce.Length != NonceSize) throw new ArgumentException($"Le nonce doit faire {NonceSize} octets.", nameof(nonce));
        if (tag.Length != TagSize) throw new ArgumentException($"Le tag doit faire {TagSize} octets.", nameof(tag));

        var plaintext = new byte[ciphertext.Length];
        using var aesGcm = new AesGcm(key, TagSize);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);

        return plaintext;
    }

    public static byte[] GenerateNonce()
    {
        return RandomNumberGenerator.GetBytes(NonceSize);
    }

    #region Protection locale DPAPI (Windows)

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DATA_BLOB
    {
        public int cbData;
        public IntPtr pbData;
    }

    [DllImport("Crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool CryptProtectData(
        ref DATA_BLOB pDataIn,
        string szDataDescr,
        IntPtr pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        int dwFlags,
        ref DATA_BLOB pDataOut);

    [DllImport("Crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool CryptUnprotectData(
        ref DATA_BLOB pDataIn,
        StringBuilder ppszDataDescr,
        IntPtr pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        int dwFlags,
        ref DATA_BLOB pDataOut);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);

    public static byte[] ProtectData(byte[] data)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return data;
        }

        var inBlob = new DATA_BLOB
        {
            cbData = data.Length,
            pbData = Marshal.AllocHGlobal(data.Length)
        };
        Marshal.Copy(data, 0, inBlob.pbData, data.Length);

        var outBlob = new DATA_BLOB();
        try
        {
            if (CryptProtectData(ref inBlob, string.Empty, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1 /* CRYPTPROTECT_UI_FORBIDDEN */, ref outBlob))
            {
                var result = new byte[outBlob.cbData];
                Marshal.Copy(outBlob.pbData, result, 0, outBlob.cbData);
                return result;
            }
            return data;
        }
        finally
        {
            Marshal.FreeHGlobal(inBlob.pbData);
            if (outBlob.pbData != IntPtr.Zero)
            {
                LocalFree(outBlob.pbData);
            }
        }
    }

    public static byte[] UnprotectData(byte[] encryptedData)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return encryptedData;
        }

        var inBlob = new DATA_BLOB
        {
            cbData = encryptedData.Length,
            pbData = Marshal.AllocHGlobal(encryptedData.Length)
        };
        Marshal.Copy(encryptedData, 0, inBlob.pbData, encryptedData.Length);

        var outBlob = new DATA_BLOB();
        try
        {
            if (CryptUnprotectData(ref inBlob, null!, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1 /* CRYPTPROTECT_UI_FORBIDDEN */, ref outBlob))
            {
                var result = new byte[outBlob.cbData];
                Marshal.Copy(outBlob.pbData, result, 0, outBlob.cbData);
                return result;
            }
            return encryptedData;
        }
        finally
        {
            Marshal.FreeHGlobal(inBlob.pbData);
            if (outBlob.pbData != IntPtr.Zero)
            {
                LocalFree(outBlob.pbData);
            }
        }
    }

    #endregion
}
