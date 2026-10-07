# Zero-DAWN-Dymb-aloy
XR development project and program, where we foolishly try to replicate the program running Aloys focus.

The first increment targets a standalone Quest 3S mixed-reality app with hand-operated Scan input, controller fallback, a procedural purple pulse, and generated audio.

Current state (October7): the mixed-reality scene is generated and open in Unity6000.3.25f1, with the original FBX reticle, Scan card and eight SDK hand/controller input sources. Actual Unity Test Runner results are37/37 EditMode and4/4 PlayMode, zero failures/skips. Physical XR remains unverified.

Unity Hub confirms an active Personal licence. Registering the installed6.3 Editor and this project, then opening through Hub, succeeded. Earlier direct CLI execution exited198; it did not establish that the user lacked a licence. A fresh CLI launch now exits 0, and fresh CLI EditMode/PlayMode runs pass all 37/4 tests. A fresh signed Core APK now contains the Scan increment; manifest, ARM64/IL2CPP and hash checks passed. No headset result exists.

The [15-page visual reference](docs/reference/Aloy%20Focus%20Reference.pdf) covers silhouettes, estimated colours, UI patterns, motion, a capability matrix, credited game references and a real original-asset render. Editable models are in `art/source`; Unity consumes FBX in `Assets/FocusCore/Art`. See [current validation and continuation](docs/validation.md) before handing this branch to another Codex.

Open the `FocusCore` folder as the Unity project. See [development setup](docs/development.md), [verified progress](docs/validation.md), and the [implementation plan](docs/superpowers/plans/2026-10-04-focus-core.md).

Open `Assets/FocusCore/Scenes/FocusCore.unity`, or generate it with `Focus/Generate First Focus Demo`. The generator builds the world-space card, original reticle, SDK hand/controller ray/poke controls, fixed-origin lattice and chirp. It does not identify real objects. Core Android export/APK succeeded; `scripts/Build.ps1 -Scene Core` requires the interactive Editor closed, at least3GiB available RAM and working CLI licensing. Headset installation, hand/controller recovery, comfort and frame timing remain acceptance requirements.

Unity MCP is pinned to Coplay10.0.0 in the manifest and generated lock. `Focus/Start Project MCP Bridge` connects port54484 with project-scoped discovery and resumes across domain reloads after an explicit start in this Editor session. Actual scene, console, menu and test round trips succeeded through a Python MCP client. A fresh Codex session may be needed to expose the registered `FocusUnity` tools directly. Earlier BlenderMCP1.8/protocol13 geometry/material/animation/FBX evidence is retained.
