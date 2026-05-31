using LanTalk.Core.Constants;

namespace LanTalk.Core.Models;

public sealed class AppSettings
{
    public string UserId { get; set; } = string.Empty;

    public string Nickname { get; set; } = string.Empty;

    public string Department { get; set; } = NetworkConstants.DefaultDepartment;

    public int UdpPort { get; set; } = NetworkConstants.DefaultUdpPort;

    public int MessagePort { get; set; } = NetworkConstants.DefaultMessagePort;

    public int FilePort { get; set; } = NetworkConstants.DefaultFilePort;

    public string DiscoverySubnet { get; set; } = NetworkConstants.DefaultDiscoverySubnet;

    public string FileSavePath { get; set; } = string.Empty;

    public bool SaveChatHistory { get; set; } = true;

    public bool PreferSystemNotifications { get; set; } = true;

    public bool EnableFileCompression { get; set; }

    public bool LaunchOnStartup { get; set; }

    public string ThemeMode { get; set; } = "System";

    public string ThemeColor { get; set; } = "Blue";
}
