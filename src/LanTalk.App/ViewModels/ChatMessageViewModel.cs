using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media.Imaging;
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

    [ObservableProperty]
    private string imagePath = string.Empty;

    [ObservableProperty]
    private Bitmap? imageSource;

    [ObservableProperty]
    private int imagePixelWidth;

    [ObservableProperty]
    private int imagePixelHeight;

    public bool IsBroadcast => Kind == MessageKind.Broadcast;

    public bool IsFile => Kind == MessageKind.File;

    public bool IsImage => Kind == MessageKind.Image;

    public bool IsText => Kind is MessageKind.Private or MessageKind.Broadcast or MessageKind.Group or MessageKind.System;

    public bool HasContentText => !string.IsNullOrWhiteSpace(Content);

    public bool CanPreviewImage => ImageSource is not null;

    partial void OnKindChanged(MessageKind value)
    {
        OnPropertyChanged(nameof(IsBroadcast));
        OnPropertyChanged(nameof(IsFile));
        OnPropertyChanged(nameof(IsImage));
        OnPropertyChanged(nameof(IsText));
    }

    partial void OnContentChanged(string value)
    {
        OnPropertyChanged(nameof(HasContentText));
    }

    partial void OnImageSourceChanged(Bitmap? value)
    {
        OnPropertyChanged(nameof(CanPreviewImage));
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
