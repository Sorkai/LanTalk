namespace LanTalk.Core.Models;

public sealed record ErrorPayload(string Code, string Message, string? FileId = null);
