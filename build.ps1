param (
    [switch]$Run = $false,
    [string]$Version = "",
    [string]$OutputDir = ""
)

Write-Host "Publishing single-file app (win-x64)..." -ForegroundColor Cyan

$publishArgs = @(
    "publish",
    "ClickTool.csproj",
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", "true",
    "/p:PublishSingleFile=true",
    "/p:IncludeNativeLibrariesForSelfExtract=true"
)

if ($Version) {
    Write-Host "Using version: $Version" -ForegroundColor Yellow
    $publishArgs += "/p:Version=$Version"
}

if ($OutputDir) {
    Write-Host "Using output directory: $OutputDir" -ForegroundColor Yellow
    $publishArgs += "-o"
    $publishArgs += $OutputDir
}

& dotnet @publishArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

$publishDir = if ($OutputDir) { $OutputDir } else { "bin\\Release\\net8.0-windows\\win-x64\\publish" }
$exePath = Join-Path $publishDir "ClickTool.exe"

if (-not (Test-Path $exePath)) {
    Write-Host "Output exe not found: $exePath" -ForegroundColor Red
    exit 1
}

Write-Host "Publish succeeded: $exePath" -ForegroundColor Green

if ($Run) {
    Write-Host "Launching app..." -ForegroundColor Yellow
    Start-Process $exePath
}
