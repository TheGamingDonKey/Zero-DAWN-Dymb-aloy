# Focus Core Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prepare and build a hand-operated Quest 3S Focus prototype while the headset is disconnected, then prove it on Shane's headset when he returns.

**Architecture:** One Unity project contains a minimal passthrough baseline and the Focus scene, sharing the same platform rig. Meta components own tracking and interaction; a small C# state model owns scan acceptance and timing, with separate procedural graphics and audio. One implementer executes the work and Shane's critic reviews the host/build checkpoints and final evidence.

**Tech Stack:** Unity 6.3 LTS/URP, Unity-managed Android SDK/NDK/OpenJDK, Unity OpenXR, Meta XR Core and Interaction SDK, Unity Test Framework, PowerShell, Meta VR CLI.

**Spec:** [Focus Core first headset build](../specs/2026-10-03-focus-core-design.md)

## Global Constraints

- Standalone Android APK for Meta Quest 3S; hands primary, controllers fallback.
- Use Unity 6.3 LTS with Universal Render Pipeline, Android Build Support, Android SDK and NDK tools, and OpenJDK.
- Use Unity OpenXR rather than the deprecated Oculus XR provider. Pin compatible packages after a successful import; upgrades require a documented compatibility problem.
- Initial world-space panel approximately 0.7 metres forward and slightly below eye level; tune from headset observations.
- Scan through an explicit large control, using either hand ray/pinch, direct poke, or controller selection. Both hands can operate it.
- One accepted request produces one fixed-world-origin purple pulse and one generated sound; duration approximately 1.25 seconds; requests during scanning are ignored.
- Hand loss cancels unfinished selection, does not activate on recovery, and permits an already accepted scan to finish. Head loss or suspension stops transients; restoration resets Ready and repositions the panel.
- XR/passthrough initialization failure blocks normal scan interaction and records the actual cause.
- No vision, raw RGB processing, AI service, voice, networking, multiplayer, detailed models, or cross-application overlay in this build.
- XR Simulator, XR Operator, and editor MCP are optional; no product dependency on them.
- Meaningful state tests complement physical tests. Source prepared, tests run, APK built, and headset verified are separate evidence states.
- Preserve a minimal baseline for device-first integration; disconnected-headset work does not pass hardware acceptance.
- At most two targeted corrections after diagnosing a repeated failure; stop that branch after persistent identical errors or roughly twenty minutes without new evidence. Continue independent work. Never count active download progress as stalled diagnosis.

## Review Focus

1. A held selection, duplicate event, or second interactor must not restart or queue a scan: Task 2 duplicate/expiry tests and Task 3 selection tests.
2. Hand loss during selection followed by a still-pinching hand must require release and a fresh selection: Task 3 recovery test plus physical check in Task 5.
3. Pause and tracking events arriving in different orders must keep the app blocked until all prerequisites recover: Task 2 availability-order tests and Task 4 lifecycle test.
4. A long frame or absent XR runtime must never leave a scan stuck or present a false success: Task 2 expiry test and Task 4 failure-path test.
5. Head movement during a scan must not drag its origin, and repeated scene lifetimes must not accumulate geometry/audio: Task 4 origin and resource-lifetime tests.

---

## File map

All paths below are relative to this repository. Generated Unity `.meta` files travel with their assets.

