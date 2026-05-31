using System.Text.Json.Serialization;
using LanTalk.Core.Enums;

namespace LanTalk.Core.Models;

public sealed record FileTransferRequest(
    string FileId,
    string FileName,
    long FileSize,
    string SenderId,
    string ReceiverId,
    int FilePort,
    bool IsImage = false,
    string? GroupId = null,
    string? GroupName = null,
    GroupKind GroupKind = GroupKind.Temporary,
    IReadOnlyList<string>? GroupMemberUserIds = null,
    string? GroupMessageId = null,
    FileTransferKind TransferKind = FileTransferKind.SingleFile,
    string? BatchId = null,
    string? BatchName = null,
    string? RelativePath = null,
    IReadOnlyList<FileTransferItem>? Items = null,
    FileTransferProtection? Protection = null)
{
    [JsonIgnore]
    public bool IsGroupTransfer => !string.IsNullOrWhiteSpace(GroupId);

    [JsonIgnore]
    public bool IsBatchTransfer => TransferKind is FileTransferKind.MultipleFiles or FileTransferKind.Folder || Items is { Count: > 0 };

    [JsonIgnore]
    public IReadOnlyList<FileTransferItem> TransferItems => Items ?? [];

    [JsonIgnore]
    public bool IsProtectedTransfer => Protection is not null;

    [JsonIgnore]
    public bool IsEncryptedTransfer => Protection?.IsEncrypted == true;

    [JsonIgnore]
    public bool UsesCompression => Protection?.UsesCompression == true;
}
