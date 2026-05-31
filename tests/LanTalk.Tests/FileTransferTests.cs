using System.Security.Cryptography;
using System.Text;
using LanTalk.Core.Compression;
using LanTalk.Core.Constants;
using LanTalk.Core.Enums;
using LanTalk.Core.Models;
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

    [Fact]
    public async Task TcpFileClientAndServer_ShouldTransferEncryptedFileByStreaming()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"lantalk-source-{Guid.NewGuid():N}.bin");
        var targetPath = Path.Combine(Path.GetTempPath(), $"lantalk-target-{Guid.NewGuid():N}.bin");
        var content = Encoding.UTF8.GetBytes(new string('加', 4096));
        await File.WriteAllBytesAsync(sourcePath, content);

        var logger = new ConsoleLanTalkLogger();
        var server = new TcpFileServer(logger);
        var client = new TcpFileClient();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var port = Random.Shared.Next(59001, 60999);
        var key = RandomNumberGenerator.GetBytes(32);

        var serverTask = server.StartAsync(
            port,
            (_, _, _, _) => Task.FromResult<Stream>(
                ProtectedFileTransfer.CreateDecryptingWriteStream(
                    File.Create(targetPath),
                    "file-encrypted",
                    content.Length,
                    key,
                    NetworkConstants.FileTransferBufferSize)),
            null,
            cts.Token);

        await Task.Delay(100, cts.Token);
        await client.SendFileAsync(
            "127.0.0.1",
            port,
            "file-encrypted",
            sourcePath,
            options: new FileTransferSendOptions
            {
                EncryptionKey = key,
                ChunkSize = NetworkConstants.FileTransferBufferSize
            },
            cancellationToken: cts.Token);
        await Task.Delay(100, cts.Token);
        await cts.CancelAsync();
        await Task.WhenAny(serverTask, Task.Delay(1000));

        Assert.True(File.Exists(targetPath));
        Assert.Equal(content, await File.ReadAllBytesAsync(targetPath));

        File.Delete(sourcePath);
        File.Delete(targetPath);
    }

    [Fact]
    public async Task GZipCompressor_ShouldRoundTripContent()
    {
        var content = Encoding.UTF8.GetBytes(string.Join('\n', Enumerable.Range(0, 512).Select(index => $"line-{index:D4}-LanTalk")));
        await using var source = new MemoryStream(content);
        await using var compressed = new MemoryStream();
        await using var restored = new MemoryStream();

        var compressor = new GZipCompressor();
        await compressor.CompressAsync(source, compressed);
        compressed.Position = 0;
        await compressor.DecompressAsync(compressed, restored);

        Assert.Equal(content, restored.ToArray());
    }

    [Fact]
    public void ProtectedFileTransfer_ShouldRoundTripMetadata()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var metadata = new EncryptedFileTransferMetadata(
            "design.png",
            2048,
            IsImage: true,
            FileTransferKind.SingleFile);
        var metadataJson = System.Text.Json.JsonSerializer.Serialize(
            metadata,
            LanTalk.Core.Serialization.LanTalkJsonContext.Default.EncryptedFileTransferMetadata);

        var payload = ProtectedFileTransfer.EncryptMetadata(key, "file-meta", metadataJson);
        var restoredJson = ProtectedFileTransfer.DecryptMetadata(key, "file-meta", payload);

        Assert.Contains("design.png", restoredJson);
    }
}
