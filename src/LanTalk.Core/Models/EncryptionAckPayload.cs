namespace LanTalk.Core.Models;

public sealed record EncryptionAckPayload(
    string SessionId,
    string KeyId,
    string PublicKey,
    int MessagePort,
    string Algorithm);
