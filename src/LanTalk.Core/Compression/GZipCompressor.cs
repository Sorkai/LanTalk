using System.IO.Compression;
using LanTalk.Core.Constants;

namespace LanTalk.Core.Compression;

public sealed class GZipCompressor : ICompressor
{
    public string Algorithm => CompressionAlgorithms.GZip;

    public async Task CompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default)
    {
        await using var gzip = new GZipStream(destination, CompressionLevel.SmallestSize, leaveOpen: true);
        await source.CopyToAsync(gzip, NetworkConstants.FileTransferBufferSize, cancellationToken).ConfigureAwait(false);
        await gzip.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DecompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default)
    {
        await using var gzip = new GZipStream(source, CompressionMode.Decompress, leaveOpen: true);
        await gzip.CopyToAsync(destination, NetworkConstants.FileTransferBufferSize, cancellationToken).ConfigureAwait(false);
        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
