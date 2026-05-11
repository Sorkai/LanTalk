# LanTalk 局域网即时通信工具项目说明文档

> 本文档用于在 Codex / AI Agent 中继续推进项目开发。请将本文档作为项目需求说明、架构约束、MVP 开发计划和代码生成规范的统一依据。

---

## 1. 项目背景

本项目是一个 C# 课程大作业 / 小组项目，目标是开发一款类似“内网通”“飞鸽传书”风格的局域网即时通信工具。软件面向教室、实验室、小型办公室、局域网协作等场景，强调“无需注册、无需外网、打开即用、自动发现在线用户、可聊天、可传文件、界面美观、运行轻量”。

项目不是简单聊天 Demo，而是希望做成一个具备完整产品感的桌面客户端。第一阶段先完成 MVP，后续再逐步扩展复杂群聊、断点续传、加密、托盘、搜索等增强功能。

---

## 2. 项目名称与定位

### 2.1 暂定项目名称

LanTalk

可选中文名：

- 局域网即时通信工具
- 内联通
- LinkChat
- LanTalk 局域网通信系统

### 2.2 产品定位

LanTalk 是一款基于 C# / .NET 的局域网即时通信客户端，支持局域网用户自动发现、在线用户列表、私聊、广播消息、文件传输、聊天记录保存和现代化桌面 UI。

### 2.3 核心使用场景

1. 多台电脑处于同一局域网内。
2. 用户打开软件后，无需登录注册，自动显示在线用户。
3. 用户可以点击在线用户进行私聊。
4. 用户可以向所有在线用户发送广播消息。
5. 用户可以向指定用户发送文件。
6. 软件可以本地保存聊天记录和文件传输记录。
7. 软件界面风格接近 Telegram 桌面端，清爽、简洁、现代。

---

## 3. 技术路线

### 3.1 目标技术栈

- 开发语言：C#
- 运行平台：.NET 10
- UI 框架：Avalonia UI
- UI 主题库：SukiUI
- 架构模式：MVVM
- MVVM 工具：CommunityToolkit.Mvvm
- 网络通信：UDP + TCP Socket
- 本地数据库：SQLite
- 配置存储：JSON 配置文件
- 序列化：System.Text.Json Source Generator
- 发布方式：开发期普通 Debug / Release，最终尝试 Native AOT 发布
- 目标风格：Telegram 风格桌面聊天客户端

### 3.2 技术选择理由

1. Avalonia UI 支持跨平台桌面应用开发，适合构建现代化 C# 客户端。
2. SukiUI 可以快速提供现代化主题、明暗模式、主题色和美观控件。
3. .NET 10 + Native AOT 有利于降低启动开销和运行时依赖，但开发阶段不强制全程 AOT 调试。
4. UDP 适合局域网自动发现和心跳广播。
5. TCP 适合可靠文本消息和文件传输。
6. SQLite 适合轻量级本地聊天记录存储。
7. JSON 配置文件适合保存昵称、端口、文件保存路径等简单设置。

---

## 4. MVP 版本目标

MVP 的目标是做出一个“能真实运行、能稳定演示、像完整软件”的第一版，而不是一次性实现所有功能。

### 4.1 MVP 必须完成的功能

#### 4.1.1 用户基础信息

- 首次启动时设置昵称。
- 自动生成本机唯一 UserId。
- 显示本机昵称、本机 IP、在线状态。
- 保存本地设置，下次启动自动加载。
- 支持修改昵称和文件接收目录。

#### 4.1.2 局域网自动发现

- 启动后发送 UDP 上线广播。
- 其他客户端收到后自动加入在线用户列表。
- 定时发送心跳包。
- 超时未收到心跳则判断为离线。
- 关闭程序时发送下线通知。
- 支持手动刷新在线用户列表。

#### 4.1.3 在线用户列表

- 左侧显示在线用户列表。
- 每个用户显示昵称、IP、在线状态。
- 点击用户进入私聊界面。
- 当前会话有未读消息时显示提醒。

#### 4.1.4 私聊文本消息

- 支持一对一文本聊天。
- 支持消息气泡显示。
- 支持发送时间显示。
- 支持发送失败提示。
- 支持收到消息后自动加入对应会话。
- 支持本地保存聊天记录。
- 重启软件后能加载该联系人最近聊天记录。

#### 4.1.5 广播消息

