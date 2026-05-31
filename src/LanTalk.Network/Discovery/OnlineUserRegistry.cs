using System.Collections.Concurrent;
using LanTalk.Core.Constants;
using LanTalk.Core.Enums;
using LanTalk.Core.Models;

namespace LanTalk.Network.Discovery;

public sealed class OnlineUserRegistry
{
    private readonly ConcurrentDictionary<string, UserInfo> _users = new();

    public event EventHandler<IReadOnlyCollection<UserInfo>>? UsersChanged;

    public IReadOnlyCollection<UserInfo> Users => _users.Values
        .OrderByDescending(user => user.Status == UserStatus.Online)
        .ThenBy(user => user.Nickname, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public void Upsert(UserInfo user)
    {
        _users.AddOrUpdate(user.UserId, user, (_, existing) => new UserInfo
        {
            UserId = user.UserId,
            Nickname = user.Nickname,
            Department = user.Department,
            IpAddress = user.IpAddress,
            MessagePort = user.MessagePort,
            FilePort = user.FilePort,
            Status = user.Status,
            LastSeenTime = user.LastSeenTime > existing.LastSeenTime ? user.LastSeenTime : existing.LastSeenTime
        });

        RaiseUsersChanged();
    }

    public void MarkOffline(string userId)
    {
        if (!_users.TryGetValue(userId, out var user))
        {
            return;
        }

        _users[userId] = new UserInfo
        {
            UserId = user.UserId,
            Nickname = user.Nickname,
            Department = user.Department,
            IpAddress = user.IpAddress,
            MessagePort = user.MessagePort,
            FilePort = user.FilePort,
            Status = UserStatus.Offline,
            LastSeenTime = user.LastSeenTime
        };

        RaiseUsersChanged();
    }

    public IReadOnlyCollection<UserInfo> MarkStaleUsersOffline(DateTimeOffset now)
    {
        var changed = new List<UserInfo>();
        var timeout = TimeSpan.FromSeconds(NetworkConstants.OfflineTimeoutSeconds);

        foreach (var user in _users.Values)
        {
            if (user.Status != UserStatus.Online || now - user.LastSeenTime <= timeout)
            {
                continue;
            }

            var offline = new UserInfo
            {
                UserId = user.UserId,
                Nickname = user.Nickname,
                Department = user.Department,
                IpAddress = user.IpAddress,
                MessagePort = user.MessagePort,
                FilePort = user.FilePort,
                Status = UserStatus.Offline,
                LastSeenTime = user.LastSeenTime
            };

            _users[user.UserId] = offline;
            changed.Add(offline);
        }

        if (changed.Count > 0)
        {
            RaiseUsersChanged();
        }

        return changed;
    }

    private void RaiseUsersChanged()
    {
        UsersChanged?.Invoke(this, Users);
    }
}
