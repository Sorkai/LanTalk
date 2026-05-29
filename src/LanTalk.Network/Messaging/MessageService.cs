using System.Text.Json;
using LanTalk.Core.Constants;
using LanTalk.Core.Enums;
using LanTalk.Core.Models;
using LanTalk.Core.Serialization;
using LanTalk.Core.Services;

namespace LanTalk.Network.Messaging;

public sealed class MessageService : IAsyncDisposable
{
    private readonly TcpMessageClient _client;
    private readonly TcpMessageServer _server;
    private readonly ILanTalkLogger _logger;
    private CancellationTokenSource? _cts;
    private Task? _serverTask;

    public MessageService(TcpMessageClient client, TcpMessageServer server, ILanTalkLogger logger)
    {
        _client = client;
        _server = server;
        _logger = logger;
    }

    public event EventHandler<NetworkPacket>? PacketReceived;

    public Task StartAsync(int port, CancellationToken cancellationToken = default)
    {
        if (_cts is not null)
        {
            return Task.CompletedTask;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _serverTask = Task.Run(() => _server.StartAsync(port, HandlePacketAsync, _cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync().ConfigureAwait(false);
            _cts.Dispose();
            _cts = null;
        }
    }

    public Task SendPrivateMessageAsync(AppSettings localSettings, UserInfo receiver, ChatMessage message, CancellationToken cancellationToken = default)
    {
        var payload = new TextMessagePayload(message.MessageId, message.SessionId, message.Content);
        var packet = new NetworkPacket
        {
            Type = PacketType.PrivateMessage,
            FromUserId = localSettings.UserId,
            ToUserId = receiver.UserId,
            PayloadJson = JsonSerializer.Serialize(payload, LanTalkJsonContext.Default.TextMessagePayload)
        };

        return _client.SendAsync(receiver.IpAddress, receiver.MessagePort, packet, cancellationToken);
    }

    public Task SendFileRequestAsync(AppSettings localSettings, UserInfo receiver, FileTransferRequest request, CancellationToken cancellationToken = default)
    {
        var packet = new NetworkPacket
        {
            Type = PacketType.FileRequest,
            FromUserId = localSettings.UserId,
            ToUserId = receiver.UserId,
            PayloadJson = JsonSerializer.Serialize(request, LanTalkJsonContext.Default.FileTransferRequest)
        };

        return _client.SendAsync(receiver.IpAddress, receiver.MessagePort, packet, cancellationToken);
    }

    public Task SendFileResponseAsync(AppSettings localSettings, UserInfo receiver, FileTransferResponse response, CancellationToken cancellationToken = default)
    {
        var packet = new NetworkPacket
        {
            Type = response.Accepted ? PacketType.FileAccept : PacketType.FileReject,
            FromUserId = localSettings.UserId,
            ToUserId = receiver.UserId,
            PayloadJson = JsonSerializer.Serialize(response, LanTalkJsonContext.Default.FileTransferResponse)
        };

        return _client.SendAsync(receiver.IpAddress, receiver.MessagePort, packet, cancellationToken);
    }

    public async Task<BroadcastSendResult> BroadcastAsync(AppSettings localSettings, IEnumerable<UserInfo> receivers, string content, CancellationToken cancellationToken = default)
    {
        var success = 0;
        var failure = 0;

        foreach (var receiver in receivers.Where(user => user.UserId != localSettings.UserId && user.Status == UserStatus.Online))
        {
            var payload = new TextMessagePayload(Guid.NewGuid().ToString("N"), NetworkConstants.BroadcastSessionId, content);
            var packet = new NetworkPacket
            {
                Type = PacketType.BroadcastMessage,
                FromUserId = localSettings.UserId,
                ToUserId = receiver.UserId,
                PayloadJson = JsonSerializer.Serialize(payload, LanTalkJsonContext.Default.TextMessagePayload)
            };

            try
            {
                await _client.SendAsync(receiver.IpAddress, receiver.MessagePort, packet, cancellationToken).ConfigureAwait(false);
                success++;
            }
            catch (Exception ex)
            {
                failure++;
                _logger.Warning($"广播发送给 {receiver.Nickname}({receiver.IpAddress}) 失败：{ex.Message}");
            }
        }

        return new BroadcastSendResult(success, failure);
    }

    private Task HandlePacketAsync(NetworkPacket packet, CancellationToken cancellationToken)
    {
        _logger.Info($"收到 TCP 消息包：{packet.Type}，来自 {packet.FromUserId}。");
        PacketReceived?.Invoke(this, packet);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);

        if (_serverTask is not null)
        {
            await Task.WhenAny(_serverTask, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
        }
    }
}
