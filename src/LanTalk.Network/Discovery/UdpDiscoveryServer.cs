using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using LanTalk.Core.Models;
using LanTalk.Core.Serialization;
using LanTalk.Core.Services;

namespace LanTalk.Network.Discovery;

public sealed class UdpDiscoveryServer : IAsyncDisposable
{
    private readonly ILanTalkLogger _logger;
    private UdpClient? _client;

    public UdpDiscoveryServer(ILanTalkLogger logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(int port, Func<NetworkPacket, IPEndPoint, CancellationToken, Task> onPacket, CancellationToken cancellationToken)
    {
        _client = new UdpClient(port)
        {
            EnableBroadcast = true
        };

        _logger.Info($"UDP 自动发现监听已启动，端口 {port}。");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await _client.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                var json = Encoding.UTF8.GetString(result.Buffer);
                var packet = JsonSerializer.Deserialize(json, LanTalkJsonContext.Default.NetworkPacket);

                if (packet is not null)
                {
                    await onPacket(packet, result.RemoteEndPoint, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (JsonException ex)
            {
                _logger.Warning($"UDP 自动发现包解析失败：{ex.Message}");
            }
            catch (SocketException ex)
            {
                _logger.Error("UDP 自动发现 Socket 异常。", ex);
            }
            catch (Exception ex)
            {
                _logger.Error("UDP 自动发现监听异常。", ex);
            }
        }
    }

    public async Task SendAsync(NetworkPacket packet, int port, IPAddress? address = null, CancellationToken cancellationToken = default)
    {
        if (_client is null)
        {
            _client = new UdpClient
            {
                EnableBroadcast = true
            };
        }

        var targetAddress = address ?? IPAddress.Broadcast;
        var endpoint = new IPEndPoint(targetAddress, port);
        var json = JsonSerializer.Serialize(packet, LanTalkJsonContext.Default.NetworkPacket);
        var bytes = Encoding.UTF8.GetBytes(json);

        await _client.SendAsync(bytes, endpoint, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        _client?.Dispose();
        _client = null;
        return ValueTask.CompletedTask;
    }
}

