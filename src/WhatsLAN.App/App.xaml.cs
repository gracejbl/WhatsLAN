using System.IO;
using System.Windows;
using WhatsLAN.App.ViewModels;
using WhatsLAN.App.Views;
using WhatsLAN.Core.Services;
using WhatsLAN.Core.Storage;
using WhatsLAN.Core.Utils;

namespace WhatsLAN.App;

public partial class App : Application
{
    private Database? _database;
    private NetworkService? _networkService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Empêche la fermeture de l'application lors de la fermeture de la boîte de dialogue de bienvenue
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Gestionnaires d'exceptions non gérées avec journalisation
        var logsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WhatsLAN", "logs");
        AppLogger.Initialize(logsDir);

        DispatcherUnhandledException += (s, args) =>
        {
            AppLogger.LogError("Exception Dispatcher non gérée", args.Exception);
            MessageBox.Show($"Une erreur est survenue : {args.Exception.Message}", "Erreur WhatsLAN", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                AppLogger.LogError("Exception Domaine non gérée", ex);
            }
        };

        // 1. Analyse des arguments CLI (--port <port>, --data <path>)
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
        AppLogger.Log($"Base de données : {dbPath}, Port TCP : {customPort}");

        // 3. Initialisation de la base SQLite et des dépôts
        _database = new Database(dbPath);
        var profileRepo = new ProfileRepository(_database);
        var contactRepo = new ContactRepository(_database);
        var convRepo = new ConversationRepository(_database);
        var msgRepo = new MessageRepository(_database);
        var settingsRepo = new SettingsRepository(_database);
        var profileService = new ProfileService(profileRepo);

        // 4. Premier lancement si aucun profil enregistré
        if (!profileService.HasProfile())
        {
            AppLogger.Log("Aucun profil existant : ouverture de WelcomeWindow");
            var welcomeVm = new WelcomeViewModel(profileService);
            var welcomeWin = new WelcomeWindow(welcomeVm);

            var result = welcomeWin.ShowDialog();
            if (result != true || !profileService.HasProfile())
            {
                AppLogger.Log("Création de profil annulée, arrêt de l'application.");
                Shutdown();
                return;
            }
        }

        var currentProfile = profileService.CurrentProfile!;
        AppLogger.Log($"Profil chargé : {currentProfile.Name} ({currentProfile.UserId})");

        // 5. Initialisation et démarrage du service réseau P2P (UDP broadcast + TCP listener)
        _networkService = new NetworkService(
            currentProfile,
            customPort,
            contactRepo,
            convRepo,
            msgRepo);

        _networkService.Start();
        AppLogger.Log("NetworkService démarré avec succès.");

        // 6. Ouverture de la fenêtre principale
        var mainVm = new MainViewModel(_database, profileService, contactRepo, convRepo, msgRepo, _networkService);
        var mainWindow = new MainWindow(mainVm);
        MainWindow = mainWindow;

        // Rétablissement du mode de fermeture automatique sur la fermeture de la fenêtre principale
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        mainWindow.Show();
        AppLogger.Log("MainWindow affichée.");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _networkService?.Dispose();
        _database?.Dispose();
        base.OnExit(e);
    }
}
