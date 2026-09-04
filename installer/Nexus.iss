; ---------------------------------------------------------------------------
; Inno Setup script for Nexus.
;
; Build via scripts\build-installer.ps1, or directly:
;   iscc /DAppVersion=0.1.0 /DPublishDir=..\dist\Nexus-0.1.0-win-x64 installer\Nexus.iss
;
; The script installs the self-contained publish output produced by
; scripts\publish.ps1 (which includes the bundled tools\ folder). Because the build is
; self-contained, no .NET runtime prerequisite is required on the target machine.
; ---------------------------------------------------------------------------

#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif

#ifndef PublishDir
  #define PublishDir "..\dist\Nexus-" + AppVersion + "-win-x64"
#endif

#define AppName "Nexus"
#define AppPublisher "Nexus contributors"
#define AppExeName "Nexus.exe"
#define AppUrl "https://github.com/your-org/nexus"

[Setup]
; A stable, application-specific GUID (keep constant across versions so upgrades work).
AppId={{B7E4C1D2-3A56-4F81-9C0E-2D6F8A1B4E37}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
OutputDir=..\dist
OutputBaseFilename=Nexus-{#AppVersion}-setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Default to a per-user install (no admin prompt); allow elevating to all-users.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Recursively install the entire published output, including the tools\ subfolder.
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent
