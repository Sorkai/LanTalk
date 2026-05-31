using LanTalk.Core.Enums;
using LanTalk.Core.Models;
using LanTalk.Storage.Database;

namespace LanTalk.Storage.Repositories;

public sealed class OutgoingDeliveryRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public OutgoingDeliveryRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task SaveAsync(OutgoingDeliveryRecord record, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT OR REPLACE INTO OutgoingDeliveries
                (DeliveryId, RecipientId, PacketType, PayloadJson, SourcePath, RequiresEncryption, CreatedTime, LastAttemptTime, AttemptCount, LastError)
            VALUES
                ($deliveryId, $recipientId, $packetType, $payloadJson, $sourcePath, $requiresEncryption, $createdTime, $lastAttemptTime, $attemptCount, $lastError);
            """;
        command.Parameters.AddWithValue("$deliveryId", record.DeliveryId);
        command.Parameters.AddWithValue("$recipientId", record.RecipientId);
        command.Parameters.AddWithValue("$packetType", record.PacketType.ToString());
        command.Parameters.AddWithValue("$payloadJson", record.PayloadJson);
        command.Parameters.AddWithValue("$sourcePath", (object?)record.SourcePath ?? DBNull.Value);
        command.Parameters.AddWithValue("$requiresEncryption", record.RequiresEncryption ? 1 : 0);
        command.Parameters.AddWithValue("$createdTime", record.CreatedTime.ToString("O"));
        command.Parameters.AddWithValue("$lastAttemptTime", record.LastAttemptTime?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$attemptCount", record.AttemptCount);
        command.Parameters.AddWithValue("$lastError", (object?)record.LastError ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OutgoingDeliveryRecord>> LoadForRecipientAsync(
        string recipientId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT DeliveryId, RecipientId, PacketType, PayloadJson, SourcePath, RequiresEncryption, CreatedTime, LastAttemptTime, AttemptCount, LastError
            FROM OutgoingDeliveries
            WHERE RecipientId = $recipientId
            ORDER BY CreatedTime
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$recipientId", recipientId);
        command.Parameters.AddWithValue("$limit", limit);

        var records = new List<OutgoingDeliveryRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            records.Add(new OutgoingDeliveryRecord
            {
                DeliveryId = reader.GetString(0),
                RecipientId = reader.GetString(1),
                PacketType = Enum.Parse<PacketType>(reader.GetString(2)),
                PayloadJson = reader.GetString(3),
                SourcePath = reader.IsDBNull(4) ? null : reader.GetString(4),
                RequiresEncryption = reader.GetInt32(5) == 1,
                CreatedTime = DateTimeOffset.Parse(reader.GetString(6)),
                LastAttemptTime = reader.IsDBNull(7) ? null : DateTimeOffset.Parse(reader.GetString(7)),
                AttemptCount = reader.GetInt32(8),
                LastError = reader.IsDBNull(9) ? null : reader.GetString(9)
            });
        }

        return records;
    }

    public async Task MarkAttemptAsync(
        string deliveryId,
        int attemptCount,
        string lastError,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE OutgoingDeliveries
            SET LastAttemptTime = $lastAttemptTime,
                AttemptCount = $attemptCount,
                LastError = $lastError
            WHERE DeliveryId = $deliveryId;
            """;
        command.Parameters.AddWithValue("$deliveryId", deliveryId);
        command.Parameters.AddWithValue("$lastAttemptTime", DateTimeOffset.Now.ToString("O"));
        command.Parameters.AddWithValue("$attemptCount", attemptCount);
        command.Parameters.AddWithValue("$lastError", lastError);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string deliveryId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM OutgoingDeliveries WHERE DeliveryId = $deliveryId;";
        command.Parameters.AddWithValue("$deliveryId", deliveryId);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
