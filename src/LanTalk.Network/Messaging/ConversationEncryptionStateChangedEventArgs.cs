namespace LanTalk.Network.Messaging;

public sealed class ConversationEncryptionStateChangedEventArgs : EventArgs
{
    public ConversationEncryptionStateChangedEventArgs(ConversationEncryptionState state)
    {
        State = state;
    }

    public ConversationEncryptionState State { get; }
}
