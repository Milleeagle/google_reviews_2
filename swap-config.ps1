#!/usr/bin/env pwsh
# Script to easily swap between local (with credentials) and safe (placeholder) configs

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("local", "safe")]
    [string]$Mode = "local"
)

$localConfig = "appsettings.Local.json"
$devConfig = "appsettings.Development.json"

if (-not (Test-Path $localConfig)) {
    Write-Host "ERROR: $localConfig not found!" -ForegroundColor Red
    Write-Host "This file should contain your real credentials." -ForegroundColor Yellow
    exit 1
}

if ($Mode -eq "local") {
    Write-Host "Switching to LOCAL mode (with real credentials)..." -ForegroundColor Cyan

    # Backup current Development config
    Copy-Item $devConfig "$devConfig.backup" -Force
    Write-Host "  - Backed up $devConfig to $devConfig.backup" -ForegroundColor Gray

    # Copy Local config to Development
    Copy-Item $localConfig $devConfig -Force
    Write-Host "  - Copied $localConfig to $devConfig" -ForegroundColor Green

    Write-Host "`n✓ Now using LOCAL configuration with real credentials" -ForegroundColor Green
    Write-Host "  Run the app with: dotnet run" -ForegroundColor White
}
elseif ($Mode -eq "safe") {
    Write-Host "Restoring SAFE mode (placeholder credentials)..." -ForegroundColor Cyan

    # Restore from backup if exists
    if (Test-Path "$devConfig.backup") {
        Copy-Item "$devConfig.backup" $devConfig -Force
        Write-Host "  - Restored $devConfig from backup" -ForegroundColor Green
    } else {
        Write-Host "  - No backup found, keeping current config" -ForegroundColor Yellow
    }

    Write-Host "`n✓ Now using SAFE configuration (placeholders)" -ForegroundColor Green
    Write-Host "  Safe to commit to git" -ForegroundColor White
}

Write-Host "`nCurrent environment: $(if ($Mode -eq 'local') { 'LOCAL (with credentials)' } else { 'SAFE (placeholders)' })" -ForegroundColor Magenta
