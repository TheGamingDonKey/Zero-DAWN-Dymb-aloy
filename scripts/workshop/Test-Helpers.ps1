# Synthetic helper checks only: never starts Unity, Blender, Gradle or ADB.
#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
$helper = Join-Path $PSScriptRoot 'Helpers.ps1'
if (-not (Test-Path -LiteralPath $helper)) { throw 'Missing shared lease/lifecycle/publication helpers.' }
. $helper
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('workshop-helpers-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot | Out-Null
$script:passed = 0
function Assert([bool]$Condition,[string]$Message) {
    if (-not $Condition) { throw $Message }
    $script:passed++
}
function Assert-Rejected([scriptblock]$Action,[string]$Pattern) {
    $caught = $null
    try {
        $result = & $Action
        foreach($value in @($result)) {
            if($value -and $value.PSObject.Properties['Stream'] -and $value.Stream -is [IDisposable]) { $value.Stream.Dispose() }
        }
    } catch { $caught = $_.Exception.Message }
    Assert ($caught -and $caught -match $Pattern) "Expected rejection matching '$Pattern', received '$caught'."
}
function Start-SyntheticProcess {
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = (Get-Process -Id $PID).Path
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    foreach($arg in @('-NoProfile','-NonInteractive','-Command','Start-Sleep -Seconds 30')) { $start.ArgumentList.Add($arg) }
    return [Diagnostics.Process]::Start($start)
}
$lease = $null
try {
    $lease = Enter-WorkshopGate -Root $testRoot -Purpose Build
    Assert-Rejected { Enter-WorkshopGate -Root $testRoot -Purpose Editor } 'another Workshop operation'
    Assert-Rejected { Assert-WorkshopBuildLease -Root $testRoot -Lease $lease } 'Build.ps1'
    $fixtureDir = Join-Path $testRoot 'scripts/workshop'
    New-Item -ItemType Directory -Path $fixtureDir -Force | Out-Null
    foreach($file in @('Helpers.ps1','Run-Unity.ps1','Test.ps1','Open.ps1','Open-Blender.ps1','Build.ps1')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination (Join-Path $fixtureDir $file)
    }
    foreach($entry in @('Run-Unity.ps1','Test.ps1','Open.ps1','Open-Blender.ps1','Build.ps1')) {
        Assert-Rejected { & (Join-Path $fixtureDir $entry) } 'another Workshop operation'
    }
    $fixtureBuild = Join-Path $fixtureDir 'Build.ps1'
    'param($Root,$Lease) Assert-WorkshopBuildLease -Root $Root -Lease $Lease' | Set-Content -LiteralPath $fixtureBuild
    & $fixtureBuild $testRoot $lease
    Assert $true 'Build-owned nested authorization failed.'
    $invalid = $lease.PSObject.Copy(); $invalid.Id = 'fabricated'
    Assert-Rejected { & $fixtureBuild $testRoot $invalid } 'authorization'
    'param($Root,$Lease) & (Join-Path $PSScriptRoot Run-Unity.ps1) -UnityPath (Join-Path $Root missing-editor.exe) -BuildLease $Lease' | Set-Content -LiteralPath $fixtureBuild
    Assert-Rejected { & $fixtureBuild $testRoot $lease } 'Editor not found'
    $child = [Diagnostics.ProcessStartInfo]::new()
    $child.FileName = (Get-Process -Id $PID).Path
    $child.UseShellExecute = $false; $child.CreateNoWindow = $true
    $child.RedirectStandardOutput = $true; $child.RedirectStandardError = $true
    foreach($arg in @('-NoProfile','-NonInteractive','-Command',". '$($helper.Replace("'","''"))'; try { Enter-WorkshopGate -Root '$($testRoot.Replace("'","''"))' -Purpose Build; exit 7 } catch { if (`$_.Exception.Message -match 'another Workshop operation') { exit 0 }; throw }")) { $child.ArgumentList.Add($arg) }
    $contender = [Diagnostics.Process]::Start($child)
    try {
        if (-not $contender.WaitForExit(15000)) { throw 'Synthetic contender timed out.' }
        Assert ($contender.ExitCode -eq 0) ('Cross-process exclusion failed: ' + $contender.StandardError.ReadToEnd())
    } finally { Stop-WorkshopOwnedProcess -Process $contender -Started $true; $contender.Dispose() }
    Exit-WorkshopGate $lease; $lease = $null
    Assert-Rejected { & (Join-Path $fixtureDir 'Run-Unity.ps1') -UnityPath (Join-Path $testRoot 'missing-editor.exe') } 'Editor not found'
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Build.ps1') -Destination $fixtureBuild -Force
    function Get-CimInstance { param($ClassName,$Filter) if ($Filter -eq "Name = 'blender.exe'") { [pscustomobject]@{Name='synthetic Blender'} } else { throw 'Unexpected system query in Blender rejection fixture.' } }
    try { Assert-Rejected { & $fixtureBuild } 'Close Blender' }
    finally { Remove-Item -LiteralPath Function:Get-CimInstance }
    $lease = Enter-WorkshopGate -Root $testRoot -Purpose Editor
    Assert ($lease.Purpose -eq 'Editor') 'Released/stale gate was not reusable.'
    Exit-WorkshopGate $lease; $lease = $null
    $self = Get-Process -Id $PID
    Write-WorkshopJson -Path (Join-Path $testRoot '.artifacts/workshop/active-build.json') -Value @{Pid=$PID; StartTicks=$self.StartTime.ToUniversalTime().Ticks; Executable=$self.Path}
    Assert-Rejected { Enter-WorkshopGate -Root $testRoot -Purpose Editor } 'packager'
    Write-WorkshopJson -Path (Join-Path $testRoot '.artifacts/workshop/active-build.json') -Value @{Pid=$PID; StartTicks=0; Executable=$self.Path}
    $lease = Enter-WorkshopGate -Root $testRoot -Purpose Build
    Assert ($lease.Purpose -eq 'Build') 'A stale/reused PID record blocked admission.'
    Exit-WorkshopGate $lease; $lease = $null
    [IO.File]::WriteAllText((Join-Path $testRoot '.artifacts/workshop/active-build.json'),'incomplete{')
    Assert-Rejected { Enter-WorkshopGate -Root $testRoot -Purpose Editor } 'ownership record is unreadable'
    Remove-Item -LiteralPath (Join-Path $testRoot '.artifacts/workshop/active-build.json')
    $launchMarker = Join-Path $testRoot '.artifacts/workshop/blender-launch.json'
    $launchId = [Guid]::NewGuid().ToString('N')
    $launchScript = Join-Path $testRoot ".artifacts/workshop/blender-start-$launchId.py"
    $launchReport = [IO.Path]::ChangeExtension($launchScript,'.json')
    Write-WorkshopJson -Path $launchMarker -Value @{InvocationId=$launchId; Script=$launchScript; Report=$launchReport}
    function Get-CimInstance { param($ClassName,$Filter) return $null }
    try {
        foreach($purpose in @('Build','CLI','Blender')) {
            Assert-Rejected { Enter-WorkshopGate -Root $testRoot -Purpose $purpose } 'unresolved Blender activation'
        }
        Assert (Test-Path -LiteralPath $launchMarker) 'Unobserved Store activation was forgotten.'
        Write-WorkshopJson -Path $launchReport -Value @{invocation_id='wrong'; script=$launchScript; startup_outcome='started'}
        Assert-Rejected { Enter-WorkshopGate -Root $testRoot -Purpose Build } 'unresolved Blender activation'
        Write-WorkshopJson -Path $launchReport -Value @{invocation_id=$launchId; script=$launchScript; startup_outcome='failed'}
        $lease = Enter-WorkshopGate -Root $testRoot -Purpose Editor
        Assert (-not (Test-Path -LiteralPath $launchMarker)) 'Matching completed startup outcome did not resolve its marker.'
        Exit-WorkshopGate $lease; $lease = $null
    } finally { Remove-Item -LiteralPath Function:Get-CimInstance }
    $aliasRoot = Join-Path $testRoot 'synthetic-local-profile'
    $alias = Join-Path $aliasRoot 'Microsoft/WindowsApps/blender-launcher.exe'
    New-Item -ItemType Directory -Path (Split-Path -Parent $alias) -Force | Out-Null
    [IO.File]::WriteAllText($alias,'synthetic alias; never executable')
    $savedLocalAppData = $env:LOCALAPPDATA
    $env:LOCALAPPDATA = $aliasRoot
    function Get-CimInstance { param($ClassName,$Filter) if($ClassName -eq 'Win32_OperatingSystem') { [pscustomobject]@{FreePhysicalMemory=4MB} } }
    function Get-NetTCPConnection { param($LocalPort,$State,$ErrorAction) }
    $activationObservation = [pscustomobject]@{Requested=$false}
    function Start-Process { param($FilePath,$ArgumentList,$WindowStyle) $activationObservation.Requested = $true }
    try {
        Assert-Rejected { & (Join-Path $fixtureDir 'Open-Blender.ps1') -LaunchTimeoutSeconds 1 } 'launch was not observed'
        Assert $activationObservation.Requested 'The synthetic Store activation was never requested.'
        Assert (Test-Path -LiteralPath $launchMarker) 'Launcher timeout released admission without retaining its unresolved Store request.'
        Assert-Rejected { Enter-WorkshopGate -Root $testRoot -Purpose Build } 'unresolved Blender activation'
        $pending = Get-Content -LiteralPath $launchMarker -Raw | ConvertFrom-Json
    } finally {
        $env:LOCALAPPDATA = $savedLocalAppData
        Remove-Item -LiteralPath Function:Get-CimInstance,Function:Get-NetTCPConnection,Function:Start-Process
    }
    function Get-CimInstance { param($ClassName,$Filter) [pscustomobject]@{Name='blender.exe'; ProcessId=123; CommandLine=('blender.exe --python "' + $pending.Script + '"')} }
    try {
        Assert-Rejected { & $fixtureBuild } 'Close Blender'
        Assert (-not (Test-Path -LiteralPath $launchMarker)) 'Observed late activation did not resolve its own request.'
        $lease = Enter-WorkshopGate -Root $testRoot -Purpose Editor
        Assert ($lease.Purpose -eq 'Editor') 'Known Blender authoring wrongly blocked Editor admission.'
        Exit-WorkshopGate $lease; $lease = $null
    } finally { Remove-Item -LiteralPath Function:Get-CimInstance }
    $source = Join-Path $testRoot 'new.apk'; $latest = Join-Path $testRoot 'latest.apk'
    [IO.File]::WriteAllText($source,'new valid package'); [IO.File]::WriteAllText($latest,'old valid package')
    Assert-Rejected { Publish-WorkshopFile -Source $source -Destination $latest -ExpectedSha256 ('0' * 64) -ExpectedBytes 17 } 'validation'
    Assert ([IO.File]::ReadAllText($latest) -eq 'old valid package') 'Failed publication replaced the previous artifact.'
    $hash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
    Publish-WorkshopFile -Source $source -Destination $latest -ExpectedSha256 $hash -ExpectedBytes (Get-Item $source).Length
    Assert ([IO.File]::ReadAllText($latest) -eq 'new valid package') 'Validated artifact was not published.'
    Assert (@(Get-ChildItem $testRoot -Filter '*.part-*').Count -eq 0) 'Publication temporary files survived.'
    $owned = Start-SyntheticProcess
    try {
        Assert-Rejected { Wait-WorkshopProcess -Process $owned -TimeoutSeconds 1 -PollMilliseconds 25 -AvailableMemoryKiB { 4MB } } 'timed out'
        Stop-WorkshopOwnedProcess -Process $owned -Started $true
        Assert $owned.HasExited 'Timed-out synthetic process was left alive.'
    } finally { Stop-WorkshopOwnedProcess -Process $owned -Started $true; $owned.Dispose() }
    $owned = Start-SyntheticProcess
    try {
        Stop-WorkshopOwnedProcess -Process $owned -Started $false
        Assert (-not $owned.HasExited) 'Cleanup killed a process without an ownership flag.'
        Assert-Rejected { Wait-WorkshopProcess -Process $owned -TimeoutSeconds 10 -PollMilliseconds 25 -AvailableMemoryKiB { 0 } } 'below 1 GiB'
        Stop-WorkshopOwnedProcess -Process $owned -Started $true
        Assert $owned.HasExited 'Low-memory synthetic process was left alive.'
    } finally { Stop-WorkshopOwnedProcess -Process $owned -Started $true; $owned.Dispose() }
    $owned = Start-SyntheticProcess
    try {
        Assert-Rejected { Wait-WorkshopProcess -Process $owned -TimeoutSeconds 10 -PollMilliseconds 25 -AvailableMemoryKiB { throw 'injected monitor failure' } } 'injected monitor failure'
        Stop-WorkshopOwnedProcess -Process $owned -Started $true
        Assert $owned.HasExited 'Monitor-failure synthetic process was left alive.'
    } finally { Stop-WorkshopOwnedProcess -Process $owned -Started $true; $owned.Dispose() }
    Write-Output "PASS: $script:passed synthetic helper assertions. No Unity/Blender/Gradle/ADB actions."
} finally {
    if ($lease) { Exit-WorkshopGate $lease }
    $resolved = [IO.Path]::GetFullPath($testRoot)
    $tempPrefix = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($tempPrefix,[StringComparison]::OrdinalIgnoreCase) -or (Split-Path -Leaf $resolved) -notlike 'workshop-helpers-*') { throw 'Refusing cleanup outside synthetic helper test directory.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
