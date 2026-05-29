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
}

