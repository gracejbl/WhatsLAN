using Microsoft.Data.Sqlite;

namespace WhatsLAN.Core.Storage;

public class SettingsRepository
{
    private readonly Database _database;

    public SettingsRepository(Database database)
    {
        _database = database;
    }

    public string? Get(string key, string? defaultValue = null)
    {
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT value FROM Setting WHERE key = @key LIMIT 1;";
        cmd.Parameters.AddWithValue("@key", key);

        var result = cmd.ExecuteScalar();
        return result != null && result != DBNull.Value ? result.ToString() : defaultValue;
    }

    public void Set(string key, string value)
    {
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO Setting (key, value) 
            VALUES (@key, @value);
        ";
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@value", value);

        cmd.ExecuteNonQuery();
    }
}
