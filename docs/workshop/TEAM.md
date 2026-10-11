# Focus and Workshop specialist team

Assigned October11,2026. These are scoped agents in this chat, not additional human engineers or continuously running background services. At most three specialists can run alongside the coordinator. Finished specialists can be reassigned. This file records dispatch and deliverables; it does not prove tests, packaging or hardware acceptance.

## Current milestone

Preserve the visible Workshop baseline and finish a corrected, inspected Quest3S APK before physical acceptance. Source checkpoint `e9e6651` and status checkpoint `e8f4eb0` are pushed on `codex/focus-core`. The corrected APK remains absent. Existing APKs must remain available and must not be described as containing newer source. FocusCore and MRWorkshop remain separate Unity projects.

## Ownership

| Role | Agent | Concrete assignment | Reserved files and resources |
| --- | --- | --- | --- |
| Coordinator and integration | root | Allocate app access, integrate changes, keep evidence and Git handoff accurate | TEAM.md, root README, commits; sole live Unity/build scheduling authority |
| Program overwatch | program_overwatch | Initial brief delivered; resume for bounded final integration review | team-overwatch.md; read-only review of peer results |
| Android release | android_release_specialist | Delivered durable build supervisor and resumable packaging handoff;10 focused fixture assertions passed | build-supervisor* helper files, android-release-handoff.md; no native build while Editor is open |
| Unity/Meta/OpenXR input and Quest acceptance | unity_meta_integration_specialist | Delivered read-only capture tool and12-case human checklist; parsing and mocked paths checked | Capture-QuestAcceptance.ps1, device-acceptance.md; physical results still pending |
| Blender and procedural asset interchange | blender_asset_pipeline_specialist | Delivered optional prop recipe;15 plan checks passed,2,784 planned triangles; actual Blender export pending | scripts/workshop/assets/*, asset-pipeline.md; no foreign live Blender scene |
| Focus shader, animation and audio | focus_audiovisual_specialist | Delivered staging workflow;2 isolated tests and6 PCM checks passed; generated WAV byte-identical to current asset | build_audio.py backward-compatible authoring helper, audio-workflow*, focus-audiovisual-workflow.md; preserve runtime assets |
| GitHub and continuation handoff | github_handoff_specialist | Delivered read-only snapshot and continuation entry;30 tracked current files hashed; no export equivalence | Write-HandoffSnapshot.ps1, CONTINUE-HERE.md; no independent commit/push |

The previous `focus_critic`, `xr_runtime_specialist` and `xr_toolchain_specialist` completed their scoped work. Their saved reviews are historical evidence, not claims that they are still running.

## Scheduling rules

- Independent file edits can run together. Only the coordinator schedules a live Editor, native build or Blender authoring operation. Never overlap native packaging with an interactive Editor.
- Preserve other chats' applications and all existing source, saved assets and unique APK outputs. An open Blender process is not automatically ours.
- A check must answer a named unresolved question. Repeat it only after a diagnosed failure or relevant change. No unchanged full-suite runs or APK rebuilds merely to produce activity.
- After a failed bounded attempt, record the observed result and next concrete correction. Two identical attempts without new evidence trigger escalation to the coordinator and overwatch; do not continue repeating them.
- A finished lane reports changed files, usable output, verification scope and remaining blockers, then yields its slot. Overwatch is invoked at checkpoints; it does not pretend to watch after the chat stops.
- New source changes invalidate claims that a retained older export packages them. A helper-only change does not update an exported app runtime.

## Evidence boundaries

Corrected source has recorded19/19 PlayMode tests. Those use simulated device data and selected SDK lifecycle/handler paths. The latest desktop demonstration was visibly running and returned zero console errors; the powered mechanism was initiated through controller methods. Native automated mouse selection worked, but dragging did not demonstrate movement. Quest hands, controllers, full SDK canvas events, live recenter, passthrough, reach, comfort and frame timing remain physically unverified.

The retained corrected export is `C:\Users\mrbos\.fbc\a0f3f63c0a54\projects\Workshop-20261010-174907-695f3571`. Resume only after the owned Editor closes and resource admission succeeds. See the release handoff for the exact command; no headset is needed to package an APK.
