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
    string? GroupMessageId = null)
{
    [JsonIgnore]
    public bool IsGroupTransfer => !string.IsNullOrWhiteSpace(GroupId);
}
