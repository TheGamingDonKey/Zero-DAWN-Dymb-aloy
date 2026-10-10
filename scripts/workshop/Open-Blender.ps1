#Requires -Version 7.0
[CmdletBinding()]
param([ValidateRange(1,120)][int]$LaunchTimeoutSeconds = 30)
$ErrorActionPreference = 'Stop'
$workshopRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'Helpers.ps1')
$workshopGate = Enter-WorkshopGate -Root $workshopRoot -Purpose Blender
try {
$workshopAlias = Join-Path $env:LOCALAPPDATA 'Microsoft\WindowsApps\blender-launcher.exe'
if (-not (Test-Path -LiteralPath $workshopAlias)) { throw 'Installed Blender Store launcher alias is missing. No application was installed or changed.' }
if (Get-CimInstance Win32_Process -Filter "Name = 'blender.exe'") { throw 'Blender is already running. Preserve its scene and start its MCP server there instead.' }
if (Get-NetTCPConnection -LocalPort 9876 -State Listen -ErrorAction SilentlyContinue) { throw 'Blender bridge port9876 is occupied. No process was stopped.' }
if ((Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory -lt 1.5MB) { throw 'Defer Blender until at least1.5GiB RAM is free.' }
$workshopArtifact = Join-Path $workshopRoot '.artifacts\workshop'
New-Item -ItemType Directory -Path $workshopArtifact -Force | Out-Null
$workshopInvocationId = [Guid]::NewGuid().ToString('N')
$workshopScript = Join-Path $workshopArtifact ('blender-start-' + $workshopInvocationId + '.py')
$workshopLaunchMarker = Join-Path $workshopArtifact 'blender-launch.json'
@'
import bpy, addon_utils, json, os
from pathlib import Path
report = Path(__file__).with_suffix('.json')
identity = {'invocation_id':Path(__file__).stem.removeprefix('blender-start-'),'script':str(Path(__file__).resolve()),'process_id':os.getpid()}
try:
    addon_utils.enable('blender_mcp', default_set=False, persistent=False)
    bpy.context.scene.blendermcp_port = 9876
    result = bpy.ops.blendermcp.start_server()
    report.write_text(json.dumps({**identity,'startup_outcome':'started','blender':bpy.app.version_string,'start_result':list(result),'port':9876,'file':bpy.data.filepath,'persistent_preferences_changed':False},indent=2))
except Exception as error:
    report.write_text(json.dumps({**identity,'startup_outcome':'failed','startup_failed':str(error),'bridge_verified':False},indent=2))
    # This invocation opened an empty session, never another user's file.
    bpy.ops.wm.quit_blender()
'@ | Set-Content -LiteralPath $workshopScript -Encoding utf8
# Persist ownership BEFORE requesting Store activation. There is no expiry:
# a timed-out Store request can still activate later.
Write-WorkshopJson -Path $workshopLaunchMarker -Value @{InvocationId=$workshopInvocationId; Script=$workshopScript; Report=[IO.Path]::ChangeExtension($workshopScript,'.json'); RequestedAtUtc=[DateTime]::UtcNow.ToString('o')}
Start-Process -FilePath $workshopAlias -ArgumentList @('--python',('"' + $workshopScript + '"')) -WindowStyle Hidden
# The Store alias can return before Blender exists. Hold admission until its
# process appears, so packaging cannot enter that asynchronous launch gap.
$workshopLaunchClock = [Diagnostics.Stopwatch]::StartNew()
while (-not (Resolve-WorkshopBlenderLaunch -Root $workshopRoot -ExpectedInvocationId $workshopInvocationId)) {
    if ($workshopLaunchClock.Elapsed.TotalSeconds -ge $LaunchTimeoutSeconds) { throw "Blender launch was not observed within $LaunchTimeoutSeconds seconds. Its unresolved Store request is retained at $workshopLaunchMarker; build/CLI/duplicate launches remain blocked. No process was stopped." }
    Start-Sleep -Milliseconds 200
}
'Blender bridge launch requested through its installed Store alias. Verify actual addon status/scene via Blender MCP; this message alone is not connectivity proof.'
} finally { Exit-WorkshopGate $workshopGate }
