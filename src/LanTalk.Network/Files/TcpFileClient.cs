using System.Buffers;
using System.Net.Sockets;
using System.Text;
using LanTalk.Core.Constants;

namespace LanTalk.Network.Files;

public sealed class TcpFileClient
{
    public async Task SendFileAsync(
        string ipAddress,
        int port,
        string fileId,
        string sourcePath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var fileInfo = new FileInfo(sourcePath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("要发送的文件不存在。", sourcePath);
        }

        using var client = new TcpClient();
        await client.ConnectAsync(ipAddress, port, cancellationToken).ConfigureAwait(false);
        await using var networkStream = client.GetStream();

        var fileIdBytes = Encoding.UTF8.GetBytes(fileId);
        await networkStream.WriteAsync(BitConverter.GetBytes(fileIdBytes.Length), cancellationToken).ConfigureAwait(false);
        await networkStream.WriteAsync(fileIdBytes, cancellationToken).ConfigureAwait(false);
        await networkStream.WriteAsync(BitConverter.GetBytes(fileInfo.Length), cancellationToken).ConfigureAwait(false);

        await using var fileStream = File.OpenRead(sourcePath);
        var buffer = ArrayPool<byte>.Shared.Rent(NetworkConstants.FileTransferBufferSize);
        long sent = 0;

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

