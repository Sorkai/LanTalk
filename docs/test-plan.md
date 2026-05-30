# LanTalk 测试计划

## 阶段 1
- `dotnet build LanTalk.sln`
- `dotnet test LanTalk.sln`
- `dotnet run --project src/LanTalk.App`
- 验证主窗口可启动，左侧显示本机信息、搜索框、广播入口、最近会话和真实在线用户/已知联系人。
- 验证右侧显示聊天标题、消息气泡、文件消息卡片和输入栏。
- 验证设置面板可打开，昵称、文件目录、保存历史、主题和端口信息可见。

## 自动化测试
当前测试覆盖：
- JSON Source Generator 序列化。
- 设置创建、保存、损坏恢复。
- SQLite 初始化。
- 文件传输记录保存。
- 在线用户注册表。
- TCP 私聊回环。
- 广播部分失败统计。
- TCP 文件流式传输。

运行：
```bash
dotnet test LanTalk.sln
```

## 后续阶段
- UDP 自动发现：两台或三台电脑互相发现，关闭后离线，重新上线恢复。
- TCP 私聊：A 发给 B，B 回复 A，重启后加载历史。
- 广播：A 发广播，B/C 收到，样式与私聊区分。
- 文件传输：小文件、100MB 文件、拒绝接收、传输中断、保存路径异常、UI 不冻结。
