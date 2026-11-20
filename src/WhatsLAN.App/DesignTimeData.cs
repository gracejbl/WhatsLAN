using System.Collections.ObjectModel;
using WhatsLAN.App.Models;

namespace WhatsLAN.App;

/// <summary>
/// Données factices génériques et fictives autorisées UNIQUEMENT en Phase 0 (design).
/// Strictement isolées dans ce fichier, conformément à AGENTS.md.
/// </summary>
public static class DesignTimeData
{
    public static ObservableCollection<FilterPill> CreateFilterPills()
    {
        return
        [
            new FilterPill { Name = "Toutes", IsSelected = true },
            new FilterPill { Name = "Non lues", Count = 3, IsSelected = false },
            new FilterPill { Name = "Favoris", IsSelected = false },
            new FilterPill { Name = "Groupes ▾", IsSelected = false }
        ];
    }

    public static ObservableCollection<ChatItem> CreateChatItems()
    {
        return
        [
            new ChatItem
            {
                Id = "chat-1",
                Name = "Contact 1",
                AvatarInitial = "C1",
                AvatarColor = "#00A884",
                LastMessage = "Message de test reçu",
                LastMessageTime = "10:42",
                UnreadCount = 2,
                IsOnline = true,
                HasUnseenStatus = true,
                IsPinned = true,
                LastMessageStatus = ""
            },
            new ChatItem
            {
                Id = "chat-2",
                Name = "Groupe Projet",
                AvatarInitial = "GP",
                AvatarColor = "#7C4DFF",
                LastMessage = "~Contact 2: Aperçu du document prêt",
                LastMessageTime = "09:15",
                UnreadCount = 0,
                IsGroup = true,
                IsPinned = true,
                LastMessageStatus = "Read"
            },
            new ChatItem
            {
                Id = "chat-3",
                Name = "Contact 2",
                AvatarInitial = "C2",
                AvatarColor = "#1F7AA3",
                LastMessage = "À tout à l'heure sur le réseau local",
                LastMessageTime = "Hier",
                UnreadCount = 0,
                IsOnline = false,
                LastMessageStatus = "Read"
            },
            new ChatItem
            {
                Id = "chat-4",
                Name = "Contact 3",
                AvatarInitial = "C3",
                AvatarColor = "#B33F7B",
                LastMessage = "Test de message avec pièce jointe",
                LastMessageTime = "18/11/2026",
                UnreadCount = 0,
                IsMuted = true,
                LastMessageStatus = "Delivered"
            },
            new ChatItem
            {
                Id = "chat-5",
                Name = "Contact 4",
                AvatarInitial = "C4",
                AvatarColor = "#E0653D",
                LastMessage = "Connexion P2P directe établie",
                LastMessageTime = "17/11/2026",
                UnreadCount = 0,
                LastMessageStatus = "Sent"
            }
        ];
    }

    public static ObservableCollection<ChatMessage> CreateMessagesForChat(string chatId)
    {
        return
        [
            new ChatMessage
            {
                Id = "msg-1",
                Text = "Bonjour ! Bienvenue sur la maquette WhatsLAN.",
                Time = "10:35",
                IsOutgoing = false,
                IsFirstInCluster = true,
                SenderName = "Contact 1",
                SenderColor = "#00A884"
            },
            new ChatMessage
            {
                Id = "msg-2",
                Text = "Ceci est un second message consécutif du même contact pour valider l'espacement réduit et l'absence de queue de bulle.",
                Time = "10:36",
                IsOutgoing = false,
                IsFirstInCluster = false,
                SenderName = "Contact 1",
                SenderColor = "#00A884"
            },
            new ChatMessage
            {
                Id = "msg-3",
                Text = "Réponse envoyée depuis cette instance de test.",
                Time = "10:38",
                IsOutgoing = true,
                IsFirstInCluster = true,
                Status = "Read",
                QuotedAuthor = "Contact 1",
                QuotedText = "Bonjour ! Bienvenue sur la maquette WhatsLAN."
            },
            new ChatMessage
            {
                Id = "msg-4",
                Text = "Le chiffrement E2E et le style visuel de WhatsApp Desktop sont respectés fidèlement.",
                Time = "10:40",
                IsOutgoing = true,
                IsFirstInCluster = false,
                Status = "Read"
            },
            new ChatMessage
            {
                Id = "msg-5",
                Text = "Message de test reçu avec accusé et horodatage.",
                Time = "10:42",
                IsOutgoing = false,
                IsFirstInCluster = true,
                SenderName = "Contact 1",
                SenderColor = "#00A884"
            }
        ];
    }
}
