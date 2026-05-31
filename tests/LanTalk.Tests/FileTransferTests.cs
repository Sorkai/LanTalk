using LanTalk.Core.Services;
using LanTalk.Network.Files;

namespace LanTalk.Tests;

public sealed class FileTransferTests
{
    [Fact]
    public async Task TcpFileClientAndServer_ShouldTransferFileByStreaming()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"lantalk-source-{Guid.NewGuid():N}.bin");
        var targetPath = Path.Combine(Path.GetTempPath(), $"lantalk-target-{Guid.NewGuid():N}.bin");
        var content = Enumerable.Range(0, 128 * 1024).Select(i => (byte)(i % 251)).ToArray();
        await File.WriteAllBytesAsync(sourcePath, content);

        var logger = new ConsoleLanTalkLogger();
        var server = new TcpFileServer(logger);
        var client = new TcpFileClient();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var port = Random.Shared.Next(59001, 60999);

        var serverTask = server.StartAsync(
            port,
            (_, _, _, _) => Task.FromResult<Stream>(File.Create(targetPath)),
            null,
            cts.Token);

        await Task.Delay(100, cts.Token);
        await client.SendFileAsync("127.0.0.1", port, "file-1", sourcePath, cancellationToken: cts.Token);
        await Task.Delay(100, cts.Token);
        await cts.CancelAsync();
        await Task.WhenAny(serverTask, Task.Delay(1000));

        Assert.True(File.Exists(targetPath));
        Assert.Equal(content.Length, new FileInfo(targetPath).Length);
        Assert.Equal(content, await File.ReadAllBytesAsync(targetPath));

        File.Delete(sourcePath);
        File.Delete(targetPath);
    }

    [Fact]
    public async Task TcpFileClientAndServer_ShouldResumeFromOffset()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"lantalk-source-{Guid.NewGuid():N}.bin");
        var targetPath = Path.Combine(Path.GetTempPath(), $"lantalk-target-{Guid.NewGuid():N}.bin");
        var content = Enumerable.Range(0, 192 * 1024).Select(i => (byte)(i % 251)).ToArray();
        var resumeOffset = 80 * 1024;
        await File.WriteAllBytesAsync(sourcePath, content);
        await File.WriteAllBytesAsync(targetPath, content[..resumeOffset]);

        var logger = new ConsoleLanTalkLogger();
        var server = new TcpFileServer(logger);
        var client = new TcpFileClient();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var port = Random.Shared.Next(59001, 60999);

        var serverTask = server.StartAsync(
            port,
            (_, _, offset, _) =>
            {
                Stream stream = new FileStream(targetPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
                stream.SetLength(offset);
                stream.Seek(offset, SeekOrigin.Begin);
                return Task.FromResult(stream);
            },
            null,
            cts.Token);

        await Task.Delay(100, cts.Token);
        await client.SendFileAsync("127.0.0.1", port, "file-1", sourcePath, resumeOffset: resumeOffset, cancellationToken: cts.Token);
        await Task.Delay(100, cts.Token);
        await cts.CancelAsync();
        await Task.WhenAny(serverTask, Task.Delay(1000));

        Assert.True(File.Exists(targetPath));
        Assert.Equal(content.Length, new FileInfo(targetPath).Length);
        Assert.Equal(content, await File.ReadAllBytesAsync(targetPath));

        File.Delete(sourcePath);
        File.Delete(targetPath);
    }
}
