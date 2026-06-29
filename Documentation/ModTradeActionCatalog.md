# Mod-trade action review catalog

Authoring reference for actions built from **next-action mod** fields. Values below are **literal strings** for the ACTIONS sheet / JSON (tune per weapon).

**Units**

- **This action’s `Damage` / `Speed` (authoring):** express in the Notes **Base** line as **Dmg %** and **Spd %** vs **100% = your baseline** for that slot (see Legend). Your pipeline may still store absolute sheet numbers—derive them from the same baseline (e.g. baseline damage × `Dmg%` / 100).
- `**SpeedMod` / `DamageMod` / `AmpMod` (hero and enemy):** additive **percentage points** on the actor’s **next** action (e.g. `20` = +20% speed term in length calc, `+25` damage multiplier term). Negative = cost/nerf.
- `**MultiHitMod` / `EnemyMultiHitMod`:** **extra hit count** (integer). Positive adds ticks. **Note:** current combat code applies `max(0, consumedMultiHit)` when adding hits, so a **negative** multihit mod **does not** reduce hit count today; rows that “pay” multihit still list a negative for sheet clarity—pair with a **lower `NumberOfHits`** on the next action, or extend the engine.

**Sheet columns (hero):** `SpeedMod`, `DamageMod`, `MultiHitMod`, `AmpMod`  
**Sheet columns (enemy):** `EnemySpeedMod`, `EnemyDamageMod`, `EnemyMultiHitMod`, `EnemyAmpMod`

**Notation:** `H[S+]/E[D−]` = hero next-action speed buff, enemy next-action damage nerf.

**Legend (Notes column — this action’s profile):** `**Dmg %`** and `**Spd %`** are relative to **100% = default / average** for the slot you are authoring (same weapon tier baseline). **Damage:** above **100%** = stronger than average; below **100%** = weaker. **Speed:** below **100%** = faster than average; above **100%** = slower. `**Hits`** stays a **count** (not a %). `Δ` in text still refers to **next-action** sheet mods (`SpeedMod` / `DamageMod` …), which are separate additive % points on the follow-up strike.

---

## 1. Same-character trades (24)

Single actor pays mod *A* on their **next** action to buff mod *B* on their **next** action. Only one side’s four mod columns are used.

### 1a. Hero (12)


| PatternId | Mechanics  | NameHero            | NameEnemy | Description                                                 | HeroMods                                  | EnemyMods | Notes                                                                                                  |
| --------- | ---------- | ------------------- | --------- | ----------------------------------------------------------- | ----------------------------------------- | --------- | ------------------------------------------------------------------------------------------------------ |
| HS_S2D    | H: S− → D+ | Commitment Cleaver  | —         | You telegraph; your next swing hits like a finisher.        | `SpeedMod` **-20**, `DamageMod` **+25**   | —         | Base: **Dmg 147%**, **Spd 118%** (slow), **Hits 1**. Δ next (sheet mods): −20 spd pts, +25 dmg pts.    |
| HS_S2A    | H: S− → A+ | Stance of Crescendo | —         | You lose rhythm now to stack combo scaling on what follows. | `SpeedMod` **-10**, `AmpMod` **+15**      | —         | Base: **Dmg 95%**, **Spd 108%** (slightly slow), **Hits 1**. Combo primer.                             |
| HS_S2M    | H: S− → M+ | Windup Flurry Mark  | —         | Slow wind-up loads extra hits on your next volley.          | `SpeedMod` **-15**, `MultiHitMod` **+1**  | —         | Base: **Dmg 100%**, **Spd 115%** (slow), **Hits 1**.                                                   |
| HS_D2S    | H: D− → S+ | Quick Razors        | —         | Light taps reclaim initiative for your next card.           | `DamageMod` **-10**, `SpeedMod` **+20**   | —         | Base: **Dmg 74%**, **Spd 92%** (fast), **Hits 1**.                                                     |
| HS_D2A    | H: D− → A+ | Feather Chain       | —         | Weak surface hit; your combo amplifier climbs faster next.  | `DamageMod` **-10**, `AmpMod` **+10**     | —         | Base: **Dmg 84%**, **Spd 92%**, **Hits 1**.                                                            |
| HS_D2M    | H: D− → M+ | Rakeload            | —         | Shallow damage now, more cuts registered next time.         | `DamageMod` **-10**, `MultiHitMod` **+2** | —         | Base: **Dmg 79%**, **Spd 100%**, **Hits 1**.                                                           |
| HS_A2S    | H: A− → S+ | Reset Step          | —         | You cash combo juice to snap your next action forward.      | `AmpMod` **-15**, `SpeedMod` **+20**      | —         | Base: **Dmg 116%**, **Spd 100%**, **Hits 1**.                                                          |
| HS_A2D    | H: A− → D+ | Compression Strike  | —         | You compress scaling into one heavier wallop next.          | `AmpMod` **-15**, `DamageMod` **+25**     | —         | Base: **Dmg 126%**, **Spd 100%**, **Hits 1**.                                                          |
| HS_A2M    | H: A− → M+ | Scatter Focus       | —         | You spread amplification across many small impacts next.    | `AmpMod` **-8**, `MultiHitMod` **+2**     | —         | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                                                          |
| HS_M2S    | H: M− → S+ | Singletap Recovery  | —         | Fewer follow-ups; you recover spacing on the next beat.     | `MultiHitMod` **-1**, `SpeedMod` **+15**  | —         | Base: **Dmg 95%**, **Spd 95%** (fast), **Hits 2**. Multihit −1: see intro; next card often **Hits 1**. |
| HS_M2D    | H: M− → D+ | Collapse Cut        | —         | You merge many cuts into one decisive strike next.          | `MultiHitMod` **-2**, `DamageMod` **+35** | —         | Base: **Dmg 84%**, **Spd 100%**, **Hits 3**. Next: fewer ticks + big dmg (clamp note).                 |
| HS_M2A    | H: M− → A+ | Tight Spiral        | —         | Fewer hits next, each carrying more combo weight.           | `MultiHitMod` **-1**, `AmpMod` **+15**    | —         | Base: **Dmg 89%**, **Spd 100%**, **Hits 2**.                                                           |


