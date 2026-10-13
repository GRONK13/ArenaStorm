#!/bin/bash

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT_DIR"

echo "Stopping ArenaStorm services..."

if [ -f "$ROOT_DIR/.webapi.pid" ]; then
    PID=$(cat "$ROOT_DIR/.webapi.pid")
    kill "$PID" 2>/dev/null && echo "Stopped Web API (PID $PID)" || echo "Web API not running"
    rm -f "$ROOT_DIR/.webapi.pid"
fi

if [ -f "$ROOT_DIR/.server.pid" ]; then
    PID=$(cat "$ROOT_DIR/.server.pid")
    kill "$PID" 2>/dev/null && echo "Stopped Game Server (PID $PID)" || echo "Game Server not running"
    rm -f "$ROOT_DIR/.server.pid"
fi

# Fallback: kill by assembly name for current user
pkill -u "$(whoami)" -f "ArenaStorm.WebAPI.dll" 2>/dev/null || true
pkill -u "$(whoami)" -f "ArenaStorm.Server.dll" 2>/dev/null || true

echo "All ArenaStorm services stopped."
