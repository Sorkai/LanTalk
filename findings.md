# 发现与决策

## 需求
- LanTalk 是一个面向教室、实验室、小型办公室局域网场景的 C# / .NET 桌面即时通信客户端。
- 第一目标是完成 MVP，而不是一次性开发完整商业 IM。
- MVP 必备能力：
  - 局域网用户自动发现。
  - 在线用户列表。
  - 一对一私聊。
  - 全员广播消息。
  - 单文件传输。
  - 本地聊天记录。
  - 基础设置。
  - Telegram 风格现代化 UI。
  - 高性能、低资源占用、Native AOT 友好的代码结构。
- 必须使用的技术栈：
  - C#。
  - .NET 10。
  - Avalonia UI。
  - SukiUI。
  - MVVM。
  - CommunityToolkit.Mvvm。
  - UDP + TCP Socket。
  - SQLite。
  - JSON 配置文件。
  - System.Text.Json Source Generator。
- 当前用户请求：
  - 使用 `planning-with-files`。
  - 深度分析项目要求。
  - 按阶段规划完整项目开发。
  - 当前先开始计划。
- 项目文档推荐的第一步：
  - 创建项目骨架。
  - 接入 Avalonia UI 和 SukiUI。
  - 实现 Telegram 风格静态主窗口。
  - 实现本机设置保存。
  - 再进入 UDP 自动发现。

## 当前仓库发现
- 仓库根目录：`C:\pr\LanTalk`。
- 规划开始时已有文件：
  - `AGENTS.md`。
  - `lan_talk_codex项目说明文档.md`。
  - `.gitignore`。
  - `.git/`。
- 当前没有解决方案文件、源码项目、docs 目录、README 或测试项目。
- 因此实现应从阶段 1 的项目初始化开始，而不是修改已有应用代码。

## 架构发现
- 要求的解决方案结构：
  - `LanTalk.App`：Avalonia UI、SukiUI、Views、ViewModels、窗口/弹窗交互、主题、页面导航、调用服务。
  - `LanTalk.Core`：模型、枚举、协议 record、常量、序列化上下文、抽象接口；不依赖 UI、不实现数据库、不实现 Socket。
  - `LanTalk.Network`：UDP 自动发现、TCP 消息、TCP 文件传输、在线用户注册表、网络异常处理；只引用 Core。
  - `LanTalk.Storage`：SQLite、仓储、JSON 设置；只引用 Core。
  - `LanTalk.Tests`：序列化、仓储、设置、基础协议单元测试。
- App 可以引用 Core、Network、Storage、Avalonia、SukiUI、CommunityToolkit.Mvvm。
- Network 不得引用 Avalonia/SukiUI，也不得直接操作 UI。
- Storage 不得引用 Avalonia/SukiUI，也不得调用网络服务。
- View code-behind 应保持很薄，业务逻辑放在 ViewModel 和服务中。

## 协议发现
- 默认端口：
  - UDP 自动发现：50000。
  - TCP 消息：50001。
  - TCP 文件：50002。
- UDP 包类型：
  - HELLO。
  - ONLINE。
  - HEARTBEAT。
  - BYE。
- TCP 包/控制类型：
  - PRIVATE_MESSAGE。
  - BROADCAST_MESSAGE。
  - FILE_REQUEST。
  - FILE_ACCEPT。
  - FILE_REJECT。
  - FILE_FINISHED。
  - ERROR。
- 自动发现流程：
  - 加载本机设置。
  - 启动 UDP 监听。
  - 发送 HELLO。
  - 收到其他客户端 HELLO 后加入或更新在线用户列表。
  - 回复 ONLINE。
  - 每 5 秒发送 HEARTBEAT。
  - 15 秒未收到心跳则标记离线。
  - 关闭时发送 BYE。
- 私聊流程：
  - 选择在线用户。
  - 构造 PRIVATE_MESSAGE 包。
  - 通过 TCP 发送。
  - 接收方解析包、更新 UI、保存历史。
