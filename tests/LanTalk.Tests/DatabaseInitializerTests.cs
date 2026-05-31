using LanTalk.Storage.Database;
using LanTalk.Storage.Repositories;
using Microsoft.Data.Sqlite;

namespace LanTalk.Tests;

public sealed class DatabaseInitializerTests
{
    [Fact]
    public async Task InitializeAsync_ShouldCreateDatabaseFile()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"lantalk-{Guid.NewGuid():N}.db");
        var factory = new SqliteConnectionFactory(databasePath);
        var initializer = new DatabaseInitializer(factory);

        await initializer.InitializeAsync();

        Assert.True(File.Exists(databasePath));
        SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }

    [Fact]
    public async Task InitializeAsync_ShouldMigrateKnownUsersDepartmentColumn()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"lantalk-migrate-{Guid.NewGuid():N}.db");
        var factory = new SqliteConnectionFactory(databasePath);

        await using (var connection = factory.CreateConnection())
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                CREATE TABLE KnownUsers (
                    UserId TEXT PRIMARY KEY,
                    Nickname TEXT NOT NULL,
                    IpAddress TEXT NOT NULL,
                    MessagePort INTEGER NOT NULL,
                    FilePort INTEGER NOT NULL,
                    Status TEXT NOT NULL,
                    LastSeenTime TEXT NOT NULL
                );

                INSERT INTO KnownUsers
                    (UserId, Nickname, IpAddress, MessagePort, FilePort, Status, LastSeenTime)
                VALUES
                    ('user-a', '张同学', '192.168.1.24', 50001, 50002, 'Offline', '2026-05-31T10:00:00.0000000+08:00');
                """;
            await command.ExecuteNonQueryAsync();
        }

        var initializer = new DatabaseInitializer(factory);
        await initializer.InitializeAsync();

        var users = await new UserRepository(factory).LoadRecentAsync();

        Assert.Single(users);
        Assert.Equal("默认部门", users[0].Department);
        SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }
}
