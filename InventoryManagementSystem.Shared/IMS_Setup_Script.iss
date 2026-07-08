[Setup]
; Basic Information
AppName=Inventory Management System
AppVersion=1.2.2
AppPublisher=IMS Professional
AppPublisherURL=https://mwimule.com/ims
AppSupportURL=https://mwimule.com/support
AppUpdatesURL=https://mwimule.com/updates

; Destination
DefaultDirName={autopf}\InventoryManagementSystem
DefaultGroupName=Inventory Management System
OutputDir=../Releases
OutputBaseFilename=IMS_Setup_v1.2.2_Windows
Compression=zip
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64

; Branding
WizardStyle=modern
DisableWelcomePage=no
DisableDirPage=no
PrivilegesRequired=lowest

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; The main executable (Source path is relative to this .iss file's location).
; The published exe is named after the Desktop project ("InventoryManagementSystem.Desktop.exe"),
; matching the name used by the Velopack/GitHub Actions pipeline.
Source: "../Releases/Windows/InventoryManagementSystem.Desktop.exe"; DestDir: "{app}"; Flags: ignoreversion
; Include all other files in the publish directory (if any) - for single file publish this is usually just pdb or config if excluded
Source: "../Releases/Windows/*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Visual C++ Redistributable (x64) - required by Avalonia/SkiaSharp's native rendering libraries.
; Without this, the app fails to start on a clean Windows machine with:
; "The application has failed to start because its side-by-side configuration is incorrect" (0x800736B1)
Source: "./redist/vc_redist.x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: VCRedistNeedsInstall

[Icons]
Name: "{group}\Inventory Management System"; Filename: "{app}\InventoryManagementSystem.Desktop.exe"
Name: "{group}\{cm:UninstallProgram,Inventory Management System}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Inventory Management System"; Filename: "{app}\InventoryManagementSystem.Desktop.exe"; Tasks: desktopicon

[Run]
Filename: "{tmp}\vc_redist.x64.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installing required Visual C++ Runtime components..."; Check: VCRedistNeedsInstall; Flags: waituntilterminated
Filename: "{app}\InventoryManagementSystem.Desktop.exe"; Description: "{cm:LaunchProgram,Inventory Management System}"; Flags: nowait postinstall skipifsilent

[Code]
// Detects whether the VC++ 2015-2022 (v14) x64 runtime is already installed on the
// target machine. Microsoft's installer sets this registry value when it succeeds.
function VCRedistNeedsInstall: Boolean;
var
  Installed: Cardinal;
begin
  Result := True;
  if RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\X64', 'Installed', Installed) then
  begin
    if Installed = 1 then
      Result := False;
  end;
end;
