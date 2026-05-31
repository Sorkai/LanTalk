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
}
