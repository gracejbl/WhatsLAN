using System.Collections.ObjectModel;
using System.Windows.Input;
using WhatsLAN.App.Models;
using WhatsLAN.App.MVVM;
using WhatsLAN.App.Services;

namespace WhatsLAN.App.ViewModels;

public class NavRailViewModel : ObservableObject
{
    private NavItem? _selectedItem;

    public ObservableCollection<NavItem> TopNavItems { get; } = [];
    public ObservableCollection<NavItem> BottomNavItems { get; } = [];

    public NavItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem != null) _selectedItem.IsSelected = false;
            if (SetProperty(ref _selectedItem, value))
            {
                if (_selectedItem != null) _selectedItem.IsSelected = true;
            }
        }
    }

    public ICommand SelectItemCommand { get; }
    public ICommand ToggleThemeCommand { get; }

    public ThemeManager Theme => ThemeManager.Instance;

    public NavRailViewModel()
    {
        SelectItemCommand = new RelayCommand<NavItem>(item =>
        {
            if (item != null)
            {
                SelectedItem = item;
            }
        });

        ToggleThemeCommand = new RelayCommand(() =>
        {
            ThemeManager.Instance.ToggleTheme();
            OnPropertyChanged(nameof(Theme));
        });

        InitializeNav();
    }

    private void InitializeNav()
    {
        var chats = new NavItem
        {
            Target = NavTarget.Chats,
            Title = "Discussions",
            IconGeometry = "IconChat",
            BadgeCount = 3,
            IsSelected = true
        };

        TopNavItems.Add(chats);
        TopNavItems.Add(new NavItem { Target = NavTarget.Calls, Title = "Appels", IconGeometry = "IconPhone" });
        TopNavItems.Add(new NavItem { Target = NavTarget.Status, Title = "Statuts", IconGeometry = "IconStatus", HasUnseenDot = true });
        TopNavItems.Add(new NavItem { Target = NavTarget.Channels, Title = "Chaînes", IconGeometry = "IconChannel" });
        TopNavItems.Add(new NavItem { Target = NavTarget.Communities, Title = "Communautés", IconGeometry = "IconCommunities" });
        TopNavItems.Add(new NavItem { Target = NavTarget.Archived, Title = "Archivées", IconGeometry = "IconArchive" });

        _selectedItem = chats;
    }
}
