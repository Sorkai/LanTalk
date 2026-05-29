using CommunityToolkit.Mvvm.ComponentModel;
using LanTalk.Core.Models;

namespace LanTalk.App.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string nickname = string.Empty;

    [ObservableProperty]
    private string fileSavePath = string.Empty;

    [ObservableProperty]
    private bool saveChatHistory = true;

    [ObservableProperty]
    private string themeMode = "System";

    [ObservableProperty]
    private string themeColor = "Blue";

    [ObservableProperty]
    private int udpPort = 50000;

    [ObservableProperty]
    private int messagePort = 50001;

    [ObservableProperty]
    private int filePort = 50002;

    public static SettingsViewModel FromSettings(AppSettings settings)
    {
        return new SettingsViewModel
        {
            Nickname = settings.Nickname,
            FileSavePath = settings.FileSavePath,
            SaveChatHistory = settings.SaveChatHistory,
            ThemeMode = settings.ThemeMode,
            ThemeColor = settings.ThemeColor,
            UdpPort = settings.UdpPort,
            MessagePort = settings.MessagePort,
            FilePort = settings.FilePort
        };
    }
}

