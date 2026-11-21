using System.Diagnostics;

namespace WhatsLAN.Core.Utils;

public static class AppLogger
{
    private static string? _logFilePath;
    private static readonly object _lock = new();

    public static void Initialize(string logsDirectory)
    {
        try
        {
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }

            _logFilePath = Path.Combine(logsDirectory, "whatslan.log");
            Log("==========================================");
            Log($"[START] WhatsLAN démarré le {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to initialize logger: {ex.Message}");
        }
    }

    public static void Log(string message)
    {
        try
        {
            var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
            Debug.WriteLine(entry);

            if (_logFilePath != null)
            {
                lock (_lock)
                {
                    File.AppendAllText(_logFilePath, entry + Environment.NewLine);
                }
            }
        }
        catch
        {
            // Éviter tout plantage causé par la journalisation
        }
    }

    public static void LogError(string message, Exception? ex = null)
    {
        var err = ex != null ? $"{message} | Exception: {ex}" : message;
        Log($"[ERROR] {err}");
    }
}
