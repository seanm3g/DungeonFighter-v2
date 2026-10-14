# Build Ladder Report — overnight pass 2026-10-12

How much each stepping stone of a build is worth, measured with the new **BUILDLAB** tool on patch `overnight-v2`.
Every number is a multiplier against the same hero one step earlier or at step 0. Kill speed = enemy health bars
removed per unit of game time (counts hero speed). HP lost ÷ = how much less HP the hero loses per fight.

## The stepping stones (measured, 23 BUILD_CATALOG builds, overnight-v2)

Kill-speed multiplier of each step over the step before it: median (10th–90th percentile across builds).

| Stone | L10 | L30–L90 | Notes |
|---|---|---|---|
| Fill the strip (default 1 action → 4–5 with class actions) | ×1.04 (0.98–1.16) | **×2.36** (1.59–3.20) | Biggest single step once the hero has slots. Balance targets are calibrated on the 1-action hero. |
| 2 pieces of a material + its convert | ×1.13 (1.02–1.30) | ×1.18 (0.68–1.86) | Convert pays % of the hit per banked currency. |
| Full set (3+2 or 5) + piece triggers | ×1.13 (1.08–1.45) | ×1.16 (0.97–2.22) | One trigger per piece; each piece ×0.85–1.30. |
| Weapon mods from the build card | ×1.02 (1.00–1.28) | ×1.00 (1.00–1.29) | Only the damage / DoT mods move kills (Heavy, Brutal, Flaming, Poisonous …). |
| Catalog skills (the card's branch) | ×1.10 (1.01–1.24) | ×1.11 (0.99–1.32) | |
| Core & Rite (kinship, synthesis, caps) | ×1.04 (0.96–1.13) | ×1.02 (0.97–1.13) | Mostly stat nodes; small by design. |
| **Whole build over the plain class strip** | **×1.59** (1.38–2.35) | **×1.81** (1.13–3.32) | Set + mods ×1.41–1.45, skills ×1.13–1.14. |
| Reference: gear 10 levels higher | ×1.66 (1.30–1.84) | ×1.18 (0.98–1.48) | A full build ≈ 1–3 gear upgrades. |

Defense (HP lost per fight, normal fights) improves along the same ladder: most builds go from 50–70% HP lost on the
class strip to 25–45% fully built.


## Balance check (overnight-v2, basic + smart bot)

```
Bot     Barbarian  Warrior    Rogue      Wizard     L10 clear     
basic   98%        95%        98%        95%        93.2%         
smart   100%       100%       100%       100%       97.8%         
(class columns: % of runs reaching L10)

Check                  Value  Target       Result
-----                  -----  ------       ------
L1 fight win rate      100.0% 100.0% +/- 4 PASS  
L1 hero turns          11.1   11 +/- 1.5   PASS  
L5 fight win rate      100.0% 99.6% +/- 4  PASS  
L5 hero turns          11.3   11 +/- 1.5   PASS  
L10 fight win rate     99.3%  99.1% +/- 4  PASS  
L10 hero turns         11.1   11 +/- 1.5   PASS  
L20 fight win rate     95.8%  97.1% +/- 4  PASS  
L20 hero turns         11.5   11 +/- 1.5   PASS  
L35 fight win rate     91.5%  93.6% +/- 4  PASS  
L35 hero turns         11.5   11 +/- 1.5   PASS  
L50 fight win rate     86.6%  89.4% +/- 4  PASS  
L50 hero turns         11.1   11 +/- 1.5   PASS  
L75 fight win rate     76.4%  79.2% +/- 4  PASS  
L100 fight win rate    1.2%   0.0% +/- 4   PASS  
Survival to L10        97%    >= 97%       PASS  
L10 real dungeon clear 93.2%  95.0% +/- 4  PASS  
Rare drops at L1       2.3%   1.5-3.5%     PASS  
Rare drops at L50      2.4%   1.5-3.5%     PASS  



18 of 18 checks passed. Logs: C:\Users\Seanye\AppData\Local\Temp\df-balance-check-ac8f8715
```

## Build power at a glance

Each cell: **strip** (class strip vs 1-action default) / **build** (set + mods + skills + rite vs class strip), kill-speed multipliers from long fights.

| Build | L10 | L30 | L60 | L90 |
|---|---|---|---|---|
| B1 Pain Engine | 0.90 / 1.59 | 2.05 / 1.82 | 2.36 / 1.59 | 3.00 / 1.28 |
| B2 Grudge Bearer | 1.01 / 1.30 | 2.51 / 1.32 | 2.32 / 1.37 | 2.80 / 1.15 |
| B3 Executioner | 1.13 / 1.67 | 2.04 / 1.57 | 2.05 / 1.56 | 2.35 / 1.81 |
| B4 Empty Fury | 1.04 / 1.46 | 1.72 / 1.55 | 1.80 / 1.50 | 2.05 / 1.51 |
| B5 Triumph Cadence | 1.28 / 1.52 | 2.88 / 1.41 | 3.08 / 1.13 | 4.04 / 1.18 |
| B6 Toxic Bludgeon | 1.07 / 1.75 | 1.92 / 1.48 | 1.98 / 1.22 | 2.32 / 1.30 |
| W1 War Drum | 1.08 / 1.75 | 1.95 / 2.57 | 2.73 / 2.20 | 2.42 / 1.98 |
| W2 Drill Sergeant | 1.08 / 1.59 | 1.84 / 2.45 | 2.33 / 1.97 | 2.38 / 1.89 |
| W3 Anchor Line | 1.02 / 1.58 | 1.86 / 3.32 | 2.26 / 2.83 | 2.29 / 2.55 |
| W4 Guardian | 1.03 / 1.58 | 2.28 / 2.26 | 3.36 / 1.86 | 3.12 / 1.84 |
| W5 Unbroken Streak | 1.03 / 1.66 | 1.98 / 2.84 | 2.61 / 2.11 | 2.71 / 1.84 |
| R1 Glass Cannon | 1.12 / 2.35 | 1.31 / 2.17 | 2.76 / 1.39 | 3.20 / 1.42 |
| R2 Marked Hunter | 1.11 / 1.22 | 1.17 / 0.74 | 2.78 / 0.56 | 3.54 / 0.46 |
| R3 Headhunter | 1.16 / 2.31 | 1.08 / 2.55 | 2.20 / 1.67 | 2.55 / 1.72 |
| R4 Toxic Ledger | 1.03 / 3.54 | 0.72 / 4.01 | 1.59 / 2.64 | 1.68 / 2.96 |
| R5 Shadow Gambler | 1.02 / 2.17 | 0.95 / 4.46 | 2.01 / 2.55 | 2.47 / 2.43 |
| R6 Dice Thief | 0.97 / 4.84 | 1.40 / 4.19 | 2.64 / 4.38 | 3.16 / 4.36 |
| Z1 Chain Weaver | 1.35 / 2.32 | 3.82 / 3.50 | 6.32 / 1.66 | 6.82 / 1.68 |
| Z2 Empty Glyph | 1.10 / 1.42 | 2.54 / 1.22 | 2.52 / 1.19 | 2.79 / 1.13 |
| Z3 Lens Sage | 0.98 / 1.38 | 2.08 / 1.53 | 2.44 / 1.02 | 2.63 / 1.03 |
| Z4 Probability Engine | 1.02 / 1.49 | 2.51 / 1.20 | 2.68 / 1.18 | 3.11 / 1.25 |
| Z5 Elementalist | 1.15 / 1.64 | 1.92 / 3.21 | 2.47 / 2.81 | 2.55 / 2.90 |
| Z6 Golem Mind | 1.03 / 1.42 | 1.76 / 2.31 | 2.05 / 2.49 | 2.09 / 2.37 |

## Ladders: class builds (BUILD_CATALOG)

Kill speed × vs step 0 (long fights); HP lost ÷ vs step 0 (normal fights, >1 = loses less HP).

### B1 · Pain Engine

| Step | L10 kill speed × | L30 kill speed × | L60 kill speed × | L90 kill speed × | L10 HP lost ÷ | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip | 0.90 | 2.05 | 2.36 | 3.00 | 0.82 | 1.60 | 1.95 | 1.32 |
| 2 2 Bone + convert | 1.13 | 2.49 | 2.68 | 3.00 | 1.31 | 1.74 | 1.94 | 1.35 |
| 3 full set Bone5 | 1.40 | 3.19 | 2.87 | 3.48 | 1.45 | 1.86 | 1.96 | 1.42 |
| 4 +weapon mods Reinforced/Hardened/Sturdy | 1.40 | 3.19 | 2.87 | 3.48 | 1.45 | 1.86 | 1.96 | 1.42 |
| 5 +catalog skills | 1.39 | 3.85 | 3.76 | 3.72 | 2.00 | 2.51 | 2.87 | 1.64 |
| 6 +core & rite | 1.42 | 3.72 | 3.75 | 3.83 | 2.04 | 2.59 | 2.93 | 1.71 |
| 7 +gear 10 levels up | 2.09 | 5.10 | 4.09 | 4.72 | 1.28 | 2.26 | 2.84 | 1.70 |

### B2 · Grudge Bearer

| Step | L10 kill speed × | L30 kill speed × | L60 kill speed × | L90 kill speed × | L10 HP lost ÷ | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip | 1.01 | 2.51 | 2.32 | 2.80 | 0.96 | 1.70 | 1.98 | 1.38 |
| 2 2 Bone + convert | 1.16 | 2.58 | 2.73 | 3.05 | 1.34 | 2.06 | 2.34 | 1.60 |
| 3 full set Bone3+Steel2 | 1.30 | 3.58 | 2.80 | 3.19 | 1.72 | 2.55 | 2.80 | 1.68 |
| 4 +weapon mods Reinforced/Sturdy | 1.30 | 3.58 | 2.80 | 3.19 | 1.72 | 2.55 | 2.80 | 1.68 |
| 5 +catalog skills | 1.38 | 3.45 | 3.11 | 3.33 | 2.04 | 2.74 | 3.06 | 1.88 |
| 6 +core & rite | 1.31 | 3.32 | 3.17 | 3.23 | 1.94 | 2.79 | 3.18 | 2.02 |
| 7 +gear 10 levels up | 2.10 | 5.19 | 3.63 | 3.89 | 1.26 | 2.45 | 2.84 | 1.84 |

### B3 · Executioner

| Step | L10 kill speed × | L30 kill speed × | L60 kill speed × | L90 kill speed × | L10 HP lost ÷ | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip | 1.13 | 2.04 | 2.05 | 2.35 | 1.11 | 1.69 | 2.06 | 1.45 |
| 2 2 Iron + convert | 1.30 | 2.46 | 2.48 | 2.92 | 1.18 | 1.82 | 1.75 | 1.42 |
| 3 full set Iron5 | 1.71 | 2.98 | 2.88 | 3.30 | 1.51 | 1.93 | 1.99 | 1.45 |
| 4 +weapon mods Heavy/Brutal/Long | 1.71 | 2.98 | 2.88 | 3.30 | 1.52 | 1.93 | 1.99 | 1.45 |
| 5 +catalog skills | 1.86 | 3.21 | 2.98 | 4.34 | 1.70 | 2.07 | 2.17 | 1.54 |
| 6 +core & rite | 1.89 | 3.19 | 3.21 | 4.25 | 1.74 | 2.06 | 2.18 | 1.55 |
| 7 +gear 10 levels up | 2.82 | 5.37 | 3.80 | 4.72 | 1.19 | 2.05 | 2.21 | 1.58 |

### B4 · Empty Fury

| Step | L10 kill speed × | L30 kill speed × | L60 kill speed × | L90 kill speed × | L10 HP lost ÷ | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip | 1.04 | 1.72 | 1.80 | 2.05 | 1.05 | 1.35 | 1.55 | 1.18 |
| 2 2 Iron + convert | 1.24 | 2.10 | 2.02 | 2.43 | 1.20 | 1.52 | 1.55 | 1.24 |
| 3 full set Iron3+Bone2 | 1.39 | 2.54 | 2.62 | 2.89 | 1.26 | 1.70 | 1.96 | 1.37 |
| 4 +weapon mods Heavy/Brutal | 1.39 | 2.54 | 2.62 | 2.89 | 1.26 | 1.70 | 1.96 | 1.37 |
| 5 +catalog skills | 1.41 | 2.50 | 2.57 | 3.06 | 1.24 | 1.75 | 2.02 | 1.35 |
| 6 +core & rite | 1.52 | 2.66 | 2.69 | 3.10 | 1.31 | 1.85 | 2.05 | 1.35 |
| 7 +gear 10 levels up | 2.02 | 3.95 | 3.23 | 3.33 | 1.16 | 1.98 | 2.11 | 1.38 |

### B5 · Triumph Cadence

| Step | L10 kill speed × | L30 kill speed × | L60 kill speed × | L90 kill speed × | L10 HP lost ÷ | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip | 1.28 | 2.88 | 3.08 | 4.04 | 1.09 | 2.01 | 2.19 | 1.49 |
| 2 2 Steel + convert | 1.31 | 2.44 | 2.11 | 2.91 | 1.12 | 1.54 | 1.63 | 1.30 |
| 3 full set Steel5 | 1.48 | 2.55 | 2.16 | 3.01 | 1.21 | 1.61 | 1.71 | 1.34 |
| 4 +weapon mods Balanced/Precise | 1.91 | 3.62 | 2.93 | 3.89 | 1.39 | 2.09 | 2.30 | 1.53 |
| 5 +catalog skills | 2.04 | 3.77 | 3.07 | 4.42 | 1.59 | 2.39 | 2.54 | 1.94 |
| 6 +core & rite | 1.95 | 4.06 | 3.47 | 4.75 | 1.50 | 2.39 | 2.66 | 2.02 |
| 7 +gear 10 levels up | 3.10 | 6.65 | 4.64 | 4.54 | 1.23 | 2.36 | 2.66 | 1.70 |

### B6 · Toxic Bludgeon

| Step | L10 kill speed × | L30 kill speed × | L60 kill speed × | L90 kill speed × | L10 HP lost ÷ | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip | 1.07 | 1.92 | 1.98 | 2.32 | 1.01 | 1.39 | 1.75 | 1.35 |
| 2 2 Iron + convert | 1.32 | 2.32 | 2.31 | 2.72 | 1.15 | 1.62 | 1.65 | 1.33 |
| 3 full set Iron3+Steel2 | 1.43 | 2.10 | 2.11 | 2.40 | 1.19 | 1.46 | 1.57 | 1.21 |
| 4 +weapon mods Poisonous/serrated | 1.67 | 2.49 | 2.28 | 2.84 | 1.32 | 1.54 | 1.63 | 1.24 |
| 5 +catalog skills | 1.92 | 2.95 | 2.34 | 2.73 | 1.50 | 1.71 | 1.95 | 1.36 |
| 6 +core & rite | 1.88 | 2.84 | 2.43 | 3.01 | 1.57 | 1.73 | 1.89 | 1.33 |
| 7 +gear 10 levels up | 2.41 | 3.96 | 3.28 | 3.13 | 1.20 | 1.93 | 2.05 | 1.31 |

### W1 · War Drum

| Step | L10 kill speed × | L30 kill speed × | L60 kill speed × | L90 kill speed × | L10 HP lost ÷ | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip | 1.08 | 1.95 | 2.73 | 2.42 | 1.05 | 1.55 | 1.58 | 1.40 |
| 2 2 Bronze + convert | 1.16 | 2.70 | 3.30 | 2.90 | 1.06 | 1.70 | 1.72 | 1.36 |
| 3 full set Bronze5 | 1.30 | 3.61 | 3.94 | 3.49 | 1.24 | 1.95 | 2.07 | 1.56 |
| 4 +weapon mods Featherweight/Nimble/Swift | 1.39 | 3.61 | 3.94 | 3.49 | 1.43 | 1.95 | 2.07 | 1.56 |
| 5 +catalog skills | 1.99 | 4.89 | 5.83 | 5.00 | 1.63 | 2.49 | 2.81 | 1.76 |
| 6 +core & rite | 1.90 | 5.02 | 5.99 | 4.79 | 1.62 | 2.82 | 2.88 | 1.75 |
| 7 +gear 10 levels up | 3.04 | 6.71 | 6.87 | 5.17 | 1.33 | 2.22 | 2.51 | 1.65 |

### W2 · Drill Sergeant

| Step | L10 kill speed × | L30 kill speed × | L60 kill speed × | L90 kill speed × | L10 HP lost ÷ | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip | 1.08 | 1.84 | 2.33 | 2.38 | 1.09 | 1.35 | 1.37 | 1.21 |
| 2 2 Bronze + convert | 1.13 | 1.97 | 2.31 | 2.43 | 1.11 | 1.45 | 1.51 | 1.24 |
| 3 full set Bronze3+Mithril2 | 1.25 | 2.73 | 2.58 | 2.46 | 1.22 | 1.84 | 1.62 | 1.30 |
| 4 +weapon mods Swift/Balanced/Precise | 1.51 | 3.15 | 3.31 | 3.40 | 1.42 | 2.09 | 1.99 | 1.51 |
| 5 +catalog skills | 1.75 | 4.26 | 4.63 | 4.40 | 1.52 | 2.74 | 2.44 | 1.97 |
| 6 +core & rite | 1.71 | 4.51 | 4.59 | 4.50 | 1.54 | 2.94 | 2.50 | 1.99 |
| 7 +gear 10 levels up | 2.96 | 6.34 | 6.25 | 3.78 | 1.31 | 2.42 | 2.66 | 1.53 |

### W3 · Anchor Line

| Step | L10 kill speed × | L30 kill speed × | L60 kill speed × | L90 kill speed × | L10 HP lost ÷ | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip | 1.02 | 1.86 | 2.26 | 2.29 | 1.03 | 1.69 | 1.82 | 1.38 |
| 2 2 Gold + convert | 1.15 | 2.73 | 3.15 | 3.09 | 1.36 | 2.07 | 1.89 | 1.37 |
| 3 full set Gold3+Mithril2 | 1.43 | 5.32 | 7.00 | 5.21 | 1.57 | 3.51 | 3.03 | 1.94 |
| 4 +weapon mods Reinforced/Hardened | 1.43 | 5.32 | 7.00 | 5.21 | 1.57 | 3.51 | 3.03 | 1.94 |
| 5 +catalog skills | 1.51 | 5.64 | 6.41 | 5.85 | 2.23 | 4.33 | 3.54 | 2.28 |
| 6 +core & rite | 1.61 | 6.19 | 6.39 | 5.82 | 2.38 | 4.52 | 3.54 | 2.30 |
| 7 +gear 10 levels up | 2.81 | 7.71 | 7.48 | 4.80 | 1.66 | 2.71 | 3.03 | 1.97 |

### W4 · Guardian

| Step | L10 kill speed × | L30 kill speed × | L60 kill speed × | L90 kill speed × | L10 HP lost ÷ | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip | 1.03 | 2.28 | 3.36 | 3.12 | 1.00 | 1.67 | 1.76 | 1.46 |
| 2 2 Leather + convert | 0.99 | 1.55 | 1.81 | 1.90 | 0.98 | 1.20 | 1.23 | 1.20 |
| 3 full set Gold2+Leather3 | 1.44 | 4.57 | 5.75 | 5.34 | 2.11 | 3.44 | 3.41 | 2.42 |
| 4 +weapon mods Reinforced/Hardened/Sturdy | 1.44 | 4.57 | 5.75 | 5.34 | 2.11 | 3.44 | 3.41 | 2.42 |
| 5 +catalog skills | 1.51 | 4.70 | 6.21 | 5.93 | 2.24 | 3.99 | 4.03 | 2.79 |
| 6 +core & rite | 1.63 | 5.15 | 6.26 | 5.76 | 2.32 | 4.39 | 3.90 | 2.72 |
| 7 +gear 10 levels up | 2.71 | 6.83 | 7.70 | 4.91 | 1.50 | 2.55 | 3.40 | 2.23 |

### W5 · Unbroken Streak

| Step | L10 kill speed × | L30 kill speed × | L60 kill speed × | L90 kill speed × | L10 HP lost ÷ | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip | 1.03 | 1.98 | 2.61 | 2.71 | 1.00 | 1.60 | 1.57 | 1.34 |
| 2 2 Mithril + convert | 1.21 | 3.69 | 4.77 | 4.54 | 1.17 | 2.33 | 2.44 | 1.71 |
| 3 full set Mithril3+Gold2 | 1.37 | 4.51 | 4.77 | 3.74 | 1.42 | 2.72 | 2.36 | 1.57 |
| 4 +weapon mods Swift/Short | 1.46 | 4.51 | 4.77 | 3.74 | 1.69 | 2.72 | 2.36 | 1.57 |
| 5 +catalog skills | 1.60 | 5.32 | 5.48 | 4.46 | 1.90 | 3.02 | 2.76 | 1.84 |
| 6 +core & rite | 1.71 | 5.63 | 5.52 | 4.98 | 1.82 | 3.47 | 2.79 | 1.90 |
| 7 +gear 10 levels up | 2.86 | 7.63 | 6.66 | 4.38 | 1.43 | 2.39 | 2.53 | 1.64 |

### R1 · Glass Cannon

| Step | L10 kill speed × | L30 kill speed × | L60 kill speed × | L90 kill speed × | L10 HP lost ÷ | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip | 1.12 | 1.31 | 2.76 | 3.20 | 1.02 | 1.39 | 1.69 | 1.26 |
| 2 2 Glass + convert | 1.26 | 1.78 | 3.15 | 3.61 | 1.12 | 1.45 | 1.69 | 1.22 |
| 3 full set Glass5 | 2.12 | 2.46 | 3.58 | 4.37 | 1.25 | 1.61 | 1.77 | 1.24 |
| 4 +weapon mods Light/Acrobatic/Featherweight | 2.12 | 2.46 | 3.58 | 4.37 | 1.25 | 1.61 | 1.77 | 1.24 |
| 5 +catalog skills | 2.33 | 2.73 | 3.84 | 4.50 | 1.37 | 1.67 | 1.80 | 1.25 |
| 6 +core & rite | 2.62 | 2.84 | 3.84 | 4.54 | 1.51 | 1.87 | 1.80 | 1.25 |
| 7 +gear 10 levels up | 3.41 | 3.68 | 3.82 | 4.95 | 1.07 | 1.87 | 1.95 | 1.30 |

### R2 · Marked Hunter

| Step | L10 kill speed × | L30 kill speed × | L60 kill speed × | L90 kill speed × | L10 HP lost ÷ | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip | 1.11 | 1.17 | 2.78 | 3.54 | 1.10 | 1.44 | 2.37 | 1.37 |
| 2 2 Glass + convert | 1.02 | 0.60 | 1.17 | 1.20 | 0.94 | 0.91 | 1.05 | 1.00 |
| 3 full set Glass3+Obsidian2 | 1.10 | 0.72 | 1.33 | 1.29 | 1.04 | 1.05 | 1.14 | 1.00 |
| 4 +weapon mods Long/Charged | 1.23 | 0.78 | 1.52 | 1.45 | 1.12 | 1.12 | 1.19 | 1.00 |
| 5 +catalog skills | 1.37 | 0.87 | 1.57 | 1.64 | 1.24 | 1.18 | 1.27 | 1.01 |
| 6 +core & rite | 1.35 | 0.86 | 1.57 | 1.64 | 1.33 | 1.17 | 1.27 | 1.01 |
| 7 +gear 10 levels up | 2.14 | 1.24 | 1.70 | 2.08 | 1.08 | 1.32 | 1.48 | 1.06 |

### R3 · Headhunter

| Step | L10 kill speed × | L30 kill speed × | L60 kill speed × | L90 kill speed × | L10 HP lost ÷ | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip | 1.16 | 1.08 | 2.20 | 2.55 | 1.08 | 1.23 | 1.59 | 1.14 |
| 2 2 Obsidian + convert | 1.98 | 1.58 | 2.69 | 3.51 | 1.27 | 1.43 | 1.76 | 1.20 |
| 3 full set Obsidian5 | 2.20 | 2.13 | 3.12 | 3.87 | 1.45 | 1.93 | 1.81 | 1.22 |
| 4 +weapon mods serrated | 2.23 | 2.25 | 3.28 | 3.81 | 1.46 | 1.98 | 1.81 | 1.22 |
| 5 +catalog skills | 2.52 | 2.41 | 3.66 | 4.37 | 1.62 | 2.09 | 1.94 | 1.24 |
| 6 +core & rite | 2.69 | 2.74 | 3.66 | 4.37 | 1.66 | 2.14 | 1.94 | 1.24 |
| 7 +gear 10 levels up | 3.69 | 3.41 | 3.83 | 4.87 | 1.13 | 1.79 | 2.03 | 1.34 |

### R4 · Toxic Ledger

| Step | L10 kill speed × | L30 kill speed × | L60 kill speed × | L90 kill speed × | L10 HP lost ÷ | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip | 1.03 | 0.72 | 1.59 | 1.68 | 0.96 | 0.97 | 1.19 | 1.00 |
| 2 2 Obsidian + convert | 2.02 | 1.48 | 2.92 | 3.49 | 1.16 | 1.44 | 1.73 | 1.17 |
| 3 full set Obsidian3+Shadow2 | 2.46 | 2.01 | 3.11 | 3.80 | 1.49 | 2.05 | 1.75 | 1.17 |
| 4 +weapon mods Poisonous/serrated/Flaming | 2.76 | 2.33 | 3.43 | 3.89 | 1.55 | 2.04 | 1.83 | 1.17 |
| 5 +catalog skills | 3.31 | 2.70 | 4.05 | 4.59 | 1.78 | 2.20 | 1.95 | 1.17 |
| 6 +core & rite | 3.63 | 2.89 | 4.18 | 4.98 | 1.77 | 2.20 | 1.95 | 1.17 |
| 7 +gear 10 levels up | 4.42 | 3.34 | 4.72 | 6.00 | 1.18 | 1.72 | 1.90 | 1.27 |

### R5 · Shadow Gambler

| Step | L10 kill speed × | L30 kill speed × | L60 kill speed × | L90 kill speed × | L10 HP lost ÷ | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip | 1.02 | 0.95 | 2.01 | 2.47 | 0.90 | 1.25 | 1.43 | 1.19 |
| 2 2 Shadow + convert | 1.33 | 2.45 | 4.31 | 5.27 | 1.22 | 2.13 | 2.52 | 1.41 |
| 3 full set Shadow5 | 1.68 | 3.38 | 4.73 | 5.34 | 1.30 | 2.50 | 2.46 | 1.45 |
| 4 +weapon mods  | 1.68 | 3.38 | 4.73 | 5.34 | 1.30 | 2.50 | 2.46 | 1.45 |
| 5 +catalog skills | 2.00 | 3.83 | 5.13 | 5.99 | 1.47 | 2.83 | 2.85 | 1.51 |
| 6 +core & rite | 2.22 | 4.22 | 5.13 | 6.00 | 1.53 | 2.87 | 2.85 | 1.51 |
| 7 +gear 10 levels up | 4.04 | 4.16 | 5.11 | 8.23 | 1.18 | 1.95 | 2.19 | 1.79 |

### R6 · Dice Thief

| Step | L10 kill speed × | L30 kill speed × | L60 kill speed × | L90 kill speed × | L10 HP lost ÷ | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip | 0.97 | 1.40 | 2.64 | 3.16 | 0.90 | 1.63 | 1.93 | 1.39 |
| 2 2 Leather + convert | 0.99 | 1.05 | 2.04 | 2.23 | 0.97 | 1.25 | 1.37 | 1.16 |
| 3 full set Shadow2+Leather3 | 2.62 | 3.94 | 8.45 | 8.87 | 1.69 | 3.64 | 4.41 | 2.43 |
| 4 +weapon mods Long/Charged/Swift | 2.90 | 3.74 | 9.17 | 9.44 | 2.14 | 5.10 | 6.38 | 2.72 |
| 5 +catalog skills | 3.60 | 4.77 | 11.55 | 13.78 | 2.45 | 5.10 | 6.45 | 2.93 |
| 6 +core & rite | 4.69 | 5.88 | 11.55 | 13.78 | 3.03 | 5.19 | 6.45 | 2.93 |
| 7 +gear 10 levels up | 8.10 | 8.68 | 12.79 | 23.10 | 1.38 | 2.46 | 3.51 | 3.06 |

### Z1 · Chain Weaver

| Step | L10 kill speed × | L30 kill speed × | L60 kill speed × | L90 kill speed × | L10 HP lost ÷ | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip | 1.35 | 3.82 | 6.32 | 6.82 | 1.32 | 2.91 | 4.17 | 2.53 |
| 2 2 Willow + convert | 1.50 | 4.71 | 6.78 | 7.28 | 1.22 | 2.36 | 3.07 | 1.70 |
| 3 full set Willow5 | 1.69 | 7.41 | 6.55 | 7.47 | 1.34 | 2.33 | 3.00 | 1.73 |
| 4 +weapon mods Balanced/Precise/Keen | 2.18 | 10.64 | 9.03 | 10.71 | 1.27 | 3.41 | 4.91 | 2.46 |
| 5 +catalog skills | 3.31 | 13.38 | 9.95 | 11.81 | 1.61 | 3.66 | 5.10 | 3.01 |
| 6 +core & rite | 3.14 | 13.38 | 10.50 | 11.44 | 1.72 | 3.67 | 5.30 | 3.09 |
| 7 +gear 10 levels up | 6.71 | 10.60 | 12.84 | 13.44 | 1.38 | 2.49 | 3.71 | 2.87 |

### Z2 · Empty Glyph

| Step | L10 kill speed × | L30 kill speed × | L60 kill speed × | L90 kill speed × | L10 HP lost ÷ | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip | 1.10 | 2.54 | 2.52 | 2.79 | 0.94 | 1.44 | 1.57 | 1.22 |
| 2 2 Willow + convert | 1.16 | 2.49 | 2.58 | 2.74 | 0.94 | 1.38 | 1.64 | 1.30 |
| 3 full set Willow3+Crystal2 | 1.21 | 2.58 | 2.57 | 2.80 | 1.09 | 1.44 | 1.64 | 1.30 |
| 4 +weapon mods Keen/refined | 1.21 | 2.58 | 2.57 | 2.80 | 1.09 | 1.44 | 1.64 | 1.30 |
| 5 +catalog skills | 1.36 | 3.11 | 2.79 | 3.25 | 1.20 | 1.60 | 1.76 | 1.38 |
| 6 +core & rite | 1.56 | 3.09 | 3.01 | 3.14 | 1.25 | 1.57 | 1.73 | 1.36 |
| 7 +gear 10 levels up | 2.60 | 3.46 | 3.75 | 3.56 | 1.20 | 1.51 | 1.81 | 1.38 |

### Z3 · Lens Sage

| Step | L10 kill speed × | L30 kill speed × | L60 kill speed × | L90 kill speed × | L10 HP lost ÷ | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip | 0.98 | 2.08 | 2.44 | 2.63 | 0.85 | 1.60 | 1.61 | 1.20 |
| 2 2 Silver + convert | 1.12 | 2.86 | 2.49 | 3.03 | 1.01 | 1.42 | 1.65 | 1.34 |
| 3 full set Silver5 | 1.25 | 2.62 | 2.41 | 3.02 | 1.13 | 1.46 | 1.57 | 1.38 |
| 4 +weapon mods  | 1.25 | 2.62 | 2.41 | 3.02 | 1.13 | 1.46 | 1.57 | 1.38 |
| 5 +catalog skills | 1.32 | 2.76 | 2.42 | 3.00 | 1.13 | 1.46 | 1.57 | 1.38 |
| 6 +core & rite | 1.35 | 3.17 | 2.48 | 2.70 | 1.23 | 1.44 | 1.69 | 1.38 |
| 7 +gear 10 levels up | 2.48 | 3.11 | 3.91 | 3.28 | 1.21 | 1.47 | 1.78 | 1.39 |

### Z4 · Probability Engine

| Step | L10 kill speed × | L30 kill speed × | L60 kill speed × | L90 kill speed × | L10 HP lost ÷ | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip | 1.02 | 2.51 | 2.68 | 3.11 | 0.99 | 1.54 | 1.59 | 1.22 |
| 2 2 Silver + convert | 1.14 | 2.86 | 2.83 | 3.23 | 0.96 | 1.57 | 1.70 | 1.22 |
| 3 full set Silver3+Willow2 | 1.24 | 2.92 | 3.47 | 3.93 | 1.22 | 1.63 | 1.61 | 1.21 |
| 4 +weapon mods Swift/Short | 1.36 | 2.92 | 3.47 | 3.93 | 1.18 | 1.63 | 1.61 | 1.21 |
| 5 +catalog skills | 1.36 | 2.94 | 3.54 | 3.67 | 1.30 | 1.71 | 1.95 | 1.24 |
| 6 +core & rite | 1.53 | 3.01 | 3.17 | 3.88 | 1.28 | 1.67 | 1.85 | 1.24 |
| 7 +gear 10 levels up | 2.56 | 4.16 | 4.78 | 4.19 | 1.17 | 1.60 | 2.25 | 1.34 |

### Z5 · Elementalist

| Step | L10 kill speed × | L30 kill speed × | L60 kill speed × | L90 kill speed × | L10 HP lost ÷ | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip | 1.15 | 1.92 | 2.47 | 2.55 | 0.98 | 1.42 | 1.77 | 1.12 |
| 2 2 Crystal + convert | 1.33 | 3.59 | 4.27 | 4.44 | 1.18 | 1.98 | 2.17 | 1.35 |
| 3 full set Crystal5 | 1.42 | 3.95 | 4.52 | 4.77 | 1.46 | 1.94 | 2.38 | 1.32 |
| 4 +weapon mods Flaming/Keen | 1.81 | 4.70 | 5.01 | 4.95 | 1.59 | 2.31 | 2.34 | 1.35 |
| 5 +catalog skills | 1.89 | 6.09 | 6.10 | 6.54 | 1.70 | 2.52 | 2.59 | 1.40 |
| 6 +core & rite | 1.90 | 6.17 | 6.93 | 7.38 | 1.72 | 2.58 | 2.50 | 1.53 |
| 7 +gear 10 levels up | 3.83 | 6.63 | 8.88 | 8.46 | 1.28 | 1.96 | 2.50 | 1.48 |

### Z6 · Golem Mind

| Step | L10 kill speed × | L30 kill speed × | L60 kill speed × | L90 kill speed × | L10 HP lost ÷ | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip | 1.03 | 1.76 | 2.05 | 2.09 | 0.85 | 1.33 | 1.49 | 1.06 |
| 2 2 Crystal + convert | 1.16 | 3.04 | 2.50 | 2.71 | 0.98 | 1.53 | 1.72 | 1.35 |
| 3 full set Crystal3+Silver2 | 1.35 | 3.95 | 4.35 | 4.43 | 1.01 | 1.63 | 1.86 | 1.37 |
| 4 +weapon mods Ancient/Sturdy | 1.35 | 3.95 | 4.35 | 4.43 | 1.01 | 1.63 | 1.87 | 1.37 |
| 5 +catalog skills | 1.40 | 3.99 | 4.37 | 5.12 | 1.27 | 1.84 | 1.99 | 1.46 |
| 6 +core & rite | 1.46 | 4.06 | 5.11 | 4.95 | 1.37 | 2.01 | 2.00 | 1.42 |
| 7 +gear 10 levels up | 2.55 | 4.52 | 5.31 | 5.66 | 1.27 | 1.67 | 2.04 | 1.35 |

## Ladders: hybrids

### HWB · Warbrute (Mace+Sword)

| Step | L30 kill speed × | L60 kill speed × | L90 kill speed × | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip (both parents) | 1.35 | 1.51 | 1.71 | 1.14 | 1.43 | 1.10 |
| 2 tag set Steel3+Bronze2 | 1.52 | 1.43 | 1.68 | 1.27 | 1.35 | 1.12 |
| 3 +hybrid root/attr/material | 1.52 | 1.43 | 1.68 | 1.27 | 1.35 | 1.12 |
| 4 +BODY BLOW (rank 3) | 1.78 | 1.84 | 2.49 | 1.40 | 1.44 | 1.29 |
| 5 +attr accumulation | 1.83 | 1.93 | 2.45 | 1.40 | 1.44 | 1.28 |
| 6 +WARBRUTE SMASH (rank 3) +tag accumulation | 2.28 | 2.06 | 3.03 | 1.57 | 1.78 | 1.40 |

### HRV · Ravager (Mace+Dagger)

| Step | L30 kill speed × | L60 kill speed × | L90 kill speed × | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip (both parents) | 1.70 | 1.72 | 1.97 | 1.31 | 1.53 | 1.11 |
| 2 tag set Shadow3+Iron2 | 3.80 | 2.96 | 3.65 | 2.39 | 2.43 | 1.49 |
| 3 +hybrid root/attr/material | 3.80 | 2.96 | 3.65 | 2.39 | 2.43 | 1.49 |
| 4 +PROWESS RUSH (rank 3) | 4.46 | 4.24 | 4.66 | 2.67 | 2.82 | 1.73 |
| 5 +attr accumulation | 4.56 | 4.23 | 5.10 | 2.67 | 2.82 | 1.73 |
| 6 +RAVAGER FLURRY (rank 3) +tag accumulation | 4.65 | 4.23 | 4.70 | 2.94 | 2.76 | 1.77 |

### HRC · Ragecaller (Mace+Wand)

| Step | L30 kill speed × | L60 kill speed × | L90 kill speed × | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip (both parents) | 1.44 | 1.58 | 1.66 | 1.27 | 1.41 | 1.08 |
| 2 tag set Crystal3+Bone2 | 2.51 | 2.52 | 2.63 | 1.81 | 2.18 | 1.33 |
| 3 +hybrid root/attr/material | 2.51 | 2.52 | 2.63 | 1.81 | 2.18 | 1.33 |
| 4 +WILLFIRE (rank 3) | 3.28 | 3.24 | 3.91 | 1.86 | 2.40 | 1.56 |
| 5 +attr accumulation | 3.28 | 3.24 | 3.97 | 1.89 | 2.40 | 1.56 |
| 6 +RAGECALLER HOWL (rank 3) +tag accumulation | 3.31 | 3.81 | 4.91 | 2.19 | 2.78 | 1.85 |

### HDU · Duelist (Sword+Dagger)

| Step | L30 kill speed × | L60 kill speed × | L90 kill speed × | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip (both parents) | 1.59 | 1.89 | 2.03 | 1.30 | 1.43 | 1.19 |
| 2 tag set Glass3+Mithril2 | 2.73 | 2.76 | 2.52 | 1.83 | 1.75 | 1.28 |
| 3 +hybrid root/attr/material | 2.73 | 2.76 | 2.52 | 1.83 | 1.75 | 1.28 |
| 4 +FINESSE THRUST (rank 3) | 3.57 | 3.78 | 3.46 | 2.14 | 2.03 | 1.45 |
| 5 +attr accumulation | 3.57 | 3.78 | 3.46 | 2.14 | 1.96 | 1.45 |
| 6 +DUELIST FEINT (rank 3) +tag accumulation | 3.51 | 3.78 | 3.26 | 2.06 | 2.12 | 1.67 |

### HSB · Spellblade (Sword+Wand)

| Step | L30 kill speed × | L60 kill speed × | L90 kill speed × | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip (both parents) | 2.13 | 2.89 | 2.68 | 1.54 | 1.70 | 1.59 |
| 2 tag set Willow3+Bronze2 | 2.44 | 3.15 | 3.09 | 1.70 | 2.02 | 1.70 |
| 3 +hybrid root/attr/material | 2.44 | 3.15 | 3.09 | 1.70 | 2.02 | 1.70 |
| 4 +WITS ARC (rank 3) | 3.29 | 3.97 | 3.62 | 1.87 | 2.47 | 1.70 |
| 5 +attr accumulation | 3.35 | 3.95 | 3.62 | 1.87 | 2.47 | 1.70 |
| 6 +SPELLBLADE WEAVE (rank 3) +tag accumulation | 2.43 | 3.07 | 3.32 | 1.66 | 2.07 | 1.71 |

### HHX · Hexblade (Dagger+Wand)

| Step | L30 kill speed × | L60 kill speed × | L90 kill speed × | L30 HP lost ÷ | L60 HP lost ÷ | L90 HP lost ÷ |
|---|---|---|---|---|---|---|
| 0 default strip | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| 1 class strip (both parents) | 0.84 | 1.69 | 1.98 | 1.06 | 1.34 | 1.02 |
| 2 tag set Crystal3+Obsidian2 | 1.35 | 2.45 | 2.72 | 1.49 | 1.80 | 1.19 |
| 3 +hybrid root/attr/material | 1.35 | 2.45 | 2.72 | 1.49 | 1.80 | 1.19 |
| 4 +MIND HEX (rank 3) | 2.15 | 3.50 | 4.01 | 2.00 | 2.19 | 1.41 |
| 5 +attr accumulation | 2.08 | 3.50 | 4.01 | 2.00 | 2.19 | 1.41 |
| 6 +HEXBLADE CURSE (rank 3) +tag accumulation | 2.05 | 3.41 | 4.30 | 2.40 | 2.96 | 1.84 |

## Material piece triggers (one piece, 3-action strip, L35, mean of 4 weapons)

Value = kill speed × HP-lost ratio vs the same piece with no trigger (1.00 = nothing).

| Top 15 | Value | Bottom 10 | Value |
|---|---|---|---|
| Gold Ingot | 1.30 | Bone Cheetah | 0.97 |
| Shadow ClockworkBeetle | 1.29 | Mithril Jackdaw | 0.97 |
| Wood Ant | 1.27 | Leather Ferret | 0.96 |
| Unknown Piranha | 1.27 | Steel Marten | 0.95 |
| Strange Millipede | 1.27 | Stone Trilobite | 0.91 |
| Strange GiantSquid | 1.27 | Leather TasmanianDevil | 0.91 |
| Strange Dragonfly | 1.27 | Bone Nautilus | 0.90 |
| Celestial Kraken | 1.27 | Steel Orangutan | 0.89 |
| Crystal Spark | 1.27 | Shadow Crocodile | 0.89 |
| Stone Abalone | 1.19 | Wood Swallow | 0.85 |
| Iron Weasel | 1.18 |  | 0.00 |
| Crystal Geode | 1.18 |  | 0.00 |
| Steel WaterBuffalo | 1.17 |  | 0.00 |
| Willow Sphinx | 1.17 |  | 0.00 |
| Gold Crown | 1.16 |  | 0.00 |


## Enemy mechanics vs builds (L40, full catalog builds, HP lost % per fight)

| Enemy (kit) | B1 | B3 | B5 | R1 | R4 | W1 | W3 | Z1 | Z5 |
|---|---|---|---|---|---|---|---|---|---|
| Spider (Venom/Ensnare) | 28% (97% win) | 43% (97% win) | 31% | 27% | 36% | 28% | 22% | 16% | 39% |
| Fire Elemental (Scorch) | 27% (97% win) | 39% | 31% | 37% | 36% (97% win) | 27% (97% win) | 23% | 19% | 33% (97% win) |
| Wolf (Rend/Hex) | 25% | 26% | 28% | 34% | 39% | 22% | 17% | 17% | 29% |
| Treant (Crush) | 17% | 27% | 21% | 26% | 32% | 29% (97% win) | 15% | 11% | 24% |
| Glass Shard Golem (Sunder/Frenzy) | 21% | 27% | 24% | 27% | 32% | 18% | 19% | 18% | 30% |
| Wraith (Drain/Disrupt) | 26% | 42% (97% win) | 33% (97% win) | 34% | 34% | 30% (97% win) | 21% | 14% | 36% |
| Ember Spirit (Frenzy) | 30% (97% win) | 39% (97% win) | 28% | 30% | 40% (97% win) | 35% (97% win) | 27% | 20% | 37% |
| Dune Roller (Shell) | 22% | 31% | 22% | 33% | 33% | 26% | 20% | 18% | 32% |
| Glare Stalker (Pierce) | 20% | 32% | 26% | 32% | 35% | 24% | 17% | 16% | 28% |
| Burial Lich (Mend/Curse) | 23% | 28% | 25% | 31% | 34% | 22% | 17% | 18% | 29% |
| Skeleton (Concuss) | 26% | 30% | 23% | 27% | 42% | 25% | 21% | 18% | 28% |

## What changed tonight

Everything new is behind `overnight-v2` knobs (v1 keeps the old behavior) except the bug fixes, which apply to both.

### Bugs found by measuring builds (apply to every patch)

| Bug | Effect before | Fix |
|---|---|---|
| SPEED_MOD cleared before the swing's duration was set | SPRINT / RUSH / MOMENTUM / MARATHON, First Formation, Metronome, War Drum, every "+10% speed" proc did nothing | Swing keeps its SPEED_MOD for `ActionSpeedSystem` |
| A foe's hit ran every material WHEN | Enemy connects minted the hero's CONTACT / GRAZE / SWING / STREAK | Take-hit path mints only take-hit currencies |
| Hit event fires before damage (`evt.Damage` = 0) | Blood Price, Mana Bleed, Toxic Ledger never fired | Heals are % max HP per connect; Ledger is a damage mod on the finisher |
| DoT mods named BURN / POISON / BLEED, code read weaponBurn … | Flaming / Poisonous / serrated did nothing (catalog gap G6) | Name mapping; Poisonous 0.5% |
| Status triggers ignored their value | Every poison proc added 1% max HP per tick | Value passed through (0.25 / 0.5) |
| Gut Instinct, Focus Lens added temp stats every hit and never decayed | Stat snowballed for the whole fight (×2.3, ×1.7 skill value) | Set once (like the earlier Steady March fix); Grease Palm / Loaded Odds last 2 turns |
| Thermal Shock didn't consume Burn + Slow | Stun-lock (×2.0) | Consumes both, as its text says |
| Cadence Keeper granted at least 1 Focus per alternation | Perma 3 Focus (×1.9) | rank × power, fraction as a chance |
| Enemy kits: sheet DoT / stun columns are item-only | (new system) | Kits use trigger bundles |
| Skill Fortify (Crest of Triumph, Second Wind, Mobile Bulwark, Field Brief) raised stacks but defense read a different field | Gave no defense | `combat.statusFortifyDefensePerStack` (v2 0.15 × K per stack, like Harden) |
| Burn / bleed are flat integer stacks | 1 damage per stack at any level | `combat.dotDamagePerLevel` (v2 0.1); bundle values reach burn / bleed |

### New systems (overnight-v2)

- **Enemy mechanic kits** (`enemySystem.actionKits`, `GameData/EnemyActions.json`, `Documentation/ENEMY_MECHANICS.md`):
  229 of 238 enemy action names didn't exist and fought as plain hits. 15 templates (Venom, Scorch, Rend, Crush,
  Sunder, Hex, Ensnare, Disrupt, Shell, Drain, Frenzy, Mend, Curse, Concuss, Pierce) with an alias for every name;
  each names the player's answer. Roster re-calibrated so threat per level is unchanged.
