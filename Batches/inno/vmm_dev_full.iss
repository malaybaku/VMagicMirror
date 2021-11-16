; #define ReposFolder "C:\Path\To\Repository\"
; #define MyAppVersion "1.9.0"
#define MyAppName "VMagicMirror(dev)"
#define MyAppEdition "Full"
#define MyAppPublisher "baku_dreameater"
#define MyAppURL "https://malaybaku.github.io/VMagicMirror/"
#define MyAppExeName "VMagicMirror.exe"
#define SrcFolderName "Bin_Dev"
; Below Content is almost same between 4 iss files, except AppId

[Setup]
AppId={{4F4D50B4-6B73-4B89-9AE2-C0CAD92C537D}
AppName={#MyAppName}_{#MyAppEdition}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequiredOverridesAllowed=dialog
OutputDir={#ReposFolder}\Installers\Dev
OutputBaseFilename=VMM_Dev_{#MyAppVersion}_{#MyAppEdition}_Installer
SetupIconFile={#ReposFolder}\vmm.ico
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

