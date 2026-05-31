using System.Text.Json;
using LanTalk.Core.Enums;
using LanTalk.Core.Models;
using LanTalk.Core.Serialization;
using LanTalk.Storage.Database;

namespace LanTalk.Storage.Repositories;

public sealed class GroupRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public GroupRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task SaveAsync(ChatGroup group, CancellationToken cancellationToken = default)
    {
        if (group.Kind != GroupKind.Permanent)
        {
            return;
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT OR REPLACE INTO ChatGroups
                (GroupId, Name, Kind, MemberUserIdsJson, CreatedTime, UpdatedTime)
            VALUES
                ($groupId, $name, $kind, $memberUserIdsJson, $createdTime, $updatedTime);
            """;
        command.Parameters.AddWithValue("$groupId", group.GroupId);
        command.Parameters.AddWithValue("$name", group.Name);
        command.Parameters.AddWithValue("$kind", group.Kind.ToString());
        command.Parameters.AddWithValue("$memberUserIdsJson", JsonSerializer.Serialize(group.MemberUserIds.ToList(), LanTalkJsonContext.Default.ListString));
        command.Parameters.AddWithValue("$createdTime", group.CreatedTime.ToString("O"));
        command.Parameters.AddWithValue("$updatedTime", group.UpdatedTime.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string groupId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM ChatGroups WHERE GroupId = $groupId;";
        command.Parameters.AddWithValue("$groupId", groupId);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ChatGroup>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT GroupId, Name, Kind, MemberUserIdsJson, CreatedTime, UpdatedTime
            FROM ChatGroups
            ORDER BY UpdatedTime DESC;
            """;

        var groups = new List<ChatGroup>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var memberIds = JsonSerializer.Deserialize(reader.GetString(3), LanTalkJsonContext.Default.ListString) ?? [];
            groups.Add(new ChatGroup
            {
                GroupId = reader.GetString(0),
                Name = reader.GetString(1),
                Kind = Enum.Parse<GroupKind>(reader.GetString(2)),
                MemberUserIds = memberIds,
                CreatedTime = DateTimeOffset.Parse(reader.GetString(4)),
                UpdatedTime = DateTimeOffset.Parse(reader.GetString(5))
            });
        }

        return groups;
    }
}
