# Combat reliability (Phase 3) — how to verify

After pulling these changes, manually check:

1. **Action Lab forced d20** — Step with a fixed d20 in Action Lab; the roll should match the chosen value. Undo/replay should still work. Parallel encounter batches must not steal each other's forced rolls.
2. **Developer sim floor scope** — Run a fundamentals sim with `--negative-hp-floor` (continue-past-zero mode). A subsequent normal sim must use the default floor (-500), not the prior override.
3. **Health bar hints under mute** — Run a muted encounter batch (Action Lab sim or battle statistics) while watching live combat HP bars; DoT segment colors on the next live damage drop should not reflect sim-only ticks.
4. **Action Lab ticker isolation** — Open Action Lab (bootstrap/reset) while a live dungeon fight could be running in the background; global combat timing must not reset to zero.

Automated coverage:

```bash
cd Code
dotnet run -- --run-test-filter CombatUiMuteScope
dotnet run -- --run-test-filter HealthBarDeltaDamageHint
dotnet run -- --run-test-filter DeveloperSimMode
dotnet run -- --run-test-filter ActionInteractionLab
dotnet run -- --run-test-filter ActionExecutionFlow
```
