# Zero-DAWN-Dymb-aloy
XR development project and program, where we foolishly try to replicate the program running Aloys focus.

The first increment targets a standalone Quest 3S mixed-reality app with hand-operated Scan input, controller fallback, a procedural purple pulse, and generated audio.

Current state: Unity 6000.3.25f1 is activated, the Android project imports and compiles, and 31 Unity EditMode tests pass. The generated passthrough baseline includes the official Meta hand/controller interaction rig. The first signed ARM64 baseline APK builds successfully; installation and headset behaviour remain unverified. The Focus Scan control, procedural pulse and audio are the next implementation stage.

Open the `FocusCore` folder as the Unity project. See [development setup](docs/development.md), [verified progress](docs/validation.md), and the [implementation plan](docs/superpowers/plans/2026-10-04-focus-core.md).
