# MR Workshop handoff

Standalone Unity root: `MRWorkshop` beside `FocusCore` in the existing isolated focus-core checkout. Source branch: `codex/focus-core`; package identity `com.thegamingdonkey.mrworkshop`. Uses Unity6000.3.25f1 / Meta207 / OpenXR1.17 / XRHands1.7.2 / URP17.3. Do not open the repository root as a Unity project. Focus remains a separate project with its October7 APK preserved.

## Current status, October10

Implemented source: three hand/controller-grabbable floating props, typed single-occupancy power socket, delayed docking after the final pointer release, cancellation, bounded size/rotation controls, powered rotor, reset refusal while held, tracking/passthrough availability handling, world-space control panel, desktop mouse/keyboard driver, scene generator, finite diagnostic and isolated Android build helpers.

Verified:16/16 pure C# rules tests and host compilation against the Workshop's own imported SDK assemblies. Actual Unity Test Runner results now pass16/16 EditMode and8/8 PlayMode, including actual Meta SDK selection cancellation. Unity generated and saved `Assets/Workshop/Scenes/Workshop.unity`. Physical Quest behaviour remains unverified.

Licence recovered: the existing Hub refreshed Personal entitlement and actual Unity Editor execution now works. The first import and first Android build each hit the bounded low-RAM guard and stopped only their owned Editor trees. Cached import resumed with one worker and completed tests; Chrome was closed under Shane's explicit low-memory permission (about4GiB working set). Other apps were left alone. Android export succeeded; native packaging is resuming from its retained cache with the Editor closed. An attempted overlap exhausted available RAM; both launchers now prevent Editor/packager overlap. No Workshop APK is claimed until actual output inspection.

Actual desktop evidence now passes both the finite socket/recovery diagnostic and injected Mouse/Keyboard events through the real Update driver: one grab, one release, one keyboard action, cell docking and a moving powered rotor. Both post-run console error queries returned zero entries. The first input run exposed shader/asset startup consuming its interaction timeout; startup warmup now has its own bound. These tests inject input and do not establish human mouse operation or headset behaviour. See [input result](evidence/desktop-input.json), [actual render](evidence/desktop-input.png), and [socket/recovery result](evidence/desktop-rules.json).

The procedural generator chat was not identifiable in the available list. Its title/folder was requested asynchronously. A concrete optional request is in [asset-request.md](asset-request.md); primitives keep the workshop independent of that handoff. The holographic console follows a working workshop increment.

## Open the Workshop

From the repository in PowerShell7, with no other Editor running:

```powershell
./scripts/workshop/Open.ps1
```

The launcher opens `MRWorkshop`, generates its actual scene on first successful import, and starts the project bridge on54486 with its own `.artifacts/workshop/mcp/status` discovery directory. Its endpoint is separate from Focus54484; the launcher defaults to one worker and refuses another Editor or this project's active Android packager. Run `./scripts/workshop/Check-Bridge.ps1` for the matching bounded probe. It uses the pinned server10.0 isolated uvx environment; bundled base Python lacks the MCP dependency and is deliberately left unchanged. Actual project discovery, instance selection and scene round trip succeeded: [connection evidence](evidence/unity-mcp-connected.json). Project discovery identifies the actual Assets folder, not merely the port. Failed licence launch is not an import success.

In Unity choose **Workshop/Open Desktop Workshop (Mouse and Keyboard)**. This starts an honestly labelled desktop mode using the same parts/socket/controller, with SDK tracking components disabled. Mouse-drag a part and release the cell near the socket; R rotates the selected free part; +/- resizes; Space toggles power; Escape cancels. Onscreen buttons duplicate these controls. Stop Play and choose **Workshop/Open Mixed Reality Scene** to restore MR.

**Workshop/Run Finite Workshop Diagnostic (Simulated Input)** separately exercises rejection, multi-pointer release, powered rotation, cancellation, reset and recovery in a bounded18-second run. It saves actual screenshots and JSON under `.artifacts/workshop/proof-*`, then restores MR. This diagnostic is not a substitute for operating the mouse controls.

**Workshop/Run Finite Desktop Input Diagnostic (Injected Devices)** queues temporary Mouse and Keyboard state through Unity's Input System and checks the actual driver counters, cell docking and powered movement. It restores the original devices/background setting even if reporting fails. GUI startup warmup is separately bounded to90 seconds, interactions to18 seconds. The bridge wrapper can invoke these with `-Action Input` or `-Action Rules`; each call performs one request and one bounded wait, no reconnect loop.

Close the owned Editor before CLI tests/build:

```powershell
./scripts/workshop/Test.ps1 -Mode EditMode
./scripts/workshop/Test.ps1 -Mode PlayMode
./scripts/workshop/Build.ps1
./scripts/workshop/Verify-Apk.ps1 -ApkPath <actual-unique-Workshop-apk>
```

Build exports to a fresh Workshop-prefixed owned cache path, packages with limited heap and explicit1–2 job bounds for Unity, IL2CPP, Bee, Gradle and CMake compile/link, and updates `.artifacts/apks/MRWorkshop.apk` only after nonempty APK output. Use `-Workers 1` on this shared laptop. A retained incomplete export can resume with `-ResumeExport <owned-export-folder>` without repeating shader work. Signature/manifest verification is a separate final check. Minimum3GiB free RAM for a build; sustained low memory stops only its owned build tree. No unrelated application is stopped. Desktop diagnostic is excluded from the Android build list.

When the headset is connected, enable Developer Mode and accept its USB-debugging prompt yourself. The charging cable must support USB data. Check the connection with `./scripts/workshop/Device.ps1`; install the inspected APK with `./scripts/workshop/Device.ps1 -Action InstallAndLaunch -ApkPath <actual-Workshop-apk>`. This verifies app identity/signature, requires an authorized Quest3S/API32+, preserves installed app data with `install -r`, and launches its verified GameActivity. With several devices, add `-Serial <Quest-serial>`. It uses private ADB54483 and leaves borrowed servers/default5037 alone. A successful launch still does not prove hands, passthrough or frame time.

## Next concrete step

Complete and inspect the fresh Workshop APK, then reopen the project for Shane. Desktop diagnostics and project-scoped Unity/Blender MCP round trips have passed. Do not rerun the already passing24 Unity tests unless changed behaviour or a diagnosed failure requires it. Human mouse operation and physical Quest hand/controller grabs, passthrough readability, reach, comfort and performance remain separate acceptance work. No MRUK or automatic furniture collision exists; objects float without gravity over a virtual workbench.


Critic source review caught a suspended test fixture and a copied CLI wrapper still pointing at Focus. Both were fixed; the fixture now asserts explicit availability, the wrapper targets MRWorkshop, and the SDK-selection test has an explicit Oculus.Interaction assembly reference. No remaining concrete source blocker was found in the reviewed code. This is not an executed Unity test result or a product score.

Compatibility fixes: restored the missing bounded Gradle properties template; removed9 unsupported/null-script volume components and7 obsolete missing renderer debug-resource references; made host helpers use Workshop dependencies; removed the bridge-confusing importWorkerCount launch flag; added configurable1–2 CLI workers. Android configuration now generates/updates its own manifest using Meta's supported silent API and asserts exactly one UnityPlayerGameActivity. Custom-manifest use is enabled in tracked settings; APK inspection also checks the packaged launch activity and contextual passthrough loading screen. A further native packaging RAM failure exposed IL2CPP's unbounded default processor count; explicit `--jobs`/`--bee-jobs` and a shared CMake compile/link pool now follow the selected worker count. Actual resumed process arguments and generated Ninja pool depth were checked, rather than assuming Gradle's worker count covered them.
