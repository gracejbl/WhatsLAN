using System.Windows;
using WhatsLAN.App.ViewModels;

namespace WhatsLAN.App.Views;

public partial class NewChatWindow : Window
{
    public string? SelectedConversationId { get; private set; }

    public NewChatWindow(NewChatViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.ContactSelected += convId =>
        {
            SelectedConversationId = convId;
            DialogResult = true;
            Close();
        };
    }
}
