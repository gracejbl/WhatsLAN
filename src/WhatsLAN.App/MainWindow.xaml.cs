using System.ComponentModel;
using System.Windows;
using WhatsLAN.App.ViewModels;

namespace WhatsLAN.App;

public partial class MainWindow : Window
{
    private System.Windows.Forms.NotifyIcon? _notifyIcon;
    private bool _isQuitting;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        InitializeTrayIcon();
    }

    private void InitializeTrayIcon()
    {
        try
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Text = "WhatsLAN",
                Visible = true
            };

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add("Ouvrir WhatsLAN", null, (s, e) => RestoreWindow());
            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            contextMenu.Items.Add("Quitter WhatsLAN", null, (s, e) => ExitApplication());

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => RestoreWindow();
            _notifyIcon.Click += (s, e) =>
            {
                if (e is System.Windows.Forms.MouseEventArgs me && me.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    RestoreWindow();
                }
            };
        }
        catch
        {
            // Silencieux si environnement spécifique
        }
    }

    private void RestoreWindow()
    {
        Show();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        Activate();
    }

    private void ExitApplication()
    {
        _isQuitting = true;
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
        Application.Current.Shutdown();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isQuitting)
        {
            e.Cancel = true;
            Hide();
            try
            {
                _notifyIcon?.ShowBalloonTip(2000, "WhatsLAN", "WhatsLAN est réduit dans la zone de notification.", System.Windows.Forms.ToolTipIcon.Info);
            }
            catch
            {
            }
        }
        else
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
            base.OnClosing(e);
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
