namespace LanTalk.Network.Messaging;

public sealed record ConversationEncryptionState(
    string UserId,
    bool IsEnabled,
    bool IsPending,
    string Fingerprint,
    string StatusText)
{
    public static ConversationEncryptionState Disabled(string userId)
    {
        return new ConversationEncryptionState(userId, false, false, string.Empty, "端到端加密未启用");
    }
}
