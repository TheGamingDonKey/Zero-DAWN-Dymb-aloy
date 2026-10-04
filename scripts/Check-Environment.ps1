[CmdletBinding()]
param([string]$UnityPath = 'C:\Users\mrbos\Documents\UnityEditors\6000.3.25f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
$focusEditorDir = Split-Path $UnityPath
$focusAndroidDir = Join-Path $focusEditorDir 'Data\PlaybackEngines\AndroidPlayer'
$focusRequired = [ordered]@{
    UnityEditor = $UnityPath
    AndroidADB = Join-Path $focusAndroidDir 'SDK\platform-tools\adb.exe'
    AndroidNDK = Join-Path $focusAndroidDir 'NDK\source.properties'
    OpenJDK = Join-Path $focusAndroidDir 'OpenJDK\bin\java.exe'
    MetaCLI = Join-Path $env:USERPROFILE '.metavr\bin\metavr.exe'
}
$focusMissing = @()
foreach ($focusItem in $focusRequired.GetEnumerator()) {
    $focusPresent = Test-Path -LiteralPath $focusItem.Value
    [PSCustomObject]@{Tool=$focusItem.Key; Installed=$focusPresent; Path=$focusItem.Value} | ConvertTo-Json -Compress
    if (-not $focusPresent) { $focusMissing += $focusItem.Key }
}
Write-Output 'File checks do not validate a Unity license, project import, APK build, or headset connection.'
if ($focusMissing.Count -gt 0) { exit 1 }
exit 0
