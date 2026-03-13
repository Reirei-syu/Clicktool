param (
    [switch]$Run = $false
)

Write-Host "Publishing single-file app (win-x64)..." -ForegroundColor Cyan

dotnet publish ClickTool.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

$exePath = "bin\\Release\\net8.0-windows\\win-x64\\publish\\ClickTool.exe"

if (-not (Test-Path $exePath)) {
    Write-Host "Output exe not found: $exePath" -ForegroundColor Red
    exit 1
}

Write-Host "Publish succeeded: $exePath" -ForegroundColor Green

if ($Run) {
    Write-Host "Launching app..." -ForegroundColor Yellow
    Start-Process $exePath
}
