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

`NetworkPacket.IsEncrypted` 用于标记 `PrivateMessage` 或 `GroupMessage` 的 `PayloadJson` 是否为加密载荷。旧客户端或未启用加密的会话会保持默认 `false`，按明文载荷处理。

## 端到端加密
- 加密范围：当前实现覆盖一对一私聊文本消息，以及群组文本消息的逐成员网络传输；广播、文件元数据和文件流仍使用原协议。
- 协商流程：用户在私聊会话中启用开关后，发送方发出 `EncryptionHello`，接收方使用临时 ECDH P-256 密钥派生会话密钥并返回 `EncryptionAck`。
- 消息加密：协商完成后，`PrivateMessage` 的 `PayloadJson` 改为 `EncryptedMessagePayload`，消息正文使用 AES-256-GCM 加密。
- 关闭流程：任一方关闭开关时发送 `EncryptionCancel`，双方清除内存中的会话密钥。
- 密钥存储：会话密钥只保存在运行内存中，不写入 SQLite 或设置文件；应用重启后需要重新协商。
- 指纹校验：界面会显示加密指纹，演示或真实使用时可由两端人工比对，降低中间人攻击风险。
- 群组模式：群组端到端加密不引入中心服务或共享群密钥，发送方复用每个成员已协商的一对一 E2EE 会话，分别加密同一条 `GroupMessagePayload`。
- 加密补发：加密群组消息的待投递记录会设置 `RequiresEncryption=1`。成员离线或尚未完成协商时，发送方只保存待补发记录并发起协商；补发时若密钥仍未就绪，不会降级为明文发送。
- 本地存储边界：聊天历史和待补发队列仍保存在本机 SQLite，当前加密保护的是局域网传输过程，不是本地数据库静态加密。

## 群组与多人会话
- 群组不依赖中心服务器，创建方维护成员列表，发送 `GroupMessage` 时按成员逐个 TCP 点对点发送。
- 群组消息载荷使用 `GroupMessagePayload`，包含 `GroupId`、群名称、群类型、成员 UserId 列表、发送者昵称和文本内容。
- 临时群组只保留在当前运行时会话列表。
- 永久群组保存到本地 SQLite `ChatGroups` 表，重启后恢复；接收方收到永久群组消息后也会保存该群组。
- 群组聊天记录继续写入 `ChatMessages`，其中 `SessionId` 和 `ReceiverId` 使用 `GroupId`。
- 群组图片/文件复用 `FileRequest`、`FileAccept`、`FileReject`、`FileFinished` 和 TCP 文件端口；发送方为每个在线成员生成独立 `FileId`，接收方仍按单文件确认和流式接收。
- 群组文件请求会在 `FileTransferRequest` 中追加可选元数据：`GroupId`、`GroupName`、`GroupKind`、`GroupMemberUserIds`、`GroupMessageId`。旧客户端缺少这些字段时会继续按普通文件请求处理。
- 群组离线补发使用发送方本地 SQLite `OutgoingDeliveries` 队列：离线成员或发送失败的成员会保留 `GroupMessagePayload` 或 `FileTransferRequest`，对方重新上线后自动补发，成功后删除队列记录。
- 群组附件补发会额外保存发送方本地 `SourcePath`。如果源文件已删除或移动，队列会保留并记录失败原因，不会静默丢弃。
- 群组文本端到端加密使用逐成员加密策略：开启后，在线且已完成一对一 E2EE 协商的成员立即收到加密 `GroupMessage`；离线或未协商成员进入 `OutgoingDeliveries` 队列并标记 `RequiresEncryption`。
- 后续若需要更接近成熟 IM 的群组加密，可在此基础上增加群密钥 epoch、成员变更轮换和附件内容加密。

## 文件传输
- 文件请求通过 TCP 消息端口发送 `FileTransferRequest`。
- 接收方返回 `FileTransferResponse`，对应 `FileAccept` 或 `FileReject`。
- 文件内容通过 TCP 文件端口传输。
- 单文件请求保持兼容旧字段：`FileId`、`FileName`、`FileSize`、`SenderId`、`ReceiverId`、`FilePort`。
- 多文件和文件夹请求使用同一个 `FileTransferRequest`，新增 `TransferKind`、`BatchId`、`BatchName` 和 `Items`：
  - `TransferKind=MultipleFiles` 表示一次选择的多个文件。
  - `TransferKind=Folder` 表示文件夹传输。
  - `Items` 中每个 `FileTransferItem` 都有独立 `FileId`、文件名、相对路径、大小和目录标记。
- 接收方对多文件/文件夹只确认一次；确认时在 `FileTransferResponse.ResumeItems` 中返回每个文件项已有的字节数。
- 文件夹只传相对路径，不传发送方绝对路径；接收方会拒绝绝对路径、`..` 和非法路径片段，避免写出接收目录。
- 文件流 v2 前缀包含协议标记、版本、`FileId`、文件总大小和续传 offset；服务端仍兼容旧的 `FileId + 文件大小` 前缀。
- 断点续传流程：
  1. 接收方接受请求时检查目标文件是否已存在。
  2. 若本地大小小于或等于期望大小，则把已有大小作为 offset 返回。
  3. 发送方从 offset 位置 `Seek` 后继续流式发送剩余内容。
  4. 接收方以追加方式写入，完成后继续发送 `FileFinished`。
- 文件内容使用 64KB 缓冲区分块传输，不使用 `File.ReadAllBytes`。
