using LanTalk.Storage.Database;
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
}
