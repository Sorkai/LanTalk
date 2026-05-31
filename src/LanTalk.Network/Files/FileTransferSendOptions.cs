namespace LanTalk.Network.Files;

public sealed class FileTransferSendOptions
{
    public byte[]? EncryptionKey { get; init; }

    public int ChunkSize { get; init; }
}
