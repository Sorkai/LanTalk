namespace LanTalk.Core.Compression;

public interface ICompressor
{
    Stream Compress(Stream source);

    Stream Decompress(Stream source);
}

