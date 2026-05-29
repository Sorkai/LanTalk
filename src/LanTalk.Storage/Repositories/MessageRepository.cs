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
                (MessageId, SessionId, SenderId, ReceiverId, MessageType, Content, SendTime, IsMine)
            VALUES
                ($messageId, $sessionId, $senderId, $receiverId, $messageType, $content, $sendTime, $isMine);
            """;
        command.Parameters.AddWithValue("$messageId", message.MessageId);
        command.Parameters.AddWithValue("$sessionId", message.SessionId);
        command.Parameters.AddWithValue("$senderId", message.SenderId);
        command.Parameters.AddWithValue("$receiverId", (object?)message.ReceiverId ?? DBNull.Value);
        command.Parameters.AddWithValue("$messageType", message.Kind.ToString());
        command.Parameters.AddWithValue("$content", message.Content);
        command.Parameters.AddWithValue("$sendTime", message.SendTime.ToString("O"));
        command.Parameters.AddWithValue("$isMine", message.IsMine ? 1 : 0);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ChatMessage>> LoadRecentMessagesAsync(string sessionId, int limit = NetworkConstants.RecentMessageLimit, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT MessageId, SessionId, SenderId, ReceiverId, MessageType, Content, SendTime, IsMine
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
            IsMine = reader.GetInt32(7) == 1
        };
    }
}

