# Requires a fresh real Unity Test Runner result, not a previous successful XML.
#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$UnityPath = 'C:\Users\mrbos\Documents\UnityEditors\6000.3.25f1\Editor\Unity.exe',
    [ValidateSet('EditMode','PlayMode')][string]$Mode = 'EditMode'
)
$ErrorActionPreference = 'Stop'
$focusRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$focusRunId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,8)
$focusResultDir = Join-Path $focusRoot ('.artifacts\tests\unity-' + $Mode + '-' + $focusRunId)
New-Item -ItemType Directory -Path $focusResultDir -Force | Out-Null
$focusXmlPath = Join-Path $focusResultDir 'results.xml'
$focusArgs = @('-buildTarget','Android','-runTests','-testPlatform',$Mode,'-testResults',$focusXmlPath)
if ($Mode -eq 'EditMode') { $focusArgs += '-nographics' }
# Test Runner owns shutdown; -quit would terminate tests before results are written.
& (Join-Path $PSScriptRoot 'Run-Unity.ps1') -UnityPath $UnityPath -EditorArguments $focusArgs -LogName ('tests-' + $Mode)
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
if (-not (Test-Path -LiteralPath $focusXmlPath)) { throw "Unity wrote no test result: $focusXmlPath" }
[xml]$focusXml = Get-Content -LiteralPath $focusXmlPath -Raw
$focusRun = $focusXml.'test-run'
$focusTotal = [int]$focusRun.total
$focusPassed = [int]$focusRun.passed
$focusFailed = [int]$focusRun.failed
$focusSkipped = [int]$focusRun.skipped
if ($focusTotal -le 0 -or $focusPassed -ne $focusTotal -or $focusFailed -ne 0 -or $focusSkipped -ne 0 -or $focusRun.result -ne 'Passed') { throw "Unity tests not all passing: total=$focusTotal passed=$focusPassed failed=$focusFailed skipped=$focusSkipped" }
[PSCustomObject]@{Environment='Unity Test Runner'; Mode=$Mode; Total=$focusTotal; Passed=$focusPassed; Results=$focusXmlPath} | ConvertTo-Json -Compress
exit 0
