# Push v1.4.0 to GitHub and create a Release when gh is available.
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

Write-Host "==> git add -A"
git add -A
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "==> git commit"
git commit -m "feat: v1.4.0 DeepSeek Harness branding, high-res icon, close DSH service"
if ($LASTEXITCODE -ne 0) {
    Write-Host "Commit returned non-zero (maybe nothing to commit). Continuing..."
}

Write-Host "==> git push origin main"
git push origin main
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$tag = "v1.4.0"
# Always point the tag at the current HEAD (idempotent: re-running after new
# commits keeps the tag in sync instead of reusing a stale tag from an
# earlier run).
Write-Host "==> git tag -f $tag"
git tag -f $tag
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "==> git push --force origin $tag"
git push --force origin $tag
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$gh = Get-Command gh -ErrorAction SilentlyContinue
if ($null -ne $gh) {
    # Idempotent: skip creation if a release with this tag already exists.
    gh release view $tag --json tagName 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Release $tag already exists - skipping create."
    }
    else {
        Write-Host "==> gh release create $tag"
        gh release create $tag "publish\win-x64\DSHLauncher.exe" `
            --title "DeepSeek Harness Launcher v1.4.0" `
            --notes "v1.4.0: DeepSeek Harness icon, new display name, close DSH service."
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
}
else {
    Write-Host "gh not found. Please create the Release manually:"
    Write-Host "https://github.com/kongbaiwds-web/dsh-launcher/releases/new?tag=v1.4.0"
}

Write-Host "Done."
