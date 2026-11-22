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
    private readonly ReactionRepository? _reactionRepository;

    private readonly DiscoveryService _discoveryService;
    private readonly ConnectionManager _connectionManager;

    public event EventHandler<(string PeerUserId, Message Message)>? MessageReceived;
    public event EventHandler<(string PeerUserId, bool IsOnline)>? PeerPresenceChanged;
    public event EventHandler<string>? MessageDelivered;
    public event EventHandler<(string MessageId, string Status)>? MessageStatusChanged;
    public event EventHandler<(string PeerUserId, List<string> MessageIds)>? MessagesRead;
    public event EventHandler<(string PeerUserId, string State)>? PeerTypingChanged;
    public event EventHandler<(string MessageId, string NewText, DateTime EditedUtc)>? MessageEdited;
    public event EventHandler<(string MessageId, string Mode)>? MessageDeleted;
    public event EventHandler<(string MessageId, string UserId, string Emoji)>? ReactionChanged;

    public int TcpPort => _connectionManager.ListeningPort;
    public Profile MyProfile => _profile;

    public NetworkService(
        Profile profile,
        int tcpPort,
        ContactRepository contactRepository,
        ConversationRepository conversationRepository,
        MessageRepository messageRepository,
        ReactionRepository? reactionRepository = null,
        int udpPort = DiscoveryService.DefaultDiscoveryPort)
    {
        _profile = profile;
        _contactRepository = contactRepository;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _reactionRepository = reactionRepository;

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

            // Dépilement des messages en attente (§4.5)
            _ = FlushPendingMessagesAsync(peer.UserId);
        };

        // Reconnexion TCP réussie : vider la file d'attente
        _connectionManager.PeerConnected += (s, peerId) =>
        {
            _ = FlushPendingMessagesAsync(peerId);
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
            MessageStatusChanged?.Invoke(this, (ack.MessageId, "Delivered"));
        };

        // Réception des accusés de lecture (§4.4, §9.3)
        _connectionManager.AckReadReceived += (s, e) =>
        {
            var (peerId, ack) = e;
            foreach (var msgId in ack.MessageIds)
            {
                _messageRepository.UpdateStatus(msgId, "Read");
                MessageStatusChanged?.Invoke(this, (msgId, "Read"));
            }
            MessagesRead?.Invoke(this, (peerId, ack.MessageIds));
        };

        // Réception de l'état "en train d'écrire..." (§4.4, §7.4, §8)
        _connectionManager.TypingReceived += (s, e) =>
        {
            var (peerId, typing) = e;
            PeerTypingChanged?.Invoke(this, (peerId, typing.State));
        };

        // Réception de modification de message (§4.4, §9.7)
        _connectionManager.EditReceived += (s, e) =>
        {
            var (_, edit) = e;
            _messageRepository.UpdateText(edit.MessageId, edit.NewText, edit.EditedAtUtc);
            MessageEdited?.Invoke(this, (edit.MessageId, edit.NewText, edit.EditedAtUtc));
        };

        // Réception de suppression de message pour tous (§4.4, §9.6)
        _connectionManager.DeleteReceived += (s, e) =>
        {
            var (_, delete) = e;
            if (delete.Mode == "forEveryone")
            {
                _messageRepository.MarkDeleted(delete.MessageId);
                MessageDeleted?.Invoke(this, (delete.MessageId, delete.Mode));
            }
        };

        // Réception d'une réaction (§4.4, §9.8)
        _connectionManager.ReactionReceived += (s, e) =>
        {
            var (peerId, react) = e;
            _reactionRepository?.SetReaction(react.MessageId, peerId, react.Emoji);
            ReactionChanged?.Invoke(this, (react.MessageId, peerId, react.Emoji));
        };
    }

    public async Task<bool> SendEditMessageAsync(string peerUserId, string messageId, string newText)
    {
        var msg = _messageRepository.GetById(messageId);
        if (msg == null) return false;

        // Règle §9.7 : mes messages dans les 15 minutes
        if (DateTime.UtcNow - msg.CreatedUtc > TimeSpan.FromMinutes(15))
        {
            return false;
        }

        var now = DateTime.UtcNow;
        _messageRepository.UpdateText(messageId, newText, now);
        MessageEdited?.Invoke(this, (messageId, newText, now));

        var editPacket = new EditPacket
        {
            MessageId = messageId,
            NewText = newText,
            EditedAtUtc = now
        };

        return await _connectionManager.SendEditAsync(peerUserId, editPacket);
    }

    public async Task<bool> SendDeleteMessageAsync(string peerUserId, string messageId, bool forEveryone)
    {
        var msg = _messageRepository.GetById(messageId);
        if (msg == null) return false;

        if (forEveryone)
        {
            // Règle §9.6 : mes messages de moins de 60 minutes
            if (DateTime.UtcNow - msg.CreatedUtc > TimeSpan.FromMinutes(60))
            {
                return false;
            }

            _messageRepository.MarkDeleted(messageId);
            MessageDeleted?.Invoke(this, (messageId, "forEveryone"));

            var delPacket = new DeletePacket
            {
                MessageId = messageId,
                Mode = "forEveryone"
            };

            return await _connectionManager.SendDeleteAsync(peerUserId, delPacket);
        }
        else
        {
            _messageRepository.DeleteForMe(messageId);
            MessageDeleted?.Invoke(this, (messageId, "forMe"));
            return true;
        }
    }

    public async Task<bool> SendReactionAsync(string peerUserId, string messageId, string emoji)
    {
        _reactionRepository?.SetReaction(messageId, _profile.UserId, emoji);
        ReactionChanged?.Invoke(this, (messageId, _profile.UserId, emoji));

        var reactPacket = new ReactionPacket
        {
            MessageId = messageId,
            Emoji = emoji
        };

        return await _connectionManager.SendReactionAsync(peerUserId, reactPacket);
    }

    public bool ToggleStarred(string messageId)
    {
        return _messageRepository.ToggleStarred(messageId);
    }

    public async Task FlushPendingMessagesAsync(string peerUserId)
    {
        var contact = _contactRepository.GetById(peerUserId);
        if (contact == null || string.IsNullOrEmpty(contact.LastIp) || string.IsNullOrEmpty(contact.PublicKey))
        {
            return;
        }

        // On cherche les conversations associées à ce contact
        var pendingMessages = _messageRepository.GetPendingMessagesByConversation(peerUserId);
        if (pendingMessages.Count == 0) return;

        foreach (var msg in pendingMessages)
        {
            var packet = new MessagePacket
            {
                Id = msg.Id,
                ConversationId = msg.ConversationId,
                From = _profile.UserId,
                To = peerUserId,
                Text = msg.Body,
                ReplyToId = msg.ReplyToId,
                TimestampUtc = msg.CreatedUtc,
                Kind = msg.Kind
            };

            var sent = await _connectionManager.SendMessageAsync(
                peerUserId,
                contact.LastIp,
                contact.LastPort,
                contact.PublicKey,
                packet);

            if (sent)
            {
                _messageRepository.UpdateStatus(msg.Id, "Sent");
                MessageStatusChanged?.Invoke(this, (msg.Id, "Sent"));
            }
        }
    }

    public async Task SendAckReadAsync(string conversationId, string peerUserId, List<string> messageIds)
    {
        if (messageIds.Count == 0) return;

        // 1. Marquer localement comme lus
        _messageRepository.MarkMessagesAsRead(messageIds);

        // 2. Remettre le compteur de non-lus à zéro pour la conversation
        var conv = _conversationRepository.GetById(conversationId);
        if (conv != null && conv.UnreadCount > 0)
        {
            conv.UnreadCount = 0;
            _conversationRepository.Save(conv);
        }

        // 3. Envoyer le paquet au pair s'il est connecté
        var ack = new AckReadPacket
        {
            ConversationId = conversationId,
            MessageIds = messageIds,
            TimestampUtc = DateTime.UtcNow
        };

        await _connectionManager.SendAckReadAsync(peerUserId, ack);
    }

    public async Task SendTypingStateAsync(string conversationId, string peerUserId, string state)
    {
        var packet = new TypingPacket
        {
            ConversationId = conversationId,
            State = state
        };

        await _connectionManager.SendTypingAsync(peerUserId, packet);
    }

    public async Task<bool> SendTextMessageAsync(string conversationId, string peerUserId, string text, string? messageId = null, string? replyToId = null)
    {
        var contact = _contactRepository.GetById(peerUserId);
        if (contact == null || string.IsNullOrEmpty(contact.LastIp) || string.IsNullOrEmpty(contact.PublicKey))
        {
            return false;
        }

        var msgId = messageId ?? Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        var packet = new MessagePacket
        {
            Id = msgId,
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
            Id = msgId,
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
            conv.LastMessageId = msgId;
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
