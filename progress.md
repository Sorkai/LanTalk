# 进度日志

## 会话：2026-05-29

### 阶段 0：规划与需求分析
- **状态：** 已完成
- **开始时间：** 2026-05-29
- 已执行操作：
  - 加载并遵循 `planning-with-files` 技能。
  - 执行会话恢复检查；未发现上一轮未同步的计划上下文。
  - 在记忆中搜索 LanTalk / Avalonia / SukiUI / UDP / TCP / SQLite / AOT 相关上下文；未发现相关历史记忆。
  - 阅读 `AGENTS.md`。
  - 阅读 `lan_talk_codex项目说明文档.md`。
  - 检查仓库根目录，确认尚无源码解决方案或项目。
  - 创建项目持久化计划文件。
  - 将用户分阶段提示词映射为从项目骨架/UI 到 Native AOT 检查的实施计划。
  - 将 `task_plan.md`、`findings.md`、`progress.md` 翻译并改写为简体中文。
  - 新增记忆更新说明，记录用户偏好：后续文档、开发界面文案、问题回复默认使用简体中文。
- 创建/修改的文件：
  - `task_plan.md`（已创建，已中文化）。
  - `findings.md`（已创建，已中文化）。
  - `progress.md`（已创建，已中文化）。
  - `C:\Users\wk\.codex\memories\extensions\ad_hoc\notes\2026-05-29-013000-simplified-chinese-preference.md`（已创建）。

### 阶段 1：项目骨架与 Telegram 风格静态 UI
- **状态：** 已完成
- 已执行操作：
  - 检查 .NET SDK，确认本机安装 `.NET SDK 10.0.300`。
  - 安装 Avalonia 模板 `Avalonia.Templates@12.0.4`。
  - 创建 `LanTalk.slnx`，随后按项目规范补建传统 `LanTalk.sln`。
  - 创建 `LanTalk.App`、`LanTalk.Core`、`LanTalk.Network`、`LanTalk.Storage`、`LanTalk.Tests`。
  - 配置项目引用：App 引用 Core/Network/Storage；Network 和 Storage 引用 Core；Tests 引用 Core/Storage/Network。
  - 添加 NuGet 包：`SukiUI 7.0.1`、`CommunityToolkit.Mvvm 8.4.2`、`Microsoft.Data.Sqlite 10.0.8`。
  - 实现 Core 模型、枚举、常量、JSON Source Generator、日志抽象、`ICompressor` 预留。
  - 实现 Storage 的 JSON 设置服务、SQLite 初始化、消息仓储、文件传输仓储、聊天历史服务。
  - 实现 Network 的 UDP 自动发现骨架、在线用户注册表、TCP 消息骨架、文件流式复制服务。
  - 将默认 Avalonia 欢迎页替换为 Telegram 风格中文双栏主界面。
  - 添加假数据用户、最近会话、消息气泡、广播样式、文件卡片、输入栏和设置侧栏。
  - 创建 README 与 docs 基础文档。
  - 执行 `dotnet build LanTalk.sln -v:minimal`，结果 0 警告 0 错误。
  - 执行 `dotnet test LanTalk.sln -v:minimal`，结果 3 个测试全部通过。
  - 执行 `dotnet run --project src/LanTalk.App/LanTalk.App.csproj` 冒烟启动，确认 `LanTalk.App` 窗口进程可启动；随后结束进程。
- 创建/修改的文件：
  - `LanTalk.sln`、`LanTalk.slnx`。
  - `src/LanTalk.App/**`。
  - `src/LanTalk.Core/**`。
  - `src/LanTalk.Network/**`。
  - `src/LanTalk.Storage/**`。
  - `tests/LanTalk.Tests/**`。
  - `docs/project-spec.md`。
  - `docs/protocol.md`。
  - `docs/ui-design.md`。
  - `docs/test-plan.md`。
  - `README.md`。

### 阶段 2：本机设置与身份
- **状态：** 已完成
- 已执行操作：
  - `AppSettings` 已在 Core 中实现。
  - `SettingsService` 已使用 JSON 与 `LanTalkJsonContext` Source Generator。
  - 首次启动会生成稳定 `UserId`、默认昵称和文件接收目录。
  - UI 设置面板支持修改昵称、文件目录、保存历史、主题模式、主题色。
  - 设置服务支持配置损坏备份和默认配置重建。
  - 设置服务支持注入配置文件路径，测试不再污染真实 `%AppData%`。
  - 添加 `UserId` 稳定性、设置字段保存、损坏配置恢复测试。
  - 执行 `dotnet build LanTalk.sln -v:minimal`，0 警告 0 错误。
  - 执行 `dotnet test LanTalk.sln -v:minimal`，5 个测试全部通过。
- 创建/修改的文件：
  - `src/LanTalk.Core/Models/AppSettings.cs`。
  - `src/LanTalk.Storage/Settings/SettingsService.cs`。
  - `src/LanTalk.App/ViewModels/SettingsViewModel.cs`。
  - `src/LanTalk.App/ViewModels/MainWindowViewModel.cs`。
  - `tests/LanTalk.Tests/SettingsServiceTests.cs`。

### 阶段 3：UDP 自动发现与在线用户注册表
- **状态：** 已完成
- 已执行操作：
  - `DiscoveryService` 注入 App ViewModel。
  - App 初始化后启动 UDP 监听并发送 HELLO。
  - 监听 ONLINE/HEARTBEAT/BYE 并维护 `OnlineUserRegistry`。
  - 在线用户变化通过 `Dispatcher.UIThread.Post` 切回 UI 线程更新列表。
  - 窗口关闭时调用 `ShutdownAsync`，发送 BYE 并停止发现服务。
  - 添加 `OnlineUserRegistryTests`，覆盖用户加入与超时离线。
  - 执行 `dotnet build LanTalk.sln -v:minimal`，0 警告 0 错误。
  - 执行 `dotnet test LanTalk.sln -v:minimal`，7 个测试全部通过。
  - 执行 App 启动冒烟，确认 UDP 监听不会导致启动期崩溃，随后结束窗口进程。
- 创建/修改的文件：
  - `src/LanTalk.Network/Discovery/DiscoveryService.cs`。
  - `src/LanTalk.Network/Discovery/OnlineUserRegistry.cs`。
  - `src/LanTalk.Network/Discovery/UdpDiscoveryServer.cs`。
  - `src/LanTalk.App/ViewModels/MainWindowViewModel.cs`。
  - `src/LanTalk.App/Views/MainWindow.axaml.cs`。
  - `tests/LanTalk.Tests/OnlineUserRegistryTests.cs`。

