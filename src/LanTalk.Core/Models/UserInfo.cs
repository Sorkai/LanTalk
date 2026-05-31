using LanTalk.Core.Enums;
using LanTalk.Core.Constants;

namespace LanTalk.Core.Models;

public sealed class UserInfo
{
    public string UserId { get; init; } = string.Empty;

    public string Nickname { get; init; } = string.Empty;

    public string Department { get; init; } = NetworkConstants.DefaultDepartment;

    public string IpAddress { get; init; } = string.Empty;

    public int MessagePort { get; init; }

    public int FilePort { get; init; }

    public UserStatus Status { get; init; }

    public DateTimeOffset LastSeenTime { get; set; }
}
