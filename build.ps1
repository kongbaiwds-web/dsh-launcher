# 构建脚本：发布 Release 版本到 publish\win-x64
# 用法：pwsh ./build.ps1
# 如果机器上装了系统级 dotnet（加入 PATH），会自动使用它；否则用 E:\deepseek\work\.dotnet

$ErrorActionPreference = "Stop"

$dotnet = "dotnet"
$localDotnet = "E:\deepseek\work\.dotnet\dotnet.exe"
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue) -and (Test-Path $localDotnet)) {
    $dotnet = $localDotnet
}

$outDir = Join-Path $PSScriptRoot "publish\win-x64"

& $dotnet publish (Join-Path $PSScriptRoot "DSHLauncher.csproj") `
    -c Release -r win-x64 --self-contained false -o $outDir

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "构建完成：$outDir\DSHLauncher.exe" -ForegroundColor Green
Write-Host "提示：开机自启/快捷方式请从该目录的 exe 操作（不要用 bin\Debug 下的临时产物）。"
