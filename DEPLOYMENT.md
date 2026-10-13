# ArenaStorm: School Server Deployment Guide

This guide covers deploying the **ArenaStorm** server stack (ASP.NET Core Minimal API, PostgreSQL database, and LiteNetLib Authoritative UDP Dedicated Server) onto your school server.

---

## Architecture Overview

```
                      +------------------------------------------+
                      |               School Server              |
                      |                                          |
                      |  +------------------------------------+  |
                      |  |     PostgreSQL DB (Port 5432)      |  |
                      |  +-----------------+------------------+  |
                      |                    |                     |
                      |  +-----------------v------------------+  |
Client 1 (Student PC) |  |   ArenaStorm.WebAPI (Port 5000)   |  |
  +------------------>|  |   - Auth, Lobbies, Leaderboards   |  |
                      |  +------------------------------------+  |
                      |                                          |
Client 2 (Lab PC)     |  +------------------------------------+  |
  +------------------>|  |  ArenaStorm.Server (Port 9050 UDP) |  |
                      |  |  - 30Hz Authoritative Game World   |  |
                      |  +------------------------------------+  |
                      +------------------------------------------+
```

---

## Required Ports & Firewall Settings

Make sure the following ports are open on the school server's firewall:

| Port | Protocol | Service | Description |
|---|---|---|---|
| **5000** | TCP | `ArenaStorm.WebAPI` | Account registration, login, lobby matchmaking, leaderboards |
| **9050** | UDP | `ArenaStorm.Server` | High-frequency 30Hz game physics, inputs, and state synchronization |

---

## Deployment Option 1: Docker Compose (Recommended)

This is the easiest and most isolated way to run the entire backend stack.

### Prerequisites
- Docker Engine & Docker Compose installed on the school server (`docker compose version`).

### Steps
1. Copy or clone the repository to the school server:
   ```bash
   git clone <your-repo-url> arenastorm
   cd arenastorm
   ```

2. *(Optional)* Customize passwords or secrets by copying `.env.example`:
   ```bash
   cp .env.example .env
   # Edit .env with your preferred database password or JWT secret
   ```

3. Build and launch all services in detached mode:
   ```bash
   docker compose up -d --build
   ```

4. Verify all containers are running and healthy:
   ```bash
   docker compose ps
   ```

5. View live logs:
   ```bash
   # View all logs
   docker compose logs -f

   # Or specific service
   docker compose logs -f gameserver
   docker compose logs -f webapi
   ```

6. To stop the services:
   ```bash
   docker compose down
   ```

---

## Deployment Option 2: Linux School Server (Non-Sudo / Student Account)

If you have a student or non-root user account (without `sudo` access), use the **User-Space Deployment**:

```bash
# 1. Update your local repository
git pull

# 2. Grant execute permissions
chmod +x deploy/linux/setup-user-server.sh start-server.sh stop-server.sh status-server.sh

# 3. Build & start the server stack
./start-server.sh
```

**How this works without sudo:**
- Automatically downloads and installs .NET 9 SDK into `~/.dotnet` using Microsoft's user-space install script if .NET 9 is not on the system.
- Builds both `ArenaStorm.WebAPI` and `ArenaStorm.Server` directly inside your home directory.
- Runs Web API on port **5000** backed by a self-initializing SQLite database file (`data/arenastorm.db`)—no PostgreSQL administration or root database required!
- Runs UDP Authoritative Game Server on port **9050**.
- Manages processes via `nohup` in the background with PID tracking (`./status-server.sh`, `./stop-server.sh`).

---

## Deployment Option 3: Linux with Sudo / Systemd Root Privileges

If your school server runs native Linux without Docker, you have two simple ways to deploy:

### Method A: One-Command Remote Deploy from this PC (via SSH)

From PowerShell on your development machine, run the automated SSH deploy script:

```powershell
.\deploy\linux\deploy-to-ssh.ps1 -ServerUser "your_school_user" -ServerHost "school.server.ip.or.hostname"
```
*(Optionally provide `-IdentityFile "path\to\key.pem"` if using an SSH key)*

This script automatically:
1. Builds and publishes `linux-x64` binaries for both WebAPI and Server.
2. Copies the binaries and systemd units to the school server via SCP.
3. Installs them into `/opt/arenastorm/`, sets up the `arenastorm` service account, and registers the systemd daemons.
4. Starts both services and configures UFW firewall rules.

### Method B: Directly on the School Server via SSH Terminal

1. SSH into the school server:
   ```bash
   ssh your_school_user@school.server.ip
   ```

2. Clone or copy the project repository onto the server:
   ```bash
   git clone <repo_url> arenastorm
   cd arenastorm
   ```

3. Run the automated installation script:
   ```bash
   chmod +x deploy/linux/setup-school-server.sh
   ./deploy/linux/setup-school-server.sh
   ```

### Manual Steps
1. **Install .NET 9 SDK and PostgreSQL**:
   ```bash
   sudo apt-get update
   sudo apt-get install -y dotnet-sdk-9.0 postgresql postgresql-contrib
   ```

2. **Publish Applications**:
   ```bash
   dotnet publish src/ArenaStorm.WebAPI -c Release -o /opt/arenastorm/webapi
   dotnet publish src/ArenaStorm.Server -c Release -o /opt/arenastorm/server
   ```

3. **Install Systemd Services**:
   ```bash
   sudo cp deploy/linux/arenastorm-api.service /etc/systemd/system/
   sudo cp deploy/linux/arenastorm-server.service /etc/systemd/system/
   sudo systemctl daemon-reload
   sudo systemctl enable --now arenastorm-api
   sudo systemctl enable --now arenastorm-server
   ```

4. **Check Status**:
   ```bash
   sudo systemctl status arenastorm-api
   sudo systemctl status arenastorm-server
   ```

---

## Deployment Option 3: Windows Server / Local Lab Host

If the school server is a Windows machine:

1. Open PowerShell as Administrator.
2. Ensure .NET 9 SDK is installed.
3. Run the automated launcher:
   ```powershell
   .\deploy\windows\start-services.ps1
   ```
   Both the Web API (port 5000) and the Game Server (UDP port 9050) will start up with output piped to the console.

---

## Distributing & Connecting Game Clients

1. **Publish Client Binaries for Students**:
   To create a standalone single-file executable for Windows PCs without requiring students to install the .NET SDK:
   ```powershell
   dotnet publish src/ArenaStorm.Client -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish/ArenaStormClient
   ```
   Share the generated `publish/ArenaStormClient` folder or zip file to student PCs via a school network drive or USB.

2. **Connecting to the Server**:
   When students launch `ArenaStorm.Client`:
   - On the title screen, enter the **School Server IP** into the `SERVER IP / HOST` field (e.g. `192.168.1.100` or `server.lab.school.edu`).
   - Click **LOGIN**, **REGISTER**, or **PLAY AS GUEST**.
   - The client immediately routes both REST API calls (port 5000) and UDP game sockets (port 9050) to the designated server IP.
   - Alternatively, students can set an environment variable before launching:
     ```powershell
     $env:ARENA_SERVER_IP = "192.168.1.100"
     .\ArenaStorm.exe
     ```
