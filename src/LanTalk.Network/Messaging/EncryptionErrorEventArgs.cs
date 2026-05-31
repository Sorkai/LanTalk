namespace LanTalk.Network.Messaging;

public sealed class EncryptionErrorEventArgs : EventArgs
{
    public EncryptionErrorEventArgs(string peerUserId, string message, Exception? exception = null)
    {
        PeerUserId = peerUserId;
        Message = message;
        Exception = exception;
    }

    public string PeerUserId { get; }

    public string Message { get; }

    public Exception? Exception { get; }
}
