namespace LanTalk.Core.Compression;

public interface ICompressor
{
    string Algorithm { get; }

    Task CompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default);

    Task DecompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default);
}
