using System.Collections.ObjectModel;
using System.Windows;
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
    private readonly ReactionRepository _reactionRepository;

    private ChatItem _chat;
    private string _messageText = string.Empty;
    private System.Timers.Timer? _typingStopTimer;
    private DateTime _lastTypingSentUtc = DateTime.MinValue;

    private ChatMessage? _quotingMessage;
    private ChatMessage? _editingMessage;

    // Propriétés de recherche dans la discussion
    private bool _isSearchOpen;
    private string _searchQuery = string.Empty;
    private readonly List<string> _searchResults = [];
    private int _currentSearchIndex = -1;
    private string _searchSummary = "0 sur 0";

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

    public ChatMessage? QuotingMessage
    {
        get => _quotingMessage;
        set
        {
            if (SetProperty(ref _quotingMessage, value))
            {
                OnPropertyChanged(nameof(HasQuotingMessage));
                OnPropertyChanged(nameof(QuotingAuthor));
                OnPropertyChanged(nameof(QuotingText));
            }
        }
    }

    public bool HasQuotingMessage => _quotingMessage != null;
    public string QuotingAuthor => _quotingMessage == null ? string.Empty : (_quotingMessage.IsOutgoing ? "Vous" : _chat.Name);
    public string QuotingText => _quotingMessage?.Text ?? string.Empty;

    public ChatMessage? EditingMessage
    {
        get => _editingMessage;
        set
        {
            if (SetProperty(ref _editingMessage, value))
            {
                OnPropertyChanged(nameof(IsEditing));
            }
        }
    }

    public bool IsEditing => _editingMessage != null;

    // Recherche
    public bool IsSearchOpen
    {
        get => _isSearchOpen;
        set
        {
            if (SetProperty(ref _isSearchOpen, value))
            {
                if (!value)
                {
                    ClearSearchHighlight();
                    SearchQuery = string.Empty;
                }
            }
        }
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                PerformSearch(value);
            }
        }
    }

    public string SearchSummary
    {
        get => _searchSummary;
        set => SetProperty(ref _searchSummary, value);
    }

    // Commandes principales
    public ICommand SendMessageCommand { get; }
    public ICommand CancelQuoteCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand CloseSearchCommand { get; }
    public ICommand NextSearchCommand { get; }
    public ICommand PrevSearchCommand { get; }
    public ICommand AudioCallCommand { get; }
    public ICommand VideoCallCommand { get; }

    // Commandes contextuelles sur un message
    public ICommand ReplyCommand { get; }
    public ICommand StartEditCommand { get; }
    public ICommand DeleteForEveryoneCommand { get; }
    public ICommand DeleteForMeCommand { get; }
    public ICommand ReactCommand { get; }
    public ICommand ReactThumbCommand { get; }
    public ICommand ReactHeartCommand { get; }
    public ICommand ReactJoyCommand { get; }
    public ICommand ReactSurprisedCommand { get; }
    public ICommand ReactSadCommand { get; }
    public ICommand ReactPrayCommand { get; }
    public ICommand ToggleStarCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand JumpToMessageCommand { get; }

    public ChatViewModel(
        ChatItem chat,
        MessageRepository messageRepository,
        ConversationRepository conversationRepository,
        ContactRepository contactRepository,
        ProfileService profileService,
        NetworkService networkService,
        ReactionRepository? reactionRepository = null)
    {
        _chat = chat;
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _contactRepository = contactRepository;
        _profileService = profileService;
        _networkService = networkService;
        _reactionRepository = reactionRepository ?? new ReactionRepository(new Database(string.Empty));

        SendMessageCommand = new RelayCommand(SendMessage, () => CanSend);
        CancelQuoteCommand = new RelayCommand(() => QuotingMessage = null);
        CancelEditCommand = new RelayCommand(() =>
        {
            EditingMessage = null;
            MessageText = string.Empty;
        });

        SearchCommand = new RelayCommand(() => IsSearchOpen = !IsSearchOpen);
        CloseSearchCommand = new RelayCommand(() => IsSearchOpen = false);
        NextSearchCommand = new RelayCommand(GoToNextSearchResult);
        PrevSearchCommand = new RelayCommand(GoToPrevSearchResult);

        AudioCallCommand = new RelayCommand(() => { });
        VideoCallCommand = new RelayCommand(() => { });

        ReplyCommand = new RelayCommand<ChatMessage>(msg =>
        {
            if (msg == null) return;
            EditingMessage = null;
            QuotingMessage = msg;
        });

        StartEditCommand = new RelayCommand<ChatMessage>(msg =>
        {
            if (msg == null || !msg.CanEdit) return;
            QuotingMessage = null;
            EditingMessage = msg;
            MessageText = msg.Text;
        });

        DeleteForEveryoneCommand = new RelayCommand<ChatMessage>(async msg =>
        {
            if (msg == null) return;
            await _networkService.SendDeleteMessageAsync(_chat.Id, msg.Id, forEveryone: true);
            msg.IsDeleted = true;
            msg.Text = "🚫 Vous avez supprimé ce message";
        });

        DeleteForMeCommand = new RelayCommand<ChatMessage>(async msg =>
        {
            if (msg == null) return;
            await _networkService.SendDeleteMessageAsync(_chat.Id, msg.Id, forEveryone: false);
            Messages.Remove(msg);
        });

        ReactCommand = new RelayCommand<(ChatMessage Message, string Emoji)>(async param =>
        {
            if (param.Message == null) return;
            await ToggleReactionInternalAsync(param.Message, param.Emoji);
        });

        ReactThumbCommand = new RelayCommand<ChatMessage>(async msg => { if (msg != null) await ToggleReactionInternalAsync(msg, "👍"); });
        ReactHeartCommand = new RelayCommand<ChatMessage>(async msg => { if (msg != null) await ToggleReactionInternalAsync(msg, "❤️"); });
        ReactJoyCommand = new RelayCommand<ChatMessage>(async msg => { if (msg != null) await ToggleReactionInternalAsync(msg, "😂"); });
        ReactSurprisedCommand = new RelayCommand<ChatMessage>(async msg => { if (msg != null) await ToggleReactionInternalAsync(msg, "😮"); });
        ReactSadCommand = new RelayCommand<ChatMessage>(async msg => { if (msg != null) await ToggleReactionInternalAsync(msg, "😢"); });
        ReactPrayCommand = new RelayCommand<ChatMessage>(async msg => { if (msg != null) await ToggleReactionInternalAsync(msg, "🙏"); });

        ToggleStarCommand = new RelayCommand<ChatMessage>(msg =>
        {
            if (msg == null) return;
            msg.IsStarred = _networkService.ToggleStarred(msg.Id);
        });

        CopyCommand = new RelayCommand<ChatMessage>(msg =>
        {
            if (msg == null || string.IsNullOrWhiteSpace(msg.Text)) return;
            Clipboard.SetText(msg.Text);
        });

        JumpToMessageCommand = new RelayCommand<string>(JumpToMessage);

        LoadMessages();
    }

    public void UpdateChat(ChatItem chat)
    {
        Chat = chat;
        QuotingMessage = null;
        EditingMessage = null;
        IsSearchOpen = false;
        LoadMessages();
    }

    private void LoadMessages()
    {
        Messages.Clear();
        var dbMessages = _messageRepository.GetByConversationId(_chat.Id);
        var myUserId = _profileService.CurrentProfile?.UserId;

        var messageDict = dbMessages.ToDictionary(m => m.Id);
        var reactionsDict = _reactionRepository.GetReactionsForMessages(dbMessages.Select(m => m.Id));

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

            string? quotedAuthor = null;
            string? quotedText = null;

            if (!string.IsNullOrEmpty(m.ReplyToId) && messageDict.TryGetValue(m.ReplyToId, out var repliedMsg))
            {
                quotedAuthor = repliedMsg.SenderId == myUserId ? "Vous" : _chat.Name;
                quotedText = repliedMsg.IsDeleted ? "Ce message a été supprimé" : repliedMsg.Body;
            }

            var reactionsSummary = FormatReactionsSummary(reactionsDict.TryGetValue(m.Id, out var rList) ? rList : []);

            Messages.Add(new ChatMessage
            {
                Id = m.Id,
                Text = m.IsDeleted ? (isOut ? "🚫 Vous avez supprimé ce message" : "🚫 Ce message a été supprimé") : m.Body,
                Time = m.CreatedUtc.ToLocalTime().ToString("HH:mm"),
                IsOutgoing = isOut,
                Status = m.Status,
                IsFirstInCluster = isFirstInCluster,
                SenderName = isOut ? "" : _chat.Name,
                ShowDateSeparator = showSeparator,
                DateSeparator = separatorText,
                CreatedUtc = m.CreatedUtc,
                IsEdited = m.EditedUtc.HasValue && !m.IsDeleted,
                EditedUtc = m.EditedUtc,
                IsDeleted = m.IsDeleted,
                IsStarred = m.IsStarred,
                ReplyToId = m.ReplyToId,
                QuotedAuthor = quotedAuthor,
                QuotedText = quotedText,
                ReactionsSummary = reactionsSummary
            });
        }

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

        string? quotedAuthor = null;
        string? quotedText = null;

        if (!string.IsNullOrEmpty(msg.ReplyToId))
        {
            var repliedMsg = _messageRepository.GetById(msg.ReplyToId);
            if (repliedMsg != null)
            {
                quotedAuthor = repliedMsg.SenderId == (_profileService.CurrentProfile?.UserId ?? "me") ? "Vous" : _chat.Name;
                quotedText = repliedMsg.IsDeleted ? "Ce message a été supprimé" : repliedMsg.Body;
            }
        }

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
            CreatedUtc = msg.CreatedUtc,
            ReplyToId = msg.ReplyToId,
            QuotedAuthor = quotedAuthor,
            QuotedText = quotedText
        });

        Chat.LastMessage = msg.Body;
        Chat.LastMessageTime = DateTimeFormatter.FormatChatListTime(msg.CreatedUtc);
        Chat.LastMessageStatus = "";

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

    public void HandleMessageEdited(string msgId, string newText, DateTime editedUtc)
    {
        var found = Messages.FirstOrDefault(m => m.Id == msgId);
        if (found != null && !found.IsDeleted)
        {
            found.Text = newText;
            found.IsEdited = true;
            found.EditedUtc = editedUtc;
        }
        if (Chat.LastMessage != null && Chat.Id == _chat.Id)
        {
            Chat.LastMessage = newText;
        }
    }

    public void HandleMessageDeleted(string msgId, string mode)
    {
        var found = Messages.FirstOrDefault(m => m.Id == msgId);
        if (found != null)
        {
            if (mode == "forEveryone")
            {
                found.IsDeleted = true;
                found.Text = found.IsOutgoing ? "🚫 Vous avez supprimé ce message" : "🚫 Ce message a été supprimé";
            }
            else
            {
                Messages.Remove(found);
            }
        }
    }

    public void HandleReactionChanged(string msgId, string userId, string emoji)
    {
        var found = Messages.FirstOrDefault(m => m.Id == msgId);
        if (found != null)
        {
            UpdateReactionsSummary(found);
        }
    }

    private async Task ToggleReactionInternalAsync(ChatMessage msg, string emoji)
    {
        var myUserId = _profileService.CurrentProfile?.UserId ?? "me";
        var currentReactions = _reactionRepository.GetReactionsByMessageId(msg.Id);
        var myExisting = currentReactions.FirstOrDefault(r => r.UserId == myUserId);

        var emojiToSend = (myExisting != null && myExisting.Emoji == emoji)
            ? string.Empty // Retrait si déjà présent
            : emoji;

        await _networkService.SendReactionAsync(_chat.Id, msg.Id, emojiToSend);
        UpdateReactionsSummary(msg);
    }

    private void UpdateReactionsSummary(ChatMessage msg)
    {
        var reactions = _reactionRepository.GetReactionsByMessageId(msg.Id);
        msg.ReactionsSummary = FormatReactionsSummary(reactions);
    }

    private static string FormatReactionsSummary(List<MessageReaction> reactions)
    {
        if (reactions.Count == 0) return string.Empty;

        var groups = reactions
            .GroupBy(r => r.Emoji)
            .OrderByDescending(g => g.Count())
            .Take(3);

        return string.Join(" ", groups.Select(g => $"{g.Key} {g.Count()}"));
    }

    private async void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(_messageText)) return;

        var text = _messageText.Trim();

        // 1. Cas Modification de message
        if (IsEditing && EditingMessage != null)
        {
            var msgToEdit = EditingMessage;
            EditingMessage = null;
            MessageText = string.Empty;

            await _networkService.SendEditMessageAsync(_chat.Id, msgToEdit.Id, text);
            msgToEdit.Text = text;
            msgToEdit.IsEdited = true;
            msgToEdit.EditedUtc = DateTime.UtcNow;
            Chat.LastMessage = text;
            return;
        }

        // 2. Cas Envoi normal ou Réponse
        var replyToId = QuotingMessage?.Id;
        var quotedAuthor = QuotingAuthor;
        var quotedText = QuotingText;

        QuotingMessage = null;
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
            CreatedUtc = now,
            ReplyToId = replyToId,
            QuotedAuthor = string.IsNullOrEmpty(replyToId) ? null : quotedAuthor,
            QuotedText = string.IsNullOrEmpty(replyToId) ? null : quotedText
        };

        Messages.Add(uiMessage);
        Chat.LastMessage = text;
        Chat.LastMessageTime = DateTimeFormatter.FormatChatListTime(now);
        Chat.LastMessageStatus = "Pending";

        if (isPeerOnNetwork)
        {
            var sent = await _networkService.SendTextMessageAsync(_chat.Id, _chat.Id, text, msgId, replyToId);
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
                ReplyToId = replyToId,
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

    private void PerformSearch(string query)
    {
        ClearSearchHighlight();
        _searchResults.Clear();
        _currentSearchIndex = -1;

        if (string.IsNullOrWhiteSpace(query))
        {
            SearchSummary = "0 sur 0";
            return;
        }

        var matching = Messages
            .Where(m => !m.IsDeleted && m.Text.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var m in matching)
        {
            _searchResults.Add(m.Id);
        }

        if (_searchResults.Count > 0)
        {
            _currentSearchIndex = 0;
            HighlightCurrentSearchResult();
        }

        UpdateSearchSummary();
    }

    private void GoToNextSearchResult()
    {
        if (_searchResults.Count == 0) return;
        ClearSearchHighlight();
        _currentSearchIndex = (_currentSearchIndex + 1) % _searchResults.Count;
        HighlightCurrentSearchResult();
        UpdateSearchSummary();
    }

    private void GoToPrevSearchResult()
    {
        if (_searchResults.Count == 0) return;
        ClearSearchHighlight();
        _currentSearchIndex = (_currentSearchIndex - 1 + _searchResults.Count) % _searchResults.Count;
        HighlightCurrentSearchResult();
        UpdateSearchSummary();
    }

    private void HighlightCurrentSearchResult()
    {
        if (_currentSearchIndex >= 0 && _currentSearchIndex < _searchResults.Count)
        {
            var msgId = _searchResults[_currentSearchIndex];
            JumpToMessage(msgId);
        }
    }

    private void ClearSearchHighlight()
    {
        foreach (var m in Messages)
        {
            m.IsHighlighted = false;
        }
    }

    private void UpdateSearchSummary()
    {
        if (_searchResults.Count == 0)
        {
            SearchSummary = "0 sur 0";
        }
        else
        {
            SearchSummary = $"{_currentSearchIndex + 1} sur {_searchResults.Count}";
        }
    }

    private void JumpToMessage(string? msgId)
    {
        if (string.IsNullOrEmpty(msgId)) return;
        var found = Messages.FirstOrDefault(m => m.Id == msgId);
        if (found != null)
        {
            found.IsHighlighted = true;
            // Clignote pendant 2 secondes (§9.5)
            Task.Delay(2000).ContinueWith(_ =>
            {
                Application.Current?.Dispatcher.Invoke(() => found.IsHighlighted = false);
            });
        }
    }
}