### 阶段 4：SQLite 存储与 TCP 私聊
- **状态：** 已完成
- 已执行操作：
  - App 启动时启动 `TcpMessageServer`，关闭时停止消息服务。
  - `MessageService` 增加 `PacketReceived` 事件，并支持 `PRIVATE_MESSAGE` 接收。
  - 发送按钮对私聊会话调用 `SendPrivateMessageAsync`，失败时显示“已保存但发送失败”。
  - 收到私聊消息后解析 `TextMessagePayload`，保存 SQLite，并根据当前会话显示或增加未读。
  - 打开会话时通过 `ChatHistoryService` 加载最近历史；无历史时显示阶段提示。
  - 在线用户 ViewModel 保留真实 MessagePort/FilePort。
  - 添加 TCP 回环测试 `MessageService_ShouldReceivePrivateMessageOverTcp`。
  - 执行 `dotnet build LanTalk.sln -v:minimal`，0 警告 0 错误。
  - 执行 `dotnet test LanTalk.sln -v:minimal`，8 个测试全部通过。
- 创建/修改的文件：
  - `src/LanTalk.Network/Messaging/MessageService.cs`。
  - `src/LanTalk.App/ViewModels/MainWindowViewModel.cs`。
  - `src/LanTalk.App/ViewModels/OnlineUserViewModel.cs`。
  - `tests/LanTalk.Tests/MessageServiceTests.cs`。

### 阶段 5：广播消息
- **状态：** 已完成
- 已执行操作：
  - 侧栏已提供“全员广播”入口。
  - `MessageService.BroadcastAsync` 使用 `BROADCAST_MESSAGE` 并遍历在线用户，排除自己。
  - UI 广播会话使用独立样式和稳定会话 ID `broadcast`。
  - 广播消息保存到 SQLite；打开广播会话加载历史。
  - 广播发送返回 `BroadcastSendResult`，UI 展示成功/失败人数。
  - 添加 `BroadcastTests` 覆盖部分失败统计。
  - 执行 `dotnet build LanTalk.sln -v:minimal`，0 警告 0 错误。
  - 执行 `dotnet test LanTalk.sln -v:minimal`，9 个测试全部通过。
- 创建/修改的文件：
  - `src/LanTalk.Core/Models/BroadcastSendResult.cs`。
  - `src/LanTalk.Core/Serialization/LanTalkJsonContext.cs`。
  - `src/LanTalk.Network/Messaging/MessageService.cs`。
  - `src/LanTalk.App/ViewModels/MainWindowViewModel.cs`。
  - `tests/LanTalk.Tests/BroadcastTests.cs`。

### 阶段 6：单文件传输
- **状态：** 已完成
- 已执行操作：
  - 实现 `TcpFileClient` / `TcpFileServer`，使用 64KB 缓冲区和 `ArrayPool` 流式传输。
  - `MessageService` 增加 `FILE_REQUEST`、`FILE_ACCEPT`、`FILE_REJECT` 控制消息发送。
  - App 文件按钮接入 Avalonia 文件选择器。
  - 发送文件时保存 pending 记录并向接收方发送 FILE_REQUEST。
  - 接收方显示确认弹窗，可接收或拒绝。
  - 接受后发送 FILE_ACCEPT，发送方开始 TCP 文件流式传输。
  - 接收端 TCP 文件服务保存到设置中的文件目录，并更新进度。
  - 发送方和接收方都将传输状态保存到 `FileTransfers`。
  - 添加文件传输 TCP 回环测试和文件传输仓储测试。
  - 执行 `dotnet build LanTalk.sln -v:minimal`，0 警告 0 错误。
  - 执行 `dotnet test LanTalk.sln -v:minimal`，11 个测试全部通过。
  - 执行 App 启动冒烟，确认 UDP/TCP 消息/TCP 文件三个默认端口可启动。
- 创建/修改的文件：
  - `src/LanTalk.Core/Models/FileTransferResponse.cs`。
  - `src/LanTalk.Network/Files/TcpFileClient.cs`。
  - `src/LanTalk.Network/Files/TcpFileServer.cs`。
  - `src/LanTalk.Network/Files/FileTransferService.cs`。
  - `src/LanTalk.Network/Messaging/MessageService.cs`。
  - `src/LanTalk.App/ViewModels/MainWindowViewModel.cs`。
  - `src/LanTalk.App/ViewModels/FileReceiveRequestViewModel.cs`。
  - `src/LanTalk.App/ViewModels/ChatMessageViewModel.cs`。
  - `src/LanTalk.App/Views/MainWindow.axaml`。
  - `tests/LanTalk.Tests/FileTransferTests.cs`。
  - `tests/LanTalk.Tests/FileTransferRepositoryTests.cs`。

### 阶段 7：异常处理、日志、UI 打磨与文档
- **状态：** 已完成
- 已执行操作：
  - 增加基础日志接口与控制台日志实现。
  - 网络、设置、SQLite、文件不存在、发送失败等路径均有基础异常处理和 UI 状态提示。
  - UI 文案保持简体中文。
  - 消息气泡、广播消息、文件卡片、进度和设置面板已打磨到 MVP 可演示状态。
  - README 已补充项目简介、技术栈、功能列表、运行/测试/发布方式、演示流程、常见问题、小组分工占位。
  - docs 已补充项目规格、协议、UI 设计和测试计划。
  - 执行最终 `dotnet build LanTalk.sln -v:minimal`，0 警告 0 错误。
  - 执行最终 `dotnet test LanTalk.sln -v:minimal`，11 个测试全部通过。
- 创建/修改的文件：
  - `README.md`。
  - `docs/project-spec.md`。
  - `docs/protocol.md`。
  - `docs/ui-design.md`。
  - `docs/test-plan.md`。

### 阶段 8：Native AOT 兼容性检查与发布验证
- **状态：** 已完成
- 已执行操作：
  - 使用 `rg` 扫描 `Assembly.Load`、`GetTypes`、`Activator.CreateInstance`、阻塞式等待、`File.ReadAllBytes` 等风险点。
  - 移除 Avalonia 模板默认 `ViewLocator` 中的 `Type.GetType` / `Activator.CreateInstance` 反射创建逻辑。
  - 确认 JSON 序列化均使用 `LanTalkJsonContext` Source Generator。
  - 执行普通 Release 发布：`dotnet publish src/LanTalk.App/LanTalk.App.csproj -c Release -r win-x64 --self-contained false -v:minimal`，成功。
  - 2026-05-29 安装 Windows linker 后重新执行 Native AOT 发布：`dotnet publish src/LanTalk.App/LanTalk.App.csproj -c Release -r win-x64 -p:PublishAot=true -v:minimal`，发布成功，输出到 `src/LanTalk.App/bin/Release/net10.0/win-x64/publish/`。
  - Native AOT 发布仍出现 Avalonia DataGrid 的 trim/AOT 分析警告，暂不阻塞产物生成，后续 UI 深度打磨时继续跟踪。
- 创建/修改的文件：
  - `src/LanTalk.App/ViewLocator.cs`。
- 创建/修改的文件：
  - 暂无。

