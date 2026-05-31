namespace LanTalk.Core.Constants;

public static class NetworkConstants
{
    public const int DefaultUdpPort = 50000;
    public const int DefaultMessagePort = 50001;
    public const int DefaultFilePort = 50002;
    public const string DefaultDiscoverySubnet = "Auto";
    public const string DefaultDepartment = "默认部门";
    public const int HeartbeatIntervalSeconds = 5;
    public const int OfflineTimeoutSeconds = 15;
    public const int RecentMessageLimit = 50;
    public const int FileTransferBufferSize = 64 * 1024;
    public const string BroadcastSessionId = "broadcast";
    public const string ApplicationFolderName = "LanTalk";
    public const string SettingsFileName = "settings.json";
    public const string DatabaseFileName = "lantalk.db";
}
