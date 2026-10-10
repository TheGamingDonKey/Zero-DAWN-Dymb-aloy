#Requires -Version 7.0
[CmdletBinding()]
param([ValidateSet('Connection','Input','Rules')][string]$Action = 'Connection')
$ErrorActionPreference = 'Stop'
$workshopPython = Join-Path $env:USERPROFILE '.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
$workshopUv = Join-Path $env:USERPROFILE '.local\bin\uvx.exe'
foreach ($workshopTool in @($workshopPython,$workshopUv)) {
    if (-not (Test-Path -LiteralPath $workshopTool)) { throw "Required installed bridge runtime missing: $workshopTool" }
}
$workshopPreviousPython = $env:FOCUS_PYTHON
try {
    $env:FOCUS_PYTHON = $workshopPython
    $workshopArguments = @('--python',$workshopPython,'--from','mcpforunityserver==10.0.0','python',(Join-Path $PSScriptRoot 'check_unity_mcp.py'))
    if ($Action -ne 'Connection') { $workshopArguments += @('--diagnostic',$Action.ToLowerInvariant()) }
    # Use the pinned server's isolated dependency environment; bundled base
    # Python deliberately does not install the MCP client library globally.
    & $workshopUv @workshopArguments
    if ($LASTEXITCODE -ne 0) { throw "Workshop bridge check failed (exit $LASTEXITCODE); no automatic retry." }
} finally { $env:FOCUS_PYTHON = $workshopPreviousPython }
