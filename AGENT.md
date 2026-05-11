# AGENT.md

> 本文件是 LanTalk 项目的 AI Agent / Codex 开发规范。  
> 所有自动生成、修改、重构代码的行为都必须遵守本文档。  
> 若本文档与临时对话指令冲突，以用户最新明确要求为准；若用户没有明确要求，不要擅自扩大功能范围。

---

## 1. 项目概述

LanTalk 是一个基于 C# / .NET 的局域网即时通信工具，目标是实现一个类似“内网通”“飞鸽传书”“Telegram 桌面端风格”的轻量级局域网聊天客户端。

项目第一阶段目标是完成 MVP，而不是一次性实现完整商业 IM。

MVP 应具备：

- 局域网用户自动发现
- 在线用户列表
- 一对一私聊
- 全员广播消息
- 单文件传输
- 本地聊天记录
- 基础设置
- Telegram 风格现代化 UI
- 高性能、低占用、面向 Native AOT 友好的代码结构

---

## 2. 技术栈

必须优先使用以下技术路线：

- 语言：C#
- 运行时：.NET 10
- UI 框架：Avalonia UI
- UI 主题：SukiUI
- 架构模式：MVVM
- MVVM 工具：CommunityToolkit.Mvvm
- 网络通信：UDP + TCP Socket
- 本地数据库：SQLite
- 配置文件：JSON
- 序列化：System.Text.Json Source Generator
- 发布目标：普通 Release 优先，后续支持 Native AOT

不要随意引入重型框架。除非用户明确要求，否则不要引入：

- Prism
- ReactiveUI 全家桶
- 大型 ORM
- 插件系统
- 复杂依赖注入框架
- 需要大量反射或运行时动态代理的库

---

## 3. 当前优先级

当前阶段优先完成 MVP。功能优先级如下：

### P0：必须完成

1. Avalonia + SukiUI 项目骨架
2. Telegram 风格主界面
3. 本机用户设置保存
4. UDP 局域网自动发现
5. 在线用户列表
6. TCP 私聊消息
7. SQLite 聊天记录
8. 广播消息
9. 单文件传输
10. 基础异常处理

### P1：推荐完成

1. 未读消息提醒
2. 文件传输进度条
3. 文件接收确认弹窗
4. 明暗主题切换
5. 简单日志
6. 文件传输记录
7. 最近会话列表

### P2：后续扩展

1. 永久群聊
2. 多文件传输
3. 文件夹压缩传输
4. 断点续传
5. 消息撤回
6. 已读回执
7. AES 加密
8. 聊天记录搜索
9. 系统托盘
10. 桌面通知
11. 开机自启
12. 跨网段手动添加 IP

在用户未明确要求前，不要优先实现 P2 功能。

---

## 4. 解决方案结构

推荐项目结构如下：

```text
LanTalk/
├── AGENT.md
├── README.md
├── LanTalk.sln
├── docs/
│   ├── project-spec.md
│   ├── protocol.md
│   ├── ui-design.md
│   └── test-plan.md
├── src/
│   ├── LanTalk.App/
│   ├── LanTalk.Core/
│   ├── LanTalk.Network/
│   └── LanTalk.Storage/
└── tests/
    └── LanTalk.Tests/
```

各项目职责：

```text
LanTalk.App       Avalonia UI、SukiUI、View、ViewModel、窗口、控件、交互
LanTalk.Core      核心模型、协议模型、枚举、常量、公共接口
LanTalk.Network   UDP 自动发现、TCP 消息、TCP 文件传输
LanTalk.Storage   SQLite、本地配置、聊天记录、文件传输记录
LanTalk.Tests     单元测试、序列化测试、仓储测试、基础协议测试
```

---

## 5. 模块边界规则

必须遵守以下边界：

### 5.1 LanTalk.App

可以引用：

- LanTalk.Core
- LanTalk.Network
- LanTalk.Storage
- Avalonia
- SukiUI
- CommunityToolkit.Mvvm

职责：

- UI 展示
- ViewModel
- 用户交互
- 弹窗
- 主题
- 页面导航
- 调用服务

禁止：

- 在 View 的 code-behind 中写复杂业务逻辑
- 在 XAML code-behind 中直接写 Socket 通信
- 在 UI 控件中直接访问 SQLite
- 在 ViewModel 中堆积底层网络细节

