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

## Action Lab Balance Layers

Interactive layer ladder lives in Action Lab (**tools → [ Balance ]**):

1. Pick a process layer (Combat equation → Feel → Level curve → … → Playthrough check).
2. Scenario auto-applies (weapon-only Goblin matchup, anchors, dungeon, etc.).
3. **Run Stats** uses the same `ActionLabEncounterSimulator` / dungeon sim as tools Sim N and prints a PASS/FAIL checklist.
4. Edit layer-filtered knobs from `CombatTuningParameterRegistry` (same as Settings → Combat Tuning).
5. **Edit checklist targets** (expander under the Goal recipe) changes the PASS/FAIL bands for the selected layer (median combined / hero / enemy, ± tolerance, mean band, and layer-specific fields like combo floors, WR %, dungeon clear). **Reset targets to defaults** restores the factory recipe. Edits are session-lived on the layer definition (not a separate JSON file).
6. **Suggest** / **Apply suggestion** picks **one** knob from the worst FAIL (fundamentals / feel suggesters, with a Lab fallback when those return empty); **RUN loop** + **MAX** repeats Run Stats → Suggest → Apply until every checklist line PASS, MAX iterations, no suggestion, or checklist progress stagnates (3 non-improving iters). The report pane shows a **RUN LOOP PROGRESS REPORT** (start → iterations → end, checklist + knob deltas).

**OK (targets met)** means every checklist line is PASS after Run Stats. The window always shows the layer recipe under the scenario summary.

### Lab success criteria by process layer

Each layer has its own **Goal** line in the Balance window. Tempo stays near **27 combined** across levels (fight-length parity); tolerance widens with level (L1 ±1.5 → L25 ±2.5 → higher ±3).

| Layer | Primary “OK” checks |
|-------|---------------------|
| **1. Combat equation** | L1 duration only (median 27 ± 1.5, hero/enemy 12 ± 1.5, mean 24–30) |
| **2. Feel** | L1 duration + combo floors (≥0.5 runs / ≥2.0 max streak) + turn std-dev ≤ 8 |
| **3. Level curve** | **Every** anchor: WR on `LevelWinRateCurve` + median combined near 27 (wider ±) |
| **4. Weapon parity** | L10: each weapon WR on curve, best−worst WR spread ≤ **12pp**, tempo per weapon |
| **5. Enemy roster** | L25: WR on curve + midgame tempo (27 ± 2.5) |
| **6. Gear injection** | L10 with gear: WR **90–99%**, mean combined **18–30** (power without trivializing) |
| **7. Dungeon attrition** | Clear rate **70–95%** |
| **8. Playthrough** | Workbench profile only (no Lab checklist) |

### L1 micro-scale experiment (`l1-micro` patch)

Active balance patch can be `l1-micro` (see `GameData/PatchProfile.json`) for a tiny L1 number space while keeping ~12 swings to kill:

| Knob | Approx value |
|------|----------------|
| Player / enemy base HP | **45** (25 was too glassy once starting-weapon dmg + STR+primary hit ~3) |
| Base attrs | **1** |
| Player armor / enemy armor | **0** |
| Min damage / starting weapon dmg (Mace/Sword/Dagger/Wand) | **1** |
| Goblin sheet attrs | **1** (was 5/3/1/3) |
| Starter catalog weapons (Log/Branch/Twig/Stick) | baseDamage **1** |

Revert: set `activeBalancePatch` back to `7-30-26` (and restore Goblin/starter weapon rows from git if desired). Higher-level curve is **not** rebalanced yet under this patch.

The older Workbench goals config **12–15 turns per combatant** (`balanceTuningGoals.combatDuration`) is separate from these Lab checklist bands.

| Surface | Use for |
|---------|---------|
| **Action Lab Balance** | Fast feel loop, one knob at a time, live scenario |
| **Balance Tuning Workbench / CLI** | Profile campaign (`TUNESIM` / playthrough), batch analyze |

Playthrough check deep-links Workbench to `class-playthrough-balance`.


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

- **Action Lab Balance Layers**: per-layer checklists (see table above); shared tempo anchor is median combined **27** with level-scaled tolerance.
- **Workbench / goals config** per-combatant optimal: **12–15 turns** (`balanceTuningGoals.combatDuration`) — used by older validators, not the Lab PASS/FAIL recipe.

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
