#Requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet('Check','InstallAndLaunch')][string]$Action = 'Check',
    [string]$Serial,
    [string]$ApkPath,
    [string]$UnityPath = 'C:\Users\mrbos\Documents\UnityEditors\6000.3.25f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$workshopAdb = Join-Path (Split-Path -Parent $UnityPath) 'Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
if (-not (Test-Path -LiteralPath $workshopAdb)) { throw 'The pinned Unity Android SDK has no adb.exe.' }
if ($Serial -and $Serial -notmatch '^[A-Za-z0-9._:-]+$') { throw 'Invalid device serial.' }
if ($Action -eq 'InstallAndLaunch') {
    if (-not $ApkPath) { throw 'InstallAndLaunch requires the actual Workshop APK path.' }
    $workshopVerifiedApk = & (Join-Path $PSScriptRoot 'Verify-Apk.ps1') -ApkPath $ApkPath -UnityPath $UnityPath | ConvertFrom-Json
    $ApkPath = $workshopVerifiedApk.APK
}
function Invoke-WorkshopAdb([string[]]$Arguments,[int]$TimeoutSeconds = 30) {
    $workshopStart = [Diagnostics.ProcessStartInfo]::new()
    $workshopStart.FileName = $workshopAdb
    $workshopStart.UseShellExecute = $false
    $workshopStart.CreateNoWindow = $true
    $workshopStart.RedirectStandardOutput = $true
    $workshopStart.RedirectStandardError = $true
    $workshopStart.Environment['ADB_SERVER_SOCKET'] = 'tcp:127.0.0.1:54483'
    $workshopStart.Environment['ANDROID_ADB_SERVER_PORT'] = '54483'
    foreach ($workshopArgument in (@('-P','54483') + $Arguments)) { $workshopStart.ArgumentList.Add($workshopArgument) }
    $workshopProcess = [Diagnostics.Process]::Start($workshopStart)
    try {
        $workshopOut = $workshopProcess.StandardOutput.ReadToEndAsync()
        $workshopErr = $workshopProcess.StandardError.ReadToEndAsync()
        if (-not $workshopProcess.WaitForExit($TimeoutSeconds * 1000)) {
            $workshopProcess.Kill($true)
            $workshopProcess.WaitForExit()
            throw "ADB command timed out after $TimeoutSeconds seconds."
        }
        $workshopOutput = $workshopOut.GetAwaiter().GetResult()
        $workshopError = $workshopErr.GetAwaiter().GetResult()
        if ($workshopProcess.ExitCode -ne 0) { throw "ADB command failed: $workshopError $workshopOutput" }
        return $workshopOutput.Trim()
    } finally { $workshopProcess.Dispose() }
}
function Get-WorkshopAdbListener {
    $workshopListeners = @(Get-NetTCPConnection -LocalPort 54483 -State Listen -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique)
    if ($workshopListeners.Count -gt 1) { throw 'Private ADB port has multiple owners; no process was stopped.' }
    if ($workshopListeners.Count -eq 0) { return $null }
    $workshopOwner = Get-CimInstance Win32_Process -Filter "ProcessId = $($workshopListeners[0])"
    if (-not $workshopOwner -or -not [string]::Equals($workshopOwner.ExecutablePath,$workshopAdb,[StringComparison]::OrdinalIgnoreCase)) {
        throw 'Private ADB port belongs to another executable; no process was stopped.'
    }
    return $workshopOwner.ProcessId
}
$workshopServerBefore = Get-WorkshopAdbListener
$workshopOwnedServer = $null
try {
    if (-not $workshopServerBefore) {
        Invoke-WorkshopAdb @('start-server') | Out-Null
        $workshopOwnedServer = Get-WorkshopAdbListener
        if (-not $workshopOwnedServer) { throw 'Private ADB server did not start.' }
    }
    $workshopDeviceText = Invoke-WorkshopAdb @('devices','-l')
    $workshopDevices = @([regex]::Matches($workshopDeviceText,'(?m)^([^\s]+)\s+(device|unauthorized|offline)\b[^\r\n]*') | ForEach-Object {
        [pscustomobject]@{Serial=$_.Groups[1].Value; State=$_.Groups[2].Value}
    })
    if ($Serial) { $workshopDevices = @($workshopDevices | Where-Object Serial -eq $Serial) }
    if ($workshopDevices.Count -eq 0) {
        if ($Action -eq 'InstallAndLaunch' -or $Serial) { throw 'Requested headset is not connected to ADB.' }
        [pscustomobject]@{Status='No connected headset'; AdbPort=54483; Installed=$false; Launched=$false; HeadsetVerified=$false} | ConvertTo-Json -Compress
        return
    }
    if ($workshopDevices.Count -ne 1) { throw 'Multiple devices are connected. Specify the Quest 3S -Serial; no app was installed.' }
    $workshopDevice = $workshopDevices[0]
    if ($workshopDevice.State -ne 'device') { throw "Headset state is $($workshopDevice.State). Accept USB debugging inside the headset, then run again." }
    $workshopTarget = @('-s',$workshopDevice.Serial)
    $workshopModel = Invoke-WorkshopAdb ($workshopTarget + @('shell','getprop','ro.product.model'))
    if ($workshopModel -notmatch '(?i)quest\s*3s') { throw "Connected model '$workshopModel' is not this project's Quest 3S target; no app was installed." }
    $workshopSdkText = Invoke-WorkshopAdb ($workshopTarget + @('shell','getprop','ro.build.version.sdk'))
    $workshopSdk = 0
    if (-not [int]::TryParse($workshopSdkText,[ref]$workshopSdk) -or $workshopSdk -lt 32) { throw "Headset API '$workshopSdkText' is below the app's minimum 32." }
    $workshopInstalled = $false
    $workshopLaunched = $false
    if ($Action -eq 'InstallAndLaunch') {
        $workshopInstallOutput = Invoke-WorkshopAdb ($workshopTarget + @('install','-r',$ApkPath)) 55
        if ($workshopInstallOutput -notmatch '(?m)^Success\s*$') { throw "Installation was not confirmed: $workshopInstallOutput" }
        $workshopInstalled = $true
        $workshopLaunchOutput = Invoke-WorkshopAdb ($workshopTarget + @('shell','am','start','-W','-n','com.thegamingdonkey.mrworkshop/com.unity3d.player.UnityPlayerGameActivity'))
        if ($workshopLaunchOutput -notmatch '(?m)^Status:\s*ok\s*$' -or $workshopLaunchOutput -match '(?im)^Error:') { throw "Launch was not confirmed: $workshopLaunchOutput" }
        $workshopLaunched = $true
    }
    [pscustomobject]@{Status='Authorized Quest 3S'; Model=$workshopModel; Api=$workshopSdk; Serial=$workshopDevice.Serial; AdbPort=54483; Installed=$workshopInstalled; Launched=$workshopLaunched; HeadsetVerified=$false} | ConvertTo-Json -Compress
} finally {
    # Leave borrowed servers alone. Never touch other chats' default port 5037.
    if ($workshopOwnedServer -and (Get-WorkshopAdbListener) -eq $workshopOwnedServer) {
        Invoke-WorkshopAdb @('kill-server') | Out-Null
    }
}
