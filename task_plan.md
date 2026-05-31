# 任务计划：LanTalk MVP 开发

## 目标
将 LanTalk 开发成一个可编译、可运行的 C# / .NET 10 局域网即时通信 MVP，具备 Avalonia + SukiUI 界面、本机设置、UDP 自动发现、TCP 私聊、广播消息、单文件传输、SQLite 历史记录、基础日志与异常处理，并保持 Native AOT 友好的代码结构。

## 当前阶段
阶段 14/15 - 验收文档与后续 P1 功能推进

## 范围边界
- 优先完成 MVP / P0：项目骨架、Telegram 风格 UI、本机设置、UDP 自动发现、在线用户、TCP 私聊、SQLite 历史记录、广播、单文件传输、基础异常与日志。
- P1 只在支持 MVP 质量时纳入：文件进度、接收确认、主题预留、简单日志、最近会话。
- P2 默认延期，除非用户明确要求：永久群聊、多文件/文件夹传输、断点续传、消息撤回、已读回执、加密、托盘、通知、开机自启、跨网段手动添加 IP。
- 保持模块边界：App 只处理 UI / ViewModel；Core 负责模型、枚举、协议、序列化；Network 负责 UDP/TCP 且不引用 Avalonia；Storage 负责 SQLite/配置且不引用 Avalonia/Network。
- 优先使用简单依赖：Avalonia、SukiUI、CommunityToolkit.Mvvm、Microsoft.Data.Sqlite、xUnit。避免 Prism、ReactiveUI 全家桶、大型 ORM、插件系统、动态代理/反射较重的框架。
- 主线目标为 .NET 10 LTS。架构上为未来 .NET 11 特性预留扩展点，但不依赖 preview-only API。

## 阶段

### 阶段 0：规划与需求分析
- [x] 阅读 `AGENTS.md` 项目开发规范。
- [x] 阅读 `lan_talk_codex项目说明文档.md` 项目说明。
- [x] 检查当前仓库结构。
- [x] 创建持久化计划文件。
- [x] 将用户给出的分阶段提示词映射为实施里程碑。
- **状态：** 已完成

### 阶段 1：项目骨架与 Telegram 风格静态 UI
- [x] 检查已安装的 .NET SDK 与 Avalonia 模板可用性。
- [x] 创建 `LanTalk.sln`。
- [x] 创建项目：`LanTalk.App`、`LanTalk.Core`、`LanTalk.Network`、`LanTalk.Storage`、`LanTalk.Tests`。
- [x] 按模块边界配置项目引用。
- [x] 添加包引用：App 引用 SukiUI 与 CommunityToolkit.Mvvm；Storage 引用 Microsoft.Data.Sqlite；测试项目按需引用测试包。
- [x] 创建基础 `docs` 目录：`project-spec.md`、`protocol.md`、`ui-design.md`、`test-plan.md`。
- [x] 尽早加入核心模型、枚举、常量、JSON Source Generator 基础结构，为后续阶段铺底。
- [x] 构建一个可运行的 Avalonia 主窗口，采用 Telegram 风格双栏布局。
- [x] 添加假数据：本机资料、在线用户、最近会话、消息气泡、广播/文件消息视觉样例。
- [x] 添加设置窗口 UI 壳：昵称、文件保存目录、是否保存历史、主题模式、端口展示。
- [x] 执行构建与应用启动冒烟测试。
- **状态：** 已完成

### 阶段 2：本机设置与身份
- [x] 在 Core 中实现 `AppSettings`。
- [x] 在 Storage 中实现基于 JSON 与 `LanTalkJsonContext` Source Generator 的 `SettingsService`。
- [x] 首次启动生成稳定的 `UserId`。
- [x] 缺少昵称时生成默认昵称，并允许通过 UI 修改昵称。
- [x] 持久化文件接收目录、主题模式/主题色预留、端口、`SaveChatHistory`。
- [x] 处理损坏配置文件：尽量备份或重置为安全默认值，并提供可见提示/日志。
- [x] 添加设置读写与 `UserId` 稳定性的单元测试。
- [x] 执行构建/测试，并记录手动测试方式。
- **状态：** 已完成

