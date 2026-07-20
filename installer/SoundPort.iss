#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif

#ifndef ReleaseTag
  #define ReleaseTag "dev"
#endif

#ifndef PayloadDir
  #error PayloadDir must point to the prepared release bundle.
#endif

#ifndef OutputDir
  #define OutputDir "."
#endif

#define AppAumid "08BCBD0D-9A71-4EF7-81C1-DBE3DE79A631_87ep7ayn4748y!App"

[Setup]
AppId={{E381AF4D-120B-4F1D-B955-B5C9E0E5A9C2}
AppName=SoundPort
AppVersion={#MyAppVersion}
AppPublisher=SoundPort contributors
AppPublisherURL=https://github.com/oduan/SoundPort
AppSupportURL=https://github.com/oduan/SoundPort/issues
AppUpdatesURL=https://github.com/oduan/SoundPort/releases
CreateAppDir=no
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir={#OutputDir}
OutputBaseFilename=SoundPort-{#ReleaseTag}-x64-Setup
SetupIconFile=..\Assets\AppIcon.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
Uninstallable=no

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#PayloadDir}\SoundPort-{#ReleaseTag}-x64.msix"; DestDir: "{tmp}\SoundPort"; DestName: "SoundPort.msix"; Flags: ignoreversion
Source: "{#PayloadDir}\SoundPort.cer"; DestDir: "{tmp}\SoundPort"; Flags: ignoreversion
Source: "{#PayloadDir}\Install-SoundPort.ps1"; DestDir: "{tmp}\SoundPort"; Flags: ignoreversion
Source: "{#PayloadDir}\Dependencies\*.msix"; DestDir: "{tmp}\SoundPort\Dependencies"; Flags: ignoreversion skipifsourcedoesntexist

[Run]
Filename: "explorer.exe"; Parameters: "shell:AppsFolder\{#AppAumid}"; Description: "{cm:LaunchProgram,SoundPort}"; Flags: postinstall nowait skipifsilent unchecked

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  PowerShellPath: String;
  ScriptPath: String;
  Parameters: String;
  ResultCode: Integer;
begin
  if CurStep <> ssPostInstall then
    Exit;

  PowerShellPath := ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe');
  ScriptPath := ExpandConstant('{tmp}\SoundPort\Install-SoundPort.ps1');
  Parameters :=
    '-NoProfile -NonInteractive -ExecutionPolicy Bypass -File "' +
    ScriptPath + '" -PackagePath "' +
    ExpandConstant('{tmp}\SoundPort\SoundPort.msix') + '"';

  WizardForm.StatusLabel.Caption := '正在安装 SoundPort 及其运行时依赖...';

  if not Exec(
    PowerShellPath,
    Parameters,
    '',
    SW_HIDE,
    ewWaitUntilTerminated,
    ResultCode) then
  begin
    RaiseException('无法启动 SoundPort 安装程序。');
  end;

  if ResultCode <> 0 then
  begin
    RaiseException(
      'SoundPort 安装失败，PowerShell 退出代码：' +
      IntToStr(ResultCode));
  end;
end;