### 1b. Enemy (12)


| PatternId | Mechanics  | NameHero | NameEnemy             | Description                                               | HeroMods | EnemyMods                                           | Notes                                                 |
| --------- | ---------- | -------- | --------------------- | --------------------------------------------------------- | -------- | --------------------------------------------------- | ----------------------------------------------------- |
| ES_S2D    | E: S− → D+ | —        | Lumbering Smash       | It falls behind, then its next hit is devastating.        | —        | `EnemySpeedMod` **-20**, `EnemyDamageMod` **+25**   | Base (enemy): **Dmg 137%**, **Spd 118%**, **Hits 1**. |
| ES_S2A    | E: S− → A+ | —        | Charging Menace       | Slow approach; its combo scaling spikes on the follow-up. | —        | `EnemySpeedMod` **-10**, `EnemyAmpMod` **+15**      | Base: **Dmg 105%**, **Spd 112%**, **Hits 1**.         |
| ES_S2M    | E: S− → M+ | —        | Delayed Shredder      | It commits tempo to set a burst of extra ticks next.      | —        | `EnemySpeedMod` **-15**, `EnemyMultiHitMod` **+1**  | Base: **Dmg 116%**, **Spd 112%**, **Hits 1**.         |
| ES_D2S    | E: D− → S+ | —        | Harrier Poke          | Weak injury that buys it the next initiative.             | —        | `EnemyDamageMod` **-10**, `EnemySpeedMod` **+20**   | Base: **Dmg 74%**, **Spd 100%**, **Hits 1**.          |
| ES_D2A    | E: D− → A+ | —        | Taunting Feint        | Scratch damage while it builds internal rage scaling.     | —        | `EnemyDamageMod` **-10**, `EnemyAmpMod` **+10**     | Base: **Dmg 79%**, **Spd 100%**, **Hits 1**.          |
| ES_D2M    | E: D− → M+ | —        | Swarm Posture         | Low per-hit pressure that widens into many ticks next.    | —        | `EnemyDamageMod` **-10**, `EnemyMultiHitMod` **+2** | Base: **Dmg 74%**, **Spd 100%**, **Hits 1**.          |
| ES_A2S    | E: A− → S+ | —        | Cooldown Rush         | It dumps buildup to cut the gap on its next move.         | —        | `EnemyAmpMod` **-15**, `EnemySpeedMod` **+20**      | Base: **Dmg 111%**, **Spd 112%**, **Hits 1**.         |
| ES_A2D    | E: A− → D+ | —        | Finishing Protocol    | It spends stacked threat on one spike next.               | —        | `EnemyAmpMod` **-15**, `EnemyDamageMod` **+25**     | Base: **Dmg 121%**, **Spd 112%**, **Hits 1**.         |
| ES_A2M    | E: A− → M+ | —        | Scatter Burst         | It sprays stored power across a flurry next.              | —        | `EnemyAmpMod` **-8**, `EnemyMultiHitMod` **+2**     | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.         |
| ES_M2S    | E: M− → S+ | —        | Repositioning Skitter | It stops chaining hits and becomes slippery next.         | —        | `EnemyMultiHitMod` **-1**, `EnemySpeedMod` **+15**  | Base: **Dmg 89%**, **Spd 100%**, **Hits 2**.          |
| ES_M2D    | E: M− → D+ | —        | Coalesce Slam         | Fewer swings; one armor-breaking slam next.               | —        | `EnemyMultiHitMod` **-2**, `EnemyDamageMod` **+35** | Base: **Dmg 79%**, **Spd 112%**, **Hits 3**.          |
| ES_M2A    | E: M− → A+ | —        | Focused Predator      | It narrows pressure into fewer, heavier meaningful hits.  | —        | `EnemyMultiHitMod` **-1**, `EnemyAmpMod` **+15**    | Base: **Dmg 95%**, **Spd 100%**, **Hits 2**.          |


