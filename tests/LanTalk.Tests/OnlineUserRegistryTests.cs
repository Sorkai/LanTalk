using LanTalk.Core.Enums;
using LanTalk.Core.Models;
using LanTalk.Network.Discovery;

namespace LanTalk.Tests;

public sealed class OnlineUserRegistryTests
{
    [Fact]
    public void Upsert_ShouldAddUserAndRaiseChange()
    {
        var registry = new OnlineUserRegistry();
        var changes = 0;
        registry.UsersChanged += (_, _) => changes++;

        registry.Upsert(new UserInfo
        {
            UserId = "user-a",
            Nickname = "测试用户",
            IpAddress = "192.168.1.10",
            MessagePort = 50001,
            FilePort = 50002,
            Status = UserStatus.Online,
            LastSeenTime = DateTimeOffset.Now
        });

        Assert.Single(registry.Users);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void MarkStaleUsersOffline_ShouldSetOfflineAfterTimeout()
    {
        var registry = new OnlineUserRegistry();
        registry.Upsert(new UserInfo
        {
            UserId = "user-a",
            Nickname = "测试用户",
            IpAddress = "192.168.1.10",
            MessagePort = 50001,
            FilePort = 50002,
            Status = UserStatus.Online,
            LastSeenTime = DateTimeOffset.Now.AddSeconds(-30)
        });

        var changed = registry.MarkStaleUsersOffline(DateTimeOffset.Now);

        Assert.Single(changed);
        Assert.Equal(UserStatus.Offline, registry.Users.Single().Status);
    }
}

