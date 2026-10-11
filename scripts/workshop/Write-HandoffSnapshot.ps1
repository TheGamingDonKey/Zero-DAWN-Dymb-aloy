#Requires -Version 7.0
[CmdletBinding()]
param()

# Read only this checkout's Git metadata and selected tracked project files.
# The only write is a new ignored handoff JSON; no app/export/device is touched.
$ErrorActionPreference = 'Stop'
$handoffRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$handoffStarted = [DateTime]::UtcNow.ToString('o')

function Get-HandoffGitText([string[]]$GitArguments) {
    $gitOutput = @(& git -C $handoffRoot @GitArguments)
    if ($LASTEXITCODE -ne 0) { throw "Git handoff read failed: $($GitArguments[0])" }
    return [string]::Join("`n", $gitOutput)
}

$handoffHead = Get-HandoffGitText @('rev-parse','HEAD')
$handoffBranch = Get-HandoffGitText @('branch','--show-current')
$handoffRemote = Get-HandoffGitText @('remote','get-url','origin')
# Never retain URL credentials, query tokens or fragments if origin is changed.
if ($handoffRemote -match '^https?://') {
    $handoffUri = [UriBuilder]::new($handoffRemote)
    $handoffUri.UserName = ''; $handoffUri.Password = ''
    $handoffUri.Query = ''; $handoffUri.Fragment = ''
    $handoffRemote = $handoffUri.Uri.AbsoluteUri
}

$handoffStatusRecords = (Get-HandoffGitText @('status','--porcelain=v1','-z','--untracked-files=all')) -split [char]0
$handoffChangedPaths = [Collections.Generic.List[string]]::new()
for ($handoffIndex = 0; $handoffIndex -lt $handoffStatusRecords.Count; $handoffIndex++) {
    $handoffRecord = $handoffStatusRecords[$handoffIndex]
    if (-not $handoffRecord) { continue }
    if ($handoffRecord.Length -lt 4) { throw 'Unexpected Git status record.' }
    $handoffChangedPaths.Add($handoffRecord.Substring(3))
    # In -z format, a rename/copy has a second, unprefixed original-path record.
    if ($handoffRecord.Substring(0,2) -match '[RC]') {
        $handoffIndex++
        if ($handoffIndex -ge $handoffStatusRecords.Count) { throw 'Incomplete Git rename record.' }
        $handoffChangedPaths.Add($handoffStatusRecords[$handoffIndex])
    }
}

$handoffFiles = [Collections.Generic.List[object]]::new()
$handoffTracked = (Get-HandoffGitText @('ls-files','-z','--','FocusCore','MRWorkshop')) -split [char]0
foreach ($handoffPath in ($handoffTracked | Where-Object { $_ } | Sort-Object)) {
    $handoffCategory = $null
    if ($handoffPath -match '^(FocusCore|MRWorkshop)/Assets/.+\.(cs|asmdef)$' -and
        $handoffPath -notmatch '/(Editor|Tests|EditorSetup)/') { $handoffCategory = 'runtime_source' }
    elseif ($handoffPath -match '^(FocusCore|MRWorkshop)/Assets/.+\.(shader|shaders|shadergraph)$') { $handoffCategory = 'shader' }
    elseif ($handoffPath -match '^(FocusCore|MRWorkshop)/Assets/.+\.unity$') { $handoffCategory = 'scene' }
    elseif ($handoffPath -match '^(FocusCore|MRWorkshop)/(Packages/manifest\.json|Assets/Plugins/Android/AndroidManifest\.xml)$') { $handoffCategory = 'manifest' }
    if (-not $handoffCategory) { continue }
    $handoffAbsolute = [IO.Path]::GetFullPath((Join-Path $handoffRoot $handoffPath))
    if (-not $handoffAbsolute.StartsWith($handoffRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Tracked hash path escaped checkout.'
    }
    $handoffFileRecord = [ordered]@{Path=$handoffPath; Project=($handoffPath -split '/')[0]; Category=$handoffCategory; State='missing'; Bytes=$null; SHA256=$null}
    if (Test-Path -LiteralPath $handoffAbsolute -PathType Leaf) {
        $handoffItem = Get-Item -LiteralPath $handoffAbsolute
        if ($handoffItem.Attributes -band [IO.FileAttributes]::ReparsePoint) {
            $handoffFileRecord.State = 'reparse_point_not_read'
        } else {
            $handoffFileRecord.Bytes = $handoffItem.Length
            $handoffFileRecord.SHA256 = (Get-FileHash -LiteralPath $handoffAbsolute -Algorithm SHA256).Hash
            $handoffFileRecord.State = 'hashed_current_file'
        }
    }
    $handoffFiles.Add([pscustomobject]$handoffFileRecord)
}

$handoffHeadAfter = Get-HandoffGitText @('rev-parse','HEAD')
$handoffBranchAfter = Get-HandoffGitText @('branch','--show-current')
$handoffSnapshot = [ordered]@{
    SchemaVersion=1; EvidenceKind='current_checkout_snapshot'; StartedUtc=$handoffStarted; FinishedUtc=[DateTime]::UtcNow.ToString('o')
    Checkout=$handoffRoot; Head=$handoffHead; Branch=$handoffBranch; Origin=$handoffRemote
    HeadAfterCapture=$handoffHeadAfter; BranchAfterCapture=$handoffBranchAfter
    HeadAndBranchStable=($handoffHead -eq $handoffHeadAfter -and $handoffBranch -eq $handoffBranchAfter)
    CaptureAtomic=$false; GitStatusPaths=@($handoffChangedPaths | Sort-Object -Unique)
    Files=@($handoffFiles)
    Scope='Current tracked runtime C#/asmdef, shaders, all saved scenes and package/Android manifests in FocusCore and MRWorkshop; Editor/tests, untracked files, settings, materials, audio, models, package locks and imported caches are not hashed.'
    Limitations=@('Sequential read; concurrent file edits are possible even when HEAD is stable.', 'No export, APK, installed-byte or physical-device equivalence is established.', 'No build, test, network, application control or credential-file read is performed.', 'Confirm integration/publication from current Git log/status; this snapshot alone does not establish either.')
}
$handoffDirectory = Join-Path $handoffRoot '.artifacts/workshop'
New-Item -ItemType Directory -Path $handoffDirectory -Force | Out-Null
$handoffOutput = Join-Path $handoffDirectory ('handoff-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N') + '.json')
$handoffBytes = [Text.UTF8Encoding]::new($false).GetBytes(($handoffSnapshot | ConvertTo-Json -Depth 8))
$handoffStream = [IO.File]::Open($handoffOutput,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
try { $handoffStream.Write($handoffBytes,0,$handoffBytes.Length) } finally { $handoffStream.Dispose() }
[ordered]@{Snapshot=$handoffOutput; Head=$handoffHead; Branch=$handoffBranch; HashedFiles=@($handoffFiles | Where-Object State -eq 'hashed_current_file').Count; ChangedPaths=$handoffChangedPaths.Count; ExportApkOrInstalledEquivalence=$false} | ConvertTo-Json -Compress