- **Percent converts** (`combat.materialConvertPercentPerUnit` + `materialConvertPercentByKeyword`): a convert pays a
  % of the hit per banked currency, with a rate per currency solved by BUILDLAB. Flat +5 per currency was worth
  nothing by L60. 5-piece mint is feed × 2 instead of feed² (`materialFiveStackMintMultiplier`).
- **Material piece triggers** (`lootSystem.materialPieceTriggers`): each piece rolls one trigger from its material's
  pool and keeps it through save/load. Before, loot never stamped them and loading cleared them (Silver could never
  mint LENS). Trigger pool re-priced one by one: multihit / retrigger / Focus / Slow / Expose procs moved to crit,
  "+10% amp" became "+10% damage" (amp lifted openers to the full TEC amp, ×1.8 for one piece). Strip-control procs
  (skip / jump / loop / random / repeat) fire on crit: on every connect they kept the strip at its opener, and a Steel set
  of them dropped Triumph Cadence to ×0.3.
- **Damage cap** 950 → 100000: the cap clipped every burst by L30–60, so build value faded with level.
- **Overflow**: swing speed past the 2 s floor becomes damage (`combat.speedOverflowDamageEfficiency` 0.75,
  `agilityOverflowSpeedPerPoint` 0.01); STR past 100 counts at half (`strengthOverflowEfficiency`). Warriors hit
  the speed floor around L50 and every later speed source was wasted.