### 阶段 3：UDP 自动发现与在线用户注册表
- [x] 实现 `UserInfo`、`PacketType`、`NetworkPacket`、发现载荷类型、`NetworkConstants`。
- [x] 在 `LanTalk.Network` 中实现不依赖 UI 的 `OnlineUserRegistry`。
- [x] 实现 `UdpDiscoveryServer`：异步 UDP 监听/发送、HELLO/ONLINE/HEARTBEAT/BYE、取消支持、Socket 异常处理。
- [x] 实现 `DiscoveryService` 编排：启动监听、发送 HELLO、每 5 秒心跳、15 秒离线判定、停止时发送 BYE。
- [x] 将 App ViewModel 连接到发现事件，并在 UI 线程更新在线用户。
- [x] 尽量避免整表刷新，只更新发生变化的用户。
- [x] 添加协议/序列化测试与注册表测试。
- [x] 手动验证：如可行则启动两个本地实例，否则记录多机器局域网测试步骤。
- **状态：** 已完成

### 阶段 4：SQLite 存储与 TCP 私聊
- [x] 实现 SQLite 连接工厂与 `DatabaseInitializer`。
- [x] 创建 `KnownUsers`、`ChatMessages`、`FileTransfers` 表及必要索引。
- [x] 实现 `MessageRepository` 与 `ChatHistoryService`，打开会话默认加载最近 50 条消息。
- [x] 实现 `TcpMessageServer` 与 `TcpMessageClient`：异步 TCP、取消支持、包帧协议、JSON Source Generator、异常处理。
- [x] 实现 `MessageService` 的 `PRIVATE_MESSAGE` 发送与接收。
- [x] 串联在线用户选择、发送输入框、消息气泡、收到消息自动建会话、发送失败状态。
- [x] 当 `SaveChatHistory` 启用时保存发出与收到的私聊消息。
- [x] 添加仓储测试与消息协议测试。
- [x] 手动验证：A 给 B 发消息，B 回复 A，重启后历史记录仍可加载。
- **状态：** 已完成

### 阶段 5：广播消息
- [x] 在侧栏或会话列表添加广播入口。
- [x] 在 `MessageService` 中加入 `BROADCAST_MESSAGE` 包流程。
- [x] 遍历在线用户发送广播，排除自己。
- [x] 用独立的 MVP 样式展示广播消息。
- [x] 使用稳定的广播会话 ID 将广播消息保存到 SQLite。
- [x] 针对部分收件人发送失败提供简洁 UI 反馈/日志。
- [x] 添加广播包序列化与仓储持久化测试。
- [x] 手动验证：A 发送广播，B/C 能收到，且视觉样式与私聊区分。
- **状态：** 已完成

### 阶段 6：单文件传输
- [x] 实现 `FileTransferRequest`、`FileTransferRecord`、`FileTransferStatus` 模型。
- [x] 实现 `FileTransferRepository` 并接入表初始化。
- [x] 实现 `FILE_REQUEST`、`FILE_ACCEPT`、`FILE_REJECT`、`FILE_FINISHED` 控制消息。
- [x] 在 App 中添加文件选择发送流程与接收确认弹窗。
- [x] 实现 `TcpFileServer` 与 `TcpFileClient`，使用流式传输与 `ArrayPool` 缓冲区；传输逻辑禁止使用 `File.ReadAllBytes`。
- [x] 将文件保存到 `AppSettings.FileSavePath`，并处理目录不存在或不可写情况。
- [x] 添加进度 ViewModel 与 UI 进度展示。
- [x] 保存传输成功、拒绝与失败记录。
- [x] 添加文件元数据、仓储记录、小型临时文件传输等可行测试。
- [x] 手动验证：小文件、拒绝接收、传输中断、100MB 文件、UI 保持响应。
- **状态：** 已完成

### 阶段 7：异常处理、日志、UI 打磨与文档
- [x] 添加简单日志：程序启动/关闭、设置加载、用户上线/离线、消息发送/接收、文件传输开始/完成/失败、网络/数据库异常。
- [x] 对端口占用、UDP 失败、TCP 失败、对方离线、JSON 解析失败、SQLite 初始化失败、文件不存在/保存失败提供友好提示。
- [x] 打磨 UI 间距、颜色、消息模板、文件卡片、状态文案、主题预留与空状态，避免重动画。
- [x] 确保 UI 显示文本使用中文，类名/方法名使用英文。
- [x] 编写 README：项目简介、技术栈、功能列表、结构、运行方式、开发计划、演示流程、常见问题、小组分工占位。
- [x] 补齐 `docs/project-spec.md`、`docs/protocol.md`、`docs/ui-design.md`、`docs/test-plan.md`。
- [x] 执行构建/测试并记录结果。
- **状态：** 已完成

