using System.Text.Json.Serialization;
using LanTalk.Core.Models;

namespace LanTalk.Core.Serialization;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(UserInfo))]
[JsonSerializable(typeof(NetworkPacket))]
[JsonSerializable(typeof(ChatMessage))]
[JsonSerializable(typeof(FileTransferRequest))]
[JsonSerializable(typeof(FileTransferResponse))]
[JsonSerializable(typeof(FileTransferItem))]
[JsonSerializable(typeof(FileTransferResumeItem))]
[JsonSerializable(typeof(FileTransferFinished))]
[JsonSerializable(typeof(FileTransferRecord))]
[JsonSerializable(typeof(ImageMessageContent))]
[JsonSerializable(typeof(ChatGroup))]
[JsonSerializable(typeof(GroupMessagePayload))]
[JsonSerializable(typeof(EncryptedMessagePayload))]
[JsonSerializable(typeof(EncryptionHelloPayload))]
[JsonSerializable(typeof(EncryptionAckPayload))]
[JsonSerializable(typeof(EncryptionCancelPayload))]
[JsonSerializable(typeof(MessageReadReceiptPayload))]
[JsonSerializable(typeof(MessageRecallPayload))]
[JsonSerializable(typeof(OfflineFileReminderPayload))]
[JsonSerializable(typeof(DiscoveryPayload))]
[JsonSerializable(typeof(TextMessagePayload))]
[JsonSerializable(typeof(ErrorPayload))]
[JsonSerializable(typeof(BroadcastSendResult))]
[JsonSerializable(typeof(IReadOnlyList<ChatMessage>))]
[JsonSerializable(typeof(List<ChatMessage>))]
[JsonSerializable(typeof(IReadOnlyList<string>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(IReadOnlyList<FileTransferItem>))]
[JsonSerializable(typeof(List<FileTransferItem>))]
[JsonSerializable(typeof(IReadOnlyList<FileTransferResumeItem>))]
[JsonSerializable(typeof(List<FileTransferResumeItem>))]
public partial class LanTalkJsonContext : JsonSerializerContext
{
}
