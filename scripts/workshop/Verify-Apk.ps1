# Verify the actual packaged Workshop artifact. This does not test a connected headset.
#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ApkPath,
    [string]$UnityPath = 'C:\Users\mrbos\Documents\UnityEditors\6000.3.25f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$focusApk = (Resolve-Path -LiteralPath $ApkPath).Path
$focusAndroid = Join-Path (Split-Path -Parent $UnityPath) 'Data\PlaybackEngines\AndroidPlayer'
$focusTools = Join-Path $focusAndroid 'SDK\build-tools\36.0.0'
$focusPreviousJava = $env:JAVA_HOME
try {
    $env:JAVA_HOME = Join-Path $focusAndroid 'OpenJDK'
    $focusSignature = & (Join-Path $focusTools 'apksigner.bat') verify --verbose $focusApk 2>&1
    if ($LASTEXITCODE -ne 0) { throw "APK signature verification failed: $focusSignature" }
    if (($focusSignature -join "`n") -notmatch 'Verified using v2 scheme.*true') { throw 'APK lacks a verified v2 signature.' }
    $focusBadging = & (Join-Path $focusTools 'aapt.exe') dump badging $focusApk
    if ($LASTEXITCODE -ne 0) { throw 'Could not read APK badging.' }
    $focusManifest = & (Join-Path $focusTools 'aapt.exe') dump xmltree $focusApk AndroidManifest.xml
    if ($LASTEXITCODE -ne 0) { throw 'Could not read packaged Android manifest.' }
} finally { $env:JAVA_HOME = $focusPreviousJava }
$focusBadgeText = $focusBadging -join "`n"
$focusManifestText = $focusManifest -join "`n"
foreach ($focusPattern in @("package: name='com.thegamingdonkey.mrworkshop'", "sdkVersion:'32'", "targetSdkVersion:'34'", "native-code: 'arm64-v8a'", "uses-permission: name='com.oculus.permission.HAND_TRACKING'", "uses-feature: name='com.oculus.feature.PASSTHROUGH'")) {
    if (-not $focusBadgeText.Contains($focusPattern)) { throw "Packaged Workshop setting missing: $focusPattern" }
}
if ($focusBadgeText -notmatch "launchable-activity: name='com\.unity3d\.player\.UnityPlayerGameActivity'") { throw 'Packaged Workshop launcher must use UnityPlayerGameActivity.' }
if ($focusBadgeText.Contains("uses-permission: name='android.permission.CAMERA'")) { throw 'Unexpected raw CAMERA permission.' }
if ($focusManifestText -notmatch '(?s)android:name[^\r\n]*="com.oculus.supportedDevices"[^\r\n]*\r?\n\s*A: android:value[^\r\n]*="quest3s"') { throw 'Packaged supported-device metadata does not target Quest 3S.' }
if ($focusManifestText -notmatch '(?s)android:name[^\r\n]*="com.oculus.ossplash.background"[^\r\n]*\r?\n\s*A: android:value[^\r\n]*="passthrough-contextual"') { throw 'Packaged MR loading screen must use contextual passthrough.' }
$focusZip = [IO.Compression.ZipFile]::OpenRead($focusApk)
try {
    $focusLibrary = $focusZip.GetEntry('lib/arm64-v8a/libil2cpp.so')
    if ($null -eq $focusLibrary -or $focusLibrary.Length -lt 100000) { throw 'Packaged ARM64 IL2CPP library is missing or empty.' }
    $focusLibraryBytes = $focusLibrary.Length
    $focusNativeAbis = @($focusZip.Entries | Where-Object { $_.FullName -match '^lib/([^/]+)/.+\.so$' } | ForEach-Object { ($_.FullName -split '/')[1] } | Sort-Object -Unique)
    if ($focusNativeAbis.Count -ne 1 -or $focusNativeAbis[0] -ne 'arm64-v8a') { throw 'Expected ARM64-only native libraries.' }
} finally { $focusZip.Dispose() }
[PSCustomObject]@{
    APK=$focusApk; Bytes=(Get-Item -LiteralPath $focusApk).Length
    SHA256=(Get-FileHash -LiteralPath $focusApk -Algorithm SHA256).Hash
    SignatureV2Verified=$true; Package='com.thegamingdonkey.mrworkshop'
    ABI='arm64-v8a'; Il2cppBytes=$focusLibraryBytes; MinApi=32; TargetApi=34
    QuestDevice='quest3s'; PassthroughRequired=$true; HandPermission=$true
    CameraPermission=$false; HeadsetVerified=$false
} | ConvertTo-Json -Compress