- 广播流程：
  - 用户进入广播模式。
  - 遍历在线用户。
  - 给每个用户发送 BROADCAST_MESSAGE。
  - 接收方用特殊样式显示广播，并保存记录。
- 文件传输流程：
  - 发送方选择单个文件。
  - 发送 FILE_REQUEST。
  - 接收方确认或拒绝。
  - 接受后通过 TCP 文件端口传输。
  - 分块流式传输文件、更新进度、写入配置中的接收目录、保存传输记录。

## 核心模型发现
- 必需或预期模型：
  - `UserInfo`。
  - `NetworkPacket`。
  - `ChatMessage`。
  - `AppSettings`。
  - `FileTransferRequest`。
  - `FileTransferRecord`。
  - 面向未来的协议载荷 `sealed record`。
- 必需或预期枚举：
  - `PacketType`。
  - `MessageKind`。
  - `UserStatus`。
  - `FileTransferStatus`。
- 必需设置默认值：
  - `UdpPort`: 50000。
  - `MessagePort`: 50001。
  - `FilePort`: 50002。
  - `SaveChatHistory`: true。
  - `ThemeMode`: System。
  - `ThemeColor`: Blue。

## 存储发现
- SQLite 默认路径建议位于 `%AppData%/LanTalk/lantalk.db`。
- 必需数据表：
  - `KnownUsers`。
  - `ChatMessages`。
  - `FileTransfers`。
- 必需索引：
  - `IX_ChatMessages_SessionId_SendTime`。
  - `IX_FileTransfers_TransferTime`。
- 聊天历史应只加载最近消息，默认最近 50 条。
- 设置应基于 JSON 文件，并能稳妥处理配置损坏。

## UI 发现
- 主窗口必须采用 Telegram 风格双栏布局：
  - 顶部标题/自定义窗口区。
  - 左侧侧栏：我的信息、搜索、最近会话、在线用户、广播入口、设置。
  - 右侧聊天面板：聊天标题、消息列表、输入框、发送按钮、文件按钮。
- UI 应清爽、现代，默认蓝色，并预留浅色/深色主题。
- 自己的消息靠右，对方消息靠左。
- 广播消息需要与私聊消息视觉区分。
- 文件消息使用卡片样式，展示文件名、大小、状态/进度。
- 避免大面积模糊、过多阴影、大量动画或每条消息上性能较重的视觉效果。
- Avalonia 实现应优先：
  - MVVM 绑定。
  - 可行时使用 `x:DataType` 与编译绑定。
  - 复用样式资源。
  - 简洁的 `DataTemplate`。
- UI 显示文本应使用中文。

## 性能与 AOT 发现
- 所有网络操作必须异步，并支持 `CancellationToken`。
- 不得用网络、文件、数据库 IO 阻塞 UI 线程。
- 避免 `Thread.Sleep`。
- 文件传输不得使用 `File.ReadAllBytes`。
- 文件传输使用 64KB 或 256KB 缓冲区，并结合 `ArrayPool` 分块流式传输。
- 避免动态 XAML、`Assembly.Load`、反射扫描、复杂动态代理和插件系统。
- ViewModel 使用 CommunityToolkit.Mvvm Source Generator。
- 设置与网络协议使用 System.Text.Json Source Generator。
- 普通 Release 优先；Native AOT 发布尝试等 MVP 稳定后再进行。

## 延期功能
- 没有用户明确要求时不实现：
  - 登录/注册或服务端。
  - 云同步。
  - 永久群聊。
  - 多文件/文件夹传输。
  - 断点续传。
  - 消息撤回。
  - 已读回执。
  - AES 加密。
  - 聊天记录搜索。
  - 系统托盘。
  - 桌面通知。
  - 开机自启。
  - 跨网段手动添加 IP。

