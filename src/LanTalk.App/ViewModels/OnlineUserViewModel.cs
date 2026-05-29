using CommunityToolkit.Mvvm.ComponentModel;
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

    public string Initial => string.IsNullOrWhiteSpace(Nickname)
        ? "?"
        : Nickname[..1].ToUpperInvariant();

    public string StatusText => Status switch
    {
        UserStatus.Online => "在线",
        UserStatus.Away => "暂离",
        _ => "离线"
    };

    public static OnlineUserViewModel FromUser(UserInfo user)
    {
        return new OnlineUserViewModel
        {
            UserId = user.UserId,
            Nickname = user.Nickname,
            IpAddress = user.IpAddress,
            MessagePort = user.MessagePort,
            FilePort = user.FilePort,
            Status = user.Status,
            LastMessage = user.Status == UserStatus.Online ? "可以开始聊天" : "等待重新上线"
        };
    }
}
