namespace LanTalk.Core.Models;

public sealed record EncryptedMessagePayload(
    string Algorithm,
    string KeyId,
    string Nonce,
    string CipherText,
    string Tag);
