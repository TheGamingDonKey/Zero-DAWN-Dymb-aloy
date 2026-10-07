# Focus validation status

Updated October7,2026. This records authored work and actual checks separately; headset acceptance is incomplete.

## Current increment and continuation

The current licence attempt exited198 before import (`.artifacts/logs/mcp-import-20261007-041645-35134d74.log`): access token unavailable and no valid Editor entitlement. Normal Unity Hub recovery was attempted once, and Shane was asked to check Settings → Licenses and activate/sign in to Personal if prompted. There has been no repeated compile/build attempt against the unchanged licence failure.

| Increment | Actual evidence | Remaining verification |
| --- | --- | --- |
| Research |15-page PDF; all pages rendered and visually inspected; primary UI portfolios, selected official gameplay frames/transcript, vendor documentation and source links | More scene-specific fidelity comparisons; no exhaustive fictional capability claim |
| Original Blender assets |Blender5.2.2, MCP addon1.8/protocol13;8 source meshes,274 base polygons; material nodes and animation evaluated at frames1/60/120; both FBX files actually reimported with materials and metre-scale bounds | Unity import and stereo appearance |
| Domain admission |32/32 actual host NUnit passes, zero skips;6 new cases first failed with intended NotImplementedException, then passed | Unity Test Runner and physical SDK behaviour |
| First-demo source |Scan card, SDK source adapter, one finite fixed-origin lattice, original220ms chirp, cancellation/availability handling and scene generator authored | Scene has not been generated; no new Core APK exists |
| Cached API compilation |Fresh Domain, Runtime, Editor and PlayMode-test assemblies compile against installed Unity/Meta DLLs; source hashes, assembly hashes and exits in `docs/reference/evidence/source-compilation.json` | This bypasses Editor execution only for static checking; no shader, asset-import, runtime or Android proof |
| Unity MCP |Pinned Coplay10.0.0 package/server; real initialization and46-tool catalogue, instance resource read; only `FocusUnity` added to Codex config, existing parsed config restored/compared unchanged | Instance count0; real Editor scene/console round trip blocked by licence. Current chat may require restart to load the newly registered server |
| Tools |Current file checks find Editor, Android ADB/NDK/JDK and Meta CLI; real Blender MCP operations and Python server work | Installed files do not prove Unity activation |

The critic's first code review found three actionable issues: passthrough readiness, controller-poke neutral and panel reposition after availability recovery. Source changes wait for successful passthrough initialization, require actual controller-poke withdrawal rather than released trigger, and replace panel placement after suspension. Cached API compilation passes after those corrections. Pending PlayMode tests cover fixed origin, duplicate audio, expiry, tracking recovery and scaled poke withdrawal; they have **not** run in Unity.

