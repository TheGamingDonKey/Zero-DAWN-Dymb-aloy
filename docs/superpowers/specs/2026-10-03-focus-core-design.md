# Focus Core first headset build

This specification defines the first usable Focus application for Shane's Meta Quest 3S. The purpose is to establish a repeatable development loop and demonstrate a hand-operated mixed-reality interaction on the actual headset. It is a design for review, not evidence that an application has been built or tested.

## First deliverable

A standalone Android APK launches on Quest 3S, displays the physical room through passthrough, and presents a large Scan button that works through hand-ray pinch selection or direct finger poking. Selecting Scan plays one procedural purple scan animation and one generated sound. Controllers operate the same button as a fallback. The app recovers cleanly from hand tracking loss and from suspension.

Success requires an installed application demonstrated on Shane's headset. A desktop scene, successful compilation, simulated interaction, or screenshot does not establish on-device success.

## Verified starting state

On October 3, 2026, the GitHub connection authenticated as TheGamingDonKey and could read the public repository TheGamingDonKey/Zero-DAWN-Dymb-aloy. The repository contained only README.md at initial commit 95cbf20. The repository was cloned to C:\Users\mrbos\Downloads\Git Hub Local_remote\Zero-DAWN-Dymb-aloy. No Unity scene, package manifest, product source, or APK existed in that checkout.

Unity Hub, Unity Editor, Meta VR CLI, and Android platform tools were not found on PATH or in the checked common installation locations. The Windows device inventory did not show a Quest, Android, ADB, or MTP device. This inventory does not establish the capabilities of an unconnected headset. Approximately 298 GB was free on C:.

Shane has a charging cable available and can connect the headset to the laptop or desktop. Its data capability, developer-mode state, and USB debugging authorization remain unverified and must be established before installation on the device. The first development host is this Windows laptop, where the checkout and tooling will live. Another computer is an alternative only if a concrete host limitation appears.

## Scope

Included in this first build:

- Color passthrough and tracked stereoscopic rendering.
- A simple, readable panel placed in front of the user at launch.
- One large Scan control and a short state label.
- Hand-ray pinch and direct poke input supplied by Meta Interaction SDK.
- Controller input for the same control as a fallback.
- A code-generated scan effect and code-generated audio.
- Tracking-loss, pause, and resume handling.
- Reproducible build and deployment instructions, with logs and on-device checks.

Subsequent increments are manual spatial markers, room-surface scan effects, and persistent spatial anchors. These do not block the first build. Object recognition, raw RGB camera processing, AI services, voice commands, networking, multiplayer, detailed 3D assets, and a background overlay across other applications are outside this first build.

## Development approach

Use Unity 6.3 LTS with Universal Render Pipeline, Android Build Support, Android SDK and NDK tools, and OpenJDK. Use Unity OpenXR rather than the deprecated Oculus XR provider. Add a mutually compatible set of Meta XR Core and Interaction SDK packages; resolve their declared dependencies and record the selected versions in the Unity project files. Do not upgrade packages during implementation unless a documented compatibility problem requires it.

Meta VR CLI handles development-tool inspection and device operations. Unity builds the APK; the Meta CLI does not replace the compiler. The first APK runs standalone on Quest, so Meta Horizon Link and PC VR streaming are not prerequisites. Android Studio is not required for the initial Unity workflow because Unity's Android modules supply the build toolchain.

Use documented Meta camera-rig, passthrough, and Interaction SDK components as the platform foundation. First Hand is a reference for interaction behavior, not a project to copy wholesale: its README identifies an older bundled SDK and several unrelated game systems. Build the minimal scene with compatible current packages.

Meta XR Operator, XR Simulator, editor MCP integration, and additional agent skills are optional development aids. The production app does not depend on them. A missing or experimental automation aid must not prevent ordinary Unity builds and physical headset testing.

## Runtime structure

Keep application behavior in focused units:

| Unit | Responsibility | Dependency |
| --- | --- | --- |
| Platform rig | Head tracking, passthrough, hands, and controller poses | OpenXR and Meta components |
| Scan control adapter | Convert supported UI selection into a single scan request | Interaction SDK button events |
| Focus state | Own readiness, scanning, and pause transitions | Small application state model |
| Scan visual | Render and retire the procedural pulse | Focus state and Unity graphics |
| Scan audio | Play a short generated audio clip once per accepted request | Focus state and Unity audio |
| Status view | Explain readiness, scan progress, and input availability | Focus state and platform input status |

The input adapter must not own animation timing. Graphics and audio must not infer gestures independently. This separation lets later marker or room-data features consume the same scan action without rewriting hand input.

## Interaction and appearance

At launch, put a world-space panel approximately 0.7 metres in front of the tracked head, facing the user and slightly below eye level. This gives an initial position that can support nearby poke interaction as well as a hand ray. Keep its interaction controls large and its text brief. This is an initial layout choice that will be adjusted from headset observations. Keep the middle of the real-world view readable rather than covering it with an opaque dashboard.

Use the SDK's supported hand ray and index pinch for distant selection, and direct poke for nearby interaction. Activate scanning through selection of the explicit button; an arbitrary pinch anywhere does not trigger a scan. Avoid temple touching and gestures reserved by the Quest system. Both hands should be able to operate the control. The controller fallback invokes the same action.

