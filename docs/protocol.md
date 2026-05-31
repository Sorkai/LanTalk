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
- `GroupMessage`：群组/多人会话消息。
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
- 加密范围：当前实现覆盖一对一私聊文本消息；广播、群组消息、文件元数据和文件流仍使用原协议。
- 协商流程：用户在私聊会话中启用开关后，发送方发出 `EncryptionHello`，接收方使用临时 ECDH P-256 密钥派生会话密钥并返回 `EncryptionAck`。
- 消息加密：协商完成后，`PrivateMessage` 的 `PayloadJson` 改为 `EncryptedMessagePayload`，消息正文使用 AES-256-GCM 加密。
- 关闭流程：任一方关闭开关时发送 `EncryptionCancel`，双方清除内存中的会话密钥。
- 密钥存储：会话密钥只保存在运行内存中，不写入 SQLite 或设置文件；应用重启后需要重新协商。
- 指纹校验：界面会显示加密指纹，演示或真实使用时可由两端人工比对，降低中间人攻击风险。

## 群组与多人会话
- 群组不依赖中心服务器，创建方维护成员列表，发送 `GroupMessage` 时按成员逐个 TCP 点对点发送。
- 群组消息载荷使用 `GroupMessagePayload`，包含 `GroupId`、群名称、群类型、成员 UserId 列表、发送者昵称和文本内容。
- 临时群组只保留在当前运行时会话列表。
- 永久群组保存到本地 SQLite `ChatGroups` 表，重启后恢复；接收方收到永久群组消息后也会保存该群组。
- 群组聊天记录继续写入 `ChatMessages`，其中 `SessionId` 和 `ReceiverId` 使用 `GroupId`。
- 群组图片/文件复用 `FileRequest`、`FileAccept`、`FileReject`、`FileFinished` 和 TCP 文件端口；发送方为每个在线成员生成独立 `FileId`，接收方仍按单文件确认和流式接收。
- 群组文件请求会在 `FileTransferRequest` 中追加可选元数据：`GroupId`、`GroupName`、`GroupKind`、`GroupMemberUserIds`、`GroupMessageId`。旧客户端缺少这些字段时会继续按普通文件请求处理。
- 群组加密和离线补发后续扩展。

## 文件传输
- 文件请求通过 TCP 消息端口发送 `FileTransferRequest`。
- 接收方返回 `FileTransferResponse`，对应 `FileAccept` 或 `FileReject`。
- 文件内容通过 TCP 文件端口传输。
- 文件流前缀包含 `FileId` 和文件大小。
- 文件内容使用 64KB 缓冲区分块传输，不使用 `File.ReadAllBytes`。