- 支持向所有在线用户发送广播消息。
- 接收端显示为“广播消息”或“局域网通知”。
- 广播消息保存到本地聊天记录。

MVP 阶段不实现复杂永久群聊。广播消息作为群聊能力的简化替代。

#### 4.1.6 文件传输

- 支持向指定在线用户发送单个文件。
- 接收方弹出确认窗口。
- 接收方可以接收或拒绝。
- 文件通过 TCP 流式分块传输。
- 显示文件名、文件大小、传输进度。
- 传输完成后保存到默认目录。
- 保存文件传输记录。

#### 4.1.7 聊天记录与文件记录

- 使用 SQLite 保存私聊消息。
- 使用 SQLite 保存广播消息。
- 使用 SQLite 保存文件传输记录。
- 打开某个联系人时加载最近消息。

#### 4.1.8 现代化 UI

- 主界面采用 Telegram 风格双栏布局。
- 左侧为个人信息、搜索框、最近会话、在线用户。
- 右侧为聊天标题、消息区、输入区。
- 消息区使用左右气泡。
- 自己的消息靠右，对方消息靠左。
- 文件消息以卡片形式展示。
- 支持浅色 / 深色主题，至少预留主题切换接口。

---

## 5. MVP 暂不实现的功能

以下功能暂不放入 MVP，避免范围失控：

1. 登录注册。
2. 中央服务器。
3. 永久群聊和群成员管理。
4. 离线消息。
5. 断点续传。
6. 文件夹传输。
7. 多文件队列。
8. 秒传。
9. 消息撤回。
10. 已读回执。
11. 复杂富文本。
12. 截图发送。
13. 语音 / 视频聊天。
14. 远程协助。
15. 复杂权限管理。
16. 云同步。
17. 插件系统。

这些功能可以作为后续增强版本规划。

---

## 6. 总体架构

项目建议采用分层架构，避免 UI、网络、存储代码混在一起。

```text
LanTalk
├── LanTalk.App       Avalonia UI 客户端
├── LanTalk.Core      核心模型、协议、公共接口
├── LanTalk.Network   UDP/TCP 网络通信实现
├── LanTalk.Storage   SQLite、配置文件、本地记录
└── LanTalk.Tests     单元测试和基础集成测试
```

### 6.1 LanTalk.App

职责：

- Avalonia / SukiUI 主界面。
- View 和 ViewModel。
- 用户交互。
- 消息展示。
- 文件接收弹窗。
- 设置页面。

不得直接写底层 Socket 代码。

### 6.2 LanTalk.Core

职责：

- 核心实体模型。
- 网络协议模型。
- 枚举类型。
- 公共接口定义。
- 与 UI 无关的业务类型。

示例：

- UserInfo
- ChatMessage
- NetworkPacket
- FileTransferRequest
- FileTransferTask
- AppSettings
- PacketType
- MessageKind
- UserStatus

### 6.3 LanTalk.Network

职责：

- UDP 发现。
- 心跳发送与接收。
- TCP 消息监听与发送。
- TCP 文件传输。
- 网络异常处理。

不得依赖 Avalonia 控件。

### 6.4 LanTalk.Storage

职责：

- SQLite 初始化。
- 聊天记录保存和读取。
- 文件传输记录保存和读取。
- JSON 配置读写。

不得依赖 Avalonia 控件。

### 6.5 LanTalk.Tests

职责：

- 协议序列化测试。
- 配置读写测试。
- 数据库初始化测试。
- 消息仓储测试。
- 网络协议基础测试。

---

## 7. 推荐目录结构

