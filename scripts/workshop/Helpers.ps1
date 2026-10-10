# Shared admission, owned-process lifecycle and atomic artifact publication.
# A retained gate file is harmless: its OPEN write handle is the lock.
#Requires -Version 7.0
function Assert-WorkshopNoOrphanPackager([string]$Root) {
    $marker = Join-Path $Root '.artifacts/workshop/active-build.json'
    if (-not (Test-Path -LiteralPath $marker)) { return }
    try { $record = Get-Content -LiteralPath $marker -Raw | ConvertFrom-Json } catch {
        throw 'Workshop packager ownership record is unreadable; inspect it before launching. No process was stopped.'
    }
    $running = Get-Process -Id $record.Pid -ErrorAction SilentlyContinue
    if ($running -and $running.StartTime.ToUniversalTime().Ticks -eq $record.StartTicks -and
        [string]::Equals($running.Path,$record.Executable,[StringComparison]::OrdinalIgnoreCase)) {
        throw 'A Workshop Android packager is still running, possibly after its wrapper ended. No process was stopped.'
    }
}

function Resolve-WorkshopBlenderLaunch {
    param([Parameter(Mandatory)][string]$Root,[string]$ExpectedInvocationId)
    $marker = Join-Path $Root '.artifacts/workshop/blender-launch.json'
    if (-not (Test-Path -LiteralPath $marker)) { return $true }
    try { $record = Get-Content -LiteralPath $marker -Raw | ConvertFrom-Json }
    catch { throw "Workshop has an unresolved Blender activation with an unreadable ownership record: $marker" }
    if ($ExpectedInvocationId -and $record.InvocationId -ne $ExpectedInvocationId) {
        throw 'Workshop has an unresolved Blender activation belonging to a different invocation; its record was preserved.'
    }
    if ($record.InvocationId -notmatch '^[a-fA-F0-9]{32}$') { throw 'Workshop has an unresolved Blender activation with invalid invocation identity.' }
    $expectedScript = Join-Path ([IO.Path]::GetFullPath($Root)) ('.artifacts/workshop/blender-start-' + $record.InvocationId + '.py')
    if (-not [string]::Equals($record.Script,$expectedScript,[StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals($record.Report,[IO.Path]::ChangeExtension($expectedScript,'.json'),[StringComparison]::OrdinalIgnoreCase)) {
        throw 'Workshop has an unresolved Blender activation with mismatched scoped paths; its record was preserved.'
    }
    $resolved = $false
    if (Test-Path -LiteralPath $record.Report) {
        try {
            $outcome = Get-Content -LiteralPath $record.Report -Raw | ConvertFrom-Json
            $resolved = $outcome.invocation_id -eq $record.InvocationId -and
                [string]::Equals($outcome.script,$record.Script,[StringComparison]::OrdinalIgnoreCase) -and
                $outcome.startup_outcome -in @('started','failed')
        } catch { } # A partial startup report is still unresolved.
    }
    if (-not $resolved) {
        $scriptArgument = [regex]::Escape($record.Script)
        $pattern = '(?i)(?:^|\s)--python\s+(?:"' + $scriptArgument + '"|' + $scriptArgument + '(?=\s|$))'
        $resolved = @(Get-CimInstance Win32_Process -Filter "Name = 'blender.exe'" | Where-Object {
            $_.Name -eq 'blender.exe' -and $_.CommandLine -and $_.CommandLine -match $pattern
        }).Count -gt 0
    }
    if ($resolved) {
        # Callers hold admission. Remove only the same request whose outcome was observed.
        $current = Get-Content -LiteralPath $marker -Raw | ConvertFrom-Json
        if ($current.InvocationId -ne $record.InvocationId -or $current.Script -ne $record.Script) {
            throw 'Unresolved Blender activation changed during resolution; its record was preserved.'
        }
        Remove-Item -LiteralPath $marker
    }
    return $resolved
}

function Enter-WorkshopGate {
    param([Parameter(Mandatory)][string]$Root,
          [Parameter(Mandatory)][ValidateSet('Build','Editor','Blender','CLI')][string]$Purpose)
    $canonicalRoot = [IO.Path]::GetFullPath($Root)
    $path = Join-Path $canonicalRoot '.artifacts/workshop/operation-gate.json'
    New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force | Out-Null
    try {
        # Sharing permits status reads, but excludes all other write/admission handles.
        $stream = [IO.FileStream]::new($path,[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::Read)
    } catch [IO.IOException] {
        throw 'Cannot launch: another Workshop operation holds the build/launch gate. No process was stopped.'
    }
    try {
        Assert-WorkshopNoOrphanPackager $canonicalRoot
        if (-not (Resolve-WorkshopBlenderLaunch -Root $canonicalRoot)) {
            throw 'Workshop has an unresolved Blender activation. Its Store request may still launch; inspect its scoped startup record before another build/launch. No process was stopped.'
        }
        $owner = Get-Process -Id $PID
        $lease = [pscustomobject]@{Root=$canonicalRoot; Purpose=$Purpose; Id=[Guid]::NewGuid().ToString('N'); OwnerPid=$PID;
            OwnerStartTicks=$owner.StartTime.ToUniversalTime().Ticks; Stream=$stream}
        $json = @{Root=$lease.Root; Purpose=$lease.Purpose; Id=$lease.Id; OwnerPid=$lease.OwnerPid; OwnerStartTicks=$lease.OwnerStartTicks} | ConvertTo-Json -Compress
        $bytes = [Text.UTF8Encoding]::new($false).GetBytes($json)
        $stream.SetLength(0); $stream.Write($bytes,0,$bytes.Length); $stream.Flush($true)
        return $lease
    } catch { $stream.Dispose(); throw }
}

function Exit-WorkshopGate($Lease) {
    if ($Lease -and $Lease.Stream) { $Lease.Stream.Dispose() }
    # Do not unlink the gate file: deleting an unlocked name creates lock races.
}

function Assert-WorkshopBuildLease {
    param([Parameter(Mandatory)][string]$Root,[Parameter(Mandatory)]$Lease)
    $canonicalRoot = [IO.Path]::GetFullPath($Root)
    $buildScript = Join-Path $canonicalRoot 'scripts/workshop/Build.ps1'
    if (-not @(Get-PSCallStack | Where-Object { $_.ScriptName -and [string]::Equals([IO.Path]::GetFullPath($_.ScriptName),$buildScript,[StringComparison]::OrdinalIgnoreCase) }).Count) {
        throw "Nested build authorization is valid only inside this checkout's Build.ps1 invocation."
    }
    $path = Join-Path $canonicalRoot '.artifacts/workshop/operation-gate.json'
    $self = Get-Process -Id $PID
    if ($Lease.Purpose -ne 'Build' -or $Lease.OwnerPid -ne $PID -or $Lease.OwnerStartTicks -ne $self.StartTime.ToUniversalTime().Ticks -or
        -not [string]::Equals($Lease.Root,$canonicalRoot,[StringComparison]::OrdinalIgnoreCase) -or
        $Lease.Stream -isnot [IO.FileStream] -or -not $Lease.Stream.CanWrite -or
        -not [string]::Equals($Lease.Stream.Name,$path,[StringComparison]::OrdinalIgnoreCase)) {
        throw 'Invalid nested build authorization; no Editor was launched.'
    }
    $record = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    if ($record.Id -ne $Lease.Id -or $record.OwnerPid -ne $PID -or $record.Purpose -ne 'Build' -or $record.OwnerStartTicks -ne $Lease.OwnerStartTicks) {
        throw 'Nested build authorization does not match the held gate; no Editor was launched.'
    }
}

function Stop-WorkshopOwnedProcess {
    param([Diagnostics.Process]$Process,[bool]$Started)
    if ($Started -and $Process -and -not $Process.HasExited) {
        $Process.Kill($true)
        if (-not $Process.WaitForExit(10000)) { throw 'Owned process did not exit after termination; inspect its recorded PID.' }
    }
}

function Wait-WorkshopProcess {
    param([Parameter(Mandatory)][Diagnostics.Process]$Process,
          [ValidateRange(1,86400)][int]$TimeoutSeconds,
          [ValidateRange(1,10000)][int]$PollMilliseconds = 10000,
          [scriptblock]$AvailableMemoryKiB = { (Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory })
    $clock = [Diagnostics.Stopwatch]::StartNew()
    $lowSamples = 0
    while ($true) {
        $remaining = $TimeoutSeconds * 1000 - $clock.ElapsedMilliseconds
        if ($remaining -le 0) { throw "Owned process timed out after $TimeoutSeconds seconds; its export/logs are retained." }
        if ($Process.WaitForExit([int][Math]::Min($PollMilliseconds,$remaining))) { return }
        if ($clock.Elapsed.TotalSeconds -ge $TimeoutSeconds) { throw "Owned process timed out after $TimeoutSeconds seconds; its export/logs are retained." }
        if ((& $AvailableMemoryKiB) -lt 1MB) { $lowSamples++ } else { $lowSamples = 0 }
        if ($lowSamples -ge 2) { throw 'Available RAM stayed below 1 GiB; stopping only this owned process tree. No success claimed.' }
    }
}

function Publish-WorkshopFile {
    param([Parameter(Mandatory)][string]$Source,[Parameter(Mandatory)][string]$Destination,
          [Parameter(Mandatory)][string]$ExpectedSha256,[Parameter(Mandatory)][long]$ExpectedBytes)
    $target = [IO.Path]::GetFullPath($Destination)
    New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
    $temporary = $target + '.part-' + [Guid]::NewGuid().ToString('N')
    try {
        [IO.File]::Copy([IO.Path]::GetFullPath($Source),$temporary,$false)
        if ($ExpectedBytes -le 0 -or (Get-Item -LiteralPath $temporary).Length -ne $ExpectedBytes -or
            (Get-FileHash -LiteralPath $temporary -Algorithm SHA256).Hash -ne $ExpectedSha256) {
            throw 'Atomic publication validation failed; the previous artifact is preserved.'
        }
        if ([IO.File]::Exists($target)) { [IO.File]::Replace($temporary,$target,[NullString]::Value) }
        else { [IO.File]::Move($temporary,$target) }
    } finally { if ([IO.File]::Exists($temporary)) { [IO.File]::Delete($temporary) } }
}

function Write-WorkshopJson {
    param([Parameter(Mandatory)][string]$Path,[Parameter(Mandatory)]$Value)
    $target = [IO.Path]::GetFullPath($Path)
    New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
    $temporary = $target + '.part-' + [Guid]::NewGuid().ToString('N')
    try {
        [IO.File]::WriteAllText($temporary,($Value | ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
        if ([IO.File]::Exists($target)) { [IO.File]::Replace($temporary,$target,[NullString]::Value) }
        else { [IO.File]::Move($temporary,$target) }
    } finally { if ([IO.File]::Exists($temporary)) { [IO.File]::Delete($temporary) } }
}
