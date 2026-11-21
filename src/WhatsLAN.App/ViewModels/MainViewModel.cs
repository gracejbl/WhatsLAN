using System.Windows.Input;
using WhatsLAN.App.Models;
using WhatsLAN.App.MVVM;
using WhatsLAN.Core.Services;
using WhatsLAN.Core.Storage;

namespace WhatsLAN.App.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly Database _database;
    private readonly ProfileService _profileService;
    private readonly ConversationRepository _conversationRepository;
    private readonly MessageRepository _messageRepository;

    private NavRailViewModel _navRail;
    private ChatListViewModel _chatList;
    private ChatViewModel? _currentChat;

    public NavRailViewModel NavRail
    {
        get => _navRail;
        set => SetProperty(ref _navRail, value);
    }

    public ChatListViewModel ChatList
    {
        get => _chatList;
        set => SetProperty(ref _chatList, value);
    }

    public ChatViewModel? CurrentChat
    {
        get => _currentChat;
        set
        {
            if (SetProperty(ref _currentChat, value))
            {
                OnPropertyChanged(nameof(HasSelectedChat));
            }
        }
    }

    public bool HasSelectedChat => _currentChat != null;

    public ICommand ClearSelectionCommand { get; }

    public MainViewModel(
        Database database,
        ProfileService profileService,
        ConversationRepository conversationRepository,
        MessageRepository messageRepository)
    {
        _database = database;
        _profileService = profileService;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;

        _navRail = new NavRailViewModel(_profileService);
        _chatList = new ChatListViewModel(_conversationRepository, _messageRepository, _profileService);

        ClearSelectionCommand = new RelayCommand(() =>
        {
            _chatList.SelectedChat = null;
            CurrentChat = null;
        });

        _chatList.SelectedChatChanged += OnSelectedChatChanged;

        if (_chatList.SelectedChat != null)
        {
            CurrentChat = new ChatViewModel(_chatList.SelectedChat, _messageRepository, _conversationRepository, _profileService);
        }
    }

    private void OnSelectedChatChanged(ChatItem? chat)
    {
        if (chat == null)
        {
            CurrentChat = null;
        }
        else
        {
            if (CurrentChat == null)
            {
                CurrentChat = new ChatViewModel(chat, _messageRepository, _conversationRepository, _profileService);
            }
            else
            {
                CurrentChat.UpdateChat(chat);
            }
        }
    }
}
