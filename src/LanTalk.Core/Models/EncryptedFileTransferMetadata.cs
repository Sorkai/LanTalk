using LanTalk.Core.Enums;

namespace LanTalk.Core.Models;

public sealed record EncryptedFileTransferMetadata(
    string FileName,
    long FileSize,
    bool IsImage,
    FileTransferKind TransferKind,
    string? BatchId = null,
    string? BatchName = null,
    string? RelativePath = null,
    IReadOnlyList<FileTransferItem>? Items = null,
    string? GroupName = null,
    GroupKind GroupKind = GroupKind.Temporary,
    IReadOnlyList<string>? GroupMemberUserIds = null,
    string? GroupMessageId = null);
