# 贡献指南

感谢你愿意帮助改进 SoundPort。

## 开始之前

- 对缺陷或兼容性问题，请先搜索现有 Issue。
- 较大的功能或架构调整，请先创建 Feature Request 讨论范围。
- 安全漏洞不要提交公开 Issue，请遵循 [SECURITY.md](SECURITY.md)。

## 开发环境

需要 Windows 11、.NET 10 SDK，以及包含 Managed Desktop、Universal 和
Windows App SDK C# 组件的 Visual Studio 2026。

```powershell
git clone <repository-url>
cd SoundPort
dotnet restore SoundPort.csproj
dotnet build SoundPort.csproj -p:Platform=x64
```

项目依赖真实的 Windows 蓝牙栈。涉及设备发现、A2DP 连接或托盘交互的修改，
请在 Pull Request 中说明测试过的 Windows 版本、蓝牙适配器和手机型号。

## 提交修改

1. 从最新的 `main` 创建功能分支。
2. 保持修改聚焦，不混入无关重构或格式化。
3. 遵循现有 C#、XAML 和项目目录风格。
4. 构建受影响的目标，确认没有新增警告。
5. 为用户可见行为同步更新 README 或相关说明。
6. 创建 Pull Request，并填写验证步骤和剩余风险。

推荐使用清晰的提交信息，例如：

```text
fix: handle Bluetooth device removal while connecting
feat: add audio output selection
docs: document tested Android devices
```

## 缺陷报告

一份有效的兼容性或缺陷报告应尽量包含：

- Windows 版本与系统内部版本
- 蓝牙适配器型号与驱动版本
- 安卓手机型号与 Android 版本
- 配对、连接和播放的完整复现步骤
- 实际结果、预期结果和错误信息

请删除蓝牙设备地址、用户名、路径中的个人信息和其他敏感数据。

## 许可证

提交贡献即表示你同意将贡献按本项目的 [MIT License](LICENSE) 发布。
