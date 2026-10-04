# Focus development setup

Use Unity 6000.3.25f1. The prepared project is the repository's `FocusCore` folder.

On Shane's laptop the implementation worktree is:

`C:\Users\mrbos\Downloads\Git Hub Local_remote\Zero-DAWN-Dymb-aloy\.worktrees\focus-core`

Installed Editor:

`C:\Users\mrbos\Documents\UnityEditors\6000.3.25f1\Editor\Unity.exe`

Unity Hub 3.22.2 is installed as a Windows package. Sign in to Unity Hub and activate an eligible license. If Hub does not list this Editor, use Locate/Add and select the executable above. Then add/open the `FocusCore` project. The Editor's first batch startup exited 198 with "No valid Unity Editor license found." Account activation requires Shane.

The install initially landed in Codex's virtualized LocalAppData. It was moved to Documents, its executable hash matched, and Unity's CLI verified the relocated Editor and Android modules. No Windows ACL or system security policy was changed.

Installed Android components include OpenJDK 17.0.18+8, NDK r27c, CMake 3.22.1, platform/build tools 36.0.0, and command-line tools 16.0. Use these Unity-managed tools for builds. The system's separate Java installations are not the build toolchain.

The candidate project manifest specifies Meta Core/Interaction/OVR integration 207.0.0, OpenXR 1.17.0, Input System 1.12.0, XR Hands 1.7.2, URP 17.0.1, Test Framework 1.4.6, and Unity NUnit package 2.0.5. URP settings came from this Editor's bundled blank URP template. This dependency set has not completed a Unity import; no package lockfile or compatibility claim is fabricated. Resolve any actual import issue before generating scenes or building.

From the implementation worktree, PowerShell 7 can run:

```powershell
./scripts/Check-Environment.ps1
./scripts/Test-Domain.ps1
```

The domain runner compiles the real `Runtime/Domain` files and the same NUnit test sources used by Unity. It uses the staged Unity NUnit DLL on this host, or finds it in the project's package cache after import. On another host, supply `-NUnitPath` pointing to the official `com.unity.ext.nunit` package's `net40/unity-custom/nunit.framework.dll`. Compilation errors, zero tests, failures, ignored tests, and inconclusive tests return nonzero. Each run receives a unique XML/DLL directory under ignored `.artifacts/tests/`.

These are host logic tests. They do not run Unity rendering, SDK hand input, Android/IL2CPP, passthrough, or headset performance. After activation/import, run the EditMode tests in Unity's Test Runner too.

Meta VR CLI is installed at `C:\Users\mrbos\.metavr\bin\metavr.exe`, version 1.8.0.17.10. Its older Hub detection does not recognize the new Windows package; this does not block device operations. Hub's bundled standalone Unity CLI was used to install the Editor, without another engine or Android Studio.

Next implementation steps are the plan's minimal platform prefab/baseline scene and Android/OpenXR configuration, then the SDK Scan control, procedural graphics/audio, and logged APK builds. The build scripts and runtime presentation are not implemented yet. When the headset is connected, verify developer mode, a data-capable cable and USB debugging before installation; validate the baseline before Focus controls and effects.