---

## 2. Dual-sided same-stat coincident (16)

Hero and enemy **same stat**, signs fixed. **Overlaps** diagonal `XC_`* rows (same numbers repeated there).

### 2a. Opposed (8)


| PatternId   | Mechanics   | NameHero            | NameEnemy            | Description                                 | HeroMods             | EnemyMods                 | Notes                                                             |
| ----------- | ----------- | ------------------- | -------------------- | ------------------------------------------- | -------------------- | ------------------------- | ----------------------------------------------------------------- |
| DE_S_opp_HP | H[S+]/E[S−] | Initiative Theft    | Staggering Lunge     | You surge; their next tempo stumbles.       | `SpeedMod` **+20**   | `EnemySpeedMod` **-20**   | Base: **Dmg 105%**, **Spd 100%**, **Hits 1**.                     |
| DE_S_opp_EH | H[S−]/E[S+] | Overextended Strike | Predator Surge       | You pay speed; they accelerate next.        | `SpeedMod` **-20**   | `EnemySpeedMod` **+20**   | Base: **Dmg 126%**, **Spd 100%**, **Hits 1**.                     |
| DE_D_opp_HP | H[D+]/E[D−] | Shatter Plate       | Weakened Claw        | Your next hit bites harder; theirs softens. | `DamageMod` **+20**  | `EnemyDamageMod` **-20**  | Base: **Dmg 95%**, **Spd 100%**, **Hits 1**.                      |
| DE_D_opp_EH | H[D−]/E[D+] | Feint Opening       | Bloodletting Riposte | You lighten damage; their next swing loads. | `DamageMod` **-10**  | `EnemyDamageMod` **+20**  | Base: **Dmg 84%**, **Spd 100%**, **Hits 1**.                      |
| DE_A_opp_HP | H[A+]/E[A−] | Momentum Hijack     | Broken Focus         | Your combo scaling improves; theirs decays. | `AmpMod` **+15**     | `EnemyAmpMod` **-15**     | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                     |
| DE_A_opp_EH | H[A−]/E[A+] | Stalled Rhythm      | Enrage Spark         | You stall scaling; they ignite theirs next. | `AmpMod` **-8**      | `EnemyAmpMod` **+15**     | Base: **Dmg 111%**, **Spd 100%**, **Hits 1**.                     |
| DE_M_opp_HP | H[M+]/E[M−] | Chainloaded         | Disrupted Swarm      | You widen follow-ups; theirs shortens next. | `MultiHitMod` **+2** | `EnemyMultiHitMod` **-2** | Base: **Dmg 89%**, **Spd 100%**, **Hits 1**.                      |
| DE_M_opp_EH | H[M−]/E[M+] | Tight Combo         | Needle Hail Incoming | You simplify hits; they go wide next.       | `MultiHitMod` **-1** | `EnemyMultiHitMod` **+2** | Base: **Dmg 100%**, **Spd 100%**, **Hits 2**; see multihit intro. |


### 2b. Aligned (8)


