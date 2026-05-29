using CommunityToolkit.Mvvm.ComponentModel;
using LanTalk.Core.Enums;

namespace LanTalk.App.ViewModels;

public sealed partial class ChatMessageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string senderName = string.Empty;

    [ObservableProperty]
    private string content = string.Empty;

    [ObservableProperty]
    private string timeText = string.Empty;

    [ObservableProperty]
    private bool isMine;

    [ObservableProperty]
    private MessageKind kind;

    [ObservableProperty]
    private string fileName = string.Empty;

    [ObservableProperty]
    private string fileSizeText = string.Empty;

    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private string statusText = string.Empty;

    public bool IsBroadcast => Kind == MessageKind.Broadcast;

    public bool IsFile => Kind == MessageKind.File;

    public bool IsText => Kind is MessageKind.Private or MessageKind.Broadcast or MessageKind.System;
}