### 5.2 LanTalk.Core

可以被所有项目引用。

职责：

- 纯模型
- 枚举
- 协议数据结构
- 常量
- Source Generator 序列化上下文
- 不依赖 UI
- 不依赖数据库实现

禁止：

- 引用 Avalonia
- 引用 SukiUI
- 写 Socket 实现
- 写 SQLite 实现

### 5.3 LanTalk.Network

可以引用：

- LanTalk.Core

职责：

- UDP 广播
- UDP 心跳
- TCP 消息传输
- TCP 文件传输
- 在线用户注册表
- 网络异常处理

禁止：

- 引用 Avalonia
- 引用 SukiUI
- 直接操作 UI 控件
- 直接写数据库，除非通过抽象接口

### 5.4 LanTalk.Storage

可以引用：

- LanTalk.Core

职责：

- SQLite 初始化
- MessageRepository
- UserRepository
- FileTransferRepository
- SettingsService
- JSON 配置读写

禁止：

- 引用 Avalonia
- 引用 SukiUI
- 直接调用网络服务
- 直接操作 UI

---

## 6. UI 设计规范

整体 UI 参考 Telegram 桌面端。

### 6.1 主窗口布局

必须采用双栏布局：

```text
┌────────────────────────────────────────────┐
│ 顶部标题栏 / 自定义窗口栏                   │
├────────────────┬───────────────────────────┤
│ 左侧 Sidebar    │ 右侧 ChatPanel             │
│                │                           │
│ 我的信息         │ 当前会话标题                │
│ 搜索框           │ 消息列表                    │
│ 最近会话         │                           │
│ 在线用户         │ 输入框 + 发送按钮 + 文件按钮 │
│ 设置入口         │                           │
└────────────────┴───────────────────────────┘
```

### 6.2 风格要求

- 简洁、清爽、现代
- 默认蓝色主题
- 支持浅色 / 深色主题预留
- 自己消息靠右
- 对方消息靠左
- 广播消息与私聊消息样式区分
- 文件消息使用卡片样式
- 不要过度动画
- 不要大面积模糊
- 不要使用过多阴影
- 不要为了炫酷牺牲性能

### 6.3 Avalonia 编码要求

尽量使用：

- 编译绑定
- 明确的 `x:DataType`
- ViewModel 绑定
- 样式资源复用
- 简洁的 DataTemplate

避免：

- 动态加载 XAML
- 复杂反射绑定
- 每条消息使用复杂动画控件
- 一次性加载大量聊天记录控件

---

## 7. 网络协议设计

### 7.1 端口

默认端口：

```text
UDP 发现端口：50000
TCP 消息端口：50001
TCP 文件端口：50002
```

端口后续可以在设置中配置。MVP 阶段可以先固定。

### 7.2 UDP 包类型

用于局域网用户发现和在线状态维护：

```text
HELLO       上线广播
ONLINE      在线响应
HEARTBEAT   心跳
BYE         下线通知
```

### 7.3 TCP 包类型

用于可靠消息与控制信令：

```text
PRIVATE_MESSAGE     私聊消息
BROADCAST_MESSAGE   广播消息
FILE_REQUEST        文件发送请求
FILE_ACCEPT         接收文件
FILE_REJECT         拒绝文件
FILE_FINISHED       文件传输完成
ERROR               错误消息
```

### 7.4 用户发现流程

```text
客户端启动
↓
加载本机设置
↓
启动 UDP 监听
↓
发送 HELLO 广播
↓
收到其他客户端 HELLO
↓
加入在线用户列表
↓
回复 ONLINE
↓
定时发送 HEARTBEAT
↓
超过 15 秒未收到心跳，标记离线
↓
关闭时发送 BYE
```

### 7.5 私聊流程

```text
用户选择在线用户
↓
输入消息
↓
构造 PRIVATE_MESSAGE
↓
通过 TCP 发送
↓
接收方解析消息
↓
更新 UI
↓
保存聊天记录
```

### 7.6 广播流程

```text
用户点击广播入口
↓
输入广播内容
↓
遍历在线用户列表
↓
分别发送 BROADCAST_MESSAGE
↓
接收端显示广播消息
↓
保存聊天记录
```

