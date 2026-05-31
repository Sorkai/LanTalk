namespace LanTalk.Core.Models;

public sealed record MessageReadReceiptPayload(
    string MessageId,
    string SessionId,
    string ReaderUserId,
    string ReaderNickname,
    bool IsGroup,
    DateTimeOffset ReadTime);
