# Workshop team oversight

Snapshot: October 11, 2026. This is a bounded review of recorded evidence and specialist reports, not a continuously running monitor. Project root: `MRWorkshop` in `C:\Users\mrbos\Downloads\Git Hub Local_remote\Zero-DAWN-Dymb-aloy\.worktrees\focus-core`, branch `codex/focus-core`. FocusCore remains a separate project.

## Honest current status

| Stage | What the evidence establishes | What remains open |
| --- | --- | --- |
| Implemented source | Runtime corrections at `e9e6651`: individual source loss cancels selection/docking, stale UI presses are rejected, recenter clears pending actions, and source registration/bindings were corrected. Status docs are at `e8f4eb0`. | Existing local Unity scene/settings changes are separate from those commits; preserve and reconcile them before claiming an exact package checkpoint. |
| Locally tested | 19/19 actual Unity PlayMode tests passed, with zero failures/skips. The saved scene has 14 nonnull source bindings. Corrected injected Mouse/Keyboard events passed through the real Update driver with zero reported Console errors. | Simulated device state and direct handler calls do not prove physical event delivery or all SDK canvas interactions. |
| Visible desktop | The last captured DesktopWorkshop was in Play mode. Native mouse selection responded. A controller-method demonstration docked the cell and powered the rotor, with zero Console errors. | Native automated drag did not demonstrate movement. Full human mouse operation remains unproven. The captured running state is not a claim about the current screen. |
| Packaged | The earlier signed/inspected APK is preserved at `.artifacts/apks/Workshop-20261010-105438-9c97bf4a/MRWorkshop.apk`. | It predates the runtime corrections. The corrected export succeeded, but native packaging stopped without a captured final result or established cause. No corrected APK exists. The export-to-`e9e6651` association is recorded in `build-interrupted.json`; no cryptographic per-export source manifest independently proves it. |
| Pushed / released | Root reports `e9e6651` and `e8f4eb0` pushed to the source branch. | Pushed source is not a published release or an installed application. No corrected release is established. |
| Physical Quest | No physical acceptance evidence exists. | Real hands/controllers, passthrough readability, recenter/tracking recovery, reach, comfort and performance must be checked on Quest3S. |

Team roster and handoffs: [TEAM.md](TEAM.md), owned by root. Root reports the top-level README now distinguishes the historical APK from corrected source.

Evidence: [current handoff](README.md), [runtime review](evidence/runtime-review.json), [green PlayMode results](evidence/source-lifecycle-green.xml), [scene generation](evidence/source-generation.json), [corrected desktop input](evidence/desktop-input-corrected.json), [visible desktop capture](evidence/desktop-reopened.json), [interrupted packaging](evidence/build-interrupted.json), [historical APK inspection](evidence/apk.json). Earlier 16/16 EditMode and 8/8 PlayMode results remain evidence for their recorded baseline, not fresh verification of every correction.

## Bounded integration review, October11

Judgement: useful work was produced, and the Workshop remains a rough prototype awaiting its corrected package and real input acceptance. This wave supplied delivery/authoring tools rather than a new runtime feature. No numerical critic score or whole-pipeline pass is claimed.

| Delivered artifact | Review evidence | Practical limit |
| --- | --- | --- |
| [Build supervisor and release handoff](android-release-handoff.md) | Read the wrapper and its focused fixture code; retained fixture statuses include exact exits0 and23 and deliberately nonterminal history. Specialist reports10 assertions passed. | Uses the existing Build admission/lifecycle, records durable status/logs and does not retry. No real native package ran; forced termination can still prevent finalization. |
| [Quest capture and checklist](device-acceptance.md) | Read `Capture-QuestAcceptance.ps1`: bounded ADB probes, unique telemetry, separate12-case pending observations, installed-byte identity explicitly unverified. Root reports parsing/mocked paths, including its stopped-app/null-PID correction. | No physical device session. Buffered current-PID logs and Android `gfxinfo` cannot establish physical interaction or XR frame time. |
| [Optional prop recipe](asset-pipeline.md) | Read geometry plan/exporter/verification code. Specialist reports15 pure-plan checks and2,784 triangles. Source `.blend` stays outside Unity Assets; the recipe refuses existing output folders. | No Blender execution, real PNG/FBX output or Unity import. Planned geometry is not a visible delivered prop. |
| [Focus audio staging](focus-audiovisual-workflow.md) | Read staging helper/tests and backward-compatible generator diff. Read the actual staged report: source unchanged, staged WAV byte-identical, all six PCM checks pass. Two focused subprocess tests are reported. | No runtime asset changed; numerical checks do not establish listening quality, game fidelity or headset synchronization. |
| [GitHub continuation entry](CONTINUE-HERE.md) and snapshot helper | Read the helper and handoff. One recorded snapshot has stable HEAD `e8f4eb0`,30 current tracked hashes (Focus18/Workshop12) and changed paths. | Sequential current-file capture does not establish export/APK/installed identity. Root integration and push of this wave remain separate. |

