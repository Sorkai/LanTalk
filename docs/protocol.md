# LanTalk 协议说明

## 默认端口
- UDP 自动发现：50000
- TCP 消息：50001
- TCP 文件：50002

## UDP 包类型
- `Hello`：上线广播。
- `Online`：在线响应。
- `Heartbeat`：心跳。
- `Bye`：下线通知。

## TCP 包类型
- `PrivateMessage`：一对一私聊消息。
- `BroadcastMessage`：全员广播消息。
- `FileRequest`：文件发送请求。
- `FileAccept`：同意接收文件。
- `FileReject`：拒绝接收文件。
- `FileFinished`：文件传输完成。
- `Error`：错误消息。

## 包结构
网络包统一使用 `NetworkPacket`，载荷放入 `PayloadJson`，并使用 `LanTalkJsonContext` 进行 Source Generator 序列化。

## 文件传输
- 文件请求通过 TCP 消息端口发送 `FileTransferRequest`。
- 接收方返回 `FileTransferResponse`，对应 `FileAccept` 或 `FileReject`。
- 文件内容通过 TCP 文件端口传输。
- 文件流前缀包含 `FileId` 和文件大小。
- 文件内容使用 64KB 缓冲区分块传输，不使用 `File.ReadAllBytes`。
