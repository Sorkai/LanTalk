using LanTalk.Core.Enums;

namespace LanTalk.Core.Models;

public sealed class FileTransferRecord
{
    public string FileId { get; init; } = Guid.NewGuid().ToString("N");

    public string SenderId { get; init; } = string.Empty;

    public string ReceiverId { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public long FileSize { get; init; }

    public string? SavePath { get; init; }

    public FileTransferStatus Status { get; init; }

    public DateTimeOffset TransferTime { get; init; } = DateTimeOffset.Now;
}

