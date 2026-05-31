namespace LanTalk.Core.Models;

public sealed record EncryptionHelloPayload(
    string SessionId,
    string KeyId,
    string PublicKey,
    int MessagePort,
    string Algorithm);
