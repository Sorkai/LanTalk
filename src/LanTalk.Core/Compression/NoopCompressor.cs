using LanTalk.Core.Constants;

namespace LanTalk.Core.Compression;

public sealed class NoopCompressor : ICompressor
{
    public string Algorithm => CompressionAlgorithms.None;

    public Task CompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default)
    {
        return source.CopyToAsync(destination, NetworkConstants.FileTransferBufferSize, cancellationToken);
    }

    public Task DecompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default)
    {
        return source.CopyToAsync(destination, NetworkConstants.FileTransferBufferSize, cancellationToken);
    }
}
