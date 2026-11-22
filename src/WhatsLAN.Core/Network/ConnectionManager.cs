using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using WhatsLAN.Core.Models;

namespace WhatsLAN.Core.Network;

public class ConnectionManager : IDisposable
{
    private readonly Profile _myProfile;
    private readonly TcpServer _server;
    private readonly ConcurrentDictionary<string, PeerConnection> _activeConnections = new();
    private readonly SemaphoreSlim _connectLock = new(1, 1);

    public event EventHandler<(string PeerUserId, MessagePacket Message)>? MessageReceived;
    public event EventHandler<(string PeerUserId, AckDeliveredPacket Ack)>? AckReceived;
    public event EventHandler<(string PeerUserId, AckReadPacket Ack)>? AckReadReceived;
    public event EventHandler<(string PeerUserId, TypingPacket Typing)>? TypingReceived;
    public event EventHandler<(string PeerUserId, EditPacket Edit)>? EditReceived;
    public event EventHandler<(string PeerUserId, DeletePacket Delete)>? DeleteReceived;
    public event EventHandler<(string PeerUserId, ReactionPacket Reaction)>? ReactionReceived;
    public event EventHandler<string>? PeerConnected;
    public event EventHandler<string>? PeerDisconnected;

    public int ListeningPort => _server.Port;

    public ConnectionManager(Profile myProfile, int listeningPort)
    {
        _myProfile = myProfile;
        _server = new TcpServer(listeningPort);
        _server.ClientConnected += OnInboundClientConnected;
    }

    public void Start()
    {
        _server.Start();
    }

    public bool IsPeerConnected(string peerUserId)
    {
        return _activeConnections.TryGetValue(peerUserId, out var conn) && conn.IsConnected;
    }

    private async void OnInboundClientConnected(object? sender, TcpClient client)
    {
        PeerConnection? connection = null;
        try
        {
            connection = new PeerConnection(client, _myProfile);
            await connection.PerformInboundHandshakeAsync();

            var peerId = connection.PeerUserId;
            if (string.IsNullOrEmpty(peerId))
            {
                connection.Dispose();
                return;
            }

            // Gestion de doublon (§4.2)
            if (_activeConnections.TryGetValue(peerId, out var existing))
            {
                if (existing.IsConnected)
                {
                    // Conserver la connexion selon l'arbitrage
                    if (string.CompareOrdinal(_myProfile.UserId, peerId) < 0)
                    {
                        // Nous étions prioritaires pour initier, refuser la connexion entrante
                        connection.Dispose();
                        return;
                    }
                    else
                    {
                        existing.Dispose();
                    }
                }
            }

            _activeConnections[peerId] = connection;
            RegisterConnectionEvents(connection);
            PeerConnected?.Invoke(this, peerId);
        }
        catch
        {
            connection?.Dispose();
        }
    }

    public async Task<PeerConnection?> GetOrCreateConnectionAsync(string peerUserId, string ip, int port, string pubKey, CancellationToken ct = default)
    {
        if (_activeConnections.TryGetValue(peerUserId, out var existing) && existing.IsConnected)
        {
            return existing;
        }

        await _connectLock.WaitAsync(ct);
        try
        {
            if (_activeConnections.TryGetValue(peerUserId, out existing) && existing.IsConnected)
            {
                return existing;
            }

            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(ip, port, ct);

            var connection = new PeerConnection(tcpClient, _myProfile);
            await connection.PerformOutboundHandshakeAsync(pubKey, ct);

            _activeConnections[peerUserId] = connection;
            RegisterConnectionEvents(connection);
            PeerConnected?.Invoke(this, peerUserId);

            return connection;
        }
        catch
        {
            return null;
        }
        finally
        {
            _connectLock.Release();
        }
    }

    public async Task<bool> SendMessageAsync(string peerUserId, string ip, int port, string pubKey, MessagePacket message)
    {
        var connection = await GetOrCreateConnectionAsync(peerUserId, ip, port, pubKey);
        if (connection == null) return false;

        try
        {
            await connection.SendJsonAsync(PacketType.Msg, message);
            return true;
        }
        catch
        {
            _activeConnections.TryRemove(peerUserId, out _);
            connection.Dispose();
            return false;
        }
    }

