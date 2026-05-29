using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using LanTalk.Core.Constants;
using LanTalk.Core.Services;

namespace LanTalk.Network.Files;

public sealed class TcpFileServer
{
    private readonly ILanTalkLogger _logger;

    public TcpFileServer(ILanTalkLogger logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(
        int port,
        Func<string, long, CancellationToken, Task<Stream>> createDestination,
        Func<string, double, CancellationToken, Task>? onProgress,
        CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        _logger.Info($"TCP 文件监听已启动，端口 {port}。");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                _ = Task.Run(() => HandleClientAsync(client, createDestination, onProgress, cancellationToken), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleClientAsync(
        TcpClient client,
        Func<string, long, CancellationToken, Task<Stream>> createDestination,
        Func<string, double, CancellationToken, Task>? onProgress,
        CancellationToken cancellationToken)
    {
        using (client)
        {
            await using var networkStream = client.GetStream();
            var lengthBuffer = new byte[sizeof(int)];
            await networkStream.ReadExactlyAsync(lengthBuffer, cancellationToken).ConfigureAwait(false);
            var fileIdLength = BitConverter.ToInt32(lengthBuffer);
            var fileIdBuffer = new byte[fileIdLength];
            await networkStream.ReadExactlyAsync(fileIdBuffer, cancellationToken).ConfigureAwait(false);
            var fileId = Encoding.UTF8.GetString(fileIdBuffer);

            var sizeBuffer = new byte[sizeof(long)];
            await networkStream.ReadExactlyAsync(sizeBuffer, cancellationToken).ConfigureAwait(false);
            var fileSize = BitConverter.ToInt64(sizeBuffer);

            await using var destination = await createDestination(fileId, fileSize, cancellationToken).ConfigureAwait(false);
            var buffer = ArrayPool<byte>.Shared.Rent(NetworkConstants.FileTransferBufferSize);
            long received = 0;

            try
            {
                while (received < fileSize)
                {
                    var remaining = fileSize - received;
                    var readSize = (int)Math.Min(buffer.Length, remaining);
                    var read = await networkStream.ReadAsync(buffer.AsMemory(0, readSize), cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                    {
                        throw new IOException("文件传输被中断。");
                    }

                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    received += read;

                    if (onProgress is not null)
                    {
                        await onProgress(fileId, fileSize == 0 ? 100 : received * 100d / fileSize, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}

