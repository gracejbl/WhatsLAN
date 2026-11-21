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
}
