namespace LanTalk.Core.Models;

public sealed record FileTransferResponse(string FileId, bool Accepted, string? Reason = null);

