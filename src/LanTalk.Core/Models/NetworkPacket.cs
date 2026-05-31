using LanTalk.Core.Enums;

namespace LanTalk.Core.Models;

public sealed class NetworkPacket
{
    public string PacketId { get; init; } = Guid.NewGuid().ToString("N");

    public PacketType Type { get; init; }

    public string FromUserId { get; init; } = string.Empty;

    public string? ToUserId { get; init; }

    public DateTimeOffset Time { get; init; } = DateTimeOffset.Now;

    public bool IsEncrypted { get; init; }

    public string PayloadJson { get; init; } = string.Empty;
}
