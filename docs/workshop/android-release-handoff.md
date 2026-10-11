# MRWorkshop Android release handoff

Recorded October 11, 2026. Checkout: `C:\Users\mrbos\Downloads\Git Hub Local_remote\Zero-DAWN-Dymb-aloy\.worktrees\focus-core`, branch `codex/focus-core`.

The corrected runtime has no packaged APK. Preserve the earlier unique APK as historical evidence; it lacks the subsequent corrections. No physical Quest acceptance is recorded.

## Retained export and interruption

The retained export is:

```text
C:\Users\mrbos\.fbc\a0f3f63c0a54\projects\Workshop-20261010-174907-695f3571
```

The Unity export log `.artifacts/logs/build-Workshop-20261010-174909-950a79d7.log` names this export, reports export success at line 7466, and ends with return code 0. Both launcher and unityLibrary Gradle files exist. The release APK output directory is absent. The associated Gradle stdout ends mid-word, stderr is empty, and the retained `active-build.json` identifies the former Gradle PID 13064 and this export. The coordinated process snapshot found no remaining build process.

[`evidence/build-interrupted.json`](evidence/build-interrupted.json) associates the export with source checkpoint `e9e6651`. This is retained build evidence; the export has no independently recorded source hash manifest. Resuming packages that saved export, not the latest checkout.

The reason for interruption and the final Gradle exit remain unknown. `Build.ps1` stores the live packager identity, but does not persist a terminal result. Its asynchronous stream copies write through FileStreams that are closed in normal cleanup. Abrupt supervisor loss can therefore leave buffered/truncated logs and no final exit record. That is an evidence gap identified in code, not proof of what stopped this particular build.

## Durable entrypoint

[`build-supervisor.ps1`](../../scripts/workshop/build-supervisor.ps1) starts a hidden, separate PowerShell worker and immediately returns its run directory. The worker invokes the existing `Build.ps1` in its script scope. `Build.ps1` remains the sole owner of admission, Editor/Blender refusal, memory checks, bounded waits, owned child cleanup, resumable exports and atomic APK publication. The wrapper adds no retry, second scheduling system, duplicate build gate or process termination logic.

Each launch retains unique `request.json`, `status.json`, `stdout.log` and `stderr.log` under `.artifacts/workshop/build-supervisor/<run-id>/`. The request records forwarded arguments and the build script hash. Status records the worker PID, creation ticks and executable, requested/started/finished times, the returned build exit code or caught exception, and the worker's intended exit code. Normal completion and caught failures are written atomically. Status readers permit Windows file replacement while inspecting.

Only run this after Shane chooses to end the open Editor session, other authoring apps are no longer occupying the build admission checks, and available memory satisfies the existing build minimum:

```powershell
$run = & ./scripts/workshop/build-supervisor.ps1 -Workers 1 -ResumeExport 'C:\Users\mrbos\.fbc\a0f3f63c0a54\projects\Workshop-20261010-174907-695f3571'
$run
& ./scripts/workshop/build-supervisor.ps1 -InspectRun $run.RunDirectory
```

Save `RunDirectory` if the terminal/session changes. Inspection is read-only. `Status=finished` requires checking `BuildExitCode`: zero means Build returned success; a nonzero value preserves that failure exactly. `Status=failed` records a thrown rejection/failure and leaves `BuildExitCode` null when none was observed. `ObservedStatus=unknown` means a nonterminal record lacks a matching live worker; it does not infer a crash, low RAM, timeout or user stop. `SupervisorExitCode` is the worker's intended exit recorded before it exits, not an independent OS exit observation. If the worker is forcibly terminated or Windows stops, finalization is not guaranteed. This entrypoint improves retained evidence; it does not make a build immune to external termination.

`SourceCommitAtLaunch` describes the current checkout and explicitly does not establish the provenance of a resumed export. A successful build still needs `Verify-Apk.ps1` against its new unique APK, with size/hash and source association recorded before release publication. APK validation does not establish physical input, passthrough, comfort or frame timing.

## Focused verification and next step

`./scripts/workshop/build-supervisor.test.ps1` passed **10 focused assertions** using disposable, hidden PowerShell fixture builds: exact zero/nonzero exit persistence, exception persistence without an invented build exit, redirected stdout, argument forwarding, worker identity/terminal timestamp, unknown disappeared-worker handling and read-only inspection. The first run failed because the supervisor was absent. The next run exposed a Windows status-read/file-replacement sharing race; changing only the supervisor reader fixed that diagnosed failure. The final focused run passed, and both new scripts parsed with zero PowerShell syntax errors. No existing helper suite was rerun and no Unity, Blender, Gradle or ADB action was performed.

The synthetic fixture retained at `C:\Users\mrbos\AppData\Local\Temp\workshop-supervisor-b02a0e818c7a4dba90cfccc05379f35e` contains raw per-run status/log proof. The earlier failed fixture `workshop-supervisor-ad29711c61484a98b95c674af743adae` was retained as diagnostic evidence. Synthetic success does not establish Android packaging or resilience to a forced OS/process-tree termination.

The remaining packaging blocker is the current authoring session and memory state: the Workshop Editor remains open, two Blender processes belong to the independent AI Procedural Generator work and must be preserved, and the coordinator observed approximately 0.93 GiB available RAM. Leave those applications open. The next Android action, once the existing admission conditions are satisfied, is one supervised resume with one worker, followed by validation of its newly produced unique APK. If that resume fails, use its durable status and native logs to diagnose the observed failure before another attempt.
