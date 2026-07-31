; Inno Setup Script for Switch Keyboard Layout Converter
; Arabic/English installer. It installs an elevated application and can start it
; automatically through a highest-privilege scheduled task.

#define MyAppName "Switch"
#define MyAppVersion "1.4"
#define MyAppPublisher "@ahmedjamalzaki"
#define MyAppExeName "Switch.exe"

[Setup]
; Unique identifier for the installation
AppId={{E6F74D28-2A2B-4BC2-91B1-6D98CFB443D0}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppCopyright=Copyright © @ahmedjamalzaki
DefaultDirName={autopf}\{#MyAppName}
UsePreviousAppDir=no
DisableProgramGroupPage=yes
ShowLanguageDialog=yes
; Required: Switch uses a manifest that always runs with administrator rights.
PrivilegesRequired=admin
OutputBaseFilename=Switch_Setup
SetupIconFile=logo.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
english.CreateDesktopIcon=Create a &desktop shortcut
arabic.CreateDesktopIcon=إنشاء اختصار على &سطح المكتب

english.RunAtStartup=Launch Switch automatically at Windows startup
arabic.RunAtStartup=تشغيل برنامج Switch تلقائياً عند بدء تشغيل ويندوز

english.LaunchApp=Launch Switch now
arabic.LaunchApp=تشغيل برنامج Switch الآن

english.AdminNoticeTitle=Administrator permission
arabic.AdminNoticeTitle=صلاحية المسؤول
english.AdminNotice=Switch requires administrator permission so it can work with applications that are also running as administrator. The permission request shown by Windows is expected.
arabic.AdminNotice=يحتاج Switch إلى صلاحية المسؤول لكي يعمل مع البرامج التي تعمل بصلاحية المسؤول أيضاً. طلب الصلاحية الذي يظهر من ويندوز أمر متوقع.

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"
Name: "startup"; Description: "{cm:RunAtStartup}"

[Files]
Source: "Switch\bin\Release\Switch.admin.exe"; DestDir: "{app}"; DestName: "{#MyAppExeName}"; Flags: ignoreversion
Source: "logo.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; A Startup-folder shortcut cannot silently elevate. A scheduled task can run at
; the highest privilege after the user has approved this administrator installer.
Filename: "{cmd}"; Parameters: "/c schtasks.exe /create /tn ""Switch"" /tr """"""{app}\{#MyAppExeName}"""""" /sc onlogon /rl highest /f"; Flags: runhidden; Tasks: startup
; ShellExecute is required here because Switch.exe has requireAdministrator in
; its manifest. CreateProcess would fail with Windows error 740.
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent shellexec

[UninstallRun]
Filename: "{cmd}"; Parameters: "/c schtasks.exe /delete /tn ""Switch"" /f"; Flags: runhidden; RunOnceId: "RemoveSwitchStartupTask"

[Code]
var
  AdminNoticePage: TOutputMsgWizardPage;

procedure InitializeWizard;
begin
  AdminNoticePage := CreateOutputMsgPage(
    wpWelcome,
    CustomMessage('AdminNoticeTitle'),
    CustomMessage('AdminNoticeTitle'),
    CustomMessage('AdminNotice'));
end;
