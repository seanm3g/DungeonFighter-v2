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

## Pixel-art UI exploration

Run **`Dungeon Fighter Art Lab.bat`** on Windows, or `dotnet run --project Code -- ART` on other platforms. After rebuilding the regular game, **F9** opens the same Art Lab in a separate window.

The art view shares the existing `GameCoordinator`, active character, generated dungeon, inventory, and auto-combat. Click the next room on the map or press **Space** at the normal between-room prompt. Click bag items to equip them through the existing inventory handler. **F1** opens the field guide.

**Game Menu** uses the backend's choices; **Classic UI** returns to the full interface for character management, settings, and combo editing. This is an alternative presentation of the real game: actions affect the same character and normal saves. The initial asset set uses one cathedral backdrop and three approximate equipment portrait families. See [the Art Lab notes](Documentation/ArtLab/README.md).

## Modular inventory icons

Run **`Dungeon Fighter Item Icons.bat`**, or `dotnet run --project Code -p:KeepRunningInstance=true -- ICONLAB`, for the standalone Rarity Gallery. Generate side-by-side rows from Common through Mythic, filter by family/tier, shuffle a new seed, inspect a card, and export PNGs or a batch. **Affix editor** opens the unrestricted editor covering all 414 catalog items, materials, qualities, and prefixes/suffixes. The shared 16×16 renderer also supplies regular inventory/comparison icons and Art Lab bag icons. See [the icon system guide](Documentation/ArtLab/ItemIcons/README.md) for the atlas and verification commands.

## Source layout

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