All six scoped lanes delivered useful artifacts. No new concrete code defect was found in this bounded read. One documentation integration defect was sent to root and the audio owner: the Focus audiovisual handoff linked the Workshop-only device checklist as a Focus hardware route. That checklist's commands target `com.thegamingdonkey.mrworkshop`; label it Workshop-only and use the existing Focus validation/deployment route for Focus acceptance. The audio owner corrected the link, and the final paragraph was read back: Focus now links its validation/deployment docs and explicitly labels the adjacent checklist Workshop-only.

Runtime/source scope stays frozen for corrected Workshop delivery. Helper-only changes do not enter a retained Android export or update an installed app. Reconcile and commit this wave's intended files separately from preserved dirty Unity settings; record a precise source/export/package association when packaging finishes. No existing full suite, app control, build, asset export or device action was performed by oversight.

The next product step remains one admitted supervised resume and inspection of the unique corrected APK, followed by human desktop and Quest acceptance. Optional asset export and audio listening are later bounded authoring tasks. Their successful helper checks must not replace that remaining product step.

## Finite priorities and lane ownership

1. **Android release specialist:** inspect the retained export/build handoff and supply durable final outcome reporting if the current helpers lack it. Root owns any actual close/resume operation. Do one admitted, bounded native package attempt after the Editor closes and sufficient RAM is available, then inspect that unique APK and record the source/export identity, final exit, size, hash, signature and manifest. The Android review found a live PID/start-time marker without a durable terminal status and delivered a small supervisor wrapper; its fixture checks remain helper evidence. Helper improvements alone do not finish delivery.
2. **Unity/Meta integration specialist:** read-only integration evidence and a short physical acceptance checklist. Keep source/scene binding evidence separate from SDK event and headset proof. The integration review confirms those boundaries and delivered bounded read-only device capture plus an explicitly pending human checklist. The next acceptance step is one session on the inspected corrected APK after an authorized Quest3S is connected. Root owns live app operations and user-facing demonstration.
3. **Program oversight:** this document, evidence boundaries, duplicate-work/loop warnings and a bounded final integration review when root requests one. No app control, build/test reruns, source edits or commits in this lane.
4. **Root:** team handoff, README consistency, resource admission, final integration and source/checkpoint handoff. Later procedural assets and Focus audiovisual work are separate increments with concrete outputs; they must not obscure the pending corrected Workshop package.

At the initial October11 report, root observed about 0.93 GiB free RAM and the Workshop Editor still running. No heavy Unity/native build or new Blender operation is admitted in that state. Root identified two Blender4.5.13 processes as belonging to another procedural-generator chat; leave them untouched. Use that chat's assets through an agreed export recipe, not by controlling its session.

The initial peer pass found no duplicated live app/build work. The concrete build failure is missing terminal outcome capture, with cause unknown; missing corrected APK and unproven native drag remain product gaps. New helper edits require a new source checkpoint before packaging claims reference them.

## Stop rules that prevent loops

- Each lane starts with one concrete artifact or decision and an exit condition. A review is complete when its scoped finding is recorded; it does not become an open-ended audit.
- Do not rerun unchanged passing suites, re-import unchanged assets or rebuild unchanged runtime. Repeat only after a relevant change, a diagnosed failure, or an unresolved concern that the repeat can actually answer.
- If an operation fails or ends without a result, preserve its owned logs/cache and report the known state. Diagnose the missing outcome before repeating it; do not invent a cause or keep issuing the same launch.
- Do not add a new review layer merely because an earlier reviewer finished. When two lanes investigate the same question, root names one owner and the other records the handoff.
- Packaging can stop honestly at a concrete admission blocker, such as insufficient RAM or an open Editor. Keep the corrected APK marked pending. A critic score, mock test or screenshot cannot promote it to ready.
- Oversight stops after this brief and peer integration findings. Root may request another bounded review after a material change; no always-on supervision is implied.

## The small amount Shane needs to learn next

- **Unity scene, Game view and Play mode:** a scene is the saved arrangement of objects; the Game view shows the camera's playable output. The Play button runs it, and stopping Play returns to editing. Open DesktopWorkshop, use its Game view, and practice selecting, dragging, releasing the cell near the socket and pressing Space. Observe which action changes the mechanism before learning more Editor panels. Changes made while playing generally need deliberate persistence; do not assume a runtime demonstration saved the scene.
- **C# component and Inspector:** a component is behaviour attached to a scene object. Selecting the socket or a part lets the Inspector show its component and assigned references. Start by tracing one visible action, cell docking, from that object to the responsible component and its fields. Shane does not need to understand the whole SDK to operate this increment.
- **Deployment and the USB prompt:** enable Developer Mode, use a cable that carries data, and accept the headset's USB-debugging prompt himself. The device helper then checks authorization and installs the inspected APK. Installation/launch is a milestone; it is followed by testing hands, passthrough and recovery in the headset.
- **Blender for assets:** use Blender to create or edit models and export an agreed asset for Unity. It is optional for the working primitive-based Workshop and is not the place to debug C# input, Android packaging or headset tracking.

Teach one action at a time, show its result, then let Shane repeat it independently. Unproven native dragging is an acceptance gap to investigate, not evidence that he failed to learn. Implementation should produce a usable increment before extra tool lessons or cosmetic asset work expand the scope.
