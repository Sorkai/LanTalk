using LanTalk.Core.Enums;
using LanTalk.Core.Models;
using LanTalk.Storage.Database;
using LanTalk.Storage.Repositories;
using Microsoft.Data.Sqlite;

namespace LanTalk.Tests;

public sealed class OutgoingDeliveryRepositoryTests
{
    [Fact]
    public async Task SaveAsync_ShouldPersistPendingDeliveriesForRecipient()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"lantalk-{Guid.NewGuid():N}.db");
        var factory = new SqliteConnectionFactory(databasePath);
        var initializer = new DatabaseInitializer(factory);
        var repository = new OutgoingDeliveryRepository(factory);

        await initializer.InitializeAsync();
        await repository.SaveAsync(new OutgoingDeliveryRecord
        {
            DeliveryId = "delivery-a",
            RecipientId = "user-b",
            PacketType = PacketType.GroupMessage,
            PayloadJson = """{"messageId":"message-a"}""",
            SourcePath = @"C:\Temp\photo.png",
            RequiresEncryption = true,
            CreatedTime = DateTimeOffset.Now
        });

        var records = await repository.LoadForRecipientAsync("user-b");

        Assert.Single(records);
        Assert.Equal("delivery-a", records[0].DeliveryId);
        Assert.Equal(PacketType.GroupMessage, records[0].PacketType);
        Assert.Equal(@"C:\Temp\photo.png", records[0].SourcePath);
        Assert.True(records[0].RequiresEncryption);

        SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }

    [Fact]
    public async Task MarkAttemptAsync_ShouldUpdateRetryMetadata()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"lantalk-{Guid.NewGuid():N}.db");
        var factory = new SqliteConnectionFactory(databasePath);
        var initializer = new DatabaseInitializer(factory);
        var repository = new OutgoingDeliveryRepository(factory);

        await initializer.InitializeAsync();
        await repository.SaveAsync(new OutgoingDeliveryRecord
        {
            DeliveryId = "delivery-a",
            RecipientId = "user-b",
            PacketType = PacketType.GroupMessage,
            PayloadJson = "{}"
        });
        await repository.MarkAttemptAsync("delivery-a", 2, "连接失败");

        var records = await repository.LoadForRecipientAsync("user-b");

        Assert.Single(records);
        Assert.Equal(2, records[0].AttemptCount);
        Assert.Equal("连接失败", records[0].LastError);
        Assert.NotNull(records[0].LastAttemptTime);

        SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveDelivery()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"lantalk-{Guid.NewGuid():N}.db");
        var factory = new SqliteConnectionFactory(databasePath);
        var initializer = new DatabaseInitializer(factory);
        var repository = new OutgoingDeliveryRepository(factory);

        await initializer.InitializeAsync();
        await repository.SaveAsync(new OutgoingDeliveryRecord
        {
            DeliveryId = "delivery-a",
            RecipientId = "user-b",
            PacketType = PacketType.GroupMessage,
            PayloadJson = "{}"
        });
        await repository.DeleteAsync("delivery-a");

        var records = await repository.LoadForRecipientAsync("user-b");

        Assert.Empty(records);

        SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }
}
