namespace LanTalk.App.ViewModels;

public sealed class FileReceiveRequestViewModel
{
    public string FileId { get; init; } = string.Empty;

    public string SenderId { get; init; } = string.Empty;

    public string SenderName { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string FileSizeText { get; init; } = string.Empty;
}

