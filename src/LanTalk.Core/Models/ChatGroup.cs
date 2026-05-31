using LanTalk.Core.Enums;

namespace LanTalk.Core.Models;

public sealed class ChatGroup
{
    public string GroupId { get; init; } = Guid.NewGuid().ToString("N");

    public string Name { get; init; } = string.Empty;

    public GroupKind Kind { get; init; }

    public IReadOnlyList<string> MemberUserIds { get; init; } = [];

    public DateTimeOffset CreatedTime { get; init; } = DateTimeOffset.Now;

    public DateTimeOffset UpdatedTime { get; init; } = DateTimeOffset.Now;
}
