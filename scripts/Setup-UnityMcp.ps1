#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$CodexPath = 'C:\Users\mrbos\AppData\Local\OpenAI\Codex\bin\5ea220ae823df3d7\codex.exe',
    [string]$PythonPath = 'C:\Users\mrbos\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe',
    [string]$UvPath = 'C:\Users\mrbos\.local\bin\uvx.exe'
)
$ErrorActionPreference = 'Stop'
$focusRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
foreach ($focusExe in @($CodexPath,$PythonPath,$UvPath)) { if (-not (Test-Path -LiteralPath $focusExe)) { throw "Executable missing: $focusExe; supply its current path." } }
$focusExisting = & $CodexPath mcp get FocusUnity --json 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Output 'FocusUnity already exists. Inspect its project path with codex mcp get FocusUnity --json; no existing entry was replaced.'
    return
}
$focusConfig = Join-Path $env:USERPROFILE '.codex\config.toml'
$focusBackup = Join-Path $focusRoot '.artifacts\config-backup'
New-Item -ItemType Directory -Path $focusBackup -Force | Out-Null
if (Test-Path -LiteralPath $focusConfig) {
    Copy-Item -LiteralPath $focusConfig -Destination (Join-Path $focusBackup ('codex-before-focus-unity-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.toml'))
}
$focusArgs = @('mcp','add','FocusUnity','--env',('UNITY_MCP_STATUS_DIR=' + (Join-Path $focusRoot '.artifacts\mcp\status')),'--env','UNITY_MCP_SKIP_STARTUP_CONNECT=1','--',$UvPath,'--python',$PythonPath,'--from','mcpforunityserver==10.0.0','mcp-for-unity','--transport','stdio')
& $CodexPath @focusArgs
if ($LASTEXITCODE -ne 0) { throw 'Codex MCP registration failed.' }
Write-Output 'Registered only FocusUnity. A fresh Codex session may be required to load it; Editor round-trip verification is separate.'
