namespace LanTalk.Core.Models;

public sealed record TextMessagePayload(
    string MessageId,
    string SessionId,
    string Content);

