using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using LanTalk.Core.Models;

namespace LanTalk.App.Services;

public static class AppThemeService
{
    private static AppSettings? lastSettings;
    private static bool isWatchingActualTheme;

    private static readonly ThemePalette LightPalette = new(
        Sidebar: "#F5F8FC",
        Surface: "#FFFFFF",
        Chat: "#EEF4FB",
        Text: "#1F2937",
        MutedText: "#6B7280",
        Border: "#D9E2EF",
        Avatar: "#E1F0FF",
        Broadcast: "#E8F2FF",
        FileCard: "#F9FAFB");

    private static readonly ThemePalette DarkPalette = new(
        Sidebar: "#111827",
        Surface: "#172033",
        Chat: "#0B1220",
        Text: "#E5E7EB",
        MutedText: "#9CA3AF",
        Border: "#334155",
        Avatar: "#1F3A5F",
        Broadcast: "#162B45",
        FileCard: "#1F2937");

    public static void Apply(AppSettings settings)
    {
        if (Application.Current is not { } application)
        {
            return;
        }

        lastSettings = settings;
        WatchActualTheme(application);

        if (Dispatcher.UIThread.CheckAccess())
        {
            ApplyOnUiThread(application, settings);
            return;
        }

        Dispatcher.UIThread.Post(() => ApplyOnUiThread(application, settings));
    }

    private static void ApplyOnUiThread(Application application, AppSettings settings)
    {
        var requestedTheme = ResolveThemeVariant(settings.ThemeMode);
        application.RequestedThemeVariant = requestedTheme;

        var useDarkPalette = requestedTheme == ThemeVariant.Dark ||
            requestedTheme == ThemeVariant.Default && application.ActualThemeVariant == ThemeVariant.Dark;
        var palette = useDarkPalette ? DarkPalette : LightPalette;
        var accent = ResolveAccent(settings.ThemeColor);

        SetBrush(application, "LanTalkBlueBrush", accent.Primary);
        SetBrush(application, "LanTalkBlueDarkBrush", accent.Strong);
        SetBrush(application, "LanTalkSidebarBrush", palette.Sidebar);
        SetBrush(application, "LanTalkSurfaceBrush", palette.Surface);
        SetBrush(application, "LanTalkChatBrush", palette.Chat);
        SetBrush(application, "LanTalkTextBrush", palette.Text);
        SetBrush(application, "LanTalkMutedTextBrush", palette.MutedText);
        SetBrush(application, "LanTalkBorderBrush", palette.Border);
        SetBrush(application, "LanTalkAvatarBrush", palette.Avatar);
        SetBrush(application, "LanTalkBroadcastBrush", palette.Broadcast);
        SetBrush(application, "LanTalkFileCardBrush", palette.FileCard);
    }

    private static ThemeVariant ResolveThemeVariant(string? themeMode)
    {
        var normalized = themeMode?.Trim();
        if (string.Equals(normalized, "Dark", StringComparison.OrdinalIgnoreCase))
        {
            return ThemeVariant.Dark;
        }

        return string.Equals(normalized, "Light", StringComparison.OrdinalIgnoreCase)
            ? ThemeVariant.Light
            : ThemeVariant.Default;
    }

    private static AccentPalette ResolveAccent(string? themeColor)
    {
        var normalized = themeColor?.Trim();
        if (string.Equals(normalized, "Green", StringComparison.OrdinalIgnoreCase))
        {
            return new AccentPalette("#22C55E", "#15803D");
        }

        return string.Equals(normalized, "Purple", StringComparison.OrdinalIgnoreCase)
            ? new AccentPalette("#8B5CF6", "#6D28D9")
            : new AccentPalette("#2A8CFF", "#1669C9");
    }

    private static void SetBrush(Application application, string key, string colorText)
    {
        var theme = application.ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Dark : ThemeVariant.Light;
        if (application.TryGetResource(key, theme, out var resource) && resource is SolidColorBrush brush)
        {
            brush.Color = Color.Parse(colorText);
        }
    }

    private static void WatchActualTheme(Application application)
    {
        if (isWatchingActualTheme)
        {
            return;
        }

        application.ActualThemeVariantChanged += (_, _) =>
        {
            if (lastSettings is null || !IsSystemTheme(lastSettings.ThemeMode))
            {
                return;
            }

            if (Dispatcher.UIThread.CheckAccess())
            {
                ApplyOnUiThread(application, lastSettings);
                return;
            }

            Dispatcher.UIThread.Post(() => ApplyOnUiThread(application, lastSettings));
        };
        isWatchingActualTheme = true;
    }

    private static bool IsSystemTheme(string? themeMode)
    {
        return string.IsNullOrWhiteSpace(themeMode) ||
            string.Equals(themeMode.Trim(), "System", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record AccentPalette(string Primary, string Strong);

    private sealed record ThemePalette(
        string Sidebar,
        string Surface,
        string Chat,
        string Text,
        string MutedText,
        string Border,
        string Avatar,
        string Broadcast,
        string FileCard);
}
