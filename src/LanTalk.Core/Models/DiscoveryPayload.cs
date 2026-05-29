namespace LanTalk.Core.Models;

public sealed record DiscoveryPayload(
    string UserId,
    string Nickname,
    int MessagePort,
    int FilePort);

