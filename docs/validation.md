# Focus validation status

Updated October7,2026. This records authored work and actual checks separately; headset acceptance is incomplete.

## Audiovisual refinement, October 7

The current runtime uses a single inward-facing procedural shell with 528 facets / 1,584 vertices, anti-aliased barycentric edges, sparse glints, faint facet tint and a progress-driven latitude sweep. Expansion is eased; the world-fixed origin, 1.25-second lifetime, duplicate suppression and tracking-loss cancellation remain unchanged. Original mono PCM audio now lasts 0.94 seconds and combines airy filtered noise, rising resonance, glass-like partials, low body and a quiet delayed tail. These are our authored choices, not Guerrilla's production settings.

Fresh Unity MCP jobs: 37 EditMode tests completed with succeeded status and no failures; 4/4 PlayMode passed with no failures/skips. The real imported audio/material/shader passed the desktop diagnostic: six accepted scans, six acknowledgements, duplicate rejection and final stopped state. Three actual Game-view frames were retained. See [test evidence](reference/evidence/refinement-tests.json), [audio measurements](reference/evidence/audio-quality.json), [desktop run](reference/evidence/refined-desktop-proof.json), [middle pulse](reference/evidence/refined-pulse-middle.png) and [inside-shell view](reference/evidence/refined-pulse-inside.png). The diagnostic driver now uses elapsed wall time because Editor callbacks are not equivalent to rendered frames; its earlier first-pulse capture could be skipped by a stale frame delta. This helper remains excluded from the Core APK.

The [research addendum](reference/audio-scan-study.json) separates inspected developer interviews, sampled official footage and a community sound pointer from authored synthesis. No Focus-device-specific production recipe was found. Playback/UI inspection is not a captured audio comparison; exact sound or visual equivalence is unverified. Triangle count does not establish transparent fill cost, stereo comfort or Quest frame time. Headset work is deferred at Shane's request.

The refined Core APK completed successfully in one build of source `afc2588b4bd736e5bcaae23c59412d59a4da60d8`. Artifact: `.artifacts/apks/Core-20261007-094501-0a33cd7d/FocusCore.apk`, 69,135,704 bytes, SHA256 `ACDF74BF15DCF0A3C6D46F43843615FE8C4222B42A14F8A425E3AFA153ECF228`. The convenience `.artifacts/apks/FocusCore.apk` now points to this increment. One actual package check passed v2 signature, ARM64-only IL2CPP, minAPI32/targetAPI34, Quest3S metadata, hand permission, required passthrough and absence of raw CAMERA permission. See [package evidence](reference/evidence/refinement-apk.json). This establishes packaging, not physical headset behavior.

The critic rated the desktop refinement 8.0/10 with no concrete source blocker, while retaining audible fidelity, passthrough contrast and device performance as unverified. Its anti-loop audit found the native compilation productive and the progress reporting excessive. This increment stops after one package check, one Editor reopening and the source handoff; no unchanged test suite, rebuild or research round is required.

Reopening exposed a concrete launcher incompatibility: Coplay's import-worker detection matched `-ImportWorkerCount` on the main Editor and skipped all command discovery. Removing that flag and restarting only this Editor once restored the bridge. After startup completed, real scene and error-console calls succeeded: Core loaded cleanly at build index 0, not dirty, with zero returned error entries. The visible GUI showed zero errors and zero warnings in this launch; this does not erase previously disclosed vendor warnings. See [reopening evidence](reference/evidence/refinement-reopen.json). No runtime or APK rebuild followed this launcher-only fix.

