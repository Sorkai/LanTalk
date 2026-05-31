using System.Text.Json;
using LanTalk.Core.Enums;
using LanTalk.Core.Models;
using LanTalk.Core.Serialization;

namespace LanTalk.Tests;

public sealed class SerializationTests
{
    [Fact]
    public void NetworkPacket_ShouldRoundTrip_WithSourceGeneratedContext()
    {
        var packet = new NetworkPacket
        {
            Type = PacketType.Hello,
            FromUserId = "user-a",
            IsEncrypted = true,
            PayloadJson = "{}"
        };

        var json = JsonSerializer.Serialize(packet, LanTalkJsonContext.Default.NetworkPacket);
        var restored = JsonSerializer.Deserialize(json, LanTalkJsonContext.Default.NetworkPacket);

        Assert.NotNull(restored);
        Assert.Equal(PacketType.Hello, restored.Type);
        Assert.Equal("user-a", restored.FromUserId);
        Assert.True(restored.IsEncrypted);
    }

    [Fact]
    public void EncryptedMessagePayload_ShouldRoundTrip()
    {
        var payload = new EncryptedMessagePayload(
            "ECDH-P256-AES-256-GCM",
            "key-1",
            Convert.ToBase64String(new byte[] { 1, 2, 3 }),
            Convert.ToBase64String(new byte[] { 4, 5, 6 }),
            Convert.ToBase64String(new byte[] { 7, 8, 9 }));

        var json = JsonSerializer.Serialize(payload, LanTalkJsonContext.Default.EncryptedMessagePayload);
        var restored = JsonSerializer.Deserialize(json, LanTalkJsonContext.Default.EncryptedMessagePayload);

        Assert.NotNull(restored);
        Assert.Equal("key-1", restored.KeyId);
        Assert.Equal(payload.CipherText, restored.CipherText);
    }

    [Fact]
    public void DiscoveryPayload_ShouldRoundTripDepartment()
    {
        var payload = new DiscoveryPayload("user-a", "张同学", 50001, 50002, "研发部");

        var json = JsonSerializer.Serialize(payload, LanTalkJsonContext.Default.DiscoveryPayload);
        var restored = JsonSerializer.Deserialize(json, LanTalkJsonContext.Default.DiscoveryPayload);

        Assert.NotNull(restored);
        Assert.Equal("研发部", restored.Department);
    }

    [Fact]
    public void DiscoveryPayload_ShouldUseDefaultDepartmentForOldPackets()
    {
        const string json = """{"userId":"user-a","nickname":"张同学","messagePort":50001,"filePort":50002}""";

        var restored = JsonSerializer.Deserialize(json, LanTalkJsonContext.Default.DiscoveryPayload);

        Assert.NotNull(restored);
        Assert.Equal("默认部门", restored.Department);
    }

    [Fact]
    public void FileTransferRequest_ShouldRoundTripImageFlag()
    {
        var request = new FileTransferRequest("file-a", "photo.png", 1024, "user-a", "user-b", 50002, true);

        var json = JsonSerializer.Serialize(request, LanTalkJsonContext.Default.FileTransferRequest);
        var restored = JsonSerializer.Deserialize(json, LanTalkJsonContext.Default.FileTransferRequest);

        Assert.NotNull(restored);
        Assert.True(restored.IsImage);
    }

    [Fact]
    public void FileTransferRequest_ShouldDefaultImageFlagForOldPackets()
    {
        const string json = """{"fileId":"file-a","fileName":"photo.png","fileSize":1024,"senderId":"user-a","receiverId":"user-b","filePort":50002}""";

        var restored = JsonSerializer.Deserialize(json, LanTalkJsonContext.Default.FileTransferRequest);

        Assert.NotNull(restored);
        Assert.False(restored.IsImage);
        Assert.False(restored.IsGroupTransfer);
        Assert.Null(restored.GroupId);
    }

    [Fact]
    public void DiscoveryPayload_ShouldRoundTripProtectedAttachmentCapability()
    {
        var payload = new DiscoveryPayload("user-a", "张同学", 50001, 50002, "研发部", SupportsProtectedAttachments: true);

        var json = JsonSerializer.Serialize(payload, LanTalkJsonContext.Default.DiscoveryPayload);
        var restored = JsonSerializer.Deserialize(json, LanTalkJsonContext.Default.DiscoveryPayload);

        Assert.NotNull(restored);
        Assert.True(restored.SupportsProtectedAttachments);
    }

