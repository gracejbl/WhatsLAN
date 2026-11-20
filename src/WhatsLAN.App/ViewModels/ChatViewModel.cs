using System.Collections.ObjectModel;
using System.Windows.Input;
using WhatsLAN.App.Models;
using WhatsLAN.App.MVVM;

namespace WhatsLAN.App.ViewModels;

public class ChatViewModel : ObservableObject
{
    private ChatItem _chat;
    private string _messageText = string.Empty;

    public ChatItem Chat
    {
        get => _chat;
        set => SetProperty(ref _chat, value);
    }

    public ObservableCollection<ChatMessage> Messages { get; } = [];

    public string MessageText
    {
        get => _messageText;
        set
        {
            if (SetProperty(ref _messageText, value))
            {
                OnPropertyChanged(nameof(CanSend));
            }
        }
    }

    public bool CanSend => !string.IsNullOrWhiteSpace(_messageText);

    public ICommand SendMessageCommand { get; }
    public ICommand AudioCallCommand { get; }
    public ICommand VideoCallCommand { get; }
    public ICommand SearchCommand { get; }

    public ChatViewModel(ChatItem chat)
    {
        _chat = chat;

        SendMessageCommand = new RelayCommand(SendMessage, () => CanSend);
        AudioCallCommand = new RelayCommand(() => { });
        VideoCallCommand = new RelayCommand(() => { });
        SearchCommand = new RelayCommand(() => { });

        LoadMessages();
    }

    public void UpdateChat(ChatItem chat)
    {
        Chat = chat;
        LoadMessages();
    }

    private void LoadMessages()
    {
        Messages.Clear();
        var msgs = DesignTimeData.CreateMessagesForChat(_chat.Id);
        foreach (var msg in msgs)
        {
            Messages.Add(msg);
        }
    }

    private void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(_messageText)) return;

        var newMsg = new ChatMessage
        {
            Id = Guid.NewGuid().ToString(),
            Text = _messageText.Trim(),
            Time = DateTime.Now.ToString("HH:mm"),
            IsOutgoing = true,
            Status = "Sent",
            IsFirstInCluster = Messages.Count == 0 || !Messages[^1].IsOutgoing
        };

        Messages.Add(newMsg);
        Chat.LastMessage = newMsg.Text;
        Chat.LastMessageTime = newMsg.Time;
        Chat.LastMessageStatus = "Sent";

        MessageText = string.Empty;
    }
}
