namespace LanTalk.Core.Models;

public sealed record FileTransferResponse(
    string FileId,
    bool Accepted,
    string? Reason = null,
    long ResumeOffset = 0,
    IReadOnlyList<FileTransferResumeItem>? ResumeItems = null);
