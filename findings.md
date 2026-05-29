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

## 资源
- `C:\pr\LanTalk\AGENTS.md`：主要 Agent / 项目规则。
- `C:\pr\LanTalk\lan_talk_codex项目说明文档.md`：项目需求、架构、里程碑、验收要求。
- 计划文件：
  - `C:\pr\LanTalk\task_plan.md`。
  - `C:\pr\LanTalk\findings.md`。
  - `C:\pr\LanTalk\progress.md`。

## 视觉/浏览器发现
- 规划阶段未使用浏览器或图片检查。

