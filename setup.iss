; Inno Setup Script for Switch Keyboard Layout Converter
; Arabic/English installer. It installs one application executable and exposes
; a visible Startup Apps entry that starts it through a highest-privilege task.

#define MyAppName "Switch"
#define MyAppVersion "2.0.3"
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
; Switch elevates itself when launched normally and uses the scheduled task at startup.
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

english.LaunchApp=Launch Switch now
arabic.LaunchApp=تشغيل برنامج Switch الآن
english.StartupTaskError=Switch was installed, but Windows could not configure automatic startup. Please run the installer again as administrator.
arabic.StartupTaskError=تم تثبيت Switch، لكن تعذر إعداد التشغيل التلقائي مع ويندوز. يرجى تشغيل المثبّت مرة أخرى بصلاحية المسؤول.

english.RunAtStartup=Launch Switch automatically at Windows startup
arabic.RunAtStartup=تشغيل برنامج Switch تلقائياً عند بدء تشغيل ويندوز

english.AdminNoticeTitle=Administrator permission
arabic.AdminNoticeTitle=صلاحية المسؤول
english.AdminNotice=Switch requires administrator permission so it can work with applications that are also running as administrator. The permission request shown by Windows is expected.
arabic.AdminNotice=يحتاج Switch إلى صلاحية المسؤول لكي يعمل مع البرامج التي تعمل بصلاحية المسؤول أيضاً. طلب الصلاحية الذي يظهر من ويندوز أمر متوقع.

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"
Name: "startup"; Description: "{cm:RunAtStartup}"

[Files]
Source: "Switch\bin\Release\Switch.exe"; DestDir: "{app}"; DestName: "{#MyAppExeName}"; Flags: ignoreversion
Source: "logo.ico"; DestDir: "{app}"; Flags: ignoreversion

[InstallDelete]
; Remove the helper shipped by v2.0.2 so upgrades leave one application exe.
Type: files; Name: "{app}\SwitchStartup.exe"

[UninstallDelete]
; Also clean the helper if an older installation is removed directly.
Type: files; Name: "{app}\SwitchStartup.exe"

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
; The main executable handles --startup without elevation, then invokes the
; elevated scheduled task. This keeps a visible Startup Apps entry and one exe.
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--startup"; WorkingDir: "{app}"; IconFilename: "{app}\logo.ico"; Tasks: startup

[Run]
; ShellExecute allows Switch.exe to request elevation when launched normally.
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent shellexec

[Code]
const
  StartupTaskName = 'Switch';
  StartupTaskRootName = '\Switch';
  StartupTaskFallbackName = '\Microsoft\Windows\Switch';
  StartupRegistryKey = 'Software\Microsoft\Windows\CurrentVersion\Run';
  StartupApprovedFolderKey = 'Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder';

var
  AdminNoticePage: TOutputMsgWizardPage;

function RunSchtasks(const Parameters: String; var ResultCode: Integer): Boolean;
begin
  Result := Exec(
    ExpandConstant('{sys}\schtasks.exe'), Parameters, '', SW_HIDE,
    ewWaitUntilTerminated, ResultCode);
end;

function XmlEscape(const Value: String): String;
begin
  Result := Value;
  StringChangeEx(Result, '&', '&amp;', True);
  StringChangeEx(Result, '<', '&lt;', True);
  StringChangeEx(Result, '>', '&gt;', True);
end;

function BuildStartupTaskXml: String;
var
  ApplicationPath: String;
  WorkingDirectory: String;
