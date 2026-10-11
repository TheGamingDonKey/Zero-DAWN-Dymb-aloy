# Continue Focus and MR Workshop here

Recorded October 11, 2026. The specialist helpers and handoff are integrated in the commit containing this entry. Inspect current Git HEAD/status before making source or publication claims; APK and physical acceptance remain separate.

## Exact projects and source

Checkout: `C:\Users\mrbos\Downloads\Git Hub Local_remote\Zero-DAWN-Dymb-aloy\.worktrees\focus-core`. Branch: `codex/focus-core`. Origin: <https://github.com/TheGamingDonKey/Zero-DAWN-Dymb-aloy>. Observed HEAD: `e8f4eb0` (status handoff), following corrected Workshop source checkpoint `e9e6651`. These checkpoints are recorded pushed; a new Codex must inspect its local HEAD/status before claiming current changes are pushed.

Open the **FocusCore** or **MRWorkshop** subfolder as a Unity project. Neither this chat folder nor the repository root is a Unity project. Editor: `C:\Users\mrbos\Documents\UnityEditors\6000.3.25f1\Editor\Unity.exe`. Blender is optional asset authoring; it is not needed to run either APK. Existing generator-owned Blender sessions belong to separate work and must remain available.

Read [working agreements](../../AGENTS.md), [Focus setup](../development.md), [Focus evidence](../validation.md), [Workshop implementation/run handoff](README.md) and [team ownership](TEAM.md). Do not restart completed setup/research or repeat unchanged suites. Source, Editor tests, exports, inspected APKs, pushed commits, release publication and physical acceptance are separate evidence.

## Resume the actual unfinished package

The corrected Workshop APK is absent. [Interruption evidence](evidence/build-interrupted.json) associates source `e9e6651` with this retained export:

```text
C:\Users\mrbos\.fbc\a0f3f63c0a54\projects\Workshop-20261010-174907-695f3571
```

Unity export returned success. Native packaging stopped without a captured final exit/cause; do not label it a RAM failure from incomplete logs. No independent runtime hash manifest was captured for that export. A resumed build packages its saved export, regardless of checkout HEAD. A new checkout snapshot cannot retroactively prove that association or that later local settings are included.

Preserved historical Workshop APK: `.artifacts/apks/Workshop-20261010-105438-9c97bf4a/MRWorkshop.apk`, also `.artifacts/apks/MRWorkshop.apk`; 69,101,824 bytes, SHA256 `D2FC31220C507224CCAC40130F4D7D214608E17580F364AC3C152E72AA1DE2A1`. [Inspection](evidence/apk.json) predates the corrections. It must not be presented as corrected runtime.

Preserved target-enabled Focus APK: `.artifacts/apks/Core-20261007-104652-fb4cbfc5/FocusCore.apk`, also `.artifacts/apks/FocusCore.apk`; built from `b26ae438f48e68a09f9537e0cf9bb207eac11d60`, 69,155,740 bytes, SHA256 `B6138EB6B223B9A63FB2712E8700E1A94CC79C33BB2277107BB8CE6FD5BD6CC2`. [Focus inspection](../reference/evidence/target-apk.json) establishes that package, not newer checkout/runtime or device behavior. These are local artifacts; no new release publication is established.

The next packaging action is **one supervised resume with one worker**, once the owned Editor session has ended and existing admission conditions allow it. At handoff the Editor and independent Blender sessions were open, with less than 1 GiB available RAM. Preserve them; do not launch packaging now or stop unrelated apps. Existing builds require at least 3 GiB and reject conflicting authoring sessions. No headset is needed for packaging.

From the checkout in PowerShell 7:

```powershell
$run = & ./scripts/workshop/build-supervisor.ps1 -Workers 1 -ResumeExport 'C:\Users\mrbos\.fbc\a0f3f63c0a54\projects\Workshop-20261010-174907-695f3571'
$run
& ./scripts/workshop/build-supervisor.ps1 -InspectRun $run.RunDirectory
# Only after success: use its newly produced unique APK path, not an old convenience file.
./scripts/workshop/Verify-Apk.ps1 -ApkPath <new-unique-Workshop-APK>
```

Save `RunDirectory` and read terminal `BuildExitCode`; an unknown/lost supervisor remains unknown. Record new APK hash/size and its retained-export association. Diagnose a concrete failure before another attempt. See [release handoff](android-release-handoff.md) for status semantics and focused fixture evidence.

## Open, check and author

Use only the project needed for the current milestone. Live app/build access belongs to root; never overlap an interactive Editor with native packaging.

| Purpose | Command or Unity menu | Evidence boundary |
| --- | --- | --- |
| Open Workshop | `./scripts/workshop/Open.ps1 -Workers 1` | Opens MRWorkshop; matching bridge uses port 54486. |
| Check matching Workshop bridge | `./scripts/workshop/Check-Bridge.ps1` | Verifies this project's discovery/scene, not Quest input. |
| Operate desktop Workshop | **Workshop/Open Desktop Workshop (Mouse and Keyboard)** | Drag/release, R rotate, +/- resize, Space power, Escape cancel; human/native dragging still needs demonstration. |
| Restore MR scene | **Workshop/Open Mixed Reality Scene** | Saved Android scene is `MRWorkshop/Assets/Workshop/Scenes/Workshop.unity`. |
| Relevant Workshop tests | `./scripts/workshop/Test.ps1 -Mode EditMode` or `-Mode PlayMode` | Editor must close first. Corrected source already records 19/19 PlayMode; rerun only for a relevant change/failure. |
| Fresh Workshop export/build if runtime changes | `./scripts/workshop/Build.ps1 -Workers 1` | Creates a new export; do not reuse old export to claim changed runtime. Prefer the supervisor for durable outcomes. |
| Open Focus | Open `FocusCore` through Unity Hub; scene `Assets/FocusCore/Scenes/FocusCore.unity` | Bridge port 54484 is separate from Workshop. |
| Focus package if required | `./scripts/Build.ps1 -Scene Core`; then `./scripts/Verify-Apk.ps1 -ApkPath <new-unique-Focus-APK>` | Preserved October 7 package already exists; no unchanged rebuild is required. |
| Connect/install Workshop after inspected packaging | `./scripts/workshop/Device.ps1`; `./scripts/workshop/Device.ps1 -Action InstallAndLaunch -ApkPath <inspected-unique-APK>` | User enables Developer Mode/USB debugging; installation preserves app data. Launch is not acceptance. |

Workshop uses floating primitive parts, socket/controller rules and a virtual workbench; no room understanding/MRUK/furniture collision is implemented. Focus reveals authored virtual records, not recognized physical objects. Corrected Workshop tests use simulated SDK state/handler paths. The visible desktop powered mechanism used controller methods with zero console errors; native mouse selection responded, but dragging did not demonstrate movement. Physical hands/controllers, canvas events, recenter, passthrough, reach, comfort and frame time remain unverified for both projects.

## Specialist outputs and current local changes

| Output | What it does | Integration status |
| --- | --- | --- |
| [Android release](android-release-handoff.md), [supervisor](../../scripts/workshop/build-supervisor.ps1) | Retains unique request/status/logs around the existing bounded build; does not retry or schedule a second build system. | Delivered and integrated with this entry. |
| [Quest acceptance](device-acceptance.md), [capture](../../scripts/workshop/Capture-QuestAcceptance.ps1) | Reads connected device/app telemetry and creates a separate pending human checklist; never turns logs into physical passes. | Delivered and integrated with this entry. |
| [Asset interchange](asset-pipeline.md), [optional recipe](../../scripts/workshop/assets/build_workshop_assets.py) | Original optional Blender props with units/pivots/budgets; no runtime import or existing scene replacement is implied. | Delivered and integrated; real Blender export/Unity import pending. |
| [Focus audiovisual workflow](focus-audiovisual-workflow.md), [staging helper](../../scripts/workshop/audio-workflow.py) | Reproducible original audio/reference workflow; backward-compatible `scripts/reference/build_audio.py` remains available. Generated staging output is not automatically runtime output or a perceptual fidelity pass. | Delivered and integrated; staged WAV matches the working asset, listening/headset checks pending. |
| [Overwatch](team-overwatch.md) | Bounded source/tool/process review and remaining evidence gaps. | Delivered and integrated; bounded final review completed. |
| [Snapshot helper](../../scripts/workshop/Write-HandoffSnapshot.ps1) | Captures HEAD/branch/origin, changed paths and selected current tracked runtime hashes to a unique ignored JSON. | Delivered and integrated; earlier local snapshot retained. |

Dirty Unity changes observed in **both** projects include XR/OpenXR, player/build/render settings, and Workshop's DesktopWorkshop scene. Focus additionally has untracked generated Android manifest/settings assets. They are preserved local changes, not an approved runtime checkpoint or evidence of export inclusion. Specialist helpers and README/status documentation are integrated by the commit containing this entry; the separate local Unity changes remain preserved. Do not reset them, stage all indiscriminately, commit another lane independently, or call their final HEAD known before integration. Helper-only changes and optional asset recipes do not update an APK runtime. Review the actual diff when integration is scheduled.

Capture local continuation state without running any app, build or network command:

```powershell
./scripts/workshop/Write-HandoffSnapshot.ps1
```

The result is machine-readable and points to `.artifacts/workshop/handoff-<UTC>-<unique>.json`. It hashes **current tracked** runtime C#/asmdef, shaders, saved scenes and package/Android manifests in both projects. Untracked generated files and other assets/settings/caches are outside hash scope and remain visible only by changed path. Reads are sequential, not an atomic snapshot. HEAD stability alone does not prove file stability. The helper explicitly establishes **no export/APK/installed-byte equivalence** and no physical evidence. Run another snapshot after final integration if needed; retain the earlier one as its own dated local capture.