| PatternId     | Mechanics   | NameHero           | NameEnemy        | Description                                | HeroMods             | EnemyMods                 | Notes                                                            |
| ------------- | ----------- | ------------------ | ---------------- | ------------------------------------------ | -------------------- | ------------------------- | ---------------------------------------------------------------- |
| DE_S_align_PP | H[S+]/E[S+] | Doubletime Gambit  | Berserk Tempo    | Both sides accelerate next; chaotic round. | `SpeedMod` **+10**   | `EnemySpeedMod` **+10**   | Base: **Dmg 95%**, **Spd 100%**, **Hits 1**.                     |
| DE_S_align_MM | H[S−]/E[S−] | Gluefoot Exchange  | Exhaustion Grind | Both slog next; mud, gravity, fatigue.     | `SpeedMod` **-10**   | `EnemySpeedMod` **-10**   | Base: **Dmg 105%**, **Spd 100%**, **Hits 1**.                    |
| DE_D_align_PP | H[D+]/E[D+] | Blood Moon Strike  | Mirror Berserk   | Both next hits hit harder.                 | `DamageMod` **+10**  | `EnemyDamageMod` **+10**  | Base: **Dmg 89%**, **Spd 100%**, **Hits 1**.                     |
| DE_D_align_MM | H[D−]/E[D−] | Pacifying Ward     | Softened Claws   | Both next hits muted.                      | `DamageMod` **-10**  | `EnemyDamageMod` **-10**  | Base: **Dmg 95%**, **Spd 100%**, **Hits 1**.                     |
| DE_A_align_PP | H[A+]/E[A+] | Duel of Crescendos | Rival’s Anthem   | Both climb scaling next.                   | `AmpMod` **+10**     | `EnemyAmpMod` **+10**     | Base: **Dmg 95%**, **Spd 100%**, **Hits 1**.                     |
| DE_A_align_MM | H[A−]/E[A−] | Suppress Sigil     | Flattened Fury   | Both lose scaling next.                    | `AmpMod` **-10**     | `EnemyAmpMod` **-10**     | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                    |
| DE_M_align_PP | H[M+]/E[M+] | Storm of Blades    | Hail Partner     | Both gain extra ticks next.                | `MultiHitMod` **+1** | `EnemyMultiHitMod` **+1** | Base: **Dmg 84%**, **Spd 100%**, **Hits 1**.                     |
| DE_M_align_MM | H[M−]/E[M−] | Clean Exchange     | Single-blow Oath | Both lose follow-up volume next.           | `MultiHitMod` **-1** | `EnemyMultiHitMod` **-1** | Base: **Dmg 95%**, **Spd 100%**, **Hits 2**; see multihit intro. |


---

## 3. Cross-stat dual-sided quadrants (64)

**Family** = (hero axis `Hx`, enemy axis `Ey`). **Quadrants:** `pp` = H[Hx+]/E[Ey+], `pm` = H[Hx+]/E[Ey−], `mp` = H[Hx−]/E[Ey+], `mm` = H[Hx−]/E[Ey−].

Scale: **“std” pair** = ±20 for S/D when marked strong, ±10 when light; Amp ±10/±15; Multihit ±1/±2. **Base** defaults for §3 unless Notes say otherwise: **Dmg 100%**, **Spd 100%**, **Hits 1**.

### Hero axis S (enemy varies)


