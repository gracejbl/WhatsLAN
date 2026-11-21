using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using WhatsLAN.Core.Models;
using WhatsLAN.Core.Security;

namespace WhatsLAN.Core.Network;

public class PacketReceivedEventArgs : EventArgs
{
    public PacketType Type { get; }
    public byte[] Payload { get; }

    public PacketReceivedEventArgs(PacketType type, byte[] payload)
    {
        Type = type;
        Payload = payload;
    }
}

public class PeerConnection : IDisposable
{
    private readonly TcpClient _tcpClient;
    private readonly NetworkStream _stream;
    private readonly Profile _myProfile;
    private CancellationTokenSource? _cts;
    private Task? _readTask;
    private Task? _pingTask;
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public string? PeerUserId { get; private set; }
    public string? PeerName { get; private set; }
    public string? PeerPublicKey { get; private set; }
    public byte[]? SharedAesKey { get; private set; }

    public bool IsConnected => _tcpClient.Connected;
    public bool IsAuthenticated => SharedAesKey != null;

    public event EventHandler<PacketReceivedEventArgs>? PacketReceived;
    public event EventHandler? Disconnected;

    public PeerConnection(TcpClient tcpClient, Profile myProfile)
    {
        _tcpClient = tcpClient;
        _stream = _tcpClient.GetStream();
        _myProfile = myProfile;
    }

    public async Task PerformOutboundHandshakeAsync(string expectedPeerPubKey, CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        // 1. Envoi de notre poignée de main initiale (non chiffrée)
        var myHandshake = new HandshakePacket
        {
            UserId = _myProfile.UserId,
            Name = _myProfile.Name,
            PublicKey = _myProfile.PublicKey
        };

        var myJson = JsonSerializer.Serialize(myHandshake);
        var myPayload = Encoding.UTF8.GetBytes(myJson);
        var frame = PacketFraming.Encode(PacketType.Handshake, myPayload, null);

        await _stream.WriteAsync(frame, _cts.Token);
        await _stream.FlushAsync(_cts.Token);

        // 2. Réception de la poignée de main distante
        var (respType, respPayload) = await PacketFraming.ReadFrameAsync(_stream, null, _cts.Token);
        if (respType != PacketType.Handshake)
        {
            throw new InvalidOperationException($"Type de paquet inattendu lors du handshake : {respType}");
        }

        var peerHandshake = JsonSerializer.Deserialize<HandshakePacket>(Encoding.UTF8.GetString(respPayload));
        if (peerHandshake == null || string.IsNullOrWhiteSpace(peerHandshake.PublicKey))
        {
            throw new InvalidOperationException("Poignée de main distante invalide.");
        }

        PeerUserId = peerHandshake.UserId;
        PeerName = peerHandshake.Name;
        PeerPublicKey = peerHandshake.PublicKey;

        // 3. Calcul du secret partagé ECDH et dérivation de la clé AES-256
        var sharedSecret = KeyManager.DeriveSharedKey(_myProfile.PrivateKey, PeerPublicKey);
        SharedAesKey = CryptoService.DeriveKey(sharedSecret);

        StartLoops();
    }

    public async Task PerformInboundHandshakeAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        // 1. Réception de la poignée de main initiale
        var (reqType, reqPayload) = await PacketFraming.ReadFrameAsync(_stream, null, _cts.Token);
        if (reqType != PacketType.Handshake)
        {
            throw new InvalidOperationException($"Type de paquet inattendu lors du handshake entrant : {reqType}");
        }

        var peerHandshake = JsonSerializer.Deserialize<HandshakePacket>(Encoding.UTF8.GetString(reqPayload));
        if (peerHandshake == null || string.IsNullOrWhiteSpace(peerHandshake.PublicKey))
        {
            throw new InvalidOperationException("Poignée de main entrante invalide.");
        }

        PeerUserId = peerHandshake.UserId;
        PeerName = peerHandshake.Name;
        PeerPublicKey = peerHandshake.PublicKey;

        // 2. Calcul du secret partagé et dérivation AES-256
        var sharedSecret = KeyManager.DeriveSharedKey(_myProfile.PrivateKey, PeerPublicKey);
        SharedAesKey = CryptoService.DeriveKey(sharedSecret);

        // 3. Réponse avec notre poignée de main (non chiffrée pour compléter l'accord)
        var myHandshake = new HandshakePacket
        {
            UserId = _myProfile.UserId,
            Name = _myProfile.Name,
            PublicKey = _myProfile.PublicKey
        };

        var myJson = JsonSerializer.Serialize(myHandshake);
        var myPayload = Encoding.UTF8.GetBytes(myJson);
        var frame = PacketFraming.Encode(PacketType.Handshake, myPayload, null);

        await _stream.WriteAsync(frame, _cts.Token);
        await _stream.FlushAsync(_cts.Token);

        StartLoops();
    }

    private void StartLoops()
    {
        _readTask = Task.Run(ReceiveLoopAsync);
        _pingTask = Task.Run(PingLoopAsync);
    }

    public async Task SendPacketAsync(PacketType type, byte[] payload)
    {
        if (!IsAuthenticated || SharedAesKey == null)
        {
            throw new InvalidOperationException("La connexion n'est pas encore chiffrée.");
        }

        var frame = PacketFraming.Encode(type, payload, SharedAesKey);

        await _sendLock.WaitAsync();
        try
        {
            await _stream.WriteAsync(frame);
            await _stream.FlushAsync();
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task SendJsonAsync<T>(PacketType type, T obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var bytes = Encoding.UTF8.GetBytes(json);
        await SendPacketAsync(type, bytes);
    }

    private async Task ReceiveLoopAsync()
    {
        var token = _cts?.Token ?? CancellationToken.None;

        while (!token.IsCancellationRequested && IsConnected)
        {
            try
            {
                var (type, payload) = await PacketFraming.ReadFrameAsync(_stream, SharedAesKey, token);

                if (type == PacketType.Ping)
                {
                    // Répondre automatiquement par PONG
                    _ = SendPacketAsync(PacketType.Pong, []);
                    continue;
                }
                else if (type == PacketType.Pong)
                {
                    continue;
                }

                PacketReceived?.Invoke(this, new PacketReceivedEventArgs(type, payload));
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                break;
            }
        }

        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    private async Task PingLoopAsync()
    {
        var token = _cts?.Token ?? CancellationToken.None;

        while (!token.IsCancellationRequested && IsConnected)
        {
            try
            {
                await Task.Delay(5000, token); // PING toutes les 5 secondes (§4.4)
                await SendPacketAsync(PacketType.Ping, []);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _stream.Dispose();
        _tcpClient.Dispose();
        _sendLock.Dispose();
        _cts?.Dispose();
    }
}
