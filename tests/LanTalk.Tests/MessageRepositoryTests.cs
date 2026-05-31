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

    [Fact]
    public async Task MarkSessionIncomingMessagesReadAsync_ShouldReturnUnreadIncomingMessages()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"lantalk-read-{Guid.NewGuid():N}.db");
        var factory = new SqliteConnectionFactory(databasePath);
        var initializer = new DatabaseInitializer(factory);
        var repository = new MessageRepository(factory);

        await initializer.InitializeAsync();
        await repository.SaveAsync(CreateMessage("user-a", "user-a", "未读消息", DateTimeOffset.Now.AddMinutes(-1)));

        var readTime = DateTimeOffset.Now;
        var messages = await repository.MarkSessionIncomingMessagesReadAsync("user-a", readTime);
        var history = await repository.LoadRecentMessagesAsync("user-a");

        Assert.Single(messages);
        Assert.True(history[0].IsRead);
        Assert.NotNull(history[0].ReadTime);

        SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }

    [Fact]
    public async Task MarkMessageReadByAsync_ShouldTrackGroupReadCount()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"lantalk-group-read-{Guid.NewGuid():N}.db");
        var factory = new SqliteConnectionFactory(databasePath);
        var initializer = new DatabaseInitializer(factory);
        var repository = new MessageRepository(factory);

        await initializer.InitializeAsync();
        await repository.SaveAsync(new ChatMessage
        {
            MessageId = "group-message-a",
            SessionId = "group-a",
            SenderId = "local",
            ReceiverId = "group-a",
            Kind = MessageKind.Group,
            Content = "需要大家确认",
            SendTime = DateTimeOffset.Now,
            IsMine = true,
            ReadTargetCount = 2
        });

        var first = await repository.MarkMessageReadByAsync(new MessageReadReceiptPayload(
            "group-message-a",
            "group-a",
            "user-b",
            "李同学",
            IsGroup: true,
            DateTimeOffset.Now));
        var second = await repository.MarkMessageReadByAsync(new MessageReadReceiptPayload(
            "group-message-a",
            "group-a",
            "user-c",
            "王同学",
            IsGroup: true,
            DateTimeOffset.Now));
        var history = await repository.LoadRecentMessagesAsync("group-a");

        Assert.Equal(1, first.ReadByCount);
        Assert.False(first.IsRead);
        Assert.Equal(2, second.ReadByCount);
        Assert.True(second.IsRead);
        Assert.Equal(2, history[0].ReadByCount);
        Assert.True(history[0].IsRead);

        SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }

    [Fact]
    public async Task RecallMessageAsync_ShouldMarkMessageRecalled()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"lantalk-recall-{Guid.NewGuid():N}.db");
        var factory = new SqliteConnectionFactory(databasePath);
        var initializer = new DatabaseInitializer(factory);
        var repository = new MessageRepository(factory);

        await initializer.InitializeAsync();
        await repository.SaveAsync(CreateMessage("user-a", "local", "需要撤回", DateTimeOffset.Now, "message-a"));

        await repository.RecallMessageAsync("user-a", "message-a", DateTimeOffset.Now);
        var history = await repository.LoadRecentMessagesAsync("user-a");

        Assert.Single(history);
        Assert.True(history[0].IsRecalled);
        Assert.NotNull(history[0].RecalledTime);

        SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }

    [Fact]
    public async Task LoadMessagesForExportAsync_ShouldReturnChronologicalMessages()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"lantalk-export-{Guid.NewGuid():N}.db");
        var factory = new SqliteConnectionFactory(databasePath);
        var initializer = new DatabaseInitializer(factory);
        var repository = new MessageRepository(factory);
        var firstTime = DateTimeOffset.Parse("2026-06-01T09:00:00+08:00");
        var secondTime = firstTime.AddMinutes(5);

        await initializer.InitializeAsync();
        await repository.SaveAsync(CreateMessage("session-export", "user-a", "第一条", firstTime, "message-1"));
        await repository.SaveAsync(CreateMessage("session-export", "user-b", "第二条", secondTime, "message-2"));

        var results = await repository.LoadMessagesForExportAsync(
            "session-export",
            firstTime.AddMinutes(-1),
            secondTime.AddMinutes(1));

        Assert.Equal(2, results.Count);
        Assert.Equal("message-1", results[0].MessageId);
        Assert.Equal("message-2", results[1].MessageId);

        SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }

    private static ChatMessage CreateMessage(string sessionId, string senderId, string content, DateTimeOffset sendTime, string? messageId = null)
    {
        return new ChatMessage
        {
            MessageId = messageId ?? Guid.NewGuid().ToString("N"),
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
