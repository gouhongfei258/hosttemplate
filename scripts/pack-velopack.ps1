param(
    [Parameter(Mandatory = $true)][string]$Version,
    [Parameter(Mandatory = $true)][string]$PublishDir,
    [Parameter(Mandatory = $true)][string]$OutputDir
)

$ErrorActionPreference = "Stop"

# vpk 的 Windows 包必须在 Windows 上打。Linux 上的 vpk 只会出 AppImage，不能给现场工控机用。
if (-not $IsWindows -and $env:OS -ne "Windows_NT") {
    throw "vpk pack for win-x64 must run on Windows. Use the GitHub Actions release job."
}

$toolDir = Join-Path $PSScriptRoot "..\.tools"
New-Item -ItemType Directory -Force -Path $toolDir | Out-Null
$vpk = Join-Path $toolDir "vpk.exe"
if (-not (Test-Path $vpk)) {
    dotnet tool update vpk --tool-path $toolDir --version 0.0.1298
}

& $vpk pack --packId HostTemplate --packVersion $Version --packDir $PublishDir --mainExe HostTemplate.exe --outputDir $OutputDir --channel win --delta None
if ($LASTEXITCODE -ne 0) {
    throw "vpk pack failed with exit code $LASTEXITCODE"
}

$nupkg = Get-ChildItem -Path $OutputDir -Filter "*-full.nupkg" | Select-Object -First 1
$feed = Join-Path $OutputDir "releases.win.json"
if (-not $nupkg -or -not (Test-Path $feed)) {
    throw "vpk did not produce a full nupkg and releases.win.json in $OutputDir"
}

Write-Host "Packed $($nupkg.FullName)"
