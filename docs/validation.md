# Focus validation status

Verified on October 6, 2026. This records the current foundation, not completed headset acceptance.

| Area | Evidence | Status |
| --- | --- | --- |
| Meta VR CLI | Installer download checksum verified; `--version` reports 1.8.0.17.10 | Installed |
| Unity Hub | WinGet verified package hash and completed install of 3.22.2.65535 | Installed |
| Unity Editor/Android modules | Unity CLI install completed; relocated Editor hash matched; `editors verify` and `Check-Environment.ps1` find Editor/ADB/NDK/Java | Installed |
| Unity license | Actual licensed import/compilation and test run exited0 | Activation works |
| Unity project | Android-target import/compile; Unity-generated package lock | Imported and compiled |
| Scan state | Tests observed missing behavior, then passed after implementation | 16 host cases pass |
| Selection admission | Tests observed missing behavior, then passed after implementation | 10 host cases pass |
| Full host domain suite | `scripts/Test-Domain.ps1` runs real C# source and Unity's NUnit package | 26 pass; zero failures/skips |
| Meta packages | Core, Interaction and OVR integration 207.0.0 resolved by Unity | Imported |
| Unity EditMode | Android-target run exited0; 31 total, 31 passed, zero failed/skipped | Passed |
| Unity PlayMode | No runtime Focus components implemented yet | Pending |
| Baseline scene/platform prefab | Saved/reloaded scene tests verify one transparent camera rig, one passthrough layer and hand/controller ray/poke interactors; repeated generation has no duplicate rigs | Generated and tested in Editor |
| Focus runtime presentation | Scan UI, SDK selection adapter, pulse/audio and lifecycle wiring still pending | Pending |
| Baseline APK | Current Gradle invocation exited0; fresh ARM64 APK, signature and packaged manifest checked | Built; headset unverified |
| APK install/launch | No connected headset used | Pending |
| Physical input, recovery, comfort and 72 FPS target | No hardware observations or measurements | Pending |
| Second visible-change build/install | Depends on the first on-device build | Pending |

Host tests cover duplicate requests, 1.25-second expiry, long frames, pause/tracking recovery order, preserving active scans through repeated status updates, invalid time values, held selections, independent sources, required neutral after tracking loss, and admission of another source while one is lost.

The gate treats `Release` as observed physical neutral with valid tracking. The future SDK adapter must not map tracking-loss cancellation/unselect to that method. Core logic tests establish the gate's behavior; they do not establish real SDK event behavior or pinch recognition.

The actual Unity green result is `.artifacts/tests/unity-EditMode-20261006-044215-43c3d70a/results.xml`, with log `.artifacts/logs/tests-EditMode-20261006-044217-8530a582.log`. The debugger-settings case also passed after its intended red failure. Four new platform cases first failed with the intended unimplemented API, then passed after implementation and the diagnosed Android-target correction. The earlier 26 Unity domain tests also passed separately.

The CLI refuses concurrent Editors and low-memory launches. Its active-Editor guard was exercised during the baseline build and returned a clear error without launching or stopping a process. Private ADB variables are scoped to the owned child; no listener was observed on5037 or54483 during the successful test run. That observation does not establish native shutdown behaviour during later builds or USB-device isolation.

APKs, logs, third-party inspection archives and host-test XML/DLL files remain under ignored `.artifacts/`. Gradle caches/exports use the ownership-marked `%USERPROFILE%/.fbc/<checkout hash>` directory. Source is on local branch `codex/focus-core`; nothing from this implementation has been pushed to GitHub.

Baseline build artifact: `.artifacts/apks/Baseline-20261006-040917-f34594ea/FocusBaseline.apk`; convenience path `.artifacts/apks/FocusBaseline.apk`. Size: 69,073,188 bytes. SHA-256: `57280903FE2E695A3E83A92F87626AD710DD6B73770AB251FF9EF490AE437FCA`. Current Gradle log `.artifacts/logs/gradle-Baseline-20261006-040917-f34594ea.stdout.log` records `BUILD SUCCESSFUL in 32m 36s`, 118 actionable tasks. Bundled apksigner exited0 and verified APK Signature Scheme v2. Bundled aapt confirmed package `com.thegamingdonkey.focuscore`, minAPI32, targetAPI34, ARM64, GameActivity, hand-tracking permission and required passthrough feature; packaged metadata targets Quest3S and there is no camera permission. The ARM64 IL2CPP library inside the APK is nonempty.

This APK packages the saved export `Baseline-20261006-035607-6cf3e129`; build-helper-only changes made afterward are not app runtime changes. First integrated compilation encountered low memory, then packaging hit Ninja's 260-character path limit. The successful run uses a shorter owned Gradle cache, regenerated CMake files, a 2048MiB JVM heap and two observed native compiler jobs after Unity exits. Only verified owned build processes were stopped; no other app was closed. Hardware, 72FPS/comfort, the Focus effects and quick incremental rebuilding remain unverified.