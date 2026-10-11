# Focus audiovisual authoring workflow

Updated October11,2026. This workflow belongs to the existing **FocusCore** project. MRWorkshop is a separate Unity project. This increment adds reproducible staging tools; it changes no runtime asset, shader, scene, import setting, APK or retained Android export.

## Generate a review copy

From the `focus-core` checkout, choose a **new** staging directory and run:

```powershell
& 'C:/Users/mrbos/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B scripts/workshop/audio-workflow.py --stage .artifacts/research/audio-workflow-review-01
```

On another host, use its Python3.12+ executable. Only the Python standard library is required. The command takes a few seconds and creates `FocusChirp.wav` and `audio-report.json`. It refuses an existing directory. Inside this checkout it accepts only `.artifacts` destinations; it resolves paths before checking. It does not start playback, Unity or Blender, install dependencies, replace source audio, or write the earlier quality evidence.

The JSON records the Python version, generator hash, source/staged WAV hashes, source preservation, direct PCM measurements and every threshold result. A differing source/staged hash is recorded as a discrepancy with the source retained; it does not authorize replacement. Exit0 means sample checks passed and the source stayed unchanged. Read `byte_identical_to_source` separately; matching the source is independent of perceptual fidelity.

The lower-level generator now supports `--output <wav-path>` and import-safe `build_audio(output)`. Its parent directory must already exist. Omitting `--output` retains the historical behavior of writing the **working Unity asset**. Use the staging command above during review. The old `scripts/reference/check_audio.py` writes earlier evidence; it is intentionally not part of this staging workflow.

## Authoring and integration map

| Part | Editable source and route | Current behavior |
| --- | --- | --- |
| Acknowledgement | `scripts/reference/build_audio.py` → staged WAV → review → reviewed asset integration | Seed7303; filtered noise, rising resonance, glass partials, low body and delayed tail;48kHz mono PCM16,0.94s |
| Runtime audio | `FocusCore/Assets/FocusCore/Art/FocusChirp.wav` → `Editor/FocusDemoSetup.cs` assignment → `Runtime/FocusController.cs` | One accepted scan plays the clip at `PlayOneShot(...,0.22f)`; unavailable tracking/pause stops playback |
| Scan shell | `Runtime/FocusVisuals.cs` → generated mesh → `Art/FocusPulse.mat` / `Art/FocusPulse.shader` |528 facets/1,584 vertices; barycentric edges, glints, tint and travelling latitude sweep; captured world origin, eased expansion up to3.8m over the finite1.25s scan |
| Reticle/device geometry | Original editable studies under `art/source` → reviewed FBX export → `FocusCore/Assets/FocusCore/Art` → Unity import | Original geometry is separate from the procedural shell. Blender/export work belongs to the asset owner |

Paths in the last three rows are relative to `FocusCore/Assets/FocusCore` unless fully qualified. No automatic promotion is supplied. When a later change is authorized, retain the working WAV and its `.meta` GUID, review the staged candidate by listening, then import only the chosen asset into FocusCore. Shader/geometry edits require a separate staged visual review. Rebuild from current source for any runtime change: packaging a retained export packages that snapshot.

## Actual bounded verification

Run the tool's isolated subprocess checks with:

```powershell
& 'C:/Users/mrbos/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B scripts/workshop/audio-workflow.test.py
```

October11 run: two tests passed. They exercise import without output, explicit output without touching the default, historical default generation inside a disposable copied checkout, fresh staging/report generation, refusal to overwrite a stage, and preservation of the real source WAV. No Unity suite was run for these helper-only changes.

The actual new review output is `.artifacts/research/audio-workflow-20261011-authoring/`. Its WAV is **byte-identical** to the working WAV: SHA256 `d204ed66ce325adf1f151acb656c876a3693935f03965653cbef12f4e7365551`. Source hashes before/after match. Measurements:45,120 frames,0.94s, peak0.4399719, RMS0.0628066, DC0.000001892, final20ms RMS0, maximum adjacent-sample step0.1453552. All six existing duration/headroom/DC/boundary/tail/continuity thresholds pass. These are PCM-file checks, not a fresh Unity import or listening result. Cross-platform floating-point/library differences should be checked by hash rather than assumed identical.

## What remains

The current synthesis parameters and scan timings are our authored choices. Earlier [reference observations](../reference/audio-scan-study.json) do not supply Guerrilla's Focus production recipe. No extracted reference recording drives this generator, and this task performed no additional fidelity search or playback.

Listening still needs to establish perceived onset, harshness, tail and loudness at the app's gain. Quest3S acceptance still needs actual audiovisual synchronization, stereo appearance, passthrough contrast, hand/controller input and tracking recovery, comfort, and measured frame timing. Numerical headroom and facet count do not establish those results. Consult the existing [Focus validation](../validation.md) and [Focus development/deployment route](../development.md) when hardware work resumes. The adjacent [device checklist](device-acceptance.md) and Workshop device scripts target MRWorkshop; they do not install or validate FocusCore.
