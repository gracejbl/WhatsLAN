using Microsoft.Data.Sqlite;
using WhatsLAN.Core.Models;

namespace WhatsLAN.Core.Storage;

public class ContactRepository
{
    private readonly Database _database;

    public ContactRepository(Database database)
    {
        _database = database;
    }

    public List<Contact> GetAll()
    {
        var list = new List<Contact>();
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT userId, name, status, avatarPath, avatarHash, lastIp, lastPort, lastSeenUtc, pubKey, isBlocked
            FROM Contact
            ORDER BY name ASC;
        ";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Contact
            {
                UserId = reader.GetString(0),
                Name = reader.GetString(1),
                Status = reader.GetString(2),
                AvatarPath = reader.IsDBNull(3) ? null : reader.GetString(3),
                AvatarHash = reader.IsDBNull(4) ? null : reader.GetString(4),
                LastIp = reader.IsDBNull(5) ? null : reader.GetString(5),
                LastPort = reader.GetInt32(6),
                LastSeenUtc = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7)),
                PublicKey = reader.IsDBNull(8) ? null : reader.GetString(8),
                IsBlocked = reader.GetInt32(9) == 1
            });
        }

        return list;
    }

    public Contact? GetById(string userId)
    {
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT userId, name, status, avatarPath, avatarHash, lastIp, lastPort, lastSeenUtc, pubKey, isBlocked
            FROM Contact
            WHERE userId = @userId
            LIMIT 1;
        ";
        cmd.Parameters.AddWithValue("@userId", userId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new Contact
        {
            UserId = reader.GetString(0),
            Name = reader.GetString(1),
            Status = reader.GetString(2),
            AvatarPath = reader.IsDBNull(3) ? null : reader.GetString(3),
            AvatarHash = reader.IsDBNull(4) ? null : reader.GetString(4),
            LastIp = reader.IsDBNull(5) ? null : reader.GetString(5),
            LastPort = reader.GetInt32(6),
            LastSeenUtc = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7)),
            PublicKey = reader.IsDBNull(8) ? null : reader.GetString(8),
            IsBlocked = reader.GetInt32(9) == 1
        };
    }

    public void Save(Contact contact)
    {
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO Contact 
            (userId, name, status, avatarPath, avatarHash, lastIp, lastPort, lastSeenUtc, pubKey, isBlocked)
            VALUES (@userId, @name, @status, @avatarPath, @avatarHash, @lastIp, @lastPort, @lastSeenUtc, @pubKey, @isBlocked);
        ";

        cmd.Parameters.AddWithValue("@userId", contact.UserId);
        cmd.Parameters.AddWithValue("@name", contact.Name);
        cmd.Parameters.AddWithValue("@status", contact.Status);
        cmd.Parameters.AddWithValue("@avatarPath", (object?)contact.AvatarPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@avatarHash", (object?)contact.AvatarHash ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@lastIp", (object?)contact.LastIp ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@lastPort", contact.LastPort);
        cmd.Parameters.AddWithValue("@lastSeenUtc", (object?)contact.LastSeenUtc?.ToString("o") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@pubKey", (object?)contact.PublicKey ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@isBlocked", contact.IsBlocked ? 1 : 0);

        cmd.ExecuteNonQuery();
    }

    public void UpdatePresence(string userId, DateTime lastSeenUtc)
    {
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Contact 
            SET lastSeenUtc = @lastSeenUtc 
            WHERE userId = @userId;
        ";
        cmd.Parameters.AddWithValue("@lastSeenUtc", lastSeenUtc.ToString("o"));
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.ExecuteNonQuery();
    }
}
