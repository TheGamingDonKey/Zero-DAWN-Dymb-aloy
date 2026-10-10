# Compile against the installed Unity/SDK reference assemblies without launching Unity.
# This is a host compiler check, not an Editor import, scene, test or headset result.
#Requires -Version 7.0
[CmdletBinding()]
param([string]$UnityPath='C:\Users\mrbos\Documents\UnityEditors\6000.3.25f1\Editor\Unity.exe')
$ErrorActionPreference='Stop'
$workshopRepo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$workshopSource=Join-Path $workshopRepo 'MRWorkshop/Assets/Workshop'
$workshopOutput=Join-Path $workshopRepo ('.artifacts/workshop/compile-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path $workshopOutput -Force | Out-Null
$workshopManaged=Join-Path (Split-Path -Parent $UnityPath) 'Data/Managed'
$workshopReferences=@(Get-ChildItem (Join-Path $PSHOME 'ref') -Filter '*.dll' | ForEach-Object FullName)
$workshopReferences+=@(Get-ChildItem $workshopManaged -Filter 'Unity*.dll' | ForEach-Object FullName)
$workshopReferences+=@(Get-ChildItem (Join-Path $workshopManaged 'UnityEngine') -Filter '*.dll' | ForEach-Object FullName)
$workshopReferences+=@(Get-ChildItem (Join-Path $workshopRepo 'FocusCore/Library/ScriptAssemblies') -Filter '*.dll' | Where-Object Name -notlike 'FocusCore*' | ForEach-Object FullName)
$workshopDomain=Join-Path $workshopOutput 'MRWorkshop.Domain.dll'
$workshopRuntime=Join-Path $workshopOutput 'MRWorkshop.Runtime.dll'
$workshopEditor=Join-Path $workshopOutput 'MRWorkshop.Editor.dll'
try {
    Add-Type -Path @(Get-ChildItem (Join-Path $workshopSource 'Runtime/Domain') -Filter '*.cs' | ForEach-Object FullName) -ReferencedAssemblies $workshopReferences -OutputAssembly $workshopDomain -CompilerOptions '-nowarn:1701,1702'
    $workshopReferences+=$workshopDomain
    Add-Type -Path @(Get-ChildItem (Join-Path $workshopSource 'Runtime') -Filter '*.cs' | ForEach-Object FullName) -ReferencedAssemblies $workshopReferences -OutputAssembly $workshopRuntime -CompilerOptions '-nowarn:1701,1702'
    $workshopReferences+=$workshopRuntime
    Add-Type -Path @(Get-ChildItem (Join-Path $workshopSource 'Editor') -Filter '*.cs' | ForEach-Object FullName) -ReferencedAssemblies $workshopReferences -OutputAssembly $workshopEditor -CompilerOptions '-nowarn:1701,1702'
    $workshopNUnit=Join-Path $workshopRepo '.artifacts/sdk/com.unity.ext.nunit/package/net40/unity-custom/nunit.framework.dll'
    if(-not(Test-Path $workshopNUnit)) { $workshopNUnit=Get-ChildItem (Join-Path $workshopRepo 'FocusCore/Library/PackageCache') -Directory -Filter 'com.unity.ext.nunit@*' | ForEach-Object { Join-Path $_.FullName 'net40/unity-custom/nunit.framework.dll' } | Where-Object { Test-Path $_ } | Select-Object -First 1 }
    if(-not $workshopNUnit) { throw 'Installed NUnit reference missing' }
    $workshopTests=Join-Path $workshopOutput 'MRWorkshop.TestSource.dll'
    Add-Type -Path @(Get-ChildItem (Join-Path $workshopSource 'Tests') -Recurse -Filter '*.cs' | ForEach-Object FullName) -ReferencedAssemblies @($workshopReferences+$workshopNUnit) -OutputAssembly $workshopTests -CompilerOptions '-nowarn:1701,1702'
    [PSCustomObject]@{Environment='Host C# compilation against installed Unity/SDK assemblies; not Unity execution';Passed=$true;Output=$workshopOutput;Assemblies=@($workshopDomain,$workshopRuntime,$workshopEditor,$workshopTests)} | ConvertTo-Json -Compress
} catch {
    $_ | Out-String | Set-Content (Join-Path $workshopOutput 'error.log')
    Write-Error $_ -ErrorAction Continue
    exit 1
}
