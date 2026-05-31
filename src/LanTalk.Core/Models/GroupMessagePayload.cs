using LanTalk.Core.Enums;

namespace LanTalk.Core.Models;

public sealed record GroupMessagePayload(
    string MessageId,
    string GroupId,
    string GroupName,
    GroupKind GroupKind,
    IReadOnlyList<string> MemberUserIds,
    string SenderNickname,
    string Content);
