using CommunityToolkit.Mvvm.ComponentModel;
using LanTalk.Core.Constants;
using LanTalk.Core.Enums;
using LanTalk.Core.Models;

namespace LanTalk.App.ViewModels;

public sealed partial class OnlineUserViewModel : ViewModelBase
{
    [ObservableProperty]
    private string userId = string.Empty;

    [ObservableProperty]
    private string nickname = string.Empty;

    [ObservableProperty]
    private string department = NetworkConstants.DefaultDepartment;

    [ObservableProperty]
    private string ipAddress = string.Empty;

    [ObservableProperty]
    private int messagePort = 50001;

    [ObservableProperty]
    private int filePort = 50002;

    [ObservableProperty]
    private UserStatus status;

    [ObservableProperty]
    private string lastMessage = string.Empty;

    [ObservableProperty]
    private int unreadCount;

    [ObservableProperty]
    private DateTimeOffset lastActiveTime = DateTimeOffset.MinValue;

    [ObservableProperty]
    private bool isSelected;

    public string Initial => string.IsNullOrWhiteSpace(Nickname)
        ? "?"
        : Nickname[..1].ToUpperInvariant();

    public string StatusText => Status switch
    {
        UserStatus.Online => "在线",
        UserStatus.Away => "暂离",
        _ => "离线"
    };

    public string DepartmentText => string.IsNullOrWhiteSpace(Department)
        ? NetworkConstants.DefaultDepartment
        : Department.Trim();

    partial void OnNicknameChanged(string value)
    {
        OnPropertyChanged(nameof(Initial));
    }

    partial void OnDepartmentChanged(string value)
    {
        OnPropertyChanged(nameof(DepartmentText));
    }

    partial void OnStatusChanged(UserStatus value)
    {
        OnPropertyChanged(nameof(StatusText));
    }

    public static OnlineUserViewModel FromUser(UserInfo user)
    {
        return new OnlineUserViewModel
        {
            UserId = user.UserId,
            Nickname = user.Nickname,
            Department = user.Department,
            IpAddress = user.IpAddress,
            MessagePort = user.MessagePort,
            FilePort = user.FilePort,
            Status = user.Status,
            LastActiveTime = user.LastSeenTime,
            LastMessage = user.Status == UserStatus.Online ? "可以开始聊天" : "等待重新上线"
        };
    }
}
