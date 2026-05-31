# LanTalk 协议说明

## 默认端口
- UDP 自动发现：50000
- TCP 消息：50001
- TCP 文件：50002

## 自动发现目标
- 默认发现网段为 `Auto`，会向 `255.255.255.255` 发送 UDP 发现包。
- 设置中可逐条添加 CIDR 网段，例如 `192.168.1.0/24`，应用会计算并发送到该网段的广播地址。
- 设置中可逐条添加通配网段，例如 `192.168.1.*`，按 `/24` 网段处理。
- 设置中可逐条添加指定 IPv4 广播地址，例如 `192.168.1.255`。
- 多个目标会保存为逗号分隔的轻量配置，并在启动、刷新、心跳和退出时逐个发送。

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
- `EncryptionHello`：端到端加密协商请求。
- `EncryptionAck`：端到端加密协商确认。
- `EncryptionCancel`：关闭端到端加密会话。

## 包结构
网络包统一使用 `NetworkPacket`，载荷放入 `PayloadJson`，并使用 `LanTalkJsonContext` 进行 Source Generator 序列化。

`NetworkPacket.IsEncrypted` 用于标记 `PrivateMessage` 的 `PayloadJson` 是否为加密载荷。旧客户端或未启用加密的会话会保持默认 `false`，按明文 `TextMessagePayload` 处理。

## 端到端加密
- 加密范围：当前实现覆盖一对一私聊文本消息；广播、文件元数据和文件流仍使用原协议。
- 协商流程：用户在私聊会话中启用开关后，发送方发出 `EncryptionHello`，接收方使用临时 ECDH P-256 密钥派生会话密钥并返回 `EncryptionAck`。
- 消息加密：协商完成后，`PrivateMessage` 的 `PayloadJson` 改为 `EncryptedMessagePayload`，消息正文使用 AES-256-GCM 加密。
- 关闭流程：任一方关闭开关时发送 `EncryptionCancel`，双方清除内存中的会话密钥。
- 密钥存储：会话密钥只保存在运行内存中，不写入 SQLite 或设置文件；应用重启后需要重新协商。
- 指纹校验：界面会显示加密指纹，演示或真实使用时可由两端人工比对，降低中间人攻击风险。

## 文件传输
- 文件请求通过 TCP 消息端口发送 `FileTransferRequest`。
- 接收方返回 `FileTransferResponse`，对应 `FileAccept` 或 `FileReject`。
- 文件内容通过 TCP 文件端口传输。
- 文件流前缀包含 `FileId` 和文件大小。
- 文件内容使用 64KB 缓冲区分块传输，不使用 `File.ReadAllBytes`。
