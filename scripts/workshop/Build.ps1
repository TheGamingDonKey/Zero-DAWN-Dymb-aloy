#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$UnityPath = 'C:\Users\mrbos\Documents\UnityEditors\6000.3.25f1\Editor\Unity.exe',
    [ValidateSet('Workshop')][string]$Scene = 'Workshop',
    [string]$ResumeExport,
    [ValidateRange(1,2)][int]$Workers = 2
)
$ErrorActionPreference = 'Stop'
$focusRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$focusActiveBuildPath = Join-Path $focusRoot '.artifacts\workshop\active-build.json'
if (Test-Path -LiteralPath $focusActiveBuildPath) {
    $focusRecordedBuild = Get-Content -LiteralPath $focusActiveBuildPath -Raw | ConvertFrom-Json
    $focusRunningBuild = Get-Process -Id $focusRecordedBuild.Pid -ErrorAction SilentlyContinue
    if ($focusRunningBuild -and $focusRunningBuild.StartTime.ToUniversalTime().Ticks -eq $focusRecordedBuild.StartTicks -and [string]::Equals($focusRunningBuild.Path,$focusRecordedBuild.Executable,[StringComparison]::OrdinalIgnoreCase)) {
        throw 'A Workshop Android packager is already running. No second build was started.'
    }
}
if (@(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { -not $_.ExecutablePath -or $_.ExecutablePath -match '\\Editor\\Unity\.exe$' }).Count) { throw 'Close the Editor before Android packaging, including resumed exports. No Editor was stopped.' }
$focusFreeKiB = (Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory
if ($focusFreeKiB -lt 3MB) { throw 'Less than 3 GiB RAM is available. Defer this Android build; no other app was closed.' }
$focusRunId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,8)
$focusOutputDir = Join-Path $focusRoot ('.artifacts\apks\' + $Scene + '-' + $focusRunId)
New-Item -ItemType Directory -Path $focusOutputDir -Force | Out-Null
$focusOutput = Join-Path $focusOutputDir ('MR' + $Scene + '.apk')
$focusCache = & (Join-Path $PSScriptRoot '../Get-BuildCache.ps1')
$focusExport = Join-Path $focusCache ('projects\' + $Scene + '-' + $focusRunId)
if ($ResumeExport) {
    $focusExport = [IO.Path]::GetFullPath($ResumeExport)
    $focusProjects = [IO.Path]::GetFullPath((Join-Path $focusCache 'projects')) + [IO.Path]::DirectorySeparatorChar
    $focusLegacyCache = Join-Path $env:USERPROFILE ('Documents\FocusBuildCache\' + (Split-Path -Leaf $focusCache))
    $focusLegacyProjects = [IO.Path]::GetFullPath((Join-Path $focusLegacyCache 'projects')) + [IO.Path]::DirectorySeparatorChar
    $focusLegacyOwner = Join-Path $focusLegacyCache 'project.txt'
    $focusOwnedLegacy = $focusExport.StartsWith($focusLegacyProjects,[StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $focusLegacyOwner) -and (Get-Content -LiteralPath $focusLegacyOwner -Raw).Trim() -eq $focusRoot
    if ((-not $focusExport.StartsWith($focusProjects,[StringComparison]::OrdinalIgnoreCase) -and -not $focusOwnedLegacy) -or (Split-Path -Leaf $focusExport) -notlike ($Scene + '-*')) { throw 'Resume export must belong to this project and scene in its owned build cache.' }
    $focusPreviousApks = Join-Path $focusExport 'launcher\build\outputs\apk\release'
    if ((Test-Path -LiteralPath $focusPreviousApks) -and @(Get-ChildItem -LiteralPath $focusPreviousApks -Filter '*.apk').Count -gt 0) { throw 'Resume export already contains an APK; refusing stale output.' }
} else {
$focusArgs = @('-quit','-nographics','-buildTarget','Android','-executeMethod',('MRWorkshop.Editor.WorkshopBuild.Build' + $Scene),'-workshopOutput',$focusOutput,'-workshopExportPath',$focusExport)
& (Join-Path $PSScriptRoot 'Run-Unity.ps1') -UnityPath $UnityPath -EditorArguments $focusArgs -LogName ('build-' + $Scene) -Workers $Workers
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
if (-not (Test-Path -LiteralPath (Join-Path $focusExport 'launcher\build.gradle'))) { throw 'Unity exported no launcher Gradle project.' }
# Gradle workers do not bound Unity's native compiler or Ninja jobs.
# Patch only this owned generated export, identically on a resumed invocation.
$focusLibraryGradle = Join-Path $focusExport 'unityLibrary\build.gradle'
$focusNativeText = [IO.File]::ReadAllText($focusLibraryGradle)
$focusCompileMarker = '    commandLineArgs.add("--compile-cpp")'
$focusCmakeMarker = 'arguments "-DANDROID_STL=c++_shared", "-DANDROID_SUPPORT_FLEXIBLE_PAGE_SIZES=ON"'
if ([regex]::Matches($focusNativeText,[regex]::Escape($focusCompileMarker)).Count -ne 1 -or [regex]::Matches($focusNativeText,[regex]::Escape($focusCmakeMarker)).Count -ne 1) {
    throw 'Unity native Gradle template changed; expected one IL2CPP and one CMake insertion point.'
}
$focusNativeText = [regex]::Replace($focusNativeText,'(?m)^    commandLineArgs\.add\("--(?:jobs|bee-jobs)=\d+"\)\r?\n','')
$focusNativeText = [regex]::Replace($focusNativeText,', "-DCMAKE_JOB_POOLS=workshop=\d+", "-DCMAKE_JOB_POOL_COMPILE=workshop", "-DCMAKE_JOB_POOL_LINK=workshop"','')
$focusNativeText = $focusNativeText.Replace($focusCompileMarker,($focusCompileMarker + "`n    commandLineArgs.add(`"--jobs=$Workers`")`n    commandLineArgs.add(`"--bee-jobs=$Workers`")"))
$focusNativeText = $focusNativeText.Replace($focusCmakeMarker,($focusCmakeMarker + ", `"-DCMAKE_JOB_POOLS=workshop=$Workers`", `"-DCMAKE_JOB_POOL_COMPILE=workshop`", `"-DCMAKE_JOB_POOL_LINK=workshop`""))
[IO.File]::WriteAllText($focusLibraryGradle,$focusNativeText,[Text.UTF8Encoding]::new($false))
Write-Output "Native concurrency: IL2CPP jobs=$Workers; Bee jobs=$Workers; CMake compile/link shared pool=$Workers; Gradle workers=$Workers"
if ((Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory -lt 3MB) { throw 'Less than 3 GiB RAM available for packaging. Native export retained; no other app was closed.' }
$focusAndroid = Join-Path (Split-Path -Parent $UnityPath) 'Data\PlaybackEngines\AndroidPlayer'
$focusJava = Join-Path $focusAndroid 'OpenJDK\bin\java.exe'
$focusGradleJars = @(Get-ChildItem -LiteralPath (Join-Path $focusAndroid 'Tools\gradle\lib') -Filter 'gradle-launcher-*.jar')
if ($focusGradleJars.Count -ne 1) { throw 'Expected one Unity-managed Gradle launcher.' }
$focusStart = [Diagnostics.ProcessStartInfo]::new()
$focusStart.FileName = $focusJava
$focusStart.WorkingDirectory = $focusExport
$focusStart.UseShellExecute = $false
$focusStart.CreateNoWindow = $true
$focusStart.RedirectStandardOutput = $true
$focusStart.RedirectStandardError = $true
$focusStart.Environment['JAVA_HOME'] = Join-Path $focusAndroid 'OpenJDK'
$focusStart.Environment['BEE_BUILD_THREADS'] = [string]$Workers
$focusStart.Environment['DOTNET_PROCESSOR_COUNT'] = [string]$Workers
$focusStart.Environment['CMAKE_BUILD_PARALLEL_LEVEL'] = [string]$Workers
$focusStart.Environment['GRADLE_USER_HOME'] = Join-Path $focusCache 'gradle'
$focusStart.Environment['ADB_SERVER_SOCKET'] = 'tcp:127.0.0.1:54483'
$focusStart.Environment['ANDROID_ADB_SERVER_PORT'] = '54483'
foreach ($focusArg in @('-Xmx256m','-classpath',$focusGradleJars[0].FullName,'org.gradle.launcher.GradleMain','-Dorg.gradle.jvmargs=-Xmx2048m -XX:MaxMetaspaceSize=512m','-Dorg.gradle.parallel=false',"--max-workers=$Workers",'--no-daemon',':launcher:assembleRelease')) {
    $focusStart.ArgumentList.Add($focusArg)
}
$focusStdoutPath = Join-Path $focusRoot ('.artifacts\logs\gradle-' + $Scene + '-' + $focusRunId + '.stdout.log')
$focusStderrPath = Join-Path $focusRoot ('.artifacts\logs\gradle-' + $Scene + '-' + $focusRunId + '.stderr.log')
$focusStdoutFile = [IO.FileStream]::new($focusStdoutPath,[IO.FileMode]::Create,[IO.FileAccess]::Write,[IO.FileShare]::ReadWrite)
$focusStderrFile = [IO.FileStream]::new($focusStderrPath,[IO.FileMode]::Create,[IO.FileAccess]::Write,[IO.FileShare]::ReadWrite)
$focusGradle = [Diagnostics.Process]::new()
$focusGradle.StartInfo = $focusStart
$focusGradleStarted = $false
try {
    if (-not $focusGradle.Start()) { throw 'Could not start Gradle.' }
    $focusGradleStarted = $true
    $focusActiveBuildPath = Join-Path $focusRoot '.artifacts\workshop\active-build.json'
    New-Item -ItemType Directory -Path (Split-Path -Parent $focusActiveBuildPath) -Force | Out-Null
    [pscustomobject]@{Pid=$focusGradle.Id; StartTicks=$focusGradle.StartTime.ToUniversalTime().Ticks; Executable=$focusJava; Export=$focusExport} | ConvertTo-Json | Set-Content -LiteralPath $focusActiveBuildPath -Encoding utf8
    try { $focusGradle.PriorityClass = [Diagnostics.ProcessPriorityClass]::BelowNormal } catch { Write-Warning 'Could not lower this Gradle process priority.' }
    $focusOutCopy = $focusGradle.StandardOutput.BaseStream.CopyToAsync($focusStdoutFile)
    $focusErrCopy = $focusGradle.StandardError.BaseStream.CopyToAsync($focusStderrFile)
    Write-Output "Gradle PID: $($focusGradle.Id); stdout: $focusStdoutPath; stderr: $focusStderrPath"
    $focusLowMemorySamples = 0
    while (-not $focusGradle.WaitForExit(10000)) {
        if ((Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory -lt 1MB) { $focusLowMemorySamples++ } else { $focusLowMemorySamples = 0 }
        if ($focusLowMemorySamples -ge 2) {
            Write-Warning 'Available RAM stayed below1GiB; stopping only this owned Gradle process tree. No APK success claimed.'
            $focusGradle.Kill($true)
            $focusGradle.WaitForExit()
            break
        }
    }
    $focusOutCopy.GetAwaiter().GetResult() | Out-Null
    $focusErrCopy.GetAwaiter().GetResult() | Out-Null
    $focusGradleExit = $focusGradle.ExitCode
} finally {
    if ($focusGradleStarted -and -not $focusGradle.HasExited) { $focusGradle.Kill($true); $focusGradle.WaitForExit() }
    if ($focusActiveBuildPath -and (Test-Path -LiteralPath $focusActiveBuildPath)) {
        $focusRecordedBuild = Get-Content -LiteralPath $focusActiveBuildPath -Raw | ConvertFrom-Json
        if ($focusRecordedBuild.Pid -eq $focusGradle.Id) { Remove-Item -LiteralPath $focusActiveBuildPath }
    }
    $focusStdoutFile.Dispose()
    $focusStderrFile.Dispose()
    $focusGradle.Dispose()
}
if ($focusGradleExit -ne 0) { throw "Gradle failed: exit=$focusGradleExit; see $focusStderrPath" }
$focusBuiltApks = @(Get-ChildItem -LiteralPath (Join-Path $focusExport 'launcher\build\outputs\apk\release') -Filter '*.apk')
if ($focusBuiltApks.Count -ne 1 -or $focusBuiltApks[0].Length -le 0) { throw 'This fresh Gradle export produced no single nonempty release APK.' }
Copy-Item -LiteralPath $focusBuiltApks[0].FullName -Destination $focusOutput
if (-not (Test-Path -LiteralPath $focusOutput) -or (Get-Item -LiteralPath $focusOutput).Length -le 0) { throw 'This invocation produced no nonempty APK.' }
$focusHash = (Get-FileHash -LiteralPath $focusOutput -Algorithm SHA256).Hash
# Publish the convenience path only after checking this invocation's unique output.
$focusLatest = Join-Path $focusRoot ('.artifacts\apks\MR' + $Scene + '.apk')
Copy-Item -LiteralPath $focusOutput -Destination $focusLatest -Force
[PSCustomObject]@{APK=$focusOutput; Latest=$focusLatest; Bytes=(Get-Item -LiteralPath $focusOutput).Length; SHA256=$focusHash; HeadsetTested=$false} | ConvertTo-Json -Compress
exit 0