```text
LanTalk/
├── LanTalk.sln
├── README.md
├── docs/
│   ├── project-spec.md
│   ├── protocol.md
│   ├── ui-design.md
│   └── test-plan.md
├── src/
│   ├── LanTalk.App/
│   │   ├── App.axaml
│   │   ├── App.axaml.cs
│   │   ├── Program.cs
│   │   ├── Views/
│   │   │   ├── MainWindow.axaml
│   │   │   ├── ChatView.axaml
│   │   │   ├── SidebarView.axaml
│   │   │   ├── SettingsWindow.axaml
│   │   │   ├── FileReceiveDialog.axaml
│   │   │   └── FileTransferDialog.axaml
│   │   ├── ViewModels/
│   │   │   ├── MainViewModel.cs
│   │   │   ├── ChatViewModel.cs
│   │   │   ├── SidebarViewModel.cs
│   │   │   ├── OnlineUserViewModel.cs
│   │   │   ├── ChatMessageViewModel.cs
│   │   │   ├── SettingsViewModel.cs
│   │   │   └── FileTransferViewModel.cs
│   │   ├── Services/
│   │   │   ├── AppServices.cs
│   │   │   ├── NotificationService.cs
│   │   │   └── DialogService.cs
│   │   ├── Styles/
│   │   │   ├── Colors.axaml
│   │   │   ├── Controls.axaml
│   │   │   └── Chat.axaml
│   │   └── Assets/
│   │       └── default-avatar.png
│   ├── LanTalk.Core/
│   │   ├── Models/
│   │   │   ├── UserInfo.cs
│   │   │   ├── ChatMessage.cs
│   │   │   ├── NetworkPacket.cs
│   │   │   ├── FileTransferRequest.cs
│   │   │   ├── FileTransferRecord.cs
│   │   │   └── AppSettings.cs
│   │   ├── Enums/
│   │   │   ├── PacketType.cs
│   │   │   ├── MessageKind.cs
│   │   │   ├── UserStatus.cs
│   │   │   └── FileTransferStatus.cs
│   │   ├── Serialization/
│   │   │   └── LanTalkJsonContext.cs
│   │   └── Constants/
│   │       └── NetworkConstants.cs
│   ├── LanTalk.Network/
│   │   ├── Discovery/
│   │   │   ├── DiscoveryService.cs
│   │   │   ├── UdpDiscoveryServer.cs
│   │   │   └── OnlineUserRegistry.cs
│   │   ├── Messaging/
│   │   │   ├── MessageService.cs
│   │   │   ├── TcpMessageServer.cs
│   │   │   └── TcpMessageClient.cs
│   │   └── Files/
│   │       ├── FileTransferService.cs
│   │       ├── TcpFileServer.cs
│   │       └── TcpFileClient.cs
│   ├── LanTalk.Storage/
│   │   ├── Database/
│   │   │   ├── DatabaseInitializer.cs
│   │   │   └── SqliteConnectionFactory.cs
│   │   ├── Repositories/
│   │   │   ├── MessageRepository.cs
│   │   │   ├── UserRepository.cs
│   │   │   └── FileTransferRepository.cs
│   │   └── Settings/
│   │       └── SettingsService.cs
│   └── LanTalk.Tests/
│       ├── SerializationTests.cs
│       ├── SettingsServiceTests.cs
│       └── MessageRepositoryTests.cs
```

---

## 8. 网络通信设计

### 8.1 端口设计

```text
UDP 发现端口：50000
TCP 消息端口：50001
TCP 文件端口：50002
```

端口后续可放入设置中修改。MVP 可先固定端口。

### 8.2 UDP 消息类型

```text
HELLO      上线广播
ONLINE     在线响应
HEARTBEAT  心跳包
BYE        下线通知
```

### 8.3 TCP 消息类型

```text
PRIVATE_MESSAGE    私聊消息
BROADCAST_MESSAGE  广播消息
FILE_REQUEST       文件发送请求
FILE_ACCEPT        接收文件
FILE_REJECT        拒绝文件
FILE_FINISHED      文件传输完成
ERROR              错误消息
```

### 8.4 用户发现流程

```text
客户端启动
↓
读取本机设置
↓
启动 UDP 监听
↓
发送 HELLO 广播
↓
其他客户端收到 HELLO
↓
加入或更新在线用户列表
↓
回复 ONLINE
↓
双方开始定时发送 HEARTBEAT
↓
超过 12~15 秒未收到心跳则标记离线
```

### 8.5 私聊消息流程

```text
用户选择联系人
↓
输入消息
↓
点击发送
↓
构造 PRIVATE_MESSAGE 包
↓
通过 TCP 发送给目标 IP:消息端口
↓
接收方 TCP 服务收到消息
↓
反序列化并校验
↓
更新 UI
↓
保存聊天记录
```

### 8.6 广播消息流程

```text
用户点击广播入口
↓
输入广播内容
↓
遍历在线用户列表
↓
对每个在线用户发送 BROADCAST_MESSAGE
↓
接收端显示为广播消息
↓
保存记录
```

### 8.7 文件传输流程

