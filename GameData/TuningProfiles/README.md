# Balance Tuning Profiles

Profiles define **what to simulate** and **what to analyze** for the three-step tuning workflow.

Location: `GameData/TuningProfiles/*.json`

## Commands

```bash
dotnet run --project Code/Code.csproj -- TUNEPROFILES
dotnet run --project Code/Code.csproj -- TUNESIM --profile combat-dials
dotnet run --project Code/Code.csproj -- TUNEANALYZE
dotnet run --project Code/Code.csproj -- TUNEAPPLY
dotnet run --project Code/Code.csproj -- TUNETUNING --profile combat-dials --max-iterations 5
```

Legacy level-curve aliases still work: `LEVELSIM`, `LEVELANALYZE`, `LEVELAPPLY`, `LEVELTUNING`.

### Overrides (optional)

```bash
TUNESIM --profile level-curve --battles 10 --levels 1,5,10
TUNESIM --profile combat-dials --continue-past-zero-hp --encounters 500
TUNESIM --profile midgame-balance --player-level 15 --enemy-level 15
```

Session file: `GameData/BalancePatches/balance-tuning-session.json`

## Four-dial routing

Set `"enableDialRouting": true` or include `"dial_routed"` in suggesters. Analysis classifies the primary failing dial:

| Dial | Suggesters used |
|------|-----------------|
| Power | global, duration, player, enemy_baseline |
| Variance | variance, weapon |
| Agency | agency, player |
| Scaling | level_curve, enemy, dungeon_scaling |

## Simulation modes

| Mode | Description |
|------|-------------|
| `multi_level_weapon_enemy` | Weapon×enemy matrix at each level in `levels` |
| `comprehensive_weapon_enemy` | Single weapon×enemy matrix at `playerLevel` / `enemyLevel` |
| `fundamentals_encounter` | No-gear lab encounters; action count + combo+ chain stats |
| `class_build_matrix` | Comprehensive weapon×enemy (class path parity) |
| `dungeon_scaling` | Comprehensive + dungeon scaling suggesters |

## Validators

| Id | Checks |
|----|--------|
| `level_curve` | Same-level win rate vs level-indexed curve |
| `comprehensive` | Full legacy balance validation |
| `win_rate` | Overall win rate in 85–98% band |
| `combat_duration` | Turns in configured min/max range (optimal 12–15 per combatant) |
| `weapon_variance` | Spread between best and worst weapon |
| `enemy_differentiation` | Enemies not too similar |
| `fundamentals_tempo` | Hero/enemy turns per encounter |
| `fundamentals_combo_streaks` | Combo+ chain frequency |
| `dial_variance` | Turn std dev and miss rate |
| `dial_agency` | Loss severity in developer sim mode |

## Suggesters

| Id | Adjusts |
|----|---------|
| `dial_routed` | Routes to dial-specific suggesters (use with enableDialRouting) |
| `level_curve` | Enemy scaling / progression for level curve |
| `global` | Global enemy multipliers |
| `player` | Player base stats |
| `enemy_baseline` | Enemy baseline stats |
| `weapon` | Weapon damage multipliers |
| `enemy` | Per-enemy tuning |
| `duration` | Combat length knobs |
| `variance` | Roll feel / variance compression |
| `agency` | TEC/INT reliability knobs |
| `dungeon_scaling` | Room count, enemy density |

## Built-in profiles

| Profile | Use when |
|---------|----------|
| `combat-dials` | Primary four-dial tuning loop |
| `level-curve` | Duration + scaling across levels |
| `combat-fundamentals` | Quick fundamentals + dial routing |
| `class-build-matrix` | Weapon path parity |
| `dungeon-scaling` | Dungeon length/density |
| `enemy-roster` | Enemy differentiation |
| `gear-probability` | Weapon variance |
| `environment-hazards` | Tempo under hazards |
| `midgame-balance` | Level 10 full-matrix balance |
| `earlygame-balance` | Level 5 onboarding |
| `weapon-focus` | Weapon parity only at level 10 |

Copy a built-in profile and edit `validators` / `suggesters` to narrow scope.
