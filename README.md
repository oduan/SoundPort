# SoundPort

SoundPort 是一个 Windows 11 蓝牙音频接收器。它使用 Windows 的
`AudioPlaybackConnection` API，让已配对的安卓手机把电脑当作蓝牙音箱。

## 使用

1. 在 Windows 11 的“蓝牙和设备”设置中配对安卓手机。
2. 启动 SoundPort，等待设备列表完成扫描。
3. 选择手机并点击“连接”。
4. 在手机的蓝牙输出设备中选择这台电脑，然后播放音乐。

关闭主窗口后，SoundPort 会继续在系统托盘运行：

- 双击托盘图标：恢复主窗口。
- 连接：连接上次选择的手机；没有选择时使用列表中的第一台设备。
- 断开连接：释放当前蓝牙音频连接。
- 退出：断开蓝牙、移除托盘图标并结束程序。

## 开发

要求：

- Windows 11
- .NET 10 SDK
- Visual Studio 2026 的 Managed Desktop、Universal 和 Windows App SDK C# 组件

本地 x64 构建：

```powershell
dotnet build SoundPort.csproj -p:Platform=x64
```

项目使用 packaged MSIX 开发模型。可从 Visual Studio 使用 Package 配置启动，
或者使用项目包含的 Windows App Development CLI 支持：

```powershell
dotnet run --project SoundPort.csproj --property:Platform=x64
```

## 主要模块

- `Services/AudioReceiverService.cs`：设备发现、A2DP Sink 启用、连接和状态管理。
- `Services/TrayIconService.cs`：原生通知区域图标、菜单和 Explorer 重启恢复。
- `Interop/`：`Shell_NotifyIcon`、隐藏消息窗口和菜单所需的 Win32 互操作。
- `MainWindow.xaml.cs`：关闭到托盘和窗口恢复。