begin
  ApplicationPath := XmlEscape(ExpandConstant('{app}\{#MyAppExeName}'));
  WorkingDirectory := XmlEscape(ExpandConstant('{app}'));
  Result :=
    '<?xml version="1.0"?>' + #13#10 +
    '<Task version="1.3" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">' + #13#10 +
    '  <RegistrationInfo>' + #13#10 +
    '    <Description>Starts Switch automatically when the user signs in.</Description>' + #13#10 +
    '  </RegistrationInfo>' + #13#10 +
    '  <Triggers>' + #13#10 +
    '    <LogonTrigger><Enabled>true</Enabled></LogonTrigger>' + #13#10 +
    '  </Triggers>' + #13#10 +
    '  <Principals>' + #13#10 +
    '    <Principal id="Author">' + #13#10 +
    '      <LogonType>InteractiveToken</LogonType>' + #13#10 +
    '      <RunLevel>HighestAvailable</RunLevel>' + #13#10 +
    '    </Principal>' + #13#10 +
    '  </Principals>' + #13#10 +
    '  <Settings>' + #13#10 +
    '    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>' + #13#10 +
    '    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>' + #13#10 +
    '    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>' + #13#10 +
    '    <AllowHardTerminate>true</AllowHardTerminate>' + #13#10 +
    '    <StartWhenAvailable>true</StartWhenAvailable>' + #13#10 +
    '    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>' + #13#10 +
    '    <RestartOnFailure><Interval>PT1M</Interval><Count>999</Count></RestartOnFailure>' + #13#10 +
    '  </Settings>' + #13#10 +
    '  <Actions Context="Author">' + #13#10 +
    '    <Exec>' + #13#10 +
    '      <Command>' + ApplicationPath + '</Command>' + #13#10 +
    '      <WorkingDirectory>' + WorkingDirectory + '</WorkingDirectory>' + #13#10 +
    '    </Exec>' + #13#10 +
    '  </Actions>' + #13#10 +
    '</Task>';
end;

function ConfigureStartupTaskAtPath(const TaskName, XmlPath: String): Boolean;
var
  ResultCode: Integer;
begin
  Result := RunSchtasks(
    '/Create /TN "' + TaskName + '" /XML "' + XmlPath + '" /F',
    ResultCode) and (ResultCode = 0);
  if not Result then
    Log('Could not create startup task ' + TaskName + '. schtasks exit code: ' + IntToStr(ResultCode));
end;

function ConfigureStartupTask: Boolean;
var
  XmlPath: String;
begin
  Result := False;
  XmlPath := ExpandConstant('{tmp}\SwitchStartup.xml');
  if not SaveStringToFile(XmlPath, BuildStartupTaskXml, False) then
  begin
    Log('Could not write the startup task XML.');
    exit;
  end;
  try
    { schtasks.exe is used instead of late-bound Task Scheduler COM calls so
      registration behaves consistently on Windows installations where the
      COM registration API rejects optional empty parameters. }
    Result := ConfigureStartupTaskAtPath(StartupTaskRootName, XmlPath);
    if not Result then
      Result := ConfigureStartupTaskAtPath(StartupTaskFallbackName, XmlPath);
  finally
    DeleteFile(XmlPath);
  end;
end;

procedure RemoveStartupTask(const TaskName: String);
var
  ResultCode: Integer;
begin
  { Deleting a missing task is intentionally harmless during upgrades and uninstall. }
  RunSchtasks('/Delete /TN "' + TaskName + '" /F', ResultCode);
end;

procedure RemoveStartupTasks;
begin
  { Remove both locations so upgrades clean up an older installation. }
  RemoveStartupTask(StartupTaskRootName);
  RemoveStartupTask(StartupTaskFallbackName);
end;

procedure RemoveLegacyStartupEntries;
begin
  { Older builds used a Run value and an earlier startup shortcut. Remove both
    so Windows does not retain a stale or duplicate startup registration. }
  RegDeleteValue(HKEY_CURRENT_USER, StartupRegistryKey, StartupTaskName);
  RegDeleteValue(HKEY_CURRENT_USER, StartupApprovedFolderKey, StartupTaskName + '.lnk');
end;

procedure InitializeWizard;
begin
  AdminNoticePage := CreateOutputMsgPage(
    wpWelcome,
    CustomMessage('AdminNoticeTitle'),
    CustomMessage('AdminNoticeTitle'),
    CustomMessage('AdminNotice'));
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    RemoveLegacyStartupEntries;
    { Remove registrations left by previous installer versions before creating
      the new task, so both task locations cannot launch duplicate instances. }
    RemoveStartupTasks;

    if WizardIsTaskSelected('startup') then
    begin
      if not ConfigureStartupTask then
      begin
        MsgBox(CustomMessage('StartupTaskError'), mbError, MB_OK);
        Abort;
      end;
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
  begin
    RemoveStartupTasks;
    RemoveLegacyStartupEntries;
  end;
end;