## 测试结果
| 测试 | 输入 | 预期 | 实际 | 状态 |
|------|------|------|------|------|
| 仓库检查 | `rg --files` | 确认现有项目文件 | 仅发现文档/配置文件，没有解决方案或源码项目 | 通过 |
| 计划文件创建 | 创建 `task_plan.md`、`findings.md`、`progress.md` | 项目根目录存在持久化计划文件 | 文件已创建 | 通过 |
| 计划文件中文化 | 覆盖三份计划文件 | 计划、发现、进度均改为简体中文 | 已完成 | 通过 |
| 阶段 1 构建 | `dotnet build LanTalk.sln -v:minimal` | 解决方案可编译 | 0 警告 0 错误 | 通过 |
| 阶段 1 测试 | `dotnet test LanTalk.sln -v:minimal` | 基础测试通过 | 3 个测试全部通过 | 通过 |
| 阶段 1 启动冒烟 | `dotnet run --project src/LanTalk.App/LanTalk.App.csproj` | 应用窗口可启动，无 XAML 加载崩溃 | `LanTalk.App` 窗口进程已启动，随后手动结束 | 通过 |
| 阶段 2 构建 | `dotnet build LanTalk.sln -v:minimal` | 设置功能集成后仍可编译 | 0 警告 0 错误 | 通过 |
| 阶段 2 测试 | `dotnet test LanTalk.sln -v:minimal` | 设置相关测试通过 | 5 个测试全部通过 | 通过 |
| 阶段 3 构建 | `dotnet build LanTalk.sln -v:minimal` | UDP 自动发现接入后仍可编译 | 0 警告 0 错误 | 通过 |
| 阶段 3 测试 | `dotnet test LanTalk.sln -v:minimal` | 注册表/序列化/设置测试通过 | 7 个测试全部通过 | 通过 |
| 阶段 3 启动冒烟 | `dotnet run --project src/LanTalk.App/LanTalk.App.csproj` | App 启动并打开 UDP 端口无崩溃 | 窗口进程已启动，随后结束 | 通过 |
| 阶段 4 构建 | `dotnet build LanTalk.sln -v:minimal` | TCP 私聊接入后仍可编译 | 0 警告 0 错误 | 通过 |
| 阶段 4 测试 | `dotnet test LanTalk.sln -v:minimal` | TCP 回环和基础测试通过 | 8 个测试全部通过 | 通过 |
| 阶段 5 构建 | `dotnet build LanTalk.sln -v:minimal` | 广播结果统计接入后仍可编译 | 0 警告 0 错误 | 通过 |
| 阶段 5 测试 | `dotnet test LanTalk.sln -v:minimal` | 广播和基础测试通过 | 9 个测试全部通过 | 通过 |
| 阶段 6 构建 | `dotnet build LanTalk.sln -v:minimal` | 文件传输接入后仍可编译 | 0 警告 0 错误 | 通过 |
| 阶段 6 测试 | `dotnet test LanTalk.sln -v:minimal` | 文件传输和仓储测试通过 | 11 个测试全部通过 | 通过 |
| 阶段 6 启动冒烟 | `dotnet run --project src/LanTalk.App/LanTalk.App.csproj` | UDP/TCP 消息/TCP 文件监听启动无冲突 | 窗口进程已启动，随后结束 | 通过 |
| 最终构建 | `dotnet build LanTalk.sln -v:minimal` | 全项目可编译 | 0 警告 0 错误 | 通过 |
| 最终测试 | `dotnet test LanTalk.sln -v:minimal` | 全部测试通过 | 11 个测试全部通过 | 通过 |
| 普通 Release 发布 | `dotnet publish src/LanTalk.App/LanTalk.App.csproj -c Release -r win-x64 --self-contained false -v:minimal` | 生成 win-x64 发布目录 | 发布成功，输出到 `src/LanTalk.App/bin/Release/net10.0/win-x64/publish/` | 通过 |
| Native AOT 发布 | `dotnet publish src/LanTalk.App/LanTalk.App.csproj -c Release -r win-x64 -p:PublishAot=true -v:minimal` | 生成 AOT 发布产物，警告可记录 | 发布成功；Avalonia DataGrid 输出 trim/AOT 分析警告 | 通过，需跟踪警告 |

## 错误日志
| 时间 | 错误 | 尝试次数 | 解决方式 |
|------|------|----------|----------|
| 2026-05-29 | `dotnet sln LanTalk.sln add` 初次失败，找不到 `LanTalk.sln` | 1 | .NET 10 默认生成 `.slnx`；补建传统 `LanTalk.sln` 后成功加入项目。 |
| 2026-05-29 | App 构建缺少 `Task` / `Type`，并触发 MVVMTK0007 | 1 | App 项目增加 `<ImplicitUsings>enable</ImplicitUsings>`。 |
| 2026-05-29 | 数据库初始化测试删除临时 SQLite 文件失败 | 1 | 调用 `SqliteConnection.ClearAllPools()` 后删除临时数据库。 |
| 2026-05-29 | Native AOT 发布失败：Platform linker not found | 1 | 已安装 Windows linker 后重新验证通过。 |
| 2026-05-29 | Avalonia DataGrid 在 Native AOT 发布时输出 trim/AOT 分析警告 | 1 | 暂不阻塞发布；记录为第三方控件 AOT 风险，后续继续观察或替换控件。 |

## 5 个恢复检查问题
| 问题 | 答案 |
|------|------|
| 我现在在哪里？ | 阶段 1 到阶段 8 已完成；LanTalk MVP 已可构建、测试、普通 Release 发布和 Native AOT 发布。 |
| 我要去哪里？ | 分阶段构建 LanTalk MVP：骨架/UI、设置、UDP 自动发现、TCP 私聊、广播、文件传输、打磨文档、AOT 检查。 |
| 目标是什么？ | 构建一个可运行的 .NET 10 Avalonia/SukiUI 局域网 IM MVP，具备设置、发现、聊天、广播、文件传输、SQLite 历史和 AOT 友好结构。 |
| 我学到了什么？ | 仓库当前只有项目文档，实现必须从项目初始化开始；详细内容见 `findings.md`。 |
| 我做了什么？ | 完成 LanTalk MVP：设置、UDP 自动发现、TCP 私聊、广播、单文件流式传输、SQLite 记录、文档、测试、普通 Release 发布；安装 Windows linker 后 Native AOT 发布已通过，剩余 Avalonia DataGrid trim/AOT 警告待跟踪。 |

## 会话：2026-05-30

### 阶段 9：通信产品化补齐与真实会话整理
- **状态：** 已完成
- 本轮目标：
  - 清理 UI 假数据和旧阶段文案。
  - 建立真实最近会话更新机制。
  - 修复消息显示昵称。
  - 实现刷新重新发现。
  - 增加 `KnownUsers` 持久化。
  - 补齐 `FILE_FINISHED` / `ERROR` 控制消息链路。
