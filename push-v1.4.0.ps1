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
Write-Host "==> git tag $tag"
git rev-parse -q --verify "refs/tags/$tag" 2>$null
if ($LASTEXITCODE -ne 0) {
    git tag $tag
}

Write-Host "==> git push origin $tag"
git push origin $tag
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$gh = Get-Command gh -ErrorAction SilentlyContinue
if ($null -ne $gh) {
    Write-Host "==> gh release create $tag"
    gh release create $tag "publish\win-x64\DSHLauncher.exe" `
        --title "DeepSeek Harness Launcher v1.4.0" `
        --notes "v1.4.0: DeepSeek Harness icon, new display name, close DSH service."
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
else {
    Write-Host "gh not found. Please create the Release manually:"
    Write-Host "https://github.com/kongbaiwds-web/dsh-launcher/releases/new?tag=v1.4.0"
}

Write-Host "Done."
