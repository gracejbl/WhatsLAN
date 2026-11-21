using WhatsLAN.Core.Models;
using WhatsLAN.Core.Network;
using WhatsLAN.Core.Storage;

namespace WhatsLAN.Core.Services;

public class NetworkService : IDisposable
{
    private readonly Profile _profile;
    private readonly ContactRepository _contactRepository;
    private readonly ConversationRepository _conversationRepository;
    private readonly MessageRepository _messageRepository;

    private readonly DiscoveryService _discoveryService;
    private readonly ConnectionManager _connectionManager;

    public event EventHandler<(string PeerUserId, Message Message)>? MessageReceived;
    public event EventHandler<(string PeerUserId, bool IsOnline)>? PeerPresenceChanged;
    public event EventHandler<string>? MessageDelivered;

    public int TcpPort => _connectionManager.ListeningPort;
    public Profile MyProfile => _profile;

    public NetworkService(
        Profile profile,
        int tcpPort,
        ContactRepository contactRepository,
        ConversationRepository conversationRepository,
        MessageRepository messageRepository,
        int udpPort = DiscoveryService.DefaultDiscoveryPort)
    {
        _profile = profile;
        _contactRepository = contactRepository;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;

        _discoveryService = new DiscoveryService(_profile, tcpPort, udpPort);
        _connectionManager = new ConnectionManager(_profile, tcpPort);

        RegisterEvents();
    }

    public void Start()
    {
        _connectionManager.Start();
        _discoveryService.Start();
    }

    private void RegisterEvents()
    {
        // Découverte UDP d'un pair
        _discoveryService.PeerDiscovered += (s, e) =>
        {
            var peer = e.Peer;
            var ip = e.RemoteAddress.ToString();

            var contact = _contactRepository.GetById(peer.UserId) ?? new Contact
            {
                UserId = peer.UserId,
                Name = peer.Name,
                Status = peer.Status,
                AvatarHash = peer.AvatarHash,
                PublicKey = peer.PublicKey
            };

            contact.Name = peer.Name;
            contact.Status = peer.Status;
            contact.LastIp = ip;
            contact.LastPort = peer.TcpPort;
            contact.LastSeenUtc = DateTime.UtcNow;
            contact.PublicKey = peer.PublicKey;
            contact.IsOnline = true;

            _contactRepository.Save(contact);
            PeerPresenceChanged?.Invoke(this, (peer.UserId, true));

            // Si notre userId est plus petit que celui du pair, on initie la connexion TCP proactivement (§4.2)
            if (string.CompareOrdinal(_profile.UserId, peer.UserId) < 0)
            {
                _ = _connectionManager.GetOrCreateConnectionAsync(peer.UserId, ip, peer.TcpPort, peer.PublicKey);
            }
        };

        // Perte d'un pair (10s sans nouvelles)
        _discoveryService.PeerLost += (s, peerUserId) =>
        {
            _contactRepository.UpdatePresence(peerUserId, DateTime.UtcNow);
            _connectionManager.CloseConnection(peerUserId);
            PeerPresenceChanged?.Invoke(this, (peerUserId, false));
        };

        // Réception d'un message direct TCP
        _connectionManager.MessageReceived += async (s, e) =>
        {
            var (peerId, packet) = e;
            _discoveryService.MarkPeerActive(peerId);

            // Vérifier que le contact n'est pas bloqué (§16)
            var contact = _contactRepository.GetById(peerId);
            if (contact != null && contact.IsBlocked) return;

            // Dé-doublonnage par ID de message (§4.5, §20)
            var existing = _messageRepository.GetByConversationId(packet.ConversationId, 100);
            if (existing.Any(m => m.Id == packet.Id))
            {
                // Message déjà reçu, acquitter quand même
                await _connectionManager.SendAckDeliveredAsync(peerId, new AckDeliveredPacket
                {
                    MessageId = packet.Id,
                    TimestampUtc = DateTime.UtcNow
                });
                return;
            }

            var dbMessage = new Message
            {
                Id = packet.Id,
                ConversationId = packet.ConversationId,
                SenderId = packet.From,
                Kind = packet.Kind,
                Body = packet.Text,
                ReplyToId = packet.ReplyToId,
                Status = "Delivered",
                CreatedUtc = packet.TimestampUtc
            };

            _messageRepository.Insert(dbMessage);

            // Mise à jour ou création de la conversation
            var conv = _conversationRepository.GetById(packet.ConversationId);
            if (conv == null)
            {
                conv = new Conversation
                {
                    Id = packet.ConversationId,
                    Type = "direct",
                    PeerUserId = contact?.Name ?? peerId,
                    LastMessageId = packet.Id,
                    UnreadCount = 1,
                    UpdatedUtc = packet.TimestampUtc
                };
            }
            else
            {
                conv.LastMessageId = packet.Id;
                conv.UnreadCount += 1;
                conv.UpdatedUtc = packet.TimestampUtc;
            }
            _conversationRepository.Save(conv);

            // Envoi immédiat de l'accusé de réception (§4.4)
            await _connectionManager.SendAckDeliveredAsync(peerId, new AckDeliveredPacket
            {
                MessageId = packet.Id,
                TimestampUtc = DateTime.UtcNow
            });

            MessageReceived?.Invoke(this, (peerId, dbMessage));
        };

        // Réception de l'accusé de distribution
        _connectionManager.AckReceived += (s, e) =>
        {
            var (_, ack) = e;
            _messageRepository.UpdateStatus(ack.MessageId, "Delivered");
            MessageDelivered?.Invoke(this, ack.MessageId);
        };
    }

    public async Task<bool> SendTextMessageAsync(string conversationId, string peerUserId, string text, string? replyToId = null)
    {
        var contact = _contactRepository.GetById(peerUserId);
        if (contact == null || string.IsNullOrEmpty(contact.LastIp) || string.IsNullOrEmpty(contact.PublicKey))
        {
            return false;
        }

        var messageId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        var packet = new MessagePacket
        {
            Id = messageId,
            ConversationId = conversationId,
            From = _profile.UserId,
            To = peerUserId,
            Text = text,
            ReplyToId = replyToId,
            TimestampUtc = now,
            Kind = "text"
        };

        var sent = await _connectionManager.SendMessageAsync(
            peerUserId,
            contact.LastIp,
            contact.LastPort,
            contact.PublicKey,
            packet);

        var dbMessage = new Message
        {
            Id = messageId,
            ConversationId = conversationId,
            SenderId = _profile.UserId,
            Kind = "text",
            Body = text,
            ReplyToId = replyToId,
            Status = sent ? "Sent" : "Pending",
            CreatedUtc = now
        };

        _messageRepository.Insert(dbMessage);

        var conv = _conversationRepository.GetById(conversationId);
        if (conv != null)
        {
            conv.LastMessageId = messageId;
            conv.UpdatedUtc = now;
            _conversationRepository.Save(conv);
        }

        return sent;
    }

    public void Dispose()
    {
        _discoveryService.Dispose();
        _connectionManager.Dispose();
    }
}
