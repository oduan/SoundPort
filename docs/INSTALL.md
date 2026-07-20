# 安装 SoundPort

SoundPort 的 GitHub Release 使用项目自签名证书签署。Windows 默认不会信任未知的
自签名发布者，因此首次安装需要同时安装公开证书。

## 推荐方式：单文件安装器

1. 从 GitHub Release 下载 `SoundPort-v*-x64-Setup.exe`。
2. 双击运行并接受管理员权限请求。
3. 安装器会自动导入项目公开证书、安装 Windows App Runtime 依赖和 SoundPort。

安装完成后，可以从开始菜单启动 SoundPort。卸载时在 Windows“设置 → 应用 →
已安装的应用”中找到 SoundPort。

> [!NOTE]
> 项目目前使用自签名证书。首次运行安装器时，Windows Defender SmartScreen
> 仍可能显示“未知发布者”提示。请只从项目的官方 GitHub Release 下载安装器。

## ZIP 安装方式

如果需要检查或手动控制每个安装步骤，可以下载并解压 `SoundPort-v*-x64.zip`。
确认压缩包中包含：

   - `SoundPort-v*-x64.msix`
   - `SoundPort.cer`
   - `Install-SoundPort.ps1`
   - `Dependencies/`（Windows App Runtime）

在解压目录打开 PowerShell 并执行：

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-SoundPort.ps1
```

脚本会请求管理员权限，把 `SoundPort.cer` 导入本地计算机的 `TrustedPeople` 证书存储，
然后安装同目录中的 MSIX 及 `Dependencies/` 内的 Windows App Runtime。

## 手动安装

也可以双击 `SoundPort.cer`，将证书安装到“本地计算机 → 受信任人”，然后双击 MSIX。

只应安装从官方 SoundPort GitHub Release 下载的证书和安装包。证书指纹可以使用：

```powershell
Get-PfxCertificate .\SoundPort.cer | Select-Object Subject, Thumbprint
```

进行核对。
