using System.Collections.ObjectModel;
using System.Windows.Input;
using WhatsLAN.App.Models;
using WhatsLAN.App.MVVM;
using WhatsLAN.Core.Models;
using WhatsLAN.Core.Services;
using WhatsLAN.Core.Storage;
using WhatsLAN.Core.Utils;

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
    private System.Timers.Timer? _typingStopTimer;
    private DateTime _lastTypingSentUtc = DateTime.MinValue;

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
                HandleUserTyping();
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

        DateTime? prevDate = null;

        for (int i = 0; i < dbMessages.Count; i++)
        {
            var m = dbMessages[i];
            var isOut = m.SenderId == myUserId || m.SenderId == "me";
            var isFirstInCluster = i == 0 || (dbMessages[i - 1].SenderId != m.SenderId);

            var msgLocalDate = m.CreatedUtc.ToLocalTime().Date;
            var showSeparator = prevDate == null || prevDate.Value != msgLocalDate;
            var separatorText = showSeparator ? DateTimeFormatter.FormatDateSeparator(m.CreatedUtc) : string.Empty;
            prevDate = msgLocalDate;

            Messages.Add(new ChatMessage
            {
                Id = m.Id,
                Text = m.Body,
                Time = m.CreatedUtc.ToLocalTime().ToString("HH:mm"),
                IsOutgoing = isOut,
                Status = m.Status,
                IsFirstInCluster = isFirstInCluster,
                SenderName = isOut ? "" : _chat.Name,
                ShowDateSeparator = showSeparator,
                DateSeparator = separatorText,
                CreatedUtc = m.CreatedUtc
            });
        }

        // Remise à zéro des non-lus et envoi des accusés de lecture (§7.4, §9.3)
        SendReadReceiptsForUnread();
    }

    private void SendReadReceiptsForUnread()
    {
        var myUserId = _profileService.CurrentProfile?.UserId ?? "me";
        var unreadIds = _messageRepository.GetUnreadIncomingMessageIds(_chat.Id, myUserId);
        if (unreadIds.Count > 0)
        {
            _ = _networkService.SendAckReadAsync(_chat.Id, _chat.Id, unreadIds);
        }

        if (_chat.UnreadCount > 0)
        {
            _chat.UnreadCount = 0;
            var conv = _conversationRepository.GetById(_chat.Id);
            if (conv != null)
            {
                conv.UnreadCount = 0;
                _conversationRepository.Save(conv);
            }
        }
    }

    private void HandleUserTyping()
    {
        if (string.IsNullOrWhiteSpace(_messageText))
        {
            SendTypingStop();
            return;
        }

        var now = DateTime.UtcNow;
        if ((now - _lastTypingSentUtc).TotalSeconds > 2.5)
        {
            _lastTypingSentUtc = now;
            _ = _networkService.SendTypingStateAsync(_chat.Id, _chat.Id, "start");
        }

        if (_typingStopTimer == null)
        {
            _typingStopTimer = new System.Timers.Timer(2500) { AutoReset = false };
            _typingStopTimer.Elapsed += (s, e) => SendTypingStop();
        }
        else
        {
            _typingStopTimer.Stop();
        }
        _typingStopTimer.Start();
    }

    private void SendTypingStop()
    {
        _typingStopTimer?.Stop();
        _lastTypingSentUtc = DateTime.MinValue;
        _ = _networkService.SendTypingStateAsync(_chat.Id, _chat.Id, "stop");
    }

    public void AddIncomingMessage(Message msg)
    {
        var isFirstInCluster = Messages.Count == 0 || Messages[^1].IsOutgoing;
        var msgLocalDate = msg.CreatedUtc.ToLocalTime().Date;
        var showSeparator = Messages.Count == 0 || Messages[^1].CreatedUtc.ToLocalTime().Date != msgLocalDate;
        var separatorText = showSeparator ? DateTimeFormatter.FormatDateSeparator(msg.CreatedUtc) : string.Empty;

        Messages.Add(new ChatMessage
        {
            Id = msg.Id,
            Text = msg.Body,
            Time = msg.CreatedUtc.ToLocalTime().ToString("HH:mm"),
            IsOutgoing = false,
            Status = "Read",
            IsFirstInCluster = isFirstInCluster,
            SenderName = _chat.Name,
            ShowDateSeparator = showSeparator,
            DateSeparator = separatorText,
            CreatedUtc = msg.CreatedUtc
        });

        Chat.LastMessage = msg.Body;
        Chat.LastMessageTime = DateTimeFormatter.FormatChatListTime(msg.CreatedUtc);
        Chat.LastMessageStatus = "";

        // Si la discussion est ouverte et active à l'écran, acquitter immédiatement en lecture (§9.3)
        _ = _networkService.SendAckReadAsync(_chat.Id, _chat.Id, [msg.Id]);
    }

    public void UpdateMessageStatus(string msgId, string status)
    {
        var found = Messages.FirstOrDefault(m => m.Id == msgId);
        if (found != null)
        {
            found.Status = status;
        }
        if (Chat.LastMessageStatus == "Sent" || Chat.LastMessageStatus == "Pending" || Chat.LastMessageStatus == "Delivered")
        {
            Chat.LastMessageStatus = status;
        }
    }

    private async void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(_messageText)) return;

        var text = _messageText.Trim();
        SendTypingStop();
        MessageText = string.Empty;

        var myUserId = _profileService.CurrentProfile?.UserId ?? "me";
        var msgId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        var isPeerOnNetwork = _contactRepository.GetById(_chat.Id) != null;

        var isFirstInCluster = Messages.Count == 0 || !Messages[^1].IsOutgoing;
        var msgLocalDate = now.ToLocalTime().Date;
        var showSeparator = Messages.Count == 0 || Messages[^1].CreatedUtc.ToLocalTime().Date != msgLocalDate;
        var separatorText = showSeparator ? DateTimeFormatter.FormatDateSeparator(now) : string.Empty;

        var uiMessage = new ChatMessage
        {
            Id = msgId,
            Text = text,
            Time = now.ToLocalTime().ToString("HH:mm"),
            IsOutgoing = true,
            Status = "Pending",
            IsFirstInCluster = isFirstInCluster,
            ShowDateSeparator = showSeparator,
            DateSeparator = separatorText,
            CreatedUtc = now
        };

        Messages.Add(uiMessage);
        Chat.LastMessage = text;
        Chat.LastMessageTime = DateTimeFormatter.FormatChatListTime(now);
        Chat.LastMessageStatus = "Pending";

        if (isPeerOnNetwork)
        {
            var sent = await _networkService.SendTextMessageAsync(_chat.Id, _chat.Id, text, msgId);
            if (sent)
            {
                uiMessage.Status = "Sent";
                Chat.LastMessageStatus = "Sent";
            }
        }
        else
        {
            var dbMessage = new Message
            {
                Id = msgId,
                ConversationId = _chat.Id,
                SenderId = myUserId,
                Kind = "text",
                Body = text,
                Status = "Pending",
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

            uiMessage.Status = "Pending";
            Chat.LastMessageStatus = "Pending";
        }
    }
}
