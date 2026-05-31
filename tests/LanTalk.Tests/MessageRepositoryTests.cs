using LanTalk.Core.Enums;
using LanTalk.Core.Models;
using LanTalk.Storage.Database;
using LanTalk.Storage.Repositories;
using Microsoft.Data.Sqlite;

namespace LanTalk.Tests;

public sealed class MessageRepositoryTests
{
    [Fact]
    public async Task SearchMessagesAsync_ShouldReturnMatchingMessagesInSession()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"lantalk-{Guid.NewGuid():N}.db");
        var factory = new SqliteConnectionFactory(databasePath);
        var initializer = new DatabaseInitializer(factory);
        var repository = new MessageRepository(factory);

        await initializer.InitializeAsync();
        await repository.SaveAsync(CreateMessage("session-a", "user-a", "今天的项目进度已经同步", DateTimeOffset.Now.AddMinutes(-3)));
        await repository.SaveAsync(CreateMessage("session-a", "user-b", "这条消息不应该命中", DateTimeOffset.Now.AddMinutes(-2)));
        await repository.SaveAsync(CreateMessage("session-b", "user-c", "项目进度在另一个会话", DateTimeOffset.Now.AddMinutes(-1)));

        var results = await repository.SearchMessagesAsync("session-a", "项目进度");

        Assert.Single(results);
        Assert.Equal("session-a", results[0].SessionId);
        Assert.Contains("项目进度", results[0].Content);

        SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }

    [Fact]
    public async Task SearchMessagesAsync_ShouldTreatPercentAsLiteralText()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"lantalk-{Guid.NewGuid():N}.db");
        var factory = new SqliteConnectionFactory(databasePath);
        var initializer = new DatabaseInitializer(factory);
        var repository = new MessageRepository(factory);

        await initializer.InitializeAsync();
        await repository.SaveAsync(CreateMessage("session-a", "user-a", "文件已经传到 100%", DateTimeOffset.Now.AddMinutes(-2)));
        await repository.SaveAsync(CreateMessage("session-a", "user-b", "普通进度消息", DateTimeOffset.Now.AddMinutes(-1)));

        var results = await repository.SearchMessagesAsync("session-a", "100%");

        Assert.Single(results);
        Assert.Equal("文件已经传到 100%", results[0].Content);

        SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }

    [Fact]
    public async Task SaveAsync_ShouldPersistImageMessages()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"lantalk-{Guid.NewGuid():N}.db");
        var factory = new SqliteConnectionFactory(databasePath);
        var initializer = new DatabaseInitializer(factory);
        var repository = new MessageRepository(factory);

        await initializer.InitializeAsync();
        await repository.SaveAsync(new ChatMessage
        {
            MessageId = "image-a",
            SessionId = "session-a",
            SenderId = "local",
            ReceiverId = "user-a",
            Kind = MessageKind.Image,
            Content = """{"fileId":"image-a","fileName":"photo.png","fileSize":1024,"localPath":"C:\\Temp\\photo.png"}""",
            SendTime = DateTimeOffset.Now,
            IsMine = true
        });

        var messages = await repository.LoadRecentMessagesAsync("session-a");

        Assert.Single(messages);
        Assert.Equal(MessageKind.Image, messages[0].Kind);
        Assert.Contains("photo.png", messages[0].Content);

        SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }

    [Fact]
    public async Task SaveAsync_ShouldPersistGroupMessages()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"lantalk-{Guid.NewGuid():N}.db");
        var factory = new SqliteConnectionFactory(databasePath);
        var initializer = new DatabaseInitializer(factory);
        var repository = new MessageRepository(factory);

        await initializer.InitializeAsync();
        await repository.SaveAsync(new ChatMessage
        {
            MessageId = "group-message-a",
            SessionId = "group-a",
            SenderId = "user-a",
            ReceiverId = "group-a",
            Kind = MessageKind.Group,
            Content = "多人会话消息",
            SendTime = DateTimeOffset.Now,
            IsMine = false
        });

        var messages = await repository.LoadRecentMessagesAsync("group-a");

        Assert.Single(messages);
        Assert.Equal(MessageKind.Group, messages[0].Kind);
        Assert.Equal("多人会话消息", messages[0].Content);

        SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }

    private static ChatMessage CreateMessage(string sessionId, string senderId, string content, DateTimeOffset sendTime)
    {
        return new ChatMessage
        {
            SessionId = sessionId,
            SenderId = senderId,
            ReceiverId = "local",
            Kind = MessageKind.Private,
            Content = content,
            SendTime = sendTime,
            IsMine = senderId == "local"
        };
    }
}
