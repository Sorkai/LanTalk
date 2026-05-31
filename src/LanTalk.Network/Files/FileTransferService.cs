using System.Buffers;

namespace LanTalk.Network.Files;

public sealed class FileTransferService
{
    private readonly TcpFileClient _client;

    public FileTransferService()
        : this(new TcpFileClient())
    {
    }

    public FileTransferService(TcpFileClient client)
    {
        _client = client;
    }

    public Task SendFileAsync(
        string ipAddress,
        int port,
        string fileId,
        string sourcePath,
        IProgress<double>? progress = null,
        long resumeOffset = 0,
        FileTransferSendOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return _client.SendFileAsync(ipAddress, port, fileId, sourcePath, progress, resumeOffset, options, cancellationToken);
    }

    public async Task CopyFileAsync(
        string sourcePath,
        Stream destination,
        IProgress<double>? progress = null,
        long resumeOffset = 0,
        CancellationToken cancellationToken = default)
    {
        var fileInfo = new FileInfo(sourcePath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("要发送的文件不存在。", sourcePath);
        }

        if (resumeOffset < 0 || resumeOffset > fileInfo.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(resumeOffset), "续传偏移量不能小于 0 或大于文件大小。");
        }

        await using var source = File.OpenRead(sourcePath);
        source.Seek(resumeOffset, SeekOrigin.Begin);
        var buffer = ArrayPool<byte>.Shared.Rent(LanTalk.Core.Constants.NetworkConstants.FileTransferBufferSize);
        long sent = resumeOffset;

        progress?.Report(fileInfo.Length == 0 ? 100 : sent * 100d / fileInfo.Length);

        try
        {
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                sent += read;
                progress?.Report(fileInfo.Length == 0 ? 100 : sent * 100d / fileInfo.Length);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
