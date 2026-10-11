# DungeonFighter v2

Turn-based RPG in C# (.NET) with a data-driven action/combat system and an Avalonia UI.

## Quick start

Requires **.NET 8 SDK** and a desktop session for the Avalonia window.

### Launchers (recommended)

Extract the **whole folder** first (do not run the `.bat` from inside the zip window).

| Platform | From repo root |
|---|---|
| **Windows** | Double-click `DungeonFighter-PC.bat` (also `Dungeon Fighter(PC).bat`) |
| **Linux** | `chmod +x "Dungeon Fighter(Linux).sh"` then `./"Dungeon Fighter(Linux).sh"` |
| **macOS** | `chmod +x "Dungeon Fighter(Mac).sh"` then `./"Dungeon Fighter(Mac).sh"` |

The Windows launcher installs the .NET 8 SDK into your user folder when needed (no administrator prompt). If `dist\DF.exe` is already in the folder, it launches that instead of building.

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

## Optional: Victory limerick (OpenAI)

After completing a dungeon, the game can append a short 5-line limerick summarizing the final battle. This is **off by default** and needs your own OpenAI API key (never committed; stored only on your machine).

1. Open **Settings → OpenAI**
2. Paste your API key (or set the `OPENAI_API_KEY` environment variable and leave the field blank)
3. Enable **Generate victory limerick after completing a dungeon**
4. Optionally change **Model** (default `gpt-4o-mini`) or **Base URL** (OpenAI-compatible proxies)
5. Save, then finish a dungeon — a center-justified limerick appears on the dungeon completion screen (with a random `-First Last, N aod` credit) when the request finishes. The poem refers to the hero via a pun/metaphor inspired by their name (not the literal name), with no "Victory verse" title.

Generation **prefetches on the final room's killing blow** (after you have stayed through the dungeon) and **shows only on dungeon completion** — not after mid-run fights or the stay/leave prompt. It does not block loot or room-clear. If the feature is off, the key is missing, the call times out, or the model returns unusable text, the game falls back to offline templates in `GameData/FlavorText.json` (`combatNarratives.victoryLimerick`) when the feature is enabled.

Prefs persist in gitignored `GameData/GeneralSettings.json` under `openAiPreferences`.

## Combat scene JSON (right-click)

During combat or a dungeon run, **right-click the center combat log** to save the current fight as scene JSON under `GameData/CombatLogs/` and copy it. Each beat looks like:

`character`, `action`, `target`, `environment`, `emotion`, `camera`, `gear` (`helmet` / `cloak` / `armor` / `weapon_main` / `weapon_offhand`), `effects`.

A single beat is one object; a full fight is a JSON array. **Ctrl+C** still copies the on-screen log as plain text.

## Combat scene live feed (ComfyUI)

While the game window is open, a localhost feed serves the same scene JSON as fights progress:

- Base URL: `http://127.0.0.1:7877/`
- `GET /combat-scene` — envelope with `revision`, `latest`, `scenes`
- `GET /combat-scene/latest` — latest beat only
- `GET /combat-scene/scenes` — full array (row 0 = empty room)
- `GET /combat-scene/wait?since=N` — long-poll for the next beat

Also writes `GameData/CombatLogs/live.json` and `live-latest.json`. ComfyUI custom node: `Tools/ComfyUI/dungeonfighter_combat_scene/` (copy into `ComfyUI/custom_nodes/`). Disable with `DF_COMBAT_SCENE_FEED=0`.

## Tests

Unit tests live under `Code/Tests/Unit/`. The project includes in-game and CLI test runners; see existing docs in `Code/Tests/README.md`.