- **INT potency** (`combat.intelligenceAmpPerPoint` 0.005): INT scales AMP. INT did nothing between strip-slot
  thresholds, so every INT skill node was dead. TEC combo AMP max 2.0 → 2.5 (`comboSystem.comboAmplifierMax`).
- **Curves re-solved** for all of the above: enemy health curve (new method: measured against near-harmless enemies),
  damage curve (WRSOLVE), roster calibration.

### Skill tree (shared `SkillTrees.json`, affects every patch)

- Every catalog node priced in its own build (add-one). Outliers cut (Wrath Unbound, Cadence-type procs, Arcane Weave,
  Resonance, Sigil Burn, Mana Bleed, Commission / Initiation …); see the table below. Nodes the solver raised on
  noise were reverted. `Scripts/skill-node-tune.py` repeats this.
- About 40 nodes still do nothing in their catalog build; listed under Open.

### Tools

- `BUILDLAB` (new headless mode): build ladders, `--long`, leave-one-out, add-one, power overrides, seeded banks,
  named triggers, weapon mods. Ladders: `GameData/TuningProfiles/build-ladders.json` (the 23 BUILD_CATALOG builds) and
  `hybrid-ladders.json`.
- `Scripts/curve-solve.py`, `Scripts/skill-node-tune.py`, `Scripts/convert-tune.py`; `balance-check.ps1 -ToolsDir`.
- Tests: `BuildGlueTests` (18). Full suite 9043 run, 83 failed = tonight's baseline minus one flaky test; the same
  5 suites crash as before.

