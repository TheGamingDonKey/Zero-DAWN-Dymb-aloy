# Runs the same pure C# NUnit tests without starting Unity. Requires PowerShell 7.
[CmdletBinding()]
param([string]$NUnitPath)
$ErrorActionPreference = 'Stop'
$focusRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (-not $NUnitPath) {
    $NUnitPath = Get-ChildItem (Join-Path $focusRoot 'MRWorkshop/Library/PackageCache') -Directory -Filter 'com.unity.ext.nunit@*' -ErrorAction SilentlyContinue | ForEach-Object { Join-Path $_.FullName 'net40/unity-custom/nunit.framework.dll' } | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $NUnitPath) { $NUnitPath = Join-Path $focusRoot '.artifacts\sdk\com.unity.ext.nunit\package\net40\unity-custom\nunit.framework.dll' }
    if (-not (Test-Path -LiteralPath $NUnitPath)) {
        $focusPackageCache = Join-Path $focusRoot 'FocusCore\Library\PackageCache'
        if (Test-Path -LiteralPath $focusPackageCache) {
            $focusCachedNUnit = Get-ChildItem -LiteralPath $focusPackageCache -Directory -Filter 'com.unity.ext.nunit@*' | ForEach-Object { Join-Path $_.FullName 'net40\unity-custom\nunit.framework.dll' } | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
            if ($focusCachedNUnit) { $NUnitPath = $focusCachedNUnit }
        }
    }
}
if (-not (Test-Path -LiteralPath $NUnitPath)) { throw 'Supply -NUnitPath pointing to Unity com.unity.ext.nunit nunit.framework.dll.' }
$focusRunId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,8)
$focusResultDir = Join-Path $focusRoot ('.artifacts\tests\domain-' + $focusRunId)
New-Item -ItemType Directory -Path $focusResultDir -Force | Out-Null
$focusNUnit = [Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($NUnitPath))
$focusSources = @()
$focusDomainDir = Join-Path $focusRoot 'MRWorkshop\Assets\Workshop\Runtime\Domain'
if (Test-Path -LiteralPath $focusDomainDir) { $focusSources += @(Get-ChildItem -LiteralPath $focusDomainDir -Filter '*.cs' | ForEach-Object FullName) }
$focusSources += @(Get-ChildItem -LiteralPath (Join-Path $focusRoot 'MRWorkshop\Assets\Workshop\Tests\EditMode') -Filter '*Tests.cs' | ForEach-Object FullName)
$focusDll = Join-Path $focusResultDir 'MRWorkshop.DomainTests.dll'
try {
    $focusReferences = @(Get-ChildItem -LiteralPath (Join-Path $PSHOME 'ref') -Filter '*.dll' | ForEach-Object FullName) + @($NUnitPath)
    Add-Type -Path $focusSources -ReferencedAssemblies $focusReferences -OutputAssembly $focusDll
} catch {
    $_ | Out-String | Set-Content -LiteralPath (Join-Path $focusResultDir 'compile-error.log')
    Write-Error ('Domain compilation failed. Log: ' + $focusResultDir) -ErrorAction Continue
    exit 1
}
$focusAssembly = [Reflection.Assembly]::LoadFrom($focusDll)
$focusBuilder = [Activator]::CreateInstance($focusNUnit.GetType('NUnit.Framework.Api.DefaultTestAssemblyBuilder'))
$focusRunner = $focusNUnit.GetType('NUnit.Framework.Api.NUnitTestAssemblyRunner').GetConstructors()[0].Invoke([object[]]@($focusBuilder))
$focusSettings = New-Object 'System.Collections.Generic.Dictionary[string,object]'
$focusSettings['NumberOfTestWorkers'] = 0
$null = $focusRunner.Load($focusAssembly, $focusSettings)
$focusFlags = [Reflection.BindingFlags]'Public,NonPublic,Static'
$focusListener = $focusNUnit.GetType('NUnit.Framework.Internal.TestListener').GetProperty('NULL',$focusFlags).GetValue($null)
$focusFilter = $focusNUnit.GetType('NUnit.Framework.Internal.TestFilter').GetField('Empty',$focusFlags).GetValue($null)
$focusResult = $focusRunner.Run($focusListener, $focusFilter)
$focusXmlPath = Join-Path $focusResultDir 'results.xml'
$focusResult.ToXml($true).OuterXml | Set-Content -LiteralPath $focusXmlPath -Encoding UTF8
$focusTotal = [int]$focusResult.PassCount + [int]$focusResult.FailCount + [int]$focusResult.SkipCount + [int]$focusResult.InconclusiveCount
[PSCustomObject]@{Environment='Host C# domain tests; not Unity or headset'; Total=$focusTotal; Passed=$focusResult.PassCount; Failed=$focusResult.FailCount; Skipped=$focusResult.SkipCount; Results=$focusXmlPath} | ConvertTo-Json -Compress
if ($focusTotal -eq 0 -or $focusResult.FailCount -gt 0 -or $focusResult.SkipCount -gt 0 -or $focusResult.InconclusiveCount -gt 0) { exit 1 }
exit 0
