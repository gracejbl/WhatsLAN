using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WhatsLAN.App.Models;
using WhatsLAN.App.MVVM;
using WhatsLAN.App.Views;
using WhatsLAN.Core.Models;
using WhatsLAN.Core.Services;
using WhatsLAN.Core.Storage;
using WhatsLAN.Core.Utils;

namespace WhatsLAN.App.ViewModels;

public class ChatListViewModel : ObservableObject
{
    private readonly ConversationRepository _conversationRepository;
    private readonly MessageRepository _messageRepository;
    private readonly ContactRepository _contactRepository;
    private readonly ProfileService _profileService;
    private readonly NetworkService _networkService;

    private string _searchText = string.Empty;
    private ChatItem? _selectedChat;
    private FilterPill? _selectedFilter;
    private readonly List<ChatItem> _allChats = [];

    public ObservableCollection<FilterPill> FilterPills { get; } = [];
    public ObservableCollection<ChatItem> Chats { get; } = [];

    public event Action<ChatItem?>? SelectedChatChanged;

    public bool HasChats => Chats.Count > 0;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilters();
            }
        }
    }

    public ChatItem? SelectedChat
    {
        get => _selectedChat;
        set
        {
            if (_selectedChat != null) _selectedChat.IsSelected = false;
            if (SetProperty(ref _selectedChat, value))
            {
                if (_selectedChat != null) _selectedChat.IsSelected = true;
                SelectedChatChanged?.Invoke(_selectedChat);
            }
        }
    }

    public FilterPill? SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (_selectedFilter != null) _selectedFilter.IsSelected = false;
            if (SetProperty(ref _selectedFilter, value))
            {
                if (_selectedFilter != null) _selectedFilter.IsSelected = true;
                ApplyFilters();
            }
        }
    }

    public ICommand SelectFilterCommand { get; }
    public ICommand NewChatCommand { get; }

    public ChatListViewModel(
        ConversationRepository conversationRepository,
        MessageRepository messageRepository,
        ContactRepository contactRepository,
        ProfileService profileService,
        NetworkService networkService)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _contactRepository = contactRepository;
        _profileService = profileService;
        _networkService = networkService;

        InitializeFilterPills();
        _selectedFilter = FilterPills.FirstOrDefault();

        SelectFilterCommand = new RelayCommand<FilterPill>(pill =>
        {
            if (pill != null)
            {
                SelectedFilter = pill;
            }
        });

        NewChatCommand = new RelayCommand(OpenNewChatDialog);

        LoadConversationsFromDb();
    }

    private void InitializeFilterPills()
    {
        FilterPills.Add(new FilterPill { Name = "Toutes", IsSelected = true });
        FilterPills.Add(new FilterPill { Name = "Non lues", Count = 0, IsSelected = false });
        FilterPills.Add(new FilterPill { Name = "Favoris", IsSelected = false });
        FilterPills.Add(new FilterPill { Name = "Groupes ▾", IsSelected = false });
    }

    public void LoadConversationsFromDb(string? selectConversationId = null)
    {
        _allChats.Clear();
        Chats.Clear();

        var dbConversations = _conversationRepository.GetAll();
        foreach (var conv in dbConversations)
        {
            var chatItem = MapToChatItem(conv);
            _allChats.Add(chatItem);
            Chats.Add(chatItem);
        }

        OnPropertyChanged(nameof(HasChats));

        if (!string.IsNullOrEmpty(selectConversationId))
        {
            SelectedChat = Chats.FirstOrDefault(c => c.Id == selectConversationId);
        }
        else if (Chats.Count > 0 && SelectedChat == null)
        {
            SelectedChat = Chats[0];
        }
    }

    public void UpdatePeerPresence(string peerUserId, bool isOnline)
    {
        var existing = _allChats.FirstOrDefault(c => c.Id == peerUserId);
        var contact = _contactRepository.GetById(peerUserId);

        if (existing == null && contact != null)
        {
            var conversation = _conversationRepository.GetById(peerUserId) ?? new Conversation
            {
                Id = peerUserId,
                Type = "direct",
                PeerUserId = contact.Name,
                Pinned = false,
                Muted = false,
                Archived = false,
                UnreadCount = 0,
                UpdatedUtc = DateTime.UtcNow
            };
            _conversationRepository.Save(conversation);

            existing = MapToChatItem(conversation);
            existing.IsOnline = isOnline;
            existing.LastSeenUtc = contact.LastSeenUtc;
            existing.UpdateSubtitle();

            _allChats.Insert(0, existing);
            Chats.Insert(0, existing);
            OnPropertyChanged(nameof(HasChats));

            if (SelectedChat == null)
            {
                SelectedChat = existing;
            }
            return;
        }

        if (existing != null)
        {
            existing.IsOnline = isOnline;
            if (contact != null)
            {
                existing.Name = contact.Name;
                existing.LastSeenUtc = contact.LastSeenUtc;
                existing.UpdateSubtitle();
            }
        }
    }

    public void HandleIncomingMessage(string peerUserId, Message msg)
    {
        var existing = _allChats.FirstOrDefault(c => c.Id == peerUserId);
        var contact = _contactRepository.GetById(peerUserId);

        if (existing == null)
        {
            UpdatePeerPresence(peerUserId, true);
            existing = _allChats.FirstOrDefault(c => c.Id == peerUserId);
        }

        if (existing != null)
        {
            existing.LastMessage = msg.Body;
            existing.LastMessageTime = DateTimeFormatter.FormatChatListTime(msg.CreatedUtc);
            existing.LastMessageStatus = "";
            if (SelectedChat?.Id != peerUserId)
            {
                existing.UnreadCount += 1;
            }

            // Remonter en haut de liste (§7.4)
            _allChats.Remove(existing);
            _allChats.Insert(0, existing);

            Chats.Remove(existing);
            Chats.Insert(0, existing);
        }
    }

    private ChatItem MapToChatItem(Conversation conv)
    {
        var lastMsg = string.IsNullOrEmpty(conv.LastMessageId) ? null : _messageRepository.GetByConversationId(conv.Id, 1).LastOrDefault();
        var contact = _contactRepository.GetById(conv.Id);
        var displayName = contact?.Name ?? conv.PeerUserId ?? "Contact";
        var initials = displayName.Length >= 2 ? displayName[..2].ToUpper() : displayName.ToUpper();

        var timeToFormat = lastMsg?.CreatedUtc ?? conv.UpdatedUtc;
        var formattedTime = DateTimeFormatter.FormatChatListTime(timeToFormat);

        var isOnline = contact?.IsOnline ?? false;
        var lastSeen = contact?.LastSeenUtc;

        var chatItem = new ChatItem
        {
            Id = conv.Id,
            Name = displayName,
            AvatarInitial = initials,
            AvatarColor = "#00A884",
            LastMessage = lastMsg?.Body ?? "Discussion créée",
            LastMessageTime = formattedTime,
            UnreadCount = conv.UnreadCount,
            IsPinned = conv.Pinned,
            IsMuted = conv.Muted,
            IsGroup = conv.Type == "group",
            LastMessageStatus = lastMsg?.Status ?? "",
            IsOnline = isOnline,
            LastSeenUtc = lastSeen
        };

        chatItem.UpdateSubtitle();
        return chatItem;
    }

    private void OpenNewChatDialog()
    {
        var vm = new NewChatViewModel(_contactRepository, _conversationRepository);
        var win = new NewChatWindow(vm)
        {
            Owner = Application.Current.MainWindow
        };

        var result = win.ShowDialog();
        if (result == true && !string.IsNullOrEmpty(win.SelectedConversationId))
        {
            LoadConversationsFromDb(win.SelectedConversationId);
        }
    }

    private void ApplyFilters()
    {
        Chats.Clear();
        var query = _allChats.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            query = query.Where(c => c.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                                     c.LastMessage.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        }

        if (_selectedFilter != null)
        {
            if (_selectedFilter.Name.StartsWith("Non lues", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(c => c.HasUnread);
            }
            else if (_selectedFilter.Name.StartsWith("Favoris", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(c => c.IsPinned);
            }
            else if (_selectedFilter.Name.StartsWith("Groupes", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(c => c.IsGroup);
            }
        }

        // Tri : épinglées en premier, puis date récente (§7.4)
        query = query.OrderByDescending(c => c.IsPinned);

        foreach (var chat in query)
        {
            Chats.Add(chat);
        }

        OnPropertyChanged(nameof(HasChats));
    }
}