- 已执行操作：
  - 移除运行时假用户、假消息和旧阶段提示文案，保留真实空状态提示。
  - “刷新”按钮接入 `RefreshDiscoveryCommand`，调用 UDP 发现服务重新广播 `HELLO`。
  - 发现用户、选择用户、收到私聊/广播/文件请求时会维护 `RecentSessions`。
  - 历史消息和实时消息显示昵称，不再默认把非本机消息显示为 `UserId`。
  - 新增 `UserRepository`，将发现到的用户写入 `KnownUsers`，启动时恢复已知联系人。
  - 新增 `FileTransferFinished` 控制载荷，并补齐 `SendFileFinishedAsync` / `SendErrorAsync`。
  - 接收端文件完成后向发送端回传 `FILE_FINISHED`，失败时可通过 `ERROR` 同步文件状态。
  - 新增 `UserRepositoryTests` 与 `MessageService_ShouldSendFileFinishedAndErrorPackets`。
  - 将设置面板的主题下拉项改为真实字符串，避免保存成 `Avalonia.Controls.ComboBoxItem`。
  - 执行 `dotnet build LanTalk.sln -v:minimal`，0 警告 0 错误。
  - 执行 `dotnet test LanTalk.sln -v:minimal`，13 个测试全部通过。
  - 启动 Debug 版应用并截图检查，确认不再显示假用户和旧阶段提示。
  - 执行 Native AOT 发布：`dotnet publish src/LanTalk.App/LanTalk.App.csproj -c Release -r win-x64 -p:PublishAot=true -v:minimal`，发布成功；仍有 Avalonia DataGrid 既有 trim/AOT 分析警告。
- 后续保留待办：
  - 双机/三机真实验收。
  - 100MB 文件传输、传输中断、保存路径异常等实机验收。
  - 设置端口可修改并重启后生效。
  - 更新课堂演示文档和最终验收矩阵。

### 阶段 10：主题模式切换修复
- **状态：** 已完成
- 本轮目标：
  - 修复设置中选择 `Dark` / `Light` / `System` 后界面仍保持浅色的问题。
  - 继续保证浅色、深色下文字和按钮可读。
- 已执行操作：
  - 将 `App.axaml` 的 `RequestedThemeVariant` 从固定 `Light` 改为 `Default`。
  - 新增 `AppThemeService`，在启动加载设置和保存设置后应用 Avalonia 主题与 LanTalk 自定义色板。
  - 将侧栏、主聊天区、设置面板、头像底色、广播提示、文件卡片等自定义颜色接入深浅色主题资源。
  - 修复普通按钮在深色悬停/按下状态下前景色可能变黑的问题。
  - 在 `SettingsService` 中规范化主题配置，避免旧配置再次保存成 `Avalonia.Controls.ComboBoxItem` 等非法值。
  - 新增主题配置规范化单元测试。
- 验证结果：
  - `dotnet build LanTalk.sln -v:minimal`：0 警告，0 错误。
  - `dotnet test LanTalk.sln -v:minimal`：14 个测试全部通过。
  - 使用当前 `themeMode: Dark` 启动 Debug 版应用并截图，确认主界面与设置面板均切换为深色，文字、输入框、下拉框、按钮可读。
  - `dotnet publish src/LanTalk.App/LanTalk.App.csproj -c Release -r win-x64 -p:PublishAot=true -v:minimal`：发布成功；仍仅保留既有 Avalonia DataGrid trim/AOT 分析警告。

### 阶段 11-15：下一步开发计划
- **状态：** 已规划，待开始
- 本轮目标：
  - 将主题修复后的下一阶段开发路线拆成可执行计划。
  - 对齐 `task_plan.md` 中已经完成的阶段 10，并补齐后续阶段。
- 已执行操作：
  - 将当前阶段更新为“阶段 11 - 多机真实验收与稳定性打磨”。
  - 新增阶段 11：双机/三机真实验收与问题闭环。
  - 新增阶段 12：设置增强与端口可配置。
  - 新增阶段 13：网络与文件传输稳定性打磨。
  - 新增阶段 14：验收文档、演示脚本与发布包。
  - 新增阶段 15：后续 Backlog 收敛。
  - 将“立即下一步”更新为先做真实多机验收，优先处理 P0/P1 问题。
- 当前工作区状态：
  - 规划前 `main` 与 `origin/main` 同步，工作区干净。
  - 本轮仅修改规划/发现/进度文档。

### 阶段 12/13：单机可推进的设置增强与稳定性打磨
- **状态：** 已完成本轮单机实现，待真实多机和端口占用场景继续验收。
- 本轮目标：
  - 多机验收先等待更多电脑，不阻塞单机可实现功能。
  - 将端口设置从只读展示升级为可编辑、可校验、可持久化。
  - 搜索框接入真实最近会话和在线用户过滤。
  - 左侧本机信息显示在线、异常、离线状态。
  - 文件接收目录不可用时给出失败状态、写入失败记录，并尽量通知发送方。
- 已执行操作：
  - 更新 `task_plan.md`：阶段 11 标为等待实机环境，阶段 12/13 标为当前推进。
  - 设置面板新增 UDP 自动发现、TCP 消息、TCP 文件端口输入，限制 1024-65535。
  - 保存设置时校验端口范围与重复端口；端口变更采用“保存后重启生效”的稳妥策略。
  - `SettingsService` 增加非法端口和重复端口规范化，避免损坏配置导致启动异常。
  - App 初始化前预检测 UDP/TCP 端口可用性，端口被占用时给出明确状态提示。
  - 搜索框现在会过滤最近会话与在线用户，匹配昵称、IP、最近消息和状态。
  - 文件接收流创建失败时会将消息标为“保存失败”，保存 `Failed` 传输记录，并尝试发送 `ERROR` 控制消息。
  - `TcpFileServer` 对接收链路异常增加日志兜底，避免后台任务静默失败。
  - 新增设置端口规范化测试。
- 验证结果：
  - `dotnet build LanTalk.sln -v:minimal`：0 警告，0 错误。
  - `dotnet test LanTalk.sln -v:minimal`：16 个测试全部通过。
  - Debug 启动冒烟：应用成功启动，8 秒后停止，未留下运行中的 LanTalk 进程。
  - `dotnet publish src/LanTalk.App/LanTalk.App.csproj -c Release -r win-x64 -p:PublishAot=true -v:minimal`：发布成功；仍仅保留既有 Avalonia DataGrid trim/AOT 分析警告。
- 后续保留待办：
  - 用户准备更多电脑后，继续阶段 11 双机/三机真实验收。
  - 手动制造端口占用场景，确认启动提示和日志符合预期。
  - 继续验证文件传输中断、100MB 文件和防火墙拦截场景。

### 阶段 12：自动发现网段设置
- **状态：** 已完成代码实现，等待多机环境验证。
- 本轮目标：
  - 给软件增加局域网自动发现网段设置。
  - 用户可自行设置自动发现使用的网段或广播地址。
  - 保持 UDP 发现协议、模块边界和 AOT 友好结构稳定。
