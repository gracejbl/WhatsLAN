using WhatsLAN.App.MVVM;

namespace WhatsLAN.App.Models;

public class ChatMessage : ObservableObject
{
    private string _id = string.Empty;
    private string _text = string.Empty;
    private string _time = string.Empty;
    private bool _isOutgoing;
    private string _status = "Sent"; // "Pending", "Sent", "Delivered", "Read"
    private string _senderName = string.Empty;
    private string _senderColor = "#1F7AA3";
    private bool _isFirstInCluster = true;
    private string? _quotedAuthor;
    private string? _quotedText;
    private string _dateSeparator = string.Empty;
    private bool _showDateSeparator;
    private DateTime _createdUtc = DateTime.UtcNow;
    private bool _isEdited;
    private DateTime? _editedUtc;
    private bool _isDeleted;
    private bool _isStarred;
    private string? _replyToId;
    private string _reactionsSummary = string.Empty;
    private bool _isHighlighted;

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }

    public string Time
    {
        get => _time;
        set => SetProperty(ref _time, value);
    }

    public bool IsOutgoing
    {
        get => _isOutgoing;
        set => SetProperty(ref _isOutgoing, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string SenderName
    {
        get => _senderName;
        set => SetProperty(ref _senderName, value);
    }

    public string SenderColor
    {
        get => _senderColor;
        set => SetProperty(ref _senderColor, value);
    }

    public bool IsFirstInCluster
    {
        get => _isFirstInCluster;
        set => SetProperty(ref _isFirstInCluster, value);
    }

    public string? QuotedAuthor
    {
        get => _quotedAuthor;
        set => SetProperty(ref _quotedAuthor, value);
    }

    public string? QuotedText
    {
        get => _quotedText;
        set
        {
            if (SetProperty(ref _quotedText, value))
            {
                OnPropertyChanged(nameof(HasQuote));
            }
        }
    }

    public bool HasQuote => !string.IsNullOrWhiteSpace(_quotedText);

    public string DateSeparator
    {
        get => _dateSeparator;
        set => SetProperty(ref _dateSeparator, value);
    }

    public bool ShowDateSeparator
    {
        get => _showDateSeparator;
        set => SetProperty(ref _showDateSeparator, value);
    }

    public DateTime CreatedUtc
    {
        get => _createdUtc;
        set => SetProperty(ref _createdUtc, value);
    }

    public bool IsEdited
    {
        get => _isEdited;
        set => SetProperty(ref _isEdited, value);
    }

    public DateTime? EditedUtc
    {
        get => _editedUtc;
        set
        {
            if (SetProperty(ref _editedUtc, value))
            {
                IsEdited = value.HasValue;
            }
        }
    }

    public bool IsDeleted
    {
        get => _isDeleted;
        set
        {
            if (SetProperty(ref _isDeleted, value))
            {
                OnPropertyChanged(nameof(CanEdit));
                OnPropertyChanged(nameof(CanDeleteForEveryone));
            }
        }
    }

    public bool IsStarred
    {
        get => _isStarred;
        set => SetProperty(ref _isStarred, value);
    }

    public string? ReplyToId
    {
        get => _replyToId;
        set => SetProperty(ref _replyToId, value);
    }

    public string ReactionsSummary
    {
        get => _reactionsSummary;
        set
        {
            if (SetProperty(ref _reactionsSummary, value))
            {
                OnPropertyChanged(nameof(HasReactions));
            }
        }
    }

    public bool HasReactions => !string.IsNullOrWhiteSpace(_reactionsSummary);

    public bool IsHighlighted
    {
        get => _isHighlighted;
        set => SetProperty(ref _isHighlighted, value);
    }

    public bool CanEdit => IsOutgoing && !IsDeleted && (DateTime.UtcNow - CreatedUtc) <= TimeSpan.FromMinutes(15);

    public bool CanDeleteForEveryone => IsOutgoing && !IsDeleted && (DateTime.UtcNow - CreatedUtc) <= TimeSpan.FromMinutes(60);
}
