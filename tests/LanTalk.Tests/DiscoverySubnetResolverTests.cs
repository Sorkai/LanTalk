using LanTalk.Core.Constants;
using LanTalk.Core.Networking;

namespace LanTalk.Tests;

public sealed class DiscoverySubnetResolverTests
{
    [Fact]
    public void TryNormalize_ShouldUseAutoForEmptyValue()
    {
        var ok = DiscoverySubnetResolver.TryNormalize(" ", out var normalized);

        Assert.True(ok);
        Assert.Equal(NetworkConstants.DefaultDiscoverySubnet, normalized);
    }

    [Fact]
    public void TryNormalize_ShouldNormalizeCidrNetworkAddress()
    {
        var ok = DiscoverySubnetResolver.TryNormalize("192.168.1.42/24", out var normalized);

        Assert.True(ok);
        Assert.Equal("192.168.1.0/24", normalized);
    }

    [Fact]
    public void GetBroadcastAddresses_ShouldResolveCidrBroadcast()
    {
        var addresses = DiscoverySubnetResolver.GetBroadcastAddresses("192.168.1.0/24");

        Assert.Equal("192.168.1.255", Assert.Single(addresses).ToString());
    }

    [Fact]
    public void GetBroadcastAddresses_ShouldResolveWildcardSubnet()
    {
        var addresses = DiscoverySubnetResolver.GetBroadcastAddresses("10.20.30.*");

        Assert.Equal("10.20.30.255", Assert.Single(addresses).ToString());
    }

    [Fact]
    public void TryGetBroadcastAddresses_ShouldRejectInvalidSubnet()
    {
        var ok = DiscoverySubnetResolver.TryGetBroadcastAddresses("192.168.1.0/99", out _);

        Assert.False(ok);
    }
}
