# Focus development setup

Use Unity 6000.3.25f1. The prepared project is the repository's `FocusCore` folder.

On Shane's laptop the implementation worktree is:

`C:\Users\mrbos\Downloads\Git Hub Local_remote\Zero-DAWN-Dymb-aloy\.worktrees\focus-core`

Installed Editor:

`C:\Users\mrbos\Documents\UnityEditors\6000.3.25f1\Editor\Unity.exe`

Unity Hub3.22.2 is installed as a Windows package. Editor import, compilation and tests succeeded on October6, but the October7 import exited198 before import because no valid entitlement was available. Check Hub Settings → Licenses and restore normal Personal activation/sign-in. If Hub does not list this Editor, use Locate/Add and select the executable above. Add/open the `FocusCore` project. Do not repeat builds until licensing is restored.

The install initially landed in Codex's virtualized LocalAppData. It was moved to Documents, its executable hash matched, and Unity's CLI verified the relocated Editor and Android modules. No Windows ACL or system security policy was changed.

Installed Android components include OpenJDK 17.0.18+8, NDK r27c, CMake 3.22.1, platform/build tools 36.0.0, and command-line tools 16.0. Use these Unity-managed tools for builds. The system's separate Java installations are not the build toolchain.

The imported manifest and Unity-generated lockfile pin Meta Core/Interaction/OVR integration 207.0.0, OpenXR 1.17.0, Input System 1.20.0, XR Hands 1.7.2, URP 17.3.0, Test Framework 1.6.0, and Unity NUnit package 2.0.5. URP settings came from this Editor's bundled blank URP template. Additional built-in modules required by SDK source are explicit in the manifest.

From the implementation worktree, PowerShell 7 can run:

```powershell
./scripts/Check-Environment.ps1
./scripts/Test-Domain.ps1
./scripts/Check-Source.ps1
./scripts/Test.ps1 -Mode EditMode
./scripts/Build.ps1 -Scene Baseline
```

The domain runner compiles the real `Runtime/Domain` files and the same NUnit test sources used by Unity. It uses the staged Unity NUnit DLL on this host, or finds it in the project's package cache after import. On another host, supply `-NUnitPath` pointing to the official `com.unity.ext.nunit` package's `net40/unity-custom/nunit.framework.dll`. Compilation errors, zero tests, failures, ignored tests, and inconclusive tests return nonzero. Each run receives a unique XML/DLL directory under ignored `.artifacts/tests/`.

The domain runner tests host logic only. `Test.ps1` instead runs Unity's real Test Runner with Android selected and checks fresh XML and the Editor exit status. The 31 EditMode cases cover domain behaviour and saved platform configuration/rig generation; they do not prove passthrough or input on hardware.

The launcher refuses another active Unity Editor, an occupied private ADB port, or less than 1.5 GiB available RAM. Android builds require 3 GiB available before starting. Each owned Editor runs below normal priority with limited workers, two Bee build threads, an owned Gradle cache under %USERPROFILE%/.fbc and a private inherited ADB endpoint at port54483. Build.ps1 exports the Android Gradle project, waits for Unity to exit, then packages separately with two Gradle/Bee workers, a 2048 MiB heap and no persistent daemon. This avoids Unity overriding the heap to 4096 MiB. A checkout ownership marker protects the short cache path, which also avoids the Windows Ninja path-length failure. Interrupted packaging can resume with -ResumeExport pointing to its owned export; an export containing an APK is refused. These bounds reduce contention; initial import/shader compilation still consumes CPU, RAM and disk. No script closes other applications or changes global environment variables. Native ADB behaviour must be verified during the actual build/device step.

`Focus/Configure Android XR` requires Android selected. `Focus/Generate Passthrough Baseline` creates the saved scene and prefab using SDK quick actions and the official passthrough-underlay prefab. Configuration selects ARM64/IL2CPP, Vulkan, minimumAPI32, targetAPI34, OpenXR, Quest support, hand tracking and controller fallback. Raw camera access is disabled. No blanket SDK machine setup fixes run.

Builds receive unique output paths under `.artifacts/apks/`; a convenience APK is replaced only after the invocation exits successfully and produces a nonempty APK. `BuildCore` now generates the first-demo scene if missing, using `FocusDemoSetup`. That generation has not yet executed because licensing blocks the Editor. Fresh export directories can repeat native compilation; a fast incremental iteration workflow has not been proven. `-ResumeExport` packages its saved export snapshot, not newer C# or scene edits.

Meta VR CLI is installed at `C:\Users\mrbos\.metavr\bin\metavr.exe`, version 1.8.0.17.10. Its older Hub detection does not recognize the new Windows package; this does not block device operations. Hub's bundled standalone Unity CLI was used to install the Editor, without another engine or Android Studio.

The baseline APK is built and signature/manifest checked. The new Scan control, procedural graphics/audio, source adapter and scene generator are authored and pass cached API compilation; runtime checks and a new Core APK remain pending. When the headset is connected, verify developer mode, a data-capable cable and USB debugging before installation. The generated SDK rig includes additional SDK interaction infrastructure; only ray/poke input is intended for the first Focus control.

`Check-Source.ps1` uses the Editor's bundled Roslyn, .NET Standard reference/shim assemblies and previously imported SDK DLLs. It records source hashes and compiler exits. It does not launch Unity or validate shader compilation, FBX import, serialized scenes or headset operation. `Tests/PlayMode` contains four pending presentation/recovery checks; static compilation does not execute them.

Blender5.2.2 LTS is installed from the Store, with Blender MCP1.8/protocol13 verified. `art/source/FocusVisualStudy.blend` is the original asset source; FBX exports are in Unity Assets. No `.blend` file is placed in Assets, so Unity does not need to launch the Store Blender executable. Materials and animation were exercised inside real Blender and exports were reimported.

Coplay Unity MCP10.0.0 is pinned in the manifest but is not yet imported into the project lockfile. The installed Python distribution is `mcpforunityserver==10.0.0`; its protocol banner reports the underlying FastMCP framework version3.4.8. The actual handshake and46-tool catalogue succeeded, with zero connected Editor instances. `Setup-UnityMcp.ps1` registers only FocusUnity; other clients are not auto-configured. The local CLI's new entry may require a fresh Codex session to load. `Start-UnityMcpEditor.ps1` launches this checkout with its isolated status directory and bridge port54484; do this after licensing recovery. The project Editor guard suppresses the package's automatic sweep of unrelated client configs.
