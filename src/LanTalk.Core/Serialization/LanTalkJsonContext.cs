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
[JsonSerializable(typeof(FileTransferFinished))]
[JsonSerializable(typeof(FileTransferRecord))]
[JsonSerializable(typeof(EncryptedMessagePayload))]
[JsonSerializable(typeof(EncryptionHelloPayload))]
[JsonSerializable(typeof(EncryptionAckPayload))]
[JsonSerializable(typeof(EncryptionCancelPayload))]
[JsonSerializable(typeof(DiscoveryPayload))]
[JsonSerializable(typeof(TextMessagePayload))]
[JsonSerializable(typeof(ErrorPayload))]
[JsonSerializable(typeof(BroadcastSendResult))]
[JsonSerializable(typeof(IReadOnlyList<ChatMessage>))]
[JsonSerializable(typeof(List<ChatMessage>))]
public partial class LanTalkJsonContext : JsonSerializerContext
{
}
