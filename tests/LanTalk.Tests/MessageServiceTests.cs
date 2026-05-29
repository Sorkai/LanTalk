using LanTalk.Core.Enums;
using LanTalk.Core.Models;
using LanTalk.Core.Services;
using LanTalk.Network.Messaging;

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
}