## 技术决策
| 决策 | 理由 |
|------|------|
| 在项目根目录创建计划文件 | `planning-with-files` 要求项目本地持久化计划文件。 |
| 从阶段 1 项目骨架开始实现 | 当前仓库没有现成解决方案或源码。 |
| 本机设置先于 UDP 自动发现 | 自动发现需要稳定的 `UserId`、昵称和端口信息。 |
| 广播放在私聊之后 | 广播可复用 `MessageService` 和 TCP 包流程。 |
| 文件传输放在消息能力之后 | 文件协商依赖 TCP 控制消息。 |
| 通过 Microsoft.Data.Sqlite 直接访问 SQLite | 轻量，符合不引入大型 ORM 的约束。 |
| 初期使用手动服务组合 | 简单、透明、AOT 友好，避免复杂 DI。 |
| 仅在核心协议结构存在后添加未来 `ICompressor` 抽象 | 为 .NET 11 / Zstd 扩展预留空间，同时不扩大 MVP 实现范围。 |

## 已遇到的问题
| 问题 | 解决方式 |
|------|----------|
| 当前没有源码项目 | 计划从项目初始化和静态 UI 开始。 |

## 2026-05-30 通信体检发现
- 当前代码已存在真实 UDP/TCP 通信实现，不是纯静态 UI：
  - `UdpDiscoveryServer` 使用 `UdpClient` 监听与广播发现包。
  - `TcpMessageServer` / `TcpMessageClient` 使用 TCP 收发消息包。
  - `TcpFileServer` / `TcpFileClient` 使用 TCP 流式传输文件。
- 仍需补齐产品化细节：
  - UI 残留阶段 1 假数据和旧阶段提示。
  - 最近会话没有围绕真实发现用户和真实收发消息维护。
  - 历史消息显示名称可能退化为 `UserId`。
  - “刷新”按钮没有真实命令。
  - `KnownUsers` 表已创建但没有仓储和使用链路。
  - `FileFinished` / `Error` 协议枚举存在，但缺少完整发送和处理链路。

## 2026-05-30 下一步规划发现
- 当前 MVP 主功能已实现并通过本机构建、测试、Native AOT 发布与主题截图验证，下一步不应继续盲目加 P2 功能。
- 最优先风险来自真实局域网环境：
  - 防火墙是否拦截 UDP/TCP。
  - 多台设备是否能互相发现。
  - 关闭客户端后离线状态是否准时更新。
  - 广播和文件传输在多设备下是否稳定。
- 设置页目前端口仍偏展示性质，下一阶段应升级为可配置并增加校验，但要保守处理生效策略，避免运行时重启监听引入不稳定。
- 文件传输已经是流式实现，后续稳定性打磨应聚焦错误状态、保存路径异常、传输中断和 UI 反馈，不应扩大到断点续传。
- 文档和课堂演示资料需要等真实验收后再最终更新，否则验收矩阵会缺少实测证据。

## 2026-05-30 单机稳定性打磨发现
- 多机真实验收已按用户要求暂缓，当前优先处理不依赖额外电脑的阶段 12/13 任务。
- 端口设置已从展示项升级为可编辑项，采用保存后重启生效策略，避免运行时重启 UDP/TCP 监听引入额外不稳定。
- 配置文件层面已规范化非法端口和重复端口；UI 层面保存前会阻止非法范围与重复端口。
- App 启动时会预检测 UDP/TCP 默认或自定义端口是否可绑定，端口不可用时给出初始化失败状态。
- 搜索框过去只绑定了文本但没有行为；现在会过滤真实最近会话和在线用户列表。
- 文件接收目录创建或文件创建失败过去可能让后台接收任务静默失败；现在会更新文件消息状态、写入失败记录并尝试通知发送方。
- `TcpFileServer` 增加接收链路异常日志，便于后续实机排查防火墙、断连和保存失败。