| Files | Responsibility |
| --- | --- |
| `.gitignore`; `FocusCore/Packages/{manifest.json,packages-lock.json}`; `FocusCore/ProjectSettings/` | Reproducible project and resolved dependency/settings state; ignore Library, Temp, builds, logs and credentials |
| `scripts/{Check-Environment,Run-Unity,Test,Build}.ps1` | Host inspection, logged editor invocation, checked test results, APK builds |
| `FocusCore/Assets/FocusCore/Editor/{FocusProjectSetup,FocusBuild}.cs` | Configure/validate Android XR settings and build an explicitly selected scene |
| `FocusCore/Assets/FocusCore/Scenes/{PassthroughBaseline,FocusCore}.unity`; `FocusCore/Assets/FocusCore/Prefabs/FocusPlatform.prefab` | Minimal platform baseline and product scene sharing one rig |
| `FocusCore/Assets/FocusCore/Runtime/Domain/{FocusState,SelectionGate}.cs`; `FocusCore/Assets/FocusCore/Runtime/Domain/FocusCore.Domain.asmdef` | Engine-independent scan state and selection admission |
| `FocusCore/Assets/FocusCore/Runtime/{FocusController,MetaScanInput,FocusPlatformStatus,FocusPanel}.cs`; `FocusCore/Assets/FocusCore/Runtime/FocusCore.Runtime.asmdef` | Wire SDK events, tracking status, lifecycle, panel placement and application state |
| `FocusCore/Assets/FocusCore/Runtime/{ScanPulse,ScanAudio}.cs`; `FocusCore/Assets/FocusCore/Shaders/ScanPulse.shader` | Procedural graphics and audio with explicit cleanup |
| `FocusCore/Assets/FocusCore/Tests/{EditMode,PlayMode}/` | Domain tests and component integration tests, each with its own test assembly |
| `docs/{development,validation}.md`; `README.md` | Exact installed versions, commands, verified results and remaining physical checks |

Runtime assembly references are resolved from the installed packages, rather than guessed. Domain assembly excludes UnityEngine. Editor and tests use separate assemblies. Artifacts live under ignored `.artifacts/{logs,tests,apks}/`; no artifact existence implies deployment.

Asset file paths in Tasks 2-4 are relative to `FocusCore/Assets/FocusCore/`.

## Task 1: Reproducible baseline APK

**Files:** Project/packages/settings, platform prefab, baseline scene, Editor helpers, four scripts, `.gitignore`, `docs/development.md`.

**Interfaces:** `FocusBuild.BuildBaseline()` and `FocusBuild.BuildCore()` are public static editor entry points. `Run-Unity.ps1 -UnityPath <absolute-path> -EditorArguments <string[]> -LogName <name>` returns the actual process exit status and retains its log. `Test.ps1 -UnityPath <path> -Mode EditMode|PlayMode` checks exit status and NUnit XML, failing for missing results, zero discovered tests, or any failure. Each invocation has a unique log/XML identifier. `Build.ps1 -UnityPath <path> -Scene Baseline|Core` first invalidates only its known previous output, then returns nonzero unless the current Unity invocation reports success and produces the expected nonempty APK. Old XML/APKs cannot satisfy a new invocation.

- [ ] Install Meta VR CLI from its official installer after inspecting the downloaded script; verify `metavr --version` and `metavr doctor`. Record the actual installed version; registry snapshot on October 4 is 1.8.1. Do not run broad agent initialization or change telemetry choices as a setup prerequisite.
- [ ] Install Unity Hub and Unity **6000.3.25f1** with `android,android-sdk-ndk-tools,android-open-jdk`. This LTS patch is present in Unity's official release API as of October 4. Use `metavr unity editors install 6000.3.25f1 --modules android,android-sdk-ndk-tools,android-open-jdk` when the installed CLI supports it; otherwise use Hub's supported installation path. Verify editor and all three modules, rather than interpreting a download as installation.
- [ ] Attempt a logged batch editor startup. If Hub sign-in/license or Windows elevation needs Shane, record the actual prompt and stop that dependent step. Continue source preparation; leave compilation/tests/build explicitly pending. Do not loop on activation or introduce another engine/test stack to bypass it.
- [ ] Create `FocusCore` from the Universal 3D template. Start with Meta Core, Interaction, and Interaction OVR integration **207.0.0**, and stable OpenXR **1.17.0**. These are candidates, not a claim of tested compatibility: inspect package manifests and current Meta compatibility documentation before import; adjust only for a specific declared constraint or reproduced error. Resolve URP/Test Framework/UGUI dependencies for the selected editor, then commit exact manifest and lockfile. No prerelease OpenXR.
- [ ] Inspect the installed SDK source and documented Building Blocks. Generate a camera/passthrough rig with hands and controllers enabled in `FocusPlatform.prefab`; create `PassthroughBaseline.unity` containing only this platform foundation. Use SDK assets/components, not fabricated prefab YAML or a wholesale First Hand import.
- [ ] Implement the scripts and `FocusProjectSetup.ConfigureAndValidate()` in the editor helper. Configure ARM64/IL2CPP, OpenXR Android/Quest features, hand support, and passthrough settings using the installed SDK's validation tool. Use Unity-managed Java/SDK/NDK. Record actual API levels and graphics configuration chosen by the supported setup; initial refresh target is 72 Hz. Baseline and Core share identifier `com.thegamingdonkey.focuscore`, so installs replace one another.
- [ ] Run `metavr unity editors list`, `./scripts/Check-Environment.ps1`, then `./scripts/Build.ps1 -UnityPath <verified-editor-path> -Scene Baseline`. Pass means licensed import/compile, completed successful build log, and `.artifacts/apks/FocusBaseline.apk` with recorded size and SHA-256. APK absent means no build claim. Check generated Android manifest/features with Unity's Android tools.
- [ ] Have the critic review the dependency choices, logs and APK evidence once. Commit the baseline and tooling locally; no headset success claim. If license blocks this task, source-only preparation in Tasks 2-4 is allowed, but their test/build checkboxes remain incomplete.

