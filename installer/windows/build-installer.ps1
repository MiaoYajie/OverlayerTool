# OverlayerTool Windows 安装包构建脚本
# 依赖：.NET 8 SDK、Inno Setup 6（ISCC.exe）
#
# 用法：
#   .\build-installer.ps1
#   .\build-installer.ps1 -SingleFile:$false

param(
    [switch]$SingleFile = $true
)

$ErrorActionPreference = "Stop"
$Root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$Project = Join-Path $Root "src\OverlayerTool.App\OverlayerTool.App.csproj"
$PublishDir = Join-Path $Root "src\OverlayerTool.App\bin\Release\net8.0\win-x64\publish"
$IssFile = Join-Path $PSScriptRoot "OverlayerTool.iss"

Write-Host "==> dotnet publish (win-x64, self-contained)" -ForegroundColor Cyan

$publishArgs = @(
    "publish", $Project,
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", "true",
    "/p:PublishReadyToRun=true"
)

if ($SingleFile) {
    $publishArgs += @(
        "/p:PublishSingleFile=true",
        "/p:IncludeNativeLibrariesForSelfExtract=true",
        "/p:EnableCompressionInSingleFile=true"
    )
}

Push-Location $Root
try {
    dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

    $iscc = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1

    if (-not $iscc) {
        Write-Host ""
        Write-Host "未找到 Inno Setup 6（ISCC.exe）。" -ForegroundColor Yellow
        Write-Host "请安装：https://jrsoftware.org/isinfo.php" -ForegroundColor Yellow
        Write-Host "发布文件已在：$PublishDir" -ForegroundColor Green
        exit 0
    }

    Write-Host "==> Inno Setup compile" -ForegroundColor Cyan
    & $iscc $IssFile
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup compile failed" }

    Write-Host ""
    Write-Host "完成。安装包输出目录：$(Join-Path $Root 'dist')" -ForegroundColor Green
}
finally {
    Pop-Location
}
