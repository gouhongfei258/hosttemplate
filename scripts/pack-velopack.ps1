# 把已发布的程序目录打成 Velopack Windows 全量包。
#
# 路径相对仓库根目录（scripts 的上一级）。调用方传入绝对路径时原样使用，
# GitHub Actions 可以把 RUNNER_TEMP 下的目录传进来。
#
# 交互式（本地，扫描输出目录里已有版本，回车接受建议的下一补丁号）：
#   pwsh ./scripts/pack-velopack.ps1
#   pwsh ./scripts/pack-velopack.ps1 -SourceDir src/HostTemplate.App/bin/Release/net8.0-windows -OutputDir artifacts/velopack
#
# 非交互（CI 必须带版本号，不再 Read-Host / pause）：
#   pwsh ./scripts/pack-velopack.ps1 -Version 1.0.42 -PublishDir $publishDir -OutputDir $feedDir -NonInteractive
#
# 只打包列出的程序文件（相对 SourceDir）。不传 -Include 时打包整个源目录，
# 但会丢掉源目录根上的工站配置：app.xml、plc.xml、api.xml、users.xml、bootstrap.xml。
# templates\ 下面的同名模板会保留。
#   pwsh ./scripts/pack-velopack.ps1 -SourceDir publish -OutputDir artifacts/velopack -Include HostTemplate.exe,HostTemplate.dll,log4net.config -Version 1.0.3 -NonInteractive

param(
    [string]$Version = "",

    [Alias("PublishDir")]
    [string]$SourceDir = "src/HostTemplate.App/bin/Release/net8.0-windows",

    [string]$OutputDir = "artifacts/velopack",

    [string]$PackId = "HostTemplate",

    [string]$MainExe = "HostTemplate.exe",

    [string]$PackTitle = "HostTemplate",

    [string]$PackAuthors = "装备开发部",

    [string[]]$Include = @(),

    [string[]]$Exclude = @("app.xml", "plc.xml", "api.xml", "users.xml", "bootstrap.xml"),

    [switch]$NonInteractive,

    [switch]$Force
)

Set-StrictMode -Version Latest

$script:RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))

function Test-InteractiveSession {
    if ($NonInteractive) {
        return $false
    }

    if ($env:GITHUB_ACTIONS -eq "true" -or $env:CI -eq "true") {
        return $false
    }

    return $true
}

function Test-WindowsHost {
    if (Test-Path variable:IsWindows) {
        return [bool]$IsWindows
    }

    return $env:OS -eq "Windows_NT"
}

function Resolve-RepoPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "Path is empty."
    }

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $script:RepoRoot $Path))
}

function Get-VersionSortKey {
    param([string]$Text)

    if ($Text -match "^(\d+)\.(\d+)\.(\d+)") {
        return ([int]$Matches[1] * 1000000) + ([int]$Matches[2] * 1000) + [int]$Matches[3]
    }

    return -1
}

