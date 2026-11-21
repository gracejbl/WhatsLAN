using System.Windows;
using WhatsLAN.App.ViewModels;

namespace WhatsLAN.App.Views;

public partial class WelcomeWindow : Window
{
    public WelcomeWindow(WelcomeViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.ProfileCreated += _ =>
        {
            DialogResult = true;
            Close();
        };
    }
}
