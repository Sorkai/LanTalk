using LanTalk.Core.Services;
using LanTalk.Storage.Settings;

namespace LanTalk.Tests;

public sealed class SettingsServiceTests
{
    [Fact]
    public async Task LoadAsync_ShouldCreateStableSettingsFile()
    {
        var settingsPath = CreateTempSettingsPath();
        var service = new SettingsService(new ConsoleLanTalkLogger(), settingsPath);

        var first = await service.LoadAsync();
        var second = await service.LoadAsync();

        Assert.False(string.IsNullOrWhiteSpace(first.UserId));
        Assert.Equal(first.UserId, second.UserId);
        Assert.False(string.IsNullOrWhiteSpace(second.Nickname));
    }

    [Fact]
    public async Task SaveAsync_ShouldPersistEditableSettings()
    {
        var settingsPath = CreateTempSettingsPath();
        var service = new SettingsService(new ConsoleLanTalkLogger(), settingsPath);
        var settings = await service.LoadAsync();

        settings.Nickname = "测试用户";
        settings.FileSavePath = Path.Combine(Path.GetTempPath(), "LanTalkFiles");
        settings.SaveChatHistory = false;
        settings.ThemeMode = "Dark";
        settings.ThemeColor = "Blue";
        settings.UdpPort = 50010;
        settings.MessagePort = 50011;
        settings.FilePort = 50012;

        await service.SaveAsync(settings);
        var loaded = await service.LoadAsync();

        Assert.Equal(settings.UserId, loaded.UserId);
        Assert.Equal("测试用户", loaded.Nickname);
        Assert.Equal(settings.FileSavePath, loaded.FileSavePath);
        Assert.False(loaded.SaveChatHistory);
        Assert.Equal("Dark", loaded.ThemeMode);
        Assert.Equal(50010, loaded.UdpPort);
        Assert.Equal(50011, loaded.MessagePort);
        Assert.Equal(50012, loaded.FilePort);
    }

    [Fact]
    public async Task SaveAsync_ShouldNormalizeUnsupportedThemeSettings()
    {
        var settingsPath = CreateTempSettingsPath();
        var service = new SettingsService(new ConsoleLanTalkLogger(), settingsPath);
        var settings = await service.LoadAsync();

        settings.ThemeMode = "Avalonia.Controls.ComboBoxItem";
        settings.ThemeColor = "Orange";

        await service.SaveAsync(settings);
        var loaded = await service.LoadAsync();

        Assert.Equal("System", loaded.ThemeMode);
        Assert.Equal("Blue", loaded.ThemeColor);
    }

    [Fact]
    public async Task LoadAsync_ShouldBackupCorruptSettingsAndCreateDefault()
    {
        var settingsPath = CreateTempSettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        await File.WriteAllTextAsync(settingsPath, "{ broken json");

        var service = new SettingsService(new ConsoleLanTalkLogger(), settingsPath);
        var settings = await service.LoadAsync();
        var backupExists = Directory.GetFiles(Path.GetDirectoryName(settingsPath)!, "settings.json.*.bak").Length == 1;

        Assert.False(string.IsNullOrWhiteSpace(settings.UserId));
        Assert.True(backupExists);
    }

    private static string CreateTempSettingsPath()
    {
        return Path.Combine(Path.GetTempPath(), $"lantalk-settings-{Guid.NewGuid():N}", "settings.json");
    }
}