| PatternId | Mechanics   | NameHero       | NameEnemy        | Description                                 | HeroMods           | EnemyMods                 | Notes                                                                                   |
| --------- | ----------- | -------------- | ---------------- | ------------------------------------------- | ------------------ | ------------------------- | --------------------------------------------------------------------------------------- |
| XC_SS_pp  | H[S+]/E[S+] | Sprint Harmony | Rush Echo        | You and it both speed up next.              | `SpeedMod` **+10** | `EnemySpeedMod` **+10**   | Same as DE_S_align_PP. Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                    |
| XC_SS_pm  | H[S+]/E[S−] | Lane Clear     | Tripwire Kick    | You surge; they stumble next.               | `SpeedMod` **+20** | `EnemySpeedMod` **-20**   | Same as DE_S_opp_HP.                                                                    |
| XC_SS_mp  | H[S−]/E[S+] | Heavy Commit   | Opportunist Dash | You pay tempo; they dash next.              | `SpeedMod` **-20** | `EnemySpeedMod` **+20**   | Same as DE_S_opp_EH.                                                                    |
| XC_SS_mm  | H[S−]/E[S−] | Anchor Duel    | Mired Beast      | Mutual slow next.                           | `SpeedMod` **-10** | `EnemySpeedMod` **-10**   | Same as DE_S_align_MM.                                                                  |
| XC_SD_pp  | H[S+]/E[D+] | Blitzer’s Mark | Armored Counter  | You quicken; their next hit hardens.        | `SpeedMod` **+10** | `EnemyDamageMod` **+10**  | Base: **Dmg 95%**, **Spd 100%**, **Hits 1** (slightly under average damage this swing). |
| XC_SD_pm  | H[S+]/E[D−] | Outpace Shell  | Dulled Fang      | You accelerate; their next damage softens.  | `SpeedMod` **+20** | `EnemyDamageMod` **-20**  | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                                           |
| XC_SD_mp  | H[S−]/E[D+] | Winded Rush    | Loaded Maw       | You slog; their next strike grows.          | `SpeedMod` **-20** | `EnemyDamageMod` **+20**  | Base: **Dmg 105%**, **Spd 100%**, **Hits 1** (pay tempo on this card).                  |
| XC_SD_mm  | H[S−]/E[D−] | Cautious Brake | Pulled Punch     | Both next hits soften after tempo dip.      | `SpeedMod` **-10** | `EnemyDamageMod` **-10**  | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                                           |
| XC_SA_pp  | H[S+]/E[A+] | Flow State     | Rival Crescendo  | Both ramp combo scaling next.               | `SpeedMod` **+10** | `EnemyAmpMod` **+10**     | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                                           |
| XC_SA_pm  | H[S+]/E[A−] | Tempo Lock     | Broken Chorus    | You speed up; their amp decays next.        | `SpeedMod` **+20** | `EnemyAmpMod` **-15**     | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                                           |
| XC_SA_mp  | H[S−]/E[A+] | Late Block     | Fury Spark       | You eat speed; they ignite scaling next.    | `SpeedMod` **-20** | `EnemyAmpMod` **+15**     | Base: **Dmg 105%**, **Spd 100%**, **Hits 1**.                                           |
| XC_SA_mm  | H[S−]/E[A−] | Fatigue Spiral | Dampened Rage    | Both lose amp pressure next.                | `SpeedMod` **-10** | `EnemyAmpMod` **-10**     | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                                           |
| XC_SM_pp  | H[S+]/E[M+] | Flurry Haste   | Partnered Storm  | Both widen hit volume next.                 | `SpeedMod` **+10** | `EnemyMultiHitMod` **+1** | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                                           |
| XC_SM_pm  | H[S+]/E[M−] | Cut the Chain  | Snapped Swarm    | You speed up; their multihit shortens next. | `SpeedMod` **+20** | `EnemyMultiHitMod` **-2** | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**. Enemy multihit −2: see intro.             |
| XC_SM_mp  | H[S−]/E[M+] | Trapped Feet   | Scatter Needles  | You slow; they spray ticks next.            | `SpeedMod` **-20** | `EnemyMultiHitMod` **+2** | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                                           |
| XC_SM_mm  | H[S−]/E[M−] | Grounded Guard | Collapsed Flurry | Both lose extra hits next after tempo dip.  | `SpeedMod` **-10** | `EnemyMultiHitMod` **-1** | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**. Enemy multihit −1: see intro.             |


### Hero axis D