## Task 2: Scan state with meaningful tests

**Files:** `Runtime/Domain/FocusState.cs`, `Runtime/Domain/FocusCore.Domain.asmdef`, `Tests/EditMode/FocusStateTests.cs`, `Tests/EditMode/FocusCore.EditModeTests.asmdef`.

**Interfaces:** Namespace `FocusCore`. `enum FocusMode { Unavailable, Ready, Scanning, Suspended }`; `FocusState(double scanDurationSeconds = 1.25)` exposes `FocusMode Mode`, `double Progress`; methods `void SetAvailability(bool xrReady, bool headTracked, bool paused)`, `bool TryStartScan()`, `void Tick(double deltaSeconds)`. Initial state is Unavailable. Transitioning from blocked to valid availability restores Ready; repeated valid status updates preserve an active scan. Hand availability does not change this model.

- [ ] Write failing tests: `DuplicateRequestsAreIgnored` accepts once and rejects ten further requests; `ScanExpiresAt125Seconds` remains Scanning at 1.24, becomes Ready at 1.25, and accepts a new request; `LongFrameCompletesScan` completes with a 5-second tick and clamps progress; `UnavailableXRRejectsScan`; `RecoveryWaitsForAllPrerequisites` tests both event orders and no replay; `SuspensionCancelsScan` restores Ready with zero progress; `RepeatedReadyStatusDoesNotResetScan` preserves progress during valid status polling.
- [ ] Run `./scripts/Test.ps1 -UnityPath <verified-editor-path> -Mode EditMode`; verify the intended missing-type/behavior failure in the XML/log, not an unrelated package/license failure.
- [ ] Implement the interfaces and exact 1.25-second behavior. No Unity scene, gesture inference, renderer, audio, or queued requests inside this class.
- [ ] Repeat the EditMode command; require all named tests discovered with zero failures, and commit this independently reviewed behavior.

## Task 3: Deliberate SDK selection with controller fallback

**Files:** `Runtime/Domain/SelectionGate.cs`, `Runtime/{MetaScanInput,FocusPlatformStatus,FocusPanel}.cs`, `Runtime/FocusCore.Runtime.asmdef`, `Scenes/FocusCore.unity`, `Tests/EditMode/SelectionGateTests.cs`, `Tests/PlayMode/FocusInputTests.cs`, `Tests/PlayMode/FocusCore.PlayModeTests.asmdef`.

