# LanTalk 项目规格

LanTalk 是基于 C# / .NET 10 的局域网即时通信工具，面向教室、实验室和小型办公室等无需外网的协作场景。

## MVP 目标
- 打开即用，无需登录注册。
- 自动发现同一局域网内的在线用户。
- 支持一对一私聊、全员广播、单文件传输。
- 使用 SQLite 保存本地聊天记录和文件传输记录。
- 使用 Avalonia UI + SukiUI 构建 Telegram 风格现代桌面界面。
- 保持异步、流式传输和 Native AOT 友好的代码结构。

## 当前实现状态
- 阶段 1 到阶段 6 已完成。
- 应用可编译、可启动。
- UDP 自动发现、TCP 私聊、广播消息、单文件传输和 SQLite 记录均已具备 MVP 实现。
- 后续重点是多机实测、课堂演示素材和 Native AOT 发布验证。

## 模块边界
- `LanTalk.App`：UI、ViewModel、窗口、弹窗与用户交互。
- `LanTalk.Core`：模型、枚举、协议、常量、JSON Source Generator、公共抽象。
- `LanTalk.Network`：UDP 自动发现、TCP 消息、TCP 文件传输。
- `LanTalk.Storage`：SQLite、仓储、JSON 设置。
- `LanTalk.Tests`：基础协议、配置、仓储测试。
