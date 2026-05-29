using LanTalk.Core.Enums;
using LanTalk.Core.Models;
using LanTalk.Storage.Database;
using LanTalk.Storage.Repositories;

namespace LanTalk.Tests;

public sealed class FileTransferRepositoryTests
{
    [Fact]
    public async Task SaveAsync_ShouldPersistFileTransferRecord()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"lantalk-transfer-{Guid.NewGuid():N}.db");
        var factory = new SqliteConnectionFactory(databasePath);
        var initializer = new DatabaseInitializer(factory);
        await initializer.InitializeAsync();

        var repository = new FileTransferRepository(factory);
        await repository.SaveAsync(new FileTransferRecord
        {
            FileId = "file-1",
            SenderId = "sender",
            ReceiverId = "receiver",
            FileName = "demo.txt",
            FileSize = 128,
            SavePath = "C:\\Temp\\demo.txt",
            Status = FileTransferStatus.Completed,
            TransferTime = DateTimeOffset.Now
        });

        var records = await repository.LoadRecentAsync();
        Assert.Single(records);
        Assert.Equal("demo.txt", records[0].FileName);
        Assert.Equal(FileTransferStatus.Completed, records[0].Status);

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }
}

