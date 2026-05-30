using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using LanTalk.Core.Constants;
using LanTalk.Core.Enums;
using LanTalk.Core.Models;
using LanTalk.Core.Networking;
using LanTalk.Core.Serialization;
using LanTalk.Core.Services;

namespace LanTalk.Network.Discovery;

public sealed class DiscoveryService : IAsyncDisposable
{
    private readonly OnlineUserRegistry _registry;
    private readonly UdpDiscoveryServer _server;
    private readonly ILanTalkLogger _logger;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;
    private Task? _heartbeatTask;
    private AppSettings? _settings;

    public DiscoveryService(OnlineUserRegistry registry, UdpDiscoveryServer server, ILanTalkLogger logger)
    {
        _registry = registry;
        _server = server;
        _logger = logger;
    }

    public event EventHandler<IReadOnlyCollection<UserInfo>>? UsersChanged
    {
        add => _registry.UsersChanged += value;
        remove => _registry.UsersChanged -= value;
    }

    public IReadOnlyCollection<UserInfo> OnlineUsers => _registry.Users;

    public async Task StartAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        if (_cts is not null)
        {
            return;
        }

        _settings = settings;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listenTask = Task.Run(() => _server.StartAsync(settings.UdpPort, HandlePacketAsync, _cts.Token), _cts.Token);
        _heartbeatTask = Task.Run(() => RunHeartbeatAsync(_cts.Token), _cts.Token);

        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        await SendDiscoveryPacketAsync(PacketType.Hello, cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync()
    {
        if (_settings is not null)
        {
            await SendDiscoveryPacketAsync(PacketType.Bye, CancellationToken.None).ConfigureAwait(false);
        }

        if (_cts is not null)
        {
            await _cts.CancelAsync().ConfigureAwait(false);
            _cts.Dispose();
            _cts = null;
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_settings is null)
        {
            return;
        }

        _registry.MarkStaleUsersOffline(DateTimeOffset.Now);
        await SendDiscoveryPacketAsync(PacketType.Hello, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunHeartbeatAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(NetworkConstants.HeartbeatIntervalSeconds), cancellationToken).ConfigureAwait(false);
                await SendDiscoveryPacketAsync(PacketType.Heartbeat, cancellationToken).ConfigureAwait(false);
                _registry.MarkStaleUsersOffline(DateTimeOffset.Now);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task HandlePacketAsync(NetworkPacket packet, IPEndPoint endpoint, CancellationToken cancellationToken)
    {
        if (_settings is null || packet.FromUserId == _settings.UserId)
        {
            return;
        }

        if (packet.Type == PacketType.Bye)
        {
            _registry.MarkOffline(packet.FromUserId);
            _logger.Info($"用户离线：{packet.FromUserId}");
            return;
        }

        var payload = JsonSerializer.Deserialize(packet.PayloadJson, LanTalkJsonContext.Default.DiscoveryPayload);
        if (payload is null)
        {
            return;
        }

        _registry.Upsert(new UserInfo
        {
            UserId = payload.UserId,
            Nickname = payload.Nickname,
            IpAddress = endpoint.Address.ToString(),
            MessagePort = payload.MessagePort,
            FilePort = payload.FilePort,
            Status = UserStatus.Online,
            LastSeenTime = DateTimeOffset.Now
        });

        if (packet.Type == PacketType.Hello)
        {
            await SendDiscoveryPacketAsync(PacketType.Online, cancellationToken, endpoint.Address).ConfigureAwait(false);
        }
    }

    private Task SendDiscoveryPacketAsync(PacketType type, CancellationToken cancellationToken, IPAddress? address = null)
    {
        if (_settings is null)
        {
            return Task.CompletedTask;
        }

        var payload = new DiscoveryPayload(
            _settings.UserId,
            _settings.Nickname,
            _settings.MessagePort,
            _settings.FilePort);

        var packet = new NetworkPacket
        {
            Type = type,
            FromUserId = _settings.UserId,
            PayloadJson = JsonSerializer.Serialize(payload, LanTalkJsonContext.Default.DiscoveryPayload)
        };

        if (address is not null)
        {
            return _server.SendAsync(packet, _settings.UdpPort, address, cancellationToken);
        }

        return SendToDiscoveryTargetsAsync(packet, cancellationToken);
    }

    private async Task SendToDiscoveryTargetsAsync(NetworkPacket packet, CancellationToken cancellationToken)
    {
        if (_settings is null)
        {
            return;
        }

        var targets = DiscoverySubnetResolver.GetBroadcastAddresses(_settings.DiscoverySubnet);
        foreach (var target in targets)
        {
            try
            {
                await _server.SendAsync(packet, _settings.UdpPort, target, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is SocketException or ObjectDisposedException or InvalidOperationException)
            {
                _logger.Warning($"UDP 自动发现发送到 {target} 失败：{ex.Message}");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);

        if (_listenTask is not null)
        {
            await Task.WhenAny(_listenTask, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
        }

        if (_heartbeatTask is not null)
        {
            await Task.WhenAny(_heartbeatTask, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
        }

        if (_server is not null)
        {
            await _server.DisposeAsync().ConfigureAwait(false);
        }

        _cts?.Dispose();
    }
}
