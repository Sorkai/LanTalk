using LanTalk.Core.Enums;

namespace LanTalk.Core.Models;

public sealed class UserInfo
{
    public string UserId { get; init; } = string.Empty;

    public string Nickname { get; init; } = string.Empty;

    public string IpAddress { get; init; } = string.Empty;

    public int MessagePort { get; init; }

    public int FilePort { get; init; }

    public UserStatus Status { get; init; }

    public DateTimeOffset LastSeenTime { get; set; }
}

