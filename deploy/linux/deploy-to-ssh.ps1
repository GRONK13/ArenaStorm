<#
.SYNOPSIS
    Automated one-command remote deployment script to deploy ArenaStorm to a Linux school server via SSH.
.EXAMPLE
    .\deploy\linux\deploy-to-ssh.ps1 -ServerUser "ubuntu" -ServerHost "192.168.1.100"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$ServerHost,

    [Parameter(Mandatory=$true)]
    [string]$ServerUser,

    [string]$RemoteDir = "/opt/arenastorm",
    [string]$IdentityFile = ""
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path "$PSScriptRoot\..\.."

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "  Deploying ArenaStorm to $ServerUser@$ServerHost " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

$sshArgs = @()
$scpArgs = @()
if ($IdentityFile -ne "") {
    $sshArgs += "-i"
    $sshArgs += $IdentityFile
    $scpArgs += "-i"
    $scpArgs += $IdentityFile
}

# 1. Publish Linux binaries locally first
Write-Host "1. Building Release binaries for Linux (linux-x64)..." -ForegroundColor Yellow
dotnet publish "$root\src\ArenaStorm.WebAPI\ArenaStorm.WebAPI.csproj" -c Release -r linux-x64 --self-contained false -o "$root\publish\linux\webapi"
dotnet publish "$root\src\ArenaStorm.Server\ArenaStorm.Server.csproj" -c Release -r linux-x64 --self-contained false -o "$root\publish\linux\server"

# 2. Prepare remote staging directory
Write-Host "`n2. Preparing remote staging directory..." -ForegroundColor Yellow
$target = "$ServerUser@$ServerHost"
& ssh @sshArgs $target "mkdir -p /tmp/arenastorm-deploy"

# 3. Transfer binaries and systemd configs
Write-Host "`n3. Copying published files to server via SCP..." -ForegroundColor Yellow
& scp -r @scpArgs "$root\publish\linux\webapi" "$target`:/tmp/arenastorm-deploy/webapi"
& scp -r @scpArgs "$root\publish\linux\server" "$target`:/tmp/arenastorm-deploy/server"
& scp @scpArgs "$root\deploy\linux\arenastorm-api.service" "$target`:/tmp/arenastorm-deploy/"
& scp @scpArgs "$root\deploy\linux\arenastorm-server.service" "$target`:/tmp/arenastorm-deploy/"

# 4. Install files and register systemd services
Write-Host "`n4. Installing services and starting via systemd..." -ForegroundColor Yellow
$remoteScript = @"
sudo mkdir -p $RemoteDir/webapi $RemoteDir/server $RemoteDir/data
sudo cp -r /tmp/arenastorm-deploy/webapi/* $RemoteDir/webapi/
sudo cp -r /tmp/arenastorm-deploy/server/* $RemoteDir/server/
sudo chmod +x $RemoteDir/webapi/ArenaStorm.WebAPI $RemoteDir/server/ArenaStorm.Server 2>/dev/null || true

# User and permissions
sudo id -u arenastorm &>/dev/null || sudo useradd -r -s /bin/false arenastorm
sudo chown -R arenastorm:arenastorm $RemoteDir

# Install systemd services
sudo cp /tmp/arenastorm-deploy/arenastorm-api.service /etc/systemd/system/
sudo cp /tmp/arenastorm-deploy/arenastorm-server.service /etc/systemd/system/
sudo systemctl daemon-reload

sudo systemctl enable --now arenastorm-api
sudo systemctl enable --now arenastorm-server

# Allow ports in UFW firewall if enabled
sudo ufw allow 5000/tcp comment 'ArenaStorm WebAPI' 2>/dev/null || true
sudo ufw allow 9050/udp comment 'ArenaStorm Game Server' 2>/dev/null || true

echo '=== Deployment Status ==='
sudo systemctl is-active arenastorm-api
sudo systemctl is-active arenastorm-server
"@

& ssh @sshArgs $target $remoteScript

Write-Host "`n=================================================" -ForegroundColor Green
Write-Host "  ArenaStorm successfully deployed to $ServerHost!" -ForegroundColor Green
Write-Host "  Web API URL:  http://$ServerHost`:5000" -ForegroundColor Green
Write-Host "  UDP Game Port: $ServerHost`:9050" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