## Morning follow-up (2026-10-12)

Answers to the open list: defense raised (1); 1-action calibration kept, strips are the first stepping stone (2);
TEC stays the dice-roll stat — Rogue builds R4–R6 already sit at or above the median through gear and actions (3);
outliers tuned (5).

**Item defense.** Loot armor came only from catalog values (head / feet 0–3, typical full gear 54 at L90 = 16% DR).
New `equipmentScaling` knobs, read at armor-total time so saved items follow the patch: `armorMultiplier`,
`armorFlatPerPiece`, `armorPerItemLevel`, `armorSlotWeights`. overnight-v2: 1.5 / 4.5 / 0.3 /
chest 1.3, legs 1.1, head 0.8, feet 0.8. Typical gear armor (DR): L1 23 (8%), L10 37 (12%), L30 78 (22%),
L60 144 (34%), L90 198 (42%). Enemy damage re-solved to hold the win-rate targets (BUILDLAB now prints an `Arm` column).

**Bugs fixed.** Repeated strip actions and `strip_replace_next` swings advanced from the action's first copy
(Marked Hunter ping-ponged STAB / MISDIRECT; any STAB > X > STAB > STAB strip skipped its tail). STEEL CREST
`block` 0 → 180 (sheet needs the same edit). Bleed and poison ticks printed to stdout in muted simulations.