| PatternId | Mechanics   | NameHero         | NameEnemy        | Description                                   | HeroMods            | EnemyMods                 | Notes                                                                       |
| --------- | ----------- | ---------------- | ---------------- | --------------------------------------------- | ------------------- | ------------------------- | --------------------------------------------------------------------------- |
| XC_DS_pp  | H[D+]/E[S+] | Power Rush       | Swift Retort     | You hit harder; they move faster next.        | `DamageMod` **+20** | `EnemySpeedMod` **+20**   | Base: **Dmg 95%**, **Spd 100%**, **Hits 1**.                                |
| XC_DS_pm  | H[D+]/E[S−] | Crushing Hold    | Pin Down         | Your damage buff; they lose speed next.       | `DamageMod` **+20** | `EnemySpeedMod` **-20**   | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                               |
| XC_DS_mp  | H[D−]/E[S+] | Glancing Blow    | Slippery Prey    | You lighten hit; they accelerate next.        | `DamageMod` **-10** | `EnemySpeedMod` **+20**   | Base: **Dmg 89%**, **Spd 100%**, **Hits 1**.                                |
| XC_DS_mm  | H[D−]/E[S−] | Soft Tap         | Sluggish Claw    | Both next actions soften on damage and tempo. | `DamageMod` **-10** | `EnemySpeedMod` **-10**   | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                               |
| XC_DD_pp  | H[D+]/E[D+] | Arms Race Cut    | Mirror Cleave    | Both next hits spike damage.                  | `DamageMod` **+10** | `EnemyDamageMod` **+10**  | Same as DE_D_align_PP.                                                      |
| XC_DD_pm  | H[D+]/E[D−] | Sundering Edge   | Cracked Carapace | You buff damage; theirs weakens next.         | `DamageMod` **+20** | `EnemyDamageMod` **-20**  | Same as DE_D_opp_HP.                                                        |
| XC_DD_mp  | H[D−]/E[D+] | Opening Gift     | Riposte Load     | You feint; they load damage next.             | `DamageMod` **-10** | `EnemyDamageMod` **+20**  | Same as DE_D_opp_EH.                                                        |
| XC_DD_mm  | H[D−]/E[D−] | Pulled Strikes   | Blunted Rage     | Both next hits muted.                         | `DamageMod` **-10** | `EnemyDamageMod` **-10**  | Same as DE_D_align_MM.                                                      |
| XC_DA_pp  | H[D+]/E[A+] | Heavy Crescendo  | Spiteful Ramp    | Big hit plus enemy scaling up next.           | `DamageMod` **+20** | `EnemyAmpMod` **+15**     | Base: **Dmg 95%**, **Spd 100%**, **Hits 1**.                                |
| XC_DA_pm  | H[D+]/E[A−] | Shatter Focus    | Silenced Fury    | You hit hard; their amp falls next.           | `DamageMod` **+20** | `EnemyAmpMod` **-15**     | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                               |
| XC_DA_mp  | H[D−]/E[A+] | Tickling Jab     | Building Menace  | Light hit; they ramp next.                    | `DamageMod` **-10** | `EnemyAmpMod` **+15**     | Base: **Dmg 89%**, **Spd 100%**, **Hits 1**.                                |
| XC_DA_mm  | H[D−]/E[A−] | Muffled Exchange | Dampened Core    | Both damage and their amp dip next.           | `DamageMod` **-10** | `EnemyAmpMod` **-10**     | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                               |
| XC_DM_pp  | H[D+]/E[M+] | Cleaving Tempest | Answering Hail   | Heavy hit meets wider enemy flurry next.      | `DamageMod` **+20** | `EnemyMultiHitMod` **+2** | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                               |
| XC_DM_pm  | H[D+]/E[M−] | Finisher’s Law   | Broken Volley    | You spike damage; their ticks shrink next.    | `DamageMod` **+20** | `EnemyMultiHitMod` **-2** | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**. Enemy multihit −2: see intro. |
| XC_DM_mp  | H[D−]/E[M+] | Chip Scatter     | Needle Echo      | Weak surface; they widen hits next.           | `DamageMod` **-10** | `EnemyMultiHitMod` **+2** | Base: **Dmg 89%**, **Spd 100%**, **Hits 1**.                                |
| XC_DM_mm  | H[D−]/E[M−] | Softened Hail    | Short Chain      | Both lose damage bite and extra hits next.    | `DamageMod` **-10** | `EnemyMultiHitMod` **-1** | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**. Enemy multihit −1: see intro. |


### Hero axis A