- 已执行操作：
  - 新增 `AppSettings.DiscoverySubnet`，默认值为 `Auto`。
  - 新增 `DiscoverySubnetResolver`，支持 `Auto`、CIDR 网段、`192.168.1.*` 通配网段、指定 IPv4 广播地址和多个目标分隔输入。
  - 设置页新增“自动发现网段”输入框，和端口设置同属“网络发现”区域。
  - 保存设置时校验网段格式，非法格式不会保存，并提示用户使用 `Auto`、CIDR 或通配网段。
  - `SettingsService` 会规范化网段配置；非法值回退到 `Auto`。
  - `DiscoveryService` 主动发送 `HELLO`、`HEARTBEAT`、`BYE` 时按设置发送到解析后的广播地址；`ONLINE` 仍定向回复源地址。
  - 新增网段解析测试，并扩展设置保存测试。
- 当前验证：
  - `dotnet build LanTalk.sln -v:minimal`：0 警告，0 错误。
  - `dotnet test LanTalk.sln -v:minimal`：22 个测试全部通过。
  - Debug 启动冒烟：应用成功启动，8 秒后停止，未留下运行中的 LanTalk 进程。
  - `dotnet publish src/LanTalk.App/LanTalk.App.csproj -c Release -r win-x64 -p:PublishAot=true -v:minimal`：发布成功；仍仅保留既有 Avalonia DataGrid trim/AOT 分析警告。
- 后续待验证：
  - 双机/三机环境下验证 `Auto`、`192.168.x.0/24` 和指定广播地址。

### 阶段 12：多网段添加 UI 改造
- **状态：** 已完成代码实现，等待验证。
- 本轮目标：
  - 将自动发现网段从单个文本框改造为可添加多个网段的设置界面。
  - 保持底层 `DiscoverySubnet` 存储和发现发送逻辑不变，降低迁移风险。
- 已执行操作：
  - `SettingsViewModel` 新增 `DiscoverySubnets` 列表和添加/删除命令。
  - 设置页新增多行网段列表，每行支持单独编辑和删除，并保留 `+` 添加入口。
  - 保存设置时将多行网段合并后统一校验与规范化。
  - 设置面板中间内容改为滚动区域，避免多网段时挤压底部按钮。
  - 扩展网段解析与设置服务测试，覆盖多个发现网段。
- 当前验证：
  - `dotnet build LanTalk.sln -v:minimal`：0 警告，0 错误。
  - `dotnet test LanTalk.sln -v:minimal`：24 个测试全部通过。
  - Debug 启动冒烟：应用成功启动，8 秒后停止，未留下运行中的 LanTalk 进程。
  - `dotnet publish src/LanTalk.App/LanTalk.App.csproj -c Release -r win-x64 -p:PublishAot=true -v:minimal`：发布成功；仍仅保留既有 Avalonia DataGrid trim/AOT 分析警告。
- 后续待验证：
  - 双机/三机环境下验证多个网段是否按预期发送发现包。

## 会话：2026-05-31

### 阶段 11-13：多机与文件传输验收回填
- **状态：** 已完成。
- 用户反馈：
  - 多机验证已经完成，没有发现问题。
  - 文件传输测试已经完成，没有发现问题。
- 已执行操作：
  - 更新 `task_plan.md`，将阶段 11 多机真实验收标记为完成。
  - 更新 `task_plan.md`，将阶段 12 的端口占用和自定义网段实机验证标记为完成。
  - 更新 `task_plan.md`，将阶段 13 网络与文件传输稳定性打磨标记为完成。
- 下一步：
  - 进入阶段 14 的验收文档和发布资料整理。
  - 进入阶段 15 的 P1 功能开发，优先实现聊天记录搜索。

### 阶段 15：聊天记录搜索
- **状态：** 已完成。
- 本轮目标：
  - 在稳定的 MVP 基础上推进第一个后续 P1 功能。
  - 实现当前会话内聊天记录搜索，不改变网络协议和模块边界。
- 已执行操作：
  - `MessageRepository` 新增 `SearchMessagesAsync`，按当前 `SessionId` 搜索 SQLite 中的历史消息。
  - `ChatHistoryService` 暴露搜索方法给 App 层使用。
  - 右侧聊天区域新增“搜索当前聊天记录”输入框和结果状态文案。
  - `MainWindowViewModel` 新增搜索状态、250ms 输入延迟、搜索结果加载和清空恢复最近消息逻辑。
  - 搜索激活时，新收发消息会重新执行当前会话搜索，避免不匹配消息混入搜索结果。
  - 新增 `MessageRepositoryTests`，覆盖会话隔离搜索和 `%` 字符字面量搜索。
  - 更新 README、`docs/test-plan.md`、`task_plan.md`、`findings.md`。
- 验证结果：
  - 初次将 `dotnet build` 与 `dotnet test` 并行执行时，build 被 `VBCSCompiler` 临时锁定 `LanTalk.Core.dll` 输出文件；随后改为顺序执行。
  - `dotnet build LanTalk.sln -v:minimal`：0 警告，0 错误。
  - `dotnet test LanTalk.sln -v:minimal`：26 个测试全部通过。

### 阶段 15：系统托盘、桌面通知与未读提醒
- **状态：** 已完成代码实现。
- 本轮目标：
  - 实现系统托盘，避免用户关闭窗口后直接退出应用。
  - 实现桌面通知，避免窗口不在前台时错过消息。
  - 强化未读提醒，让窗口标题、托盘和会话列表形成一致反馈。
- 已执行操作：
  - 新增 `UserNotificationEventArgs`，由 ViewModel 发出“需要提醒用户”的轻量事件。
  - `MainWindowViewModel` 新增 `TotalUnreadCount`、`UnreadSummary`、`WindowTitle`，收到非当前会话消息或文件请求时更新未读总数。
  - 私聊、广播和文件请求到达时触发通知事件；选择会话后清除该会话未读并刷新总数。
  - `MainWindow` 新增 Avalonia `TrayIcon`，关闭窗口默认隐藏到托盘，托盘支持打开窗口和退出应用。
  - 新增 `DesktopNotificationService` 和 `ToastNotificationWindow`，窗口隐藏、最小化或非活跃时弹出轻量桌面通知。
  - README、`docs/test-plan.md`、`task_plan.md`、`findings.md` 已同步更新。
- 验证结果：
  - `dotnet build LanTalk.sln -v:minimal`：0 警告，0 错误。
  - `dotnet test LanTalk.sln -v:minimal`：26 个测试全部通过。
  - Debug 启动冒烟：使用带日志的 `dotnet run --project src/LanTalk.App/LanTalk.App.csproj` 启动 15 秒，stdout/stderr 均为空，未观察到运行期异常输出。

### 内网通功能差距检查
- **状态：** 已完成评估。
- 本轮目标：
  - 检查当前 LanTalk 与“内网通”类局域网办公 IM 的功能差距。
  - 按必要性整理后续优化空间。
- 已执行操作：
  - 读取 `AGENTS.md`、`README.md`、`task_plan.md`、`findings.md`、`progress.md`。
  - 抽查 `MainWindowViewModel`、`MessageService`、`TcpFileServer` 等关键实现。
  - 检索公开资料中内网通的常见功能描述，用于功能广度对标。
  - 执行 `dotnet test LanTalk.sln -v:minimal`。
