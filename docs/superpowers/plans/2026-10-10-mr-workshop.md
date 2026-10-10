# MR Workshop Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans inline. The authorized critic performs one final review.

**Goal:** Deliver a separate usable mixed reality object workshop with a reliable typed snap socket and powered rotor.

**Architecture:** Pure C# `WorkbenchState` owns socket and power rules; Unity `WorkshopPart`, `WorkshopSocket` and `WorkshopController` own presentation and SDK release lifecycle. Scene generation uses the existing platform prefab and official SDK QuickActions. Desktop driver uses the same controllers with explicitly simulated input.

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
- [x] Implement SDK grab lifecycle adapters, typed socket and controller/UI; eight Unity tests authored (not executed); host compilation passed.
- [ ] Generate actual MR and Desktop scenes, compile, run relevant Unity tests once.
- [ ] Demonstrate human usable mouse/keyboard controls, then finite diagnostic with evidence/screenshot.
Files: Runtime/WorkshopPart.cs, WorkshopSocket.cs, WorkshopController.cs, WorkshopDesktopInput.cs; Editor/WorkshopSceneSetup.cs, WorkshopDesktopProof.cs; Tests/PlayMode/WorkshopInteractionTests.cs.

### Task3: Review and deliver
- [x] One bounded critic source review and concrete source fixes; runtime/scene acceptance remains blocked by licence.
- [ ] Commit source, one Android build, one real package check; reopen visible project.
- [ ] Save exact evidence and concise build/run handoff; push branch. Console deferred until workshop acceptance.
Files: scripts/workshop/Run-Unity.ps1, Open.ps1, Build.ps1, Verify-Apk.ps1; docs/workshop/README.md, validation.md, evidence/.

## Execution ledger
Ruling: Interpret 'slot thing' as socket provisionally; ask asynchronously in case Shane meant another app. No unrelated app modifications.
Ruling: Source stays in existing isolated codex/focus-core worktree, standalone project is a new folder. No existing Focus files changed besides repository ignore rules/docs.
Ruling: Rather than forcibly terminating a real SDK grab on Reset, reject Reset while held and instruct release first; on tracking loss cancel pending socket actions and stop power.

Blocker: GUI launch PID50856 reports no valid Editor licence; Windows desktop is locked. Opened existing Unity Hub and requested unlock/Personal refresh asynchronously. No repeated Unity launches against unchanged failure. Host domain tests and source implementation continue.


Source checkpoint:16/16 host domain tests, four-assembly host compilation including test source. Critic fixes: explicit simulated availability, correct MRWorkshop CLI project, separate MCP54486/status, MRWorkshop.apk convenience output. No unchanged domain rerun. Eight Unity interaction tests are unexecuted; no generated scene, desktop run or APK. Console deferred until live workshop acceptance.
