using System.Collections.Specialized;
using System.Windows.Controls;
using WhatsLAN.App.ViewModels;

namespace WhatsLAN.App.Views;

public partial class ChatView : UserControl
{
    private ChatViewModel? _currentVm;

    public ChatView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        EmojiPicker.EmojiSelected += emoji =>
        {
            if (DataContext is ChatViewModel vm)
            {
                vm.InsertEmoji(emoji);
            }
        };
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (_currentVm != null)
        {
            _currentVm.Messages.CollectionChanged -= OnMessagesCollectionChanged;
        }

        _currentVm = e.NewValue as ChatViewModel;

        if (_currentVm != null)
        {
            _currentVm.Messages.CollectionChanged += OnMessagesCollectionChanged;
            Dispatcher.InvokeAsync(() => MessagesScrollViewer.ScrollToBottom());
        }
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            Dispatcher.InvokeAsync(() => MessagesScrollViewer.ScrollToBottom());
        }
    }
}
