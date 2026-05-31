using LanTalk.Core.Enums;
using LanTalk.Core.Models;
using LanTalk.Core.Services;
using LanTalk.Network.Messaging;
using System.Text;

namespace LanTalk.Tests;

public sealed class MessageServiceTests
{
    [Fact]
    public async Task MessageService_ShouldReceivePrivateMessageOverTcp()
    {
        var logger = new ConsoleLanTalkLogger();
        var service = new MessageService(new TcpMessageClient(), new TcpMessageServer(logger), logger);
        var received = new TaskCompletionSource<NetworkPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
        var port = Random.Shared.Next(56000, 59000);

        service.PacketReceived += (_, packet) => received.TrySetResult(packet);
        await service.StartAsync(port);

        var local = new AppSettings
        {
            UserId = "user-a",
            MessagePort = port,
            FilePort = port + 1,
            UdpPort = port + 2
        };
        var receiver = new UserInfo
        {
            UserId = "user-b",
            Nickname = "接收方",
            IpAddress = "127.0.0.1",
            MessagePort = port,
            FilePort = port + 1,
            Status = UserStatus.Online,
            LastSeenTime = DateTimeOffset.Now
        };
        var message = new ChatMessage
        {
            MessageId = "message-1",
            SessionId = "user-b",
            SenderId = "user-a",
            ReceiverId = "user-b",
            Kind = MessageKind.Private,
            Content = "你好",
            IsMine = true,
            SendTime = DateTimeOffset.Now
        };

        await service.SendPrivateMessageAsync(local, receiver, message);
        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(3)));

        await service.StopAsync();
        await service.DisposeAsync();

        Assert.Same(received.Task, completed);
        var packet = await received.Task;
        Assert.Equal(PacketType.PrivateMessage, packet.Type);
        Assert.Equal("user-a", packet.FromUserId);
    }

    [Fact]
    public async Task MessageService_ShouldSendFileFinishedAndErrorPackets()
    {
        var logger = new ConsoleLanTalkLogger();
        var service = new MessageService(new TcpMessageClient(), new TcpMessageServer(logger), logger);
        var finishedReceived = new TaskCompletionSource<NetworkPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
        var errorReceived = new TaskCompletionSource<NetworkPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
        var port = Random.Shared.Next(56000, 59000);

        service.PacketReceived += (_, packet) =>
        {
            if (packet.Type == PacketType.FileFinished)
            {
                finishedReceived.TrySetResult(packet);
            }

            if (packet.Type == PacketType.Error)
            {
                errorReceived.TrySetResult(packet);
            }
        };

        await service.StartAsync(port);

        var local = new AppSettings { UserId = "user-a" };
        var receiver = new UserInfo
        {
            UserId = "user-b",
            Nickname = "接收方",
            IpAddress = "127.0.0.1",
            MessagePort = port,
            FilePort = port + 1,
            Status = UserStatus.Online,
            LastSeenTime = DateTimeOffset.Now
        };

        await service.SendFileFinishedAsync(local, receiver, "file-1");
        var finishedCompleted = await Task.WhenAny(finishedReceived.Task, Task.Delay(TimeSpan.FromSeconds(3)));

        await service.SendErrorAsync(local, receiver, new ErrorPayload("FILE_TRANSFER_FAILED", "失败", "file-1"));
        var errorCompleted = await Task.WhenAny(errorReceived.Task, Task.Delay(TimeSpan.FromSeconds(3)));

        await service.StopAsync();
        await service.DisposeAsync();

        Assert.Same(finishedReceived.Task, finishedCompleted);
        Assert.Same(errorReceived.Task, errorCompleted);
    }

    [Fact]
    public async Task MessageService_ShouldSendGroupFileRequestOverTcp()
    {
        var logger = new ConsoleLanTalkLogger();
        var service = new MessageService(new TcpMessageClient(), new TcpMessageServer(logger), logger);
        var received = new TaskCompletionSource<NetworkPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
        var port = Random.Shared.Next(56000, 59000);

        service.PacketReceived += (_, packet) => received.TrySetResult(packet);
        await service.StartAsync(port);

        var local = new AppSettings { UserId = "user-a" };
        var receiver = new UserInfo
        {
            UserId = "user-b",
            Nickname = "接收方",
            IpAddress = "127.0.0.1",
            MessagePort = port,
            FilePort = port + 1,
            Status = UserStatus.Online,
            LastSeenTime = DateTimeOffset.Now
        };
        var request = new FileTransferRequest(
            "file-a",
            "photo.png",
            1024,
            "user-a",
            "user-b",
            50002,
            true,
            "group-a",
            "项目组",
            GroupKind.Permanent,
            ["user-a", "user-b"],
            "message-a");

        await service.SendFileRequestAsync(local, receiver, request);
        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(3)));

        await service.StopAsync();
        await service.DisposeAsync();

        Assert.Same(received.Task, completed);
        var packet = await received.Task;
        Assert.Equal(PacketType.FileRequest, packet.Type);

        var restored = System.Text.Json.JsonSerializer.Deserialize(packet.PayloadJson, LanTalk.Core.Serialization.LanTalkJsonContext.Default.FileTransferRequest);
        Assert.NotNull(restored);
        Assert.True(restored.IsGroupTransfer);
        Assert.Equal("group-a", restored.GroupId);
        Assert.Equal("message-a", restored.GroupMessageId);
    }

    [Fact]
    public void EndToEndEncryptionManager_ShouldEncryptAndDecryptPayload()
    {
        using var sender = new EndToEndEncryptionManager();
        using var receiver = new EndToEndEncryptionManager();
        var hello = sender.CreateHello("user-a", "user-b", 50001);
        var ack = receiver.AcceptHello("user-b", "user-a", hello, 50002);
        sender.CompleteHandshake("user-a", "user-b", ack);

        var plaintext = Encoding.UTF8.GetBytes("端到端加密测试");
        var associatedData = Encoding.UTF8.GetBytes("packet-associated-data");

        var encrypted = sender.Encrypt("user-b", plaintext, associatedData);
        var decrypted = receiver.Decrypt("user-a", encrypted, associatedData);

        Assert.NotEqual(Convert.ToBase64String(plaintext), encrypted.CipherText);
        Assert.Equal("端到端加密测试", Encoding.UTF8.GetString(decrypted));
        Assert.True(sender.GetState("user-b").IsEnabled);
        Assert.True(receiver.GetState("user-a").IsEnabled);
        Assert.Equal(sender.GetState("user-b").Fingerprint, receiver.GetState("user-a").Fingerprint);
    }

    [Fact]
    public async Task MessageService_ShouldSendEncryptedPrivateMessageOverTcp()
    {
        var logger = new ConsoleLanTalkLogger();
        var sender = new MessageService(new TcpMessageClient(), new TcpMessageServer(logger), logger);
        var receiver = new MessageService(new TcpMessageClient(), new TcpMessageServer(logger), logger);
        var senderPort = Random.Shared.Next(56000, 57500);
        var receiverPort = Random.Shared.Next(57501, 59000);
        var senderSettings = new AppSettings { UserId = "user-a", MessagePort = senderPort, FilePort = senderPort + 2000, UdpPort = senderPort + 3000 };
        var receiverSettings = new AppSettings { UserId = "user-b", MessagePort = receiverPort, FilePort = receiverPort + 2000, UdpPort = receiverPort + 3000 };
        var received = new TaskCompletionSource<NetworkPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
        var senderEncrypted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        receiver.PacketReceived += (_, packet) => received.TrySetResult(packet);
        sender.EncryptionStateChanged += (_, e) =>
        {
            if (e.State.IsEnabled)
            {
                senderEncrypted.TrySetResult();
            }
        };

        await sender.StartAsync(senderSettings);
        await receiver.StartAsync(receiverSettings);

        var receiverUser = new UserInfo
        {
            UserId = "user-b",
            Nickname = "接收方",
            IpAddress = "127.0.0.1",
            MessagePort = receiverPort,
            FilePort = receiverPort + 2000,
            Status = UserStatus.Online,
            LastSeenTime = DateTimeOffset.Now
        };

        await sender.EnableEncryptionAsync(senderSettings, receiverUser);
        var encryptedCompleted = await Task.WhenAny(senderEncrypted.Task, Task.Delay(TimeSpan.FromSeconds(3)));

        var message = new ChatMessage
        {
            MessageId = "encrypted-message-1",
            SessionId = "user-b",
            SenderId = "user-a",
            ReceiverId = "user-b",
            Kind = MessageKind.Private,
            Content = "这条消息应该加密传输",
            IsMine = true,
            SendTime = DateTimeOffset.Now
        };

        await sender.SendPrivateMessageAsync(senderSettings, receiverUser, message);
        var messageCompleted = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        var senderState = sender.GetEncryptionState("user-b");
        var receiverState = receiver.GetEncryptionState("user-a");

        await sender.StopAsync();
        await receiver.StopAsync();
        await sender.DisposeAsync();
        await receiver.DisposeAsync();

        Assert.Same(senderEncrypted.Task, encryptedCompleted);
        Assert.Same(received.Task, messageCompleted);

        var packet = await received.Task;
        var payload = System.Text.Json.JsonSerializer.Deserialize(packet.PayloadJson, LanTalk.Core.Serialization.LanTalkJsonContext.Default.TextMessagePayload);
        Assert.False(packet.IsEncrypted);
        Assert.NotNull(payload);
        Assert.Equal("这条消息应该加密传输", payload.Content);
        Assert.True(senderState.IsEnabled);
        Assert.True(receiverState.IsEnabled);
    }

    [Fact]
    public async Task MessageService_ShouldSendGroupMessageOverTcp()
    {
        var logger = new ConsoleLanTalkLogger();
        var receiver = new MessageService(new TcpMessageClient(), new TcpMessageServer(logger), logger);
        var received = new TaskCompletionSource<NetworkPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
        var port = Random.Shared.Next(56000, 59000);

        receiver.PacketReceived += (_, packet) => received.TrySetResult(packet);
        await receiver.StartAsync(port);

        var sender = new MessageService(new TcpMessageClient(), new TcpMessageServer(logger), logger);
        var local = new AppSettings { UserId = "user-a", MessagePort = port + 100, FilePort = port + 101, UdpPort = port + 102 };
        var peer = new UserInfo
        {
            UserId = "user-b",
            Nickname = "接收方",
            IpAddress = "127.0.0.1",
            MessagePort = port,
            FilePort = port + 1,
            Status = UserStatus.Online,
            LastSeenTime = DateTimeOffset.Now
        };
        var payload = new GroupMessagePayload(
            "message-a",
            "group-a",
            "项目组",
            GroupKind.Temporary,
            ["user-a", "user-b"],
            "发送方",
            "群组消息");

        var result = await sender.SendGroupMessageAsync(local, [peer], payload);
        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(3)));

        await receiver.StopAsync();
        await receiver.DisposeAsync();
        await sender.DisposeAsync();

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.Same(received.Task, completed);

        var packet = await received.Task;
        Assert.Equal(PacketType.GroupMessage, packet.Type);
        Assert.Equal("user-a", packet.FromUserId);
    }

    [Fact]
    public async Task MessageService_ShouldSendEncryptedGroupMessageOverTcp()
    {
        var logger = new ConsoleLanTalkLogger();
        var sender = new MessageService(new TcpMessageClient(), new TcpMessageServer(logger), logger);
        var receiver = new MessageService(new TcpMessageClient(), new TcpMessageServer(logger), logger);
        var senderPort = Random.Shared.Next(56000, 57500);
        var receiverPort = Random.Shared.Next(57501, 59000);
        var senderSettings = new AppSettings { UserId = "user-a", MessagePort = senderPort, FilePort = senderPort + 2000, UdpPort = senderPort + 3000 };
        var receiverSettings = new AppSettings { UserId = "user-b", MessagePort = receiverPort, FilePort = receiverPort + 2000, UdpPort = receiverPort + 3000 };
        var received = new TaskCompletionSource<NetworkPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
        var senderEncrypted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        receiver.PacketReceived += (_, packet) => received.TrySetResult(packet);
        sender.EncryptionStateChanged += (_, e) =>
        {
            if (e.State.IsEnabled)
            {
                senderEncrypted.TrySetResult();
            }
        };

        await sender.StartAsync(senderSettings);
        await receiver.StartAsync(receiverSettings);

        var receiverUser = new UserInfo
        {
            UserId = "user-b",
            Nickname = "接收方",
            IpAddress = "127.0.0.1",
            MessagePort = receiverPort,
            FilePort = receiverPort + 2000,
            Status = UserStatus.Online,
            LastSeenTime = DateTimeOffset.Now
        };
        var payload = new GroupMessagePayload(
            "encrypted-group-message-1",
            "group-a",
            "项目组",
            GroupKind.Permanent,
            ["user-a", "user-b"],
            "发送方",
            "这条群消息应该逐成员加密传输");

        await sender.EnableEncryptionAsync(senderSettings, receiverUser);
        var encryptedCompleted = await Task.WhenAny(senderEncrypted.Task, Task.Delay(TimeSpan.FromSeconds(3)));

        await sender.SendGroupMessageToAsync(senderSettings, receiverUser, payload, encrypt: true);
        var messageCompleted = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(3)));

        await sender.StopAsync();
        await receiver.StopAsync();
        await sender.DisposeAsync();
        await receiver.DisposeAsync();

        Assert.Same(senderEncrypted.Task, encryptedCompleted);
        Assert.Same(received.Task, messageCompleted);

        var packet = await received.Task;
        var restored = System.Text.Json.JsonSerializer.Deserialize(packet.PayloadJson, LanTalk.Core.Serialization.LanTalkJsonContext.Default.GroupMessagePayload);
        Assert.False(packet.IsEncrypted);
        Assert.Equal(PacketType.GroupMessage, packet.Type);
        Assert.NotNull(restored);
        Assert.Equal("这条群消息应该逐成员加密传输", restored.Content);
    }

    [Fact]
    public async Task MessageService_ShouldRejectEncryptedGroupMessageWithoutSession()
    {
        var logger = new ConsoleLanTalkLogger();
        var sender = new MessageService(new TcpMessageClient(), new TcpMessageServer(logger), logger);
        var settings = new AppSettings { UserId = "user-a" };
        var receiver = new UserInfo
        {
            UserId = "user-b",
            Nickname = "接收方",
            IpAddress = "127.0.0.1",
            MessagePort = 59901,
            FilePort = 59902,
            Status = UserStatus.Online,
            LastSeenTime = DateTimeOffset.Now
        };
        var payload = new GroupMessagePayload(
            "encrypted-group-message-2",
            "group-a",
            "项目组",
            GroupKind.Permanent,
            ["user-a", "user-b"],
            "发送方",
            "缺少密钥不能发送");

        await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendGroupMessageToAsync(settings, receiver, payload, encrypt: true));
        await sender.DisposeAsync();
    }
}