    public async Task<bool> SendAckDeliveredAsync(string peerUserId, AckDeliveredPacket ack)
    {
        if (_activeConnections.TryGetValue(peerUserId, out var connection) && connection.IsConnected)
        {
            try
            {
                await connection.SendJsonAsync(PacketType.AckDelivered, ack);
                return true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    public async Task<bool> SendAckReadAsync(string peerUserId, AckReadPacket ack)
    {
        if (_activeConnections.TryGetValue(peerUserId, out var connection) && connection.IsConnected)
        {
            try
            {
                await connection.SendJsonAsync(PacketType.AckRead, ack);
                return true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    public async Task<bool> SendTypingAsync(string peerUserId, TypingPacket typing)
    {
        if (_activeConnections.TryGetValue(peerUserId, out var connection) && connection.IsConnected)
        {
            try
            {
                await connection.SendJsonAsync(PacketType.Typing, typing);
                return true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    public async Task<bool> SendEditAsync(string peerUserId, EditPacket edit)
    {
        if (_activeConnections.TryGetValue(peerUserId, out var connection) && connection.IsConnected)
        {
            try
            {
                await connection.SendJsonAsync(PacketType.Edit, edit);
                return true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    public async Task<bool> SendDeleteAsync(string peerUserId, DeletePacket delete)
    {
        if (_activeConnections.TryGetValue(peerUserId, out var connection) && connection.IsConnected)
        {
            try
            {
                await connection.SendJsonAsync(PacketType.Delete, delete);
                return true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    public async Task<bool> SendReactionAsync(string peerUserId, ReactionPacket reaction)
    {
        if (_activeConnections.TryGetValue(peerUserId, out var connection) && connection.IsConnected)
        {
            try
            {
                await connection.SendJsonAsync(PacketType.React, reaction);
                return true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    private void RegisterConnectionEvents(PeerConnection conn)
    {
        conn.PacketReceived += (s, e) =>
        {
            var peerId = conn.PeerUserId;
            if (string.IsNullOrEmpty(peerId)) return;

            if (e.Type == PacketType.Msg)
            {
                var json = Encoding.UTF8.GetString(e.Payload);
                var msg = JsonSerializer.Deserialize<MessagePacket>(json);
                if (msg != null)
                {
                    MessageReceived?.Invoke(this, (peerId, msg));
                }
            }
            else if (e.Type == PacketType.AckDelivered)
            {
                var json = Encoding.UTF8.GetString(e.Payload);
                var ack = JsonSerializer.Deserialize<AckDeliveredPacket>(json);
                if (ack != null)
                {
                    AckReceived?.Invoke(this, (peerId, ack));
                }
            }
            else if (e.Type == PacketType.AckRead)
            {
                var json = Encoding.UTF8.GetString(e.Payload);
                var ack = JsonSerializer.Deserialize<AckReadPacket>(json);
                if (ack != null)
                {
                    AckReadReceived?.Invoke(this, (peerId, ack));
                }
            }
            else if (e.Type == PacketType.Typing)
            {
                var json = Encoding.UTF8.GetString(e.Payload);
                var typing = JsonSerializer.Deserialize<TypingPacket>(json);
                if (typing != null)
                {
                    TypingReceived?.Invoke(this, (peerId, typing));
                }
            }
            else if (e.Type == PacketType.Edit)
            {
                var json = Encoding.UTF8.GetString(e.Payload);
                var edit = JsonSerializer.Deserialize<EditPacket>(json);
                if (edit != null)
                {
                    EditReceived?.Invoke(this, (peerId, edit));
                }
            }
            else if (e.Type == PacketType.Delete)
            {
                var json = Encoding.UTF8.GetString(e.Payload);
                var delete = JsonSerializer.Deserialize<DeletePacket>(json);
                if (delete != null)
                {
                    DeleteReceived?.Invoke(this, (peerId, delete));
                }
            }
            else if (e.Type == PacketType.React)
            {
                var json = Encoding.UTF8.GetString(e.Payload);
                var react = JsonSerializer.Deserialize<ReactionPacket>(json);
                if (react != null)
                {
                    ReactionReceived?.Invoke(this, (peerId, react));
                }
            }
        };

        conn.Disconnected += (s, e) =>
        {
            var peerId = conn.PeerUserId;
            if (!string.IsNullOrEmpty(peerId))
            {
                _activeConnections.TryRemove(peerId, out _);
                PeerDisconnected?.Invoke(this, peerId);
            }
            conn.Dispose();
        };
    }

    public void CloseConnection(string peerUserId)
    {
        if (_activeConnections.TryRemove(peerUserId, out var conn))
        {
            conn.Dispose();
            PeerDisconnected?.Invoke(this, peerUserId);
        }
    }

    public void Dispose()
    {
        _server.Dispose();
        foreach (var conn in _activeConnections.Values)
        {
            conn.Dispose();
        }
        _activeConnections.Clear();
        _connectLock.Dispose();
    }
}
