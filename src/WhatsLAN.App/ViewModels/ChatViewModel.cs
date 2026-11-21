using System.Collections.ObjectModel;
using System.Windows.Input;
using WhatsLAN.App.Models;
using WhatsLAN.App.MVVM;
using WhatsLAN.Core.Models;
using WhatsLAN.Core.Services;
using WhatsLAN.Core.Storage;

namespace WhatsLAN.App.ViewModels;

public class ChatViewModel : ObservableObject
{
    private readonly MessageRepository _messageRepository;
    private readonly ConversationRepository _conversationRepository;
    private readonly ContactRepository _contactRepository;
    private readonly ProfileService _profileService;
    private readonly NetworkService _networkService;

    private ChatItem _chat;
    private string _messageText = string.Empty;

    public ChatItem Chat
    {
        get => _chat;
        set => SetProperty(ref _chat, value);
    }

    public ObservableCollection<ChatMessage> Messages { get; } = [];

    public string MessageText
    {
        get => _messageText;
        set
        {
            if (SetProperty(ref _messageText, value))
            {
                OnPropertyChanged(nameof(CanSend));
            }
        }
    }

    public bool CanSend => !string.IsNullOrWhiteSpace(_messageText);

    public ICommand SendMessageCommand { get; }
    public ICommand AudioCallCommand { get; }
    public ICommand VideoCallCommand { get; }
    public ICommand SearchCommand { get; }

    public ChatViewModel(
        ChatItem chat,
        MessageRepository messageRepository,
        ConversationRepository conversationRepository,
        ContactRepository contactRepository,
        ProfileService profileService,
        NetworkService networkService)
    {
        _chat = chat;
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _contactRepository = contactRepository;
        _profileService = profileService;
        _networkService = networkService;

        SendMessageCommand = new RelayCommand(SendMessage, () => CanSend);
        AudioCallCommand = new RelayCommand(() => { });
        VideoCallCommand = new RelayCommand(() => { });
        SearchCommand = new RelayCommand(() => { });

        LoadMessages();
    }

    public void UpdateChat(ChatItem chat)
    {
        Chat = chat;
        LoadMessages();
    }

    private void LoadMessages()
    {
        Messages.Clear();
        var dbMessages = _messageRepository.GetByConversationId(_chat.Id);
        var myUserId = _profileService.CurrentProfile?.UserId;

        for (int i = 0; i < dbMessages.Count; i++)
        {
            var m = dbMessages[i];
            var isOut = m.SenderId == myUserId || m.SenderId == "me";
            var isFirstInCluster = i == 0 || (dbMessages[i - 1].SenderId != m.SenderId);

            Messages.Add(new ChatMessage
            {
                Id = m.Id,
                Text = m.Body,
                Time = m.CreatedUtc.ToLocalTime().ToString("HH:mm"),
                IsOutgoing = isOut,
                Status = m.Status,
                IsFirstInCluster = isFirstInCluster,
                SenderName = isOut ? "" : _chat.Name
            });
        }
    }

    public void AddIncomingMessage(Message msg)
    {
        var isFirstInCluster = Messages.Count == 0 || Messages[^1].IsOutgoing;
        Messages.Add(new ChatMessage
        {
            Id = msg.Id,
            Text = msg.Body,
            Time = msg.CreatedUtc.ToLocalTime().ToString("HH:mm"),
            IsOutgoing = false,
            Status = "Delivered",
            IsFirstInCluster = isFirstInCluster,
            SenderName = _chat.Name
        });

        Chat.LastMessage = msg.Body;
        Chat.LastMessageTime = msg.CreatedUtc.ToLocalTime().ToString("HH:mm");
        Chat.LastMessageStatus = "";
    }

    public void UpdateMessageStatus(string msgId, string status)
    {
        var found = Messages.FirstOrDefault(m => m.Id == msgId);
        if (found != null)
        {
            found.Status = status;
        }
        if (Chat.LastMessageStatus == "Sent" || Chat.LastMessageStatus == "Pending")
        {
            Chat.LastMessageStatus = status;
        }
    }

    private async void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(_messageText)) return;

        var text = _messageText.Trim();
        MessageText = string.Empty;

        var myUserId = _profileService.CurrentProfile?.UserId ?? "me";
        var msgId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        var isPeerOnNetwork = _contactRepository.GetById(_chat.Id) != null;

        var isFirstInCluster = Messages.Count == 0 || !Messages[^1].IsOutgoing;
        var uiMessage = new ChatMessage
        {
            Id = msgId,
            Text = text,
            Time = now.ToLocalTime().ToString("HH:mm"),
            IsOutgoing = true,
            Status = "Pending",
            IsFirstInCluster = isFirstInCluster
        };

        Messages.Add(uiMessage);
        Chat.LastMessage = text;
        Chat.LastMessageTime = uiMessage.Time;
        Chat.LastMessageStatus = "Pending";

        if (isPeerOnNetwork)
        {
            // Envoi réel sur le réseau P2P chiffré via TCP
            var sent = await _networkService.SendTextMessageAsync(_chat.Id, _chat.Id, text);
            if (sent)
            {
                uiMessage.Status = "Sent";
                Chat.LastMessageStatus = "Sent";
            }
        }
        else
        {
            // Message local si discussion manuelle sans pair réseau direct
            var dbMessage = new Message
            {
                Id = msgId,
                ConversationId = _chat.Id,
                SenderId = myUserId,
                Kind = "text",
                Body = text,
                Status = "Sent",
                CreatedUtc = now
            };
            _messageRepository.Insert(dbMessage);

            var conv = _conversationRepository.GetById(_chat.Id);
            if (conv != null)
            {
                conv.LastMessageId = msgId;
                conv.UpdatedUtc = now;
                _conversationRepository.Save(conv);
            }

            uiMessage.Status = "Sent";
            Chat.LastMessageStatus = "Sent";
        }
    }
}