### 7.7 文件传输流程

```text
发送方选择文件
↓
发送 FILE_REQUEST
↓
接收方弹出确认框
↓
接收方接受或拒绝
↓
接受后建立 TCP 文件传输
↓
发送方流式分块读取文件
↓
接收方流式写入文件
↓
更新进度
↓
完成后记录文件传输结果
```

---

## 8. 核心模型规范

### 8.1 UserInfo

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

### 8.2 NetworkPacket

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

### 8.3 ChatMessage

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

### 8.4 AppSettings

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

## 9. 数据存储规范

使用 SQLite 保存本地记录。

默认数据库位置建议：

```text
%AppData%/LanTalk/lantalk.db
```

### 9.1 KnownUsers

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

### 9.2 ChatMessages

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

### 9.3 FileTransfers

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

### 9.4 索引

```sql
CREATE INDEX IF NOT EXISTS IX_ChatMessages_SessionId_SendTime
ON ChatMessages(SessionId, SendTime);

CREATE INDEX IF NOT EXISTS IX_FileTransfers_TransferTime
ON FileTransfers(TransferTime);
```

---

## 10. Native AOT 兼容规范

项目需要面向 Native AOT 友好设计，但开发阶段不要求始终使用 Native AOT 运行。

### 10.1 必须遵守

- 使用 System.Text.Json Source Generator
- ViewModel 使用 CommunityToolkit.Mvvm Source Generator
- 避免反射扫描自动注册
- 避免 Assembly.Load
- 避免动态 XAML
- 避免运行时插件系统
- 避免复杂动态代理库
- 尽量使用编译绑定
- 保持依赖简单

### 10.2 JSON 序列化

必须优先使用 Source Generator，例如：

```csharp
using System.Text.Json.Serialization;

[JsonSerializable(typeof(NetworkPacket))]
[JsonSerializable(typeof(UserInfo))]
[JsonSerializable(typeof(ChatMessage))]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(FileTransferRequest))]
public partial class LanTalkJsonContext : JsonSerializerContext
{
}
```

序列化示例：

```csharp
var json = JsonSerializer.Serialize(packet, LanTalkJsonContext.Default.NetworkPacket);
```

反序列化示例：

```csharp
var packet = JsonSerializer.Deserialize(json, LanTalkJsonContext.Default.NetworkPacket);
```

### 10.3 发布策略

开发阶段：

```bash
dotnet run --project src/LanTalk.App
```

普通发布：

```bash
dotnet publish src/LanTalk.App -c Release -r win-x64
```

Native AOT 发布在 MVP 基本稳定后再执行。若发现 AOT 警告，不要为了消除警告破坏核心功能稳定性，应逐步修复。

---

## 11. 性能规范

### 11.1 UI 性能

必须：

- 聊天记录分页加载
- 打开会话默认只加载最近 50 条消息
- 避免一次性创建大量消息控件
- 避免给每条消息使用复杂动画
- 避免频繁刷新整个用户列表
- 网络事件进入 UI 前应进行必要节流

建议：

- 消息列表使用虚拟化控件
- 在线用户变化时只更新变化项
- 心跳更新不触发完整 UI 重绘

### 11.2 网络性能

必须：

- 所有网络操作异步执行
- 不阻塞 UI 线程
- 所有长时间运行任务支持 CancellationToken
- Socket 异常必须捕获
- 对方离线时要有提示

### 11.3 文件传输性能

禁止：

```csharp
File.ReadAllBytes(path)
```

必须使用流式分块传输：

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

推荐缓冲区：

```text
64KB 或 256KB
```

---

## 12. 编码规范

### 12.1 通用规范

- 开启 Nullable
- 开启 ImplicitUsings
- 使用 async / await
- 不写阻塞式 Thread.Sleep
- 不在 UI 线程做网络或文件 IO
- 不在 UI 线程做大量数据库操作
- 不吞异常
- 不写无法编译的伪代码
- 修改后尽量保证项目能 build
- 新增功能必须说明测试方式

### 12.2 命名规范

类名：

```text
UserInfo
ChatMessage
NetworkPacket
DiscoveryService
MessageService
FileTransferService
MessageRepository
SettingsService
MainViewModel
ChatViewModel
```

枚举：

