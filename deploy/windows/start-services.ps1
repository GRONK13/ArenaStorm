<#
.SYNOPSIS
    Starts ArenaStorm backend services (Web API and UDP Dedicated Server) on Windows Server or local PC.
#>

param(
    [int]$WebApiPort = 5000,
    [int]$GameServerPort = 9050
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path "$PSScriptRoot\..\.."

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "   Starting ArenaStorm School Server Stack   " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

# Publish both if needed
Write-Host "Building services..." -ForegroundColor Yellow
dotnet build "$root\ArenaStorm.sln" -c Release

# Start WebAPI in background job or separate process
Write-Host "Launching Web API on port $WebApiPort..." -ForegroundColor Green
$webProcess = Start-Process dotnet -ArgumentList "run --project `"$root\src\ArenaStorm.WebAPI`" --no-build -c Release" -PassThru -NoNewWindow

# Start UDP Server
Write-Host "Launching Game Server on UDP port $GameServerPort..." -ForegroundColor Green
$serverProcess = Start-Process dotnet -ArgumentList "run --project `"$root\src\ArenaStorm.Server`" --no-build -c Release -- $GameServerPort" -PassThru -NoNewWindow

Write-Host "`nServices running! Press Ctrl+C in this console to stop all services." -ForegroundColor Cyan

try {
    while ($true) {
        Start-Sleep -Seconds 2
        if ($webProcess.HasExited -or $serverProcess.HasExited) {
            Write-Host "One of the services stopped." -ForegroundColor Red
            break
        }
    }
}
finally {
    Write-Host "Shutting down services..." -ForegroundColor Yellow
    if (!$webProcess.HasExited) { Stop-Process -Id $webProcess.Id -Force -ErrorAction SilentlyContinue }
    if (!$serverProcess.HasExited) { Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue }
}
