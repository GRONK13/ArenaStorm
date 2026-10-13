#!/bin/bash

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT_DIR"

echo "=== ArenaStorm Process Status ==="

if [ -f "$ROOT_DIR/.webapi.pid" ]; then
    PID=$(cat "$ROOT_DIR/.webapi.pid")
    if ps -p "$PID" > /dev/null 2>&1; then
        echo "Web API:     RUNNING (PID $PID)"
    else
        echo "Web API:     STOPPED (stale pid file)"
    fi
else
    echo "Web API:     NOT RUNNING"
fi

if [ -f "$ROOT_DIR/.server.pid" ]; then
    PID=$(cat "$ROOT_DIR/.server.pid")
    if ps -p "$PID" > /dev/null 2>&1; then
        echo "Game Server: RUNNING (PID $PID)"
    else
        echo "Game Server: STOPPED (stale pid file)"
    fi
else
    echo "Game Server: NOT RUNNING"
fi

echo ""
echo "=== Port Listen Status ==="
ss -tuln 2>/dev/null | grep -E ':(5000|9050)' || netstat -tuln 2>/dev/null | grep -E ':(5000|9050)' || echo "(Check logs if ports do not appear)"

echo ""
echo "=== Recent Web API Logs ==="
tail -n 5 "$ROOT_DIR/webapi.log" 2>/dev/null || echo "No webapi.log yet"

echo ""
echo "=== Recent Game Server Logs ==="
tail -n 5 "$ROOT_DIR/server.log" 2>/dev/null || echo "No server.log yet"
