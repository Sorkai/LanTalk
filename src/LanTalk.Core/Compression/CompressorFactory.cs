namespace LanTalk.Core.Compression;

public static class CompressorFactory
{
    public static ICompressor Create(string? algorithm)
    {
        return string.Equals(algorithm, CompressionAlgorithms.GZip, StringComparison.OrdinalIgnoreCase)
            ? new GZipCompressor()
            : new NoopCompressor();
    }
}
