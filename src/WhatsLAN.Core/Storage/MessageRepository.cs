using Microsoft.Data.Sqlite;
using WhatsLAN.Core.Models;

namespace WhatsLAN.Core.Storage;

public class MessageRepository
{
    private readonly Database _database;

    public MessageRepository(Database database)
    {
        _database = database;
    }

    public Message? GetById(string messageId)
    {
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, conversationId, senderId, kind, body, replyToId, status, 
                   createdUtc, editedUtc, isDeleted, isStarred, attachmentId, durationMs
            FROM Message
            WHERE id = @id;
        ";
        cmd.Parameters.AddWithValue("@id", messageId);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Message
            {
                Id = reader.GetString(0),
                ConversationId = reader.GetString(1),
                SenderId = reader.GetString(2),
                Kind = reader.GetString(3),
                Body = reader.GetString(4),
                ReplyToId = reader.IsDBNull(5) ? null : reader.GetString(5),
                Status = reader.GetString(6),
                CreatedUtc = DateTime.Parse(reader.GetString(7)),
                EditedUtc = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8)),
                IsDeleted = reader.GetInt32(9) == 1,
                IsStarred = reader.GetInt32(10) == 1,
                AttachmentId = reader.IsDBNull(11) ? null : reader.GetString(11),
                DurationMs = reader.IsDBNull(12) ? null : reader.GetInt32(12)
            };
        }

        return null;
    }

    public void UpdateText(string messageId, string newText, DateTime editedUtc)
    {
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Message 
            SET body = @body, editedUtc = @editedUtc 
            WHERE id = @id;
        ";
        cmd.Parameters.AddWithValue("@body", newText);
        cmd.Parameters.AddWithValue("@editedUtc", editedUtc.ToString("o"));
        cmd.Parameters.AddWithValue("@id", messageId);
        cmd.ExecuteNonQuery();
    }

    public void MarkDeleted(string messageId)
    {
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Message 
            SET isDeleted = 1, body = 'Ce message a été supprimé' 
            WHERE id = @id;
        ";
        cmd.Parameters.AddWithValue("@id", messageId);
        cmd.ExecuteNonQuery();
    }

    public void DeleteForMe(string messageId)
    {
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Message WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", messageId);
        cmd.ExecuteNonQuery();
    }

    public bool ToggleStarred(string messageId)
    {
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Message 
            SET isStarred = CASE WHEN isStarred = 1 THEN 0 ELSE 1 END 
            WHERE id = @id
            RETURNING isStarred;
        ";
        cmd.Parameters.AddWithValue("@id", messageId);
        var result = cmd.ExecuteScalar();
        return result is long l && l == 1;
    }

    public List<Message> SearchMessages(string conversationId, string query)
    {
        var list = new List<Message>();
        if (string.IsNullOrWhiteSpace(query)) return list;

        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, conversationId, senderId, kind, body, replyToId, status, 
                   createdUtc, editedUtc, isDeleted, isStarred, attachmentId, durationMs
            FROM Message
            WHERE conversationId = @convId AND isDeleted = 0 AND body LIKE @query
            ORDER BY createdUtc ASC;
        ";
        cmd.Parameters.AddWithValue("@convId", conversationId);
        cmd.Parameters.AddWithValue("@query", $"%{query}%");

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Message
            {
                Id = reader.GetString(0),
                ConversationId = reader.GetString(1),
                SenderId = reader.GetString(2),
                Kind = reader.GetString(3),
                Body = reader.GetString(4),
                ReplyToId = reader.IsDBNull(5) ? null : reader.GetString(5),
                Status = reader.GetString(6),
                CreatedUtc = DateTime.Parse(reader.GetString(7)),
                EditedUtc = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8)),
                IsDeleted = reader.GetInt32(9) == 1,
                IsStarred = reader.GetInt32(10) == 1,
                AttachmentId = reader.IsDBNull(11) ? null : reader.GetString(11),
                DurationMs = reader.IsDBNull(12) ? null : reader.GetInt32(12)
            });
        }

        return list;
    }

    public List<Message> GetByConversationId(string conversationId, int limit = 100)
    {
        var list = new List<Message>();
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, conversationId, senderId, kind, body, replyToId, status, 
                   createdUtc, editedUtc, isDeleted, isStarred, attachmentId, durationMs
            FROM Message
            WHERE conversationId = @convId
            ORDER BY createdUtc ASC
            LIMIT @limit;
        ";
        cmd.Parameters.AddWithValue("@convId", conversationId);
        cmd.Parameters.AddWithValue("@limit", limit);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Message
            {
                Id = reader.GetString(0),
                ConversationId = reader.GetString(1),
                SenderId = reader.GetString(2),
                Kind = reader.GetString(3),
                Body = reader.GetString(4),
                ReplyToId = reader.IsDBNull(5) ? null : reader.GetString(5),
                Status = reader.GetString(6),
                CreatedUtc = DateTime.Parse(reader.GetString(7)),
                EditedUtc = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8)),
                IsDeleted = reader.GetInt32(9) == 1,
                IsStarred = reader.GetInt32(10) == 1,
                AttachmentId = reader.IsDBNull(11) ? null : reader.GetString(11),
                DurationMs = reader.IsDBNull(12) ? null : reader.GetInt32(12)
            });
        }

        return list;
    }

    public void Insert(Message message)
    {
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Message 
            (id, conversationId, senderId, kind, body, replyToId, status, createdUtc, editedUtc, isDeleted, isStarred, attachmentId, durationMs)
            VALUES (@id, @conversationId, @senderId, @kind, @body, @replyToId, @status, @createdUtc, @editedUtc, @isDeleted, @isStarred, @attachmentId, @durationMs);
        ";

        cmd.Parameters.AddWithValue("@id", message.Id);
        cmd.Parameters.AddWithValue("@conversationId", message.ConversationId);
        cmd.Parameters.AddWithValue("@senderId", message.SenderId);
        cmd.Parameters.AddWithValue("@kind", message.Kind);
        cmd.Parameters.AddWithValue("@body", message.Body);
        cmd.Parameters.AddWithValue("@replyToId", (object?)message.ReplyToId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", message.Status);
        cmd.Parameters.AddWithValue("@createdUtc", message.CreatedUtc.ToString("o"));
        cmd.Parameters.AddWithValue("@editedUtc", (object?)message.EditedUtc?.ToString("o") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@isDeleted", message.IsDeleted ? 1 : 0);
        cmd.Parameters.AddWithValue("@isStarred", message.IsStarred ? 1 : 0);
        cmd.Parameters.AddWithValue("@attachmentId", (object?)message.AttachmentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@durationMs", (object?)message.DurationMs ?? DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    public void UpdateStatus(string messageId, string status)
    {
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Message 
            SET status = @status 
            WHERE id = @id;
        ";
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@id", messageId);

        cmd.ExecuteNonQuery();
    }

    public List<Message> GetPendingMessagesByConversation(string conversationId)
    {
        var list = new List<Message>();
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, conversationId, senderId, kind, body, replyToId, status, 
                   createdUtc, editedUtc, isDeleted, isStarred, attachmentId, durationMs
            FROM Message
            WHERE conversationId = @convId AND status = 'Pending'
            ORDER BY createdUtc ASC;
        ";
        cmd.Parameters.AddWithValue("@convId", conversationId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Message
            {
                Id = reader.GetString(0),
                ConversationId = reader.GetString(1),
                SenderId = reader.GetString(2),
                Kind = reader.GetString(3),
                Body = reader.GetString(4),
                ReplyToId = reader.IsDBNull(5) ? null : reader.GetString(5),
                Status = reader.GetString(6),
                CreatedUtc = DateTime.Parse(reader.GetString(7)),
                EditedUtc = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8)),
                IsDeleted = reader.GetInt32(9) == 1,
                IsStarred = reader.GetInt32(10) == 1,
                AttachmentId = reader.IsDBNull(11) ? null : reader.GetString(11),
                DurationMs = reader.IsDBNull(12) ? null : reader.GetInt32(12)
            });
        }

        return list;
    }

    public List<string> GetUnreadIncomingMessageIds(string conversationId, string myUserId)
    {
        var list = new List<string>();
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id
            FROM Message
            WHERE conversationId = @convId AND senderId != @myUserId AND status != 'Read';
        ";
        cmd.Parameters.AddWithValue("@convId", conversationId);
        cmd.Parameters.AddWithValue("@myUserId", myUserId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(reader.GetString(0));
        }

        return list;
    }

    public void MarkMessagesAsRead(IEnumerable<string> messageIds)
    {
        using var conn = _database.CreateConnection();
        using var trans = conn.BeginTransaction();
        foreach (var id in messageIds)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = trans;
            cmd.CommandText = "UPDATE Message SET status = 'Read' WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }
        trans.Commit();
    }
}
