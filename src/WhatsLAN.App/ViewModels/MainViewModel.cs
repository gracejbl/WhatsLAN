using System.Windows.Input;
using WhatsLAN.App.Models;
using WhatsLAN.App.MVVM;

namespace WhatsLAN.App.ViewModels;

public class MainViewModel : ObservableObject
{
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

    public MainViewModel()
    {
        _navRail = new NavRailViewModel();
        _chatList = new ChatListViewModel();

        ClearSelectionCommand = new RelayCommand(() =>
        {
            _chatList.SelectedChat = null;
            CurrentChat = null;
        });

        _chatList.SelectedChatChanged += OnSelectedChatChanged;

        if (_chatList.SelectedChat != null)
        {
            CurrentChat = new ChatViewModel(_chatList.SelectedChat);
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
                CurrentChat = new ChatViewModel(chat);
            }
            else
            {
                CurrentChat.UpdateChat(chat);
            }
        }
    }
}
