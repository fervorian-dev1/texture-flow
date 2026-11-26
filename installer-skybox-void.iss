#ifndef PortableDir
#define PortableDir "build\v0.4.0-stage"
#endif
[Setup]
AppId={{D3CA4B46-48CA-4D9B-8A9A-72A0D72BF116}
AppName=Texture Flow for VRChat
AppVersion=0.4.0
DefaultDirName={localappdata}\Programs\TextureFlow
DefaultGroupName=Texture Flow
PrivilegesRequired=lowest
OutputDir=build\releases
SetupIconFile=src\TextureFlow\Assets\AppIcon.ico
OutputBaseFilename=TextureFlow-0.4.0-Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\TextureFlow.exe
[Files]
Source: "{#PortableDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\Texture Flow"; Filename: "{app}\TextureFlow.exe"
Name: "{autodesktop}\Texture Flow"; Filename: "{app}\TextureFlow.exe"; Tasks: desktopicon
[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked
[Run]
Filename: "{app}\TextureFlow.exe"; Description: "Launch Texture Flow"; Flags: nowait postinstall skipifsilent