```text
发送方选择文件
↓
构造 FILE_REQUEST
↓
接收方弹出确认窗口
↓
接收方点击接收
↓
发送 FILE_ACCEPT
↓
发送方通过 TCP 文件端口开始传输
↓
文件按 64KB 或 256KB 分块读取
↓
接收方流式写入文件
↓
双方更新进度
↓
传输完成后保存文件记录
```

### 8.8 心跳与离线策略

建议参数：

```text
心跳间隔：5 秒
离线判断超时：15 秒
UDP 接收循环：后台异步任务
UI 更新：节流或批量刷新
```

---

## 9. 核心模型设计

### 9.1 UserInfo

```csharp
public sealed class UserInfo
{
    public string UserId { get; init; } = string.Empty;
    public string Nickname { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
    public int MessagePort { get; init; }
    public int FilePort { get; init; }
    public UserStatus Status { get; init; }
    public DateTimeOffset LastSeenTime { get; set; }
}
```

### 9.2 NetworkPacket

```csharp
public sealed class NetworkPacket
{
    public string PacketId { get; init; } = Guid.NewGuid().ToString("N");
    public PacketType Type { get; init; }
    public string FromUserId { get; init; } = string.Empty;
    public string? ToUserId { get; init; }
    public DateTimeOffset Time { get; init; } = DateTimeOffset.Now;
    public string PayloadJson { get; init; } = string.Empty;
}
```

### 9.3 ChatMessage

```csharp
public sealed class ChatMessage
{
    public string MessageId { get; init; } = Guid.NewGuid().ToString("N");
    public string SessionId { get; init; } = string.Empty;
    public string SenderId { get; init; } = string.Empty;
    public string? ReceiverId { get; init; }
    public MessageKind Kind { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTimeOffset SendTime { get; init; }
    public bool IsMine { get; init; }
}
```

### 9.4 AppSettings

```csharp
public sealed class AppSettings
{
    public string UserId { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public int UdpPort { get; set; } = 50000;
    public int MessagePort { get; set; } = 50001;
    public int FilePort { get; set; } = 50002;
    public string FileSavePath { get; set; } = string.Empty;
    public bool SaveChatHistory { get; set; } = true;
    public string ThemeMode { get; set; } = "System";
    public string ThemeColor { get; set; } = "Blue";
}
```

---

## 10. 数据库设计

使用 SQLite。数据库文件建议保存在应用数据目录，例如：

```text
%AppData%/LanTalk/lantalk.db
```

### 10.1 KnownUsers

```sql
CREATE TABLE IF NOT EXISTS KnownUsers (
    UserId TEXT PRIMARY KEY,
    Nickname TEXT NOT NULL,
    IpAddress TEXT NOT NULL,
    MessagePort INTEGER NOT NULL,
    FilePort INTEGER NOT NULL,
    Status TEXT NOT NULL,
    LastSeenTime TEXT NOT NULL
);
```

### 10.2 ChatMessages

```sql
CREATE TABLE IF NOT EXISTS ChatMessages (
    MessageId TEXT PRIMARY KEY,
    SessionId TEXT NOT NULL,
    SenderId TEXT NOT NULL,
    ReceiverId TEXT,
    MessageType TEXT NOT NULL,
    Content TEXT NOT NULL,
    SendTime TEXT NOT NULL,
    IsMine INTEGER NOT NULL
);
```

### 10.3 FileTransfers

```sql
CREATE TABLE IF NOT EXISTS FileTransfers (
    FileId TEXT PRIMARY KEY,
    SenderId TEXT NOT NULL,
    ReceiverId TEXT NOT NULL,
    FileName TEXT NOT NULL,
    FileSize INTEGER NOT NULL,
    SavePath TEXT,
    Status TEXT NOT NULL,
    TransferTime TEXT NOT NULL
);
```

### 10.4 索引建议

```sql
CREATE INDEX IF NOT EXISTS IX_ChatMessages_SessionId_SendTime
ON ChatMessages(SessionId, SendTime);

CREATE INDEX IF NOT EXISTS IX_FileTransfers_TransferTime
ON FileTransfers(TransferTime);
```

---

## 11. UI 设计要求

### 11.1 整体风格

- 参考 Telegram 桌面端。
- 双栏布局。
- 左侧为会话和在线用户。
- 右侧为聊天内容。
- 颜色清爽，默认蓝色主题。
- 尽量少用复杂阴影和重动画。
- 优先保证流畅、清晰、现代。

