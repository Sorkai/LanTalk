using LanTalk.Core.Services;

namespace LanTalk.Tests;

public sealed class ConsoleLanTalkLoggerTests
{
    [Fact]
    public void Info_ShouldWriteToDailyLogFile()
    {
        var directory = CreateTempDirectory();

        try
        {
            using var logger = new ConsoleLanTalkLogger(
                directory,
                maxFileSizeBytes: 256 * 1024,
                clock: () => new DateTimeOffset(2026, 6, 1, 8, 30, 0, TimeSpan.Zero));

            logger.Info("测试写入日志文件");
            logger.Dispose();

            var logPath = Path.Combine(directory, "lantalk-2026-06-01.log");
            Assert.True(File.Exists(logPath));
            var content = File.ReadAllText(logPath);
            Assert.Contains("[INFO] 测试写入日志文件", content);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Error_ShouldIncludeExceptionTypeAndStackTrace()
    {
        var directory = CreateTempDirectory();

        try
        {
            using var logger = new ConsoleLanTalkLogger(
                directory,
                maxFileSizeBytes: 256 * 1024,
                clock: () => new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero));

            Exception exception;
            try
            {
                ThrowFailure();
                throw new InvalidOperationException("不可达");
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            logger.Error("附件解密失败。", exception);
            logger.Dispose();

            var logPath = Path.Combine(directory, "lantalk-2026-06-01.log");
            var content = File.ReadAllText(logPath);
            Assert.Contains("附件解密失败。", content);
            Assert.Contains(nameof(InvalidOperationException), content);
            Assert.Contains(nameof(ThrowFailure), content);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Info_ShouldRotateFileWhenCurrentLogExceedsLimit()
    {
        var directory = CreateTempDirectory();

        try
        {
            using var logger = new ConsoleLanTalkLogger(
                directory,
                maxFileSizeBytes: 128,
                clock: () => new DateTimeOffset(2026, 6, 1, 10, 15, 0, TimeSpan.Zero));

            logger.Info(new string('A', 160));
            logger.Info(new string('B', 160));
            logger.Dispose();

            var files = Directory.GetFiles(directory, "lantalk-2026-06-01*.log");
            Assert.True(files.Length >= 2);
            var currentLog = Path.Combine(directory, "lantalk-2026-06-01.log");
            Assert.Contains(new string('B', 160), File.ReadAllText(currentLog));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Info_ShouldStartNewDailyLogWhenDateChanges()
    {
        var directory = CreateTempDirectory();
        var current = new DateTimeOffset(2026, 6, 1, 22, 0, 0, TimeSpan.Zero);

        try
        {
            using var logger = new ConsoleLanTalkLogger(
                directory,
                maxFileSizeBytes: 256 * 1024,
                clock: () => current);

            logger.Info("第一天");
            current = current.AddDays(1);
            logger.Info("第二天");
            logger.Dispose();

            Assert.True(File.Exists(Path.Combine(directory, "lantalk-2026-06-01.log")));
            Assert.True(File.Exists(Path.Combine(directory, "lantalk-2026-06-02.log")));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"LanTalk.LoggerTests.{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void ThrowFailure()
    {
        throw new InvalidOperationException("模拟异常");
    }
}
