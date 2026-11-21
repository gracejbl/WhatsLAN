using Microsoft.Data.Sqlite;
using WhatsLAN.Core.Models;

namespace WhatsLAN.Core.Storage;

public class ConversationRepository
{
    private readonly Database _database;

    public ConversationRepository(Database database)
    {
        _database = database;
    }

    public List<Conversation> GetAll()
    {
        var list = new List<Conversation>();
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, type, peerUserId, groupId, pinned, muted, archived, lastMessageId, unreadCount, draftText, updatedUtc
            FROM Conversation
            ORDER BY pinned DESC, updatedUtc DESC;
        ";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Conversation
            {
                Id = reader.GetString(0),
                Type = reader.GetString(1),
                PeerUserId = reader.IsDBNull(2) ? null : reader.GetString(2),
                GroupId = reader.IsDBNull(3) ? null : reader.GetString(3),
                Pinned = reader.GetInt32(4) == 1,
                Muted = reader.GetInt32(5) == 1,
                Archived = reader.GetInt32(6) == 1,
                LastMessageId = reader.IsDBNull(7) ? null : reader.GetString(7),
                UnreadCount = reader.GetInt32(8),
                DraftText = reader.IsDBNull(9) ? null : reader.GetString(9),
                UpdatedUtc = DateTime.Parse(reader.GetString(10))
            });
        }

        return list;
    }

    public Conversation? GetById(string id)
    {
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, type, peerUserId, groupId, pinned, muted, archived, lastMessageId, unreadCount, draftText, updatedUtc
            FROM Conversation
            WHERE id = @id
            LIMIT 1;
        ";
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new Conversation
        {
            Id = reader.GetString(0),
            Type = reader.GetString(1),
            PeerUserId = reader.IsDBNull(2) ? null : reader.GetString(2),
            GroupId = reader.IsDBNull(3) ? null : reader.GetString(3),
            Pinned = reader.GetInt32(4) == 1,
            Muted = reader.GetInt32(5) == 1,
            Archived = reader.GetInt32(6) == 1,
            LastMessageId = reader.IsDBNull(7) ? null : reader.GetString(7),
            UnreadCount = reader.GetInt32(8),
            DraftText = reader.IsDBNull(9) ? null : reader.GetString(9),
            UpdatedUtc = DateTime.Parse(reader.GetString(10))
        };
    }

    public void Save(Conversation conversation)
    {
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO Conversation 
            (id, type, peerUserId, groupId, pinned, muted, archived, lastMessageId, unreadCount, draftText, updatedUtc)
            VALUES (@id, @type, @peerUserId, @groupId, @pinned, @muted, @archived, @lastMessageId, @unreadCount, @draftText, @updatedUtc);
        ";

        cmd.Parameters.AddWithValue("@id", conversation.Id);
        cmd.Parameters.AddWithValue("@type", conversation.Type);
        cmd.Parameters.AddWithValue("@peerUserId", (object?)conversation.PeerUserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@groupId", (object?)conversation.GroupId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@pinned", conversation.Pinned ? 1 : 0);
        cmd.Parameters.AddWithValue("@muted", conversation.Muted ? 1 : 0);
        cmd.Parameters.AddWithValue("@archived", conversation.Archived ? 1 : 0);
        cmd.Parameters.AddWithValue("@lastMessageId", (object?)conversation.LastMessageId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@unreadCount", conversation.UnreadCount);
        cmd.Parameters.AddWithValue("@draftText", (object?)conversation.DraftText ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@updatedUtc", conversation.UpdatedUtc.ToString("o"));

        cmd.ExecuteNonQuery();
    }
}
