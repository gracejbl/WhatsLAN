using System.IO;
using System.Windows;
using WhatsLAN.App.ViewModels;
using WhatsLAN.App.Views;
using WhatsLAN.Core.Services;
using WhatsLAN.Core.Storage;

namespace WhatsLAN.App;

public partial class App : Application
{
    private Database? _database;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Analyse des arguments de ligne de commande (--port <port>, --data <path>)
        string? customDataDir = null;
        int customPort = 50001;

        for (int i = 0; i < e.Args.Length; i++)
        {
            if (e.Args[i].Equals("--data", StringComparison.OrdinalIgnoreCase) && i + 1 < e.Args.Length)
            {
                customDataDir = e.Args[i + 1];
                i++;
            }
            else if (e.Args[i].Equals("--port", StringComparison.OrdinalIgnoreCase) && i + 1 < e.Args.Length)
            {
                if (int.TryParse(e.Args[i + 1], out var p))
                {
                    customPort = p;
                }
                i++;
            }
        }

        // 2. Dossier de données (%AppData%\WhatsLAN\ par défaut)
        var dataDir = !string.IsNullOrWhiteSpace(customDataDir)
            ? customDataDir
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WhatsLAN");

        if (!Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }

        var dbPath = Path.Combine(dataDir, "whatslan.db");

        // 3. Initialisation de la base SQLite et des dépôts
        _database = new Database(dbPath);
        var profileRepo = new ProfileRepository(_database);
        var convRepo = new ConversationRepository(_database);
        var msgRepo = new MessageRepository(_database);
        var settingsRepo = new SettingsRepository(_database);
        var profileService = new ProfileService(profileRepo);

        // 4. Premier lancement si aucun profil enregistré
        if (!profileService.HasProfile())
        {
            var welcomeVm = new WelcomeViewModel(profileService);
            var welcomeWin = new WelcomeWindow(welcomeVm);

            var result = welcomeWin.ShowDialog();
            if (result != true || !profileService.HasProfile())
            {
                Shutdown();
                return;
            }
        }

        // 5. Ouverture de la fenêtre principale
        var mainVm = new MainViewModel(_database, profileService, convRepo, msgRepo);
        var mainWindow = new MainWindow(mainVm);
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _database?.Dispose();
        base.OnExit(e);
    }
}
