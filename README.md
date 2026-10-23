# Drova – Deutsche Sprachausgabe

Full application source for Benchler's unofficial German voiceover mod for **Drova – Forsaken Kin**: voice playback, optional automatic dialogue continuation, Windows installer scripts and the C# installer launcher.

**Nexus security review:** [Mod 40](https://www.nexusmods.com/drovaforsakenkin/mods/40), quarantined **File 257**. Start with [SECURITY-REVIEW.md](SECURITY-REVIEW.md) and [BUILD.md](BUILD.md).

| Source snapshot | Purpose |
| --- | --- |
| [0.1.4-preview-11479-f4a508d7](versions/0.1.4-preview-11479-f4a508d7) | Source preserved for the quarantined File 257. This is the version relevant to the original review request. |
| [0.1.6-preview-11309-ca6150e5](versions/0.1.6-preview-11309-ca6150e5) | Latest prepared version at this submission, including route/actor and playback lifecycle corrections. This is a different package, not a replacement source claim for File 257. |

Every version contains:

- `drova_de/runtime/`: the complete `DrovaVoices.dll` source and project.
- `drova_de/auto_dialogue/`: the complete `DrovaAutoDialogue.dll` source, project and menu localizations.
- `installer/`: the actual distributed PowerShell/CMD scripts, C# launcher, manifest, configuration and visual assets.
- `EXPORT-SHA256.json`: checksums of the exported source/configuration/assets.

The latest snapshot also includes headless regression tests. [releases.json](releases.json) records original package and executable checksums. [VALIDATION.md](VALIDATION.md) separates compilation, automated tests and unverified gameplay.

The GitHub repository is a **source and security-review distribution**. The large voice recordings and game-derived runtime dialogue data remain in the mod download; no TTS service account is needed to compile or run the mod. Proprietary game binaries, generated game interop assemblies, credentials and private production/account records are excluded. Build references come from the reviewer's own installed game. The optional package assembly step reuses an existing, verified mod payload instead of generating new audio.

## Quick build (Windows)

Install Python 3.11 or newer and .NET SDK 10. From the repository root:

```powershell
python build_review.py --game 'D:\Games\Drova - Forsaken Kin' --test
```

Use your actual game folder, containing `Drova.exe` and `MelonLoader/Il2CppAssemblies/`. For the quarantined version:

```powershell
python build_review.py --version 0.1.4-preview-11479-f4a508d7 --game 'D:\Games\Drova - Forsaken Kin'
```

See [BUILD.md](BUILD.md) for prerequisites, exact dependency versions, offline options, manual compiler commands, test-only mode and package reconstruction. The build script never starts or installs into the game.

## Rights and third-party components

Source is provided for inspection and security review. No additional redistribution licence is granted here. Drova name, logo and artwork belong to their respective rights holders; this unofficial fanmod is not endorsed by the developers. See each installer's `assets/CREDITS.txt` and `assets/Philosopher-OFL.txt`. Third-party dependencies retain their own licences and are obtained from their official releases; no proprietary game assemblies are redistributed in this repository.