| PatternId | Mechanics   | NameHero            | NameEnemy            | Description                             | HeroMods         | EnemyMods                 | Notes                                                                       |
| --------- | ----------- | ------------------- | -------------------- | --------------------------------------- | ---------------- | ------------------------- | --------------------------------------------------------------------------- |
| XC_AS_pp  | H[A+]/E[S+] | Crescendo Sprint    | Racing Torment       | Your scaling and their speed rise next. | `AmpMod` **+10** | `EnemySpeedMod` **+10**   | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                               |
| XC_AS_pm  | H[A+]/E[S−] | Dominant Line       | Anchored Beast       | You ramp; they slow next.               | `AmpMod` **+15** | `EnemySpeedMod` **-20**   | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                               |
| XC_AS_mp  | H[A−]/E[S+] | Lost Bridge         | Surging Flank        | You stall amp; they speed up next.      | `AmpMod` **-10** | `EnemySpeedMod` **+20**   | Base: **Dmg 105%**, **Spd 100%**, **Hits 1**.                               |
| XC_AS_mm  | H[A−]/E[S−] | Quiet Measure       | Winded Hunter        | Both amp and speed pressure ease next.  | `AmpMod` **-10** | `EnemySpeedMod` **-10**   | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                               |
| XC_AD_pp  | H[A+]/E[D+] | Amp to Steel        | Spiked Counter       | Scaling meets enemy damage buff next.   | `AmpMod` **+10** | `EnemyDamageMod` **+10**  | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                               |
| XC_AD_pm  | H[A+]/E[D−] | Overwhelming Theme  | Dulled Retort        | You ramp; their hit softens next.       | `AmpMod` **+15** | `EnemyDamageMod` **-20**  | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                               |
| XC_AD_mp  | H[A−]/E[D+] | Burnt Fuse          | Sundering Answer     | You drop amp; they sharpen damage next. | `AmpMod` **-10** | `EnemyDamageMod` **+20**  | Base: **Dmg 105%**, **Spd 100%**, **Hits 1**.                               |
| XC_AD_mm  | H[A−]/E[D−] | Flattened Verse     | Weakened Slam        | Both scaling and enemy damage dip next. | `AmpMod` **-10** | `EnemyDamageMod` **-10**  | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                               |
| XC_AA_pp  | H[A+]/E[A+] | Dueling Anthems     | Mirror Rage          | Both climb amp next.                    | `AmpMod` **+10** | `EnemyAmpMod` **+10**     | Same as DE_A_align_PP.                                                      |
| XC_AA_pm  | H[A+]/E[A−] | Steal the Chorus    | Shattered Pride      | You ramp; they lose amp next.           | `AmpMod` **+15** | `EnemyAmpMod` **-15**     | Same as DE_A_opp_HP.                                                        |
| XC_AA_mp  | H[A−]/E[A+] | Broken Streak       | Predator’s Crescendo | You stall; they surge scaling next.     | `AmpMod` **-8**  | `EnemyAmpMod` **+15**     | Same as DE_A_opp_EH.                                                        |
| XC_AA_mm  | H[A−]/E[A−] | Suppressed Duel     | Cowed Beast          | Both amp curves flatten next.           | `AmpMod` **-10** | `EnemyAmpMod` **-10**     | Same as DE_A_align_MM.                                                      |
| XC_AM_pp  | H[A+]/E[M+] | Chorus of Cuts      | Swarm in Kind        | You ramp; they widen multihit next.     | `AmpMod` **+10** | `EnemyMultiHitMod` **+2** | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                               |
| XC_AM_pm  | H[A+]/E[M−] | Precision Crescendo | Snipped Tide         | You ramp; their volume shrinks next.    | `AmpMod` **+15** | `EnemyMultiHitMod` **-2** | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**. Enemy multihit −2: see intro. |
| XC_AM_mp  | H[A−]/E[M+] | Fumbled Bridge      | Scatter Spite        | You lose amp; they go wide next.        | `AmpMod` **-10** | `EnemyMultiHitMod` **+2** | Base: **Dmg 105%**, **Spd 100%**, **Hits 1**.                               |
| XC_AM_mm  | H[A−]/E[M−] | Muted Finale        | Broken Hail          | Both amp and multihit ease next.        | `AmpMod` **-10** | `EnemyMultiHitMod` **-1** | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**. Enemy multihit −1: see intro. |


### Hero axis M


