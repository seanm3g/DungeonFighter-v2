# Combat reliability (Phase 2) — how to verify

After pulling these changes, manually check:

1. **Combat display failure visibility** — If a combat log render throws (or you inject a test throw into the display path), the failure should appear in Debug output / `DebugLogger` as `[BlockDisplayManager] …`, not vanish silently. Combat should continue.
2. **Load cancel / timeout** — Load a character; a hung or cancelled read should abort the IO (linked CTS + `CancelAfter`), not leave a fire-and-forget `ReadAllTextAsync` running after the UI gives up.
3. **Menu-exit save** — From the in-game menu, choose Save & Exit (`0`). Exit should use `SaveCharacterAsync` so a slow disk write does not freeze the Avalonia UI thread.
4. **Character switch dungeon/room** — With two registered characters, set dungeon/room on A, switch to B (no dungeon). B must show no room/dungeon. Switch back to A and confirm A's room is still on A's context.
5. **Muted fight ticker isolation** — Run Action Lab / battle statistics while a live dungeon fight could be active: muted `RunCombat` / `BattleExecutor` / `CombatSimulator` must not `GameTicker.Reset()` the live global clock (isolated AsyncLocal time).

Automated coverage:

```bash
cd Code
dotnet run -- --run-test-filter CombatUiMuteScope
dotnet run -- --run-test-filter BlockDisplayManager
dotnet run -- --run-test-filter GameStateManagerMultiCharacter
dotnet run -- --run-test-filter SaveLoad
```
