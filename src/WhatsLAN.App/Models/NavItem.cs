using WhatsLAN.App.MVVM;

namespace WhatsLAN.App.Models;

public enum NavTarget
{
    Chats,
    Calls,
    Status,
    Channels,
    Communities,
    Archived,
    Settings
}

public class NavItem : ObservableObject
{
    private NavTarget _target;
    private string _title = string.Empty;
    private string _iconGeometry = string.Empty;
    private int _badgeCount;
    private bool _hasUnseenDot;
    private bool _isSelected;

    public NavTarget Target
    {
        get => _target;
        set => SetProperty(ref _target, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string IconGeometry
    {
        get => _iconGeometry;
        set => SetProperty(ref _iconGeometry, value);
    }

    public int BadgeCount
    {
        get => _badgeCount;
        set
        {
            if (SetProperty(ref _badgeCount, value))
            {
                OnPropertyChanged(nameof(HasBadge));
            }
        }
    }

    public bool HasBadge => _badgeCount > 0;

    public bool HasUnseenDot
    {
        get => _hasUnseenDot;
        set => SetProperty(ref _hasUnseenDot, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
