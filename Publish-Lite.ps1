# Ponytail: single-file publish for LensLite
# Run: .\Publish-Lite.ps1 (after dotnet 8 installed)

$src = Join-Path $PSScriptRoot "LensLite.csproj"
$out = Join-Path $PSScriptRoot "bin"

Write-Host "Building LensLite..." -ForegroundColor Cyan
dotnet publish $src `
    --configuration Release `
    --self-contained true `
    --runtime win-x64 `
    --output $out `
    /p:PublishSingleFile=true 2>&1 | ForEach-Object { Write-Host $_ }

$exe = Join-Path $out "LensLite.exe"
if (Test-Path $exe) {
    Write-Host "`n✓ Ready: $exe" -ForegroundColor Green
    Write-Host "Drag this to your tray or double-click to start."
} else {
    Write-Host "`n✗ Build failed. Install .NET 8 SDK first." -ForegroundColor Red
}
