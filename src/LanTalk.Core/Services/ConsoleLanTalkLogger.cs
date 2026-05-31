using System.Text;
using LanTalk.Core.Constants;

namespace LanTalk.Core.Services;

public sealed class ConsoleLanTalkLogger : ILanTalkLogger, IDisposable
{
    private const string LogFilePrefix = "lantalk";
    private readonly object _syncRoot = new();
    private readonly Func<DateTimeOffset> _clock;
    private readonly long _maxFileSizeBytes;
    private StreamWriter? _writer;
    private string? _currentLogPath;
    private string? _currentDayStamp;

    public ConsoleLanTalkLogger(
        string? logDirectory = null,
        long maxFileSizeBytes = 4 * 1024 * 1024,
        bool enableFileLogging = true,
        Func<DateTimeOffset>? clock = null)
    {
        LogDirectory = logDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            NetworkConstants.ApplicationFolderName,
            "logs");
        _maxFileSizeBytes = maxFileSizeBytes > 0 ? maxFileSizeBytes : 4 * 1024 * 1024;
        EnableFileLogging = enableFileLogging;
        _clock = clock ?? (() => DateTimeOffset.Now);
    }

    public string LogDirectory { get; }

    public bool EnableFileLogging { get; }

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
        var details = exception is null
            ? message
            : $"{message}{Environment.NewLine}{exception}";
        Write("ERROR", details);
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            DisposeWriter();
        }
    }

    private void Write(string level, string message)
    {
        var now = _clock();
        var entry = FormatEntry(now, level, message);

        Console.WriteLine(entry);

        if (!EnableFileLogging)
        {
            return;
        }

        lock (_syncRoot)
        {
            try
            {
                EnsureWriter(now);
                _writer!.WriteLine(entry);
                _writer.Flush();
            }
            catch
            {
                // File logging is best-effort and must not break the app's main flow.
            }
        }
    }

    private void EnsureWriter(DateTimeOffset now)
    {
        Directory.CreateDirectory(LogDirectory);

        var dayStamp = now.ToString("yyyy-MM-dd");
        var logPath = Path.Combine(LogDirectory, $"{LogFilePrefix}-{dayStamp}.log");
        var shouldReopen = !string.Equals(_currentDayStamp, dayStamp, StringComparison.Ordinal) ||
            !string.Equals(_currentLogPath, logPath, StringComparison.OrdinalIgnoreCase) ||
            _writer is null;

        if (shouldReopen)
        {
            DisposeWriter();
            _writer = CreateWriter(logPath);
            _currentDayStamp = dayStamp;
            _currentLogPath = logPath;
        }

        RotateIfNeeded(now);
    }

    private void RotateIfNeeded(DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(_currentLogPath) || _writer is null)
        {
            return;
        }

        var info = new FileInfo(_currentLogPath);
        if (!info.Exists || info.Length < _maxFileSizeBytes)
        {
            return;
        }

        DisposeWriter();

        var archivePath = Path.Combine(
            LogDirectory,
            $"{LogFilePrefix}-{now:yyyy-MM-dd-HHmmss}.log");
        var suffix = 1;
        while (File.Exists(archivePath))
        {
            archivePath = Path.Combine(
                LogDirectory,
                $"{LogFilePrefix}-{now:yyyy-MM-dd-HHmmss}-{suffix}.log");
            suffix++;
        }

        File.Move(_currentLogPath, archivePath);
        _writer = CreateWriter(_currentLogPath);
    }

    private static StreamWriter CreateWriter(string path)
    {
        var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        return new StreamWriter(stream, Encoding.UTF8)
        {
            AutoFlush = true
        };
    }

    private void DisposeWriter()
    {
        _writer?.Dispose();
        _writer = null;
    }

    private static string FormatEntry(DateTimeOffset timestamp, string level, string message)
    {
        var prefix = $"[{timestamp:yyyy-MM-dd HH:mm:ss}] [{level}] ";
        var normalized = (message ?? string.Empty).Replace("\r\n", "\n");
        var lines = normalized.Split('\n');
        var builder = new StringBuilder();

        for (var index = 0; index < lines.Length; index++)
        {
            if (index > 0)
            {
                builder.AppendLine();
            }

            builder.Append(prefix);
            builder.Append(lines[index]);
        }

        return builder.ToString();
    }
}
