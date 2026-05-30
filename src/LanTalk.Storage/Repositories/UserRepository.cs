using LanTalk.Core.Enums;
using LanTalk.Core.Models;
using LanTalk.Storage.Database;

namespace LanTalk.Storage.Repositories;

public sealed class UserRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public UserRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task SaveAsync(UserInfo user, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT OR REPLACE INTO KnownUsers
                (UserId, Nickname, IpAddress, MessagePort, FilePort, Status, LastSeenTime)
            VALUES
                ($userId, $nickname, $ipAddress, $messagePort, $filePort, $status, $lastSeenTime);
            """;
        command.Parameters.AddWithValue("$userId", user.UserId);
        command.Parameters.AddWithValue("$nickname", user.Nickname);
        command.Parameters.AddWithValue("$ipAddress", user.IpAddress);
        command.Parameters.AddWithValue("$messagePort", user.MessagePort);
        command.Parameters.AddWithValue("$filePort", user.FilePort);
        command.Parameters.AddWithValue("$status", user.Status.ToString());
        command.Parameters.AddWithValue("$lastSeenTime", user.LastSeenTime.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveManyAsync(IEnumerable<UserInfo> users, CancellationToken cancellationToken = default)
    {
        foreach (var user in users)
        {
            await SaveAsync(user, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<UserInfo>> LoadRecentAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT UserId, Nickname, IpAddress, MessagePort, FilePort, Status, LastSeenTime
            FROM KnownUsers
            ORDER BY LastSeenTime DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", limit);

        var users = new List<UserInfo>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            users.Add(new UserInfo
            {
                UserId = reader.GetString(0),
                Nickname = reader.GetString(1),
                IpAddress = reader.GetString(2),
                MessagePort = reader.GetInt32(3),
                FilePort = reader.GetInt32(4),
                Status = Enum.Parse<UserStatus>(reader.GetString(5)),
                LastSeenTime = DateTimeOffset.Parse(reader.GetString(6))
            });
        }

        return users;
    }
}
