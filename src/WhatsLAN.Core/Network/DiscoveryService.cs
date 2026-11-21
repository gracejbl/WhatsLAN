using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using WhatsLAN.Core.Models;

namespace WhatsLAN.Core.Network;

public class PeerDiscoveredEventArgs : EventArgs
{
    public HelloPacket Peer { get; }
    public IPAddress RemoteAddress { get; }

    public PeerDiscoveredEventArgs(HelloPacket peer, IPAddress remoteAddress)
    {
        Peer = peer;
        RemoteAddress = remoteAddress;
    }
}

public class DiscoveryService : IDisposable
{
    public const int DefaultDiscoveryPort = 50000;
    private readonly Profile _myProfile;
    private readonly int _myTcpPort;
    private readonly int _udpPort;
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private Task? _listenerTask;
    private Task? _broadcastTask;
    private Task? _heartbeatTask;

    private readonly Dictionary<string, (HelloPacket Peer, IPAddress Address, DateTime LastSeen)> _knownPeers = [];
    private readonly object _lock = new();

    public event EventHandler<PeerDiscoveredEventArgs>? PeerDiscovered;
    public event EventHandler<string>? PeerLost;

    public DiscoveryService(Profile myProfile, int myTcpPort, int udpPort = DefaultDiscoveryPort)
    {
        _myProfile = myProfile;
        _myTcpPort = myTcpPort;
        _udpPort = udpPort;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();

        try
        {
            _udpClient = new UdpClient();
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.ExclusiveAddressUse = false;
            _udpClient.EnableBroadcast = true;
            _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, _udpPort));
        }
        catch (Exception)
        {
            // Secours si le port est bloqué
            _udpClient = new UdpClient(0) { EnableBroadcast = true };
        }

        _listenerTask = Task.Run(() => ListenLoopAsync(_cts.Token));
        _broadcastTask = Task.Run(() => BroadcastLoopAsync(_cts.Token));
        _heartbeatTask = Task.Run(() => PresenceCheckLoopAsync(_cts.Token));
    }

    public async Task SendQuickHellosAsync(int count = 3, int delayMs = 200)
    {
        for (int i = 0; i < count; i++)
        {
            await BroadcastHelloAsync();
            await Task.Delay(delayMs);
        }
    }

    public async Task BroadcastHelloAsync()
    {
        if (_udpClient == null) return;

        var hello = new HelloPacket
        {
            Type = "HELLO",
            UserId = _myProfile.UserId,
            Name = _myProfile.Name,
            TcpPort = _myTcpPort,
            AvatarHash = _myProfile.AvatarHash,
            Status = _myProfile.Status,
            PublicKey = _myProfile.PublicKey
        };

        var json = JsonSerializer.Serialize(hello);
        var bytes = Encoding.UTF8.GetBytes(json);

        var targets = new List<IPEndPoint>
        {
            new(IPAddress.Broadcast, _udpPort),
            new(IPAddress.Loopback, _udpPort)
        };

        foreach (var target in targets)
        {
            try
            {
                await _udpClient.SendAsync(bytes, bytes.Length, target);
            }
            catch { }
        }
    }

    public async Task SendByeAsync()
    {
        if (_udpClient == null) return;

        var bye = new ByePacket
        {
            Type = "BYE",
            UserId = _myProfile.UserId
        };

        var json = JsonSerializer.Serialize(bye);
        var bytes = Encoding.UTF8.GetBytes(json);

        var target = new IPEndPoint(IPAddress.Broadcast, _udpPort);
        try
        {
            await _udpClient.SendAsync(bytes, bytes.Length, target);
        }
        catch { }
    }

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _udpClient != null)
        {
            try
            {
                var result = await _udpClient.ReceiveAsync(ct);
                var json = Encoding.UTF8.GetString(result.Buffer);

                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("t", out var typeProp)) continue;

                var type = typeProp.GetString();
                if (type == "HELLO")
                {
                    var hello = JsonSerializer.Deserialize<HelloPacket>(json);
                    if (hello == null || hello.UserId == _myProfile.UserId) continue; // Ignorer ses propres paquets

                    lock (_lock)
                    {
                        _knownPeers[hello.UserId] = (hello, result.RemoteEndPoint.Address, DateTime.UtcNow);
                    }

                    PeerDiscovered?.Invoke(this, new PeerDiscoveredEventArgs(hello, result.RemoteEndPoint.Address));
                }
                else if (type == "BYE")
                {
                    var bye = JsonSerializer.Deserialize<ByePacket>(json);
                    if (bye == null || bye.UserId == _myProfile.UserId) continue;

                    lock (_lock)
                    {
                        _knownPeers.Remove(bye.UserId);
                    }

                    PeerLost?.Invoke(this, bye.UserId);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Journaliser sans crasher
            }
        }
    }

    private async Task BroadcastLoopAsync(CancellationToken ct)
    {
        // 3 HELLOs rapides au démarrage (§4.1)
        await SendQuickHellosAsync(3, 200);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(3000, ct); // Toutes les 3 secondes (§4.1)
                await BroadcastHelloAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch { }
        }
    }

    private async Task PresenceCheckLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(2000, ct);

                var now = DateTime.UtcNow;
                var lostPeerIds = new List<string>();

                lock (_lock)
                {
                    foreach (var (userId, (_, _, lastSeen)) in _knownPeers)
                    {
                        // Un contact est hors ligne si aucun HELLO depuis 10 secondes (§4.1)
                        if (now - lastSeen > TimeSpan.FromSeconds(10))
                        {
                            lostPeerIds.Add(userId);
                        }
                    }

                    foreach (var id in lostPeerIds)
                    {
                        _knownPeers.Remove(id);
                    }
                }

                foreach (var id in lostPeerIds)
                {
                    PeerLost?.Invoke(this, id);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch { }
        }
    }

    public void MarkPeerActive(string userId)
    {
        lock (_lock)
        {
            if (_knownPeers.TryGetValue(userId, out var entry))
            {
                _knownPeers[userId] = (entry.Peer, entry.Address, DateTime.UtcNow);
            }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        try
        {
            SendByeAsync().GetAwaiter().GetResult();
        }
        catch { }

        _udpClient?.Dispose();
        _cts?.Dispose();
    }
}
