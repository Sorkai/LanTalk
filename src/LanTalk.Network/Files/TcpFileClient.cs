using System.Buffers;
using System.Net.Sockets;
using System.Text;
using LanTalk.Core.Constants;

namespace LanTalk.Network.Files;

public sealed class TcpFileClient
{
    private const int FileStreamHeaderMarker = -1;
    private const int FileStreamHeaderVersion = 2;

    public async Task SendFileAsync(
        string ipAddress,
        int port,
        string fileId,
        string sourcePath,
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

        using var client = new TcpClient();
        await client.ConnectAsync(ipAddress, port, cancellationToken).ConfigureAwait(false);
        await using var networkStream = client.GetStream();

        var fileIdBytes = Encoding.UTF8.GetBytes(fileId);
        await networkStream.WriteAsync(BitConverter.GetBytes(FileStreamHeaderMarker), cancellationToken).ConfigureAwait(false);
        await networkStream.WriteAsync(BitConverter.GetBytes(FileStreamHeaderVersion), cancellationToken).ConfigureAwait(false);
        await networkStream.WriteAsync(BitConverter.GetBytes(fileIdBytes.Length), cancellationToken).ConfigureAwait(false);
        await networkStream.WriteAsync(fileIdBytes, cancellationToken).ConfigureAwait(false);
        await networkStream.WriteAsync(BitConverter.GetBytes(fileInfo.Length), cancellationToken).ConfigureAwait(false);
        await networkStream.WriteAsync(BitConverter.GetBytes(resumeOffset), cancellationToken).ConfigureAwait(false);

        await using var fileStream = File.OpenRead(sourcePath);
        fileStream.Seek(resumeOffset, SeekOrigin.Begin);
        var buffer = ArrayPool<byte>.Shared.Rent(NetworkConstants.FileTransferBufferSize);
        long sent = resumeOffset;

        progress?.Report(fileInfo.Length == 0 ? 100 : sent * 100d / fileInfo.Length);

        try
        {
            int read;
            while ((read = await fileStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await networkStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
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
