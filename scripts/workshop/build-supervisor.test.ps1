#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
$supervisor = Join-Path $PSScriptRoot 'build-supervisor.ps1'
if (-not (Test-Path -LiteralPath $supervisor)) { throw 'FAIL: durable build supervisor entrypoint is missing.' }
$fixtureRoot = Join-Path ([IO.Path]::GetTempPath()) ('workshop-supervisor-' + [Guid]::NewGuid().ToString('N'))
$fixtureScripts = Join-Path $fixtureRoot 'scripts/workshop'
New-Item -ItemType Directory -Path $fixtureScripts -Force | Out-Null
Copy-Item -LiteralPath $supervisor,(Join-Path $PSScriptRoot 'Helpers.ps1') -Destination $fixtureScripts
$fixtureSupervisor = Join-Path $fixtureScripts 'build-supervisor.ps1'
$fixtureBuild = Join-Path $fixtureScripts 'Build.ps1'
$assertions = 0
function Assert-Supervisor($Condition,[string]$Message) {
    if (-not $Condition) { throw "FAIL: $Message. Fixture retained: $fixtureRoot" }
    $script:assertions++
}
function Invoke-SupervisorFixture([string]$Body) {
    [IO.File]::WriteAllText($fixtureBuild,$Body)
    $launch = & $fixtureSupervisor -Workers 1 -ResumeExport (Join-Path $fixtureRoot 'retained export') -PackageTimeoutSeconds 5 -ExportTimeoutSeconds 3
    $deadline = [DateTime]::UtcNow.AddSeconds(20)
    do {
        Start-Sleep -Milliseconds 100
        $outcome = & $fixtureSupervisor -InspectRun $launch.RunDirectory
    } while ($outcome.Status -in @('pending','running') -and [DateTime]::UtcNow -lt $deadline)
    Assert-Supervisor ($outcome.Status -in @('finished','failed')) 'fixture writes terminal status without console/session result'
    return $outcome
}
$parameterHeader = 'param($UnityPath,$Scene,$ResumeExport,$Workers,$ExportTimeoutSeconds,$PackageTimeoutSeconds)'
$success = Invoke-SupervisorFixture ($parameterHeader + "`n" + 'Write-Output "fixture stdout"; if ($Workers -ne 1 -or $PackageTimeoutSeconds -ne 5 -or $ExportTimeoutSeconds -ne 3 -or $ResumeExport -notlike "*retained export") { throw "arguments not forwarded" }; exit 0')
Assert-Supervisor ($success.BuildExitCode -eq 0 -and $success.SupervisorExitCode -eq 0) 'explicit success exits are durable'
Assert-Supervisor ((Get-Content -LiteralPath $success.Stdout -Raw) -match 'fixture stdout') 'stdout persists separately'
Assert-Supervisor ($success.WorkerPid -gt 0 -and $success.WorkerStartTicks -gt 0 -and $success.FinishedUtc) 'worker identity and terminal timestamp persist'
$nonzero = Invoke-SupervisorFixture ($parameterHeader + "`nexit 23")
Assert-Supervisor ($nonzero.BuildExitCode -eq 23 -and $nonzero.SupervisorExitCode -eq 23) 'nonzero build exit is retained exactly'
$exception = Invoke-SupervisorFixture ($parameterHeader + "`nthrow 'synthetic build rejection'")
Assert-Supervisor ($exception.Status -eq 'failed' -and $null -eq $exception.BuildExitCode -and $exception.Error -match 'synthetic build rejection') 'exception persists without inventing a build exit'
$abandoned = Get-Content -LiteralPath $exception.StatusPath -Raw | ConvertFrom-Json
$abandoned.Status = 'running'
$abandoned.WorkerPid = 2147483647
$abandoned.FinishedUtc = $null
$abandoned.BuildExitCode = $null
$abandoned.SupervisorExitCode = $null
$abandoned | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $exception.StatusPath
$unknown = & $fixtureSupervisor -InspectRun $exception.RunDirectory
Assert-Supervisor ($unknown.ObservedStatus -eq 'unknown' -and $null -eq $unknown.BuildExitCode) 'disappeared worker remains unknown'
Assert-Supervisor ((Get-Content -LiteralPath $exception.StatusPath -Raw | ConvertFrom-Json).Status -eq 'running') 'inspection does not rewrite incomplete history'
Write-Output "PASS: $assertions focused supervisor assertions. Lightweight fixture PowerShell only; no Unity/Blender/Gradle/ADB actions. Fixture: $fixtureRoot"
