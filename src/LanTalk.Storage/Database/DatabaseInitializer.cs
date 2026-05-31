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

        await EnsureKnownUsersDepartmentColumnAsync(connection, cancellationToken).ConfigureAwait(false);
        await EnsureOutgoingDeliveriesSourcePathColumnAsync(connection, cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureKnownUsersDepartmentColumnAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var tableInfoCommand = connection.CreateCommand();
        tableInfoCommand.CommandText = "PRAGMA table_info(KnownUsers);";

        await using var reader = await tableInfoCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (string.Equals(reader.GetString(1), "Department", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        await using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText = "ALTER TABLE KnownUsers ADD COLUMN Department TEXT NOT NULL DEFAULT '默认部门';";
        await alterCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureOutgoingDeliveriesSourcePathColumnAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var tableInfoCommand = connection.CreateCommand();
        tableInfoCommand.CommandText = "PRAGMA table_info(OutgoingDeliveries);";

        await using var reader = await tableInfoCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (string.Equals(reader.GetString(1), "SourcePath", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        await using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText = "ALTER TABLE OutgoingDeliveries ADD COLUMN SourcePath TEXT;";
        await alterCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static readonly string[] CreateStatements =
    [
        """
        CREATE TABLE IF NOT EXISTS KnownUsers (
            UserId TEXT PRIMARY KEY,
            Nickname TEXT NOT NULL,
            Department TEXT NOT NULL DEFAULT '默认部门',
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
        CREATE TABLE IF NOT EXISTS ChatGroups (
            GroupId TEXT PRIMARY KEY,
            Name TEXT NOT NULL,
            Kind TEXT NOT NULL,
            MemberUserIdsJson TEXT NOT NULL,
            CreatedTime TEXT NOT NULL,
            UpdatedTime TEXT NOT NULL
        );
        """,
        """
        CREATE TABLE IF NOT EXISTS OutgoingDeliveries (
            DeliveryId TEXT PRIMARY KEY,
            RecipientId TEXT NOT NULL,
            PacketType TEXT NOT NULL,
            PayloadJson TEXT NOT NULL,
            SourcePath TEXT,
            CreatedTime TEXT NOT NULL,
            LastAttemptTime TEXT,
            AttemptCount INTEGER NOT NULL,
            LastError TEXT
        );
        """,
        """
        CREATE INDEX IF NOT EXISTS IX_ChatMessages_SessionId_SendTime
        ON ChatMessages(SessionId, SendTime);
        """,
        """
        CREATE INDEX IF NOT EXISTS IX_FileTransfers_TransferTime
        ON FileTransfers(TransferTime);
        """,
        """
        CREATE INDEX IF NOT EXISTS IX_ChatGroups_UpdatedTime
        ON ChatGroups(UpdatedTime);
        """,
        """
        CREATE INDEX IF NOT EXISTS IX_OutgoingDeliveries_RecipientId_CreatedTime
        ON OutgoingDeliveries(RecipientId, CreatedTime);
        """
    ];
}