## 2026-05-30 自动发现网段设置发现
- 旧实现中 `DiscoveryService` 的 `HELLO`、`HEARTBEAT`、`BYE` 默认只通过 `UdpDiscoveryServer` 发到 `255.255.255.255`；`ONLINE` 回复会定向发送到收到包的源地址。
- 新增 `DiscoverySubnet` 设置后，主动发现包会按设置解析为一个或多个广播目标；定向 `ONLINE` 回复继续使用源地址，避免改变握手流程。
- 网段输入采用轻量格式解析，不引入额外依赖：
  - `Auto` 或空值：使用默认全局广播 `255.255.255.255`。
  - `192.168.1.0/24`：解析为定向广播 `192.168.1.255`。
  - `192.168.1.*`：按 `/24` 通配网段处理。
  - `192.168.1.255`：直接作为目标广播地址发送。
  - 多个目标可用逗号或分号分隔。
- 设置保存时会规范化网段文本；配置损坏或非法网段会回退到 `Auto`，避免启动失败。

## 2026-05-30 多网段 UI 改造发现
- 底层 `DiscoverySubnetResolver` 已支持多个目标，但旧设置页只有单个文本框，用户需要手动输入逗号或分号，容易误解为只能配置一个网段。
- 本轮将设置页改为多行列表，每行一个发现目标，用户可以通过 `+` 添加、通过 `×` 删除。
- 保存时仍将多行合并为逗号分隔的轻量字符串，继续复用现有 `DiscoverySubnet` 配置字段，避免 JSON 配置结构膨胀或迁移成本。
- 设置面板内容改为可滚动，避免添加较多网段后底部保存按钮被挤出视图。

## 2026-05-31 内网通对标检查发现
- 当前 LanTalk 已覆盖内网通类软件的核心 MVP 主线：局域网自动发现、在线用户、一对一私聊、全员广播、单文件传输、本地聊天记录、基础设置、主题切换、端口和多网段发现配置。
- 本轮重新执行 `dotnet test LanTalk.sln -v:minimal`，结果为 24 个测试全部通过。
- 与公开资料中内网通常见能力相比，LanTalk 的主要差距不在“能不能聊天”，而在产品化成熟度和功能广度：
  - 缺少真实双机/三机验收数据，尤其是防火墙、跨网段、端口占用、100MB 文件、传输中断场景。
  - 缺少永久群组、多标签聊天、部门/联系人分组等多人协作体验。
  - 文件能力目前是单文件流式传输，未支持文件夹、多文件、断点续传、离线文件提醒。
  - 历史记录目前能保存和加载最近消息，但缺少聊天记录/文件记录搜索与导出。
  - 缺少图片预览/缩放、表情、头像、皮肤等高频体验细节。
  - 缺少已读回执、消息撤回、系统托盘、桌面通知、开机自启等成熟 IM 功能。
  - 缺少与飞鸽/飞秋协议互通和通信加密等对标内网通的差异化能力。
- 当前优先级建议：先完成实机验收与文件传输异常闭环，再做历史搜索/通知/托盘，最后再考虑永久群组、文件夹/断点续传、互通和加密。

## 2026-05-31 端到端加密实现发现

- 私聊文本加密已从 P2 差距项进入实现：
  - 会话内手动开启。
  - 临时 ECDH P-256 协商会话密钥。
  - AES-256-GCM 加密 `PrivateMessage` 的文本载荷。
  - 加密指纹显示给用户人工比对。
- 当前边界：
  - 广播不启用端到端加密。
  - 文件请求、文件元数据和文件流仍沿用原协议，后续若要做到完整文件 E2EE 需要扩展文件传输协议。

