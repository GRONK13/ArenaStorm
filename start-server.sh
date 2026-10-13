#!/bin/bash

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT_DIR"

export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"

# If binaries are not compiled yet, build them first
if [ ! -f "$ROOT_DIR/dist/webapi/ArenaStorm.WebAPI.dll" ] || [ ! -f "$ROOT_DIR/dist/server/ArenaStorm.Server.dll" ]; then
    echo ">> Binaries not found. Running setup..."
    bash "$ROOT_DIR/deploy/linux/setup-user-server.sh"
fi

# Stop existing processes if running
if [ -f "$ROOT_DIR/.webapi.pid" ]; then
    kill "$(cat "$ROOT_DIR/.webapi.pid")" 2>/dev/null || true
    rm -f "$ROOT_DIR/.webapi.pid"
fi
if [ -f "$ROOT_DIR/.server.pid" ]; then
    kill "$(cat "$ROOT_DIR/.server.pid")" 2>/dev/null || true
    rm -f "$ROOT_DIR/.server.pid"
fi

mkdir -p "$ROOT_DIR/data"

echo "=========================================================="
echo "  Starting ArenaStorm Services (User-Space)               "
echo "=========================================================="

# 1. Start Web API (Port 5000 with local SQLite database)
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_HTTP_PORTS=5000
export ConnectionStrings__DefaultConnection="Data Source=$ROOT_DIR/data/arenastorm.db"

nohup dotnet "$ROOT_DIR/dist/webapi/ArenaStorm.WebAPI.dll" > "$ROOT_DIR/webapi.log" 2>&1 &
WEB_PID=$!
echo $WEB_PID > "$ROOT_DIR/.webapi.pid"
echo ">> Web API started (PID $WEB_PID) on port 5000"

# 2. Start Authoritative UDP Game Server (Port 9050)
export GAME_SERVER_PORT=9050

nohup dotnet "$ROOT_DIR/dist/server/ArenaStorm.Server.dll" 9050 > "$ROOT_DIR/server.log" 2>&1 &
SERVER_PID=$!
echo $SERVER_PID > "$ROOT_DIR/.server.pid"
echo ">> Game Server started (PID $SERVER_PID) on UDP port 9050"

sleep 1

echo "=========================================================="
echo "  ArenaStorm is LIVE on your school server!               "
echo "  - Web API:       http://$(hostname -I | awk '{print $1}'):5000"
echo "  - UDP Game Port: $(hostname -I | awk '{print $1}'):9050"
echo "  - Logs:          tail -f webapi.log server.log          "
echo "  - Stop Services: ./stop-server.sh                       "
echo "=========================================================="
