namespace LanTalk.Core.Enums;

public enum PacketType
{
    Hello,
    Online,
    Heartbeat,
    Bye,
    PrivateMessage,
    BroadcastMessage,
    GroupMessage,
    FileRequest,
    FileAccept,
    FileReject,
    FileFinished,
    Error,
    EncryptionHello,
    EncryptionAck,
    EncryptionCancel
}
