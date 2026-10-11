#Requires -Version 7.0
[CmdletBinding(DefaultParameterSetName='Launch')]
param(
    [Parameter(ParameterSetName='Launch')][string]$UnityPath = 'C:\Users\mrbos\Documents\UnityEditors\6000.3.25f1\Editor\Unity.exe',
    [Parameter(ParameterSetName='Launch')][ValidateSet('Workshop')][string]$Scene = 'Workshop',
    [Parameter(ParameterSetName='Launch')][string]$ResumeExport,
    [Parameter(ParameterSetName='Launch')][ValidateRange(1,2)][int]$Workers = 1,
    [Parameter(ParameterSetName='Launch')][ValidateRange(1,86400)][int]$ExportTimeoutSeconds = 3600,
    [Parameter(ParameterSetName='Launch')][ValidateRange(1,86400)][int]$PackageTimeoutSeconds = 7200,
    [Parameter(Mandatory,ParameterSetName='Inspect')][string]$InspectRun,
    [Parameter(Mandatory,ParameterSetName='Worker')][string]$WorkerRun
)
$ErrorActionPreference = 'Stop'
$supervisorRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'Helpers.ps1')
$supervisorRuns = Join-Path $supervisorRoot '.artifacts/workshop/build-supervisor'

function Read-SupervisorJson([string]$Path) {
    # Windows readers must permit replacement while the worker atomically writes status.
    $stream = [IO.FileStream]::new($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
    $reader = [IO.StreamReader]::new($stream)
    try { return $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
}

if ($PSCmdlet.ParameterSetName -in @('Inspect','Worker')) {
    $runDirectory = [IO.Path]::GetFullPath($(if ($WorkerRun) { $WorkerRun } else { $InspectRun }))
    if (-not [string]::Equals((Split-Path -Parent $runDirectory),$supervisorRuns,[StringComparison]::OrdinalIgnoreCase) -or
        (Split-Path -Leaf $runDirectory) -notmatch '^[a-f0-9]{32}$') {
        throw 'Supervisor run must be a uniquely identified directory inside this checkout.'
    }
    $statusPath = Join-Path $runDirectory 'status.json'
    $status = Read-SupervisorJson $statusPath
    if ($PSCmdlet.ParameterSetName -eq 'Inspect') {
        $worker = if ($status.WorkerPid) { Get-Process -Id $status.WorkerPid -ErrorAction SilentlyContinue }
        $alive = $false
        if ($worker) {
            try { $alive = $worker.StartTime.ToUniversalTime().Ticks -eq $status.WorkerStartTicks -and
                [string]::Equals($worker.Path,$status.WorkerExecutable,[StringComparison]::OrdinalIgnoreCase) } catch { }
        }
        $observed = if ($status.Status -in @('finished','failed')) { $status.Status } elseif ($alive) { 'running' } else { 'unknown' }
        $status | Add-Member -NotePropertyName WorkerAlive -NotePropertyValue $alive
        $status | Add-Member -NotePropertyName ObservedStatus -NotePropertyValue $observed
        return $status
    }
    if ($status.Status -ne 'pending') { throw 'Supervisor worker refuses to reuse an existing run.' }
    $request = Get-Content -LiteralPath (Join-Path $runDirectory 'request.json') -Raw | ConvertFrom-Json
    $self = Get-Process -Id $PID
    $status.Status = 'running'
    $status.WorkerPid = $PID
    $status.WorkerStartTicks = $self.StartTime.ToUniversalTime().Ticks
    $status.WorkerExecutable = $self.Path
    $status.StartedUtc = [DateTime]::UtcNow.ToString('o')
    Write-WorkshopJson -Path $statusPath -Value $status
    $supervisorExit = 1
    try {
        $buildParameters = @{}
        foreach ($property in $request.BuildParameters.PSObject.Properties) { $buildParameters[$property.Name] = $property.Value }
        # Build remains the sole owner of admission, timeout and process cleanup.
        # A nested script's explicit exit returns here as LASTEXITCODE.
        $global:LASTEXITCODE = 0
        & (Join-Path $PSScriptRoot 'Build.ps1') @buildParameters
        $status.BuildExitCode = $LASTEXITCODE
        $supervisorExit = [int]$LASTEXITCODE
        $status.Status = 'finished'
    } catch {
        $status.Status = 'failed'
        $status.Error = $_ | Out-String
        Write-Warning $status.Error
    } finally {
        $status.SupervisorExitCode = $supervisorExit
        $status.FinishedUtc = [DateTime]::UtcNow.ToString('o')
        Write-WorkshopJson -Path $statusPath -Value $status
    }
    exit $supervisorExit
}

$runDirectory = Join-Path $supervisorRuns ([Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $runDirectory | Out-Null
$statusPath = Join-Path $runDirectory 'status.json'
$buildParameters = @{UnityPath=$UnityPath; Scene=$Scene; Workers=$Workers; ExportTimeoutSeconds=$ExportTimeoutSeconds; PackageTimeoutSeconds=$PackageTimeoutSeconds}
if ($ResumeExport) { $buildParameters.ResumeExport = [IO.Path]::GetFullPath($ResumeExport) }
$sourceCommit = $null
if (Test-Path -LiteralPath (Join-Path $supervisorRoot '.git')) {
    $sourceCommit = & git -C $supervisorRoot rev-parse HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Cannot record current source commit.' }
}
$status = [pscustomobject]@{
    SchemaVersion=1; Status='pending'; RunDirectory=$runDirectory; StatusPath=$statusPath
    RequestedUtc=[DateTime]::UtcNow.ToString('o'); StartedUtc=$null; FinishedUtc=$null
    WorkerPid=$null; WorkerStartTicks=$null; WorkerExecutable=$null
    BuildExitCode=$null; SupervisorExitCode=$null; Error=$null
    Stdout=(Join-Path $runDirectory 'stdout.log'); Stderr=(Join-Path $runDirectory 'stderr.log')
    SourceCommitAtLaunch=$sourceCommit; ResumeExport=$buildParameters.ResumeExport; HeadsetTested=$false
}
Write-WorkshopJson -Path (Join-Path $runDirectory 'request.json') -Value @{
    BuildParameters=$buildParameters
    BuildScriptSha256=(Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'Build.ps1') -Algorithm SHA256).Hash
    Note='SourceCommitAtLaunch records this checkout, not the provenance of a retained export.'
}
Write-WorkshopJson -Path $statusPath -Value $status
try {
    $powerShellExecutable = (Get-Process -Id $PID).Path
    # Start-Process joins ArgumentList into a Windows command line: quote paths explicitly.
    $workerArguments = @('-NoProfile','-File',('"' + $PSCommandPath + '"'),'-WorkerRun',('"' + $runDirectory + '"'))
    $worker = Start-Process -FilePath $powerShellExecutable -ArgumentList $workerArguments -WindowStyle Hidden -WorkingDirectory $supervisorRoot -RedirectStandardOutput $status.Stdout -RedirectStandardError $status.Stderr -PassThru
    [pscustomobject]@{RunDirectory=$runDirectory; StatusPath=$statusPath; WorkerPid=$worker.Id; Stdout=$status.Stdout; Stderr=$status.Stderr}
} catch {
    $status.Status = 'failed'
    $status.Error = $_ | Out-String
    $status.SupervisorExitCode = 1
    $status.FinishedUtc = [DateTime]::UtcNow.ToString('o')
    Write-WorkshopJson -Path $statusPath -Value $status
    throw
}
