#Requires -Version 7.0
<# Read-only device evidence. Does not install, launch, clear logcat or change app data.
   Run immediately after the human checklist; recent buffered app logs are not an event trace. #>
[CmdletBinding()]
param(
    [string]$Serial,
    [string]$ApkPath,
    [string]$PackagedSourceRevision,
    [string]$OutputDirectory,
    [string]$UnityPath = 'C:\Users\mrbos\Documents\UnityEditors\6000.3.25f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$capturePackage = 'com.thegamingdonkey.mrworkshop'
$captureAdb = Join-Path (Split-Path -Parent $UnityPath) 'Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
if (-not (Test-Path -LiteralPath $captureAdb)) { throw 'The pinned Unity Android SDK has no adb.exe.' }
if ($Serial -and $Serial -notmatch '^[A-Za-z0-9._:-]+$') { throw 'Invalid device serial.' }
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) ('.artifacts/workshop/device-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,8))
}
# Each capture is unique; never overwrite an operator's observations or earlier evidence.
if (Test-Path -LiteralPath $OutputDirectory) { throw 'OutputDirectory already exists. Choose a new capture directory.' }
$null = New-Item -ItemType Directory -Path $OutputDirectory
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path
$captureRecord = [ordered]@{
    SchemaVersion=1; CapturedAtUtc=[DateTime]::UtcNow.ToString('o'); Status='Not captured'
    Package=$capturePackage; AdbPort=54483; HeadsetVerified=$false
    Device=$null; InstalledApp=$null; SuppliedArtifact=$null; Telemetry=[ordered]@{}
    Limitations=@('Human observations are separate and initially pending.', 'Logs are recent buffered app lines, not a timed or complete interaction trace.', 'Memory usage and Android window statistics do not prove XR frame time.')
}
$captureChecklist = @('passthrough-readability','left-hand-grab-release','right-hand-grab-release','left-hand-poke-ray-buttons','right-hand-poke-ray-buttons','left-controller-grab-buttons','right-controller-grab-buttons','source-loss-no-dock','stale-button-press','recenter-held-and-pending-release','pause-resume','comfort-and-performance') | ForEach-Object {
    [ordered]@{Id=$_; Status='not_observed'; ObservedAtUtc=$null; Operator=''; Observation=''; EvidenceReference=''}
}
[ordered]@{SchemaVersion=1; EvidenceKind='human_observation'; InstalledVersionCode=''; DeviceSerial=''; SessionStartUtc=''; SessionEndUtc=''; Cases=@($captureChecklist)} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'human-observations.json') -Encoding utf8

