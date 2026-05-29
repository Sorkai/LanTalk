using Microsoft.Data.Sqlite;

namespace LanTalk.Storage.Database;

public sealed class DatabaseInitializer
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public DatabaseInitializer(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        foreach (var commandText in CreateStatements)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = commandText;
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static readonly string[] CreateStatements =
    [
        """
        CREATE TABLE IF NOT EXISTS KnownUsers (
            UserId TEXT PRIMARY KEY,
            Nickname TEXT NOT NULL,
            IpAddress TEXT NOT NULL,
            MessagePort INTEGER NOT NULL,
            FilePort INTEGER NOT NULL,
            Status TEXT NOT NULL,
            LastSeenTime TEXT NOT NULL
        );
        """,
        """
        CREATE TABLE IF NOT EXISTS ChatMessages (
            MessageId TEXT PRIMARY KEY,
            SessionId TEXT NOT NULL,
            SenderId TEXT NOT NULL,
            ReceiverId TEXT,
            MessageType TEXT NOT NULL,
            Content TEXT NOT NULL,
            SendTime TEXT NOT NULL,
            IsMine INTEGER NOT NULL
        );
        """,
        """
        CREATE TABLE IF NOT EXISTS FileTransfers (
            FileId TEXT PRIMARY KEY,
            SenderId TEXT NOT NULL,
            ReceiverId TEXT NOT NULL,
            FileName TEXT NOT NULL,
            FileSize INTEGER NOT NULL,
            SavePath TEXT,
            Status TEXT NOT NULL,
            TransferTime TEXT NOT NULL
        );
        """,
        """
        CREATE INDEX IF NOT EXISTS IX_ChatMessages_SessionId_SendTime
        ON ChatMessages(SessionId, SendTime);
        """,
        """
        CREATE INDEX IF NOT EXISTS IX_FileTransfers_TransferTime
        ON FileTransfers(TransferTime);
        """
    ];
}

