DROVA GERMAN VOICEOVER - SECURITY REVIEW SOURCE PACKAGE
Nexus: https://www.nexusmods.com/drovaforsakenkin/mods/40
Nexus file ID: 257
Release: 0.1.4-preview-11479-f4a508d7
Uploaded archive SHA-256: 881cc9d93fa4fc362543f365de8f7778cc67f58948bb588d91d897a18f77c41e

CONTENTS
installer/: Exact PowerShell and CMD scripts shipped in this release; C# launcher source and application manifest. The launcher starts the adjacent Setup.ps1 using Windows PowerShell. ExecutionPolicy Bypass applies to that process; it does not persistently change system policy.
drova_de/runtime/: German voice playback mod source.
drova_de/auto_dialogue/: Automatic dialogue continuation mod source (500 ms default).
No credentials, game binaries, third-party dependency DLLs or voice recordings are included. No licence to redistribute game files or artwork is granted by this review submission.

BUILD REQUIREMENTS
Windows x64, .NET SDK supporting net6.0, Windows .NET Framework C# compiler.
A legally installed Drova and locally generated MelonLoader IL2CPP references are needed to build the mod DLLs. This review package does not generate references or launch the game.
Official dependencies:
https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3 (MelonLoader.x64.zip)
https://github.com/Drova-Modding/Drova-Modding-API/releases/tag/release-0.6.0 (Modding_API.zip)

BUILD THE EXE
In PowerShell, change to installer/ and run:
& "$env:WINDIR/Microsoft.NET/Framework64/v4.0.30319/csc.exe" /nologo /target:winexe /optimize+ /reference:System.Windows.Forms.dll /win32manifest:Launcher.manifest /win32icon:assets/icon.ico /out:Deutsche-Stimmen-Setup.exe Launcher.cs
The EXE is a small launcher, not a self-extracting audio archive. Full installer visual assets and the payload remain in the already uploaded Nexus archive.

BUILD THE MOD DLLS
1. Extract the official MelonLoader ZIP into drova_de/research/melonloader-0.7.3-official/ (so MelonLoader/net6/MelonLoader.dll exists below it).
2. Extract the official API ZIP into drova_de/research/runtime-dependencies/ (so Mods/Drova_Modding_API.dll exists below it).
3. Set $refs to the existing MelonLoader/Il2CppAssemblies folder in your own game installation. For example:
$refs = 'C:/Program Files (x86)/Steam/steamapps/common/Drova - Forsaken Kin/MelonLoader/Il2CppAssemblies'
4. From this source package's root, run:
dotnet build drova_de/runtime/DrovaVoices.csproj -c Release -o review-build/voices "-p:UnityReferenceRoot=$refs"
dotnet build drova_de/auto_dialogue/DrovaAutoDialogue.csproj -c Release -o review-build/auto-dialogue "-p:InteropRoot=$refs"
SDK/compiler/reference versions can affect byte-for-byte binary reproduction. These are source review and compilation instructions, not a claim of a reproducible-build certification.

BEHAVIOUR
The installer validates and copies local package files and user-selected official dependency archives into a selected game folder. It supports updates, backups and removal. Download/help buttons open browser pages; the installer does not upload telemetry or call TTS APIs. Mod DLLs run in Drova through MelonLoader. The voice mod plays packaged NPC audio; the add-on advances voiced dialogue after playback and a configurable delay. Answer choices remain manual.
The exact quarantine trigger has not been disclosed. This submission does not claim that Nexus or VirusTotal has cleared the archive.
