# ArenaStorm ⚡

A high-octane 2D top-down multiplayer action arena game engineered in **C# (.NET 9)**. Features an authoritative dedicated server with LiteNetLib UDP transport, client-side prediction, entity interpolation, procedural audio synthesis, an ASP.NET Core Minimal API backend with PostgreSQL / SQLite fallback, and a cross-platform MonoGame DesktopGL client.

---

## 🌟 Key Features

- **Authoritative Dedicated Game Server**: High-precision 30Hz simulation loop using LiteNetLib UDP. Handles physics, collisions, combat, and snapshots.
- **Client Prediction & Reconciliation**: Responsive local movement prediction with server snapshot reconciliation and remote player interpolation.
- **4 Character Archetypes**:
  - 🏹 **Ranger**: High mobility, rapid fire, long range.
  - 🛡️ **Tank**: High health pool, heavy close-range burst.
  - 🔮 **Mage**: Glass cannon with high-damage mystical projectiles.
  - ⚔️ **Warrior**: Balanced melee combatant with sweeping melee strikes.
- **Dual Combat System**:
  - Left-Click: Directional ranged projectile weapons.
  - Right-Click: Frontal 90° cone melee slash with custom visual arcs.
- **Game Modes**:
  - **PvP Deathmatch**: Free-for-all respawn combat to score limit.
  - **PvP Elimination**: Last-man-standing sudden death.
  - **Co-Op PvE Survival**: Wave-based AI enemies pathfinding toward players.
- **Dynamic Pickups**: Health Packs (+35 HP), Speed Boost (+40% speed for 6s), and Temporary Shield (+50 points).
- **Procedural Audio Synthesizer**: 100% code-generated 16-bit 44.1kHz PCM sound effects (lasers, hits, explosions, pickups, melee slashes)—zero external audio files needed!
- **School Server Ready**: Pre-configured Docker Compose stack, Linux systemd services, and remote SSH deployment scripts.

---

## 🎮 Controls

| Action | Control |
|---|---|
| **Move** | `W`, `A`, `S`, `D` / Arrow Keys |
| **Aim** | Mouse Pointer |
| **Shoot (Ranged)** | Left Mouse Button |
| **Melee Slash** | Right Mouse Button |
| **Scoreboard** | `Tab` (Hold) |
| **In-Game Chat** | `T` or `Enter` to focus, `Esc` to cancel |

---

## 🚀 Quick Start (Local Development)

### 1. Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### 2. Run the Stack
Open three separate terminal windows:

```powershell
# 1. Start Web API (Account, Lobby & Leaderboard service on port 5000)
dotnet run --project src/ArenaStorm.WebAPI

# 2. Start Dedicated Server (UDP port 9050)
dotnet run --project src/ArenaStorm.Server

# 3. Launch MonoGame Client
dotnet run --project src/ArenaStorm.Client
```

---

## 🏫 School Server Deployment

Refer to [`DEPLOYMENT.md`](DEPLOYMENT.md) for detailed instructions.

### Docker Compose (Recommended)
```bash
docker compose up -d --build
```

### Linux Bare-Metal (systemd)
```powershell
# Deploy remotely over SSH from this machine:
.\deploy\linux\deploy-to-ssh.ps1 -ServerUser "ubuntu" -ServerHost "192.168.1.100"
```

---

## 🧪 Test Suite

Run the automated test suite across all projects:
```powershell
dotnet test ArenaStorm.sln
```
All 22 unit and integration tests verify combat mechanics, wave survival, protocol serialization, and REST endpoints.