**Outliers, absolute** (final kill time vs the median build at that level; 1.00 = median, higher = stronger; measured
with INT potency 0.01 before it was reverted to 0.005 — it lifted every build about equally, so the ratios hold):

| Build | before | after (L30 / L60 / L90) | change |
|---|---|---|---|
| R6 Dice Thief | ~1.8–2.1 | 1.16 / 1.01 / 1.17 | ClockworkBeetle retrigger on combo end (was on crit: ×1.9 alone in crit builds) |
| R5 Shadow Gambler | high | 1.12 / 0.83 / 0.96 | same |
| Z1 Chain Weaver | ~2.1 | 0.97 / 1.57 / 1.56 | `combat.retriggerDamageMultiplier` 0.5 (encore swings at half damage) |
| R2 Marked Hunter | 0.65–0.96 | 1.33 / 1.00 / 1.00 | strip fix; ladder runs MISDIRECT last; HEADHUNT out of the single-fight ladder (G13) |
| Converts | | | SWING 1.39 → 0.8, CHAIN 5.08 → 8, LENS 8 → 12 |
| Z2 / Z3 / Z4 / Z6 | 0.55–0.8 | 0.52–0.92 | **still low** — see below |

R4 / R5 / W3 looked high in "×over class strip" only because their class strips are weak (three STABs); in absolute
terms they sit at 0.8–1.2. Everything except the four wizard builds now lands in 0.8–1.35 of the median.

**Still open — weak wizard builds.** Add-one pricing: Focus Lens, Dungeon Analysis, Archmage, Loaded Odds give no
response; Prime Theory (×0.83), Spell Memory (×0.94) and Slot Mind (×0.96) hurt. Raising INT potency lifts every hero
(all heroes have INT), not wizards relative to others, so it was reverted. These need mechanic changes, not powers.

**Median-build calibration (designer decision, 2026-10-12).** Enemies are now solved against the median of the 23
catalog builds at the stepping stone expected for each level, with strips trimmed to the new level slot cap
(`lootSystem.comboSlotCapByLevel`: 1 slot at L1, 2 at L5, 3 at L10, 4 at L15, 5 at L25, 6 at L45, 7 at L70, 8 at L90).
`Scripts/median-curve-solve.py`, three passes. Result (`--check`): median build hero turns 11.1 / 10.8 / 11.0 / 10.8 /
9.5 / 8.5 / 8.4 at L1–L75, win rate on target at every level (L50 87.5% vs 89.4%). Enemy HP anchors rose to
0.89 / 1.04 / 1.22 / 2.19 / 6.30 / 10.57 / 20.2 / 32.5 (L1…L100), so late-game enemies are 2–4× tougher than before.
L50–L75 turns plateau near 8.5: part of build damage scales with enemy max HP (% poison, % converts).
A bare hero (the old balance-check fight rows, FUNDMATRIX typical) now loses: L35 18%, L50 0% win — by design.

