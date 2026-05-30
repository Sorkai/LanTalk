using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LanTalk.Core.Constants;
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

    public ObservableCollection<DiscoverySubnetEntryViewModel> DiscoverySubnets { get; } = [];

    public string GetDiscoverySubnetText()
    {
        var values = DiscoverySubnets
            .Select(item => item.Value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        return values.Length == 0
            ? NetworkConstants.DefaultDiscoverySubnet
            : string.Join(", ", values);
    }

    [RelayCommand]
    private void AddDiscoverySubnet()
    {
        DiscoverySubnets.Add(new DiscoverySubnetEntryViewModel());
    }

    [RelayCommand]
    private void RemoveDiscoverySubnet(DiscoverySubnetEntryViewModel? entry)
    {
        if (entry is null)
        {
            return;
        }

        if (DiscoverySubnets.Count <= 1)
        {
            entry.Value = NetworkConstants.DefaultDiscoverySubnet;
            return;
        }

        DiscoverySubnets.Remove(entry);
    }

    public static SettingsViewModel FromSettings(AppSettings settings)
    {
        var viewModel = new SettingsViewModel
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

        viewModel.LoadDiscoverySubnets(settings.DiscoverySubnet);
        return viewModel;
    }

    private void LoadDiscoverySubnets(string? value)
    {
        DiscoverySubnets.Clear();

        var values = SplitDiscoverySubnetTargets(value);
        if (values.Length == 0)
        {
            DiscoverySubnets.Add(new DiscoverySubnetEntryViewModel { Value = NetworkConstants.DefaultDiscoverySubnet });
            return;
        }

        foreach (var subnet in values)
        {
            DiscoverySubnets.Add(new DiscoverySubnetEntryViewModel { Value = subnet });
        }
    }

    private static string[] SplitDiscoverySubnetTargets(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split([',', ';', '，', '；'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}

public sealed partial class DiscoverySubnetEntryViewModel : ViewModelBase
{
    [ObservableProperty]
    private string value = string.Empty;
}
