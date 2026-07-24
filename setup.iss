; Inno Setup Script for Switch Keyboard Layout Converter
; Custom designed to support Arabic and English installations, non-admin installation, desktop icon, and startup launch.

#define MyAppName "Switch"
#define MyAppVersion "1.1"
#define MyAppPublisher "@ahmedjamalzaki"
#define MyAppExeName "switch.exe"

[Setup]
; Unique identifier for the installation
AppId={{E6F74D28-2A2B-4BC2-91B1-6D98CFB443D0}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppCopyright=Copyright © @ahmedjamalzaki
DefaultDirName={localappdata}\Programs\{#MyAppName}
UsePreviousAppDir=no
DisableProgramGroupPage=yes
; Install for the current user only (no admin rights required)
PrivilegesRequired=lowest
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

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"
Name: "startup"; Description: "{cm:RunAtStartup}"

[Files]
Source: "dist\switch.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "logo.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent
