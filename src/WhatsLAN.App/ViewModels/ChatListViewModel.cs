using System.Collections.ObjectModel;
using System.Windows.Input;
using WhatsLAN.App.Models;
using WhatsLAN.App.MVVM;
using WhatsLAN.Core.Models;
using WhatsLAN.Core.Services;
using WhatsLAN.Core.Storage;

namespace WhatsLAN.App.ViewModels;

public class ChatListViewModel : ObservableObject
{
    private readonly ConversationRepository _conversationRepository;
    private readonly MessageRepository _messageRepository;
    private readonly ProfileService _profileService;
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
        ProfileService profileService)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _profileService = profileService;

        InitializeFilterPills();
        _selectedFilter = FilterPills.FirstOrDefault();

        SelectFilterCommand = new RelayCommand<FilterPill>(pill =>
        {
            if (pill != null)
            {
                SelectedFilter = pill;
            }
        });

        NewChatCommand = new RelayCommand(CreateNewDirectChat);

        LoadConversationsFromDb();
    }

    private void InitializeFilterPills()
    {
        FilterPills.Add(new FilterPill { Name = "Toutes", IsSelected = true });
        FilterPills.Add(new FilterPill { Name = "Non lues", Count = 0, IsSelected = false });
        FilterPills.Add(new FilterPill { Name = "Favoris", IsSelected = false });
        FilterPills.Add(new FilterPill { Name = "Groupes ▾", IsSelected = false });
    }

    public void LoadConversationsFromDb()
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
        if (Chats.Count > 0 && SelectedChat == null)
        {
            SelectedChat = Chats[0];
        }
    }

    private ChatItem MapToChatItem(Conversation conv)
    {
        // Récupérer le dernier message si existant
        var lastMsg = string.IsNullOrEmpty(conv.LastMessageId) ? null : _messageRepository.GetByConversationId(conv.Id, 1).LastOrDefault();

        var displayName = !string.IsNullOrEmpty(conv.PeerUserId)
            ? conv.PeerUserId
            : (conv.Type == "group" ? "Groupe" : "Discussion");

        var initials = displayName.Length >= 2 ? displayName[..2].ToUpper() : displayName.ToUpper();

        return new ChatItem
        {
            Id = conv.Id,
            Name = displayName,
            AvatarInitial = initials,
            AvatarColor = "#00A884",
            LastMessage = lastMsg?.Body ?? "Discussion créée",
            LastMessageTime = lastMsg?.CreatedUtc.ToLocalTime().ToString("HH:mm") ?? conv.UpdatedUtc.ToLocalTime().ToString("HH:mm"),
            UnreadCount = conv.UnreadCount,
            IsPinned = conv.Pinned,
            IsMuted = conv.Muted,
            IsGroup = conv.Type == "group",
            LastMessageStatus = lastMsg?.Status ?? ""
        };
    }

    private void CreateNewDirectChat()
    {
        // Créer une nouvelle discussion dans la base SQLite locale
        var convId = Guid.NewGuid().ToString();
        var convNumber = _allChats.Count + 1;
        var peerName = $"Contact {convNumber}";

        var conversation = new Conversation
        {
            Id = convId,
            Type = "direct",
            PeerUserId = peerName,
            Pinned = false,
            Muted = false,
            Archived = false,
            UnreadCount = 0,
            UpdatedUtc = DateTime.UtcNow
        };

        _conversationRepository.Save(conversation);

        var chatItem = MapToChatItem(conversation);
        _allChats.Insert(0, chatItem);
        Chats.Insert(0, chatItem);

        SelectedChat = chatItem;
        OnPropertyChanged(nameof(HasChats));
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

        foreach (var chat in query)
        {
            Chats.Add(chat);
        }

        OnPropertyChanged(nameof(HasChats));
    }
}
