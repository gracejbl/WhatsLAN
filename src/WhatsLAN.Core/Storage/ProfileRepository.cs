using Microsoft.Data.Sqlite;
using WhatsLAN.Core.Models;

namespace WhatsLAN.Core.Storage;

public class ProfileRepository
{
    private readonly Database _database;

    public ProfileRepository(Database database)
    {
        _database = database;
    }

    public bool HasProfile()
    {
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM Profile;";
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        return count > 0;
    }

    public Profile? GetProfile()
    {
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT userId, name, status, avatarPath, avatarHash, privateKey, publicKey, createdAtUtc
            FROM Profile
            LIMIT 1;
        ";

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new Profile
        {
            UserId = reader.GetString(0),
            Name = reader.GetString(1),
            Status = reader.GetString(2),
            AvatarPath = reader.IsDBNull(3) ? null : reader.GetString(3),
            AvatarHash = reader.IsDBNull(4) ? null : reader.GetString(4),
            PrivateKey = reader.GetString(5),
            PublicKey = reader.GetString(6),
            CreatedAtUtc = DateTime.Parse(reader.GetString(7))
        };
    }

    public void SaveProfile(Profile profile)
    {
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO Profile (userId, name, status, avatarPath, avatarHash, privateKey, publicKey, createdAtUtc)
            VALUES (@userId, @name, @status, @avatarPath, @avatarHash, @privateKey, @publicKey, @createdAtUtc);
        ";

        cmd.Parameters.AddWithValue("@userId", profile.UserId);
        cmd.Parameters.AddWithValue("@name", profile.Name);
        cmd.Parameters.AddWithValue("@status", profile.Status);
        cmd.Parameters.AddWithValue("@avatarPath", (object?)profile.AvatarPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@avatarHash", (object?)profile.AvatarHash ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@privateKey", profile.PrivateKey);
        cmd.Parameters.AddWithValue("@publicKey", profile.PublicKey);
        cmd.Parameters.AddWithValue("@createdAtUtc", profile.CreatedAtUtc.ToString("o"));

        cmd.ExecuteNonQuery();
    }
}
