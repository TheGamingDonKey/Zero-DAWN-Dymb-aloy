# Quest 3S acceptance session

The corrected runtime has recorded PlayMode coverage for source-loss cancellation, stale UI presses, recenter handling, runtime source attachment and exact SDK HandRef binding. Those tests exercise simulated device state and handlers. Physical SDK canvas dispatch, headset recenter events and each real hand/controller still require this session. The October 10 inspected APK is a historical baseline; package and inspect the corrected export before using it for corrected-runtime acceptance.

Use the actual unique inspected APK and its build evidence. Record its hash and packaged source revision; the current checkout revision alone cannot identify an APK. `Device.ps1 -Action InstallAndLaunch` performs the authorized update/launch separately, retaining app data. The capture below only reads the connected device, installed Workshop metadata and recent app telemetry. It uses private ADB 54483, preserves borrowed servers and never clears logcat or changes app data.

```powershell
# From the repository in PowerShell 7, after the corrected APK is inspected:
./scripts/workshop/Device.ps1 -Action InstallAndLaunch -ApkPath <actual-unique-inspected-APK> -Serial <Quest-serial>
# Perform the cases below, then capture immediately while Workshop still runs:
./scripts/workshop/Capture-QuestAcceptance.ps1 -Serial <Quest-serial> -ApkPath <same-APK> -PackagedSourceRevision <revision-from-build-record>
```

The capture prints its unique `.artifacts/workshop/device-*` directory. `telemetry.json` records device model/API/build, installed package version, app PID, local APK hash, probe availability and limits. `app-logcat.txt` contains at most 1,000 recent buffered lines from the current Workshop PID; `app-error-candidates.txt` selects potential errors for human review. This is not a complete or timed interaction trace, and an empty error file does not establish a pass. A crash/restart may remove the old PID from this snapshot; note the observed failure and retain its time for targeted follow-up. A missing/unauthorized headset is recorded as pending, not a failed interaction test.

`human-observations.json` is a separate pending template. Fill operator, device serial, installed version, UTC session start/end and each case's observed time, status (`pass`, `fail`, `not_observed`), observation and any screenshot/video/metric reference. Keep the original telemetry unchanged. A skipped case stays `not_observed`; do not convert it to a pass from desktop evidence or logs. For a second run, create another capture directory.

| Case ID | Human action | Required observation |
| --- | --- | --- |
| passthrough-readability | Launch in the physical room, look around and approach the panel/workbench. | Real room remains visible; panel/parts read clearly in stereo and stay reachable. Record any visual discomfort. |
| left-hand-grab-release | Put controllers aside. With the left hand, grab/move the cell; release away from socket, then grab and release near socket. | Movement follows the hand, away release does not dock, valid socket release docks once. |
| right-hand-grab-release | Repeat the same sequence using the right hand. | Same behaviour independently; a left-hand pass does not cover this case. |
| left-hand-poke-ray-buttons | With the left hand, use near poke and far ray/pinch on fresh presses for rotate, resize and power. | Both SDK canvas paths respond once per intended press; record paths separately if only one is usable. |
| right-hand-poke-ray-buttons | Repeat near poke and far ray/pinch with the right hand. | Correct action and hand routing for both paths. |
| left-controller-grab-buttons | Switch to controller input; use the left controller to grab/release and operate panel buttons. | Correct movement, docking on valid release, and one button action per fresh press. |
| right-controller-grab-buttons | Repeat independently with the right controller. | Same behaviour; switching sources leaves no stuck selection. |
| source-loss-no-dock | With each hand in turn, hold the cell near the socket and move the hand out of tracking without intentionally releasing. Restore tracking. Repeat controller loss only if a normal, safe power-off/loss action is available. | Loss cancels the pending release; the cell does not dock on loss or recovery. A fresh valid grab/release works. Record each source actually tested. |
| stale-button-press | Begin a hand/button press, lose its tracking before release, then return and release without a new press. Repeat for the other hand and available controller source. | Old press does not toggle power/rotate/resize; a new press works once. If the loss cannot be induced mid-press, mark that source unobserved. |
| recenter-held-and-pending-release | Use the headset's normal recenter command while holding a free part, then in another attempt immediately after release near the socket. | Workbench recovery follows the new pose; selections/pending release clear and no stale docking/action occurs. Record if the timing attempt was inconclusive. |
| pause-resume | Suspend via the normal headset/system flow while holding or pressing; resume without relaunching. | Workbench becomes usable after tracking returns; held selection/old button press does not resume; fresh interaction works. |
| comfort-and-performance | Operate hands/controllers with the powered rotor for several minutes, looking around naturally. | Record duration, visible hitching/judder, stereo/reach issues and comfort. Attach XR frame-time capture only if a supported metric provider is already available. |

`app-memory.txt` is an Android memory snapshot. `android-window-statistics.txt` is a best-effort `dumpsys gfxinfo` snapshot, which may have no useful Unity XR frames. Neither establishes headset CPU/GPU frame time, refresh rate, dropped frames or sustained performance. This script does not enable an overlay, install a metrics tool or change device settings. Until an actual XR metric capture is attached, measured performance remains unverified even if the operator reports comfortable movement.

Completion requires the corrected APK provenance, telemetry and the operator's explicit observations for every required case. Keep source-level test evidence, APK inspection and physical acceptance distinct. Failures should produce a concrete source/event reproduction before any runtime change; inconclusive timing should remain inconclusive.
