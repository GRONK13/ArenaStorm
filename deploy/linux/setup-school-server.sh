#!/bin/bash
set -e

echo "============================================="
echo "  ArenaStorm School Server Deployment Setup"
echo "============================================="

# 1. Update and install prerequisites
if ! command -v dotnet &>/dev/null; then
    echo "Installing .NET 9..."
    # Download and register Microsoft package signing key
    sudo apt-get update && sudo apt-get install -y wget apt-transport-https ufw
    wget -q https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs 2>/dev/null || echo "24.04")/packages-microsoft-prod.deb -O packages-microsoft-prod.deb || true
    if [ -f packages-microsoft-prod.deb ]; then
        sudo dpkg -i packages-microsoft-prod.deb || true
        rm -f packages-microsoft-prod.deb
    fi
    sudo apt-get update
    sudo apt-get install -y dotnet-sdk-9.0 || sudo apt-get install -y dotnet-runtime-9.0 aspnetcore-runtime-9.0
fi

# 2. Configure PostgreSQL or fallback to SQLite
if command -v psql &>/dev/null; then
    sudo -u postgres psql -c "CREATE USER arenastorm WITH PASSWORD 'school_password_2026';" || true
    sudo -u postgres psql -c "CREATE DATABASE arenastorm OWNER arenastorm;" || true
else
    echo "PostgreSQL not found; WebAPI will use built-in SQLite database at /opt/arenastorm/data/arenastorm.db"
fi

# 3. Create deployment user and directories
sudo id -u arenastorm &>/dev/null || sudo useradd -r -s /bin/false arenastorm
sudo mkdir -p /opt/arenastorm/webapi /opt/arenastorm/server
sudo chown -R arenastorm:arenastorm /opt/arenastorm

# 4. Publish binaries
echo "Publishing ArenaStorm.WebAPI..."
dotnet publish src/ArenaStorm.WebAPI/ArenaStorm.WebAPI.csproj -c Release -o /opt/arenastorm/webapi
echo "Publishing ArenaStorm.Server..."
dotnet publish src/ArenaStorm.Server/ArenaStorm.Server.csproj -c Release -o /opt/arenastorm/server

sudo chown -R arenastorm:arenastorm /opt/arenastorm

# 5. Copy and enable systemd services
sudo cp deploy/linux/arenastorm-api.service /etc/systemd/system/
sudo cp deploy/linux/arenastorm-server.service /etc/systemd/system/

sudo systemctl daemon-reload
sudo systemctl enable --now arenastorm-api
sudo systemctl enable --now arenastorm-server

# 6. Firewall rules
sudo ufw allow 5000/tcp comment 'ArenaStorm WebAPI'
sudo ufw allow 9050/udp comment 'ArenaStorm Game Server'

echo "============================================="
echo "  Deployment complete! Services are active."
echo "  Web API: http://$(hostname -I | awk '{print $1}'):5000"
echo "  UDP Game Server: $(hostname -I | awk '{print $1}'):9050"
echo "============================================="
