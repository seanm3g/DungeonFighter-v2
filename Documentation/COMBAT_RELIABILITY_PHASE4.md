# Combat reliability (Phase 4) — how to verify

Phase 4 closes the combat reliability track from the Refactor Hotspots audit. After this, structural maintainability work (`Item.cs`, UI coordinator splits, `Character`/`Actor` shrink) is a separate initiative.

After pulling these changes, manually check:

1. **Lab encounter sim cleanup** — Run Action Lab encounter batch twice (or fundamentals sim). No slowdown or odd metrics drift from leaked `OneShotKillOccurred` handlers.
2. **Settings save** — Settings → Save Game: UI stays responsive on slow disk (async IO).
3. **Death save** — Death screen → clone or tombstone: save completes without UI hitch.
4. **Multi-character dungeon** — Two characters: set dungeon on A, switch to B and set a different dungeon, switch back to A — A's dungeon is unchanged (no legacy fallback resurrection).

Automated coverage:

```bash
cd Code
dotnet run -- --run-test-filter GameStateManagerMultiCharacter
dotnet run -- --run-test-filter SaveLoad
dotnet run -- --run-test-filter DeveloperSimMode
dotnet run -- --run-test-filter ActionInteractionLab
dotnet run -- --run-test-filter SettingsMenuHandler
```

**Note:** `SimulationPacing.EnableFastMode` remains a one-way flip for CLI/MCP/test startup (`Program.cs`, `GameWrapper`) — intentional for headless runs.