## Open — decisions for you

1. **Defense still barely matters.** HARDEN in the strip cuts HP lost from 77% to 74% at L30; doubling Harden per stack
   only takes it to 73%; +21 armor (Bone Temper at 10× power) cuts it 3%. Harden lasts one enemy swing per strip cycle and
   armor DR uses a flat K (275) at every level, so flat armor from skills fades. Skill-granted Fortify gave *no* defense
   (fixed tonight, `statusFortifyDefensePerStack`), but the defensive nodes (Bone Temper, Phalanx, Guard Duty, Riposte,
   Shield Geometry …) still need a stat-level decision: e.g. Harden / Fortify as % damage reduction with a few turns of
   uptime, or armor scaled by level.
2. **The balance targets are calibrated on a 1-action hero.** FUNDMATRIX's typical hero (and the basic bot) swings one
   action, so "11 hero turns" is for a hero with no combo. Filling the strip is worth ×1.3–2.5 on its own, so any real
   player is well ahead of the curve before materials or skills. Either calibrate on a 3-action typical strip, or keep
   it and treat strips as the first stepping stone (current state).
3. **TEC is still the weakest class stat.** +30 TEC ≈ +1–6% vs STR +10–16%, AGI +30–47%, INT (now) +4–14%. TEC drives
   combo AMP (max raised 2.0 → 2.5) and crit thresholds, which is not enough for Rogue's class stat. Options: TEC adds
   crit damage, or TEC milestones come faster.