### 11.2 主窗口结构

```text
MainWindow
├── 顶部标题栏
├── 左侧 Sidebar
│   ├── 我的信息
│   ├── 搜索框
│   ├── 最近会话
│   ├── 在线用户
│   └── 设置按钮
└── 右侧 ChatPanel
    ├── 聊天头部
    ├── 消息列表
    └── 输入栏
```

### 11.3 左侧 Sidebar

内容：

- 头像或字母头像。
- 昵称。
- 在线状态。
- 搜索框。
- 在线用户数量。
- 用户列表。
- 广播入口。
- 设置入口。

### 11.4 右侧 ChatPanel

内容：

- 当前聊天对象昵称。
- IP 地址 / 在线状态。
- 消息列表。
- 文件消息卡片。
- 输入框。
- 发送按钮。
- 发送文件按钮。

### 11.5 消息气泡

- 自己消息靠右。
- 对方消息靠左。
- 广播消息可使用系统提示或特殊气泡。
- 文件消息使用卡片，显示文件名、大小、传输状态。
- 时间显示不要太突兀。

### 11.6 设置窗口

MVP 设置项：

- 昵称。
- 文件接收目录。
- 是否保存聊天记录。
- 主题模式。
- 当前 UDP / TCP 端口展示。

---

## 12. Native AOT 与性能约束

项目需要面向 Native AOT 友好设计，但开发阶段不要求每次都以 AOT 方式运行。

### 12.1 开发策略

1. 开发期使用普通 Debug 运行。
2. 每完成一个阶段，执行一次 Release 发布检查。
3. 后期统一修复 Native AOT 警告和裁剪问题。
4. 代码从一开始避免明显不兼容 AOT 的写法。

### 12.2 AOT 兼容要求

必须遵守：

- JSON 序列化使用 Source Generator。
- XAML 尽量使用编译绑定。
- 避免动态加载 XAML。
- 避免 Assembly.Load。
- 避免不必要的反射扫描。
- 避免使用需要大量运行时动态代理的框架。
- 不做插件系统。
- ViewModel 使用 CommunityToolkit.Mvvm Source Generator。

### 12.3 System.Text.Json Source Generator 示例

```csharp
using System.Text.Json.Serialization;

[JsonSerializable(typeof(NetworkPacket))]
[JsonSerializable(typeof(UserInfo))]
[JsonSerializable(typeof(ChatMessage))]
[JsonSerializable(typeof(FileTransferRequest))]
[JsonSerializable(typeof(AppSettings))]
public partial class LanTalkJsonContext : JsonSerializerContext
{
}
```

使用时：

```csharp
var json = JsonSerializer.Serialize(packet, LanTalkJsonContext.Default.NetworkPacket);
var packet = JsonSerializer.Deserialize(json, LanTalkJsonContext.Default.NetworkPacket);
```

### 12.4 UI 性能要求

- 聊天列表不要一次性加载所有历史记录。
- 每次打开会话只加载最近 50 条消息。
- 后续可实现上滑加载更多。
- 消息模板要简单，不要为每条消息加复杂动画。
- 不要频繁刷新整个用户列表。
- 收到心跳只更新必要字段。

### 12.5 文件传输性能要求

- 不允许使用 File.ReadAllBytes 读取大文件。
- 必须使用流式分块传输。
- 推荐缓冲区大小：64KB 或 256KB。
- 文件传输不能阻塞 UI 线程。
- 使用 async / await。
- 可使用 ArrayPool<byte> 复用缓冲区。

示例原则：

```csharp
await using var fs = File.OpenRead(path);
var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
try
{
    int read;
    while ((read = await fs.ReadAsync(buffer, cancellationToken)) > 0)
    {
        await networkStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
    }
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}
```

---

## 13. 编码规范

### 13.1 基础规范

- 使用 C# 现代语法。
- 开启 Nullable。
- 开启 ImplicitUsings。
- 类名、方法名使用英文。
- UI 文本使用中文。
- 每个类职责单一。
- 不要在 View 的 code-behind 中写复杂业务逻辑。
- 网络层不得引用 Avalonia。
- 存储层不得引用 Avalonia。
- 所有网络任务必须支持 CancellationToken。
- 所有 Socket 异常必须捕获并记录。

### 13.2 命名规范

