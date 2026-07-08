# Combat reliability (Phase 1) — how to verify

After pulling these changes, manually check:

1. **Line-by-line combat reveal** — Fight any enemy with normal Text Delays (not instant / not max Page Up speed). Action blocks should reveal lines with a short pause between them, then wait ~`ActionDelayMs` before the next actor.
2. **Environment hazards** — Enter a hostile room with environmental actions. Hazard lines should finish displaying before the next hero/enemy turn starts (no overlapping jumpy log).
3. **Action Lab mute isolation** — With a live fight UI idle, run a silent Action Lab sim (or batch). The live combat mute flag should not stay stuck off after the sim ends.
4. **Background dungeon cancel** — Start a background dungeon for a character, then start another for the same character (or call cancel). The first run's token should cancel instead of keeping shared combat/UI state busy.

Automated coverage:

```bash
cd Code
dotnet run -- --run-test-filter CombatDelayManager
dotnet run -- --run-test-filter CombatUiMuteScope
dotnet run -- --run-test-filter BackgroundDungeonTaskManager
```