### 阶段 8：Native AOT 兼容性检查与发布验证
- [x] 审计 JSON 使用，将反射式序列化替换为 `LanTalkJsonContext`。
- [x] 审计 `Assembly.Load`、反射扫描、动态 XAML、动态代理、阻塞式 Socket/IO 等风险。
- [x] 确保 ViewModel 使用 CommunityToolkit.Mvvm Source Generator。
- [x] 在不牺牲 MVP 稳定性的前提下检查 trimming/AOT 警告。
- [x] 执行普通 `win-x64` Release 发布。
- [x] MVP 稳定后再尝试 Native AOT 发布，并记录警告与延期修复项。
- [x] 记录最终测试矩阵与演示检查清单。
- **状态：** 已完成

### 阶段 9：通信产品化补齐与真实会话整理
- [x] 清理 UI 假数据和旧阶段文案：启动后无真实用户时显示“暂未发现用户”，不再显示阶段 1/阶段 4 提示。
- [x] 完成真实最近会话系统：发现用户、选择用户、收到私聊、收到广播、收到文件请求时自动创建/更新/置顶会话。
- [x] 修复消息显示：历史消息和实时消息优先显示昵称，不直接暴露 `UserId`。
- [x] 给“刷新”按钮接真实命令：重新广播 `HELLO`，并触发离线用户清理。
- [x] 补齐 `KnownUsers` 持久化：上线/离线时写库，重启后可恢复已知联系人或最近联系人基础信息。
- [x] 补齐 `FILE_FINISHED` 和 `ERROR` 控制消息：文件完成/失败双方状态一致，数据库记录可靠更新。
- **状态：** 已完成

### 阶段 10：主题模式切换修复
- [x] 修复 `App.axaml` 固定 `RequestedThemeVariant="Light"` 导致设置不生效的问题。
- [x] 新增 `AppThemeService`，保存设置和启动加载时应用 `System` / `Light` / `Dark`。
- [x] 将 LanTalk 自定义颜色刷接入深浅色主题，覆盖侧栏、主面板、设置面板、广播提示、文件卡片和头像底色。
- [x] 修复深色模式下普通按钮悬停/按下状态文字可能变黑的问题。
- [x] 规范化旧版非法主题配置值，避免保存成 `Avalonia.Controls.ComboBoxItem`。
- [x] 通过 build/test/Native AOT 发布与深色截图验证。
- **状态：** 已完成

### 阶段 11：多机真实验收与问题闭环
- [x] 准备双机/三机验收环境：确认同一局域网、防火墙提示、默认端口占用情况。
- [x] 验收 UDP 发现：A/B/C 同时启动、互相发现、刷新重新广播、关闭后 15 秒左右离线、重新上线恢复。
- [x] 验收 TCP 私聊：A 发 B、B 回 A、未选择会话时收消息、离线用户发送失败提示、重启加载历史。
- [x] 验收广播：A 广播给 B/C，接收端样式正确，广播历史保存。
- [x] 验收文件传输：小文件、100MB 文件、接收方拒绝、传输中断、保存路径不存在或不可写、UI 不冻结。
- [x] 将所有发现的问题分级记录：P0 阻塞演示、P1 影响体验、P2 后续增强。
- **状态：** 已完成；用户反馈多机验证和文件传输测试均无问题。

### 阶段 12：设置增强与端口可配置
- [x] 将设置面板中的 UDP/TCP 消息/TCP 文件端口从只读展示升级为可编辑输入。
- [x] 增加端口范围、重复端口、非法数字校验。
- [x] 保存设置时提示端口变更需要重启监听或重启应用，并选择 MVP 内最稳妥的生效策略。
- [x] 启动时检测端口占用，给出明确状态提示和日志。
- [x] 为端口设置读写、非法配置规范化增加测试。
- [x] 增加自动发现网段设置，支持 `Auto`、CIDR 网段、通配网段和指定广播地址。
- [x] 将自动发现网段从单文本框改为可逐条添加/删除的多网段列表。
- [x] 用真实端口占用场景手动验证冲突提示文案。
- [x] 用双机/三机环境验证自定义发现网段是否能发现目标设备。
- **状态：** 已完成；端口与发现网段已随多机验收确认。

