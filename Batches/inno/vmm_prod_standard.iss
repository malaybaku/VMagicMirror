; #define ReposFolder "C:\Path\To\Repository\"
; #define MyAppVersion "1.9.0"
#define MyAppName "VMagicMirror"
#define MyAppEdition "Standard"
#define MyAppPublisher "baku_dreameater"
#define MyAppURL "https://malaybaku.github.io/VMagicMirror/"
#define MyAppExeName "VMagicMirror.exe"
#define SrcFolderName "Bin_Standard"
; Below Content is almost same between 4 iss files

[Setup]
AppId={{D1069D29-D24C-4D24-9E10-C68ED06BF019}
AppName={#MyAppName}_{#MyAppEdition}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequiredOverridesAllowed=dialog
OutputDir={#ReposFolder}\Installers\Prod
OutputBaseFilename=VMM_{#MyAppVersion}_{#MyAppEdition}_Installer
SetupIconFile={#ReposFolder}\Installers\_icon\vmm.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#ReposFolder}\{#SrcFolderName}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#ReposFolder}\{#SrcFolderName}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

