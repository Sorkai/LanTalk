using LanTalk.Core.Constants;
using LanTalk.Core.Enums;
using LanTalk.Core.Models;
using LanTalk.Storage.Database;
using Microsoft.Data.Sqlite;

namespace LanTalk.Storage.Repositories;

public sealed class MessageRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public MessageRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task SaveAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT OR REPLACE INTO ChatMessages
                (MessageId, SessionId, SenderId, ReceiverId, MessageType, Content, SendTime, IsMine, IsRead, ReadTime, ReadTargetCount, IsRecalled, RecalledTime)
            VALUES
                ($messageId, $sessionId, $senderId, $receiverId, $messageType, $content, $sendTime, $isMine, $isRead, $readTime, $readTargetCount, $isRecalled, $recalledTime);
            """;
        command.Parameters.AddWithValue("$messageId", message.MessageId);
        command.Parameters.AddWithValue("$sessionId", message.SessionId);
        command.Parameters.AddWithValue("$senderId", message.SenderId);
        command.Parameters.AddWithValue("$receiverId", (object?)message.ReceiverId ?? DBNull.Value);
        command.Parameters.AddWithValue("$messageType", message.Kind.ToString());
        command.Parameters.AddWithValue("$content", message.Content);
        command.Parameters.AddWithValue("$sendTime", message.SendTime.ToString("O"));
        command.Parameters.AddWithValue("$isMine", message.IsMine ? 1 : 0);
        command.Parameters.AddWithValue("$isRead", message.IsRead ? 1 : 0);
        command.Parameters.AddWithValue("$readTime", message.ReadTime?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$readTargetCount", message.ReadTargetCount);
        command.Parameters.AddWithValue("$isRecalled", message.IsRecalled ? 1 : 0);
        command.Parameters.AddWithValue("$recalledTime", message.RecalledTime?.ToString("O") ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ChatMessage>> LoadRecentMessagesAsync(string sessionId, int limit = NetworkConstants.RecentMessageLimit, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT MessageId, SessionId, SenderId, ReceiverId, MessageType, Content, SendTime, IsMine,
                   IsRead, ReadTime, ReadTargetCount, IsRecalled, RecalledTime,
                   (SELECT COUNT(*) FROM MessageReadReceipts receipts WHERE receipts.MessageId = ChatMessages.MessageId) AS ReadByCount
            FROM ChatMessages
            WHERE SessionId = $sessionId
            ORDER BY SendTime DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$sessionId", sessionId);
        command.Parameters.AddWithValue("$limit", limit);

        var messages = new List<ChatMessage>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            messages.Add(ReadMessage(reader));
        }

        messages.Reverse();
        return messages;
    }

    public async Task<IReadOnlyList<ChatMessage>> SearchMessagesAsync(
        string sessionId,
        string query,
        int limit = NetworkConstants.RecentMessageLimit,
        CancellationToken cancellationToken = default)
    {
        var trimmedQuery = query.Trim();
        if (string.IsNullOrWhiteSpace(trimmedQuery))
        {
            return await LoadRecentMessagesAsync(sessionId, limit, cancellationToken).ConfigureAwait(false);
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT MessageId, SessionId, SenderId, ReceiverId, MessageType, Content, SendTime, IsMine,
                   IsRead, ReadTime, ReadTargetCount, IsRecalled, RecalledTime,
                   (SELECT COUNT(*) FROM MessageReadReceipts receipts WHERE receipts.MessageId = ChatMessages.MessageId) AS ReadByCount
            FROM ChatMessages
            WHERE SessionId = $sessionId
              AND Content LIKE $query ESCAPE '\'
            ORDER BY SendTime DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$sessionId", sessionId);
        command.Parameters.AddWithValue("$query", $"%{EscapeLikePattern(trimmedQuery)}%");
        command.Parameters.AddWithValue("$limit", limit);

        var messages = new List<ChatMessage>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            messages.Add(ReadMessage(reader));
        }

        messages.Reverse();
        return messages;
    }

    public async Task<IReadOnlyList<ChatMessage>> MarkSessionIncomingMessagesReadAsync(
        string sessionId,
        DateTimeOffset readTime,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var selectCommand = connection.CreateCommand();
        selectCommand.CommandText =
            """
            SELECT MessageId, SessionId, SenderId, ReceiverId, MessageType, Content, SendTime, IsMine,
                   IsRead, ReadTime, ReadTargetCount, IsRecalled, RecalledTime,
                   (SELECT COUNT(*) FROM MessageReadReceipts receipts WHERE receipts.MessageId = ChatMessages.MessageId) AS ReadByCount
            FROM ChatMessages
            WHERE SessionId = $sessionId
              AND IsMine = 0
              AND IsRead = 0
              AND IsRecalled = 0
              AND MessageType IN ('Private', 'Group', 'Image')
            ORDER BY SendTime;
            """;
        selectCommand.Parameters.AddWithValue("$sessionId", sessionId);

        var messages = new List<ChatMessage>();
        await using (var reader = await selectCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                messages.Add(ReadMessage(reader));
            }
        }

        if (messages.Count == 0)
        {
            return messages;
        }

        await using var updateCommand = connection.CreateCommand();
        updateCommand.CommandText =
            """
            UPDATE ChatMessages
            SET IsRead = 1,
                ReadTime = $readTime
            WHERE SessionId = $sessionId
              AND IsMine = 0
              AND IsRead = 0
              AND IsRecalled = 0
              AND MessageType IN ('Private', 'Group', 'Image');
            """;
        updateCommand.Parameters.AddWithValue("$sessionId", sessionId);
        updateCommand.Parameters.AddWithValue("$readTime", readTime.ToString("O"));
        await updateCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        return messages;
    }

    public async Task<(int ReadByCount, int ReadTargetCount, bool IsRead)> MarkMessageReadByAsync(
        MessageReadReceiptPayload receipt,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText =
            """
            INSERT OR REPLACE INTO MessageReadReceipts
                (MessageId, SessionId, ReaderId, ReaderNickname, ReadTime)
            VALUES
                ($messageId, $sessionId, $readerId, $readerNickname, $readTime);
            """;
        insertCommand.Parameters.AddWithValue("$messageId", receipt.MessageId);
        insertCommand.Parameters.AddWithValue("$sessionId", receipt.SessionId);
        insertCommand.Parameters.AddWithValue("$readerId", receipt.ReaderUserId);
        insertCommand.Parameters.AddWithValue("$readerNickname", receipt.ReaderNickname);
        insertCommand.Parameters.AddWithValue("$readTime", receipt.ReadTime.ToString("O"));
        await insertCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        var readTargetCount = await LoadReadTargetCountAsync(connection, receipt.MessageId, cancellationToken).ConfigureAwait(false);
        var readByCount = await CountReadReceiptsAsync(connection, receipt.MessageId, cancellationToken).ConfigureAwait(false);
        var isRead = receipt.IsGroup
            ? readTargetCount > 0 && readByCount >= readTargetCount
            : true;

        await using var updateCommand = connection.CreateCommand();
        updateCommand.CommandText =
            """
            UPDATE ChatMessages
            SET IsRead = $isRead,
                ReadTime = CASE WHEN $isRead = 1 THEN $readTime ELSE ReadTime END
            WHERE MessageId = $messageId;
            """;
        updateCommand.Parameters.AddWithValue("$messageId", receipt.MessageId);
        updateCommand.Parameters.AddWithValue("$isRead", isRead ? 1 : 0);
        updateCommand.Parameters.AddWithValue("$readTime", receipt.ReadTime.ToString("O"));
        await updateCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        return (readByCount, readTargetCount, isRead);
    }

    public async Task RecallMessageAsync(
        string sessionId,
        string messageId,
        DateTimeOffset recalledTime,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE ChatMessages
            SET IsRecalled = 1,
                RecalledTime = $recalledTime
            WHERE SessionId = $sessionId
              AND MessageId = $messageId;
            """;
        command.Parameters.AddWithValue("$sessionId", sessionId);
        command.Parameters.AddWithValue("$messageId", messageId);
        command.Parameters.AddWithValue("$recalledTime", recalledTime.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static ChatMessage ReadMessage(SqliteDataReader reader)
    {
        return new ChatMessage
        {
            MessageId = reader.GetString(0),
            SessionId = reader.GetString(1),
            SenderId = reader.GetString(2),
            ReceiverId = reader.IsDBNull(3) ? null : reader.GetString(3),
            Kind = Enum.Parse<MessageKind>(reader.GetString(4)),
            Content = reader.GetString(5),
            SendTime = DateTimeOffset.Parse(reader.GetString(6)),
            IsMine = reader.GetInt32(7) == 1,
            IsRead = reader.GetInt32(8) == 1,
            ReadTime = reader.IsDBNull(9) ? null : DateTimeOffset.Parse(reader.GetString(9)),
            ReadTargetCount = reader.GetInt32(10),
            IsRecalled = reader.GetInt32(11) == 1,
            RecalledTime = reader.IsDBNull(12) ? null : DateTimeOffset.Parse(reader.GetString(12)),
            ReadByCount = reader.GetInt32(13)
        };
    }

    private static async Task<int> LoadReadTargetCountAsync(SqliteConnection connection, string messageId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT ReadTargetCount FROM ChatMessages WHERE MessageId = $messageId;";
        command.Parameters.AddWithValue("$messageId", messageId);
        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return value is null or DBNull ? 0 : Convert.ToInt32(value);
    }

    private static async Task<int> CountReadReceiptsAsync(SqliteConnection connection, string messageId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM MessageReadReceipts WHERE MessageId = $messageId;";
        command.Parameters.AddWithValue("$messageId", messageId);
        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return value is null or DBNull ? 0 : Convert.ToInt32(value);
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