- 实体模型：UserInfo、ChatMessage、NetworkPacket。
- 服务类：DiscoveryService、MessageService、FileTransferService。
- 仓储类：MessageRepository、UserRepository。
- ViewModel：MainViewModel、ChatViewModel。
- 枚举：PacketType、MessageKind、UserStatus。

### 13.3 错误处理

必须处理：

- 端口被占用。
- UDP 广播失败。
- TCP 连接失败。
- 对方离线。
- 文件路径不存在。
- 文件保存失败。
- JSON 解析失败。
- 数据库初始化失败。

### 13.4 日志要求

MVP 阶段至少使用简单日志输出：

- 程序启动。
- 用户上线 / 下线。
- 消息发送 / 接收。
- 文件传输开始 / 完成 / 失败。
- 网络异常。
- 数据库异常。

---

## 14. 开发里程碑

### 阶段 1：项目骨架与 UI 壳子

目标：软件能启动，界面像聊天软件。

任务：

1. 创建解决方案和四个主要项目。
2. 配置 Avalonia UI。
3. 引入 SukiUI。
4. 搭建 MainWindow 双栏布局。
5. 添加假数据在线用户列表。
6. 添加假数据消息气泡。
7. 完成设置窗口 UI。

验收标准：

- 软件可启动。
- 左侧能显示用户列表假数据。
- 右侧能显示聊天气泡假数据。
- UI 风格接近 Telegram。

### 阶段 2：本机设置与配置保存

目标：本机身份可保存。

任务：

1. 实现 AppSettings。
2. 实现 SettingsService。
3. 首次启动生成 UserId。
4. 首次启动输入昵称。
5. 保存文件接收目录。
6. 重启后加载设置。

验收标准：

- 修改昵称后重启仍然保留。
- 本机 UserId 不重复生成。
- 文件保存目录可配置。

### 阶段 3：UDP 自动发现

目标：多客户端自动发现。

任务：

1. 实现 UdpDiscoveryServer。
2. 实现 DiscoveryService。
3. 发送 HELLO。
4. 回复 ONLINE。
5. 定时 HEARTBEAT。
6. 处理 BYE。
7. 维护 OnlineUserRegistry。
8. UI 实时显示在线用户。

验收标准：

- 两台电脑打开软件后互相发现。
- 关闭一台电脑后另一台能看到离线。
- 用户重新上线后能重新显示。

### 阶段 4：TCP 私聊消息

目标：能够真实聊天。

任务：

1. 实现 TcpMessageServer。
2. 实现 TcpMessageClient。
3. 实现 MessageService。
4. 发送 PRIVATE_MESSAGE。
5. 接收 PRIVATE_MESSAGE。
6. UI 显示消息气泡。
7. 保存聊天记录。
8. 打开联系人加载最近消息。

验收标准：

- A 可以给 B 发消息。
- B 可以回复 A。
- 双方 UI 正确显示。
- 重启后可看到历史消息。

### 阶段 5：广播消息

目标：可以向所有在线用户发消息。

任务：

1. 添加广播入口。
2. 构造 BROADCAST_MESSAGE。
3. 遍历在线用户发送。
4. 接收端显示广播消息。
5. 保存广播记录。

验收标准：

- A 发广播，B/C 都能收到。
- 广播消息显示样式与普通私聊区分。

### 阶段 6：文件传输

目标：可以发送单个文件。

任务：

1. 实现 FILE_REQUEST。
2. 接收端显示确认弹窗。
3. 实现 FILE_ACCEPT / FILE_REJECT。
4. 实现 TcpFileServer。
5. 实现 TcpFileClient。
6. 实现流式分块传输。
7. UI 显示传输进度。
8. 保存文件传输记录。

验收标准：

- A 可以给 B 发送文件。
- B 可选择接收或拒绝。
- 文件传输时 UI 不冻结。
- 100MB 文件可以正常传输。

### 阶段 7：打磨与发布

目标：可展示、可打包、可汇报。

任务：

1. 优化 UI 样式。
2. 修复异常提示。
3. 增加基础日志。
4. 增加 README。
5. 编写测试说明。
6. 尝试 Release 发布。
7. 尝试 Native AOT 发布。
8. 准备演示流程。

验收标准：

- 软件可以在多台电脑上稳定演示。
- 没有明显 UI 卡顿。
- 没有未处理异常弹出。
- 演示流程完整。

---

## 15. 课堂演示流程

建议最终展示按照如下顺序：

