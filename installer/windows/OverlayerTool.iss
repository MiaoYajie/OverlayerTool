; OverlayerTool Windows 安装包脚本（Inno Setup 6）
; 用法见 installer/README.md

#ifndef PublishDir
  #define PublishDir "..\..\src\OverlayerTool.App\bin\Release\net8.0\win-x64\publish"
#endif

#define AppName "OverlayerTool"
#define AppVersion "0.0.3"
#define AppPublisher "myj"
#define AppURL "https://github.com/MiaoYajie/OverlayerTool"
#define AppExeName "OverlayerTool.App.exe"
#define AppId "{{A7B3C4D5-E6F7-4890-ABCD-EF1234567890}"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
OutputDir=..\..\dist
OutputBaseFilename=OverlayerTool-Setup-{#AppVersion}-win-x64
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=no
UninstallDisplayIcon={app}\{#AppExeName}
; 若已添加 src/OverlayerTool.App/Assets/app.ico，可取消下一行注释
SetupIconFile=..\..\src\OverlayerTool.App\Assets\app.ico
LicenseFile=..\..\LICENSE

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; 请先 dotnet publish（见 build-installer.ps1），将 publish 目录全部打包
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "启动 {#AppName}"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
begin
  if not DirExists(ExpandConstant('{#PublishDir}')) then
  begin
    MsgBox('未找到发布目录：' + ExpandConstant('{#PublishDir}') + #13#10 +
           '请先运行 installer/windows/build-installer.ps1 或手动 dotnet publish。',
           mbError, MB_OK);
    Result := False;
  end
  else if not FileExists(ExpandConstant('{#PublishDir}\{#AppExeName}')) then
  begin
    MsgBox('发布目录中缺少主程序 {#AppExeName}。' + #13#10 +
           '请确认已使用 --self-contained true 完成 publish。',
           mbError, MB_OK);
    Result := False;
  end
  else
    Result := True;
end;
