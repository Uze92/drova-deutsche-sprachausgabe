# Build and review instructions

## Requirements

1. Windows x64, Windows PowerShell 5.1 and .NET Framework 4.x (including `Framework64/v4.0.30319/csc.exe`). The application itself targets .NET 6, matching MelonLoader 0.7.3.
2. Python **3.11+** (standard library only), and **.NET SDK 10**. The mod projects target `net6.0`; the included headless test projects target `net10.0`. Initial `dotnet restore` can need access to NuGet for targeting packs. Tested SDK: **10.0.401**.
3. A legally installed Drova. Install MelonLoader **0.7.3 x64** and have the game owner perform its first launch once so MelonLoader generates `MelonLoader/Il2CppAssemblies/`. Then close the game. The build does **not** perform this launch or alter the installation. Generated references are tied to the installed game version; a different game update may require code changes rather than an arbitrary dependency upgrade.
4. The two official dependency archives below. The helper fetches them automatically on the first full build, checking SHA-256 before extraction. Alternatively put the exact ZIPs in `.cache/` with the indicated names before running it. Cached archives are rechecked on every build.

| Archive in `.cache/` | Official file | SHA-256 |
| --- | --- | --- |
| `melonloader.zip` | [MelonLoader.x64.zip, v0.7.3](https://github.com/LavaGang/MelonLoader/releases/download/v0.7.3/MelonLoader.x64.zip) | `5b2b2f3d1cd42b59ec886c5bdc2663edae87a0097a4f4a8f58c0965a99dda416` |
| `modding-api.zip` | [Modding_API.zip, release-0.6.0](https://github.com/Drova-Modding/Drova-Modding-API/releases/download/release-0.6.0/Modding_API.zip) | `f8e4756166bc9925e6f39394f0877ed56be146be550cee2710aa26dafc89cdf4` |

Official projects: [MelonLoader](https://github.com/LavaGang/MelonLoader), [Drova Modding API](https://github.com/Drova-Modding/Drova-Modding-API). Download the Windows **x64 ZIP**, not the x86/Linux/macOS/installer/source archives. For the API use **Modding_API.zip**, not Dev or TestRunner.

## Automated compilation

Clone or download this repository, extract completely, open PowerShell in its root, and substitute your own game folder:

```powershell
python build_review.py --version 0.1.4-preview-11479-f4a508d7 --game 'D:\Games\Drova - Forsaken Kin'
python build_review.py --version 0.1.6-preview-11309-ca6150e5 --game 'D:\Games\Drova - Forsaken Kin' --test
```

For each selected version this verifies source checksums, obtains/verifies official dependencies, compiles both DLLs and compiles the EXE launcher. Outputs:

```text
build/<version>/voices/DrovaVoices.dll
build/<version>/auto-dialogue/DrovaAutoDialogue.dll
build/<version>/Deutsche-Stimmen-Setup.exe
build/<version>/build-result.json
```

Only these three application binaries are relevant. Do not copy the build folder's reference assemblies into the game. Source projects are preserved unchanged; command-line MSBuild properties override their historical reference locations. No other private project or hardcoded developer folder is required. Commands fail on compilation or checksum errors.

## Tests without a game or downloads

```powershell
python build_review.py --tests-only
```

Runs the latest startup/provider fixture, playback lifecycle fixtures, auto-dialogue/menu fixtures and WAV parser checks with a synthetic sample. They use simulated engine objects and do not launch a game. They do not certify real engine hooking, sound quality or all gameplay paths.

For a full real-payload route/provider regression, supply the extracted latest mod package:

```powershell
python build_review.py --tests-only --package-root 'D:\Downloads\Drova-0.1.6-extracted'
```

That folder must contain `Paket.json` and `Mod-Dateien/Mods/DrovaVoices/routes.json` plus its recordings. Tests read the supplied package and create their own temporary fixtures; no game installation is touched. Historical 0.1.4 predates these current regression fixtures and is compiled separately without `--test`.

## Individual compiler commands

If reviewing without the Python helper, extract MelonLoader beneath `versions/<version>/drova_de/research/melonloader-0.7.3-official/` and the API beneath `versions/<version>/drova_de/research/runtime-dependencies/`. Expected files are respectively `MelonLoader/net6/MelonLoader.dll` and `Mods/Drova_Modding_API.dll`.

From the repository root:

```powershell
$v = '0.1.4-preview-11479-f4a508d7'
$base = Join-Path $PWD "versions/$v"
$refs = 'D:\Games\Drova - Forsaken Kin\MelonLoader\Il2CppAssemblies'
dotnet build "$base/drova_de/runtime/DrovaVoices.csproj" -c Release -o "build/$v/voices" "-p:UnityReferenceRoot=$refs"
dotnet build "$base/drova_de/auto_dialogue/DrovaAutoDialogue.csproj" -c Release -o "build/$v/auto-dialogue" "-p:InteropRoot=$refs"
& "$env:WINDIR/Microsoft.NET/Framework64/v4.0.30319/csc.exe" /nologo /target:winexe /optimize+ /reference:System.Windows.Forms.dll "/win32manifest:$base/installer/Launcher.manifest" "/win32icon:$base/installer/assets/icon.ico" "/out:build/$v/Deutsche-Stimmen-Setup.exe" "$base/installer/Launcher.cs"
```

Change `$v` to select 0.1.6. The EXE is a small launcher for adjacent `Setup.ps1`, not a self-extracting voice archive. All functional installer code is readable PowerShell; no additional compiled installer component is missing.

## Optional complete installer folder

Compilation needs no recordings. To reconstruct a usable installer folder, obtain and fully extract the **matching** mod ZIP (for Nexus reviewers, File 257 for 0.1.4), then run:

```powershell
python build_review.py --version 0.1.4-preview-11479-f4a508d7 --game 'D:\Games\Drova - Forsaken Kin' --package-root 'D:\Downloads\Drova-0.1.4-extracted'
```

The helper requires the selected version to match `Paket.json`, validates every input payload hash, copies the full payload into `build/<version>/package/`, adds the exact installer scripts/assets and the three newly compiled binaries, and updates the payload checksums. It deliberately labels this result `<version>-source-review-build`. It refuses to overwrite an existing assembled `package/` folder. It neither uploads nor installs the result. Run `Deutsche-Stimmen-Setup.exe` from this complete output folder only if you intentionally want to test installation. Original uploaded archives remain untouched.

No TTS regeneration, service key or paid cloud request is involved. The original voice data is an input rather than executable source code. A compilation from this source is **not a claim of byte-for-byte reproducibility**: SDK, compiler, references, source paths, timestamps and build settings can affect binary bytes. Original distribution hashes are recorded separately in `releases.json`; new local hashes are in `build-result.json`.