    [Fact]
    public void FileTransferRequest_ShouldRoundTripGroupMetadata()
    {
        var request = new FileTransferRequest(
            "file-a",
            "photo.png",
            1024,
            "user-a",
            "user-b",
            50002,
            true,
            "group-a",
            "项目组",
            GroupKind.Permanent,
            ["user-a", "user-b", "user-c"],
            "message-a");

        var json = JsonSerializer.Serialize(request, LanTalkJsonContext.Default.FileTransferRequest);
        var restored = JsonSerializer.Deserialize(json, LanTalkJsonContext.Default.FileTransferRequest);

        Assert.NotNull(restored);
        Assert.True(restored.IsImage);
        Assert.True(restored.IsGroupTransfer);
        Assert.Equal("group-a", restored.GroupId);
        Assert.Equal("项目组", restored.GroupName);
        Assert.Equal(GroupKind.Permanent, restored.GroupKind);
        Assert.Contains("user-c", restored.GroupMemberUserIds ?? []);
        Assert.Equal("message-a", restored.GroupMessageId);
    }

    [Fact]
    public void FileTransferRequest_ShouldRoundTripBatchItems()
    {
        var request = new FileTransferRequest(
            "batch-a",
            "资料包",
            4096,
            "user-a",
            "user-b",
            50002,
            TransferKind: FileTransferKind.Folder,
            BatchId: "batch-a",
            BatchName: "资料包",
            Items:
            [
                new FileTransferItem("dir-a", "docs", "docs", 0, true),
                new FileTransferItem("file-a", "readme.txt", @"docs\readme.txt", 4096)
            ]);

        var json = JsonSerializer.Serialize(request, LanTalkJsonContext.Default.FileTransferRequest);
        var restored = JsonSerializer.Deserialize(json, LanTalkJsonContext.Default.FileTransferRequest);

        Assert.NotNull(restored);
        Assert.True(restored.IsBatchTransfer);
        Assert.Equal(FileTransferKind.Folder, restored.TransferKind);
        Assert.Equal("batch-a", restored.BatchId);
        Assert.Equal("资料包", restored.BatchName);
        Assert.Equal(2, restored.TransferItems.Count);
        Assert.Contains(restored.TransferItems, item => item.IsDirectory && item.RelativePath == "docs");
        Assert.Contains(restored.TransferItems, item => item.FileId == "file-a" && item.FileSize == 4096);
    }

    [Fact]
    public void FileTransferRequest_ShouldRoundTripProtectionMetadata()
    {
        var request = new FileTransferRequest(
            "file-protected",
            "已加密文件",
            2048,
            "user-a",
            "user-b",
            50002,
            Protection: new FileTransferProtection(
                IsEncrypted: true,
                CompressionAlgorithm: "none",
                ChunkSize: 65536,
                ResumeSupported: false,
                MetadataPayload: new EncryptedMessagePayload(
                    "FILE-METADATA-AES-256-GCM",
                    "metadata",
                    Convert.ToBase64String(new byte[] { 1, 2, 3 }),
                    Convert.ToBase64String(new byte[] { 4, 5, 6 }),
                    Convert.ToBase64String(new byte[] { 7, 8, 9 }))));

        var json = JsonSerializer.Serialize(request, LanTalkJsonContext.Default.FileTransferRequest);
        var restored = JsonSerializer.Deserialize(json, LanTalkJsonContext.Default.FileTransferRequest);

        Assert.NotNull(restored);
        Assert.True(restored.IsProtectedTransfer);
        Assert.True(restored.IsEncryptedTransfer);
        Assert.False(restored.Protection?.ResumeSupported);
        Assert.NotNull(restored.Protection?.MetadataPayload);
    }

    [Fact]
    public void FileTransferResponse_ShouldRoundTripResumeOffsets()
    {
        var response = new FileTransferResponse(
            "batch-a",
            true,
            ResumeItems:
            [
                new FileTransferResumeItem("file-a", 1024),
                new FileTransferResumeItem("file-b", 0)
            ]);

        var json = JsonSerializer.Serialize(response, LanTalkJsonContext.Default.FileTransferResponse);
        var restored = JsonSerializer.Deserialize(json, LanTalkJsonContext.Default.FileTransferResponse);

        Assert.NotNull(restored);
        Assert.True(restored.Accepted);
        Assert.Equal(2, restored.ResumeItems?.Count);
        Assert.Contains(restored.ResumeItems ?? [], item => item.FileId == "file-a" && item.Offset == 1024);
    }