**Interfaces:** `SelectionGate.TrySelect(int sourceId)` returns true only for a fresh, armed selection; `void Release(int sourceId)`, `void RequireNeutral(int sourceId)` and `void Clear()` manage per-source state. A tracking-lost source is disarmed until physical neutral is observed after tracking returns; SDK cancellation/unselect caused by loss cannot count as physical release. Only map `Release` from a normal tracked release or that observed neutral. `MetaScanInput` exposes `event Action ScanRequested`, with `void OnSelect(int sourceId)`, `void OnNeutral(int sourceId)` and `void OnTrackingLost(int sourceId)` as the SDK adapter/test seam. `FocusPlatformStatus` exposes bool properties `XrReady`, `HeadTracked`, `Paused`, `HandsAvailable`, `ControllersAvailable`, and `event Action Changed`; `XrReady` includes successful passthrough initialization. `FocusPanel.Place(Transform head)` places the world-space control at the spec's initial offset. SDK source identifiers and exact selection/release/cancel callbacks are mapped from the installed package.

- [ ] Write failing tests `HeldSelectionDoesNotRepeat`, `BothHandsUseSameAction`, `ControllerUsesSameAction`, `TrackingRecoveryRequiresRelease`, `CancellationDoesNotRearmLostInput`, and `UnavailableInputDoesNotActivate`. Drive the adapter's selection/release/status seam using fakes; tests must exercise the behavior of the adapter and admission gate together, not merely assert that an event was subscribed.
- [ ] Run EditMode/PlayMode tests using `./scripts/Test.ps1` with each Mode; retain their intended failures.
- [ ] Implement admission and SDK integration using one activation edge per source. Listen for release/cancellation; do not infer global pinch or bind multiple event phases to activation.
- [ ] Create the large Scan control, short state label and supported SDK ray/poke/controller interactors. Wire both hands and controller fallback to the same request path. Display "Bring hands into view" when no supported input is available; disable selection on XR failure or suspension.
- [ ] Run both test modes using `./scripts/Test.ps1`; require all named tests discovered with zero failures.
- [ ] If XR Simulator is readily available, allow one bounded installation/configuration attempt for simulated hand/controller events; document simulation explicitly. If it is gated or incompatible, continue with component tests and builds. Physical pinch/poke validation remains Task 5.
- [ ] Commit the interaction increment after the critic checks that simulated input has not been mistaken for recognized real hands.

## Task 4: Procedural Focus effect and lifecycle

**Files:** `Runtime/{FocusController,ScanPulse,ScanAudio}.cs`, `Shaders/ScanPulse.shader`, Core scene wiring, `Tests/PlayMode/FocusRuntimeTests.cs`.

**Interfaces:** `void FocusController.RequestScan()` consumes the boolean result of `FocusState.TryStartScan()`. `void ScanPulse.Begin(Vector3 worldOrigin)`, `void SetProgress(float normalized)`, `void Clear()` own one generated mesh/material; `void ScanAudio.PlayOnce()`, `void Stop()` own one generated clip/source. Controller consumes the Task 3 status properties/event, uses unscaled time, updates status/panel and stops both effects on suspension/head loss or XR failure. All owned resources are disposed when their component lifetime ends.

- [ ] Write failing PlayMode tests: `OneAcceptedRequestProducesOnePulseAndSound` sends duplicates and counts one start each; `HandLossDoesNotCancelAcceptedScan`; `HeadMovementDoesNotMoveScanOrigin`; `PauseStopsEffectsAndResumeDoesNotReplay`; `XRFailureBlocksNormalScan`; `RepeatedSceneLifetimesReleaseOwnedResources` destroys/recreates the components and checks no owned mesh/material/clip survives.
- [ ] Run the tests using `./scripts/Test.ps1 -UnityPath <verified-editor-path> -Mode PlayMode` and inspect intended failures.
- [ ] Implement a modest translucent purple expanding pulse with the fixed origin and 1.25-second timing. Generate a short conservative sound in code; respect system volume. Do not add physics, physical-object labels, or geometry reconstruction.
- [ ] Wire the controller and platform events, reset to Ready and re-place the panel only after valid restoration. Verify panel placement uses current head pose; returning tracking cannot replay stale selection. First XR failure records its specific cause instead of logging it every frame.
- [ ] Run both test modes using `./scripts/Test.ps1`; require all named tests discovered with zero failures.
- [ ] Build Core using `./scripts/Build.ps1 -UnityPath <verified-editor-path> -Scene Core`. Pass means actual `.artifacts/apks/FocusCore.apk`, successful current build log, size and hash.
- [ ] Visually inspect the effect in the editor or available simulator and identify which environment was used. This is not passthrough/comfort evidence.
- [ ] Have the critic audit the diff, test discovery and artifact evidence once. Commit the product increment and update `docs/validation.md` with actual dates, commands, outputs, blockers and pending physical checks. Do not create empty evidence entries implying future tests passed.