- 验证结果：
  - `dotnet test LanTalk.sln -v:minimal`：24 个测试全部通过。
- 结论：
  - 当前 LanTalk 已具备项目 MVP 的主体能力，距离“课堂/演示级内网通替代”主要差在真实多机验收和异常闭环。
  - 距离完整成熟内网通类产品仍有明显差距，重点缺口是群组/多标签、文件夹与断点续传、搜索导出、通知托盘、丰富消息形态、互通和加密。

### 阶段 15：联系人分组、最近会话排序与多会话切换
- **状态：** 已完成。
- 本轮目标：
  - 增加联系人部门/分组能力，让对端发现、已知联系人恢复和 UI 分组使用同一字段。
  - 优化最近会话排序，减少消息来了但被列表淹没的问题。
  - 增强多会话切换体验，降低频繁切换用户时的操作成本。
- 已执行操作：
  - `AppSettings`、`UserInfo`、`DiscoveryPayload` 新增 `Department` 字段。
  - `SettingsService` 增加部门默认值规范化。
  - `KnownUsers` 表新增 `Department` 字段，并补充旧表迁移逻辑。
  - `UserRepository` 保存和读取部门字段。
  - `DiscoveryService` 在 UDP 发现包中发送部门，接收旧包时回落到默认部门。
  - 左侧联系人区域改为按部门/分组展示，本机信息和设置面板显示部门。
  - 最近会话按未读优先、最近活跃时间排序；选择会话后清空对应未读并更新选中态。
  - 增加上一会话、下一会话、下一未读会话命令和界面按钮。
  - 左上角产品描述移除 MVP 字样，README 改为完整内网协作应用口径。
- 当前验证：
  - `dotnet build LanTalk.sln -v:minimal`：0 警告，0 错误。
  - `dotnet test LanTalk.sln -v:minimal`：30 个测试全部通过。
  - Debug 启动冒烟：应用运行 12 秒无 stdout/stderr 异常输出，随后主动结束进程。

### 阶段 16：私聊文本端到端加密

- 本轮目标：
  - 在一对一私聊会话中提供可选端到端加密开关。
  - 保持广播、文件传输和旧明文私聊路径兼容。
- 实现内容：
  - `PacketType` 新增 `EncryptionHello`、`EncryptionAck`、`EncryptionCancel`。
  - `NetworkPacket` 新增 `IsEncrypted`，旧包默认明文兼容。
  - `LanTalk.Network` 新增 `EndToEndEncryptionManager`，使用临时 ECDH P-256 协商密钥，使用 AES-256-GCM 加密私聊文本载荷。
  - `MessageService` 在 Network 层处理加密协商与解密，UI 继续接收原有 `TextMessagePayload`。
  - 主窗口当前私聊会话新增“端到端加密”开关，并显示协商状态和可人工比对的指纹。
  - 密钥仅保存在内存中，不写入 SQLite 或设置文件。
- 当前验证：
  - `dotnet build LanTalk.sln -v:minimal`：0 警告，0 错误。
  - `dotnet test LanTalk.sln -v:minimal`：37 个测试全部通过。

### 阶段 17：图片消息、表情与头像
- **状态：** 已完成。
- 本轮目标：
  - 实现图片消息便捷发送，让图片走单独入口而不是混在普通附件中。
  - 在聊天气泡内展示图片缩略图，并提供大图预览/缩放。
  - 增加常用表情快捷插入。
  - 改善联系人头像识别度。
- 已执行操作：
  - `MessageKind` 新增 `Image`，`FileTransferRequest` 新增可选 `IsImage` 字段。
  - 新增 `ImageMessageContent`，用 JSON 元数据保存图片文件名、大小和本地路径，不改 SQLite 表结构。
  - `MainWindowViewModel` 增加图片选择、发送、接收、历史加载、预览弹层和缩放命令。
  - 图片消息继续复用现有 TCP 文件传输和接收确认流程。
  - 输入栏新增表情按钮、图片按钮和常用表情面板。
  - 新增 `AvatarService`，本机与联系人头像使用昵称首字和稳定色彩。
  - README、`docs/test-plan.md`、`docs/ui-design.md`、`task_plan.md`、`findings.md` 已同步更新。
- 验证结果：
  - `dotnet build LanTalk.sln -v:minimal`：0 警告，0 错误。
  - 初次 `dotnet test` 失败：新增测试误用了不存在的 `MessageRepository.LoadRecentAsync`。
  - 修复为 `LoadRecentMessagesAsync` 并合并加密基线后，`dotnet test LanTalk.sln -v:minimal`：37 个测试全部通过。
  - Debug 启动冒烟：应用运行 12 秒无 stdout/stderr 异常输出，随后主动结束进程。

### 阶段 18：临时群组、永久群组与多人会话
- **状态：** 已完成。
- 设计结论：
  - 不引入服务端，群组消息复用 TCP 点对点发送，发送方遍历当前在线成员。
  - 群组会话使用 `GroupId` 作为聊天记录 `SessionId`。
  - 临时群组只保留在当前运行时会话列表；永久群组写入本地 SQLite 并在启动时恢复。
  - 群组文本消息先不启用端到端加密，文件/图片群发也先明确提示后续支持，避免和现有一对一文件传输状态机混用。
- 已执行操作：
  - Core 新增 `GroupKind`、`ChatGroup`、`GroupMessagePayload`、`PacketType.GroupMessage` 和 `MessageKind.Group`。
  - Storage 新增 `ChatGroups` 表和 `GroupRepository`，支持永久群组保存、加载和删除。
  - Network 新增 `SendGroupMessageAsync`，按在线成员逐个发送群组消息并返回成功/失败统计。
  - App 新增群组创建弹层、成员选择、临时/永久群组标识、群组会话加载、群消息收发、未读提醒和永久群组启动恢复。
  - 群组会话禁用端到端加密开关，并对群组文件/图片发送给出后续支持提示。
- 当前验证：
  - `dotnet build LanTalk.sln -v:minimal`：0 警告，0 错误。
  - `dotnet test LanTalk.sln -v:minimal`：43 个测试全部通过。

### 加密基础设施打磨：HKDF 官方实现替换
- **状态：** 已完成并验证通过。
- 本轮目标：
  - 将 `EndToEndEncryptionManager` 中手写 HKDF-SHA256 Extract/Expand 替换为 .NET 官方 `System.Security.Cryptography.HKDF`。
  - 保持现有私聊端到端加密协议、密钥指纹、AES-GCM 消息加解密行为不变。
- 已执行操作：
  - 查阅 Microsoft Learn 官方 API：`HKDF.DeriveKey(HashAlgorithmName, ikm, outputLength, salt, info)`。
  - 将私聊 ECDH 共享密钥派生改为 `HKDF.DeriveKey(HashAlgorithmName.SHA256, sharedSecret, KeySize, salt, info)`。
  - 删除本地 `HkdfSha256` 手写实现，减少自维护密码学代码。