### 阶段 13：网络与文件传输稳定性打磨
- [x] 梳理 UDP/TCP/File 三条链路的异常提示，确保用户能看懂失败原因。
- [x] 文件接收路径不可用时给出可恢复提示，避免静默失败。
- [x] 文件传输中断后双方状态一致，数据库记录为失败，UI 不停留在“正在传输”。
- [x] 对发送失败、对方离线、JSON 解析失败补充日志上下文。
- [x] 搜索框接入真实最近会话与在线用户过滤。
- [x] 左侧本机信息显示在线、异常、离线状态。
- [x] 保持文件传输继续使用流式分块和 `ArrayPool`，不引入断点续传等 P2 范围。
- **状态：** 已完成；用户反馈多机和文件传输验收无问题。

### 阶段 14：验收文档、演示脚本与发布包
- [ ] 更新 `docs/test-plan.md`：补齐双机/三机验收矩阵和实测结果列。
- [ ] 更新 README：当前功能完成度、已知限制、演示流程、发布包路径。
- [ ] 编写课堂汇报用演示脚本：启动、发现、私聊、广播、传文件、看历史、切主题。
- [ ] 执行最终 `dotnet build`、`dotnet test`、普通 Release publish、Native AOT publish。
- [ ] 记录 Avalonia DataGrid trim/AOT 既有警告是否仍存在，以及是否影响演示。
- **状态：** 待开始

### 阶段 15：后续 Backlog 收敛
- [x] 根据阶段 11-14 的验收结果整理 P1/P2 待办，不直接扩大实现范围。
- [x] 优先候选 P1：聊天记录搜索、桌面通知、系统托盘、跨网段手动添加 IP 的可行性评估。
- [x] 实现当前会话内聊天记录搜索：右侧聊天面板可输入关键词，从 SQLite 查询当前会话历史。
- [x] 实现系统托盘：关闭窗口默认隐藏到托盘，托盘可打开窗口或退出应用。
- [x] 实现桌面通知：主窗口隐藏、最小化或非活跃时收到消息/文件请求会弹出轻量置顶通知。
- [x] 强化未读提醒：窗口标题、托盘提示和托盘菜单显示总未读数。
- [x] 实现更完整的联系人/会话体验：联系人按部门/分组展示，最近会话按未读和活跃时间排序，支持多会话快速切换。
- [ ] 评估并实现下一个 P1：打包发布细节、开机自启或更完整的文件/群组体验。
- [x] 实现私聊文本端到端加密开关：临时 ECDH 协商、AES-GCM 加密、会话指纹展示和关闭同步。
- [ ] 明确暂缓剩余 P2：永久群聊、多文件/文件夹、断点续传、撤回、已读、文件传输加密。
- **状态：** 进行中；聊天记录搜索、系统托盘、桌面通知、未读提醒强化和联系人/会话体验优化已完成。

## 当前项目状态下推荐的里程碑顺序
1. 阶段 11 的多机真实验收先等待更多电脑；这是验收证据问题，不阻塞单机功能开发。
2. 当前先推进阶段 12：端口可编辑、发现网段可配置、校验、非法配置规范化和启动端口占用提示。
3. 同步推进阶段 13 中的单机可复现问题：搜索过滤、本机状态展示、文件保存失败兜底。
4. 等用户准备好双机/三机环境后回到阶段 11，按验收矩阵记录真实结果。
5. 阶段 14/15 在真实验收后整理发布包、演示脚本和后续 backlog。

## 关键问题
1. 当前机器是否已安装 .NET 10 SDK？如果没有，只有在已安装 SDK 能目标到 `net10.0` 时才继续，否则报告阻塞。
2. Avalonia 模板是否已安装？如果没有，在网络/包访问可用时安装或恢复 `dotnet new` 模板。
3. 哪个 SukiUI 包版本兼容当前 Avalonia/.NET 组合？优先选择 NuGet 上可用的稳定兼容版本。
4. 两个本地实例是否能同时绑定默认端口？大概率不能；本地多实例验证可能需要临时不同端口或多机器，但 MVP 默认端口仍保持 50000/50001/50002。
5. 配置文件或数据库损坏时如何处理？计划是尽量保留备份，重建默认文件，并记录日志。

