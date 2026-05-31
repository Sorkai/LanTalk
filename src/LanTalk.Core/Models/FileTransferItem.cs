namespace LanTalk.Core.Models;

public sealed record FileTransferItem(
    string FileId,
    string FileName,
    string RelativePath,
    long FileSize,
    bool IsDirectory = false);
