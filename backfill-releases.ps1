# Backfill historical GitHub Releases for v1.2.1 and v1.3.0.
# These versions were never tagged/released, so people could not see them.
# Note: no old build artifacts exist, so these releases carry no exe asset -
# they make the version history visible on the Releases page.
#
# Usage (run once from this directory):
#   pwsh ./backfill-releases.ps1

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "gh not found."
    exit 1
}
gh auth status *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Error "gh is not authenticated. Run 'gh auth login' first."
    exit 1
}

$items = @(
    @{
        Tag    = "v1.2.1"
        Commit = "70143b7"
        Title  = "DSH 启动器 v1.2.1"
        Notes  = "最小化按钮改为任务栏最小化，不再收进系统托盘。"
    },
    @{
        Tag    = "v1.3.0"
        Commit = "fe23184"
        Title  = "DSH 启动器 v1.3.0"
        Notes  = "启动器自动拉起 DSH 服务：服务未运行时自动启动并等待就绪；菜单新增「启动 DSH 服务」；服务配置可查看；离线横幅「重试」自动拉起服务。"
    }
)

foreach ($item in $items) {
    $tag = $item.Tag
    Write-Host "==> git tag -f $tag $($item.Commit)"
    git tag -f $tag $item.Commit
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    git push --force origin $tag
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

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
        Write-Host "Release $tag already exists - skipping."
        continue
    }
    # Pass notes via a UTF-8 (no BOM) file to avoid encoding issues with
    # Chinese text in native-command arguments under Windows PowerShell 5.1.
    $notesPath = Join-Path $env:TEMP "gh_notes_$tag.txt"
    [System.IO.File]::WriteAllText($notesPath, $item.Notes, (New-Object System.Text.UTF8Encoding($false)))
    gh release create $tag --title $item.Title --notes-file $notesPath
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host ""
Write-Host "Backfill done. See https://github.com/kongbaiwds-web/dsh-launcher/releases" -ForegroundColor Green
