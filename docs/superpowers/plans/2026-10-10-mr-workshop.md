# MR Workshop Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans inline. The authorized critic performs one final review.

**Goal:** Deliver a separate usable mixed reality object workshop with a reliable typed snap socket and powered rotor.

**Architecture:** Pure C# `WorkbenchState` owns socket and power rules; Unity `WorkshopPart` and `WorkshopController` own presentation, the socket and SDK release lifecycle. Scene generation uses the existing platform prefab and official SDK QuickActions. Desktop driver uses the same controllers with explicitly simulated input.

**Tech Stack:** Unity6000.3.25f1, Meta207, OpenXR1.17, XRHands1.7.2, URP17.3, NUnit.

**Spec:** docs/superpowers/specs/2026-10-10-mr-workshop-design.md

## Global Constraints
- Preserve Focus project/source/configuration and previous APKs.
- Package `com.thegamingdonkey.mrworkshop`; separate Unity project `MRWorkshop`.
- Socket radius0.14m; resize0.75–1.5; rotation15degrees; held/docked transform changes reject.
- No forced reset of held objects; no docking on cancellation/tracking loss.
- No headset claims from desktop simulation; no blanket SDK Fix All.

## Review Focus
- Multi-pointer release: first hand release must not dock while another holds.
- Occupied or incompatible socket: no silent replacement.
- Tracking interruption: cancel pending docking and stop the rotor.
- Held/docked controls: never fight SDK transforms or move a held object on reset.
- Desktop paths: human usable input and finite diagnostic must both work.

### Task1: Rules and standalone project
- [x] Copy tracked pinned settings/platform into standalone MRWorkshop; no Library or credentials.
- [x] Write rules tests, observe intended failures, implement, run one meaningful domain pass (16 failed NotImplementedException, then16/16 passed).
Files: Runtime/Domain/WorkbenchState.cs; Tests/EditMode/WorkbenchStateTests.cs; scripts/workshop/Test-Domain.ps1.
Interface: `WorkbenchState.TryDock(id,kind,held,distance)`, `Undock(id)`, `TogglePower()`, `Reset()`, `ClampSize(value)`.

### Task2: Live interaction and desktop mode
- [x] Implement SDK grab lifecycle adapters, typed socket and controller/UI; host compilation and actual8/8 Unity PlayMode tests passed.
- [x] Generate actual MR scene, compile, run relevant Unity tests (16/16 EditMode,8/8 PlayMode).
- [x] Generate Desktop scene and execute both finite diagnostics, including injected events through the real Update driver; actual screenshots saved and console queries returned zero errors.
- [ ] Demonstrate human usable mouse/keyboard controls, then finite diagnostic with evidence/screenshot.
Files: Runtime/WorkshopPart.cs, WorkshopController.cs, WorkshopDesktopInput.cs; Editor/WorkshopSceneSetup.cs, WorkshopDesktopProof.cs, WorkshopDesktopInputProof.cs; Tests/PlayMode/WorkshopInteractionTests.cs.

### Task3: Review and deliver
- [x] Bounded critic reviews and concrete source fixes; licensing recovered and actual Unity tests passed.
- [x] Commit source, one successful Android package and signature/manifest/native check; reopen saved MR scene and prove its live bridge.
- [ ] Confirm Shane can see and operate the Editor after unlocking Windows; API and rendered diagnostics do not establish human operation.
- [x] Save exact evidence and concise build/run handoff; push branch. Console deferred until workshop acceptance.
Files: scripts/workshop/Run-Unity.ps1, Open.ps1, Build.ps1, Verify-Apk.ps1; docs/workshop/README.md, evidence/.

## Execution ledger
Ruling: Interpret 'slot thing' as socket provisionally; ask asynchronously in case Shane meant another app. No unrelated app modifications.
Ruling: Source stays in existing isolated codex/focus-core worktree, standalone project is a new folder. No existing Focus files changed besides repository ignore rules/docs.
Ruling: Rather than forcibly terminating a real SDK grab on Reset, reject Reset while held and instruct release first; on tracking loss cancel pending socket actions and stop power.

Recovered blocker: GUI launch PID50856 initially reported no valid Editor licence. Hub refreshed Personal entitlement; actual Unity tests and Android export now succeeded. No repeated licence retry was made against unchanged failure. Native screen interaction was unavailable while the desktop was locked.


Source checkpoint8e30e92 pushed:16/16 host domain tests, four-assembly host compilation including test source. Subsequent actual Unity tests passed16/16 EditMode and8/8 PlayMode; MR scene generated and Android export succeeded. Console deferred until live workshop acceptance.

Compatibility recovery: restored missing Gradle properties template; removed invalid/null volume scripts and obsolete missing renderer debug resources; corrected helper dependency paths; generated the required GameActivity manifest through Meta's API. Import/export resource guards stopped only owned failed runs; cached work resumed with one Unity worker. Chrome was closed under explicit low-memory authorization, other applications were preserved. A native packaging resource failure exposed unbounded IL2CPP jobs despite Gradle limits. Explicit IL2CPP/Bee limits and one shared CMake compile/link pool now follow Workers; actual emitted arguments and Ninja pool depth verified. Retained export packaging resumed; no APK claimed until fresh output verification.

Bounded tooling checks: private ADB helper Check executed with no connected headset, no installation; Blender5.2.2/addon1.8/protocol13 real MCP scene/source-library read passed through normal Store launcher alias, temporary empty session closed. Unity MCP selected exact MRWorkshop Assets and queried the loaded MR scene. Editor-only injected-input and socket/recovery acceptance diagnostics actually passed, with screenshots and zero-error console queries. Input diagnostic startup timing was fixed after first GUI asset warmup consumed its interaction timeout. Human input and physical Quest remain pending.

Resource correction: opening Editor alongside native packaging exhausted RAM even with one native job; the owned packaging guard stopped it. Editor diagnostics completed, saved MR scene restored, owned Editor stopped;7.81GiB free before cache resume. Open/Build now prevent this overlap, including resumed exports. Bridge wrapper now uses the pinned uvx server environment because bundled base Python lacks mcp. No global dependency install was performed.

Final offline delivery: Gradle succeeded after64 minutes with one native worker. Fresh Workshop APK69,101,824bytes passed v2 signature, ARM64-only IL2CPP, Quest3S/hand/passthrough/GameActivity inspection. Unity reopened, a fresh bounded socket/recovery diagnostic passed with zero console errors, and a final scene query confirmed clean saved Workshop restored. Durable Blender launcher executed; actual protocol13/addon1.8 handshake and scene query passed. Bridge helper now fails absent Editor, accommodates bounded startup/runtime completion, and handles a partially written result without rerunning the job. Physical Quest and human interaction remain pending.
