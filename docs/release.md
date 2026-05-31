# LanTalk 发布说明

## 适用范围
- 适用于当前 `main` 基线的 Windows `win-x64` 本地发布验证。
- 主线运行时仍为 `.NET 10`；架构上保留对后续 `.NET 11` 演进的接口扩展点。

## 发布前检查
- 先串行执行以下命令，避免 Windows 上并行生成导致 `MSB3713` 或 `CS2012` 文件锁：

```bash
dotnet build LanTalk.sln -v:minimal
dotnet test LanTalk.sln -v:minimal
```

## 常规 Release 发布
```bash
dotnet publish src/LanTalk.App/LanTalk.App.csproj -c Release -r win-x64 --self-contained false -v:minimal
```

- 输出目录：
  - `src/LanTalk.App/bin/Release/net10.0/win-x64/publish/`
- 当前验证结果：
  - 2026-06-01 本机实测成功，命令可直接完成发布。

## Native AOT 发布
```bash
dotnet publish src/LanTalk.App/LanTalk.App.csproj -c Release -r win-x64 -p:PublishAot=true -v:minimal
```

- 输出目录：
  - `src/LanTalk.App/bin/Release/net10.0/win-x64/publish/`
- 当前验证结果：
  - 2026-06-01 本机实测成功，AOT 产物可生成。

## 已知警告
- Native AOT 发布仍会出现 Avalonia DataGrid 的既有警告：
  - `IL2104: Assembly 'Avalonia.Controls.DataGrid' produced trim warnings`
  - `IL3053: Assembly 'Avalonia.Controls.DataGrid' produced AOT analysis warnings`
- 当前评估：
  - 这两条警告不阻塞发布产物生成。
  - 代码侧本轮未新增新的 trim / AOT 告警。

## 发布后建议验证
- 启动发布目录中的 `LanTalk.App.exe`，确认主窗口、UDP/TCP 监听和 SQLite 初始化正常。
- 验证设置保存、聊天记录加载、文件接收目录可写。
- 如需课堂演示，优先使用常规 Release 产物；AOT 产物用于性能与部署验证。
