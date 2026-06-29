# Balance Tuning (Profile-Driven)

Automated tuning uses **profiles** to declare what to simulate and what to analyze.

Profiles: `GameData/TuningProfiles/*.json` — see `GameData/TuningProfiles/README.md`

## Commands

```bash
dotnet run --project Code/Code.csproj -- TUNEPROFILES
dotnet run --project Code/Code.csproj -- TUNESIM --profile <id>
dotnet run --project Code/Code.csproj -- TUNEANALYZE
dotnet run --project Code/Code.csproj -- TUNEAPPLY
dotnet run --project Code/Code.csproj -- TUNETUNING --profile combat-dials --max-iterations 5 --stop-when-pass
```

Session: `GameData/BalancePatches/balance-tuning-session.json`

Legacy level-curve aliases: `LEVELSIM`, `LEVELANALYZE`, `LEVELAPPLY`, `LEVELTUNING`

Skill: `.cursor/skills/level-balance-tuning/SKILL.md`

## Four-dial diagnostic model

| Dial | Symptom | Typical knobs |
|------|---------|---------------|
| **Power** | Win rate or avg turns off target | HP, damage mults, global enemy stats |
| **Variance** | High spread, low combo streaks | Roll thresholds, variance compression |
| **Agency** | High loss severity, low control | TEC/INT, combo affordance |
| **Scaling** | Level curve drift | Per-level growth, dungeon scaling |

Enable with `"enableDialRouting": true` or `"suggesters": ["dial_routed"]` in a profile. `TUNEANALYZE` reports `PrimaryDial` and diagnosis.

## Developer sim mode

Continue past 0 HP to measure loss severity:

```bash
TUNESIM --profile combat-dials --continue-past-zero-hp
```

Or set `"continuePastZeroHp": true` in profile `simulation`.

## Combat duration targets

Per-combatant optimal: **12–15 turns** (config: `balanceTuningGoals.combatDuration`)

## Built-in profiles

| Profile | Purpose |
|---------|---------|
| `combat-dials` | Primary four-dial fundamentals pass (1000 encounters) |
| `level-curve` | Same-level sweeps; duration + level curve suggesters |
| `combat-fundamentals` | Quick fundamentals pass with dial routing |
| `class-build-matrix` | Weapon path parity @ L10 |
| `dungeon-scaling` | Comprehensive + dungeon scaling suggesters |
| `enemy-roster` | Enemy differentiation @ L25 |
| `gear-probability` | Weapon variance @ L15 |
| `environment-hazards` | Fundamentals tempo + variance |
| `midgame-balance` | L10 full matrix |
| `earlygame-balance` | L5 matrix |
| `weapon-focus` | L10 weapon parity only |

## Code map

| Component | Path |
|-----------|------|
| Profiles | `Code/Game/Tuning/Profiles/` |
| Dial classifier | `Code/Game/Tuning/Profiles/BalanceDialClassifier.cs` |
| Workflow | `Code/Game/Tuning/BalanceTuningWorkflow.cs` |
| Full loop | `Code/Game/Tuning/LevelTuningRunner.cs` |
| Session | `Code/Game/Tuning/LevelTuningSessionStore.cs` |