1. 三台电脑启动 LanTalk。
2. 软件自动发现局域网用户。
3. 展示在线用户列表。
4. A 点击 B，发送私聊消息。
5. B 回复 A。
6. A 发送广播消息，B 和 C 同时收到。
7. B 给 A 发送文件。
8. A 点击接收，显示进度条。
9. 文件传输完成后打开文件所在目录。
10. A 打开聊天记录，展示刚才的消息仍然存在。
11. 修改昵称或文件接收路径。
12. 关闭 C，其他客户端显示 C 离线。

---

## 16. 验收标准

### 16.1 基础运行

| 测试项 | 标准 |
|---|---|
| 软件启动 | 正常打开主界面 |
| 昵称设置 | 可保存，可重新加载 |
| 本机 IP 显示 | 能显示局域网 IP |
| 设置保存 | 重启后仍然有效 |
| 异常处理 | 常见错误有提示 |

### 16.2 自动发现

| 测试项 | 标准 |
|---|---|
| 两台电脑互相发现 | 成功 |
| 三台电脑互相发现 | 成功 |
| 关闭客户端 | 其他客户端能检测离线 |
| 重新上线 | 能重新出现 |
| 心跳稳定性 | 不频繁误判离线 |

### 16.3 聊天功能

| 测试项 | 标准 |
|---|---|
| 私聊发送 | 对方能收到 |
| 私聊回复 | 本机能收到 |
| 消息显示 | 气泡方向正确 |
| 历史记录 | 重启后仍可查看 |
| 对方离线 | 发送失败有提示 |

### 16.4 广播功能

| 测试项 | 标准 |
|---|---|
| 全员广播 | 所有在线用户收到 |
| 广播样式 | 与私聊消息区分 |
| 广播记录 | 能保存 |

### 16.5 文件传输

| 测试项 | 标准 |
|---|---|
| 发送请求 | 接收方弹窗 |
| 接收文件 | 文件保存成功 |
| 拒绝文件 | 发送方有提示 |
| 进度显示 | 能显示百分比 |
| 大文件 | 至少支持 100MB 文件 |
| UI 响应 | 传输过程中不冻结 |

---

## 17. 后续增强功能规划

MVP 完成后，可以考虑加入：

1. 多文件传输。
2. 文件夹压缩发送。
3. 文件断点续传。
4. 文件 Hash 校验。
5. 聊天记录搜索。
6. 会话置顶。
7. 系统托盘。
8. 桌面通知。
9. 消息撤回。
10. 已读回执。
11. 临时群聊。
12. 永久群聊。
13. 群文件。
14. AES 消息加密。
15. 局域网跨网段手动添加 IP。
16. 导出聊天记录。
17. 主题切换和自定义主题色。
18. 开机自启动。

---

## 18. 给 Codex / AI Agent 的工作规则

在后续开发中，AI Agent 必须遵守以下规则：

1. 不要擅自扩大需求范围。
2. 优先完成 MVP 必须功能。
3. 每次修改前先理解现有项目结构。
4. 不要把网络代码写进 View 或 ViewModel。
5. 不要把 UI 代码写进 LanTalk.Network 或 LanTalk.Storage。
6. 新增类时放入正确项目和目录。
7. 所有网络任务必须支持 CancellationToken。
8. 所有文件传输必须使用流式处理。
9. 所有 JSON 序列化尽量使用 Source Generator。
10. ViewModel 优先使用 CommunityToolkit.Mvvm。
11. XAML 尽量使用编译绑定。
12. 不要引入重型依赖。
13. 不要引入需要复杂反射或动态代理的框架。
14. 每完成一个功能，要给出对应测试方式。
15. 修改数据库结构时，要同步更新初始化 SQL 和相关 Repository。
16. 如果功能可能影响 Native AOT，需要说明风险。
17. UI 优先保证简洁、清晰、稳定，不要过度动画。
18. 代码必须可编译，不要只写伪代码。

---

## 19. 推荐 Codex 开发提示词

### 19.1 初始化项目提示词

请根据本文档创建 LanTalk 项目骨架。要求使用 .NET 10、Avalonia UI、SukiUI、MVVM 架构，创建 LanTalk.App、LanTalk.Core、LanTalk.Network、LanTalk.Storage、LanTalk.Tests 项目。先完成可运行的主窗口，主窗口采用 Telegram 风格双栏布局，左侧显示个人信息、搜索框、在线用户列表假数据，右侧显示聊天标题、消息气泡假数据和输入栏。暂时不要实现真实网络通信。代码必须能编译运行。

