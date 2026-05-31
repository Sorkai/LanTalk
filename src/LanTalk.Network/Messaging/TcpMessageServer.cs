using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using LanTalk.Core.Models;
using LanTalk.Core.Serialization;
using LanTalk.Core.Services;

namespace LanTalk.Network.Messaging;

public sealed class TcpMessageServer
{
    private readonly ILanTalkLogger _logger;

    public TcpMessageServer(ILanTalkLogger logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(int port, Func<NetworkPacket, IPEndPoint?, CancellationToken, Task> onPacket, CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        _logger.Info($"TCP 消息监听已启动，端口 {port}。");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                _ = Task.Run(() => HandleClientAsync(client, onPacket, cancellationToken), cancellationToken);
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

    private async Task HandleClientAsync(TcpClient client, Func<NetworkPacket, IPEndPoint?, CancellationToken, Task> onPacket, CancellationToken cancellationToken)
    {
        using (client)
        {
            try
            {
                var remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                await using var stream = client.GetStream();
                var lengthBuffer = new byte[sizeof(int)];
                await stream.ReadExactlyAsync(lengthBuffer, cancellationToken).ConfigureAwait(false);
                var length = BitConverter.ToInt32(lengthBuffer);
                var buffer = ArrayPool<byte>.Shared.Rent(length);

                try
                {
                    await stream.ReadExactlyAsync(buffer.AsMemory(0, length), cancellationToken).ConfigureAwait(false);
                    var json = Encoding.UTF8.GetString(buffer, 0, length);
                    var packet = JsonSerializer.Deserialize(json, LanTalkJsonContext.Default.NetworkPacket);

                    if (packet is not null)
                    {
                        await onPacket(packet, remoteEndPoint, cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            catch (Exception ex) when (ex is IOException or SocketException or JsonException)
            {
                _logger.Error("TCP 消息接收失败。", ex);
            }
        }
    }
}
