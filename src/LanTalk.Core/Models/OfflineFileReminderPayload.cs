using LanTalk.Core.Enums;

namespace LanTalk.Core.Models;

public sealed record OfflineFileReminderPayload(
    string ReminderId,
    string FileId,
    string FileName,
    long FileSize,
    string SenderId,
    string SenderNickname,
    string ReceiverId,
    bool IsImage,
    FileTransferKind TransferKind,
    string? BatchId,
    string? BatchName,
    string? GroupId,
    string? GroupName,
    GroupKind GroupKind,
    DateTimeOffset CreatedTime);
