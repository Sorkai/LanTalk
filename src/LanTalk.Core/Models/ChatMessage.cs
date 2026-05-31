using LanTalk.Core.Enums;

namespace LanTalk.Core.Models;

public sealed class ChatMessage
{
    public string MessageId { get; init; } = Guid.NewGuid().ToString("N");

    public string SessionId { get; init; } = string.Empty;

    public string SenderId { get; init; } = string.Empty;

    public string? ReceiverId { get; init; }

    public MessageKind Kind { get; init; }

    public string Content { get; init; } = string.Empty;

    public DateTimeOffset SendTime { get; init; } = DateTimeOffset.Now;

    public bool IsMine { get; init; }

    public bool IsRead { get; init; }

    public DateTimeOffset? ReadTime { get; init; }

    public int ReadByCount { get; init; }

    public int ReadTargetCount { get; init; }

    public bool IsRecalled { get; init; }

    public DateTimeOffset? RecalledTime { get; init; }
}
