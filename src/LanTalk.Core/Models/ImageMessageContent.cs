namespace LanTalk.Core.Models;

public sealed record ImageMessageContent(
    string FileId,
    string FileName,
    long FileSize,
    string? LocalPath);
