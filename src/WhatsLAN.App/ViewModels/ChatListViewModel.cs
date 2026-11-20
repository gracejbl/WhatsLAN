using System.Collections.ObjectModel;
using System.Windows.Input;
using WhatsLAN.App.Models;
using WhatsLAN.App.MVVM;

namespace WhatsLAN.App.ViewModels;

public class ChatListViewModel : ObservableObject
{
    private string _searchText = string.Empty;
    private ChatItem? _selectedChat;
    private FilterPill? _selectedFilter;
    private readonly List<ChatItem> _allChats = [];

    public ObservableCollection<FilterPill> FilterPills { get; }
    public ObservableCollection<ChatItem> Chats { get; } = [];

    public event Action<ChatItem?>? SelectedChatChanged;

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

    public ChatListViewModel()
    {
        FilterPills = DesignTimeData.CreateFilterPills();
        _selectedFilter = FilterPills.FirstOrDefault();

        SelectFilterCommand = new RelayCommand<FilterPill>(pill =>
        {
            if (pill != null)
            {
                SelectedFilter = pill;
            }
        });

        NewChatCommand = new RelayCommand(() =>
        {
            // Placeholder for Phase 0 new chat button
        });

        var items = DesignTimeData.CreateChatItems();
        foreach (var item in items)
        {
            _allChats.Add(item);
            Chats.Add(item);
        }

        if (Chats.Count > 0)
        {
            SelectedChat = Chats[0];
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

        foreach (var chat in query)
        {
            Chats.Add(chat);
        }
    }
}