function Get-VersionFromPackageName {
    param(
        [string]$Name,
        [string]$Id
    )

    if ([string]::IsNullOrWhiteSpace($Name)) {
        return $null
    }

    $pattern = "^" + [regex]::Escape($Id) + "-(?<ver>\d+\.\d+\.\d+(?:\.\d+)?)(?:-full|-delta)?\.nupkg$"
    $match = [regex]::Match($Name, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($match.Success) {
        return $match.Groups["ver"].Value
    }

    return $null
}

function Add-Version {
    param(
        [System.Collections.Generic.List[string]]$Versions,
        [string]$Candidate
    )

    if ([string]::IsNullOrWhiteSpace($Candidate)) {
        return
    }

    if ($Versions -notcontains $Candidate) {
        $Versions.Add($Candidate)
    }
}

function Get-ExistingPackVersions {
    param(
        [Parameter(Mandatory = $true)][string]$Directory,
        [Parameter(Mandatory = $true)][string]$Id
    )

    $versions = New-Object "System.Collections.Generic.List[string]"
    if (-not (Test-Path -LiteralPath $Directory)) {
        return @()
    }

    $releaseFile = Join-Path $Directory "RELEASES"
    if (Test-Path -LiteralPath $releaseFile -PathType Leaf) {
        foreach ($line in @(Get-Content -LiteralPath $releaseFile -ErrorAction SilentlyContinue)) {
            foreach ($token in @($line -split "\s+")) {
                Add-Version $versions (Get-VersionFromPackageName $token $Id)
            }
        }
    }

    $feedFiles = @(Get-ChildItem -LiteralPath $Directory -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "releases*.json" -or $_.Name -eq "RELEASES" })
    foreach ($feed in $feedFiles) {
        $feedText = Get-Content -LiteralPath $feed.FullName -Raw -ErrorAction SilentlyContinue
        if ([string]::IsNullOrWhiteSpace($feedText)) {
            continue
        }

        foreach ($match in [regex]::Matches($feedText, '"Version"\s*:\s*"(?<ver>\d+\.\d+\.\d+(?:\.\d+)?)"')) {
            Add-Version $versions $match.Groups["ver"].Value
        }

        foreach ($match in [regex]::Matches($feedText, '(?<name>[\w\.\-]+\.nupkg)')) {
            Add-Version $versions (Get-VersionFromPackageName $match.Groups["name"].Value $Id)
        }
    }

    $packages = @(Get-ChildItem -LiteralPath $Directory -Filter "*.nupkg" -File -ErrorAction SilentlyContinue)
    foreach ($package in $packages) {
        Add-Version $versions (Get-VersionFromPackageName $package.Name $Id)
    }

    return @($versions | Sort-Object @{ Expression = { Get-VersionSortKey $_ }; Descending = $true }, @{ Expression = { $_ }; Descending = $true })
}

function Get-NextPatchVersion {
    param([string]$Current)

    if ($Current -match "^(\d+)\.(\d+)\.(\d+)") {
        return "{0}.{1}.{2}" -f [int]$Matches[1], [int]$Matches[2], ([int]$Matches[3] + 1)
    }

    return ""
}

