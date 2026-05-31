using LanTalk.Core.Enums;
using LanTalk.Core.Models;
using LanTalk.Storage.Database;
using LanTalk.Storage.Repositories;
using Microsoft.Data.Sqlite;

namespace LanTalk.Tests;

public sealed class GroupRepositoryTests
{
    [Fact]
    public async Task SaveAsync_ShouldPersistPermanentGroups()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"lantalk-{Guid.NewGuid():N}.db");
        var factory = new SqliteConnectionFactory(databasePath);
        var initializer = new DatabaseInitializer(factory);
        var repository = new GroupRepository(factory);
        var group = new ChatGroup
        {
            GroupId = "group-a",
            Name = "项目组",
            Kind = GroupKind.Permanent,
            MemberUserIds = ["user-a", "user-b"],
            CreatedTime = DateTimeOffset.Now.AddMinutes(-2),
            UpdatedTime = DateTimeOffset.Now
        };

        await initializer.InitializeAsync();
        await repository.SaveAsync(group);
        var groups = await repository.LoadAllAsync();

        Assert.Single(groups);
        Assert.Equal("项目组", groups[0].Name);
        Assert.Equal(GroupKind.Permanent, groups[0].Kind);
        Assert.Contains("user-b", groups[0].MemberUserIds);

        SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }

    [Fact]
    public async Task SaveAsync_ShouldIgnoreTemporaryGroups()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"lantalk-{Guid.NewGuid():N}.db");
        var factory = new SqliteConnectionFactory(databasePath);
        var initializer = new DatabaseInitializer(factory);
        var repository = new GroupRepository(factory);

        await initializer.InitializeAsync();
        await repository.SaveAsync(new ChatGroup
        {
            GroupId = "group-temp",
            Name = "临时组",
            Kind = GroupKind.Temporary,
            MemberUserIds = ["user-a", "user-b"]
        });

        var groups = await repository.LoadAllAsync();

        Assert.Empty(groups);

        SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemovePermanentGroup()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"lantalk-{Guid.NewGuid():N}.db");
        var factory = new SqliteConnectionFactory(databasePath);
        var initializer = new DatabaseInitializer(factory);
        var repository = new GroupRepository(factory);

        await initializer.InitializeAsync();
        await repository.SaveAsync(new ChatGroup
        {
            GroupId = "group-a",
            Name = "项目组",
            Kind = GroupKind.Permanent,
            MemberUserIds = ["user-a", "user-b"]
        });
        await repository.DeleteAsync("group-a");

        var groups = await repository.LoadAllAsync();

        Assert.Empty(groups);

        SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }
}
