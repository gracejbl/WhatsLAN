using WhatsLAN.App.MVVM;
using WhatsLAN.Core.Utils;

namespace WhatsLAN.App.Models;

public class ChatItem : ObservableObject
{
    private string _id = string.Empty;
    private string _name = string.Empty;
    private string _avatarInitial = string.Empty;
    private string _avatarColor = "#00A884";
    private string _lastMessage = string.Empty;
    private string _lastMessageTime = string.Empty;
    private int _unreadCount;
    private bool _isPinned;
    private bool _isMuted;
    private bool _isArchived;
    private bool _isOnline;
    private bool _hasUnseenStatus;
    private bool _isGroup;
    private string _lastMessageStatus = string.Empty; // "Read", "Delivered", "Sent", ""
    private bool _isSelected;
    private string _subtitle = "hors ligne";
    private DateTime? _lastSeenUtc;
    private bool _isTyping;
    private string? _draftText;

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string AvatarInitial
    {
        get => _avatarInitial;
        set => SetProperty(ref _avatarInitial, value);
    }

    public string AvatarColor
    {
        get => _avatarColor;
        set => SetProperty(ref _avatarColor, value);
    }

    public string LastMessage
    {
        get => _lastMessage;
        set
        {
            if (SetProperty(ref _lastMessage, value))
            {
                OnPropertyChanged(nameof(DisplayLastMessage));
            }
        }
    }

    public bool IsTyping
    {
        get => _isTyping;
        set
        {
            if (SetProperty(ref _isTyping, value))
            {
                UpdateSubtitle();
                OnPropertyChanged(nameof(DisplayLastMessage));
                OnPropertyChanged(nameof(IsGreenPreview));
            }
        }
    }

    public string? DraftText
    {
        get => _draftText;
        set
        {
            if (SetProperty(ref _draftText, value))
            {
                OnPropertyChanged(nameof(HasDraft));
                OnPropertyChanged(nameof(DisplayLastMessage));
                OnPropertyChanged(nameof(IsGreenPreview));
            }
        }
    }

    public bool HasDraft => !string.IsNullOrWhiteSpace(_draftText);

    public string DisplayLastMessage
    {
        get
        {
            if (_isTyping) return "écrit…";
            if (!string.IsNullOrWhiteSpace(_draftText)) return $"Brouillon : {_draftText}";
            return _lastMessage;
        }
    }

    public bool IsGreenPreview => _isTyping || !string.IsNullOrWhiteSpace(_draftText);

    public string LastMessageTime
    {
        get => _lastMessageTime;
        set => SetProperty(ref _lastMessageTime, value);
    }

    public int UnreadCount
    {
        get => _unreadCount;
        set
        {
            if (SetProperty(ref _unreadCount, value))
            {
                OnPropertyChanged(nameof(HasUnread));
            }
        }
    }

    public bool HasUnread => _unreadCount > 0;

    public bool IsPinned
    {
        get => _isPinned;
        set => SetProperty(ref _isPinned, value);
    }

    public bool IsMuted
    {
        get => _isMuted;
        set => SetProperty(ref _isMuted, value);
    }

    public bool IsArchived
    {
        get => _isArchived;
        set => SetProperty(ref _isArchived, value);
    }

    public bool IsOnline
    {
        get => _isOnline;
        set
        {
            if (SetProperty(ref _isOnline, value))
            {
                UpdateSubtitle();
            }
        }
    }

    public DateTime? LastSeenUtc
    {
        get => _lastSeenUtc;
        set
        {
            if (SetProperty(ref _lastSeenUtc, value))
            {
                UpdateSubtitle();
            }
        }
    }

    public string Subtitle
    {
        get => _subtitle;
        set => SetProperty(ref _subtitle, value);
    }

    public void UpdateSubtitle()
    {
        if (_isTyping)
        {
            Subtitle = "écrit…";
        }
        else
        {
            Subtitle = DateTimeFormatter.FormatLastSeen(_isOnline, _lastSeenUtc);
        }
    }

    public bool HasUnseenStatus
    {
        get => _hasUnseenStatus;
        set => SetProperty(ref _hasUnseenStatus, value);
    }

    public bool IsGroup
    {
        get => _isGroup;
        set => SetProperty(ref _isGroup, value);
    }

    public string LastMessageStatus
    {
        get => _lastMessageStatus;
        set => SetProperty(ref _lastMessageStatus, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
