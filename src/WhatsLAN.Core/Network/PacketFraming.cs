using System.Buffers.Binary;
using WhatsLAN.Core.Security;

namespace WhatsLAN.Core.Network;

public static class PacketFraming
{
    public const int MaxFrameSize = 1024 * 1024; // 1 Mo (§4.3)

    /// <summary>
    /// Encode une trame selon la spécification §4.3:
    /// [4 octets longueur big-endian][1 octet type][1 octet flags][nonce 12 octets][payload chiffré AES-GCM][tag 16 octets]
    /// </summary>
    public static byte[] Encode(PacketType type, byte[] payload, byte[]? aesKey = null)
    {
        if (aesKey != null)
        {
            var nonce = CryptoService.GenerateNonce();
            // Associated Data = type (1 octet) + nonce (12 octets)
            var ad = new byte[1 + CryptoService.NonceSize];
            ad[0] = (byte)type;
            Buffer.BlockCopy(nonce, 0, ad, 1, CryptoService.NonceSize);

            var (ciphertext, tag) = CryptoService.Encrypt(aesKey, nonce, payload, ad);

            // Longueur de la trame suivant l'en-tête de 4 octets
            var frameBodyLen = 1 /* type */ + 1 /* flags */ + CryptoService.NonceSize + ciphertext.Length + CryptoService.TagSize;
            var frame = new byte[4 + frameBodyLen];

            // 4 octets longueur big-endian
            BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(0, 4), frameBodyLen);
            frame[4] = (byte)type;
            frame[5] = (byte)FrameFlags.Encrypted;

            Buffer.BlockCopy(nonce, 0, frame, 6, CryptoService.NonceSize);
            Buffer.BlockCopy(ciphertext, 0, frame, 6 + CryptoService.NonceSize, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, frame, 6 + CryptoService.NonceSize + ciphertext.Length, CryptoService.TagSize);

            return frame;
        }
        else
        {
            // Trame non chiffrée (ex: handshake initial avant établissement de clé)
            var frameBodyLen = 1 /* type */ + 1 /* flags */ + payload.Length;
            var frame = new byte[4 + frameBodyLen];

            BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(0, 4), frameBodyLen);
            frame[4] = (byte)type;
            frame[5] = (byte)FrameFlags.None;
            Buffer.BlockCopy(payload, 0, frame, 6, payload.Length);

            return frame;
        }
    }

    /// <summary>
    /// Lit et décode une trame complète depuis un flux asynchrone (§4.3).
    /// </summary>
    public static async Task<(PacketType Type, byte[] Payload)> ReadFrameAsync(Stream stream, byte[]? aesKey, CancellationToken ct = default)
    {
        var lenBuffer = new byte[4];
        await ReadExactAsync(stream, lenBuffer, 0, 4, ct);
        var frameBodyLen = BinaryPrimitives.ReadInt32BigEndian(lenBuffer);

        if (frameBodyLen <= 0 || frameBodyLen > MaxFrameSize)
        {
            throw new InvalidOperationException($"Taille de trame invalide : {frameBodyLen} octets.");
        }

        var frameBody = new byte[frameBodyLen];
        await ReadExactAsync(stream, frameBody, 0, frameBodyLen, ct);

        var type = (PacketType)frameBody[0];
        var flags = (FrameFlags)frameBody[1];

        if (flags.HasFlag(FrameFlags.Encrypted))
        {
            if (aesKey == null)
            {
                throw new InvalidOperationException("Trame chiffrée reçue mais aucune clé AES n'est disponible.");
            }

            var minEncryptedLen = 1 + 1 + CryptoService.NonceSize + CryptoService.TagSize;
            if (frameBodyLen < minEncryptedLen)
            {
                throw new InvalidOperationException("Trame chiffrée tronquée.");
            }

            var nonce = new byte[CryptoService.NonceSize];
            Buffer.BlockCopy(frameBody, 2, nonce, 0, CryptoService.NonceSize);

            var ciphertextLen = frameBodyLen - minEncryptedLen;
            var ciphertext = new byte[ciphertextLen];
            Buffer.BlockCopy(frameBody, 2 + CryptoService.NonceSize, ciphertext, 0, ciphertextLen);

            var tag = new byte[CryptoService.TagSize];
            Buffer.BlockCopy(frameBody, 2 + CryptoService.NonceSize + ciphertextLen, tag, 0, CryptoService.TagSize);

            // Associated Data identique à l'émission
            var ad = new byte[1 + CryptoService.NonceSize];
            ad[0] = (byte)type;
            Buffer.BlockCopy(nonce, 0, ad, 1, CryptoService.NonceSize);

            var plaintext = CryptoService.Decrypt(aesKey, nonce, ciphertext, tag, ad);
            return (type, plaintext);
        }
        else
        {
            var payloadLen = frameBodyLen - 2;
            var payload = new byte[payloadLen];
            Buffer.BlockCopy(frameBody, 2, payload, 0, payloadLen);
            return (type, payload);
        }
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken ct)
    {
        var totalRead = 0;
        while (totalRead < count)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset + totalRead, count - totalRead), ct);
            if (read == 0)
            {
                throw new EndOfStreamException("La connexion réseau a été fermée par le pair distant.");
            }
            totalRead += read;
        }
    }
}
