# Publish a new launcher version to GitHub in one command:
#   bump <Version> in DSHLauncher.csproj -> build -> commit -> tag -> push -> create Release with exe.
#
# Usage (run from this directory):
#   pwsh ./publish-release.ps1 -Version 1.5.0 -Notes "新增 xxx；修复 yyy"
#   pwsh ./publish-release.ps1 -Version 1.5.1 -SkipBuild   # 复用现有构建产物
#
# Notes:
# - Requires: git, gh (authenticated via `gh auth login`), .NET SDK (build.ps1 uses
#   E:\deepseek\work\.dotnet\dotnet.exe when available).
# - If a Release with the same tag already exists, creation is skipped (idempotent).
# - Remember to update CHANGELOG.md manually before running.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Version,

    [string]$Notes = "",

    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

# --- validate version -------------------------------------------------------
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Write-Error "Version must look like 1.5.0 (got '$Version')"
    exit 1
}
$tag = "v$Version"

# --- prerequisites ----------------------------------------------------------
if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Error "git not found."
    exit 1
}
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "gh not found. Install GitHub CLI first (E:\deepseek\work\bin is on PATH)."
    exit 1
}
gh auth status *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Error "gh is not authenticated. Run 'gh auth login' first."
    exit 1
}

# --- bump version in DSHLauncher.csproj ------------------------------------
$csproj = Join-Path $PSScriptRoot "DSHLauncher.csproj"
$content = Get-Content -LiteralPath $csproj -Raw -Encoding UTF8
if ($content -notmatch '<Version>') {
    Write-Error "No <Version> element found in DSHLauncher.csproj"
    exit 1
}
$content = [regex]::Replace($content, '<Version>[^<]*</Version>', "<Version>$Version</Version>")
[System.IO.File]::WriteAllText($csproj, $content, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "Version set to $Version in DSHLauncher.csproj"

# --- build ------------------------------------------------------------------
if (-not $SkipBuild) {
    Write-Host "==> build-installer.ps1 (launcher + setup)"
    & (Join-Path $PSScriptRoot "build-installer.ps1")
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
$exe = Join-Path $PSScriptRoot "publish\win-x64\DSHLauncher.exe"
if (-not (Test-Path -LiteralPath $exe)) {
    Write-Error "Build output not found: $exe (run without -SkipBuild first)"
    exit 1
}
$setup = Join-Path $PSScriptRoot "DSHLauncherSetup.exe"
if (-not (Test-Path -LiteralPath $setup)) {
    Write-Error "Installer not found: $setup (run build-installer.ps1 first)"
    exit 1
}

# --- commit -----------------------------------------------------------------
Write-Host "==> git add -A"
git add -A
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "==> git commit"
git commit -m "feat: v$Version release"
if ($LASTEXITCODE -ne 0) {
    Write-Host "Nothing to commit (clean tree) - continuing."
}

# --- tag & push -------------------------------------------------------------
Write-Host "==> git tag -f $tag"
git tag -f $tag
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "==> git push origin main"
git push origin main
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "==> git push --force origin $tag"
git push --force origin $tag
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# --- create release (skip if it already exists) ------------------------------
Write-Host "==> gh release create $tag"
# gh release view writes "release not found" to stderr; with
# $ErrorActionPreference=Stop that stderr becomes a terminating error in
# PowerShell 5.1, so guard the existence check with try/catch.
$releaseExists = $false
try {
    gh release view $tag --json tagName *> $null
    $releaseExists = ($LASTEXITCODE -eq 0)
}
catch {
    $releaseExists = $false
}
if ($releaseExists) {
    Write-Host "Release $tag already exists - skipping create."
}
else {
    $ghArgs = @("release", "create", $tag, $setup, $exe, "--title", "DeepSeek Harness 启动器 v$Version")
    if ($Notes) {
        # Pass notes via a UTF-8 (no BOM) file to avoid encoding issues with
        # Chinese text in native-command arguments under Windows PowerShell 5.1.
        $notesPath = Join-Path $env:TEMP "gh_notes_$tag.txt"
        [System.IO.File]::WriteAllText($notesPath, $Notes, (New-Object System.Text.UTF8Encoding($false)))
        $ghArgs += "--notes-file"
        $ghArgs += $notesPath
    }
    gh @ghArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host ""
Write-Host "Done. Release: https://github.com/kongbaiwds-web/dsh-launcher/releases/tag/$tag" -ForegroundColor Green
