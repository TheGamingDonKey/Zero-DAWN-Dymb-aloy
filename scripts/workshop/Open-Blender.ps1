#Requires -Version 7.0
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$workshopRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$workshopAlias = Join-Path $env:LOCALAPPDATA 'Microsoft\WindowsApps\blender-launcher.exe'
if (-not (Test-Path -LiteralPath $workshopAlias)) { throw 'Installed Blender Store launcher alias is missing. No application was installed or changed.' }
if (Get-CimInstance Win32_Process -Filter "Name = 'blender.exe'") { throw 'Blender is already running. Preserve its scene and start its MCP server there instead.' }
if (Get-NetTCPConnection -LocalPort 9876 -State Listen -ErrorAction SilentlyContinue) { throw 'Blender bridge port9876 is occupied. No process was stopped.' }
$workshopActiveBuild = Join-Path $workshopRoot '.artifacts\workshop\active-build.json'
if (Test-Path -LiteralPath $workshopActiveBuild) {
    $workshopRecorded = Get-Content -LiteralPath $workshopActiveBuild -Raw | ConvertFrom-Json
    $workshopRunning = Get-Process -Id $workshopRecorded.Pid -ErrorAction SilentlyContinue
    if ($workshopRunning -and $workshopRunning.StartTime.ToUniversalTime().Ticks -eq $workshopRecorded.StartTicks -and [string]::Equals($workshopRunning.Path,$workshopRecorded.Executable,[StringComparison]::OrdinalIgnoreCase)) {
        throw 'Finish Workshop Android packaging before opening Blender. No process was stopped.'
    }
}
if ((Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory -lt 1.5MB) { throw 'Defer Blender until at least1.5GiB RAM is free.' }
$workshopArtifact = Join-Path $workshopRoot '.artifacts\workshop'
New-Item -ItemType Directory -Path $workshopArtifact -Force | Out-Null
$workshopScript = Join-Path $workshopArtifact 'blender-start.py'
@'
import bpy, addon_utils, json
from pathlib import Path
report = Path(__file__).with_suffix('.json')
try:
    addon_utils.enable('blender_mcp', default_set=False, persistent=False)
    bpy.context.scene.blendermcp_port = 9876
    result = bpy.ops.blendermcp.start_server()
    report.write_text(json.dumps({'blender':bpy.app.version_string,'start_result':list(result),'port':9876,'file':bpy.data.filepath,'persistent_preferences_changed':False},indent=2))
except Exception as error:
    report.write_text(json.dumps({'startup_failed':str(error),'bridge_verified':False},indent=2))
    # This invocation opened an empty session, never another user's file.
    bpy.ops.wm.quit_blender()
'@ | Set-Content -LiteralPath $workshopScript -Encoding utf8
if (Test-Path -LiteralPath ([IO.Path]::ChangeExtension($workshopScript,'.json'))) { Remove-Item -LiteralPath ([IO.Path]::ChangeExtension($workshopScript,'.json')) }
Start-Process -FilePath $workshopAlias -ArgumentList @('--python',('"' + $workshopScript + '"')) -WindowStyle Hidden
'Blender bridge launch requested through its installed Store alias. Verify actual addon status/scene via Blender MCP; this message alone is not connectivity proof.'