- 验证结果：
  - `dotnet build LanTalk.sln -v:minimal`：0 警告，0 错误。
  - `dotnet test LanTalk.sln -v:minimal`：43 个测试全部通过。

### 阶段 19：群组高级功能
- **状态：** 进行中。
- 阶段规划：
  - 第一阶段实现群组图片/文件，复用现有 TCP 文件传输通道，每个在线成员独立 `FileId`，群组会话聚合展示投递状态。
  - 第二阶段实现离线成员补发，新增持久化待投递队列、上线重试和去重。
  - 第三阶段实现群组端到端加密，先评估逐成员加密，再扩展为群密钥 epoch 轮换。
- 当前执行：
  - 已确认当前群组文本消息、私聊图片/文件、私聊加密和 HKDF 基础均可复用。
  - 先修改 `FileTransferRequest` 和 `MainWindowViewModel`，不改变底层 TCP 文件流协议。
- 已完成：
  - `FileTransferRequest` 追加可选群组元数据，并保持旧文件请求兼容。
  - 群组会话允许发送图片/文件，发送方按在线成员逐个创建 `FileId` 和文件请求。
  - 发送方用 `GroupFileTransferState` 聚合显示群发请求、接收、拒绝、失败和完成状态。
  - 接收方收到群组附件请求时自动恢复群组会话，永久群组继续持久化。
  - 群组图片可写入群组聊天历史，继续复用现有图片预览逻辑。
- 验证结果：
  - `dotnet build LanTalk.sln -v:minimal`：0 警告，0 错误。
  - `dotnet test LanTalk.sln -v:minimal`：45 个测试全部通过。

### 阶段 19：群组文本离线补发
- **状态：** 已完成并验证通过。
- 已执行操作：
  - Core 新增 `OutgoingDeliveryRecord`，表示待补发的 TCP 控制消息。
  - Storage 新增 `OutgoingDeliveries` 表和 `OutgoingDeliveryRepository`，支持保存、按收件人加载、更新重试信息和删除。
  - Network 新增 `SendGroupMessageToAsync`，让 App 能按成员逐个发送并精确处理失败成员。
  - App 群组文本发送改为逐成员处理：在线成员立即发送，离线或发送失败成员进入待补发队列。
  - 发现服务报告成员重新上线后，App 自动读取该成员的待补发群组消息并重试，成功后删除队列记录。
- 验证结果：
  - `dotnet build LanTalk.sln -v:minimal`：0 警告，0 错误。
  - `dotnet test LanTalk.sln -v:minimal`：48 个测试全部通过。

### 阶段 19：群组附件离线补发
- **状态：** 已完成并验证通过。
- 已执行操作：
  - `OutgoingDeliveryRecord` 和 `OutgoingDeliveries` 表新增 `SourcePath`，用于保存发送方本地源文件路径。
  - `DatabaseInitializer` 增加 `OutgoingDeliveries.SourcePath` 轻量迁移，兼容已存在的队列表。
  - 群组附件发送覆盖全部非本人群成员：在线成员立即发送，离线或发送失败成员进入附件补发队列。
  - 成员重新上线后，自动读取 `FileTransferRequest + SourcePath` 并重发附件请求；成功后删除队列记录。
  - 源文件缺失时保留队列记录并更新错误原因，状态栏提示源文件不可用。
- 验证结果：
  - `dotnet build LanTalk.sln -v:minimal`：0 警告，0 错误。
  - 初次 `dotnet test LanTalk.sln -v:minimal` 失败：新增 SQLite 迁移测试在 Windows 上未释放验证连接就删除临时库。
  - 收紧测试连接作用域后，`dotnet test LanTalk.sln -v:minimal`：49 个测试全部通过。

### 阶段 19：群组端到端加密
- **状态：** 已完成并验证通过。
- 设计结论：
  - 当前阶段采用逐成员端到端加密，不引入中心服务、共享群密钥或复杂 epoch 轮换。
  - 群组会话开启加密后，发送方复用每个成员已协商的一对一 E2EE 会话，分别加密同一条 `GroupMessagePayload`。
  - 成员离线或尚未完成协商时，消息进入 `OutgoingDeliveries`，并标记 `RequiresEncryption`，补发时不会降级为明文。
  - 当前保护范围是群组文本的网络传输；本地 SQLite 历史/待补发队列和图片/文件内容仍按现有协议处理。
- 已执行操作：
  - `MessageService` 支持加密 `GroupMessage`，接收端复用已有 AES-GCM 解密路径。
  - 主窗口群组会话启用端到端加密开关，开启时自动向在线成员发起一对一加密协商。
  - 群组发送链路按成员判断加密状态：已就绪成员立即加密发送，未就绪/离线成员进入加密补发队列。
  - `OutgoingDeliveryRecord`、`OutgoingDeliveries` 和仓储新增 `RequiresEncryption`，旧数据库通过轻量迁移补齐字段。
  - 加密会话完成后自动触发对应成员的待补发队列重试。
- 验证结果：
  - `dotnet build LanTalk.sln -v:minimal`：0 警告，0 错误。
  - `dotnet test LanTalk.sln -v:minimal`：51 个测试全部通过。

### 阶段 20：多文件、文件夹传输与断点续传
- **状态：** 已完成并通过自动化验证。
- 本轮目标：
  - 支持一次选择多个文件发送。
  - 支持选择文件夹并保留相对目录结构传输。
  - 支持传输中断后重新确认同一批次或同一文件时从接收端已有字节继续。
- 已执行操作：
  - 读取 `README.md`、`docs/protocol.md`、`docs/test-plan.md`、`task_plan.md`、`findings.md`、`progress.md`。
  - 抽查 `FileTransferRequest`、`FileTransferResponse`、`TcpFileClient`、`TcpFileServer`、`FileTransferService`、`MainWindowViewModel`、`FileTransferRepository`、`DatabaseInitializer` 和相关测试。
  - 将阶段 20 写入 `task_plan.md`，并把协议/路径安全/续传 offset 的设计发现写入 `findings.md`。
  - Core 新增 `FileTransferKind`、`FileTransferItem`、`FileTransferResumeItem`，并扩展 `FileTransferRequest` / `FileTransferResponse` / `FileTransferRecord`。
  - Network 文件流新增 v2 header，支持 offset 续传，并保留旧 header 兼容。
  - App 文件按钮支持多选，新增文件夹按钮；接收端对多文件/文件夹批次只确认一次。
  - App 接收端增加相对路径安全校验、目录创建、已有文件长度检测和批次总体进度。
  - Storage `FileTransfers` 增加批次与续传字段，并补齐旧表迁移。
  - 更新 README、`docs/protocol.md`、`docs/test-plan.md`。
