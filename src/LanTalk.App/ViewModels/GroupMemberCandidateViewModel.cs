using CommunityToolkit.Mvvm.ComponentModel;
using LanTalk.Core.Enums;

namespace LanTalk.App.ViewModels;

public sealed partial class GroupMemberCandidateViewModel : ViewModelBase
{
    [ObservableProperty]
    private string userId = string.Empty;

    [ObservableProperty]
    private string nickname = string.Empty;

    [ObservableProperty]
    private string department = string.Empty;

    [ObservableProperty]
    private UserStatus status;

    [ObservableProperty]
    private bool isSelected;

    public string Summary => $"{Department} · {StatusText}";

    public string StatusText => Status == UserStatus.Online ? "在线" : "离线";

    partial void OnDepartmentChanged(string value)
    {
        OnPropertyChanged(nameof(Summary));
    }

    partial void OnStatusChanged(UserStatus value)
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(Summary));
    }
}
