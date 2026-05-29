namespace LanTalk.Core.Models;

public sealed record BroadcastSendResult(int SuccessCount, int FailureCount)
{
    public int TotalCount => SuccessCount + FailureCount;
}