```text
PacketType
MessageKind
UserStatus
FileTransferStatus
```

方法名使用英文动词短语：

```text
StartAsync
StopAsync
SendMessageAsync
BroadcastAsync
SaveMessageAsync
LoadRecentMessagesAsync
```

UI 显示文本使用中文。

### 12.3 异常处理

必须处理：

- 端口占用
- UDP 广播失败
- TCP 连接失败
- 对方离线
- 文件不存在
- 文件保存失败
- 文件传输中断
- JSON 解析失败
- SQLite 初始化失败
- 配置文件损坏

### 12.4 日志

MVP 至少记录：

- 程序启动
- 程序关闭
- 本机配置加载
- 用户上线
- 用户离线
- 消息发送
- 消息接收
- 文件传输开始
- 文件传输完成
- 文件传输失败
- 网络异常
- 数据库异常

---

## 13. 开发步骤

### 阶段 1：项目初始化与静态 UI

任务：

1. 创建解决方案
2. 创建四个主要项目
3. 接入 Avalonia UI
4. 接入 SukiUI
5. 创建主窗口
6. 实现 Telegram 风格双栏布局
7. 添加假数据用户列表
8. 添加假数据消息气泡
9. 添加设置窗口 UI

验收：

- 项目可编译
- 软件可启动
- UI 看起来像聊天软件
- 暂不需要真实网络通信

### 阶段 2：设置与本机身份

任务：

1. 实现 AppSettings
2. 实现 SettingsService
3. 首次启动生成 UserId
4. 首次启动设置昵称
5. 保存文件接收目录
6. 重启后恢复设置

验收：

- 昵称能保存
- UserId 不重复生成
- 设置文件可读写

### 阶段 3：UDP 自动发现

任务：

1. 实现 UdpDiscoveryServer
2. 实现 DiscoveryService
3. 发送 HELLO
4. 回复 ONLINE
5. 定时发送 HEARTBEAT
6. 处理 BYE
7. 维护 OnlineUserRegistry
8. UI 显示真实在线用户

验收：

- 两台电脑互相发现
- 关闭客户端后能离线
- 重新上线能恢复

### 阶段 4：TCP 私聊

任务：

1. 实现 TcpMessageServer
2. 实现 TcpMessageClient
3. 实现 MessageService
4. 发送 PRIVATE_MESSAGE
5. 接收 PRIVATE_MESSAGE
6. UI 显示消息
7. SQLite 保存消息
8. 打开会话加载最近消息

验收：

- 两台电脑可互发文本
- 消息方向正确
- 重启后消息仍在

### 阶段 5：广播消息

任务：

1. 添加广播入口
2. 构造 BROADCAST_MESSAGE
3. 遍历在线用户发送
4. 接收端显示广播消息
5. 保存广播记录

验收：

- A 发广播，B/C 同时收到
- 广播样式与私聊区分

### 阶段 6：文件传输

任务：

1. 发送 FILE_REQUEST
2. 接收方确认或拒绝
3. 发送 FILE_ACCEPT / FILE_REJECT
4. TCP 流式传输文件
5. 显示进度
6. 保存文件
7. 记录传输结果

验收：

- A 给 B 发送文件
- B 可接收或拒绝
- 传输时 UI 不冻结
- 100MB 文件可传输

### 阶段 7：打磨与发布

任务：

1. 优化 UI
2. 增加日志
3. 完善异常提示
4. 编写 README
5. 编写测试说明
6. 普通 Release 发布
7. 尝试 Native AOT 发布

验收：

- 多台电脑稳定演示
- UI 无明显卡顿
- 没有未处理异常
- 演示流程完整

---

## 14. 测试要求

每个功能完成后必须提供测试方式。

### 14.1 基础测试

- 软件能否启动
- 设置能否保存
- 重启后配置是否恢复
- 端口被占用时是否提示

### 14.2 自动发现测试

- 两台电脑互相发现
- 三台电脑互相发现
- 客户端关闭后离线
- 客户端重新上线
- 心跳是否误判

### 14.3 聊天测试

- A 发给 B
- B 回复 A
- 对方离线时发送
- 重启后加载历史记录
- 未选择会话时收到消息

### 14.4 广播测试

- A 广播给所有在线用户
- 广播消息保存
- 广播消息样式正确