The scan is a deliberately visual effect. An accepted scan request captures an origin in the tracked world and expands a translucent purple pulse for approximately 1.25 seconds, then fades and removes the effect. Head movement during the animation does not continually move its origin. The effect must not imply that physical objects were identified or that surfaces were measured.

Generate the geometry, material animation, and short audio clip in the project. No sculpted assets or external AI generation service are necessary. Keep the audio conservative and respect the headset's volume control.

## State and failure behavior

An accepted request begins exactly one scan and one sound. Requests while that scan is active are ignored rather than spawning overlapping effects. The control becomes available again after the effect finishes.

Hand tracking loss cancels an unfinished input gesture. It must not trigger a scan, preserve a stuck selection, or activate automatically when tracking returns. A scan already accepted can finish when only hand tracking is lost. If controllers are available, their input remains usable. Otherwise the view explains that the user should bring their hands into view.

On application suspension or loss of valid head tracking, stop audio and hide transient scan effects. On resume and restoration of tracking, reset the app to ready and place the panel in front of the current head pose. Do not resume an old gesture or replay the previous scan sound.

If passthrough or the XR session cannot initialize, record the actual cause and stop normal scan interaction. Do not present a desktop or fabricated room as proof that real passthrough works.

## Development checkpoints

Shane authorized preparation while the headset is disconnected on October 4, 2026. The host and source work can advance independently of hardware acceptance:

1. Establish the host tooling and verify the chosen Unity editor opens with Android modules present and a usable license. Downloads and source preparation can proceed if sign-in or an installer prompt needs Shane; compilation remains pending until the editor can run.
2. Prepare a minimal passthrough baseline scene and build its APK. Retain this scene so device integration can begin with the smallest application.
3. Prepare the hand-operated control, controller fallback, Focus state, procedural pulse, and generated audio. Run meaningful host tests and attempt a separate Focus APK. Without a headset these are source, host-test, and build results only.
4. When Shane returns, establish a data connection, retrieve device and OS information, and verify USB debugging authorization. Developer mode and account prompts require Shane's participation.
5. Install the minimal baseline first and confirm passthrough and head tracking while wearing the headset. Then validate the Focus control and recovery behavior, followed by the pulse and audio. Fix the first failing platform layer before adding later features.
6. Repeat build, install, and launch after a small visible change to prove that iteration is repeatable.

Device deployment depends on a verified device connection. Product behavior is implemented only after this specification and the implementation plan are reviewed. The expected twelve-hour absence is not a completion deadline or a promise that account prompts can be passed unattended.

Use one implementer and the critic Shane requested at meaningful checkpoints. For a repeated setup/build error, retain the log, diagnose the cause, and make at most two targeted corrective attempts. If the same failure persists or roughly twenty minutes of investigation produces no new evidence, stop that branch and continue independent useful work. Download/install progress does not count as stalled investigation. Optional simulator or agent integration cannot block the core project.

## Verification and completion criteria

- Unity opens the project without unresolved compilation or dependency errors.
- The Android build produces an actual APK and a retained build log.
- The APK installs and launches on Shane's Quest 3S, with no launch crash in the inspected device logs.
- Passthrough and tracked rendering are verified while wearing the headset.
- Ten deliberate scan selections, each made after the previous scan finishes, produce ten scan effects and sounds, with no duplicates from a held pinch or repeated input during scanning.
- Both pinch-ray and poke operation are checked on the headset; controller fallback is checked separately.
- Moving hands out of tracking and bringing them back causes no unwanted scan or stuck control.
- Suspension and resume recover a usable panel and ready state.
- The initial performance target is a steady 72 Hz with the app rendering at 72 FPS during the core interaction. Measure this on device; lower frame rates or capture overhead are reported rather than hidden.
- A second build with a visible change installs successfully and confirms that the edit/build/deploy loop works.

Automated checks should cover meaningful application state behavior, especially duplicate requests, tracking loss, and pause/resume. They do not replace headset checks for readability, comfort, hand recognition, or performance. Report separately what compiled, what was simulated, what ran on the device, and what Shane confirmed while wearing it.

## Current sources

- [Meta Unity setup](https://developers.meta.com/vr/documentation/unity/unity-project-setup/)
- [Meta Interaction SDK](https://developers.meta.com/vr/documentation/unity/unity-isdk-interaction-sdk-overview/)
- [Hand tracking limitations](https://developers.meta.com/vr/design/hands-limitations-mitigations/)
- [Meta VR CLI installation](https://developers.meta.com/vr/essentials/metavr-install/)
- [Meta environment tools](https://developers.meta.com/vr/essentials/metavr-environment/)
- [Device setup](https://developers.meta.com/vr/documentation/native/android/mobile-device-setup/)
- [Device deployment commands](https://developers.meta.com/vr/essentials/metavr-devices-and-apps/)
- [First Hand reference project](https://github.com/oculus-samples/Unity-FirstHand)
- [Meta agentic tools](https://github.com/meta-quest/agentic-tools)
