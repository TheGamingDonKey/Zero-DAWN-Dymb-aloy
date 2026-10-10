# Runs the installed Editor directly. The standalone CLI manages installation/licenses.
#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$UnityPath = 'C:\Users\mrbos\Documents\UnityEditors\6000.3.25f1\Editor\Unity.exe',
    [string[]]$EditorArguments = @('-quit', '-nographics'),
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]*$')][string]$LogName = 'editor',
    [ValidateRange(1,2)][int]$Workers = 2,
    [ValidateRange(1,86400)][int]$TimeoutSeconds = 3600,
    [object]$BuildLease
)
$ErrorActionPreference = 'Stop'
$focusRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$focusProject = Join-Path $focusRoot 'MRWorkshop'
. (Join-Path $PSScriptRoot 'Helpers.ps1')
$focusOwnGate = $null
$focusProcess = $null
$focusStarted = $false
try {
if ($BuildLease) { Assert-WorkshopBuildLease -Root $focusRoot -Lease $BuildLease }
else { $focusOwnGate = Enter-WorkshopGate -Root $focusRoot -Purpose CLI }
if (-not (Test-Path -LiteralPath $UnityPath)) { throw "Editor not found: $UnityPath" }
if (-not (Test-Path -LiteralPath (Join-Path $focusProject 'ProjectSettings\ProjectVersion.txt'))) { throw 'Workshop project not found.' }
# Never close another Editor or compete with an interactive Editor for this project.
$focusEditors = @(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { -not $_.ExecutablePath -or $_.ExecutablePath -match '\\Editor\\Unity\.exe$' })
if ($focusEditors.Count -gt 0) { throw 'An Editor is already running. Finish/close it before starting another CLI Editor; no process was stopped.' }
$focusAdbListener = @(Get-NetTCPConnection -State Listen -LocalPort 54483 -ErrorAction SilentlyContinue)
if ($focusAdbListener.Count -gt 0) { throw 'Focus private ADB port 54483 is already occupied. Defer this run; no server was stopped.' }
$focusFreeKiB = (Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory
if ($focusFreeKiB -lt 1.5MB) { throw 'Less than 1.5 GiB RAM is available. Defer this Editor run; no other app was closed.' }
$focusRunId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,8)
$focusLogDir = Join-Path $focusRoot '.artifacts\logs'
New-Item -ItemType Directory -Path $focusLogDir -Force | Out-Null
$focusLogPath = Join-Path $focusLogDir ($LogName + '-' + $focusRunId + '.log')
$focusStart = [Diagnostics.ProcessStartInfo]::new()
$focusStart.FileName = [IO.Path]::GetFullPath($UnityPath)
$focusStart.WorkingDirectory = $focusRoot
$focusStart.UseShellExecute = $false
$focusStart.CreateNoWindow = $true
$focusStart.Environment['DOTNET_PROCESSOR_COUNT'] = [string]$Workers
$focusStart.Environment['BEE_BUILD_THREADS'] = [string]$Workers
$focusCache = & (Join-Path $PSScriptRoot '../Get-BuildCache.ps1')
$focusStart.Environment['GRADLE_USER_HOME'] = Join-Path $focusCache 'gradle'
$focusStart.Environment['ADB_SERVER_SOCKET'] = 'tcp:127.0.0.1:54483'
$focusStart.Environment['ANDROID_ADB_SERVER_PORT'] = '54483'
foreach ($focusArg in @('-batchmode', '-projectPath', $focusProject, '-logFile', $focusLogPath, '-job-worker-count', [string]$Workers) + $EditorArguments) {
    $focusStart.ArgumentList.Add($focusArg)
}
$focusProcess = [Diagnostics.Process]::new()
$focusProcess.StartInfo = $focusStart
    if (-not $focusProcess.Start()) { throw 'Could not start Editor.' }
    $focusStarted = $true
    try { $focusProcess.PriorityClass = [Diagnostics.ProcessPriorityClass]::BelowNormal } catch { Write-Warning 'Could not lower this Editor process priority.' }
    Write-Output "Editor PID: $($focusProcess.Id); log: $focusLogPath"
    Wait-WorkshopProcess -Process $focusProcess -TimeoutSeconds $TimeoutSeconds
    $focusExit = $focusProcess.ExitCode
    [PSCustomObject]@{ExitCode=$focusExit; Log=$focusLogPath} | ConvertTo-Json -Compress
} finally {
    try { Stop-WorkshopOwnedProcess -Process $focusProcess -Started $focusStarted }
    finally {
        if ($focusProcess) { $focusProcess.Dispose() }
        if ($focusOwnGate) { Exit-WorkshopGate $focusOwnGate }
    }
}
exit $focusExit
