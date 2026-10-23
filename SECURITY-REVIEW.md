# Nexus Mods security review – Mod 40 / File 257

Author: **Benchler**. Mod: <https://www.nexusmods.com/drovaforsakenkin/mods/40>.

The review request concerns **0.1.4-preview-11479-f4a508d7**, Nexus **File 257**. Its source is preserved in [`versions/0.1.4-preview-11479-f4a508d7/`](versions/0.1.4-preview-11479-f4a508d7/). The original uploaded ZIP SHA-256 is:

```text
881cc9d93fa4fc362543f365de8f7778cc67f58948bb588d91d897a18f77c41e
```

The separate latest snapshot **0.1.6-preview-11309-ca6150e5** is included for transparency and current maintenance. It does not purport to be the code that built File 257. Its prepared archive SHA-256 is:

```text
382bc3159195b8ba11a2c750d33b842c84c96063b3ab34bad8acd171521eb783
```

See [releases.json](releases.json) for the hashes of each distributed EXE and both own mod DLLs. The quarantined source was recovered from the preserved 0.1.4 review export; its original `SOURCE-SHA256.json` and `README-BUILD-AND-REVIEW.txt` remain present. Additional package configuration/assets and the new helper have separate exported checksums.

## Executable-to-source map

| Distributed file | Source |
| --- | --- |
| `Deutsche-Stimmen-Setup.exe` | `installer/Launcher.cs`, `Launcher.manifest`, `assets/icon.ico` |
| `Installieren.cmd` | Exact script in `installer/` |
| `Setup.ps1` | Exact entry script in `installer/`; loads `InstallerCore.ps1` and `InstallerUI.ps1` |
| Other installer actions/UI | `InstallerCore.ps1`, `InstallerUI.ps1`, `InstallerExperience.ps1`, `ExperienceCore.ps1` |
| `Mod-Dateien/Mods/DrovaVoices.dll` | `drova_de/runtime/*.cs` and `DrovaVoices.csproj` |
| `Mod-Dateien/Mods/DrovaAutoDialogue.dll` | `drova_de/auto_dialogue/Core.cs`, `AudioEndGate.cs`, project file |

## Behaviour and review points

The launcher starts Windows PowerShell with `-NoProfile -ExecutionPolicy Bypass -STA` for the adjacent local installer. This bypass is process-scoped; it does not persistently change the system execution policy. The GUI starts worker PowerShell processes to check/install/remove local files, and can request elevation with `RunAs` if writing to a protected game folder requires it. These behaviours are disclosed for inspection, not asserted to be the undisclosed quarantine trigger.

The installer checks package and game compatibility, verifies/copies payload and user-selected dependency archives, and maintains installation receipts/backups for update/removal. Steam detection reads normal registry/library locations. Support/download buttons open configured web links on user action. Diagnostics are copied locally on request with personal paths hidden; nothing is automatically sent. Inspect the scripts for precise branches, validation and rollback behaviour.

The voice mod reads packaged routes and WAV files and supplies NPC audio through the Modding API. The optional add-on exposes menu settings and advances finished voiced dialogue with a default 500 ms delay; response choices remain manual. The latest code adds lifecycle protection around mod-owned audio sources and fixes actor/route resolution. Tests simulate engine behaviour; the newest changes still require user ingame verification.

The distributed mod is local playback. It does not require TTS/Gemini credentials or generate voices at runtime. The separate `build_review.py` helper downloads only the two pinned official dependency archives when not supplied locally; normal .NET restore can access NuGet. Proprietary game binaries/interop assemblies are not included. They are read as compiler references from the reviewer's own installation.

No VirusTotal clearance or Nexus approval is claimed. The original quarantined file remains available to Nexus for inspection; this source submission does not replace or modify it. Detailed commands: [BUILD.md](BUILD.md).
