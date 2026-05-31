namespace LanTalk.Core.Models;

public sealed record FileTransferRequest(
    string FileId,
    string FileName,
    long FileSize,
    string SenderId,
    string ReceiverId,
    int FilePort,
    bool IsImage = false);
