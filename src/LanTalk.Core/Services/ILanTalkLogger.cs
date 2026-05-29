namespace LanTalk.Core.Services;

public interface ILanTalkLogger
{
    void Info(string message);

    void Warning(string message);

    void Error(string message, Exception? exception = null);
}

