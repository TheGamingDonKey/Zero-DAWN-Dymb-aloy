#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$UnityPath = 'C:\Users\mrbos\Documents\UnityEditors\6000.3.25f1\Editor\Unity.exe',
    [ValidateRange(1,2)][int]$Workers = 1
)
$ErrorActionPreference = 'Stop'
$focusRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$focusActiveBuildPath = Join-Path $focusRoot '.artifacts\workshop\active-build.json'
if (Test-Path -LiteralPath $focusActiveBuildPath) {
    $focusRecordedBuild = Get-Content -LiteralPath $focusActiveBuildPath -Raw | ConvertFrom-Json
    $focusRunningBuild = Get-Process -Id $focusRecordedBuild.Pid -ErrorAction SilentlyContinue
    if ($focusRunningBuild -and $focusRunningBuild.StartTime.ToUniversalTime().Ticks -eq $focusRecordedBuild.StartTicks -and [string]::Equals($focusRunningBuild.Path,$focusRecordedBuild.Executable,[StringComparison]::OrdinalIgnoreCase)) {
        throw 'This Workshop Android packager is running. Finish packaging before opening its Editor; no process was stopped.'
    }
}
if (@(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { -not $_.ExecutablePath -or $_.ExecutablePath -match '\\Editor\\Unity\.exe$' }).Count) { throw 'An Editor is already open. No Editor was stopped; close it before this isolated launch.' }
if ((Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory -lt 1.5MB) { throw 'Defer until at least1.5GiB RAM is free.' }
if (@(Get-NetTCPConnection -State Listen -LocalPort 54486 -ErrorAction SilentlyContinue).Count) { throw 'Workshop MCP port54486 is occupied; no process was stopped.' }
$focusLogDir = Join-Path $focusRoot '.artifacts\logs'
$focusStatus = Join-Path $focusRoot '.artifacts\workshop\mcp\status'
New-Item -ItemType Directory -Path $focusLogDir,$focusStatus -Force | Out-Null
$focusStart = [Diagnostics.ProcessStartInfo]::new()
$focusStart.FileName = $UnityPath
$focusStart.UseShellExecute = $false
$focusStart.WorkingDirectory = $focusRoot
$focusStart.Environment['UNITY_MCP_STATUS_DIR'] = $focusStatus
$focusStart.Environment['BEE_BUILD_THREADS'] = [string]$Workers
$focusStart.Environment['DOTNET_PROCESSOR_COUNT'] = [string]$Workers
$focusStart.Environment['ADB_SERVER_SOCKET'] = 'tcp:127.0.0.1:54483'
$focusStart.Environment['ANDROID_ADB_SERVER_PORT'] = '54483'
# Coplay 10.0.0 identifies workers by the substring "-importWorker". Its
# registry therefore mistakes -ImportWorkerCount on the MAIN Editor for a
# worker and skips every command. Keep the two-job limit without that flag.
foreach ($focusArg in @('-projectPath',(Join-Path $focusRoot 'MRWorkshop'),'-buildTarget','Android','-job-worker-count',[string]$Workers,'-logFile',(Join-Path $focusLogDir ('mcp-editor-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')),'-executeMethod','MRWorkshop.Editor.WorkshopBootstrap.Open')) { $focusStart.ArgumentList.Add($focusArg) }
$focusProcess = [Diagnostics.Process]::Start($focusStart)
try { $focusProcess.PriorityClass = [Diagnostics.ProcessPriorityClass]::BelowNormal } catch { Write-Warning 'Could not lower owned Editor priority.' }
Write-Output "Project Editor PID: $($focusProcess.Id). This launch is not proof of licence/import/bridge readiness."
