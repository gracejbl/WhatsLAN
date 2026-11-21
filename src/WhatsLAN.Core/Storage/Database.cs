using Microsoft.Data.Sqlite;

namespace WhatsLAN.Core.Storage;

public class Database : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    public Database(string dbPath)
    {
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        Initialize();
    }

    public SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private void Initialize()
    {
        using var conn = CreateConnection();

        // Pragmas pour performance et intégrité
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                PRAGMA journal_mode = WAL;
                PRAGMA foreign_keys = ON;
            ";
            cmd.ExecuteNonQuery();
        }

        // Création des tables de la spécification (§5)
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Profile (
                    userId TEXT PRIMARY KEY,
                    name TEXT NOT NULL,
                    status TEXT NOT NULL,
                    avatarPath TEXT,
                    avatarHash TEXT,
                    privateKey TEXT NOT NULL,
                    publicKey TEXT NOT NULL,
                    createdAtUtc TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Contact (
                    userId TEXT PRIMARY KEY,
                    name TEXT NOT NULL,
                    status TEXT NOT NULL,
                    avatarPath TEXT,
                    avatarHash TEXT,
                    lastIp TEXT,
                    lastPort INTEGER NOT NULL,
                    lastSeenUtc TEXT,
                    pubKey TEXT,
                    isBlocked INTEGER NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS Conversation (
                    id TEXT PRIMARY KEY,
                    type TEXT NOT NULL,
                    peerUserId TEXT,
                    groupId TEXT,
                    pinned INTEGER NOT NULL DEFAULT 0,
                    muted INTEGER NOT NULL DEFAULT 0,
                    archived INTEGER NOT NULL DEFAULT 0,
                    lastMessageId TEXT,
                    unreadCount INTEGER NOT NULL DEFAULT 0,
                    draftText TEXT,
                    updatedUtc TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Message (
                    id TEXT PRIMARY KEY,
                    conversationId TEXT NOT NULL,
                    senderId TEXT NOT NULL,
                    kind TEXT NOT NULL,
                    body TEXT NOT NULL,
                    replyToId TEXT,
                    status TEXT NOT NULL,
                    createdUtc TEXT NOT NULL,
                    editedUtc TEXT,
                    isDeleted INTEGER NOT NULL DEFAULT 0,
                    isStarred INTEGER NOT NULL DEFAULT 0,
                    attachmentId TEXT,
                    durationMs INTEGER
                );

                CREATE TABLE IF NOT EXISTS Attachment (
                    id TEXT PRIMARY KEY,
                    messageId TEXT NOT NULL,
                    fileName TEXT NOT NULL,
                    localPath TEXT NOT NULL,
                    size INTEGER NOT NULL,
                    mime TEXT NOT NULL,
                    sha256 TEXT NOT NULL,
                    thumbPath TEXT,
                    width INTEGER,
                    height INTEGER,
                    transferState TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Reaction (
                    messageId TEXT NOT NULL,
                    userId TEXT NOT NULL,
                    emoji TEXT NOT NULL,
                    PRIMARY KEY(messageId, userId)
                );

                CREATE TABLE IF NOT EXISTS Setting (
                    key TEXT PRIMARY KEY,
                    value TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_message_conv_created 
                    ON Message(conversationId, createdUtc);
            ";
            cmd.ExecuteNonQuery();
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _connection = null;
    }
}
