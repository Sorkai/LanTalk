namespace LanTalk.Core.Models;

public sealed record EncryptionCancelPayload(
    string SessionId,
    string Reason);
