using System.Windows;
using System.Windows.Input;
using WhatsLAN.App.Models;
using WhatsLAN.App.MVVM;
using WhatsLAN.Core.Models;
using WhatsLAN.Core.Services;
using WhatsLAN.Core.Storage;

namespace WhatsLAN.App.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly Database _database;
    private readonly ProfileService _profileService;
    private readonly ContactRepository _contactRepository;
    private readonly ConversationRepository _conversationRepository;
    private readonly MessageRepository _messageRepository;
    private readonly NetworkService _networkService;

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
        ContactRepository contactRepository,
        ConversationRepository conversationRepository,
        MessageRepository messageRepository,
        NetworkService networkService)
    {
        _database = database;
        _profileService = profileService;
        _contactRepository = contactRepository;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _networkService = networkService;

        _navRail = new NavRailViewModel(_profileService);
        _chatList = new ChatListViewModel(_conversationRepository, _messageRepository, _contactRepository, _profileService, _networkService);

        ClearSelectionCommand = new RelayCommand(() =>
        {
            _chatList.SelectedChat = null;
            CurrentChat = null;
        });

        _chatList.SelectedChatChanged += OnSelectedChatChanged;

        if (_chatList.SelectedChat != null)
        {
            CurrentChat = new ChatViewModel(_chatList.SelectedChat, _messageRepository, _conversationRepository, _contactRepository, _profileService, _networkService);
        }

        RegisterNetworkEvents();
    }

    private void RegisterNetworkEvents()
    {
        _networkService.PeerPresenceChanged += (s, e) =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                _chatList.UpdatePeerPresence(e.PeerUserId, e.IsOnline);
                if (CurrentChat?.Chat.Id == e.PeerUserId)
                {
                    CurrentChat.Chat.IsOnline = e.IsOnline;
                }
            });
        };

        _networkService.MessageReceived += (s, e) =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var (peerId, msg) = e;

                _chatList.HandleIncomingMessage(peerId, msg);

                if (CurrentChat != null && CurrentChat.Chat.Id == peerId)
                {
                    CurrentChat.AddIncomingMessage(msg);
                }
            });
        };

        _networkService.MessageDelivered += (s, msgId) =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                CurrentChat?.UpdateMessageStatus(msgId, "Delivered");
                _chatList.UpdateLastMessageStatus(msgId, "Delivered");
            });
        };

        _networkService.MessageStatusChanged += (s, e) =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var (msgId, status) = e;
                CurrentChat?.UpdateMessageStatus(msgId, status);
                _chatList.UpdateLastMessageStatus(msgId, status);
            });
        };

        _networkService.MessagesRead += (s, e) =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var (peerId, msgIds) = e;
                foreach (var msgId in msgIds)
                {
                    CurrentChat?.UpdateMessageStatus(msgId, "Read");
                    _chatList.UpdateLastMessageStatus(msgId, "Read");
                }
            });
        };

        _networkService.PeerTypingChanged += (s, e) =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var (peerId, state) = e;
                _chatList.HandlePeerTyping(peerId, state);
                if (CurrentChat?.Chat.Id == peerId)
                {
                    CurrentChat.Chat.IsTyping = (state == "start");
                }
            });
        };
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
                CurrentChat = new ChatViewModel(chat, _messageRepository, _conversationRepository, _contactRepository, _profileService, _networkService);
            }
            else
            {
                CurrentChat.UpdateChat(chat);
            }
        }
    }
}
