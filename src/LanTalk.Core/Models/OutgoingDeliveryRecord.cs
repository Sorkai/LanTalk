using LanTalk.Core.Enums;

namespace LanTalk.Core.Models;

public sealed class OutgoingDeliveryRecord
{
    public string DeliveryId { get; init; } = Guid.NewGuid().ToString("N");

    public string RecipientId { get; init; } = string.Empty;

    public PacketType PacketType { get; init; }

    public string PayloadJson { get; init; } = string.Empty;

    public string? SourcePath { get; init; }

    public bool RequiresEncryption { get; init; }

    public DateTimeOffset CreatedTime { get; init; } = DateTimeOffset.Now;

    public DateTimeOffset? LastAttemptTime { get; init; }

    public int AttemptCount { get; init; }

    public string? LastError { get; init; }
}