A bounded October 7 check of [Unity Technologies Japan's Spark announcement](https://prtimes.jp/main/html/rd/p/000000320.000016287.html) found a new creation environment built on Google's Playground planned later this year. Immediate availability and a Quest export/import route were not established. Spark does not change this increment's Unity/Meta stack or delay its handoff.

## Earlier verified setup milestone

Unity Hub's existing Personal licence is active. Hub lacked the installed6.3 Editor registration and the FocusCore project; both were added, and Hub opened the real project successfully. Actual C# compilation, Meta207 package import, original FBX import and Coplay10.0.0 import succeeded. A fresh CLI launch also exited 0 on October 7; licensing is now verified through both routes.

The saved `Assets/FocusCore/Scenes/FocusCore.unity` contains eight SDK input adapters, the Scan card, original reticle, chirp and procedural pulse component. Its initial panel position is in front of the camera, with readable Scene View framing. Meta's required contextual-passthrough system loading-screen setting is applied. Latest GUI inspection: no required Meta items; twelve recommendations and fourteen OpenXR validation warnings remain for device/performance work.

Actual Unity tests: **37/37 EditMode and4/4 PlayMode, zero failures/skips**. Fresh CLI runs at 07:13 and 07:15 also returned exit 0 and the same green totals; see `docs/reference/evidence/setup-acceptance.json`. Presentation tests exercise fixed pulse origin, one chirp, duplicate rejection, expiry, tracking loss/recovery, panel replacement and scaled poke withdrawal. They drive availability explicitly; physical headset tracking and SDK events remain unverified. Raw result summaries are retained in `docs/reference/evidence/unity-editor-validation.json`.

The real `FocusCore@d367bfbb` Editor bridge on54484 completed scene, console, menu and test round trips. A project-local SessionState flag resumes only an explicitly started bridge through domain reloads; the second PlayMode run and subsequent scene query succeeded without manual restart. Global client transport preferences were not changed.

Initial worker Import Error4 was traced to a stale modification time for `Assets/XR/Settings/OpenXR Package Settings.asset`. After normal refresh/configuration, scene generation produced no new import/compiler error. The console's later `Saving results to ...TestResults.xml` entry is test-runner output. Incidental baseline prefab-instance-ID changes from test generation are excluded from this handoff.

The earlier desktop setup diagnostic completed six finite scans, six chirps and duplicate rejection using the real Focus components and explicitly simulated input. Actual GUI Console counters were zero errors and zero warnings. Unity Core Android export and Gradle packaging succeeded (exit 0). That earlier Core APK is 69,125,912 bytes, SHA256 `7AB3966062F1C6378E5B35668D22834D68AB180D730FB52A066A1FE88D166F90`. Bundled apksigner verified v2; aapt/ZIP inspection confirmed ARM64, a nonempty IL2CPP library, minAPI32/targetAPI34, Quest3S, hand permission, required passthrough and no CAMERA permission. Preserved artifact: `.artifacts/apks/Core-20261007-071616-16b92f57/FocusCore.apk`. Gradle completed in 28m39s. See `docs/reference/evidence/setup-acceptance.json`. The refinement above supersedes it at the convenience path. **Remaining:** installation and physical acceptance on Quest3S. No actual passthrough image, hand interaction, comfort or frame-time result is claimed. The following section preserves the earlier source-only milestone for provenance; its licence blocker and ungenerated-scene statements are superseded by the checks above.

## Setup acceptance after fresh build and reopening

The actual Core APK build passed, then Hub reopened the real FocusCore project. Opening `Assets/FocusCore/Scenes/FocusCore.unity` resolved scene-dependent requirements from the initially empty scene. Core is loaded at build index 0. Meta lists zero required items and twelve recommendations; OpenXR has zero errors and fourteen warnings. No blanket Fix All was applied.

Unity's dependency panel reported Python 3.12.14 and uv 0.12.10 ready. The corrected MCP probe selected the exact FocusCore Assets path and queried the live Core scene successfully; evidence is `docs/reference/evidence/unity-mcp-connected.json`. Blender MCP scene and animation operations also succeeded in Blender 5.2.2/addon1.8/protocol13.

The saved, clearly labelled simulated desktop proof completed six scans/chirps, rejected duplicates and stopped its pulse. The real Game view displayed the purple lattice while Scanning, and the run finished with zero Console errors/warnings. Stopping restored Core and its Scene view; the build list still contains Core only. Game view scale is 1x, so the labels are readable. This exercises real Focus components without asserting headset availability or physical input.

**Warnings are disclosed, not erased:** this reopening initially logged eleven warnings: vendor URP `TraceVirtualOffset` float-to-min16float precision warnings and Meta notices from the empty scene. Unity's automatic Clear On Play reset the Console before the demo. The demo's zero counts do not prove the startup/import log is warning-free. Gradle also logs vendor manifest/deprecation warnings; it nonetheless completed successfully. Optional Meta/OpenXR recommendations include rendering/performance settings and the optional simulator. These are not required software installs or observed app failures, and performance remains unmeasured until hardware acceptance.

Feature production stayed on hold for this acceptance check. The source branch is a verified Editor/packaging increment, not a released or physically tested Quest product. The next concrete step is connecting Quest 3S, installing the new Core APK and testing passthrough, hand ray/pinch, poke, fallback, recovery, comfort and frame timing.

## Earlier source-only milestone

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
