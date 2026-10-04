# Focus validation status

Verified on October 4, 2026. This records the current foundation, not completed headset acceptance.

| Area | Evidence | Status |
| --- | --- | --- |
| Meta VR CLI | Installer download checksum verified; `--version` reports 1.8.0.17.10 | Installed |
| Unity Hub | WinGet verified package hash and completed install of 3.22.2.65535 | Installed |
| Unity Editor/Android modules | Unity CLI install completed; relocated Editor hash matched; `editors verify` and `Check-Environment.ps1` find Editor/ADB/NDK/Java | Installed |
| Unity license | Batch startup log reports no valid Editor license, exit 198 | Needs Shane's sign-in/activation |
| Unity project | URP settings, candidate manifest, domain source, tests and assembly definitions exist | Prepared; import/compatibility unverified |
| Scan state | Tests observed missing behavior, then passed after implementation | 16 host cases pass |
| Selection admission | Tests observed missing behavior, then passed after implementation | 10 host cases pass |
| Full host domain suite | `scripts/Test-Domain.ps1` runs real C# source and Unity's NUnit package | 26 pass; zero failures/skips |
| Meta package inspection | Core, Interaction and OVR integration 207.0.0 archives downloaded; registry SHA512 matched | Staged locally; not imported into Unity |
| Unity EditMode/PlayMode | Editor activation prevents execution | Pending |
| Baseline/Focus scenes and runtime presentation | No platform prefab, Scene, graphics or audio implementation yet | Pending |
| APK build/install/launch | No APK exists; no connected headset used | Pending |
| Physical input, recovery, comfort and 72 FPS target | No hardware observations or measurements | Pending |
| Second visible-change build/install | Depends on the first on-device build | Pending |

Host tests cover duplicate requests, 1.25-second expiry, long frames, pause/tracking recovery order, preserving active scans through repeated status updates, invalid time values, held selections, independent sources, required neutral after tracking loss, and admission of another source while one is lost.

The gate treats `Release` as observed physical neutral with valid tracking. The future SDK adapter must not map tracking-loss cancellation/unselect to that method. Core logic tests establish the gate's behavior; they do not establish real SDK event behavior or pinch recognition.

Builds, logs, third-party inspection caches and host-test XML/DLL files remain under ignored `.artifacts/`. Source is on local branch `codex/focus-core`; nothing from this implementation has been pushed to GitHub.
