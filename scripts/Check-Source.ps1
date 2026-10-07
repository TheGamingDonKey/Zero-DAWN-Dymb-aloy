#Requires -Version 7.0
[CmdletBinding()]
param([string]$UnityData = 'C:\Users\mrbos\Documents\UnityEditors\6000.3.25f1\Editor\Data')
$ErrorActionPreference = 'Stop'
$focusRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$focusOut = Join-Path $focusRoot ('.artifacts\checks\source-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $focusOut -Force | Out-Null
$focusCompiler = Join-Path $UnityData 'DotNetSdkRoslyn\csc.dll'
$focusDotnet = Join-Path $UnityData 'NetCoreRuntime\dotnet.exe'
$focusSdk = Join-Path $focusRoot 'FocusCore\Library\ScriptAssemblies'
if (-not (Test-Path -LiteralPath (Join-Path $focusSdk 'Oculus.Interaction.dll'))) { throw 'Import the pinned SDKs once before this cached-API check.' }
$focusRefs = @((Join-Path $UnityData 'NetStandard\ref\2.1.0\netstandard.dll'))
$focusRefs += @(Get-ChildItem -LiteralPath (Join-Path $UnityData 'NetStandard\compat\2.1.0\shims\netfx') -Filter '*.dll' | Select-Object -ExpandProperty FullName)
$focusRefs += @(Get-ChildItem -LiteralPath (Join-Path $UnityData 'Managed\UnityEngine') -Filter '*.dll' | Select-Object -ExpandProperty FullName)
$focusRefs += @(Get-ChildItem -LiteralPath $focusSdk -Filter '*.dll' | Where-Object { $_.Name -notlike 'FocusCore.*' } | Select-Object -ExpandProperty FullName)
$focusResults = @()
foreach ($focusPart in @('Domain','Runtime','Editor','PlayModeTests')) {
    if ($focusPart -eq 'Domain') { $focusFolder = 'Runtime\Domain' }
    elseif ($focusPart -eq 'PlayModeTests') { $focusFolder = 'Tests\PlayMode' }
    else { $focusFolder = $focusPart }
    $focusFiles = @(Get-ChildItem -LiteralPath (Join-Path $focusRoot ('FocusCore\Assets\FocusCore\' + $focusFolder)) -Filter '*.cs' | Select-Object -ExpandProperty FullName)
    if ($focusFiles.Count -eq 0) { continue }
    $focusDll = Join-Path $focusOut ('FocusCore.' + $focusPart + '.dll')
    $focusArgs = @('-nologo','-target:library','-nostdlib+','-langversion:9','-define:UNITY_EDITOR,UNITY_6000_3_OR_NEWER,UNITY_INCLUDE_TESTS',('-out:"' + $focusDll + '"'))
    $focusPartRefs = @($focusRefs)
    if ($focusPart -eq 'Editor') { $focusPartRefs += Join-Path $UnityData 'Managed\UnityEditor.dll' }
    if ($focusPart -eq 'PlayModeTests') {
        $focusNunit = @(Get-ChildItem -LiteralPath (Join-Path $focusRoot 'FocusCore\Library\PackageCache') -Filter 'com.unity.ext.nunit@*' -Directory)
        $focusPartRefs += Join-Path $focusNunit[0].FullName 'net40\unity-custom\nunit.framework.dll'
    }
    $focusArgs += $focusPartRefs | Sort-Object -Unique | ForEach-Object { '-reference:"' + $_ + '"' }
    $focusArgs += $focusFiles | ForEach-Object { '"' + $_ + '"' }
    $focusRsp = Join-Path $focusOut ($focusPart + '.rsp')
    [IO.File]::WriteAllLines($focusRsp,$focusArgs)
    $focusOutput = & $focusDotnet $focusCompiler ('@' + $focusRsp) 2>&1
    [IO.File]::WriteAllLines((Join-Path $focusOut ($focusPart + '.log')),[string[]]@($focusOutput))
    if ($LASTEXITCODE -ne 0) { $focusOutput | Write-Output; throw "Static compile failed: $focusPart. This is not a Unity runtime check." }
    $focusRefs += $focusDll
    $focusResults += [ordered]@{Assembly=$focusPart;SourceCount=$focusFiles.Count;ExitCode=0;OutputSHA256=(Get-FileHash -LiteralPath $focusDll -Algorithm SHA256).Hash;Sources=@($focusFiles | ForEach-Object { [ordered]@{Path=[IO.Path]::GetRelativePath($focusRoot,$_);SHA256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash} })}
    Write-Output "Cached Unity API compile: $focusPart succeeded ($($focusFiles.Count) source files)."
}
[ordered]@{Check='Static cached Unity API compilation; NOT Editor/runtime/shader/Android verification';UnityData=$UnityData;Assemblies=$focusResults} | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $focusOut 'result.json')
Write-Output "Evidence: $focusOut. Shader/import/scene/PlayMode/Android execution NOT verified by this check."
