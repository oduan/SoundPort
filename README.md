# SoundPort

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
![Platform](https://img.shields.io/badge/platform-Windows%2011-0078D4)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)

让 Windows 电脑成为安卓手机的蓝牙音箱。

SoundPort is a Windows Bluetooth audio receiver that lets an Android phone stream
audio to a PC over Bluetooth A2DP.

> [!IMPORTANT]
> SoundPort 目前处于早期开发阶段。不同蓝牙芯片、驱动和安卓设备的兼容性可能不同，
> 欢迎通过 Issue 提交测试结果。

## 功能

- 发现支持远程音频播放的已配对蓝牙设备
- 通过 Windows A2DP Sink 接收安卓手机音频
- 连接、断开与连接状态提示
- 记住上次选择的手机
- 关闭主窗口后继续在系统托盘运行
- 通过托盘连接、断开、恢复窗口或退出
- Windows Explorer 重启后自动恢复托盘图标
- 支持 Windows 浅色、深色和高对比度主题

## 工作原理

SoundPort 使用 Windows 提供的
[`AudioPlaybackConnection`](https://learn.microsoft.com/windows/apps/develop/media-playback/enable-remote-audio-playback)
API 启用系统内置的 Bluetooth Classic A2DP Sink。蓝牙传输、音频解码和系统音频输出
均由 Windows 处理，项目不包含自定义蓝牙驱动、虚拟声卡或安卓端应用。

```text
安卓手机（A2DP Source）
          │ Bluetooth Classic / A2DP
          ▼
Windows 蓝牙音频栈
          │
          ▼
电脑当前默认音频输出
```

## 系统要求

运行：

- Windows 11
- 支持 Bluetooth Classic 的蓝牙适配器及正常工作的厂商驱动
- 支持 A2DP Source 的安卓手机

开发：

- .NET 10 SDK
- Visual Studio 2026
- Managed Desktop、Universal 和 Windows App SDK C# 组件
- 已启用 Windows 开发者模式

## 使用方法

1. 打开 Windows“设置 → 蓝牙和设备”，配对安卓手机。
2. 启动 SoundPort，等待设备扫描完成。
3. 选择手机并点击“连接”。
4. 在手机的蓝牙输出设备中选择这台电脑。
5. 在手机上播放音乐。

关闭主窗口不会退出 SoundPort。应用会继续在系统托盘运行：

- 双击托盘图标：恢复主窗口
- 连接：连接上次选择的手机；未选择时使用列表中的第一台设备
- 断开连接：释放当前蓝牙音频连接
- 退出：断开蓝牙、移除托盘图标并结束进程

## 从源码构建

克隆仓库后，在项目目录执行：

```powershell
dotnet restore SoundPort.csproj
dotnet build SoundPort.csproj -p:Platform=x64
```

项目使用 packaged MSIX 开发模型。可以从 Visual Studio 选择 Package 配置启动，
也可以使用项目包含的 Windows App Development CLI 支持：

```powershell
dotnet run --project SoundPort.csproj --property:Platform=x64
```

首次启动会为本地开发注册应用包，并可能安装匹配的 Windows App Runtime。

## 项目结构

```text
SoundPort/
├─ Models/       设备与接收器状态
├─ Services/     蓝牙接收、设置和系统托盘
├─ Interop/      Shell_NotifyIcon 与 Win32 消息窗口
├─ Assets/       应用图标和 MSIX 资源
├─ MainPage.*    设备列表与连接界面
└─ MainWindow.*  应用窗口与关闭到托盘生命周期
```

## 当前限制

- 必须先通过 Windows 设置完成手机配对
- 音频延迟由手机、蓝牙适配器和驱动决定，不适合实时监听
- 当前使用 Windows 默认音频输出，应用内暂不提供输出设备选择
- 应用退出后 A2DP Sink 会被释放，手机音频连接随之结束
- 尚未覆盖多台手机同时播放、媒体封面或播放控制

## 参与贡献

Issue、兼容性报告和 Pull Request 都很欢迎。开始前请阅读
[贡献指南](CONTRIBUTING.md)和[行为准则](CODE_OF_CONDUCT.md)。

提交蓝牙兼容性问题时，请附上 Windows 版本、蓝牙适配器型号、驱动版本、手机型号
以及可复现步骤，但不要公开设备地址、日志中的个人信息或其他敏感数据。

## 安全

请不要在公开 Issue 中披露安全漏洞。报告方式见[安全策略](SECURITY.md)。

## 许可证

本项目基于 [MIT License](LICENSE) 开源。
