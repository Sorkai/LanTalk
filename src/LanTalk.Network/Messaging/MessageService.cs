using System.Net;
using System.Security.Cryptography;
using System.Text;
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
    private readonly EndToEndEncryptionManager _encryption = new();
    private CancellationTokenSource? _cts;
    private Task? _serverTask;
    private string _localUserId = string.Empty;
    private int _localMessagePort;

    public MessageService(TcpMessageClient client, TcpMessageServer server, ILanTalkLogger logger)
    {
        _client = client;
        _server = server;
        _logger = logger;
    }

    public event EventHandler<NetworkPacket>? PacketReceived;

    public event EventHandler<ConversationEncryptionStateChangedEventArgs>? EncryptionStateChanged;

    public event EventHandler<EncryptionErrorEventArgs>? EncryptionError;

    public Task StartAsync(AppSettings localSettings, CancellationToken cancellationToken = default)
    {
        _localUserId = localSettings.UserId;
        _localMessagePort = localSettings.MessagePort;
        return StartAsync(localSettings.MessagePort, cancellationToken);
    }

    public Task StartAsync(int port, CancellationToken cancellationToken = default)
    {
        if (_cts is not null)
        {
            return Task.CompletedTask;
        }

        _localMessagePort = port;
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

    public ConversationEncryptionState GetEncryptionState(string peerUserId)
    {
        return _encryption.GetState(peerUserId);
    }

    public async Task EnableEncryptionAsync(AppSettings localSettings, UserInfo receiver, CancellationToken cancellationToken = default)
    {
        _localUserId = localSettings.UserId;
        _localMessagePort = localSettings.MessagePort;

        var payload = _encryption.CreateHello(localSettings.UserId, receiver.UserId, localSettings.MessagePort);
        EmitEncryptionState(receiver.UserId);

        var packet = new NetworkPacket
        {
            Type = PacketType.EncryptionHello,
            FromUserId = localSettings.UserId,
            ToUserId = receiver.UserId,
            PayloadJson = JsonSerializer.Serialize(payload, LanTalkJsonContext.Default.EncryptionHelloPayload)
        };

        try
        {
            await _client.SendAsync(receiver.IpAddress, receiver.MessagePort, packet, cancellationToken).ConfigureAwait(false);
            _logger.Info($"已向 {receiver.UserId} 发起端到端加密协商。");
        }
        catch
        {
            _encryption.Disable(receiver.UserId);
            EmitEncryptionState(receiver.UserId);
            throw;
        }
    }

    public async Task DisableEncryptionAsync(AppSettings localSettings, UserInfo receiver, CancellationToken cancellationToken = default)
    {
        _encryption.Disable(receiver.UserId);
        EmitEncryptionState(receiver.UserId);

        var payload = new EncryptionCancelPayload(
            EndToEndEncryptionManager.CreateSessionId(localSettings.UserId, receiver.UserId),
            "用户关闭端到端加密");
        var packet = new NetworkPacket
        {
            Type = PacketType.EncryptionCancel,
            FromUserId = localSettings.UserId,
            ToUserId = receiver.UserId,
            PayloadJson = JsonSerializer.Serialize(payload, LanTalkJsonContext.Default.EncryptionCancelPayload)
        };

        await _client.SendAsync(receiver.IpAddress, receiver.MessagePort, packet, cancellationToken).ConfigureAwait(false);
        _logger.Info($"已关闭与 {receiver.UserId} 的端到端加密会话。");
    }

    public async Task SendPrivateMessageAsync(AppSettings localSettings, UserInfo receiver, ChatMessage message, CancellationToken cancellationToken = default)
    {
        var payload = new TextMessagePayload(message.MessageId, message.SessionId, message.Content);
        var packet = CreatePrivateMessagePacket(localSettings.UserId, receiver.UserId, payload);
        await _client.SendAsync(receiver.IpAddress, receiver.MessagePort, packet, cancellationToken).ConfigureAwait(false);
        _logger.Info($"私聊消息已发送：{message.MessageId} -> {receiver.Nickname}({receiver.UserId})。");
    }

    public async Task SendFileRequestAsync(AppSettings localSettings, UserInfo receiver, FileTransferRequest request, CancellationToken cancellationToken = default)
    {
        var packet = new NetworkPacket
        {
            Type = PacketType.FileRequest,
            FromUserId = localSettings.UserId,
            ToUserId = receiver.UserId,
            PayloadJson = JsonSerializer.Serialize(request, LanTalkJsonContext.Default.FileTransferRequest)
        };

        await _client.SendAsync(receiver.IpAddress, receiver.MessagePort, packet, cancellationToken).ConfigureAwait(false);
        _logger.Info($"{request.FileName} 的文件请求已发送给 {receiver.Nickname}({receiver.UserId})。");
    }

    public async Task SendFileResponseAsync(AppSettings localSettings, UserInfo receiver, FileTransferResponse response, CancellationToken cancellationToken = default)
    {
        var packet = new NetworkPacket
        {
            Type = response.Accepted ? PacketType.FileAccept : PacketType.FileReject,
            FromUserId = localSettings.UserId,
            ToUserId = receiver.UserId,
            PayloadJson = JsonSerializer.Serialize(response, LanTalkJsonContext.Default.FileTransferResponse)
        };

        await _client.SendAsync(receiver.IpAddress, receiver.MessagePort, packet, cancellationToken).ConfigureAwait(false);
        _logger.Info($"文件响应已发送：{response.FileId} -> {receiver.Nickname}({receiver.UserId})，结果 {(response.Accepted ? "接受" : "拒绝")}。");
    }

    public async Task SendFileFinishedAsync(AppSettings localSettings, UserInfo receiver, string fileId, CancellationToken cancellationToken = default)
    {
        var packet = new NetworkPacket
        {
            Type = PacketType.FileFinished,
            FromUserId = localSettings.UserId,
            ToUserId = receiver.UserId,
            PayloadJson = JsonSerializer.Serialize(new FileTransferFinished(fileId), LanTalkJsonContext.Default.FileTransferFinished)
        };

        await _client.SendAsync(receiver.IpAddress, receiver.MessagePort, packet, cancellationToken).ConfigureAwait(false);
        _logger.Info($"文件完成确认已发送：{fileId} -> {receiver.Nickname}({receiver.UserId})。");
    }

    public async Task SendErrorAsync(AppSettings localSettings, UserInfo receiver, ErrorPayload error, CancellationToken cancellationToken = default)
    {
        var packet = new NetworkPacket
        {
            Type = PacketType.Error,
            FromUserId = localSettings.UserId,
            ToUserId = receiver.UserId,
            PayloadJson = JsonSerializer.Serialize(error, LanTalkJsonContext.Default.ErrorPayload)
        };

        await _client.SendAsync(receiver.IpAddress, receiver.MessagePort, packet, cancellationToken).ConfigureAwait(false);
        _logger.Warning($"错误通知已发送：{error.Code} -> {receiver.Nickname}({receiver.UserId})。");
    }

    public async Task SendReadReceiptAsync(AppSettings localSettings, UserInfo receiver, MessageReadReceiptPayload receipt, CancellationToken cancellationToken = default)
    {
        var packet = new NetworkPacket
        {
            Type = PacketType.MessageReadReceipt,
            FromUserId = localSettings.UserId,
            ToUserId = receiver.UserId,
            PayloadJson = JsonSerializer.Serialize(receipt, LanTalkJsonContext.Default.MessageReadReceiptPayload)
        };

        await _client.SendAsync(receiver.IpAddress, receiver.MessagePort, packet, cancellationToken).ConfigureAwait(false);
        _logger.Info($"已读回执已发送：{receipt.MessageId} -> {receiver.Nickname}({receiver.UserId})。");
    }

    public async Task SendMessageRecallAsync(AppSettings localSettings, UserInfo receiver, MessageRecallPayload recall, CancellationToken cancellationToken = default)
    {
        var packet = new NetworkPacket
        {
            Type = PacketType.MessageRecall,
            FromUserId = localSettings.UserId,
            ToUserId = receiver.UserId,
            PayloadJson = JsonSerializer.Serialize(recall, LanTalkJsonContext.Default.MessageRecallPayload)
        };

        await _client.SendAsync(receiver.IpAddress, receiver.MessagePort, packet, cancellationToken).ConfigureAwait(false);
        _logger.Info($"撤回通知已发送：{recall.MessageId} -> {receiver.Nickname}({receiver.UserId})。");
    }

    public async Task SendOfflineFileReminderAsync(AppSettings localSettings, UserInfo receiver, OfflineFileReminderPayload reminder, CancellationToken cancellationToken = default)
    {
        var packet = new NetworkPacket
        {
            Type = PacketType.OfflineFileReminder,
            FromUserId = localSettings.UserId,
            ToUserId = receiver.UserId,
            PayloadJson = JsonSerializer.Serialize(reminder, LanTalkJsonContext.Default.OfflineFileReminderPayload)
        };

        await _client.SendAsync(receiver.IpAddress, receiver.MessagePort, packet, cancellationToken).ConfigureAwait(false);
        _logger.Info($"离线文件提醒已发送：{reminder.FileId} -> {receiver.Nickname}({receiver.UserId})。");
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

        _logger.Info($"广播发送完成：成功 {success}，失败 {failure}。");
        return new BroadcastSendResult(success, failure);
    }

    public async Task<BroadcastSendResult> SendGroupMessageAsync(
        AppSettings localSettings,
        IEnumerable<UserInfo> receivers,
        GroupMessagePayload payload,
        bool encrypt = false,
        CancellationToken cancellationToken = default)
    {
        var success = 0;
        var failure = 0;

        foreach (var receiver in receivers.Where(user => user.UserId != localSettings.UserId && user.Status == UserStatus.Online))
        {
            try
            {
                await SendGroupMessageToAsync(localSettings, receiver, payload, encrypt, cancellationToken).ConfigureAwait(false);
                success++;
            }
            catch (Exception ex)
            {
                failure++;
                _logger.Warning($"群组消息发送给 {receiver.Nickname}({receiver.IpAddress}) 失败：{ex.Message}");
            }
        }

        _logger.Info($"群组消息批量发送完成：群组 {payload.GroupId}，成功 {success}，失败 {failure}，加密 {encrypt}。");
        return new BroadcastSendResult(success, failure);
    }

    public async Task SendGroupMessageToAsync(
        AppSettings localSettings,
        UserInfo receiver,
        GroupMessagePayload payload,
        bool encrypt = false,
        CancellationToken cancellationToken = default)
    {
        var packet = CreateGroupMessagePacket(localSettings.UserId, receiver.UserId, payload, encrypt);
        await _client.SendAsync(receiver.IpAddress, receiver.MessagePort, packet, cancellationToken).ConfigureAwait(false);
        _logger.Info($"群组消息已发送：{payload.MessageId} -> {receiver.Nickname}({receiver.UserId})，加密 {encrypt}。");
    }

    private NetworkPacket CreatePrivateMessagePacket(string fromUserId, string toUserId, TextMessagePayload payload)
    {
        var packetId = Guid.NewGuid().ToString("N");
        var time = DateTimeOffset.Now;
        var payloadJson = JsonSerializer.Serialize(payload, LanTalkJsonContext.Default.TextMessagePayload);

        if (!_encryption.GetState(toUserId).IsEnabled)
        {
            return new NetworkPacket
            {
                PacketId = packetId,
                Type = PacketType.PrivateMessage,
                FromUserId = fromUserId,
                ToUserId = toUserId,
                Time = time,
                PayloadJson = payloadJson
            };
        }

        var associatedData = BuildAssociatedData(packetId, PacketType.PrivateMessage, fromUserId, toUserId, time);
        var encrypted = _encryption.Encrypt(toUserId, Encoding.UTF8.GetBytes(payloadJson), associatedData);
        return new NetworkPacket
        {
            PacketId = packetId,
            Type = PacketType.PrivateMessage,
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Time = time,
            IsEncrypted = true,
            PayloadJson = JsonSerializer.Serialize(encrypted, LanTalkJsonContext.Default.EncryptedMessagePayload)
        };
    }

    private NetworkPacket CreateGroupMessagePacket(string fromUserId, string toUserId, GroupMessagePayload payload, bool encrypt)
    {
        var packetId = Guid.NewGuid().ToString("N");
        var time = DateTimeOffset.Now;
        var payloadJson = JsonSerializer.Serialize(payload, LanTalkJsonContext.Default.GroupMessagePayload);

        if (!encrypt)
        {
            return new NetworkPacket
            {
                PacketId = packetId,
                Type = PacketType.GroupMessage,
                FromUserId = fromUserId,
                ToUserId = toUserId,
                Time = time,
                PayloadJson = payloadJson
            };
        }

        if (!_encryption.GetState(toUserId).IsEnabled)
        {
            throw new InvalidOperationException("目标成员尚未启用一对一端到端加密会话，无法发送加密群组消息。");
        }

        var associatedData = BuildAssociatedData(packetId, PacketType.GroupMessage, fromUserId, toUserId, time);
        var encrypted = _encryption.Encrypt(toUserId, Encoding.UTF8.GetBytes(payloadJson), associatedData);
        return new NetworkPacket
        {
            PacketId = packetId,
            Type = PacketType.GroupMessage,
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Time = time,
            IsEncrypted = true,
            PayloadJson = JsonSerializer.Serialize(encrypted, LanTalkJsonContext.Default.EncryptedMessagePayload)
        };
    }

    private async Task HandlePacketAsync(NetworkPacket packet, IPEndPoint? remoteEndPoint, CancellationToken cancellationToken)
    {
        if (packet.Type == PacketType.EncryptionHello)
        {
            await HandleEncryptionHelloAsync(packet, remoteEndPoint, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (packet.Type == PacketType.EncryptionAck)
        {
            HandleEncryptionAck(packet);
            return;
        }

        if (packet.Type == PacketType.EncryptionCancel)
        {
            HandleEncryptionCancel(packet);
            return;
        }

        if ((packet.Type is PacketType.PrivateMessage or PacketType.GroupMessage) && packet.IsEncrypted)
        {
            try
            {
                packet = DecryptMessagePacket(packet);
            }
            catch
            {
                return;
            }
        }

        _logger.Info($"收到 TCP 消息包：{packet.Type}，来自 {packet.FromUserId}。");
        PacketReceived?.Invoke(this, packet);
    }

    private async Task HandleEncryptionHelloAsync(NetworkPacket packet, IPEndPoint? remoteEndPoint, CancellationToken cancellationToken)
    {
        var hello = JsonSerializer.Deserialize(packet.PayloadJson, LanTalkJsonContext.Default.EncryptionHelloPayload);
        if (hello is null || remoteEndPoint is null)
        {
            return;
        }

        try
        {
            var localUserId = ResolveLocalUserId(packet);
            var ack = _encryption.AcceptHello(localUserId, packet.FromUserId, hello, _localMessagePort);
            var response = new NetworkPacket
            {
                Type = PacketType.EncryptionAck,
                FromUserId = localUserId,
                ToUserId = packet.FromUserId,
                PayloadJson = JsonSerializer.Serialize(ack, LanTalkJsonContext.Default.EncryptionAckPayload)
            };

            await _client.SendAsync(remoteEndPoint.Address.ToString(), hello.MessagePort, response, cancellationToken).ConfigureAwait(false);
            EmitEncryptionState(packet.FromUserId);
            _logger.Info($"已接受来自 {packet.FromUserId} 的端到端加密协商。");
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException or CryptographicException)
        {
            _logger.Error("端到端加密协商失败。", ex);
            EncryptionError?.Invoke(this, new EncryptionErrorEventArgs(packet.FromUserId, "端到端加密协商失败。", ex));
        }
    }

    private void HandleEncryptionAck(NetworkPacket packet)
    {
        var ack = JsonSerializer.Deserialize(packet.PayloadJson, LanTalkJsonContext.Default.EncryptionAckPayload);
        if (ack is null)
        {
            return;
        }

        try
        {
            _encryption.CompleteHandshake(ResolveLocalUserId(packet), packet.FromUserId, ack);
            EmitEncryptionState(packet.FromUserId);
            _logger.Info($"与 {packet.FromUserId} 的端到端加密已启用。");
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException or CryptographicException)
        {
            _logger.Error("端到端加密确认失败。", ex);
            EncryptionError?.Invoke(this, new EncryptionErrorEventArgs(packet.FromUserId, "端到端加密确认失败。", ex));
        }
    }

    private void HandleEncryptionCancel(NetworkPacket packet)
    {
        _encryption.Disable(packet.FromUserId);
        EmitEncryptionState(packet.FromUserId);
        _logger.Info($"对方已关闭端到端加密：{packet.FromUserId}。");
    }

    private NetworkPacket DecryptMessagePacket(NetworkPacket packet)
    {
        try
        {
            var encrypted = JsonSerializer.Deserialize(packet.PayloadJson, LanTalkJsonContext.Default.EncryptedMessagePayload)
                ?? throw new JsonException("加密消息载荷为空。");
            var associatedData = BuildAssociatedData(packet);
            var plaintext = _encryption.Decrypt(packet.FromUserId, encrypted, associatedData);
            var payloadJson = Encoding.UTF8.GetString(plaintext);

            return new NetworkPacket
            {
                PacketId = packet.PacketId,
                Type = packet.Type,
                FromUserId = packet.FromUserId,
                ToUserId = packet.ToUserId,
                Time = packet.Time,
                PayloadJson = payloadJson
            };
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException or CryptographicException or JsonException)
        {
            _logger.Error("端到端加密消息解密失败。", ex);
            EncryptionError?.Invoke(this, new EncryptionErrorEventArgs(packet.FromUserId, "端到端加密消息解密失败。", ex));
            throw;
        }
    }

    private string ResolveLocalUserId(NetworkPacket packet)
    {
        if (!string.IsNullOrWhiteSpace(_localUserId))
        {
            return _localUserId;
        }

        return packet.ToUserId ?? string.Empty;
    }

    private void EmitEncryptionState(string peerUserId)
    {
        EncryptionStateChanged?.Invoke(this, new ConversationEncryptionStateChangedEventArgs(_encryption.GetState(peerUserId)));
    }

    private static byte[] BuildAssociatedData(NetworkPacket packet)
    {
        return BuildAssociatedData(packet.PacketId, packet.Type, packet.FromUserId, packet.ToUserId, packet.Time);
    }

    private static byte[] BuildAssociatedData(string packetId, PacketType type, string fromUserId, string? toUserId, DateTimeOffset time)
    {
        return Encoding.UTF8.GetBytes($"{packetId}|{type}|{fromUserId}|{toUserId}|{time.ToUnixTimeMilliseconds()}");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);

        if (_serverTask is not null)
        {
            await Task.WhenAny(_serverTask, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
        }

        _encryption.Dispose();
    }
}
