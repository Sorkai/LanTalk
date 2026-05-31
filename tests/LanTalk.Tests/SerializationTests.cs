using System.Text.Json;
using LanTalk.Core.Enums;
using LanTalk.Core.Models;
using LanTalk.Core.Serialization;

namespace LanTalk.Tests;

public sealed class SerializationTests
{
    [Fact]
    public void NetworkPacket_ShouldRoundTrip_WithSourceGeneratedContext()
    {
        var packet = new NetworkPacket
        {
            Type = PacketType.Hello,
            FromUserId = "user-a",
            PayloadJson = "{}"
        };

        var json = JsonSerializer.Serialize(packet, LanTalkJsonContext.Default.NetworkPacket);
        var restored = JsonSerializer.Deserialize(json, LanTalkJsonContext.Default.NetworkPacket);

        Assert.NotNull(restored);
        Assert.Equal(PacketType.Hello, restored.Type);
        Assert.Equal("user-a", restored.FromUserId);
    }

    [Fact]
    public void DiscoveryPayload_ShouldRoundTripDepartment()
    {
        var payload = new DiscoveryPayload("user-a", "张同学", 50001, 50002, "研发部");

        var json = JsonSerializer.Serialize(payload, LanTalkJsonContext.Default.DiscoveryPayload);
        var restored = JsonSerializer.Deserialize(json, LanTalkJsonContext.Default.DiscoveryPayload);

        Assert.NotNull(restored);
        Assert.Equal("研发部", restored.Department);
    }

    [Fact]
    public void DiscoveryPayload_ShouldUseDefaultDepartmentForOldPackets()
    {
        const string json = """{"userId":"user-a","nickname":"张同学","messagePort":50001,"filePort":50002}""";

        var restored = JsonSerializer.Deserialize(json, LanTalkJsonContext.Default.DiscoveryPayload);

        Assert.NotNull(restored);
        Assert.Equal("默认部门", restored.Department);
    }
}
