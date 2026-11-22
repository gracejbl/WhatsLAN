using Microsoft.Data.Sqlite;

namespace WhatsLAN.Core.Storage;

public record MessageReaction(string MessageId, string UserId, string Emoji);

public class ReactionRepository
{
    private readonly Database _database;

    public ReactionRepository(Database database)
    {
        _database = database;
    }

    public void SetReaction(string messageId, string userId, string emoji)
    {
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();

        if (string.IsNullOrWhiteSpace(emoji))
        {
            cmd.CommandText = "DELETE FROM Reaction WHERE messageId = @mid AND userId = @uid;";
            cmd.Parameters.AddWithValue("@mid", messageId);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.ExecuteNonQuery();
            return;
        }

        cmd.CommandText = @"
            INSERT INTO Reaction (messageId, userId, emoji)
            VALUES (@mid, @uid, @emoji)
            ON CONFLICT(messageId, userId) DO UPDATE SET emoji = excluded.emoji;
        ";
        cmd.Parameters.AddWithValue("@mid", messageId);
        cmd.Parameters.AddWithValue("@uid", userId);
        cmd.Parameters.AddWithValue("@emoji", emoji);
        cmd.ExecuteNonQuery();
    }

    public void RemoveReaction(string messageId, string userId)
    {
        SetReaction(messageId, userId, string.Empty);
    }

    public List<MessageReaction> GetReactionsByMessageId(string messageId)
    {
        var list = new List<MessageReaction>();
        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT messageId, userId, emoji FROM Reaction WHERE messageId = @mid;";
        cmd.Parameters.AddWithValue("@mid", messageId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new MessageReaction(reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        }

        return list;
    }

    public Dictionary<string, List<MessageReaction>> GetReactionsForMessages(IEnumerable<string> messageIds)
    {
        var dict = new Dictionary<string, List<MessageReaction>>();
        var idsList = messageIds.ToList();
        if (idsList.Count == 0) return dict;

        using var conn = _database.CreateConnection();
        using var cmd = conn.CreateCommand();

        var paramNames = idsList.Select((_, idx) => $"@p{idx}").ToArray();
        cmd.CommandText = $"SELECT messageId, userId, emoji FROM Reaction WHERE messageId IN ({string.Join(",", paramNames)});";

        for (int i = 0; i < idsList.Count; i++)
        {
            cmd.Parameters.AddWithValue(paramNames[i], idsList[i]);
        }

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var mid = reader.GetString(0);
            var reaction = new MessageReaction(mid, reader.GetString(1), reader.GetString(2));
            if (!dict.TryGetValue(mid, out var list))
            {
                list = [];
                dict[mid] = list;
            }
            list.Add(reaction);
        }

        return dict;
    }
}