4. **~40 skill nodes still do nothing in their catalog build** (rules that need a rare condition, or defensive): w-discipline,
   w-line, w-opening, w-mastery, w-geometry, w-riposte, w-duty, w-brief, w-bulwark, w-anchor, w-combo, r-shadowcraft,
   r-channel, r-crime, r-witness, r-critmiss, r-ghost, r-fingers, r-glass, r-streak, r-trophy (dungeon-long), r-umbra,
   r-ledger, r-precision, z-analysis, z-archmage, z-catalyst, z-kindle, z-mastery, z-refract, z-pure, z-prime, b-age2,
   b-age3, b-combo, b-crest, b-cull, b-mastery, b-memory, b-temper, b-lvl2. The ladder data per build is in
   `GameData/TuningRuns/` (BUILDLAB JSON) — rerun with `BUILDLAB --builds X --aoo-base 4 --aoo-upto 6`.
5. **Build outliers** (whole build over class strip, L30–L90): high — Dice Thief R6 ×4.2–4.4 (Leather Lucky advantage
   + crit-face tools), Shadow Gambler R5 ×2.4–4.5, Toxic Ledger R4 ×2.6–4.0, Anchor Line W3 ×2.6–3.3, Elementalist Z5
   ×2.8–3.2; low — Marked Hunter R2 ×0.5–0.7 (MISDIRECT replaces the next slot with STAB, so the strip never reaches its
   converts; catalog gap G10), Lens Sage Z3 ×1.0–1.5, Empty Glyph Z2 ×1.1–1.2, Probability Engine Z4 ×1.2, Grudge Bearer
   B2 ×1.15–1.37. Triumph Cadence (B5) was ×0.3 until strip-control procs moved to crit; it is ×1.1–1.4 now (STEEL CREST
   still has `block: 0`).
6. **Hybrids:** "secondary attribute becomes primary" does nothing under the STR-damage formula (primary no longer adds
   damage); the attribute actions carry the hybrids (×1.15–1.5 each). Spellblade Weave measured lower than the strip it
   replaced partly because the ladder dropped RAPID STRIKE for it.
7. **Sheet sync:** `Actions.json` was not edited (enemy kits live in `EnemyActions.json`). `SkillTrees.json` node powers
   and two effect texts changed; `MaterialTriggerCatalog.cs` trigger timings changed — `Triggers.json` is a stamped copy
   (`--stamp-item-triggers`) if you want the sheet to match.
8. **Nothing is committed.** Your uncommitted work from before tonight is untouched (backup ref
   `refs/overnight/baseline-2026-10-12`). `GameData/Patches/Balance/overnight-v2.json` is gitignored: `git add -f` it.
9. **Survival margin is thin.** Survival to L10 is exactly 97% (60 runs per class; Warrior and Wizard 95%), and fight
   win rates from L20 to L75 sit 2–3 points under their targets (inside the ±4 tolerance). Most deaths before L11 are at
   the L10 wall, led by fire enemies (Scorch kit + burn now scaling with level). If you want more margin: lower the L9 /
   L10 damage anchors a little, or soften `combat.dotDamagePerLevel` (0.1) for enemy burns.

## Follow-up pass (2026-10-13)

Designer calls: item defense should grow with item tier; TEC deferred; dead skill nodes removed unless they can be
made to work; the four weak wizard builds made stronger; a little more survival before L10; late curve (L75–L100)
solved with DUNGEONSIM.

**Armor by tier.** New `equipmentScaling.armorPerItemTier` (armor per tier above 1, times the slot weight, read at
armor-total time like the other armor knobs). overnight-v2: `armorPerItemLevel` 0.3 → 0.15 and `armorPerItemTier` 6,
so a T5 piece carries +24 × slot weight over a T1 of the same level, and typical gear at L60 goes 144 → 163 armor. The
level part was halved so early and mid levels barely move.

**Survival before L10.** Damage anchors L9 0.30 → 0.27 and L10 0.38 → 0.35 (last check: survival 95% vs ≥97%,
L10 real clear 77.8% vs 75).

**Dead skill nodes.** 39 of the 41 nodes listed under Open (4) were removed from `SkillTrees.json`,
`skill-tree-templates.json` and the catalog ladders: b-lvl2, b-temper, b-memory, b-mastery, b-cull, b-combo, b-age2,
b-age3, b-crest, w-combo, w-riposte, w-duty, w-geometry, w-bulwark, w-anchor, w-discipline, w-opening, w-brief, w-mastery,
w-line, r-precision, r-fingers, r-channel, r-glass, r-critmiss, r-streak, r-ghost, r-witness, r-umbra, r-shadowcraft,
r-ledger, r-trophy, r-crime, z-pure, z-mastery, z-prime, z-analysis, z-kindle, z-catalyst. Two were reworked instead:
- **Archmage** (z-archmage): also multiplies INT's AMP potency (`intelligenceAmpPerPoint`) for the wizard who has it;
  power 0.828 → 1.6. It's the wizard-only INT lever the morning pass was missing.
- **Refraction** (z-refract): REFRACT gains +3% damage per LENS per rank (was 0.5 Focus per LENS).

Saves drop learned ids that no longer exist when they load. Spent points are summed per tree, so the refund is
automatic. Their C# handlers are left in place, unused. Removing them takes 33 / 33 / 45 points (Barbarian / Warrior /
Rogue) out of each tree's total sink (98 / 115 / 103), so heroes run out of things to buy earlier. If you want the sink
back, add redesigned nodes there. **Sheet:** the Class Upgrades tab still has these rows; delete them there too, or the
next pull brings them back.

**Wizard builds** (BUILDLAB L60 `--long 4`, final kill time in enemy swings, lower is better; B1 Pain Engine 16.0
for reference):

| Build | before | after | what changed |
|---|---|---|---|
| Z2 Empty Glyph | 22.3 | 9.6 | ladder strip never ran WILLOW LASH (it sat behind the finisher); Archmage |
| Z3 Lens Sage | 23.7 | 18.6 | Silver Prism / Mirror Focus procs on first hit (on crit starved LENS); LENS convert 12 → 18; Refraction rework |
| Z4 Probability Engine | 18.6 | 15.6 | strip runs REFRACT instead of ROLL FIFTEEN; z-refract in its skills; LENS; Archmage |
| Z6 Golem Mind | 12.9 | 11.6 | Archmage, LENS |
| Z5 Elementalist | 8.3 | 6.9 | Archmage (side effect) |
| Z1 Chain Weaver | 3.5 | 3.5 | already kills in ~5 swings |

Z3 is still the weakest: past the catalog-skills step its damage per turn doubles but kill time doesn't move, because
REFRACT's payoff waits on LENS banking, so most of the burst is overkill. The next lever is how fast LENS is minted, not
convert potency.

**Tools.** `dungeon-sim.py --shard-timeout` (default 1800 s) drops a stuck shard instead of hanging. The 2026-10-13 02:25
L75 solve stalled on one Rogue shard that ran for 8 h. Possibly an endless fight, worth a look.

**Late damage curve (DUNGEONSIM, real dungeons, HP carried, retreat below 50%).** Targets from `realClearAnchors`:
L75 30%, L90 12% (interpolated), L100 0%.

| Level | Anchor before | Anchor now | Clear | Per class (B / W / R / Z) |
|---|---|---|---|---|
| L75 | 2.54 | **1.2815** | 34.9% [27–44] | 6% / 50% / 4% / 54% |
| L90 | (none) | **3.4662** | 12.1% [7–20] | 5% / 17% / 0% / 19% |
| L100 | 25.146 | **6.7289** (L75→L90 growth continued) | 3.6% [1–10] | 0% / 3% / 0% / 10% |

The old L75 / L100 anchors were left over from the full-HP solve, and 2.54 at L75 was far too lethal once HP carries
between fights. The per-class split is noisy (12 heroes per class per step: Barbarian read 0%, 6% and 44% at
neighbouring anchors), but Barbarian and Rogue sit well under Warrior and Wizard at L75–L90 in most steps, and Wizard
max HP at L75 (~1284) is ~40% above the other classes. This needs a bigger sample before anyone tunes on it.