    [Fact]
    public void ImageMessageContent_ShouldRoundTrip()
    {
        var content = new ImageMessageContent("file-a", "photo.png", 1024, @"C:\Temp\photo.png");

        var json = JsonSerializer.Serialize(content, LanTalkJsonContext.Default.ImageMessageContent);
        var restored = JsonSerializer.Deserialize(json, LanTalkJsonContext.Default.ImageMessageContent);

        Assert.NotNull(restored);
        Assert.Equal("file-a", restored.FileId);
        Assert.Equal("photo.png", restored.FileName);
        Assert.Equal(1024, restored.FileSize);
        Assert.Equal(@"C:\Temp\photo.png", restored.LocalPath);
    }

    [Fact]
    public void GroupMessagePayload_ShouldRoundTrip()
    {
        var payload = new GroupMessagePayload(
            "message-a",
            "group-a",
            "项目组",
            GroupKind.Permanent,
            ["user-a", "user-b"],
            "张同学",
            "大家同步一下进度");

        var json = JsonSerializer.Serialize(payload, LanTalkJsonContext.Default.GroupMessagePayload);
        var restored = JsonSerializer.Deserialize(json, LanTalkJsonContext.Default.GroupMessagePayload);

        Assert.NotNull(restored);
        Assert.Equal("group-a", restored.GroupId);
        Assert.Equal(GroupKind.Permanent, restored.GroupKind);
        Assert.Contains("user-b", restored.MemberUserIds);
        Assert.Equal("大家同步一下进度", restored.Content);
    }

    [Fact]
    public void MessageReadReceiptPayload_ShouldRoundTrip()
    {
        var payload = new MessageReadReceiptPayload(
            "message-a",
            "group-a",
            "user-b",
            "李同学",
            IsGroup: true,
            DateTimeOffset.Parse("2026-06-01T10:00:00+08:00"));

        var json = JsonSerializer.Serialize(payload, LanTalkJsonContext.Default.MessageReadReceiptPayload);
        var restored = JsonSerializer.Deserialize(json, LanTalkJsonContext.Default.MessageReadReceiptPayload);

        Assert.NotNull(restored);
        Assert.True(restored.IsGroup);
        Assert.Equal("user-b", restored.ReaderUserId);
    }

    [Fact]
    public void MessageRecallPayload_ShouldRoundTrip()
    {
        var payload = new MessageRecallPayload(
            "message-a",
            "user-b",
            "user-a",
            "张同学",
            IsGroup: false,
            DateTimeOffset.Parse("2026-06-01T10:01:00+08:00"));

        var json = JsonSerializer.Serialize(payload, LanTalkJsonContext.Default.MessageRecallPayload);
        var restored = JsonSerializer.Deserialize(json, LanTalkJsonContext.Default.MessageRecallPayload);

        Assert.NotNull(restored);
        Assert.False(restored.IsGroup);
        Assert.Equal("message-a", restored.MessageId);
    }

    [Fact]
    public void OfflineFileReminderPayload_ShouldRoundTrip()
    {
        var payload = new OfflineFileReminderPayload(
            "reminder-a",
            "file-a",
            "资料包",
            4096,
            "user-a",
            "张同学",
            "user-b",
            IsImage: false,
            FileTransferKind.Folder,
            "batch-a",
            "资料包",
            "group-a",
            "项目组",
            GroupKind.Permanent,
            DateTimeOffset.Parse("2026-06-01T10:02:00+08:00"));

        var json = JsonSerializer.Serialize(payload, LanTalkJsonContext.Default.OfflineFileReminderPayload);
        var restored = JsonSerializer.Deserialize(json, LanTalkJsonContext.Default.OfflineFileReminderPayload);

        Assert.NotNull(restored);
        Assert.Equal(FileTransferKind.Folder, restored.TransferKind);
        Assert.Equal("项目组", restored.GroupName);
    }
}