- 当前设计：
  - 协议层增加文件批次和文件项模型。
  - TCP 文件流增加兼容旧 header 的 v2 header，用于携带续传 offset。
  - UI 层增加多文件选择和文件夹选择入口，接收端批次只确认一次。
  - 断点续传由接收端根据已有目标文件长度返回 offset，发送端从 offset 后继续流式发送。
- 验证结果：
  - `dotnet build LanTalk.sln -v:minimal`：0 警告，0 错误。
  - `dotnet test LanTalk.sln -v:minimal`：55 个测试全部通过。
- 验证插曲：
  - 一次将 `dotnet build` 和 `dotnet test` 并行执行时，`MessageService_ShouldSendEncryptedGroupMessageOverTcp` 出现 TCP 回环连接拒绝；单独重跑 `dotnet test LanTalk.sln -v:minimal` 后 55 项全部通过，判断为并行执行带来的测试时序波动。
- 后续建议：
  - 用双机真实环境补充 100MB 多文件、深层文件夹和传输中断后续传验收。

### 阶段 20 提交与推送
- **状态：** 已完成。
- 用户要求先把“之前的那些变更”提交推送，因此先暂停阶段 21 实现，提交并推送阶段 20。
- 验证结果：`dotnet test LanTalk.sln -v:minimal` 55 项全部通过。
- 提交：`fe8d344 feat: add batch file transfers and resume`。
- 推送：`git push origin main` 成功，远端从 `eedb5ce` 更新到 `fe8d344`。

### 阶段 21：已读回执、消息撤回与离线文件提醒
- **状态：** 进行中。
- 本轮目标：
  - 实现私聊已读回执，发送方能看到“未读/已读”。
  - 实现群组已读回执，按成员区分并显示“已读 x/y”。
  - 实现消息撤回，参考钉钉式同步撤回状态，不物理删除历史。
  - 实现离线文件提醒，对方上线后提示有文件待发送/待接收，再复用现有文件确认和流式传输链路。
- 已执行操作：
  - 读取 `AGENTS.md`、`README.md`、`docs/protocol.md`、`docs/test-plan.md`、`task_plan.md`、`findings.md` 和 `progress.md`。
  - 确认阶段 20 已提交推送，阶段 21 可在最新 `main` 基线上继续实现。
  - 将阶段 21 写入 `task_plan.md`，并把初步设计边界写入 `findings.md`。
  - Core 新增 `MessageReadReceiptPayload`、`MessageRecallPayload`、`OfflineFileReminderPayload`，并扩展 `PacketType`、`FileTransferStatus` 和 `ChatMessage`。
  - Storage 新增 `MessageReadReceipts` 表，`ChatMessages` 补齐已读/撤回字段和轻量迁移，`MessageRepository` 支持会话标记已读、群组成员已读统计和撤回标记。
  - Network 新增已读回执、撤回通知和离线文件提醒发送方法。
  - App 接入私聊/群组已读、撤回按钮、离线文件提醒队列和上线补发逻辑。
  - 文档已更新 README、协议说明和测试计划。
- 当前验证：
  - 中途 `dotnet build LanTalk.sln -v:minimal` 发现 3 个变量名/作用域错误，已修复。
  - `dotnet test LanTalk.sln -v:minimal`：63 项全部通过。

### 阶段 22：交付闭环、生产日志与后续增强
- **状态：** 进行中。
- 本轮目标：
  - 按 `next-development-plan.md` 完整推进下一轮开发任务，并在阶段边界自动提交推送。
  - 先收口文档和计划文件，再进入日志、性能、安全和平台增强。
- 已执行操作：
  - 读取 `AGENTS.md`、`README.md`、`task_plan.md`、`findings.md`、`progress.md`、`docs/test-plan.md` 和 `next-development-plan.md`。
  - 检索当前仓库的日志、通知、压缩、导出和虚拟化相关实现，确认现有基础与缺口。
  - 更新 `task_plan.md`，新增阶段 22-27，作为本轮持续开发主线。
  - 将差距梳理与 Windows 上并行 `build` / `test` 的文件锁问题写入 `findings.md`。
- 当前验证：
  - 并行执行 `dotnet build LanTalk.sln -v:minimal` 与 `dotnet test LanTalk.sln -v:minimal` 会触发 `CS2012`，原因是 `VBCSCompiler` 占用 `LanTalk.Core.dll`。
  - 改为串行后，`dotnet test LanTalk.sln -v:minimal`：63 项全部通过。
  - 串行重跑 `dotnet build LanTalk.sln -v:minimal`：0 警告，0 错误。

### 阶段 23：日志能力生产化
- **状态：** 已完成并通过自动化验证。
- 本轮目标：
  - 将现有控制台日志升级为可落盘、可滚动、可测试的生产化日志。
  - 补齐应用生命周期、离线补发、文件续传、已读回执和撤回相关关键日志事件。
- 已执行操作：
  - `ConsoleLanTalkLogger` 扩展为控制台 + 文件双写，默认写入 `%AppData%\\LanTalk\\logs\\lantalk-YYYY-MM-DD.log`。
  - 增加按天轮转和单文件长度保护；错误日志输出完整 `Exception.ToString()`，包含异常类型与堆栈。
  - MainWindow / App / MainWindowViewModel / MessageService 已补齐启动、关闭、用户状态变化、离线补发、文件收发、已读回执、撤回和离线文件提醒关键日志。
  - 新增 `ConsoleLanTalkLoggerTests`，覆盖文件写入、异常堆栈、大小轮转和跨天切换。
- 验证结果：
  - 首次 `dotnet test LanTalk.sln -v:minimal` 失败：日志测试在读取前未释放句柄，且大小保护下限过高导致轮转测试未命中阈值。
  - 修复后，`dotnet test LanTalk.sln -v:minimal`：67 项全部通过。
  - `dotnet build LanTalk.sln -v:minimal`：0 警告，0 错误。

### 阶段 24：列表虚拟化与刷新优化
- **状态：** 已完成并通过自动化验证与桌面启动冒烟。
- 本轮目标：
  - 把左侧最近会话 / 联系人分组和右侧消息区迁移到可虚拟化控件。
  - 减少 `ObservableCollection` 的整表清空重建，降低切会话和搜索时的 UI 抖动。
- 已执行操作：
  - 左侧最近会话改为 `ListBox`，保留现有会话选择和未读显示。
  - 联系人分组改为扁平化 `ContactListEntries` + `ListBox.DataTemplates`，避免嵌套 `ItemsControl`。
  - 右侧消息区改为 `ListBox`，并在 code-behind 使用 `ScrollIntoView` 自动滚动到最后一条消息。
  - `ReplaceCollection` 改为按差异移动 / 插入 / 删除，而不是每次 `Clear + Add` 全量重建。
- 验证结果：
  - `dotnet build LanTalk.sln -v:minimal`：0 警告，0 错误。
  - `dotnet test LanTalk.sln -v:minimal`：67 项全部通过。
  - 桌面冒烟启动：`dotnet run --project src/LanTalk.App/LanTalk.App.csproj --no-build` 隐藏运行 12 秒，无 `stderr` 输出。
