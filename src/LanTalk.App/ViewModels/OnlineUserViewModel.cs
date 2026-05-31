using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media;
using LanTalk.App.Services;
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

    [ObservableProperty]
    private bool isGroupSession;

    [ObservableProperty]
    private GroupKind groupKind = GroupKind.Temporary;

    public List<string> GroupMemberIds { get; } = [];

    public string Initial => string.IsNullOrWhiteSpace(Nickname)
        ? "?"
        : Nickname[..1].ToUpperInvariant();

    public string AvatarText => AvatarService.GetInitial(Nickname);

    public IBrush AvatarBrush => AvatarService.CreateBrush($"{UserId}:{Nickname}");

    public string StatusText => Status switch
    {
        _ when IsGroupSession && GroupKind == GroupKind.Permanent => "永久群组",
        _ when IsGroupSession => "临时群组",
        UserStatus.Online => "在线",
        UserStatus.Away => "暂离",
        _ => "离线"
    };

    public string GroupKindText => GroupKind == GroupKind.Permanent ? "永久群组" : "临时群组";

    public int GroupMemberCount => GroupMemberIds.Count;

    public string DepartmentText => string.IsNullOrWhiteSpace(Department)
        ? NetworkConstants.DefaultDepartment
        : Department.Trim();

    partial void OnNicknameChanged(string value)
    {
        OnPropertyChanged(nameof(Initial));
        OnPropertyChanged(nameof(AvatarText));
        OnPropertyChanged(nameof(AvatarBrush));
    }

    partial void OnUserIdChanged(string value)
    {
        OnPropertyChanged(nameof(AvatarBrush));
    }

    partial void OnDepartmentChanged(string value)
    {
        OnPropertyChanged(nameof(DepartmentText));
    }

    partial void OnStatusChanged(UserStatus value)
    {
        OnPropertyChanged(nameof(StatusText));
    }

    partial void OnIsGroupSessionChanged(bool value)
    {
        OnPropertyChanged(nameof(StatusText));
    }

    partial void OnGroupKindChanged(GroupKind value)
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(GroupKindText));
    }

    public void RefreshGroupMetadata()
    {
        OnPropertyChanged(nameof(GroupMemberCount));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(GroupKindText));
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
