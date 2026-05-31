using System.Security.Cryptography;
using System.Text;
using LanTalk.Core.Models;

namespace LanTalk.Network.Messaging;

public sealed class EndToEndEncryptionManager : IDisposable
{
    public const string Algorithm = "ECDH-P256-AES-256-GCM";

    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly object gate = new();
    private readonly Dictionary<string, PendingEncryptionSession> pendingSessions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ActiveEncryptionSession> activeSessions = new(StringComparer.Ordinal);

    public ConversationEncryptionState GetState(string peerUserId)
    {
        lock (gate)
        {
            if (activeSessions.TryGetValue(peerUserId, out var active))
            {
                return new ConversationEncryptionState(peerUserId, true, false, active.Fingerprint, $"端到端加密已启用 · 指纹 {active.Fingerprint}");
            }

            if (pendingSessions.ContainsKey(peerUserId))
            {
                return new ConversationEncryptionState(peerUserId, false, true, string.Empty, "端到端加密正在协商");
            }
        }

        return ConversationEncryptionState.Disabled(peerUserId);
    }

    public EncryptionHelloPayload CreateHello(string localUserId, string peerUserId, int messagePort)
    {
        var localKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var keyId = Guid.NewGuid().ToString("N");
        var publicKey = Convert.ToBase64String(localKey.ExportSubjectPublicKeyInfo());
        var sessionId = CreateSessionId(localUserId, peerUserId);

        lock (gate)
        {
            RemoveNoLock(peerUserId);
            pendingSessions[peerUserId] = new PendingEncryptionSession(localKey, keyId, sessionId);
        }

        return new EncryptionHelloPayload(sessionId, keyId, publicKey, messagePort, Algorithm);
    }

    public EncryptionAckPayload AcceptHello(string localUserId, string peerUserId, EncryptionHelloPayload hello, int messagePort)
    {
        ValidateAlgorithm(hello.Algorithm);

        using var localKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var publicKey = Convert.ToBase64String(localKey.ExportSubjectPublicKeyInfo());
        var key = DeriveKey(localKey, hello.PublicKey, localUserId, peerUserId, hello.SessionId, hello.KeyId);
        var fingerprint = CreateFingerprint(key);

        lock (gate)
        {
            RemoveNoLock(peerUserId);
            activeSessions[peerUserId] = new ActiveEncryptionSession(key, hello.KeyId, hello.SessionId, fingerprint);
        }

        return new EncryptionAckPayload(hello.SessionId, hello.KeyId, publicKey, messagePort, Algorithm);
    }

