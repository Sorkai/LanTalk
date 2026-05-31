namespace LanTalk.App.ViewModels;

public sealed class FileReceiveRequestViewModel
{
    public string FileId { get; init; } = string.Empty;

    public string SenderId { get; init; } = string.Empty;

    public string SenderName { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string FileSizeText { get; init; } = string.Empty;

    public bool IsImage { get; init; }

    public string Title => IsImage ? "接收图片" : "接收文件";

    public string Description => IsImage
        ? "图片会保存到设置中的文件接收目录，接收完成后可在聊天中预览。"
        : "文件会保存到设置中的文件接收目录。";
}
