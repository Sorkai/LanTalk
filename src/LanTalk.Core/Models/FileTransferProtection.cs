using LanTalk.Core.Compression;
using LanTalk.Core.Constants;

namespace LanTalk.Core.Models;

public sealed record FileTransferProtection(
    bool IsEncrypted = false,
    string CompressionAlgorithm = CompressionAlgorithms.None,
    int ChunkSize = NetworkConstants.FileTransferBufferSize,
    bool ResumeSupported = true,
    EncryptedMessagePayload? MetadataPayload = null)
{
    public bool UsesCompression =>
        !string.Equals(CompressionAlgorithm, CompressionAlgorithms.None, StringComparison.OrdinalIgnoreCase);
}
