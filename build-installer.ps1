# Build the one-click installer: publish the launcher, pack payload.zip,
# then build DSHLauncherSetup.exe (single file with the whale icon).
# Usage:  powershell -ExecutionPolicy Bypass -File build-installer.ps1
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

$dotnet = "dotnet"
$localDotnet = "E:\deepseek\work\.dotnet\dotnet.exe"
if (Test-Path $localDotnet) { $dotnet = $localDotnet }

# 1) build the launcher (publish to publish\win-x64) - includes icon + version
& (Join-Path $PSScriptRoot "build.ps1")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# 2) pack payload
$payload = Join-Path $PSScriptRoot "Installer\payload.zip"
if (Test-Path $payload) { Remove-Item $payload -Force }
Compress-Archive -Path (Join-Path $PSScriptRoot "publish\win-x64\*") -DestinationPath $payload -CompressionLevel Optimal -Force
Write-Host "payload: $((Get-Item $payload).Length) bytes"

# 3) publish the installer as a SINGLE FILE (embeds payload.zip + whale icon)
$outDir = Join-Path $PSScriptRoot "publish-installer"
& $dotnet publish (Join-Path $PSScriptRoot "Installer\Installer.csproj") -c Release -r win-x64 --self-contained false -o $outDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# 4) copy the single-file installer to the project root so repo downloaders
#    can click it directly (and the release commit carries it)
$rootSetup = Join-Path $PSScriptRoot "DSHLauncherSetup.exe"
Copy-Item (Join-Path $outDir "DSHLauncherSetup.exe") $rootSetup -Force
Write-Host ""
Write-Host "root installer: $rootSetup ($((Get-Item $rootSetup).Length) bytes)" -ForegroundColor Green