## 已做决策
| 决策 | 理由 |
|------|------|
| 先创建持久化计划文件再实现代码 | 用户明确要求先计划，并点名使用 `planning-with-files`。 |
| 将阶段 1 + 阶段 2 作为首个实现目标 | 当前仓库没有解决方案或源码项目；本机设置也是自动发现的前置条件。 |
| 默认以 .NET 10 作为项目目标，除非本地 SDK 阻止 | 项目规范要求以 .NET 10 LTS 为主线。 |
| .NET 11 兼容性以抽象和预留点体现，不引入 preview 依赖 | 需求要求主线稳定，同时为未来 Runtime Async、Union Type、Zstandard 预留空间。 |
| 协议包/载荷在合适位置使用 `sealed record` | 模拟未来 union type 风格，并保持协议形状不可变、AOT 友好。 |
| 后续在 Core 预留 `ICompressor` 抽象，但 MVP 不实现压缩 | 为未来 Zstd 扩展留接口，同时不扩大 MVP 范围。 |
| 从第一批协议/设置代码开始使用 JSON Source Generator | 降低 AOT/trimming 风险，并符合项目规范。 |
| 通过 Microsoft.Data.Sqlite 直接访问 SQLite | 轻量，避免大型 ORM。 |
| 初期使用简单手动服务组合，不引入复杂 DI 框架 | 启动流程透明，AOT 友好，依赖更少。 |

## 风险与应对
| 风险 | 应对 |
|------|------|
| .NET 10 / Avalonia / SukiUI 包兼容性不匹配 | 生成代码前先验证 SDK 和包版本，优先稳定兼容版本。 |
| 多个本地实例无法共享固定端口 | MVP 保持默认端口；必要时仅测试环境使用临时端口。 |
| 网络/数据库/文件 IO 阻塞 UI 线程 | 所有 IO 使用异步，只把最终状态更新切回 UI 线程。 |
| Network 层意外依赖 App/Avalonia | 通过项目引用边界约束，Network/Storage 不引用 App。 |
| JSON 或 XAML 写法引发 AOT 警告 | 使用 Source Generator、尽量编译绑定，避免动态加载/反射。 |
| 文件传输内存暴涨 | 使用 Stream + `ArrayPool` 分块传输，明确禁止 `File.ReadAllBytes`。 |
| 范围蔓延到 P2 | P2 保持延期，除非用户明确要求。 |

## 已遇到错误
| 错误 | 尝试次数 | 解决方式 |
|------|----------|----------|
| `dotnet sln LanTalk.sln add` 初次失败：找不到 `LanTalk.sln` | 1 | .NET 10 默认创建 `.slnx`，随后使用 `dotnet new sln --format sln --force` 补建传统 `LanTalk.sln` 并成功加入项目。 |
| App 构建缺少 `Task` / `Type`，并触发 MVVMTK0007 | 1 | App 项目模板未开启 `ImplicitUsings`，已在 `LanTalk.App.csproj` 增加 `<ImplicitUsings>enable</ImplicitUsings>`。 |
| 数据库初始化测试删除临时 SQLite 文件失败 | 1 | Windows 上 SQLite 连接池仍持有文件，测试中调用 `SqliteConnection.ClearAllPools()` 后删除。 |
| Native AOT 发布失败：Platform linker not found | 1 | 本机缺少 Native AOT 前置条件，需安装 Visual Studio Desktop Development for C++ / C++ build tools；项目代码扫描未发现明显高风险反射或阻塞模式。 |

## 立即下一步
1. 先更新阶段 14 文档，把用户已完成的多机与文件传输验收结果写入测试计划和 README。
2. 推进阶段 15 的 P1 功能，聊天记录搜索、通知托盘和联系人/会话体验已完成。
3. 后续再评估打包发布、开机自启和更完整的文件/群组体验。
4. P2 剩余功能继续保持延期：永久群聊、多文件/文件夹、断点续传、撤回、已读、文件传输加密。