### 14.5 文件传输测试

- 小文件传输
- 100MB 文件传输
- 接收方拒绝
- 传输中断
- 保存路径不存在
- UI 是否冻结

---

## 15. 禁止事项

除非用户明确要求，否则不要做以下事情：

1. 不要引入服务端。
2. 不要引入登录注册。
3. 不要引入云同步。
4. 不要优先做永久群聊。
5. 不要优先做断点续传。
6. 不要优先做消息加密。
7. 不要重写整个项目结构。
8. 不要把业务逻辑塞进 XAML code-behind。
9. 不要让网络层依赖 Avalonia。
10. 不要让存储层依赖 Avalonia。
11. 不要用 File.ReadAllBytes 传大文件。
12. 不要使用阻塞式 Socket 导致 UI 卡死。
13. 不要为了 UI 动画牺牲性能。
14. 不要写无法编译的伪代码。
15. 不要引入过多第三方库。
16. 不要在没有说明的情况下删除已有功能。
17. 不要擅自改变用户已确定的技术栈。

---

## 16. 推荐给 Agent 的默认工作方式

每次执行任务时，请按以下方式工作：

1. 先阅读本 AGENT.md。
2. 再阅读 README.md 和 docs 中的相关文档。
3. 检查当前项目结构。
4. 明确本次任务属于哪个阶段。
5. 只修改与任务相关的文件。
6. 保持模块边界清晰。
7. 编写可编译代码。
8. 给出运行或测试方式。
9. 若引入新依赖，说明理由。
10. 若存在 Native AOT 风险，明确说明。

---

## 17. 初始化项目时的建议命令

```bash
dotnet new sln -n LanTalk

dotnet new avalonia.app -o src/LanTalk.App
dotnet new classlib -o src/LanTalk.Core
dotnet new classlib -o src/LanTalk.Network
dotnet new classlib -o src/LanTalk.Storage
dotnet new xunit -o tests/LanTalk.Tests

dotnet sln add src/LanTalk.App/LanTalk.App.csproj
dotnet sln add src/LanTalk.Core/LanTalk.Core.csproj
dotnet sln add src/LanTalk.Network/LanTalk.Network.csproj
dotnet sln add src/LanTalk.Storage/LanTalk.Storage.csproj
dotnet sln add tests/LanTalk.Tests/LanTalk.Tests.csproj

dotnet add src/LanTalk.App reference src/LanTalk.Core
dotnet add src/LanTalk.App reference src/LanTalk.Network
dotnet add src/LanTalk.App reference src/LanTalk.Storage
dotnet add src/LanTalk.Network reference src/LanTalk.Core
dotnet add src/LanTalk.Storage reference src/LanTalk.Core
dotnet add tests/LanTalk.Tests reference src/LanTalk.Core
dotnet add tests/LanTalk.Tests reference src/LanTalk.Storage
```

常用包：

```bash
dotnet add src/LanTalk.App package SukiUI
dotnet add src/LanTalk.App package CommunityToolkit.Mvvm
dotnet add src/LanTalk.Storage package Microsoft.Data.Sqlite
```

---

## 18. README 中应体现的项目介绍

README 应至少包括：

1. 项目简介
2. 技术栈
3. 功能列表
4. 项目结构
5. 运行方式
6. 开发计划
7. 演示流程
8. 常见问题
9. 小组分工

---

## 19. 课堂汇报亮点

汇报中应突出：

1. 使用 Avalonia UI 构建现代桌面客户端。
2. 使用 SukiUI 实现 Telegram 风格界面。
3. 使用 UDP 广播实现局域网用户自动发现。
4. 使用心跳机制维护在线状态。
5. 使用 TCP 实现可靠聊天和文件传输。
6. 使用 SQLite 保存本地聊天记录。
7. 使用流式文件传输降低内存占用。
8. 面向 Native AOT 进行轻量化发布设计。
9. 软件无需登录注册，打开即用。
10. 适用于教室、实验室、小型办公室等局域网环境。

---

## 20. 一句话目标

LanTalk 的第一阶段目标是：

> 做出一个打开即用、自动发现、能私聊、能广播、能传文件、能保存记录、界面像 Telegram、运行轻量稳定的 C# 局域网即时通信 MVP。
