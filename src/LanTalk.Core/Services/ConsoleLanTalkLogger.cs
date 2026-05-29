namespace LanTalk.Core.Services;

public sealed class ConsoleLanTalkLogger : ILanTalkLogger
{
    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Warning(string message)
    {
        Write("WARN", message);
    }

    public void Error(string message, Exception? exception = null)
    {
        Write("ERROR", exception is null ? message : $"{message} {exception.Message}");
    }

    private static void Write(string level, string message)
    {
        Console.WriteLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");
    }
}

