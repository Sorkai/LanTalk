using System.Text.Json;
using LanTalk.Core.Constants;
using LanTalk.Core.Models;
using LanTalk.Core.Serialization;
using LanTalk.Core.Services;

namespace LanTalk.Storage.Settings;

public sealed class SettingsService
{
    private readonly ILanTalkLogger _logger;

    public SettingsService(ILanTalkLogger logger, string? settingsPath = null)
    {
        _logger = logger;
        SettingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            NetworkConstants.ApplicationFolderName,
            NetworkConstants.SettingsFileName);
    }

    public string SettingsPath { get; }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);

        if (!File.Exists(SettingsPath))
        {
            var created = CreateDefaultSettings();
            await SaveAsync(created, cancellationToken).ConfigureAwait(false);
            _logger.Info("已创建默认本机配置。");
            return created;
        }

        try
        {
            await using var stream = File.OpenRead(SettingsPath);
            var settings = await JsonSerializer.DeserializeAsync(stream, LanTalkJsonContext.Default.AppSettings, cancellationToken).ConfigureAwait(false);

            if (settings is null)
            {
                throw new JsonException("配置文件为空。");
            }

            EnsureRequiredValues(settings);
            return settings;
        }
        catch (JsonException ex)
        {
            var backupPath = $"{SettingsPath}.{DateTimeOffset.Now:yyyyMMddHHmmss}.bak";
            File.Move(SettingsPath, backupPath, overwrite: true);
            _logger.Error($"配置文件损坏，已备份到 {backupPath}。", ex);

            var created = CreateDefaultSettings();
            await SaveAsync(created, cancellationToken).ConfigureAwait(false);
            return created;
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        EnsureRequiredValues(settings);

        await using var stream = File.Create(SettingsPath);
        await JsonSerializer.SerializeAsync(stream, settings, LanTalkJsonContext.Default.AppSettings, cancellationToken).ConfigureAwait(false);
    }

    private static AppSettings CreateDefaultSettings()
    {
        var nickname = $"{Environment.UserName}";
        var receivePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            NetworkConstants.ApplicationFolderName);

        return new AppSettings
        {
            UserId = Guid.NewGuid().ToString("N"),
            Nickname = string.IsNullOrWhiteSpace(nickname) ? "LanTalk 用户" : nickname,
            FileSavePath = receivePath
        };
    }

    private static void EnsureRequiredValues(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.UserId))
        {
            settings.UserId = Guid.NewGuid().ToString("N");
        }

        if (string.IsNullOrWhiteSpace(settings.Nickname))
        {
            settings.Nickname = "LanTalk 用户";
        }

        if (string.IsNullOrWhiteSpace(settings.FileSavePath))
        {
            settings.FileSavePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                NetworkConstants.ApplicationFolderName);
        }
    }
}