function Get-RelativePackPath {
    param(
        [string]$Root,
        [System.IO.FileInfo]$File
    )

    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $fileFull = [System.IO.Path]::GetFullPath($File.FullName)
    if (-not $fileFull.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "File is outside the source directory: $fileFull"
    }

    return $fileFull.Substring($rootFull.Length).TrimStart('\', '/').Replace('\', '/')
}

function Copy-OneFile {
    param(
        [System.IO.FileInfo]$File,
        [string]$SourceRoot,
        [string]$DestinationRoot
    )

    $relative = Get-RelativePackPath $SourceRoot $File
    $target = Join-Path $DestinationRoot ($relative.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
    $parent = Split-Path -Parent $target
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    Copy-Item -LiteralPath $File.FullName -Destination $target -Force
    return $relative
}

function Resolve-IncludeFiles {
    param(
        [string]$SourceRoot,
        [string]$Pattern
    )

    $trimmed = $Pattern.Trim().TrimStart('\', '/').Replace('\', '/')
    if ([string]::IsNullOrWhiteSpace($trimmed)) {
        return @()
    }

    $osRelative = $trimmed.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
    $direct = Join-Path $SourceRoot $osRelative
    $found = New-Object "System.Collections.Generic.List[System.IO.FileInfo]"

    if ($trimmed -notmatch '[\*\?]') {
        if (Test-Path -LiteralPath $direct -PathType Leaf) {
            $found.Add((Get-Item -LiteralPath $direct))
            return @($found)
        }

        if (Test-Path -LiteralPath $direct -PathType Container) {
            foreach ($file in @(Get-ChildItem -LiteralPath $direct -File -Recurse)) {
                $found.Add($file)
            }

            return @($found)
        }
    }

    $leaf = Split-Path -Leaf $osRelative
    $parentRelative = Split-Path -Parent $osRelative
    $searchRoot = $SourceRoot
    if (-not [string]::IsNullOrWhiteSpace($parentRelative) -and $parentRelative -notmatch '[\*\?]') {
        $searchRoot = Join-Path $SourceRoot $parentRelative
    }

    if (Test-Path -LiteralPath $searchRoot) {
        foreach ($file in @(Get-ChildItem -Path (Join-Path $searchRoot $leaf) -File -ErrorAction SilentlyContinue)) {
            $found.Add($file)
        }
    }

    if ($found.Count -eq 0) {
        $like = $trimmed -replace '\*\*', '*'
        foreach ($file in @(Get-ChildItem -LiteralPath $SourceRoot -File -Recurse -ErrorAction SilentlyContinue)) {
            $relative = Get-RelativePackPath $SourceRoot $file
            if ($relative -like $like) {
                $found.Add($file)
            }
        }
    }

    return @($found)
}

function New-PackDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$SourceRoot,
        [string]$MainExecutable,
        [string[]]$IncludePatterns,
        [string[]]$ExcludeNames
    )

    $excluded = New-Object "System.Collections.Generic.HashSet[string]"
    foreach ($name in @($ExcludeNames)) {
        if (-not [string]::IsNullOrWhiteSpace($name)) {
            [void]$excluded.Add($name.Trim())
        }
    }

    $includePatterns = @($IncludePatterns | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $skipped = New-Object "System.Collections.Generic.List[string]"

    if ($includePatterns.Count -eq 0) {
        foreach ($name in $excluded) {
            $candidate = Join-Path $SourceRoot $name
            if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                $skipped.Add($name)
            }
        }

        if ($skipped.Count -eq 0) {
            return [pscustomobject]@{
                PackDir = $SourceRoot
                Staged = $false
                Skipped = @()
            }
        }
    }

    $stage = Join-Path ([System.IO.Path]::GetTempPath()) ("hosttemplate-pack-" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Force -Path $stage | Out-Null

    try {
        if ($includePatterns.Count -gt 0) {
            foreach ($pattern in $includePatterns) {
                $matches = @(Resolve-IncludeFiles $SourceRoot $pattern)
                if ($matches.Count -eq 0) {
                    throw "Include pattern matched no files: $pattern"
                }

                foreach ($file in $matches) {
                    Copy-OneFile $file $SourceRoot $stage | Out-Null
                }
            }
        }
        else {
            foreach ($file in @(Get-ChildItem -LiteralPath $SourceRoot -File -Recurse)) {
                $relative = Get-RelativePackPath $SourceRoot $file
                if ($relative -notmatch '/' -and $excluded.Contains($relative)) {
                    continue
                }

                Copy-OneFile $file $SourceRoot $stage | Out-Null
            }
        }
    }
    catch {
        if (Test-Path -LiteralPath $stage) {
            Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue
        }

        throw
    }

    return [pscustomobject]@{
        PackDir = $stage
        Staged = $true
        Skipped = @($skipped)
    }
}

function Write-PackError {
    param($ErrorRecord)

    Write-Host ""
    Write-Host "=========================================" -ForegroundColor Red
    Write-Host "          SCRIPT ERROR" -ForegroundColor Red
    Write-Host "=========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "[Error Message]" -ForegroundColor Yellow
    Write-Host "  $($ErrorRecord.Exception.Message)" -ForegroundColor White
    Write-Host ""
    Write-Host "[Exception Type]" -ForegroundColor Yellow
    Write-Host "  $($ErrorRecord.Exception.GetType().FullName)" -ForegroundColor White
    Write-Host ""
    Write-Host "[Error Location]" -ForegroundColor Yellow
    Write-Host "  Line: $($ErrorRecord.InvocationInfo.ScriptLineNumber)" -ForegroundColor White
    $line = $ErrorRecord.InvocationInfo.Line
    if (-not [string]::IsNullOrWhiteSpace($line)) {
        Write-Host "  Content: $($line.Trim())" -ForegroundColor White
    }
    Write-Host ""
    Write-Host "[Stack Trace]" -ForegroundColor Yellow
    Write-Host $ErrorRecord.ScriptStackTrace -ForegroundColor DarkGray
    Write-Host ""

    $message = [string]$ErrorRecord.Exception.Message
    if ($message -match "not recognized|not found|not exist|could not find|Main executable|Source directory|Include pattern") {
        Write-Host "[Possible Cause] Command or file not found" -ForegroundColor Magenta
        Write-Host "  Make sure the source directory and main exe exist." -ForegroundColor Magenta
        Write-Host "  Velopack CLI is installed by this script into .tools (dotnet tool update vpk)." -ForegroundColor Magenta
        Write-Host ""
    }

    if ($message -match "denied|Unauthorized|permission") {
        Write-Host "[Possible Cause] Permission denied" -ForegroundColor Magenta
        Write-Host "  Try running this script as Administrator" -ForegroundColor Magenta
        Write-Host ""
    }

    if ($message -match "must run on Windows|AppImage") {
        Write-Host "[Possible Cause] vpk is not running on Windows" -ForegroundColor Magenta
        Write-Host "  Linux vpk only produces an AppImage. Use windows-latest or a Windows machine." -ForegroundColor Magenta
        Write-Host ""
    }
}

function Invoke-HostTemplatePack {
    try {
        Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force -ErrorAction SilentlyContinue
    }
    catch {
        # 进程级策略改不了也不影响 CI。下面照常打包。
    }

    $sourceFull = Resolve-RepoPath $SourceDir
    $outputFull = Resolve-RepoPath $OutputDir

    if (-not (Test-Path -LiteralPath $sourceFull -PathType Container)) {
        throw "Source directory not found: $sourceFull"
    }

    Write-Host "===========================================" -ForegroundColor Yellow
    Write-Host "Checking existing versions..." -ForegroundColor Cyan
    Write-Host "===========================================" -ForegroundColor Yellow

    $existing = @(Get-ExistingPackVersions -Directory $outputFull -Id $PackId)
    $interactive = Test-InteractiveSession
    $chosen = $Version

    if ($existing.Count -gt 0) {
        Write-Host ""
        Write-Host "===========================================" -ForegroundColor Yellow
        Write-Host "Existing versions found:" -ForegroundColor Yellow
        Write-Host "===========================================" -ForegroundColor Yellow
        for ($i = 0; $i -lt $existing.Count; $i++) {
            Write-Host "[$($i + 1)] $($existing[$i])" -ForegroundColor Green
        }

        Write-Host ""
        Write-Host "Latest version: $($existing[0])" -ForegroundColor Magenta
        $suggested = Get-NextPatchVersion $existing[0]
        if ($interactive -and [string]::IsNullOrWhiteSpace($chosen)) {
            if ($suggested) {
                Write-Host "Suggested next version: $suggested" -ForegroundColor Cyan
                $entered = Read-Host "Enter version number (press Enter to use suggested: $suggested)"
                $chosen = $(if ([string]::IsNullOrWhiteSpace($entered)) { $suggested } else { $entered.Trim() })
            }
            else {
                $entered = Read-Host "Enter version number"
                $chosen = $entered.Trim()
            }
        }
        elseif ([string]::IsNullOrWhiteSpace($chosen)) {
            throw "Version is required in non-interactive mode. Pass -Version x.y.z."
        }
    }
    else {
        Write-Host ""
        Write-Host "===========================================" -ForegroundColor Yellow
        Write-Host "No existing versions found." -ForegroundColor Yellow
        Write-Host "===========================================" -ForegroundColor Yellow
        $suggested = "1.0.0"
        if ($interactive -and [string]::IsNullOrWhiteSpace($chosen)) {
            $entered = Read-Host "Enter version number (press Enter to use default: $suggested)"
            $chosen = $(if ([string]::IsNullOrWhiteSpace($entered)) { $suggested } else { $entered.Trim() })
        }
        elseif ([string]::IsNullOrWhiteSpace($chosen)) {
            throw "Version is required in non-interactive mode. Pass -Version x.y.z."
        }
    }

    if ($chosen -notmatch "^\d+\.\d+\.\d+") {
        throw "Invalid version format: $chosen. Expected format: X.Y.Z (e.g., 1.0.0)"
    }

    if ($existing -contains $chosen) {
        Write-Host ""
        Write-Host "WARNING: Version $chosen already exists!" -ForegroundColor Red
        if ($Force) {
            Write-Host "Overwrite confirmed by -Force." -ForegroundColor Yellow
        }
        elseif ($interactive) {
            $confirm = Read-Host "Do you want to overwrite? (y/N)"
            if ($confirm -ne "y" -and $confirm -ne "Y") {
                Write-Host "Operation cancelled." -ForegroundColor Yellow
                return
            }
        }
        else {
            throw "Version $chosen already exists in $outputFull. Pass -Force to overwrite."
        }
    }

    $prepared = New-PackDirectory -SourceRoot $sourceFull -MainExecutable $MainExe -IncludePatterns $Include -ExcludeNames $Exclude
    $script:StageDir = $null
    if ($prepared.Staged) {
        $script:StageDir = $prepared.PackDir
    }

    $packDir = $prepared.PackDir
    $mainPath = Join-Path $packDir $MainExe
    if (-not (Test-Path -LiteralPath $mainPath -PathType Leaf)) {
        throw "Main executable not found: $mainPath"
    }

    if (@($prepared.Skipped).Count -gt 0) {
        Write-Host "Left station config out of the package: $(@($prepared.Skipped) -join ', ')" -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "===========================================" -ForegroundColor Yellow
    Write-Host "Ready to pack version: $chosen" -ForegroundColor Green
    Write-Host "Source: $sourceFull" -ForegroundColor Cyan
    Write-Host "Pack directory: $packDir" -ForegroundColor Cyan
    Write-Host "Output: $outputFull" -ForegroundColor Cyan
    Write-Host "Pack id: $PackId" -ForegroundColor Cyan
    Write-Host "Main exe: $MainExe" -ForegroundColor Cyan
    Write-Host "===========================================" -ForegroundColor Yellow

    if (-not (Test-WindowsHost)) {
        throw "vpk pack for win-x64 must run on Windows. Linux vpk only produces an AppImage. Use the GitHub Actions release job."
    }

    if (-not (Test-Path -LiteralPath $outputFull)) {
        Write-Host "Creating output directory: $outputFull" -ForegroundColor Cyan
        New-Item -ItemType Directory -Force -Path $outputFull | Out-Null
    }

    $toolDir = Join-Path $script:RepoRoot ".tools"
    New-Item -ItemType Directory -Force -Path $toolDir | Out-Null
    $vpk = Join-Path $toolDir "vpk.exe"
    if (-not (Test-Path -LiteralPath $vpk)) {
        & dotnet tool update vpk --tool-path $toolDir --version 0.0.1298
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet tool update vpk failed with exit code $LASTEXITCODE"
        }
    }

    $vpkArgs = @(
        "pack",
        "--packId", $PackId,
        "--packVersion", $chosen,
        "--packDir", $packDir,
        "--mainExe", $MainExe,
        "--packTitle", $PackTitle,
        "--packAuthors", $PackAuthors,
        "--outputDir", $outputFull,
        "--channel", "win",
        "--delta", "None"
    )

    Write-Host ""
    Write-Host ("Executing: vpk " + ($vpkArgs -join " ")) -ForegroundColor DarkGray

    $savedNative = $null
    $hasNativePreference = Test-Path variable:PSNativeCommandUseErrorActionPreference
    if ($hasNativePreference) {
        $savedNative = $PSNativeCommandUseErrorActionPreference
        $PSNativeCommandUseErrorActionPreference = $false
    }

    try {
        & $vpk @vpkArgs
        if ($LASTEXITCODE -ne 0) {
            throw "vpk pack failed with exit code $LASTEXITCODE"
        }
    }
    finally {
        if ($hasNativePreference) {
            $PSNativeCommandUseErrorActionPreference = $savedNative
        }
    }

    $nupkg = Get-ChildItem -LiteralPath $outputFull -Filter "*-full.nupkg" -File | Select-Object -First 1
    $feed = Join-Path $outputFull "releases.win.json"
    if (-not $nupkg -or -not (Test-Path -LiteralPath $feed -PathType Leaf)) {
        throw "vpk did not produce a full nupkg and releases.win.json in $outputFull"
    }

    Write-Host ""
    Write-Host "Pack completed successfully!" -ForegroundColor Green
    Write-Host "Version: $chosen" -ForegroundColor Green
    Write-Host "Packed $($nupkg.FullName)" -ForegroundColor Green
    Write-Host "Output files: $outputFull" -ForegroundColor Green
}

if ($MyInvocation.InvocationName -ne ".") {
    $script:StageDir = $null
    $failed = $false
    try {
        Invoke-HostTemplatePack
    }
    catch {
        $failed = $true
        Write-PackError $_
    }
    finally {
        if ($script:StageDir -and (Test-Path -LiteralPath $script:StageDir)) {
            Remove-Item -LiteralPath $script:StageDir -Recurse -Force -ErrorAction SilentlyContinue
        }

        if (Test-InteractiveSession) {
            Write-Host ""
            Write-Host "===========================================" -ForegroundColor Yellow
            Write-Host "Script execution finished." -ForegroundColor White
            cmd /c pause
        }
    }

    if ($failed) {
        exit 1
    }
}
