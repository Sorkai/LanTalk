using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media.Imaging;
using LanTalk.Core.Enums;

namespace LanTalk.App.ViewModels;

public sealed partial class ChatMessageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string messageId = string.Empty;

    [ObservableProperty]
    private string sessionId = string.Empty;

    [ObservableProperty]
    private string senderId = string.Empty;

    [ObservableProperty]
    private string? receiverId;

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

    [ObservableProperty]
    private string imagePath = string.Empty;

    [ObservableProperty]
    private bool isRead;

    [ObservableProperty]
    private int readByCount;

    [ObservableProperty]
    private int readTargetCount;

    [ObservableProperty]
    private bool isRecalled;

    [ObservableProperty]
    private Bitmap? imageSource;

    [ObservableProperty]
    private int imagePixelWidth;

    [ObservableProperty]
    private int imagePixelHeight;

    public bool IsBroadcast => Kind == MessageKind.Broadcast;

    public bool IsFile => !IsRecalled && Kind == MessageKind.File;

    public bool IsImage => !IsRecalled && Kind == MessageKind.Image;

    public bool IsText => IsRecalled || Kind is MessageKind.Private or MessageKind.Broadcast or MessageKind.Group or MessageKind.System;

    public bool HasContentText => !string.IsNullOrWhiteSpace(Content);

    public bool CanPreviewImage => ImageSource is not null;

    public bool HasReadStatus => IsMine &&
        !IsRecalled &&
        Kind is MessageKind.Private or MessageKind.Group &&
        ReadTargetCount > 0;

    public string ReadStatusText
    {
        get
        {
            if (!HasReadStatus)
            {
                return string.Empty;
            }

            if (Kind == MessageKind.Group)
            {
                return $"已读 {Math.Min(ReadByCount, ReadTargetCount)}/{ReadTargetCount}";
            }

            return IsRead || ReadByCount > 0 ? "已读" : "未读";
        }
    }

    public bool CanRecall => IsMine &&
        !IsRecalled &&
        !string.IsNullOrWhiteSpace(MessageId) &&
        Kind is MessageKind.Private or MessageKind.Group or MessageKind.Image;

    partial void OnKindChanged(MessageKind value)
    {
        OnPropertyChanged(nameof(IsBroadcast));
        OnPropertyChanged(nameof(IsFile));
        OnPropertyChanged(nameof(IsImage));
        OnPropertyChanged(nameof(IsText));
        OnPropertyChanged(nameof(HasReadStatus));
        OnPropertyChanged(nameof(ReadStatusText));
        OnPropertyChanged(nameof(CanRecall));
    }

    partial void OnContentChanged(string value)
    {
        OnPropertyChanged(nameof(HasContentText));
    }

    partial void OnImageSourceChanged(Bitmap? value)
    {
        OnPropertyChanged(nameof(CanPreviewImage));
    }

    partial void OnIsReadChanged(bool value)
    {
        OnPropertyChanged(nameof(ReadStatusText));
    }

    partial void OnReadByCountChanged(int value)
    {
        OnPropertyChanged(nameof(ReadStatusText));
    }

    partial void OnReadTargetCountChanged(int value)
    {
        OnPropertyChanged(nameof(HasReadStatus));
        OnPropertyChanged(nameof(ReadStatusText));
    }

    partial void OnIsRecalledChanged(bool value)
    {
        OnPropertyChanged(nameof(IsFile));
        OnPropertyChanged(nameof(IsImage));
        OnPropertyChanged(nameof(IsText));
        OnPropertyChanged(nameof(HasReadStatus));
        OnPropertyChanged(nameof(ReadStatusText));
        OnPropertyChanged(nameof(CanRecall));
    }

    public void SetImagePath(string? path)
    {
        ImagePath = path ?? string.Empty;
        ImageSource = null;
        ImagePixelWidth = 0;
        ImagePixelHeight = 0;

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return;
        }

        try
        {
            using var stream = File.OpenRead(path);
            var bitmap = new Bitmap(stream);
            ImagePixelWidth = bitmap.PixelSize.Width;
            ImagePixelHeight = bitmap.PixelSize.Height;
            ImageSource = bitmap;
        }
        catch (Exception)
        {
            ImageSource = null;
            ImagePixelWidth = 0;
            ImagePixelHeight = 0;
        }
    }
}
