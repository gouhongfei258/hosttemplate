# HostTemplate 初始化脚本
#
# 用法：
#   .\init.ps1 -Name "MyStation" -Title "某产线某工站"
#
# 作用：
#   1. 把命名空间 HostTemplate.* 替换成 <Name>.*
#   2. 重命名项目目录与 csproj / sln
#   3. 替换界面标题与程序名
#
# 执行前请先关闭 Visual Studio，并确保当前目录是 HostTemplate 根目录。

param(
    [Parameter(Mandatory = $true)]
    [string]$Name,

    [string]$Title = ""
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path "HostTemplate.sln")) {
    throw "请在 HostTemplate 根目录下执行本脚本。"
}

if ($Title -eq "") { $Title = $Name }

Write-Host "==> 替换命名空间：HostTemplate.* -> $Name.*" -ForegroundColor Cyan

$textFiles = Get-ChildItem -Recurse -Include *.cs, *.csproj, *.sln, *.md, *.config, *.manifest |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

foreach ($file in $textFiles) {
    $content = Get-Content -Raw -Encoding UTF8 $file.FullName
    $updated = $content -replace 'HostTemplate', $Name
    if ($updated -ne $content) {
        Set-Content -Path $file.FullName -Value $updated -Encoding UTF8 -NoNewline
        Write-Host "    updated: $($file.FullName)"
    }
}

Write-Host "==> 重命名项目文件与目录" -ForegroundColor Cyan

Get-ChildItem -Recurse -Include *HostTemplate* |
    Sort-Object { $_.FullName.Length } -Descending |
    ForEach-Object {
        $newName = $_.Name -replace 'HostTemplate', $Name
        if ($newName -ne $_.Name) {
            Rename-Item -Path $_.FullName -NewName $newName
            Write-Host "    renamed: $($_.Name) -> $newName"
        }
    }

Write-Host "==> 替换界面标题为：$Title" -ForegroundColor Cyan
$mainForm = Join-Path "src" "$Name.App\MainForm.Designer.cs"
if (Test-Path $mainForm) {
    (Get-Content -Raw -Encoding UTF8 $mainForm) -replace '上位机模板 · 监控', $Title |
        Set-Content -Path $mainForm -Encoding UTF8 -NoNewline
}

Write-Host ""
Write-Host "完成。下一步：" -ForegroundColor Green
Write-Host "  1. 打开 $Name.sln，还原 NuGet"
Write-Host "  2. 修改 src/$Name.App/Steps/DemoSteps.cs 为真实业务"
Write-Host "  3. dotnet run --project src/$Name.App -- --simulate 验证流程"