## 2026-05-31 聊天记录搜索实现发现
- 用户已完成多机验证和文件传输测试，反馈没有问题，因此阶段 11-13 可从“待实机验证”推进为完成状态。
- 第一项后续 P1 选择“当前会话内聊天记录搜索”，原因是收益高、边界清晰，不改变 UDP/TCP 协议，也不引入新依赖。
- 搜索实现落在 `MessageRepository.SearchMessagesAsync`，按 `SessionId` 限定当前会话，用 SQLite `LIKE` 查询 `Content`，并对 `%`、`_`、`\` 做转义，避免用户搜索特殊字符时变成通配查询。
- UI 在右侧聊天标题下增加“搜索当前聊天记录”输入框；输入关键词后展示当前会话匹配历史，清空后恢复最近消息。
- 新增测试覆盖会话隔离和 `%` 字符按字面量搜索。

## 2026-05-31 托盘、桌面通知与未读提醒发现
- 本轮实现继续保持轻量路线，没有新增 NuGet 包。
- 系统托盘使用 Avalonia 自带 `TrayIcon` 和 `NativeMenu`：
  - 点击窗口关闭按钮时默认隐藏到托盘，不停止 UDP/TCP/File 服务。
  - 托盘图标点击或菜单“打开 LanTalk”恢复窗口。
  - 托盘菜单“退出”才执行真实关闭和 `ShutdownAsync`。
- 桌面通知使用一个自绘 Avalonia 置顶小窗口，不依赖 Windows App SDK 或系统 Toast 注册；优点是轻量、AOT 风险小，限制是不会进入 Windows 通知中心。
- 未读提醒强化点：
  - `MainWindowViewModel.TotalUnreadCount` 汇总最近会话未读数量。
  - 窗口标题显示 `LanTalk (N)`。
  - 托盘 tooltip 和菜单显示未读总数。
  - 收到私聊、广播、文件请求时触发通知事件；窗口隐藏、最小化或非活跃时弹出桌面提醒。

## 2026-05-31 联系人分组与多会话体验发现
- 部门/分组需要成为真实协议和存储字段，而不是只在 UI 上分组；本轮已把 `Department` 加入 `AppSettings`、`UserInfo`、`DiscoveryPayload` 和 `KnownUsers`。
- 为兼容旧数据库，`DatabaseInitializer` 会检查 `KnownUsers` 是否缺少 `Department` 列，缺失时用默认部门迁移。
- 为兼容旧发现包，`DiscoveryPayload.Department` 使用默认值，接收端仍会对空白部门回落到“默认部门”。
- 最近会话排序应由真实互动驱动：收到消息、发送消息、选择会话会刷新活跃时间；普通 UDP 心跳/发现刷新不会持续把联系人顶到最前。
- 多会话切换采用 ViewModel 命令加按钮/快捷键实现，不改变网络层和存储层边界。

## 2026-05-31 图片、表情与头像体验发现
- 图片消息复用现有 TCP 文件传输通道最稳妥：`FileTransferRequest` 增加可选 `IsImage` 字段，旧客户端缺字段时默认按普通文件处理。
- 图片聊天历史不需要新增 SQLite 字段：使用 `MessageKind.Image` 和 `ImageMessageContent` JSON 存入 `ChatMessages.Content`，其中包含 `FileId`、文件名、大小和本地路径。
- 图片缩略图只从本地路径加载；路径失效时显示不可预览状态，不阻塞聊天记录加载。
- 表情输入保持纯 ViewModel 追加文本，不引入富文本控件或第三方 emoji 库，降低 AOT 和 UI 复杂度。
- 头像先采用昵称首字和稳定色彩生成，满足列表识别度，不引入头像上传、裁剪或同步协议。

## 2026-05-31 群组与多人会话实现发现
- 群组不引入服务端，使用 `GroupMessagePayload` 携带群信息和成员列表，发送时由本机遍历在线成员并逐个 TCP 发送。
- 群组聊天记录复用 `ChatMessages`，`SessionId` 使用 `GroupId`，因此现有最近消息加载和当前会话搜索可以继续复用。
- 永久群组需要独立 `ChatGroups` 表保存群名称、类型、成员列表和更新时间；临时群组不写入该表。
- 群组暂不接入一对一端到端加密和文件/图片发送，避免把私聊密钥状态与文件传输状态机混入多人场景。

## 2026-05-31 HKDF 官方实现替换发现
- .NET 提供 `System.Security.Cryptography.HKDF`，可直接执行 RFC5869 HMAC-based Extract-and-Expand Key Derivation。
- `HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, outputLength, salt, info)` 与当前私聊加密所需的 SHA-256 HKDF 参数模型匹配。
- 本轮替换只影响 `EndToEndEncryptionManager` 内部派生函数，保留现有 ECDH P-256、AES-256-GCM、密钥指纹、协议载荷和 UI 行为。
- 这能减少手写密码学代码面积，为后续群组端到端加密复用密钥派生逻辑打更稳的基础。

## 2026-05-31 群组高级功能规划发现
- 群组图片/文件应先做，因为现有 `FileTransferRequest`、`TcpFileClient`、`TcpFileServer` 和接收确认弹层已经稳定；不需要改变文件流传输协议。
- 发送方应为每个在线群成员生成独立 `FileId`，这样可以继续复用一对一接收确认、文件端口和完成确认逻辑；UI 再把这些独立传输聚合成一条群组附件消息。
- `FileTransferRequest` 需要追加可选群组元数据：`GroupId`、`GroupName`、`GroupKind`、`GroupMemberUserIds`、`GroupMessageId`；旧客户端缺字段时继续按普通文件处理。
- 离线补发不应与群组附件第一阶段混在一起；第一阶段先记录未在线/发送失败数量，第二阶段再把这些状态持久化为待投递队列。
- 群组端到端加密应最后实现；较稳的路径是先逐成员复用一对一密钥加密，后续再做群密钥和成员变更 epoch 轮换。

## 2026-05-31 群组图片/文件实现发现
- 已在不改变 TCP 文件流帧协议的前提下支持群组图片/文件：文件内容仍通过接收方 TCP 文件端口流式传输，控制消息仍使用 `FileRequest` / `FileAccept` / `FileReject` / `FileFinished`。
- 发送方为每个在线群成员生成独立 `FileId`，并通过内存中的聚合状态把多条一对一传输显示成群组会话中的一条附件消息。
- 接收方收到带 `GroupId` 的文件请求时会自动恢复群组会话，永久群组继续写入 `ChatGroups`；图片历史写入群组 `SessionId`，本地路径存在时可预览。
- 当前第一阶段只处理在线成员；离线成员补发需要在下一阶段把未在线/失败投递持久化。

## 2026-05-31 群组文本离线补发实现发现
- 已新增 `OutgoingDeliveries` 表和 `OutgoingDeliveryRepository`，发送方本地保存待补发的群组文本消息。
- 群组文本发送改为逐成员发送：在线成员立即 TCP 发送；离线成员或发送失败成员保存 `GroupMessagePayload`，对方重新上线后自动重试。
- 队列记录使用 `PacketType + RecipientId + MessageId` 组成稳定 `DeliveryId`，避免同一条群组消息重复入队。
- 本阶段只覆盖群组文本；群组附件补发需要额外保存源文件路径并处理源文件已删除、接收方再次确认等状态。

## 2026-05-31 群组附件离线补发实现发现
- `OutgoingDeliveryRecord` 新增 `SourcePath`，`OutgoingDeliveries` 表通过轻量迁移补齐 `SourcePath` 列，兼容已创建过旧队列表的本地数据库。
- 群组附件发送现在会覆盖全部非本人群成员：在线成员立即发送 `FileRequest`，离线成员或发送失败成员保存 `FileTransferRequest + SourcePath` 到待补发队列。
- 成员重新上线时会重新发送原 `FileRequest`；发送成功后删除队列记录，接收方仍走原有接收/拒绝/文件流流程。
- 如果发送方本地源文件已删除或移动，补发会保留队列记录并更新错误原因，避免把附件补发静默当作成功。

## 2026-05-31 群组端到端加密实现发现
- 群组 E2EE 第一阶段最稳的实现是逐成员加密：不设计共享群密钥，而是复用已有一对一 ECDH/HKDF/AES-GCM 会话，对每个收件人分别生成加密 `GroupMessage`。
- 加密群消息必须有“不降级明文”的队列语义；因此 `OutgoingDeliveries` 需要 `RequiresEncryption` 标记，补发时密钥未就绪只能继续等待或重新发起协商。
- 群组加密开关属于运行时状态，和已有一对一会话密钥一样不持久化；应用重启后需要重新启用/协商。
- 当前实现保护群组文本的局域网传输，不保护本地 SQLite 静态存储，也不加密图片/文件流。后续如果要达到更完整的群聊安全，应继续做附件内容加密、群密钥 epoch、成员变更轮换和设备指纹校验。

## 2026-05-31 多文件、文件夹传输与断点续传规划发现
- 现有 `TcpFileClient` / `TcpFileServer` 的流协议前缀只有 `FileId + FileSize`，无法表达续传 offset；需要新增 v2 header，同时让服务端兼容旧 header，避免单文件旧路径失效。
- 现有 UI 和 ViewModel 以“一个源路径对应一个 `FileId`”为核心假设；完整多文件和文件夹传输应增加批次模型，批次只确认一次，底层仍按文件项逐个流式发送，避免一次性读入内存或引入压缩依赖。
- 文件夹传输应保存相对路径，不应传绝对路径；接收端必须拒绝空路径、绝对路径、`..` 和非法路径片段，防止写出接收目录。
- 断点续传的低风险实现是接收端接受请求时检查已有目标文件长度，返回每个文件项的 `ResumeOffset`；发送端从该 offset `Seek` 后继续写 TCP 流。若本地文件长度大于期望大小，则从 0 开始覆盖。
- 群组附件已经支持按成员逐个发送和离线补发；批量附件可继续复用同一路线：每个成员收到一个批次请求，批次内每个文件项有独立 `FileId`，发送端用聚合状态更新 UI。

## 2026-05-31 多文件、文件夹传输与断点续传实现发现
- `FileTransferKind`、`FileTransferItem` 和 `FileTransferResumeItem` 已落地，`FileTransferRequest` 通过追加可选字段保持旧单文件协议兼容。
- TCP 文件流新增 v2 header：`-1` 标记、版本号、`FileId`、总大小和 offset；`TcpFileServer` 对旧 header 仍按 offset 0 处理。
- UI 文件按钮支持多选；选择 1 个文件时走旧单文件路径，选择多个文件时走批次路径。新增文件夹按钮会展开子目录并传相对路径。
- 接收端批次确认只弹一次；接受后为每个文件项保存路径、创建目录并返回续传 offset，文件夹路径会经过相对路径安全校验。
- SQLite `FileTransfers` 增加 `TransferKind`、`BatchId`、`RelativePath`、`BytesTransferred`，并提供旧表迁移。
- 本轮自动化验证更新到 55 项通过，新增覆盖批次序列化、续传 response、文件传输记录扩展字段、旧表迁移和 TCP 文件流 offset 续传。

## 2026-06-01 已读回执、消息撤回与离线文件提醒规划发现
- 阶段 20 已先按用户要求独立提交并推送到 `main`，提交为 `fe8d344 feat: add batch file transfers and resume`，自动化验证为 55 项通过。
- 已读回执需要区分私聊和群组：私聊可显示“未读/已读”，群组应记录成员维度并显示类似“已读 x/y”，避免把群组当成单个接收方。
- 消息撤回应作为控制消息同步状态，SQLite 保留原消息记录并追加撤回标记/时间，UI 显示“你撤回了一条消息”或“对方撤回了一条消息”，不要物理删除历史。
- 离线文件提醒不等同于服务端式离线文件箱；在当前无服务端架构下，发送方保存本地待提醒/待发送记录，对方上线后先收到提醒，再复用现有接收确认和 TCP 流式传输链路。

## 2026-06-01 已读回执、消息撤回与离线文件提醒实现发现
- 已读回执使用轻量控制包 `MessageReadReceipt`：私聊收到后直接置为已读，群组写入 `MessageReadReceipts` 并按 `MessageId + ReaderId` 去重统计。
- 群组已读目标人数来自发送方本地群组成员数减去自己；旧历史消息如果没有 `ReadTargetCount`，仍可记录已读人数，但不会误判全员已读。
- 消息撤回使用 `MessageRecall` 控制包和 `ChatMessages.IsRecalled/RecalledTime`，UI 层隐藏原文件/图片卡片并改为撤回提示，历史搜索加载后也保持撤回态。
- 离线文件提醒复用 `OutgoingDeliveries`：单文件保存源路径，批量/文件夹保存 fileId 到源路径的 base64 映射；成员上线重试前先发送 `OfflineFileReminder`，随后重新发送原 `FileRequest`。
- 当前自动化验证已扩展到 63 项，覆盖新增 payload 序列化、TCP 控制包回环、旧表迁移、仓储已读/撤回行为。

## 2026-06-01 下一步开发计划差距梳理
- `next-development-plan.md` 已把下一轮工作收敛成“文档闭环、日志生产化、性能优化、安全增强、导出/压缩、通知/开机自启”六类任务，适合继续沿用阶段化推进而不是大爆炸重构。
- 当前仓库里已经存在可复用基础：
  - `DesktopNotificationService` 已实现自绘 toast，可以作为通知抽象的 fallback。
  - `ICompressor` 已存在于 `LanTalk.Core/Compression`，但尚未进入实际文件传输管道。
  - 私聊 / 群组文本 E2EE、离线补发、批量文件传输和断点续传都已落地，可作为附件加密的兼容锚点。
- 当前仓库里仍然明确缺失的部分：
  - `ConsoleLanTalkLogger` 只有控制台输出，没有文件日志、滚动和长度保护。
  - `MainWindow.axaml` 的最近会话、联系人分组和消息区仍以 `ItemsControl` 为主，尚未利用虚拟化。
  - 没有聊天记录 / 文件记录导出入口，也没有压缩选项或开机自启设置。
  - README / `docs/test-plan.md` 仍保留旧阶段口径，未按“已完成 / 进行中 / 暂未实现”和“待人工验收项”重新收口。
- Windows 本机验证时，`dotnet build` 和 `dotnet test` 不能并行运行：并行执行会因为 `VBCSCompiler` 占用 `LanTalk.Core.dll` 触发 `CS2012` 文件锁错误；后续应统一采用串行验证。

## 2026-06-01 附件保护、导出与平台增强实现发现
- 受保护附件不需要推翻既有文件协议：在 `FileTransferRequest` 上追加 `Protection` 和加密元数据后，旧端仍能走普通附件路径，新端则可在已有一对一 E2EE 会话基础上启用附件元数据加密和 AES-GCM 文件流保护。
- 附件压缩最稳妥的落点是 ViewModel 发送前预处理和接收后解压，不把 `TcpFileServer` / `TcpFileClient` 改造成复杂的多层流状态机；因此当前实现默认关闭压缩，开启后使用临时文件和 GZip 回环。
- 聊天记录与文件传输记录导出无需新增复杂 DTO：仓储层直接提供按时间窗口导出查询，UI 层按 CSV / JSON 异步写文件即可，足以满足当前课堂 / 演示交付。
- Windows 开机自启采用当前用户 `Run` 注册表项是当前仓库里最轻量、最不破坏 AOT 结构的实现；系统通知则先抽象接口并保留应用内 toast 回退，避免为单一平台引入重型依赖。
- 发布验证再次证明：Windows 上 `build` / `test` / `publish` 都应串行执行，尤其两个 `publish` 并行时会触发 `MSB3713` 文件锁。

## 资源
- `C:\pr\LanTalk\AGENTS.md`：主要 Agent / 项目规则。
- `C:\pr\LanTalk\lan_talk_codex项目说明文档.md`：项目需求、架构、里程碑、验收要求。
- 计划文件：
  - `C:\pr\LanTalk\task_plan.md`。
  - `C:\pr\LanTalk\findings.md`。
  - `C:\pr\LanTalk\progress.md`。

## 视觉/浏览器发现
- 规划阶段未使用浏览器或图片检查。
