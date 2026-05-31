using LanTalk.Core.Enums;

namespace LanTalk.App.ViewModels;

public sealed class FileReceiveRequestViewModel
{
    public string FileId { get; init; } = string.Empty;

    public string SenderId { get; init; } = string.Empty;

    public string SenderName { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string FileSizeText { get; init; } = string.Empty;

    public bool IsImage { get; init; }

    public FileTransferKind TransferKind { get; init; } = FileTransferKind.SingleFile;

    public int ItemCount { get; init; } = 1;

    public bool IsBatchTransfer => TransferKind is FileTransferKind.MultipleFiles or FileTransferKind.Folder;

    public string GroupId { get; init; } = string.Empty;

    public string GroupName { get; init; } = string.Empty;

    public bool IsGroupTransfer => !string.IsNullOrWhiteSpace(GroupId);

    public string Title => TransferKind switch
    {
        FileTransferKind.Folder => "接收文件夹",
        FileTransferKind.MultipleFiles => "接收多文件",
        _ => IsImage ? "接收图片" : "接收文件"
    };

    public string Description
    {
        get
        {
            if (TransferKind == FileTransferKind.Folder)
            {
                return IsGroupTransfer
                    ? $"来自群组“{GroupName}”，将保留文件夹内的相对目录结构，并保存到设置中的文件接收目录。"
                    : "将保留文件夹内的相对目录结构，并保存到设置中的文件接收目录。";
            }

            if (TransferKind == FileTransferKind.MultipleFiles)
            {
                return IsGroupTransfer
                    ? $"来自群组“{GroupName}”，共 {ItemCount} 个文件，会保存到设置中的文件接收目录。"
                    : $"共 {ItemCount} 个文件，会保存到设置中的文件接收目录。";
            }

            return IsImage
                ? IsGroupTransfer
                    ? $"来自群组“{GroupName}”，图片会保存到设置中的文件接收目录，接收完成后可在群聊中预览。"
                    : "图片会保存到设置中的文件接收目录，接收完成后可在聊天中预览。"
                : IsGroupTransfer
                    ? $"来自群组“{GroupName}”，文件会保存到设置中的文件接收目录。"
                    : "文件会保存到设置中的文件接收目录。";
        }
    }
}