| PatternId | Mechanics   | NameHero               | NameEnemy      | Description                                      | HeroMods             | EnemyMods                 | Notes                                                                      |
| --------- | ----------- | ---------------------- | -------------- | ------------------------------------------------ | -------------------- | ------------------------- | -------------------------------------------------------------------------- |
| XC_MS_pp  | H[M+]/E[S+] | Flurry Sprint          | Echo Rush      | Extra ticks and enemy speed up next.             | `MultiHitMod` **+1** | `EnemySpeedMod` **+10**   | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                              |
| XC_MS_pm  | H[M+]/E[S−] | Tanglefoot Flurry      | Caught Leg     | You widen hits; they slow next.                  | `MultiHitMod` **+2** | `EnemySpeedMod` **-20**   | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                              |
| XC_MS_mp  | H[M−]/E[S+] | Short Chain            | Slipstream     | Fewer your hits; they accelerate next.           | `MultiHitMod` **-1** | `EnemySpeedMod` **+20**   | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**. Hero multihit −1: see intro. |
| XC_MS_mm  | H[M−]/E[S−] | Collapsed Tempo        | Mired Swarm    | Both volume and speed ease next.                 | `MultiHitMod` **-1** | `EnemySpeedMod` **-10**   | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**. Hero multihit −1: see intro. |
| XC_MD_pp  | H[M+]/E[D+] | Many Cuts, Deep Answer | Spiked Hail    | Volume meets enemy damage spike next.            | `MultiHitMod` **+2** | `EnemyDamageMod` **+20**  | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                              |
| XC_MD_pm  | H[M+]/E[D−] | Shred Armor            | Dulled Swipes  | You add ticks; their damage softens next.        | `MultiHitMod` **+2** | `EnemyDamageMod` **-20**  | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                              |
| XC_MD_mp  | H[M−]/E[D+] | Single Bite            | Heavy Tail     | Fewer hits; enemy damage loads next.             | `MultiHitMod` **-2** | `EnemyDamageMod` **+20**  | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**. Hero multihit −2: see intro. |
| XC_MD_mm  | H[M−]/E[D−] | Quiet Rake             | Soft Swipes    | Both volume and enemy damage dip next.           | `MultiHitMod` **-1** | `EnemyDamageMod` **-10**  | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**. Hero multihit −1: see intro. |
| XC_MA_pp  | H[M+]/E[A+] | Volume Crescendo       | Hate Symphony  | You widen hits; their combo scaling climbs next. | `MultiHitMod` **+2** | `EnemyAmpMod` **+15**     | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                              |
| XC_MA_pm  | H[M+]/E[A−] | Shred the Song         | Broken Meter   | You widen hits; their amp decays next.           | `MultiHitMod` **+2** | `EnemyAmpMod` **-15**     | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**.                              |
| XC_MA_mp  | H[M−]/E[A+] | Tight Rope             | Spiteful Build | Fewer hits; they ramp next.                      | `MultiHitMod` **-2** | `EnemyAmpMod` **+15**     | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**. Hero multihit −2: see intro. |
| XC_MA_mm  | H[M−]/E[A−] | Sparse Line            | Dampened Spite | Both multihit and enemy amp ease next.           | `MultiHitMod` **-1** | `EnemyAmpMod` **-10**     | Base: **Dmg 100%**, **Spd 100%**, **Hits 1**. Hero multihit −1: see intro. |
| XC_MM_pp  | H[M+]/E[M+] | Twin Storms            | Mirror Hail    | Both widen multihit next.                        | `MultiHitMod` **+1** | `EnemyMultiHitMod` **+1** | Same as DE_M_align_PP.                                                     |
| XC_MM_pm  | H[M+]/E[M−] | Overwhelming Tide      | Snapped Chain  | You add ticks; theirs shortens next.             | `MultiHitMod` **+2** | `EnemyMultiHitMod` **-2** | Same as DE_M_opp_HP.                                                       |
| XC_MM_mp  | H[M−]/E[M+] | Controlled Pace        | Needle Torrent | You tighten; they spray next.                    | `MultiHitMod` **-1** | `EnemyMultiHitMod` **+2** | Same as DE_M_opp_EH.                                                       |
| XC_MM_mm  | H[M−]/E[M−] | Sparse Exchange        | Broken Rhythm  | Both lose extra hits next.                       | `MultiHitMod` **-1** | `EnemyMultiHitMod` **-1** | Same as DE_M_align_MM.                                                     |


---

## 4. Row counts (acceptance)


| Section                 | Count   |
| ----------------------- | ------- |
| §1 Hero same-character  | 12      |
| §1 Enemy same-character | 12      |
| §2 Dual same-stat       | 16      |
| §3 Cross-stat quadrants | 64      |
| **Total catalog rows**  | **104** |


---

## 5. In-game data (implemented)

- **Runtime data:** the **104** catalog rows are stored in [GameData/Actions.json](../../GameData/Actions.json) with `**category`:** `ModTrade`, `**tags`:** `modtrade`, and the same `**action`** / `**description**` rules as above. The generator removes any existing mod-trade rows (same tag/category), then re-appends the catalog so the list stays canonical.
- **Regenerate** after catalog edits: `python scripts/generate_mod_trade_actions.py` (from repo root).

### Optional follow-up

1. Wire selected **action** names (NameHero / NameEnemy as generated) into **weapons** / **enemy pools** / labs as needed (they are not auto-equipped).
2. Spot-check in Action Interaction Lab: consumed % and multihit match expectations.
3. If you need **multihit “pay”** to actually remove ticks, adjust hit pipeline or encode the pay as lower **NumberOfHits** on the follow-up weapon template.

