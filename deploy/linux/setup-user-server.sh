#!/bin/bash
set -e

echo "=========================================================="
echo "  ArenaStorm School Server Setup (Non-Sudo / User-Space)  "
echo "=========================================================="

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"
cd "$ROOT_DIR"

# 1. Check or install .NET 9 in user space (~/.dotnet)
if ! command -v dotnet &>/dev/null && [ ! -f "$HOME/.dotnet/dotnet" ]; then
    echo ">> .NET 9 not detected. Installing .NET 9 SDK into ~/.dotnet without sudo..."
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 9.0
fi

# Add ~/.dotnet to PATH for current session and bashrc
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"

if ! grep -q 'DOTNET_ROOT' "$HOME/.bashrc" 2>/dev/null; then
    echo 'export DOTNET_ROOT="$HOME/.dotnet"' >> "$HOME/.bashrc"
    echo 'export PATH="$HOME/.dotnet:$PATH"' >> "$HOME/.bashrc"
fi

echo ">> Using .NET version: $(dotnet --version)"

# 2. Build and publish release binaries locally
echo ">> Publishing ArenaStorm.WebAPI..."
dotnet publish src/ArenaStorm.WebAPI/ArenaStorm.WebAPI.csproj -c Release -o "$ROOT_DIR/dist/webapi"

echo ">> Publishing ArenaStorm.Server..."
dotnet publish src/ArenaStorm.Server/ArenaStorm.Server.csproj -c Release -o "$ROOT_DIR/dist/server"

# 3. Create data directory for SQLite database
mkdir -p "$ROOT_DIR/data"

echo "=========================================================="
echo "  Build Complete! You can now start the server with:      "
echo "  ./start-server.sh                                       "
echo "=========================================================="
