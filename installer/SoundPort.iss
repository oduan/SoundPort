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
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#PayloadDir}\SoundPort-{#ReleaseTag}-x64.msix"; DestDir: "{tmp}\SoundPort"; DestName: "SoundPort.msix"; Flags: dontcopy noencryption
Source: "{#PayloadDir}\SoundPort.cer"; DestDir: "{tmp}\SoundPort"; Flags: dontcopy noencryption
Source: "{#PayloadDir}\Install-SoundPort.ps1"; DestDir: "{tmp}\SoundPort"; Flags: dontcopy noencryption
Source: "{#PayloadDir}\Dependencies\*.msix"; DestDir: "{tmp}\SoundPort\Dependencies"; Flags: dontcopy noencryption skipifsourcedoesntexist

[Run]
Filename: "explorer.exe"; Parameters: "shell:AppsFolder\{#AppAumid}"; Description: "{cm:LaunchProgram,SoundPort}"; Flags: postinstall nowait skipifsilent unchecked

[Code]
function JoinOutputLines(const Lines: TArrayOfString): String;
var
  I: Integer;
begin
  Result := '';
  for I := 0 to GetArrayLength(Lines) - 1 do
  begin
    if Result <> '' then
      Result := Result + #13#10;
    Result := Result + Lines[I];
  end;

  if Length(Result) > 4000 then
    Result := Copy(Result, 1, 4000) + #13#10 + '（错误信息已截断）';
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  PowerShellPath: String;
  ScriptPath: String;
  Parameters: String;
  ErrorDetails: String;
  ResultCode: Integer;
  Output: TExecOutput;
begin
  Result := '';

  try
    ExtractTemporaryFiles('{tmp}\SoundPort\SoundPort.msix');
    ExtractTemporaryFiles('{tmp}\SoundPort\SoundPort.cer');
    ExtractTemporaryFiles('{tmp}\SoundPort\Install-SoundPort.ps1');

    try
      ExtractTemporaryFiles('{tmp}\SoundPort\Dependencies\*.msix');
    except
      Log('No bundled dependency package was extracted: ' +
        GetExceptionMessage);
    end;

    PowerShellPath :=
      ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe');
    ScriptPath :=
      ExpandConstant('{tmp}\SoundPort\Install-SoundPort.ps1');
    Parameters :=
      '-NoProfile -NonInteractive -ExecutionPolicy Bypass -File "' +
      ScriptPath + '" -PackagePath "' +
      ExpandConstant('{tmp}\SoundPort\SoundPort.msix') + '"';

    WizardForm.StatusLabel.Caption :=
      '正在安装 SoundPort 及其运行时依赖...';

    if not ExecAndCaptureOutput(
      PowerShellPath,
      Parameters,
      '',
      SW_SHOWNORMAL,
      ewWaitUntilTerminated,
      ResultCode,
      Output) then
    begin
      Result :=
        '无法启动 SoundPort 安装程序：' +
        SysErrorMessage(ResultCode);
      Exit;
    end;

    if ResultCode <> 0 then
    begin
      ErrorDetails := JoinOutputLines(Output.StdErr);
      if ErrorDetails = '' then
        ErrorDetails := JoinOutputLines(Output.StdOut);
      if ErrorDetails = '' then
        ErrorDetails := 'PowerShell 退出代码：' + IntToStr(ResultCode);

      Result :=
        'SoundPort 安装失败。' + #13#10 + #13#10 +
        ErrorDetails;
    end;
  except
    Result :=
      '准备 SoundPort 安装文件失败：' + GetExceptionMessage;
  end;
end;
