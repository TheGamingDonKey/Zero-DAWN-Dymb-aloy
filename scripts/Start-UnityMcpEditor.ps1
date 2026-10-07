#Requires -Version 7.0
[CmdletBinding()]
param([string]$UnityPath = 'C:\Users\mrbos\Documents\UnityEditors\6000.3.25f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
$focusRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (@(Get-Process -Name Unity -ErrorAction SilentlyContinue).Count) { throw 'An Editor is already open. No Editor was stopped; close it before this isolated launch.' }
if ((Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory -lt 1.5MB) { throw 'Defer until at least1.5GiB RAM is free.' }
if (@(Get-NetTCPConnection -State Listen -LocalPort 54484 -ErrorAction SilentlyContinue).Count) { throw 'Project MCP port54484 is occupied; no process was stopped.' }
$focusLogDir = Join-Path $focusRoot '.artifacts\logs'
$focusStatus = Join-Path $focusRoot '.artifacts\mcp\status'
New-Item -ItemType Directory -Path $focusLogDir,$focusStatus -Force | Out-Null
$focusStart = [Diagnostics.ProcessStartInfo]::new()
$focusStart.FileName = $UnityPath
$focusStart.UseShellExecute = $false
$focusStart.WorkingDirectory = $focusRoot
$focusStart.Environment['UNITY_MCP_STATUS_DIR'] = $focusStatus
$focusStart.Environment['BEE_BUILD_THREADS'] = '2'
$focusStart.Environment['DOTNET_PROCESSOR_COUNT'] = '2'
$focusStart.Environment['ADB_SERVER_SOCKET'] = 'tcp:127.0.0.1:54483'
$focusStart.Environment['ANDROID_ADB_SERVER_PORT'] = '54483'
foreach ($focusArg in @('-projectPath',(Join-Path $focusRoot 'FocusCore'),'-buildTarget','Android','-job-worker-count','2','-ImportWorkerCount','1','-logFile',(Join-Path $focusLogDir ('mcp-editor-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')),'-executeMethod','FocusCore.Editor.FocusUnityMcp.Start')) { $focusStart.ArgumentList.Add($focusArg) }
$focusProcess = [Diagnostics.Process]::Start($focusStart)
try { $focusProcess.PriorityClass = [Diagnostics.ProcessPriorityClass]::BelowNormal } catch { Write-Warning 'Could not lower owned Editor priority.' }
Write-Output "Project Editor PID: $($focusProcess.Id). This launch is not proof of licence/import/bridge readiness."
