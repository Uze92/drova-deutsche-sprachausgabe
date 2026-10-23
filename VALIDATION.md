# Validation of this source submission

Checked on **23 October 2026**, Windows x64, Python 3.14, .NET SDK **10.0.401**.

- **Both version snapshots:** voice DLL, auto-dialogue DLL and Windows launcher compiled successfully from this exported repository. Both production DLL projects compiled with zero warnings and zero errors. Test-only fixture projects can emit unused-parameter warnings in their simulated engine classes.
- **Official dependency downloads:** both pinned archives were downloaded by `build_review.py` and matched the documented SHA-256 values before extraction.
- **Latest startup fixture:** actual runtime initialization code remains inactive in the simulated main menu, connects once after world readiness, and preserves the gain/clipping guard behaviour.
- **Latest playback fixture:** **1,744 assertions**, including 100 repeated lifetimes, using the actual guard with simulated native coroutine/source events.
- **Latest auto-dialogue fixture:** **502 assertions** covering audio completion and the actual core/menu bridge with simulated engine types.
- **WAV parser:** three checks using a generated synthetic WAV, plus runtime startup gate checks.
- **Real latest payload route/provider fixture:** **11,309 routes / 26,072 assertions**; includes the reported Asmus/Carima lines, native actor parameter regression, fixed voice identity, alias sharing, destroyed clip cache and disabled/player-choice exclusions.
- **Source provenance:** original 0.1.4 source-export checksums were verified. Exported installer scripts/configuration match the corresponding original release folders. Exported source/configuration/assets have per-version SHA-256 manifests.
- **Publication scan:** the allowlisted export was checked for common provider/GitHub secrets, private user paths and private workspace paths; no matches. Only explicit mod source/configuration/assets are included; unrelated projects, game binaries and production/account data are excluded.
- **Complete package build:** the optional assembly step successfully validated and copied the real 0.1.6 payload, replaced the three application binaries and wrote a separate `source-review-build` manifest. The compiled launcher returned exit code 0 in check mode. The actual assembled PowerShell UI was exercised in a real window: all three tabs, help/about/support actions and layout checks passed. Nothing was installed into the game.
- **Checkout integrity:** Windows Git can otherwise rewrite LF/CRLF endings during checkout and invalidate historical snapshot checksums. Repository `.gitattributes` disables text conversion to preserve the exact versioned bytes.

The tests do **not** launch Drova. They do not prove real engine detours, real audio playback or complete gameplay coverage. Current ingame verification remains the user's task. Compilation is not byte-for-byte reproducibility certification and does not imply Nexus approval.

The original mod packages remain unchanged. This repository contains source and instructions rather than a new Nexus file submission. Original binary/package checksums are in [releases.json](releases.json); local newly compiled hashes are emitted under ignored `build/<version>/build-result.json`.
