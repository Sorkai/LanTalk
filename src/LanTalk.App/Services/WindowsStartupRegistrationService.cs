using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace LanTalk.App.Services;

public sealed class WindowsStartupRegistrationService : IStartupRegistrationService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string EntryName = "LanTalk";

    public bool IsSupported => OperatingSystem.IsWindows();

    public bool IsEnabled()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        return IsEnabledOnWindows();
    }

    public void SetEnabled(bool enabled)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("当前平台不支持 Windows 开机自启。");
        }

        SetEnabledOnWindows(enabled);
    }

    [SupportedOSPlatform("windows")]
    private static bool IsEnabledOnWindows()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        var value = key?.GetValue(EntryName) as string;
        return string.Equals(value, BuildCommandLine(), StringComparison.Ordinal);
    }

    [SupportedOSPlatform("windows")]
    private static void SetEnabledOnWindows(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

        if (enabled)
        {
            key.SetValue(EntryName, BuildCommandLine(), RegistryValueKind.String);
            return;
        }

        key.DeleteValue(EntryName, throwOnMissingValue: false);
    }

    private static string BuildCommandLine()
    {
        var processPath = Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule?.FileName
            ?? throw new InvalidOperationException("无法确定当前进程路径。");
        var entryAssemblyPath = Environment.GetCommandLineArgs()
            .Skip(1)
            .FirstOrDefault(argument =>
                argument.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                File.Exists(argument));

        if (processPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(entryAssemblyPath) &&
            entryAssemblyPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            return $"\"{processPath}\" \"{entryAssemblyPath}\"";
        }

        return $"\"{processPath}\"";
    }
}
