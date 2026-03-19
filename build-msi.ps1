Write-Host "Building single-file app..." -ForegroundColor Cyan
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1

if ($LASTEXITCODE -ne 0) {
    Write-Host "App publish failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Building MSI installer..." -ForegroundColor Cyan
dotnet build .\Installer\ClickTool.Installer.wixproj -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "MSI build failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

$msiPath = "Installer\bin\x64\Release\ClickTool.Installer.msi"

if (-not (Test-Path $msiPath)) {
    Write-Host "MSI not found: $msiPath" -ForegroundColor Red
    exit 1
}

Write-Host "MSI build succeeded: $msiPath" -ForegroundColor Green
