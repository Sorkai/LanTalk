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
            TransferKind = FileTransferKind.Folder,
            BatchId = "batch-1",
            RelativePath = "docs\\demo.txt",
            BytesTransferred = 64,
            Status = FileTransferStatus.Completed,
            TransferTime = DateTimeOffset.Now
        });

        var records = await repository.LoadRecentAsync();
        Assert.Single(records);
        Assert.Equal("demo.txt", records[0].FileName);
        Assert.Equal(FileTransferStatus.Completed, records[0].Status);
        Assert.Equal(FileTransferKind.Folder, records[0].TransferKind);
        Assert.Equal("batch-1", records[0].BatchId);
        Assert.Equal("docs\\demo.txt", records[0].RelativePath);
        Assert.Equal(64, records[0].BytesTransferred);

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }

    [Fact]
    public async Task LoadForExportAsync_ShouldRespectTimeRange()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"lantalk-transfer-export-{Guid.NewGuid():N}.db");
        var factory = new SqliteConnectionFactory(databasePath);
        var initializer = new DatabaseInitializer(factory);
        await initializer.InitializeAsync();

        var repository = new FileTransferRepository(factory);
        var firstTime = DateTimeOffset.Parse("2026-06-01T09:00:00+08:00");
        var secondTime = firstTime.AddMinutes(30);
        await repository.SaveAsync(new FileTransferRecord
        {
            FileId = "file-export-1",
            SenderId = "sender",
            ReceiverId = "receiver",
            FileName = "first.txt",
            FileSize = 64,
            Status = FileTransferStatus.Pending,
            TransferTime = firstTime
        });
        await repository.SaveAsync(new FileTransferRecord
        {
            FileId = "file-export-2",
            SenderId = "sender",
            ReceiverId = "receiver",
            FileName = "second.txt",
            FileSize = 128,
            Status = FileTransferStatus.Completed,
            TransferTime = secondTime
        });

        var records = await repository.LoadForExportAsync(firstTime.AddMinutes(-1), firstTime.AddMinutes(1));

        Assert.Single(records);
        Assert.Equal("file-export-1", records[0].FileId);

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }
}