Independent final milestone score: **5.0/10** (Quest experience0/3; visual/audio1.1/2; engineering1.2/2; research1.8/2; GitHub0.9/1). The critic verified recorded compiler source hashes against the reviewed files and32 host passes with zero failures/skips, confirmed the three source fixes, sampled the booklet/render and found no further concrete source blocker in its bounded pass. It explicitly rated this as a research/engineering increment, not a demonstrated Quest demo. Implementation commit `3a3dac614ffe3e90600fe59995696d05de9d22f3` is published on [GitHub](https://github.com/TheGamingDonKey/Zero-DAWN-Dymb-aloy/tree/codex/focus-core); final documentation commits record the assessment and preserve binary handling alongside the existing Unity YAML rules. No Core APK has been delivered.

Reproducible continuation after normal licence recovery:

1. From this checkout, run `scripts/Test.ps1 -Mode EditMode`; stop if import/test errors occur. Generate `Focus/Generate First Focus Demo` in the Editor, or use the generator execute method in a bounded CLI invocation. The generator uses the existing verified platform prefab and preserves its baseline source.
2. Inspect the generated `Assets/FocusCore/Scenes/FocusCore.unity`, its SDK source bindings, card/reticle orientation and URP shader output. Run `scripts/Test.ps1 -Mode PlayMode` with no concurrent Editor. Fix concrete failures once per diagnosis; do not treat static compilation as a substitute.
3. Close the interactive Editor and run `scripts/Build.ps1 -Scene Core`. This creates a fresh Unity export and packages it with scoped ADB54483, two workers and bounded JVM memory. Never resume the older baseline export to claim the new features were built.
4. Verify the new APK's signature, packaged permissions and SHA256; publish it only with its matching source commit. Connect Quest3S by a data-capable USB cable, enable normal developer access/authorize USB debugging, install and launch. Record hand ray/pinch, reachable poke, controller fallback, held tracking recovery, pause, comfort and actual frame timing.
5. For Unity MCP, `scripts/Start-UnityMcpEditor.ps1` launches this project with its own status directory and requests port54484. Read the instance resource, select this project, then read scene and console. Do not configure all detected clients or connect another project's Editor.

The exact game features that depend on fictional sensing/hardware remain unavailable. Real-world recognition is a later Quest camera/perception project; this increment has no camera permission or object recognition. The practice model/UI is original geometry, not extracted Horizon assets. `docs/reference/focus-reference.json` and generators accompany the PDF; the credited analysis plate stays in documentation, not runtime assets.

## Historical baseline evidence (October6)

| Area | Evidence | Status |
| --- | --- | --- |
| Meta VR CLI | Installer download checksum verified; `--version` reports 1.8.0.17.10 | Installed |
| Unity Hub | WinGet verified package hash and completed install of 3.22.2.65535 | Installed |
| Unity Editor/Android modules | Unity CLI install completed; relocated Editor hash matched; `editors verify` and `Check-Environment.ps1` find Editor/ADB/NDK/Java | Installed |
| Unity license | Actual licensed import/compilation and test run exited0 on October6 | Historical success; currently blocked |
| Unity project | Android-target import/compile; Unity-generated package lock | Imported and compiled |
| Scan state | Tests observed missing behavior, then passed after implementation | 16 host cases pass |
| Selection admission | Tests observed missing behavior, then passed after implementation | 10 host cases pass |
| Full host domain suite | `scripts/Test-Domain.ps1` runs real C# source and Unity's NUnit package | 26 pass; zero failures/skips |
| Meta packages | Core, Interaction and OVR integration 207.0.0 resolved by Unity | Imported |
| Unity EditMode | Android-target run exited0; 31 total, 31 passed, zero failed/skipped | Passed |
| Unity PlayMode | No runtime execution demonstrated | Pending |
| Baseline scene/platform prefab | Saved/reloaded scene tests verify one transparent camera rig, one passthrough layer and hand/controller ray/poke interactors; repeated generation has no duplicate rigs | Generated and tested in Editor |
| Focus runtime presentation | Baseline does not contain Scan UI/effects | New source prepared October7; not built |
| Baseline APK | Current Gradle invocation exited0; fresh ARM64 APK, signature and packaged manifest checked | Built; headset unverified |
| APK install/launch | No connected headset used | Pending |
| Physical input, recovery, comfort and 72 FPS target | No hardware observations or measurements | Pending |
| Second visible-change build/install | Depends on the first on-device build | Pending |

Host tests cover duplicate requests, 1.25-second expiry, long frames, pause/tracking recovery order, preserving active scans through repeated status updates, invalid time values, held selections, independent sources, required neutral after tracking loss, and admission of another source while one is lost.

The gate treats `Release` as observed physical neutral with valid tracking. The future SDK adapter must not map tracking-loss cancellation/unselect to that method. Core logic tests establish the gate's behavior; they do not establish real SDK event behavior or pinch recognition.

The actual Unity green result is `.artifacts/tests/unity-EditMode-20261006-044215-43c3d70a/results.xml`, with log `.artifacts/logs/tests-EditMode-20261006-044217-8530a582.log`. The debugger-settings case also passed after its intended red failure. Four new platform cases first failed with the intended unimplemented API, then passed after implementation and the diagnosed Android-target correction. The earlier 26 Unity domain tests also passed separately.

The CLI refuses concurrent Editors and low-memory launches. Its active-Editor guard was exercised during the baseline build and returned a clear error without launching or stopping a process. Private ADB variables are scoped to the owned child; no listener was observed on5037 or54483 during the successful test run. That observation does not establish native shutdown behaviour during later builds or USB-device isolation.

APKs, raw logs, third-party inspection archives and compiler outputs remain under ignored `.artifacts/`. Selected non-sensitive evidence summaries and host-test XML are published in `docs/reference/evidence`; Gradle caches/exports use the ownership-marked `%USERPROFILE%/.fbc/<checkout hash>` directory. Source handoff uses branch `codex/focus-core`. No new Core APK has been published.

Baseline build artifact: `.artifacts/apks/Baseline-20261006-040917-f34594ea/FocusBaseline.apk`; convenience path `.artifacts/apks/FocusBaseline.apk`. Size: 69,073,188 bytes. SHA-256: `57280903FE2E695A3E83A92F87626AD710DD6B73770AB251FF9EF490AE437FCA`. Current Gradle log `.artifacts/logs/gradle-Baseline-20261006-040917-f34594ea.stdout.log` records `BUILD SUCCESSFUL in 32m 36s`, 118 actionable tasks. Bundled apksigner exited0 and verified APK Signature Scheme v2. Bundled aapt confirmed package `com.thegamingdonkey.focuscore`, minAPI32, targetAPI34, ARM64, GameActivity, hand-tracking permission and required passthrough feature; packaged metadata targets Quest3S and there is no camera permission. The ARM64 IL2CPP library inside the APK is nonempty.

This APK packages the saved export `Baseline-20261006-035607-6cf3e129`; build-helper-only changes made afterward are not app runtime changes. First integrated compilation encountered low memory, then packaging hit Ninja's 260-character path limit. The successful run uses a shorter owned Gradle cache, regenerated CMake files, a 2048MiB JVM heap and two observed native compiler jobs after Unity exits. Only verified owned build processes were stopped; no other app was closed. Hardware, 72FPS/comfort, the Focus effects and quick incremental rebuilding remain unverified.
