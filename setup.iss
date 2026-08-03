; Inno Setup Script for Switch Keyboard Layout Converter
; Arabic/English installer. It installs an elevated application and starts it
; automatically through a highest-privilege scheduled task.

#define MyAppName "Switch"
#define MyAppVersion "2.0.1"
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

english.LaunchApp=Launch Switch now
arabic.LaunchApp=تشغيل برنامج Switch الآن
english.StartupTaskError=Switch was installed, but Windows could not configure automatic startup. Please run the installer again as administrator.
arabic.StartupTaskError=تم تثبيت Switch، لكن تعذر إعداد التشغيل التلقائي مع ويندوز. يرجى تشغيل المثبّت مرة أخرى بصلاحية المسؤول.

english.AdminNoticeTitle=Administrator permission
arabic.AdminNoticeTitle=صلاحية المسؤول
english.AdminNotice=Switch requires administrator permission so it can work with applications that are also running as administrator. The permission request shown by Windows is expected.
arabic.AdminNotice=يحتاج Switch إلى صلاحية المسؤول لكي يعمل مع البرامج التي تعمل بصلاحية المسؤول أيضاً. طلب الصلاحية الذي يظهر من ويندوز أمر متوقع.

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"

[Files]
Source: "Switch\bin\Release\Switch.exe"; DestDir: "{app}"; DestName: "{#MyAppExeName}"; Flags: ignoreversion
Source: "logo.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; ShellExecute is required here because Switch.exe has requireAdministrator in
; its manifest. CreateProcess would fail with Windows error 740.
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent shellexec

[Code]
const
  TaskCreateOrUpdate = 6;
  TaskLogonInteractiveToken = 3;
  TaskRunLevelHighest = 1;
  TaskTriggerLogon = 9;
  TaskActionExec = 0;
  StartupTaskName = 'Switch';
  StartupTaskFallbackFolder = '\Microsoft\Windows\Switch';

var
  AdminNoticePage: TOutputMsgWizardPage;

function TryGetStartupTaskFolder(TaskService: Variant; var TaskFolder: Variant): Boolean;
var
  ParentFolder: Variant;
begin
  Result := False;
  try
    { The normal Windows root folder is the preferred location. }
    TaskFolder := TaskService.GetFolder('\');
    Result := True;
    exit;
  except
    { Some machines have a damaged or inaccessible root task folder. }
  end;

  try
    { Keep a fallback task inside a standard Windows task namespace. }
    try
      TaskFolder := TaskService.GetFolder(StartupTaskFallbackFolder);
    except
      ParentFolder := TaskService.GetFolder('\Microsoft\Windows');
      TaskFolder := ParentFolder.CreateFolder('Switch', '');
    end;
    Result := True;
  except
    Result := False;
  end;
end;

function ConfigureStartupTask: Boolean;
var
  TaskService: Variant;
  TaskFolder: Variant;
  TaskDefinition: Variant;
  Trigger: Variant;
  Action: Variant;
begin
  Result := False;
  try
    TaskService := CreateOleObject('Schedule.Service');
    TaskService.Connect;
    if not TryGetStartupTaskFolder(TaskService, TaskFolder) then
      exit;
    TaskDefinition := TaskService.NewTask(0);

    TaskDefinition.RegistrationInfo.Description :=
      'Starts Switch automatically when the user signs in.';
    TaskDefinition.Principal.LogonType := TaskLogonInteractiveToken;
    TaskDefinition.Principal.RunLevel := TaskRunLevelHighest;
    TaskDefinition.Settings.Enabled := True;
    TaskDefinition.Settings.StartWhenAvailable := True;

    Trigger := TaskDefinition.Triggers.Create(TaskTriggerLogon);
    Trigger.Enabled := True;

    Action := TaskDefinition.Actions.Create(TaskActionExec);
    Action.Path := ExpandConstant('{app}\{#MyAppExeName}');

    TaskFolder.RegisterTaskDefinition(
      StartupTaskName, TaskDefinition, TaskCreateOrUpdate, '', '',
      TaskLogonInteractiveToken, '');
    Result := True;
  except
    Result := False;
  end;
end;

procedure RemoveStartupTaskFromFolder(TaskFolder: Variant);
begin
  try
    TaskFolder.DeleteTask(StartupTaskName, 0);
  except
    { The task may already be absent. }
  end;
end;

procedure RemoveStartupTasks;
var
  TaskService: Variant;
  TaskFolder: Variant;
  ParentFolder: Variant;
begin
  try
    TaskService := CreateOleObject('Schedule.Service');
    TaskService.Connect;

    { Remove both locations so upgrades clean up an older installation. }
    try
      TaskFolder := TaskService.GetFolder('\');
      RemoveStartupTaskFromFolder(TaskFolder);
    except
      { The root folder may be unavailable on this machine. }
    end;

    try
      TaskFolder := TaskService.GetFolder(StartupTaskFallbackFolder);
      RemoveStartupTaskFromFolder(TaskFolder);
    except
      { The fallback task may already be absent. }
    end;

    try
      ParentFolder := TaskService.GetFolder('\Microsoft\Windows');
      ParentFolder.DeleteFolder('Switch', 0);
    except
      { Leave the folder in place if it is not empty or cannot be removed. }
    end;
  except
    { Task Scheduler may be unavailable during uninstall. }
  end;
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
    if not ConfigureStartupTask then
    begin
      MsgBox(CustomMessage('StartupTaskError'), mbError, MB_OK);
      Abort;
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
    RemoveStartupTasks;
end;
