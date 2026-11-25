[Setup]
AppId=JarvisBoostCommunity
AppName=LATA BOOST
AppVersion=0.4.0
DefaultDirName={localappdata}\Programs\LataBoost
DefaultGroupName=LATA BOOST
PrivilegesRequired=lowest
OutputDir=..\dist
OutputBaseFilename=LATA_BOOST_SETUP
Compression=lzma2
SolidCompression=yes
UninstallDisplayIcon={app}\LataBoost.exe
[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\pix.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\THIRD_PARTY_NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion
[Icons]
Name: "{group}\LATA BOOST"; Filename: "{app}\LataBoost.exe"
[Run]
Filename: "{app}\LataBoost.exe"; Description: "Abrir LATA BOOST"; Flags: nowait postinstall skipifsilent
