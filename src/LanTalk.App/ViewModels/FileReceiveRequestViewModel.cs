namespace LanTalk.App.ViewModels;

public sealed class FileReceiveRequestViewModel
{
    public string FileId { get; init; } = string.Empty;

    public string SenderId { get; init; } = string.Empty;

    public string SenderName { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string FileSizeText { get; init; } = string.Empty;

    public bool IsImage { get; init; }

    public string GroupId { get; init; } = string.Empty;

    public string GroupName { get; init; } = string.Empty;

    public bool IsGroupTransfer => !string.IsNullOrWhiteSpace(GroupId);

    public string Title => IsImage ? "接收图片" : "接收文件";

    public string Description => IsImage
        ? IsGroupTransfer
            ? $"来自群组“{GroupName}”，图片会保存到设置中的文件接收目录，接收完成后可在群聊中预览。"
            : "图片会保存到设置中的文件接收目录，接收完成后可在聊天中预览。"
        : IsGroupTransfer
            ? $"来自群组“{GroupName}”，文件会保存到设置中的文件接收目录。"
            : "文件会保存到设置中的文件接收目录。";
}
