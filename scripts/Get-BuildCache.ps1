# A short, checkout-owned path avoids Windows Ninja's 260-character limit.
#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
$focusRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$focusKey = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($focusRoot.ToLowerInvariant()))).Substring(0,12).ToLowerInvariant()
$focusCache = Join-Path $env:USERPROFILE ('.fbc\' + $focusKey)
$focusOwner = Join-Path $focusCache 'project.txt'
if (Test-Path -LiteralPath $focusCache) {
    if (-not (Test-Path -LiteralPath $focusOwner) -or (Get-Content -LiteralPath $focusOwner -Raw).Trim() -ne $focusRoot) {
        throw "Build cache ownership check failed: $focusCache"
    }
} else {
    New-Item -ItemType Directory -Path $focusCache -Force | Out-Null
    [IO.File]::WriteAllText($focusOwner, $focusRoot)
}
New-Item -ItemType Directory -Path (Join-Path $focusCache 'gradle') -Force | Out-Null
Write-Output $focusCache
