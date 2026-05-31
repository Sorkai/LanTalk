using LanTalk.Core.Enums;
using LanTalk.Core.Models;
using LanTalk.Storage.Database;
using Microsoft.Data.Sqlite;

namespace LanTalk.Storage.Repositories;

public sealed class FileTransferRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public FileTransferRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task SaveAsync(FileTransferRecord record, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT OR REPLACE INTO FileTransfers
                (FileId, SenderId, ReceiverId, FileName, FileSize, SavePath, TransferKind, BatchId, RelativePath, BytesTransferred, Status, TransferTime)
            VALUES
                ($fileId, $senderId, $receiverId, $fileName, $fileSize, $savePath, $transferKind, $batchId, $relativePath, $bytesTransferred, $status, $transferTime);
            """;
        command.Parameters.AddWithValue("$fileId", record.FileId);
        command.Parameters.AddWithValue("$senderId", record.SenderId);
        command.Parameters.AddWithValue("$receiverId", record.ReceiverId);
        command.Parameters.AddWithValue("$fileName", record.FileName);
        command.Parameters.AddWithValue("$fileSize", record.FileSize);
        command.Parameters.AddWithValue("$savePath", (object?)record.SavePath ?? DBNull.Value);
        command.Parameters.AddWithValue("$transferKind", record.TransferKind.ToString());
        command.Parameters.AddWithValue("$batchId", (object?)record.BatchId ?? DBNull.Value);
        command.Parameters.AddWithValue("$relativePath", (object?)record.RelativePath ?? DBNull.Value);
        command.Parameters.AddWithValue("$bytesTransferred", record.BytesTransferred);
        command.Parameters.AddWithValue("$status", record.Status.ToString());
        command.Parameters.AddWithValue("$transferTime", record.TransferTime.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FileTransferRecord>> LoadRecentAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT FileId, SenderId, ReceiverId, FileName, FileSize, SavePath, TransferKind, BatchId, RelativePath, BytesTransferred, Status, TransferTime
            FROM FileTransfers
            ORDER BY TransferTime DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", limit);

        var records = new List<FileTransferRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            records.Add(new FileTransferRecord
            {
                FileId = reader.GetString(0),
                SenderId = reader.GetString(1),
                ReceiverId = reader.GetString(2),
                FileName = reader.GetString(3),
                FileSize = reader.GetInt64(4),
                SavePath = reader.IsDBNull(5) ? null : reader.GetString(5),
                TransferKind = Enum.Parse<FileTransferKind>(reader.GetString(6)),
                BatchId = reader.IsDBNull(7) ? null : reader.GetString(7),
                RelativePath = reader.IsDBNull(8) ? null : reader.GetString(8),
                BytesTransferred = reader.GetInt64(9),
                Status = Enum.Parse<FileTransferStatus>(reader.GetString(10)),
                TransferTime = DateTimeOffset.Parse(reader.GetString(11))
            });
        }

        return records;
    }
}
