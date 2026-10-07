# Zero-DAWN-Dymb-aloy
XR development project and program, where we foolishly try to replicate the program running Aloys focus.

The first increment targets a standalone Quest 3S mixed-reality app with hand-operated Scan input, controller fallback, a procedural purple pulse, and generated audio.

Current state (October 7): research, original Blender assets, Scan admission and first-demo Unity source are prepared. 32 host NUnit tests pass, and Domain/Runtime/Editor/PlayMode-test source compiles against cached Unity 6000.3.25f1 and Meta 207 assemblies. These checks do not execute Unity, shaders or XR. The new scene must still be generated and tested in the Editor.

**Current blocker:** Unity exited198 before import because no valid Editor entitlement was available. Restore the normal Unity Hub Personal licence before trying another import/build. Do not repeat builds against the same missing licence. The earlier October 6 baseline APK and31 Unity EditMode passes remain historical evidence; that APK does not contain this increment and has not been installed or tested on the headset.

The [15-page visual reference](docs/reference/Aloy%20Focus%20Reference.pdf) covers silhouettes, estimated colours, UI patterns, motion, a capability matrix, credited game references and a real original-asset render. Editable models are in `art/source`; Unity consumes FBX in `Assets/FocusCore/Art`. See [current validation and continuation](docs/validation.md) before handing this branch to another Codex.

Open the `FocusCore` folder as the Unity project. See [development setup](docs/development.md), [verified progress](docs/validation.md), and the [implementation plan](docs/superpowers/plans/2026-10-04-focus-core.md).

After licence recovery: run `scripts/Test.ps1 -Mode EditMode`; generate the scene with `Focus/Generate First Focus Demo`; run PlayMode tests and inspect the URP scene; then run `scripts/Build.ps1 -Scene Core` with the interactive Editor closed. The generator builds the world-space card, original reticle, SDK hand/controller ray/poke controls, fixed-origin lattice and chirp. It does not identify real objects. Actual headset installation, hand/controller recovery, comfort and frame timing remain acceptance requirements.

Unity MCP is pinned to Coplay10.0.0 in the project manifest. `scripts/Setup-UnityMcp.ps1` registers only the `FocusUnity` Codex server; `scripts/Start-UnityMcpEditor.ps1` opens this project with isolated status files and requests its localhost bridge. The Python server handshake/catalogue is verified; the Editor scene/console round trip is blocked by licensing. A new Codex session may be needed to load the new server. Blender MCP1.8/protocol13 works in Blender5.2.2; actual geometry/material/animation/FBX round-trip evidence is retained.
