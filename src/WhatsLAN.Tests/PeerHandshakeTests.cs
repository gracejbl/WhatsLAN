using System.Net;
using System.Net.Sockets;
using System.Text;
using WhatsLAN.Core.Models;
using WhatsLAN.Core.Network;
using WhatsLAN.Core.Security;
using Xunit;

namespace WhatsLAN.Tests;

public class PeerHandshakeTests
{
    [Fact]
    public async Task Handshake_EstablishesSharedAesKeyAndTransfersEncryptedPacket()
    {
        var aliceKeys = KeyManager.GenerateEcdhKeyPair();
        var aliceProfile = new Profile
        {
            UserId = Guid.NewGuid().ToString(),
            Name = "Alice",
            PrivateKey = aliceKeys.PrivateKeyBase64,
            PublicKey = aliceKeys.PublicKeyBase64
        };

        var bobKeys = KeyManager.GenerateEcdhKeyPair();
        var bobProfile = new Profile
        {
            UserId = Guid.NewGuid().ToString(),
            Name = "Bob",
            PrivateKey = bobKeys.PrivateKeyBase64,
            PublicKey = bobKeys.PublicKeyBase64
        };

        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var serverTask = Task.Run(async () =>
        {
            using var serverClient = await listener.AcceptTcpClientAsync();
            using var bobConn = new PeerConnection(serverClient, bobProfile);
            await bobConn.PerformInboundHandshakeAsync();

            var tcs = new TaskCompletionSource<string>();
            bobConn.PacketReceived += (s, e) =>
            {
                if (e.Type == PacketType.Msg)
                {
                    tcs.SetResult(Encoding.UTF8.GetString(e.Payload));
                }
            };

            var msg = await tcs.Task;
            Assert.Equal("Hello Bob!", msg);
            Assert.NotNull(bobConn.SharedAesKey);
            return bobConn.SharedAesKey;
        });

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        using var aliceConn = new PeerConnection(client, aliceProfile);
        await aliceConn.PerformOutboundHandshakeAsync(bobProfile.PublicKey);

        Assert.True(aliceConn.IsAuthenticated);
        Assert.NotNull(aliceConn.SharedAesKey);

        await aliceConn.SendPacketAsync(PacketType.Msg, Encoding.UTF8.GetBytes("Hello Bob!"));

        var bobKey = await serverTask;
        listener.Stop();

        Assert.Equal(aliceConn.SharedAesKey, bobKey);
    }
}
