namespace LanTalk.Core.Models;

public sealed record MessageRecallPayload(
    string MessageId,
    string SessionId,
    string SenderUserId,
    string SenderNickname,
    bool IsGroup,
    DateTimeOffset RecallTime);
