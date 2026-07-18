# DungeonFighter v2

Turn-based RPG in C# (.NET) with a data-driven action/combat system and an Avalonia UI.

## Quick start

Requires **.NET 8 SDK** and a desktop session for the Avalonia window.

### Launchers (recommended)

| Platform | From repo root |
|---|---|
| **Windows** | Double-click `Dungeon Fighter(PC).bat` |
| **Linux** | `chmod +x "Dungeon Fighter(Linux).sh"` then `./"Dungeon Fighter(Linux).sh"` |
| **macOS** | `chmod +x "Dungeon Fighter(Mac).sh"` then `./"Dungeon Fighter(Mac).sh"` |

Linux details: [LINUX_SETUP_GUIDE.md](LINUX_SETUP_GUIDE.md) · Windows details: [WINDOWS_SETUP_GUIDE.md](WINDOWS_SETUP_GUIDE.md)

### Manual build / run

```bash
cd Code
dotnet build
dotnet run
```

## Where things are

- **Main code**: `Code/`
- **Game data**: `GameData/`
- **Architecture docs**: `Documentation/ARCHITECTURE.md`
- **Settings system docs**: `Documentation/SETTINGS_SYSTEM_ARCHITECTURE.md`
- **Documentation index**: `Documentation/README.md`
- **Work tracking**: `TASKLIST.md`
- **Product overview**: `OVERVIEW.md`

## Playing With Regions

Each character is always in one of three regions: Ancient Forest, Lava Caves, or Haunted Crypt. From the in-game hub, choose `Travel` to move to one of the other two regions. A trip rolls 10 route events using the same d20 outcome bands as combat, then dungeon selection favors the region you arrived in.

## Tests

Unit tests live under `Code/Tests/Unit/`. The project includes in-game and CLI test runners; see existing docs in `Code/Tests/README.md`.

