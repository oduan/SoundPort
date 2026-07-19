# 安装 SoundPort

SoundPort 的 GitHub Release 使用项目自签名证书签署。Windows 默认不会信任未知的
自签名发布者，因此首次安装需要同时安装公开证书。

## 推荐方式

1. 下载并解压 `SoundPort-v*-x64.zip`。
2. 确认压缩包中包含：
   - `SoundPort-v*-x64.msix`
   - `SoundPort.cer`
   - `Install-SoundPort.ps1`
   - `Dependencies/`（Windows App Runtime）
3. 在解压目录打开 PowerShell。
4. 执行：

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
