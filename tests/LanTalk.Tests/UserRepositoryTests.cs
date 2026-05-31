using LanTalk.Core.Enums;
using LanTalk.Core.Models;
using LanTalk.Storage.Database;
using LanTalk.Storage.Repositories;
using Microsoft.Data.Sqlite;

namespace LanTalk.Tests;

public sealed class UserRepositoryTests
{
    [Fact]
    public async Task SaveAsync_ShouldPersistKnownUser()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"lantalk-users-{Guid.NewGuid():N}.db");
        var factory = new SqliteConnectionFactory(databasePath);
        var initializer = new DatabaseInitializer(factory);
        await initializer.InitializeAsync();

        var repository = new UserRepository(factory);
        await repository.SaveAsync(new UserInfo
        {
            UserId = "user-a",
            Nickname = "张同学",
            Department = "研发部",
            IpAddress = "192.168.1.24",
            MessagePort = 50001,
            FilePort = 50002,
            Status = UserStatus.Online,
            LastSeenTime = DateTimeOffset.Now
        });

        var users = await repository.LoadRecentAsync();
        Assert.Single(users);
        Assert.Equal("张同学", users[0].Nickname);
        Assert.Equal("研发部", users[0].Department);
        Assert.Equal(UserStatus.Online, users[0].Status);

        SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }
}