### 19.2 UDP 自动发现提示词

请在现有项目基础上实现局域网 UDP 自动发现功能。要求在 LanTalk.Network 中实现 DiscoveryService、UdpDiscoveryServer 和 OnlineUserRegistry，支持 HELLO、ONLINE、HEARTBEAT、BYE 四种消息。启动后发送 HELLO，每 5 秒发送 HEARTBEAT，超过 15 秒未收到心跳则判断用户离线。LanTalk.App 的在线用户列表需要实时更新。注意网络层不要引用 Avalonia，UI 更新通过 ViewModel 处理。

### 19.3 TCP 私聊提示词

请实现 TCP 私聊消息功能。要求在 LanTalk.Network 中实现 TcpMessageServer、TcpMessageClient 和 MessageService，支持 PRIVATE_MESSAGE 消息。用户点击在线用户后可以发送文本消息，对方收到后在聊天界面显示。消息需要保存到 SQLite。请同时实现 ChatMessages 表、MessageRepository 和 ChatHistoryService。注意消息发送和接收不能阻塞 UI 线程。

### 19.4 广播消息提示词

请在现有私聊功能基础上实现广播消息。要求用户可以点击广播入口，向所有在线用户发送 BROADCAST_MESSAGE。接收方需要在 UI 中以广播样式显示，并保存到本地聊天记录。不要实现复杂永久群聊，只实现 MVP 需要的全员广播。

### 19.5 文件传输提示词

请实现单文件传输功能。要求发送方选择文件后向接收方发送 FILE_REQUEST，接收方弹出确认窗口，可以接收或拒绝。接收后通过 TCP 文件端口进行流式分块传输，显示文件名、文件大小、传输进度，传输完成后保存到设置中的文件接收目录，并记录到 FileTransfers 表。文件读取必须使用流式处理，不允许 File.ReadAllBytes。

### 19.6 Native AOT 优化提示词

请检查当前项目对 Native AOT 的兼容性。重点检查 JSON 序列化是否使用 Source Generator，是否存在不必要反射、Assembly.Load、动态 XAML、运行时类型扫描等问题。请给出需要修改的地方并逐步修复。不要为了 AOT 牺牲 MVP 功能稳定性。

---

## 20. 当前最推荐的第一步

当前阶段最适合先做：

1. 创建项目骨架。
2. 接入 Avalonia UI 和 SukiUI。
3. 实现 Telegram 风格主窗口静态界面。
4. 实现本机设置保存。
5. 再进入 UDP 自动发现。

不要一开始就同时做网络、文件传输、数据库和 AOT 发布，否则调试复杂度会迅速上升。

---

## 21. 项目最终汇报亮点表述

可以在课程汇报中这样描述：

本项目设计并实现了一款基于 C# 与 .NET 平台的局域网即时通信工具。系统采用 Avalonia UI 与 SukiUI 构建现代化桌面客户端界面，整体参考 Telegram 的双栏聊天布局。通信层通过 UDP 广播和心跳机制实现局域网用户自动发现，使用 TCP Socket 实现可靠的私聊消息、广播消息和文件传输。存储层基于 SQLite 保存聊天记录与文件传输记录，配置层使用 JSON 保存本机用户信息和系统设置。项目在架构设计上面向 Native AOT 发布进行优化，尽量减少反射和运行时动态行为，并通过异步 Socket、流式文件传输和轻量化 UI 模板提升运行效率。

---

## 22. 一句话目标

先做出一个“打开即用、自动发现、能聊天、能广播、能传文件、能保存记录、界面像 Telegram、运行轻量稳定”的 C# 局域网即时通信 MVP，然后再逐步扩展成更完整的内网通信工具。







另外保持对未来.net11新特性的兼容预留：

技术前瞻：net11-preview 
协议设计：先用 sealed record 模拟 union type
压缩设计：先预留 ICompressor，后续接 Zstd
性能目标：先做好异步、流式传输、UI 虚拟化、AOT 兼容

项目主线基于 .NET 10 LTS 保证稳定性，同时在架构上面向 .NET 11 的 Runtime Async、C# Union Type 和 Zstandard 做前瞻性设计，为后续高性能版本升级预留扩展空间。