## Task 5: Physical acceptance and repeatable iteration

**Files:** `docs/{development,validation}.md`, `README.md`; component/scene/settings fixes only when the headset reveals a concrete problem.

**Interfaces:** Consume the Baseline/Core builds and recorded package identifier. Use installed CLI help to confirm exact install/launch/log syntax; verify commands against the selected physical serial so another device cannot be targeted accidentally.

- [ ] With Shane, enable/verify developer mode, connect a data-capable cable and approve USB debugging. Run `metavr device list` and retrieve device/OS information. Zero devices or unauthorized state leaves deployment pending. Install drivers only if the actual connection error requires them.
- [ ] Install/launch Baseline first; inspect device logs and have Shane verify the real room and head tracking. Resolve this layer before testing Focus controls. Then install Core and validate controls before evaluating effects.
- [ ] Have Shane perform ten separated selections, left/right ray pinch, direct poke, and separate controller fallback. Confirm one sound/effect per accepted request, no held-selection duplicates, no scan on hand return, and no stuck control. Check readability and reach; adjust the initial 0.7-metre layout only from observed feedback.
- [ ] Test headset suspension/resume and head-tracking recovery. Verify a current panel, Ready state, no old effect/audio replay, and no launch/recovery exceptions in logs.
- [ ] Measure rendering against the 72 Hz/72 FPS target during core use; retain the measurement context and disclose capture overhead or lower performance. No desktop benchmark substitutes for it.
- [ ] Make a small visible status-label change, rebuild, reinstall and launch on the same headset. Retain both build identifiers and Shane's confirmation; this is the iteration acceptance gate.
- [ ] Critic performs one final evidence review. Record which criteria passed, failed or are pending; fix consequential failures before declaring the milestone complete. Keep runtime/build fixes tested and committed. Provide the project path, latest APK path, exact deployment commands and next useful increment.

## Host constraints and handoff

On October 4, 2026, this checkout is on local branch `codex/focus-core-design`; the spec exists but no Unity project/APK does. Unity/Meta CLI were absent from the checked paths; about 298 GB is free. The shell is not elevated. Shane authorized installation and administrator use, but interactive UAC, Unity ID sign-in/license, Meta verification and headset prompts can still require his participation. No passwords or verification codes should be put in project files or logs.

Expected outcome while Shane is away: installed tools where unattended installation works, committed source and project, retained baseline, meaningful host tests, and APKs if a usable license permits compilation. If an account/install prompt blocks execution, leave the source ready for import and identify the exact required action; do not claim tests or APKs exist. Host remaining powered, awake and connected enables uninterrupted downloads/builds. Twelve hours is an available window, not a promise of total completion or an autonomous schedule after this chat stops.

## Primary setup references

- [Unity 6 LTS support](https://unity.com/releases/unity-6/support); [selected editor release](https://unity.com/releases/editor/whats-new/6000.3.25f1).
- [Meta Unity setup](https://developers.meta.com/vr/documentation/unity/unity-project-setup/); [Unity/OpenXR compatibility](https://developers.meta.com/vr/documentation/unity/unity-and-openxr-compatibility/).
- [Meta developer environment](https://developers.meta.com/vr/essentials/metavr-environment/); [CLI installation](https://developers.meta.com/vr/essentials/metavr-install/).
- [Unity license activation](https://docs.unity.com/en-us/hub/manage-license); [device setup](https://developers.meta.com/vr/documentation/native/android/mobile-device-setup/).
- [Meta Core package metadata](https://npm.developer.oculus.com/com.meta.xr.sdk.core); [Interaction metadata](https://npm.developer.oculus.com/com.meta.xr.sdk.interaction); [integration metadata](https://npm.developer.oculus.com/com.meta.xr.sdk.interaction.ovr).