    public void CompleteHandshake(string localUserId, string peerUserId, EncryptionAckPayload ack)
    {
        ValidateAlgorithm(ack.Algorithm);

        PendingEncryptionSession pending;
        lock (gate)
        {
            if (!pendingSessions.TryGetValue(peerUserId, out pending!) ||
                !string.Equals(pending.KeyId, ack.KeyId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("未找到匹配的端到端加密协商。");
            }

            pendingSessions.Remove(peerUserId);
        }

        var key = DeriveKey(pending.LocalKey, ack.PublicKey, localUserId, peerUserId, ack.SessionId, ack.KeyId);
        var fingerprint = CreateFingerprint(key);
        pending.Dispose();

        lock (gate)
        {
            RemoveActiveNoLock(peerUserId);
            activeSessions[peerUserId] = new ActiveEncryptionSession(key, ack.KeyId, ack.SessionId, fingerprint);
        }
    }

    public EncryptedMessagePayload Encrypt(string peerUserId, byte[] plaintext, byte[] associatedData)
    {
        ActiveEncryptionSession session;
        lock (gate)
        {
            if (!activeSessions.TryGetValue(peerUserId, out session!))
            {
                throw new InvalidOperationException("当前会话尚未启用端到端加密。");
            }
        }

        var nonce = new byte[NonceSize];
        var cipherText = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(session.Key, TagSize);
        aes.Encrypt(nonce, plaintext, cipherText, tag, associatedData);

        return new EncryptedMessagePayload(
            Algorithm,
            session.KeyId,
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(cipherText),
            Convert.ToBase64String(tag));
    }

    public byte[] Decrypt(string peerUserId, EncryptedMessagePayload payload, byte[] associatedData)
    {
        ValidateAlgorithm(payload.Algorithm);

        ActiveEncryptionSession session;
        lock (gate)
        {
            if (!activeSessions.TryGetValue(peerUserId, out session!) ||
                !string.Equals(session.KeyId, payload.KeyId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("当前会话没有可用的端到端加密密钥。");
            }
        }

        var nonce = Convert.FromBase64String(payload.Nonce);
        var cipherText = Convert.FromBase64String(payload.CipherText);
        var tag = Convert.FromBase64String(payload.Tag);
        var plaintext = new byte[cipherText.Length];

        using var aes = new AesGcm(session.Key, TagSize);
        aes.Decrypt(nonce, cipherText, tag, plaintext, associatedData);
        return plaintext;
    }

    public void Disable(string peerUserId)
    {
        lock (gate)
        {
            RemoveNoLock(peerUserId);
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            foreach (var pending in pendingSessions.Values)
            {
                pending.Dispose();
            }

            foreach (var active in activeSessions.Values)
            {
                active.Dispose();
            }

            pendingSessions.Clear();
            activeSessions.Clear();
        }
    }

    public static string CreateSessionId(string firstUserId, string secondUserId)
    {
        return string.CompareOrdinal(firstUserId, secondUserId) <= 0
            ? $"{firstUserId}:{secondUserId}"
            : $"{secondUserId}:{firstUserId}";
    }

    private static byte[] DeriveKey(
        ECDiffieHellman localKey,
        string remotePublicKey,
        string localUserId,
        string peerUserId,
        string sessionId,
        string keyId)
    {
        using var remoteKey = ECDiffieHellman.Create();
        remoteKey.ImportSubjectPublicKeyInfo(Convert.FromBase64String(remotePublicKey), out _);

        var sharedSecret = localKey.DeriveKeyMaterial(remoteKey.PublicKey);
        try
        {
            var salt = SHA256.HashData(Encoding.UTF8.GetBytes($"{sessionId}|{CreateSessionId(localUserId, peerUserId)}|{keyId}"));
            var info = Encoding.UTF8.GetBytes(Algorithm);
            return HKDF.DeriveKey(HashAlgorithmName.SHA256, sharedSecret, KeySize, salt, info);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(sharedSecret);
        }
    }

    private static string CreateFingerprint(byte[] key)
    {
        var hash = SHA256.HashData(key);
        var hex = Convert.ToHexString(hash.AsSpan(0, 8));
        return $"{hex[..4]}-{hex[4..8]}-{hex[8..12]}-{hex[12..16]}";
    }

    private static void ValidateAlgorithm(string algorithm)
    {
        if (!string.Equals(algorithm, Algorithm, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"不支持的端到端加密算法：{algorithm}");
        }
    }

    private void RemoveNoLock(string peerUserId)
    {
        if (pendingSessions.Remove(peerUserId, out var pending))
        {
            pending.Dispose();
        }

        RemoveActiveNoLock(peerUserId);
    }

    private void RemoveActiveNoLock(string peerUserId)
    {
        if (activeSessions.Remove(peerUserId, out var active))
        {
            active.Dispose();
        }
    }

    private sealed class PendingEncryptionSession : IDisposable
    {
        public PendingEncryptionSession(ECDiffieHellman localKey, string keyId, string sessionId)
        {
            LocalKey = localKey;
            KeyId = keyId;
            SessionId = sessionId;
        }

        public ECDiffieHellman LocalKey { get; }

        public string KeyId { get; }

        public string SessionId { get; }

        public void Dispose()
        {
            LocalKey.Dispose();
        }
    }

    private sealed class ActiveEncryptionSession : IDisposable
    {
        public ActiveEncryptionSession(byte[] key, string keyId, string sessionId, string fingerprint)
        {
            Key = key;
            KeyId = keyId;
            SessionId = sessionId;
            Fingerprint = fingerprint;
        }

        public byte[] Key { get; }

        public string KeyId { get; }

        public string SessionId { get; }

        public string Fingerprint { get; }

        public void Dispose()
        {
            CryptographicOperations.ZeroMemory(Key);
        }
    }
}
