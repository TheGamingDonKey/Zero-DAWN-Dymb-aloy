# Focus development setup

Updated October 7, 2026. Use the existing project; a chat folder move does not move source.

Repository: https://github.com/TheGamingDonKey/Zero-DAWN-Dymb-aloy, branch `codex/focus-core`.

Checkout: `C:\Users\mrbos\Downloads\Git Hub Local_remote\Zero-DAWN-Dymb-aloy\.worktrees\focus-core`. Open its **FocusCore** subfolder in Unity Hub. Hub 3.22.2 has this project and Unity 6000.3.25f1 registered. The existing Personal licence is active; Hub opening and a fresh command-line launch succeeded. Earlier exit 198 is historical.

Editor: `C:\Users\mrbos\Documents\UnityEditors\6000.3.25f1\Editor\Unity.exe`. Bundled Android tools: OpenJDK 17.0.18+8, NDK r27c, CMake 3.22.1, SDK/build tools 36, Gradle 9.3.1 and Android Gradle Plugin 9.0.0. Android Studio and another Java installation are unnecessary.

## Open and demonstrate

Open `Assets/FocusCore/Scenes/FocusCore.unity`. **Focus/Generate First Focus Demo** regenerates the actual MR scene: one SDK platform rig, transparent camera and passthrough underlay, eight hand/controller ray/poke adapters, world-space Scan card, original reticle, pulse and chirp. Required contextual-passthrough loading-screen configuration is applied. The latest Core check has no required Meta items; twelve optional recommendations and fourteen OpenXR validation warnings await device/performance work.

Without a headset, choose **Focus/Run Desktop Setup Proof (Simulated Input)**. It uses the actual controller, shader, lattice and audio with explicitly simulated availability/input. Six finite scans and six chirps must finish; duplicates are rejected. Game view labels simulation and writes `.artifacts/research/desktop-proof.json`. Stop Play mode to return to FocusCore. The diagnostic is excluded from the Android build; it proves neither passthrough nor physical hand tracking.

## Build and test

From the checkout, PowerShell 7:

```powershell
./scripts/Check-Environment.ps1
./scripts/Test-Domain.ps1
./scripts/Test.ps1 -Mode EditMode
./scripts/Test.ps1 -Mode PlayMode
./scripts/Build.ps1 -Scene Core
```

Close the interactive Editor before CLI tests/builds. Fresh October 7 CLI results: **37/37 EditMode and 4/4 PlayMode**, zero failures/skips and exit 0. Tests simulate availability. Domain-only/cached compilation checks are narrower than actual Unity execution.

EditMode platform tests regenerate Baseline and may leave the build list pointing to it. Restore Core using **Focus/Generate First Focus Demo** afterward. Explicit `Build.ps1 -Scene Core` selects Core independently of that list.

The launcher refuses concurrent Editors, occupied private ADB port or insufficient RAM. Builds require 3 GiB available before launch. Only owned processes run below normal priority with limited workers, private ADB 54483 and a checkout-owned short cache under `%USERPROFILE%/.fbc`. After Unity exits, Gradle packages with two workers and a 2048 MiB heap. Sustained memory below 1 GiB stops only the owned build tree. Other applications and global environment variables are untouched.

Outputs use unique APK/export paths. The convenience APK changes only after successful packaging. `-ResumeExport` packages its saved snapshot, never newer source, and refuses an already-packaged export. See [validation](validation.md) for actual artifact outcome. Installation, real input/recovery, comfort and frame timing require Quest 3S, developer access, a data-capable USB cable and authorized USB debugging.

## Automation and assets

Meta Core/Interaction/OVR 207.0.0, OpenXR 1.17.0, XR Hands 1.7.2, Input System 1.20.0, URP 17.3.0 and Coplay Unity MCP 10.0.0 are pinned in the imported manifest/lockfile. No raw camera permission or object recognition is implemented.

Choose **Focus/Start Project MCP Bridge** after opening through Hub. Project-only discovery, port 54484 and Python search path are scoped to this Editor process; an explicitly started bridge resumes across domain reloads. Bundled Python is under `%USERPROFILE%/.cache/codex-runtimes/codex-primary-runtime/dependencies/python`; uv/uvx under `%USERPROFILE%/.local/bin`. `scripts/reference/check_unity_mcp.py` verifies a real scene round trip against this exact project's Assets path. Do not configure every MCP client. A new Codex session may be needed for directly exposed FocusUnity tools; the Python MCP client is usable now.

The actual Unity Console is authoritative. Coplay can misclassify Unity 6 normal Test Runner logs as exceptions/warnings. Inspect Console counts, compiler messages and test exits/results.

Blender 5.2.2 LTS with MCP addon 1.8/protocol 13 is working. `art/source/FocusVisualStudy.blend` contains the original eight-mesh study; Unity imports exported FBX, not a .blend file. Actual scene/material/animation round trips were verified. Optional external asset generators are unnecessary. Meta VR CLI `%USERPROFILE%/.metavr/bin/metavr.exe` reports 1.8.0.17.10; its older detection of Store Hub is not a build blocker.
