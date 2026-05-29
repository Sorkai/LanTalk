using LanTalk.Core.Enums;
using LanTalk.Core.Models;
using LanTalk.Core.Services;
using LanTalk.Network.Messaging;

namespace LanTalk.Tests;

public sealed class BroadcastTests
{
    [Fact]
    public async Task BroadcastAsync_ShouldReportPartialFailures()
    {
        var logger = new ConsoleLanTalkLogger();
        var service = new MessageService(new TcpMessageClient(), new TcpMessageServer(logger), logger);
        var local = new AppSettings { UserId = "sender" };
        var receivers = new[]
        {
            new UserInfo
            {
                UserId = "offline-target",
                Nickname = "离线用户",
                IpAddress = "127.0.0.1",
                MessagePort = 9,
                FilePort = 50002,
                Status = UserStatus.Online,
                LastSeenTime = DateTimeOffset.Now
            }
        };

        var result = await service.BroadcastAsync(local, receivers, "广播测试");

        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
    }
}