function Invoke-CaptureAdb([string[]]$Arguments,[int]$TimeoutSeconds=20) {
    $captureStart = [Diagnostics.ProcessStartInfo]::new()
    $captureStart.FileName=$captureAdb; $captureStart.UseShellExecute=$false; $captureStart.CreateNoWindow=$true
    $captureStart.RedirectStandardOutput=$true; $captureStart.RedirectStandardError=$true
    $captureStart.Environment['ADB_SERVER_SOCKET']='tcp:127.0.0.1:54483'
    $captureStart.Environment['ANDROID_ADB_SERVER_PORT']='54483'
    foreach ($captureArgument in (@('-P','54483') + $Arguments)) { $captureStart.ArgumentList.Add($captureArgument) }
    $captureProcess=[Diagnostics.Process]::Start($captureStart)
    try {
        $captureOut=$captureProcess.StandardOutput.ReadToEndAsync(); $captureErr=$captureProcess.StandardError.ReadToEndAsync()
        if (-not $captureProcess.WaitForExit($TimeoutSeconds * 1000)) {
            $captureProcess.Kill($true); $captureProcess.WaitForExit(); throw "ADB command timed out after $TimeoutSeconds seconds."
        }
        $captureText=$captureOut.GetAwaiter().GetResult(); $captureError=$captureErr.GetAwaiter().GetResult()
        if ($captureProcess.ExitCode -ne 0) { throw "ADB command failed: $captureError $captureText" }
        return $captureText.Trim()
    } finally { $captureProcess.Dispose() }
}
function Get-CaptureAdbListener {
    $captureListeners=@(Get-NetTCPConnection -LocalPort 54483 -State Listen -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique)
    if ($captureListeners.Count -gt 1) { throw 'Private ADB port has multiple owners; no process was stopped.' }
    if ($captureListeners.Count -eq 0) { return $null }
    $captureOwner=Get-CimInstance Win32_Process -Filter "ProcessId = $($captureListeners[0])"
    if (-not $captureOwner -or -not [string]::Equals($captureOwner.ExecutablePath,$captureAdb,[StringComparison]::OrdinalIgnoreCase)) { throw 'Private ADB port belongs to another executable; no process was stopped.' }
    return $captureOwner.ProcessId
}
function Save-CaptureProbe([string]$Name,[string[]]$Arguments) {
    try {
        $captureText=Invoke-CaptureAdb ($captureTarget + $Arguments)
        $captureText | Set-Content -LiteralPath (Join-Path $OutputDirectory ($Name + '.txt')) -Encoding utf8
        $captureRecord.Telemetry[$Name]=[ordered]@{Status='captured'; File=($Name + '.txt')}
        return $captureText
    } catch {
        $captureRecord.Telemetry[$Name]=[ordered]@{Status='unavailable'; Reason=$_.Exception.Message}
        return $null
    }
}
$captureServerBefore=$null; $captureOwnedServer=$null
try {
    if ($ApkPath) {
        $captureArtifact=(Resolve-Path -LiteralPath $ApkPath).Path
        $captureRecord.SuppliedArtifact=[ordered]@{Path=$captureArtifact; Bytes=(Get-Item -LiteralPath $captureArtifact).Length; SHA256=(Get-FileHash -LiteralPath $captureArtifact -Algorithm SHA256).Hash; PackagedSourceRevision=$PackagedSourceRevision; InstalledByteIdentityVerified=$false}
        $captureRecord.Limitations += 'Supplied APK hash identifies the local file; this capture does not prove the installed bytes match it. Use the package inspection/build record for packaged source provenance.'
    }
    $captureServerBefore=Get-CaptureAdbListener
    if (-not $captureServerBefore) {
        Invoke-CaptureAdb @('start-server') | Out-Null
        $captureOwnedServer=Get-CaptureAdbListener
        if (-not $captureOwnedServer) { throw 'Private ADB server did not start.' }
    }
    $captureDevicesText=Invoke-CaptureAdb @('devices','-l')
    $captureDevices=@([regex]::Matches($captureDevicesText,'(?m)^([^\s]+)\s+(device|unauthorized|offline)\b[^\r\n]*') | ForEach-Object { [pscustomobject]@{Serial=$_.Groups[1].Value; State=$_.Groups[2].Value} })
    if ($Serial) { $captureDevices=@($captureDevices | Where-Object Serial -eq $Serial) }
    if ($captureDevices.Count -eq 0) { $captureRecord.Status='No connected headset'; return }
    if ($captureDevices.Count -ne 1) { $captureRecord.Status='Selection required'; $captureRecord.Limitations += 'Several devices are connected. Rerun with -Serial; no device telemetry was read.'; return }
    $captureDevice=$captureDevices[0]
    $captureRecord.Device=[ordered]@{Serial=$captureDevice.Serial; State=$captureDevice.State}
    if ($captureDevice.State -ne 'device') { $captureRecord.Status='Device authorization or connection pending'; return }
    $captureTarget=@('-s',$captureDevice.Serial)
    $captureModel=Invoke-CaptureAdb ($captureTarget + @('shell','getprop','ro.product.model'))
    $captureRecord.Device.Model=$captureModel
    if ($captureModel -notmatch '(?i)quest\s*3s') { $captureRecord.Status='Different device model'; return }
    $captureRecord.Device.Api=Invoke-CaptureAdb ($captureTarget + @('shell','getprop','ro.build.version.sdk'))
    $captureRecord.Device.Build=Invoke-CaptureAdb ($captureTarget + @('shell','getprop','ro.build.display.id'))
    $captureRecord.Device.DeviceTime=Invoke-CaptureAdb ($captureTarget + @('shell','date','+%Y-%m-%dT%H:%M:%S%z'))
    $capturePackageText=Save-CaptureProbe 'installed-package' @('shell','dumpsys','package',$capturePackage)
    $captureVersion=[regex]::Match([string]$capturePackageText,'\bversionCode=(\d+)')
    if (-not $captureVersion.Success) { $captureRecord.Status='Workshop installation not confirmed'; return }
    $captureVersionName=[regex]::Match([string]$capturePackageText,'(?m)^\s*versionName=(.*)$').Groups[1].Value.Trim()
    $captureRecord.InstalledApp=[ordered]@{Package=$capturePackage; VersionCode=$captureVersion.Groups[1].Value; VersionName=$captureVersionName}
    $capturePidText=Save-CaptureProbe 'app-process' @('shell','pidof',$capturePackage)
    $capturePids=@([regex]::Matches([string]$capturePidText,'\b\d+\b') | ForEach-Object Value)
    if ($capturePids.Count -ne 1) { $captureRecord.Status='Workshop process not uniquely running'; return }
    $captureRecord.InstalledApp.ProcessId=$capturePids[0]
    $captureLog=Save-CaptureProbe 'app-logcat' @('logcat','-d','-t','1000',('--pid=' + $capturePids[0]),'-v','threadtime')
    if ($null -ne $captureLog) {
        $captureErrors=@($captureLog -split '\r?\n' | Where-Object { $_ -match '\s[EF]\s|Exception|FATAL|SIGSEGV|SIGABRT|ANR in' })
        $captureErrors | Set-Content -LiteralPath (Join-Path $OutputDirectory 'app-error-candidates.txt') -Encoding utf8
        $captureRecord.Telemetry['app-error-candidates']=[ordered]@{Status='captured'; File='app-error-candidates.txt'; CandidateLines=$captureErrors.Count; Interpretation='Review candidates manually. Empty output does not prove error-free interaction; process restarts/crashes may be absent from this PID buffer.'}
    }
    Save-CaptureProbe 'app-memory' @('shell','dumpsys','meminfo',$capturePackage) | Out-Null
    Save-CaptureProbe 'android-window-statistics' @('shell','dumpsys','gfxinfo',$capturePackage) | Out-Null
    $captureRecord.Telemetry['xr-frame-time']=[ordered]@{Status='not_measured'; Reason='No XR CPU/GPU frame-time provider is enabled by this read-only capture. gfxinfo reports Android window rendering and may be empty for Unity XR.'}
    $captureRecord.Status='Device telemetry captured; human acceptance pending'
} catch {
    $captureRecord.Status='Capture incomplete'; $captureRecord.Error=$_.Exception.Message
    throw
} finally {
    try {
        # Release only the private server started here; never stop a borrowed server or port 5037.
        if ($captureOwnedServer -and (Get-CaptureAdbListener) -eq $captureOwnedServer) { Invoke-CaptureAdb @('kill-server') | Out-Null }
    } finally {
        $captureRecord | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'telemetry.json') -Encoding utf8
        [pscustomobject]@{Status=$captureRecord.Status; Directory=$OutputDirectory; HeadsetVerified=$false} | ConvertTo-Json -Compress
    }
}
