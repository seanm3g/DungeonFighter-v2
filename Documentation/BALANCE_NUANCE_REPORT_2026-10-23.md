# Balance & Nuance Report — overnight run 2026-10-22/23

Same DF.dll, two frozen GameData copies differing only in `Actions.json` (**base** = before the action rework,
**new** = after). 12.5 h on 24 cores. Raw tables are in the appendix.

| Study | Size |
|---|---|
| ACTIONVALUE (every hero action, 2-slot strip vs weapon basic) | L1/10/20/35/50 × 2 arms × 400 fights |
| Catalog builds (23 builds = 4 classes × 13 materials, median stepping stone) | 8 levels × 2 arms × 64 fights × 12 enemies |
| Full playthroughs (basic + smart bot, retreat at 50%) | ~1,500 runs to L10, 800 from L10→L20 (smart L10→L20 timed out: no data) |
| Rooms (`DF_SIM_ROOM`, new): every authored room × every catalog build | 116 rooms + neutral × 23 builds × L10/L35 |
| Deep balance-scan (6 heroes × 8 dungeons per class/level) | L5–L75, both arms |

## 1. The action rework is balance-neutral overall

- Deep scan, catalog builds and playthroughs move within noise (L10 reach: Barbarian 70→73%, Wizard 82→86%, smart bot ±3).
- **Fixed after the run:** LEECHING CUT's 25% lifesteal was broken (value index 21,807 at L20; a plain attack is ~124).
  Swept 25/10/5/2% → **2%** lands next to the other heal actions (129 at L10, 236 at L35 vs CRIT HEAL 286/194). Applied.
- Changed actions now pricing **weak** (below a plain attack at most levels): CHANNEL (the −20% speed costs more than +50% AMP
  pays), LOOSE GRIP (−2 weapon damage outweighs +2 speed), RAPID STRIKE (2 × 50% ≈ plain attack), HAYMAKER late (110 at L50 vs 155).
- Changed actions pricing **strong**: PHANTOM FOLLOW-UP (+1 hit next action: 222 at L35 vs 160 plain — same as old RAPID STRIKE),
  REWRITE FATE (die → 18: 208 at L50). TAUNT went from dead weight to useful at L20+ (191/203 at L35/50).

## 2. Classes: Barbarian is behind, Rogue ahead (mid/late game)

Kill time in enemy swings, median catalog build (lower = faster):

| Level | Barbarian | Warrior | Rogue | Wizard | slowest / fastest |
|---|---|---|---|---|---|
| L10 | 6.9 | 8.0 | 7.7 | 6.7 | 1.19 |
| L35 | 10.1 | 8.2 | 6.7 | 8.3 | **1.51** |
| L50 | 9.4 | 7.4 | 6.3 | 8.8 | **1.49** |
| L100 | 9.2 | 7.5 | 6.3 | 8.1 | **1.47** |

- At L35 and L50 **all six Barbarian builds sit in the slower half**, and they also lose the most HP per fight (21–29%).
- Real play agrees early: basic-bot reach-L10 is Barbarian 73%, Warrior 78%, Rogue 82%, Wizard 86%.
- The classes differ in *speed* far more than in *how* they win — kill time and HP lost move together (fast builds also lose
  less HP), so there is no "slow but tanky" class right now.

## 3. Materials: big power gaps, identical mechanics

Same-level spread is large: at L50 Glass/Obsidian builds kill in ~5.9 swings, Silver in 10.5 (1.8×).

| Fastest (L50) | KillT / HP lost | Slowest (L50) | KillT / HP lost |
|---|---|---|---|
| Obsidian | 5.8 / 15% | Silver | 10.5 / 26% |
| Glass | 5.9 / 14% | Iron | 9.5 / 25% |
| Shadow | 6.6 / 16% | Bone | 9.3 / 20% |

Why they feel samey (design, not numbers):
- **Every material is the same machine**: bank a currency on a trigger (RAGE on taking a hit, CRISIS on enemy HP
  thresholds, GRAZE on hit…) and its convert action spends it as **+% damage**. `keywordScale` is `DAMAGE` on all 12.
- The per-currency percent rates (`materialConvertPercentByKeyword`) were tuned to equalize output, which flattens
  identity further.
- Piece triggers are a grab-bag, not themes: "+10% AMP on first hit" is on Bone, Glass, Shadow, Silver and Leather; poison
  on Glass/Shadow/Obsidian; "crit on 18–19" on Iron/Willow/Silver.
- `MaterialBuilds.json`: Gold's feed reads "per MITHRIL" and Mithril's "per GOLD" — looks swapped.

## 4. Environments: they hurt, but they hurt everyone the same

- Median room adds **+4 HP points lost per fight** and makes fights ~3% shorter (hazards hit the enemy too).
  **34 of 116 rooms change HP lost by < 2 points** — flavor only.
- **Class spread is tiny**: the difference in room penalty between the most and least affected class is 0.8 pts (L10) /
  1.3 pts (L35) for the median room. The "hardest for" class is just the weakest class overall (Barbarian at L35 in
  nearly every room), not a room-specific matchup.
- **Material spread is tiny too**: every material's kill-time ratio ranges 0.15–0.25 across rooms with no material
  standing out — no room favors a currency (e.g. frost rooms don't feed Gold's DRAG, fire rooms don't feed Crystal).
- Oddity: the most punishing rooms are the *holy* ones — Celestial Sanctum, Sacred Altar, Heavenly Chamber (+11 HP pts),
  ahead of Magma Pool and Glacial Chamber. Probably Sacred Flames / Divine Blessing hitting the hero; worth a look.

## 5. Global: the game is too easy from L10 on (pre-existing)

Dungeon clear 97–100% vs targets 65–75%, zero deaths at every level, basic bot L10→L20 100%. Class/material nuance
tuning should be measured after the difficulty curve is back near target, or it will be judged against fights nobody loses.

## Proposals

### A. Data-only (I can sim-test and apply on your OK)
1. **Material rates**: raise LENS (Silver), CRISIS (Iron), RAGE (Bone), TRIUMPH (Steel); trim GRAZE, TROPHY, SWING — target
   ≤1.25× spread at L35/L50. First confirm with `BUILDLAB --loo-step` that the convert (not a skill/weapon step) is the gap.
2. **Barbarian**: after (1), lift remaining gap with `classBalance.Barbarian` or Barbarian class actions.
3. **Action fixes**: LOADED DICE roll 12→16 (12 is often worse than the natural roll), CHANNEL drop the speed penalty,
   LOOSE GRIP −2→−1 damage, RAPID STRIKE 2×50%→2×60%, PHANTOM FOLLOW-UP trim to ON COMBO; remove SELF TARGET (hits
   yourself; value 15–55) and the two unimplemented cards (SET BONUS, FLIP ROOM SWITCH) from gear pools.
4. **Late defense**: WEAKEN/CHALLENGE/WARCRY/CONFUSE reach 200–264 at L50 — `statusWeakenDamageMultiplier` 0.5 may be too strong late.
5. Fix the Gold/Mithril feed text; inspect the holy rooms' hazards.

### B. Code (for the nuance you asked for)
1. **Material payout identity** — convert actions pay out in their own currency instead of damage only, e.g.
   Bone RAGE → heal, Steel TRIUMPH → armor/fortify, Iron CRISIS → execute bonus vs low-HP enemies, Glass GRAZE → crit,
   Shadow SWING → speed / retrigger, Obsidian TROPHY → persists across fights, Willow CHAIN → AMP, Silver LENS → focus/crit,
   Crystal CONTACT → burn, Bronze BEAT → swing speed, Gold DRAG → enemy slow / damage down, Mithril STREAK → accuracy.
   Hook: `MaterialSetController.TryResolveConvertScale` already reads `keywordScale`; it only knows `DAMAGE`.
2. **Room ↔ build interactions** — room tags (fire, water, earth, air, exposed, elegant, dilapidated, overgrown…) feed
   statuses or currencies so the same room is good for one build and bad for another (fire: burn → Crystal; water/frost:
   slow → Gold; dilapidated: armor break → Iron; elegant: crit → Glass/Silver). Success metric: room class spread ≥ 3 pts
   and material ranges that differ by material.
3. **Re-theme piece triggers** per material around (1), so 2/3/5-piece bonuses reinforce one identity.

## How to rerun

- Room permutations: set `DF_SIM_ROOM=<room location>` for any BUILDLAB / ACTIONVALUE / SEQLAB run (new; see
  `TestCharacterFactory.CreateTestEnvironment`).
- Runner + analyzer used tonight were scratch scripts; they can be moved into `Scripts/` if you want to keep them.

## Appendix: raw tables

## ACTIONVALUE (100 = same as weapon basic alone; best slot, mean over 4 weapons)

| Action | Tier | L1 base→new | L10 base→new | L20 base→new | L35 base→new | L50 base→new |
|---|---|---|---|---|---|---|
| LEECHING CUT | 3 | –→137 | –→535 | –→21807 | –→21582 | –→2158 |
| CRIT HEAL | 3 | 109→109 | 282→282 | 144→144 | 200→200 | 203→203 |
| REPLACE ROLL | 3 | 115→115 | 155→155 | 135→135 | 209→209 | 281→281 |
| CONFUSE | 2 | 109→109 | 125→125 | 161→161 | 214→214 | 245→245 |
| CHALLENGE | 3 | 96→95 | 103→103 | 146→146 | 206→206 | 264→264 |
| PHANTOM FOLLOW-UP | 2 | 101→112 | 114→115 | 124→155 | 161→222 | 161→205 |
| WARCRY | 3 | 102→102 | 106→106 | 141→141 | 198→198 | 245→245 |
| HONE | 1 | 129→129 | 124→124 | 152→152 | 203→203 | 177→177 |
| WEAKEN | 2 | 106→106 | 119→119 | 151→151 | 194→194 | 199→199 |
| COMBO END HEAL | 3 | 117→117 | 135→135 | 123→123 | 210→210 | 181→181 |
| REWRITE FATE | 3 | 94→100 | 101→135 | 105→115 | 134→192 | 124→208 |
| BASH | 3 | 111→112 | 121→122 | 159→158 | 188→187 | 168→168 |
| WHETSTONE | 1 | 129→129 | 120→120 | 144→144 | 195→195 | 160→160 |
| TAUNT | 3 | 93→97 | 99→102 | 117→137 | 153→191 | 162→203 |
| EXPOSE | 2 | 106→106 | 118→118 | 143→143 | 185→185 | 178→178 |
| SHARPEN | 1 | 124→124 | 124→124 | 139→139 | 182→182 | 154→154 |
| VULNERABILITY | 2 | 107→107 | 115→115 | 142→142 | 187→187 | 169→169 |
| ECHO SPELL | 3 | 102→102 | 121→121 | 126→126 | 169→169 | 195→195 |
| BARBARIAN RAGE | 3 | 104→103 | 116→116 | 134→134 | 183→183 | 173→173 |
| STRIKE | 0 | 104→104 | 114→114 | 133→133 | 177→177 | 175→175 |
| PARRY | 3 | –→101 | –→112 | –→128 | –→174 | –→186 |
| RETRIGGER SLOT | 3 | 104→104 | 123→123 | 128→128 | 172→172 | 173→173 |
| POUND | 0 | 103→103 | 114→114 | 131→131 | 171→171 | 173→173 |
| HAYMAKER | 3 | 123→131 | 127→127 | 152→159 | 172→165 | 121→110 |
| SLOW | 2 | 105→105 | 115→115 | 133→133 | 173→173 | 164→164 |
| SKULL CRACK | 3 | –→107 | –→112 | –→142 | –→173 | –→154 |
| RETRIGGER FINISHER | 3 | 104→104 | 123→123 | 128→128 | 165→165 | 166→166 |
| HARDEN | 2 | 105→105 | 117→117 | 128→128 | 168→168 | 164→164 |
| AFTER MISS HEAL | 3 | 128→128 | 113→113 | 124→124 | 160→160 | 155→155 |
| LUCK | 2 | 107→107 | 117→117 | 122→122 | 167→167 | 165→165 |
| BLUDGEON | 3 | 104→103 | 113→113 | 129→129 | 169→168 | 163→163 |
| KEEN EYE | 1 | 101→101 | 114→116 | 124→124 | 161→163 | 161→171 |
| ROLL FIFTEEN | 3 | 112→112 | 123→123 | 130→130 | 163→163 | 146→146 |
| STAB | 0 | 109→109 | 110→110 | 134→134 | 166→166 | 156→156 |
| CUNNING | 3 | 102→102 | 114→114 | 127→127 | 163→163 | 159→159 |
| STEEL CREST | 3 | 101→101 | 113→113 | 135→135 | 160→160 | 155→155 |
| RAPID STRIKE | 1 | 112→102 | 115→111 | 155→128 | 222→165 | 205→157 |
| ACTION BONUS | 1 | 104→104 | 112→112 | 128→128 | 169→169 | 151→151 |
| CRITICAL ATTACK | 1 | 101→101 | 114→114 | 124→124 | 161→161 | 161→161 |
| SLAM | 0 | 106→106 | 115→115 | 124→124 | 162→162 | 153→153 |
| PRE-FIGHT PLANNING | 1 | 102→102 | 114→114 | 127→127 | 161→161 | 155→155 |
| GAMBLERS BLESSING | 3 | 101→102 | 112→126 | 119→121 | 153→156 | 141→153 |
| RETRIGGER OPENER | 3 | 100→100 | 116→116 | 124→124 | 158→158 | 158→158 |
| NATURAL SEVEN | 3 | 101→101 | 112→112 | 125→125 | 168→168 | 149→149 |
| FOCUS | 1 | 101→101 | 114→114 | 124→124 | 161→161 | 155→155 |
| FROST BOLT | 3 | –→101 | –→111 | –→125 | –→163 | –→154 |
| MITHRIL FLOW | 3 | 101→101 | 113→113 | 124→124 | 160→160 | 157→157 |
| GLASS NICK | 3 | 101→101 | 113→113 | 124→124 | 160→160 | 155→155 |
| IRON CULL | 3 | 101→101 | 113→113 | 124→124 | 160→160 | 155→155 |
| PROWESS RUSH | 3 | 101→101 | 113→113 | 124→124 | 160→160 | 155→155 |
| DUELIST FEINT | 3 | 101→101 | 113→113 | 124→124 | 160→160 | 155→155 |
| GOLD ANCHOR | 3 | 101→101 | 113→113 | 124→124 | 160→160 | 155→155 |
| PUNCH | 0 | 101→101 | 113→113 | 124→124 | 160→160 | 155→155 |
| RAGECALLER HOWL | 3 | 101→101 | 113→113 | 124→124 | 160→160 | 155→155 |
| WILLFIRE | 3 | 101→101 | 113→113 | 124→124 | 160→160 | 155→155 |
| HEXBLADE CURSE | 3 | 101→101 | 113→113 | 124→124 | 160→160 | 155→155 |
| MIND HEX | 3 | 101→101 | 113→113 | 124→124 | 160→160 | 155→155 |
| RAVAGER FLURRY | 3 | 101→101 | 113→113 | 124→124 | 160→160 | 155→155 |
| WITS ARC | 3 | 101→101 | 113→113 | 124→124 | 160→160 | 155→155 |
| RESONATE | 3 | 101→101 | 113→113 | 124→124 | 160→160 | 155→155 |
| SPELLBLADE WEAVE | 3 | 101→101 | 113→113 | 124→124 | 160→160 | 155→155 |
| FINESSE THRUST | 3 | 101→101 | 113→113 | 124→124 | 160→160 | 155→155 |
| UMBRA | 3 | 101→101 | 113→113 | 124→124 | 160→160 | 155→155 |
| PIERCE | 2 | 101→101 | 113→113 | 124→124 | 160→160 | 155→155 |
| BODY BLOW | 3 | 101→101 | 113→113 | 124→124 | 160→160 | 155→155 |
| WARBRUTE SMASH | 3 | 101→101 | 113→113 | 124→124 | 160→160 | 155→155 |
| MAGIC MISSILE | 0 | 101→101 | 113→113 | 123→123 | 160→160 | 156→156 |
| ENRAGE | 3 | 101→100 | 116→113 | 120→116 | 166→159 | 167→164 |
| JAB | 2 | 102→102 | 112→112 | 123→123 | 157→157 | 156→156 |
| STRIP DISABLE | 3 | 101→101 | 109→109 | 122→122 | 152→152 | 165→165 |
| MOMENTUM | 1 | 103→103 | 112→112 | 120→120 | 161→161 | 147→147 |
| MEASURED CUT | 3 | 100→100 | 109→112 | 114→121 | 146→154 | 136→154 |
| SPRINT | 1 | 103→103 | 111→111 | 120→120 | 157→157 | 147→147 |
| EMPOWERED | 1 | 101→102 | 112→112 | 119→121 | 153→158 | 140→144 |
| CONCENTRATE | 3 | 103→103 | 112→112 | 118→118 | 161→161 | 141→141 |
| LUCKY CHARM | 1 | 101→101 | 112→112 | 119→119 | 153→155 | 141→147 |
| EXTRA EFFORT | 1 | 102→102 | 112→112 | 120→120 | 155→155 | 143→143 |
| SALVAGE MISS | 3 | 107→107 | 112→112 | 119→119 | 153→153 | 141→141 |
| READ BOOK | 1 | 102→102 | 112→112 | 121→121 | 155→155 | 143→143 |
| STRIP RANDOM | 3 | 101→101 | 112→112 | 122→122 | 146→146 | 152→152 |
| STRIP LOOP | 3 | 101→101 | 113→113 | 119→119 | 148→148 | 151→151 |
| CUT | 0 | 103→103 | 111→111 | 121→121 | 154→154 | 142→142 |
| STRIP JUMP | 3 | 101→101 | 110→110 | 118→118 | 153→153 | 147→147 |
| WILLOW LASH | 3 | 101→101 | 112→112 | 119→119 | 155→155 | 142→142 |
| REACTION TRAINING | 1 | 102→102 | 111→111 | 119→119 | 153→153 | 143→143 |
| STRIP REPLACE | 3 | 100→100 | 113→113 | 120→120 | 155→155 | 141→141 |
| MIGHTY SWING | 3 | 102→102 | 112→112 | 120→120 | 154→154 | 140→140 |
| MEASURED ADVANCE | 1 | 101→100 | 112→113 | 119→119 | 155→154 | 147→142 |
| DANCE | 1 | 101→101 | 111→111 | 119→119 | 154→154 | 143→143 |
| DEEP BREATHING | 1 | 101→101 | 111→111 | 119→119 | 154→154 | 143→143 |
| DRUM CUT | 3 | 101→101 | 112→112 | 119→119 | 154→154 | 142→142 |
| FIGHT BONUS | 1 | 101→107 | 112→103 | 119→119 | 153→153 | 141→144 |
| BONE WRATH | 3 | 101→101 | 112→112 | 119→119 | 155→155 | 141→141 |
| STEADY HAND | 1 | 101→101 | 112→112 | 119→119 | 153→153 | 141→141 |
| QUIET MIND | 1 | 101→101 | 112→112 | 119→119 | 154→154 | 141→141 |
| CONCENTRATION | 1 | 101→101 | 113→113 | 119→119 | 152→152 | 141→141 |
| EXPLODING DICE | 3 | 101→101 | 112→112 | 119→119 | 153→153 | 141→141 |
| ROOM CLEAR HEAL | 3 | 101→101 | 112→112 | 119→119 | 153→153 | 141→141 |
| HEADHUNT | 3 | 101→101 | 112→112 | 119→119 | 153→153 | 141→141 |
| REFRACT | 3 | 101→101 | 112→112 | 119→119 | 153→153 | 141→141 |
| JUICED | 1 | 101→101 | 112→112 | 119→119 | 153→153 | 141→141 |
| CRIT MISS HEAL | 3 | 101→101 | 112→112 | 119→119 | 153→153 | 141→141 |
| TAKING AIM | 1 | 101→101 | 112→112 | 119→119 | 153→153 | 141→141 |
| BERSERKER | 3 | 101→101 | 112→112 | 119→119 | 153→153 | 141→141 |
| BLUR | 1 | 101→101 | 112→112 | 119→119 | 153→153 | 141→141 |
| SLASH | 0 | 106→106 | 112→112 | 119→119 | 152→152 | 137→137 |
| STRONGER | 1 | 104→104 | 110→110 | 122→122 | 157→157 | 131→131 |
| AVENGE | 3 | 101→101 | 110→110 | 120→120 | 149→149 | 140→140 |
| CRIT FACES | 3 | 101→101 | 112→112 | 125→125 | 154→154 | 126→126 |
| STRIP SHUFFLE | 3 | 101→101 | 112→112 | 117→117 | 148→148 | 139→139 |
| UNLUCK | 2 | 101→101 | 112→112 | 117→117 | 148→148 | 139→139 |
| MIGHT | 3 | 102→104 | 111→112 | 115→118 | 146→151 | 128→130 |
| STRIP REPEAT | 3 | 101→101 | 113→113 | 121→121 | 140→140 | 137→137 |
| OPENER | 1 | 96→96 | 107→107 | 117→117 | 152→152 | 138→138 |
| STRIP SKIP | 3 | 101→101 | 109→109 | 113→113 | 138→138 | 147→147 |
| BOLT | 0 | 100→100 | 108→108 | 117→117 | 149→149 | 135→135 |
| CONCENTRATED CHAOS | 3 | 99→99 | 112→112 | 117→117 | 143→143 | 134→134 |
| MOMENTUM BASH | 1 | 102→102 | 111→111 | 115→115 | 146→146 | 128→128 |
| FIGHTER | 1 | 102→102 | 111→111 | 115→115 | 146→146 | 128→128 |
| MARATHON | 1 | 102→102 | 110→110 | 115→115 | 145→145 | 129→129 |
| DUNGEON ANALYSIS | 1 | 102→102 | 111→111 | 115→115 | 145→145 | 126→126 |
| RUSH | 1 | 101→101 | 110→110 | 114→114 | 143→143 | 126→126 |
| SET BONUS | 3 | 101→101 | 110→110 | 113→113 | 143→143 | 125→125 |
| STRIP STOP | 3 | 101→101 | 110→110 | 113→113 | 143→143 | 125→125 |
| FLIP ROOM SWITCH | 3 | 101→101 | 110→110 | 113→113 | 143→143 | 125→125 |
| FINISHER | 1 | 101→101 | 110→110 | 110→110 | 139→139 | 124→124 |
| LOOSE GRIP | 1 | 101→96 | 112→107 | 119→108 | 153→137 | 141→136 |
| MISDIRECT | 3 | 94→94 | 104→104 | 110→110 | 139→139 | 132→132 |
| LOADED DICE | 3 | 99→99 | 99→99 | 111→111 | 137→137 | 121→121 |
| CHANNEL | 3 | 88→90 | 93→93 | 107→103 | 148→132 | 124→112 |
| RETRIGGER NEXT | 3 | 92→92 | 101→101 | 94→94 | 108→108 | 116→116 |
| SELF TARGET | 2 | 55→55 | 15→15 | 20→20 | 38→38 | 24→24 |

## Catalog builds (median stepping stone per level, neutral room)

KillTime = enemy swings to kill (lower = faster). Class spread = slowest class / fastest class.

| Level | Barbarian | Warrior | Rogue | Wizard | class spread | worst build | best build |
|---|---|---|---|---|---|---|---|
| L1 | 10.4 (10.4) | 9.0 (9.0) | 9.1 (9.1) | 8.2 (8.2) | 1.27 | B1 10.4 | Z1 8.2 |
| L5 | 6.0 (6.0) | 6.2 (6.3) | 6.4 (6.4) | 6.3 (6.4) | 1.08 | Z2 6.8 | B1 6.0 |
| L10 | 6.9 (7.0) | 8.0 (8.0) | 7.7 (7.7) | 6.7 (6.8) | 1.19 | R6 9.3 | Z5 5.5 |
| L20 | 9.5 (9.7) | 9.9 (10.1) | 9.4 (9.3) | 8.7 (8.9) | 1.14 | W4 10.7 | Z1 7.2 |
| L35 | 10.1 (10.2) | 8.2 (8.4) | 6.7 (6.7) | 8.3 (8.5) | 1.51 | B6 11.4 | Z1 5.9 |
| L50 | 9.4 (9.6) | 7.4 (7.5) | 6.3 (6.3) | 8.8 (8.8) | 1.49 | Z3 11.9 | R2 5.0 |
| L75 | 8.6 (8.8) | 7.3 (7.4) | 6.6 (6.8) | 7.7 (7.6) | 1.29 | B3 10.9 | Z5 5.1 |
| L100 | 9.2 (9.5) | 7.5 (7.6) | 6.3 (6.3) | 8.1 (8.1) | 1.47 | B3 10.3 | Z5 5.4 |

(values: new data, base data in parentheses)


### Per material (mean KillTime / HP lost of builds using it, new data)

| Material | L1 | L5 | L10 | L20 | L35 | L50 | L75 | L100 |
|---|---|---|---|---|---|---|---|---|
| Bone |  |  | 7.2 / 11% | 9.6 / 19% | 9.7 / 17% | 9.3 / 20% | 8.2 / 54% | 9.2 / 96% |
| Bronze |  |  | 7.9 / 13% | 9.8 / 24% | 9.1 / 20% | 7.6 / 20% | 6.3 / 59% | 7.1 / 98% |
| Crystal |  |  | 6.1 / 10% | 8.5 / 20% | 8.7 / 18% | 8.8 / 22% | 7.8 / 66% | 8.0 / 97% |
| Glass |  |  | 7.9 / 13% | 10.2 / 24% | 6.5 / 13% | 5.9 / 14% | 6.0 / 48% | 6.0 / 88% |
| Gold |  |  | 7.7 / 12% | 10.2 / 20% | 7.6 / 13% | 7.2 / 15% | 7.9 / 54% | 7.7 / 94% |
| Iron |  |  | 6.7 / 11% | 9.6 / 23% | 10.6 / 23% | 9.5 / 25% | 9.4 / 74% | 9.6 / 97% |
| Leather |  |  | 9.0 / 15% | 10.3 / 24% | 6.4 / 11% | 6.9 / 14% | 7.9 / 54% | 7.3 / 95% |
| Mithril |  |  | 7.8 / 13% | 9.1 / 22% | 8.6 / 18% | 7.3 / 18% | 7.1 / 57% | 7.4 / 96% |
| Obsidian |  |  | 7.8 / 13% | 9.2 / 23% | 6.6 / 14% | 5.8 / 15% | 6.3 / 54% | 6.2 / 92% |
| Shadow |  |  | 5.7 / 9% | 7.5 / 17% | 6.9 / 14% | 6.6 / 16% | 6.8 / 57% | 6.2 / 94% |
| Silver |  |  | 7.1 / 12% | 9.2 / 21% | 9.5 / 19% | 10.5 / 26% | 8.8 / 74% | 9.0 / 97% |
| Steel |  |  | 7.1 / 12% | 9.0 / 21% | 10.3 / 19% | 9.2 / 20% | 8.1 / 57% | 8.9 / 95% |
| Willow |  |  | 7.0 / 11% | 8.4 / 20% | 7.3 / 16% | 8.0 / 20% | 7.5 / 63% | 8.3 / 96% |

## Full playthroughs (bot plays real dungeons; reach% = got to the target level)

| Config | Barbarian base→new | Warrior base→new | Rogue base→new | Wizard base→new |
|---|---|---|---|---|
| l10-basic | 70%→73% (n=400) | 78%→78% (n=400) | 80%→82% (n=400) | 82%→86% (n=400) |
| l10-smart | 80%→79% (n=200) | 80%→80% (n=200) | 89%→86% (n=200) | 96%→94% (n=200) |
| l20-basic | 100%→99% (n=200) | 100%→100% (n=200) | 100%→100% (n=200) | 100%→100% (n=200) |

## Rooms at L10 vs neutral room (all 23 catalog builds)

kill = KillTime ratio (1.10 = fights 10% longer); HP = extra % max HP lost per fight; class spread = difference in room penalty between the most and least affected class (higher = more class nuance).

Median room: kill x0.97, HP +4.2 pts, class spread 0.8. Rooms with |HP| < 2 pts: 34/116.

| Room | kill | HP | class spread | hardest for | easiest for |
|---|---|---|---|---|---|
| Celestial Sanctum | 0.93 | +11.4 | 2.0 | Rogue | Wizard |
| Sacred Altar | 0.94 | +11.3 | 1.3 | Rogue | Wizard |
| Heavenly Chamber | 0.93 | +11.2 | 1.7 | Warrior | Wizard |
| Glacial Chamber | 0.97 | +8.6 | 0.7 | Rogue | Wizard |
| Frozen Cavern | 0.96 | +8.6 | 0.8 | Rogue | Wizard |
| Magma Pool | 0.98 | +8.4 | 2.1 | Rogue | Wizard |
| Sacred Grove | 0.97 | +8.0 | 0.9 | Barbarian | Wizard |
| Fire Chamber | 0.99 | +6.5 | 0.9 | Barbarian | Wizard |
| Tomb Entrance | 0.96 | +6.5 | 1.7 | Rogue | Wizard |
| Throne Room | 0.94 | +6.3 | 1.4 | Barbarian | Wizard |
| Wildflower Valley | 0.96 | +6.2 | 1.5 | Rogue | Wizard |
| Lava Moat Bridge | 0.95 | +6.2 | 0.6 | Barbarian | Rogue |
| … | | | | | |
| Deep Tunnel | 0.98 | -1.0 | 1.5 | Barbarian | Rogue |
| Mining Shaft | 0.98 | -1.0 | 2.1 | Barbarian | Rogue |
| Rocky Outcrop | 0.98 | -1.0 | 1.5 | Barbarian | Rogue |
| Peak Summit | 0.98 | -0.9 | 1.8 | Barbarian | Rogue |
| The Summit | 0.97 | -0.6 | 1.1 | Barbarian | Wizard |
| Endless Maze | 0.97 | -0.6 | 1.5 | Barbarian | Warrior |
| Hedge Corridor | 0.98 | -0.6 | 1.1 | Wizard | Rogue |
| Cliffs Edge | 0.97 | -0.6 | 0.9 | Barbarian | Rogue |

| Material | mean kill ratio across rooms | best room ratio | worst room ratio | range |
|---|---|---|---|---|
| Shadow | 0.979 | 0.92 | 1.07 | 0.15 |
| Obsidian | 0.971 | 0.90 | 1.08 | 0.18 |
| Gold | 0.983 | 0.91 | 1.10 | 0.19 |
| Crystal | 0.957 | 0.88 | 1.07 | 0.19 |
| Glass | 0.992 | 0.91 | 1.11 | 0.20 |
| Mithril | 0.976 | 0.91 | 1.11 | 0.20 |
| Silver | 0.990 | 0.93 | 1.14 | 0.21 |
| Steel | 1.009 | 0.93 | 1.14 | 0.22 |
| Bronze | 0.961 | 0.88 | 1.09 | 0.22 |
| Willow | 0.994 | 0.92 | 1.14 | 0.23 |
| Leather | 0.983 | 0.90 | 1.12 | 0.23 |
| Bone | 0.989 | 0.90 | 1.13 | 0.23 |
| Iron | 1.019 | 0.92 | 1.17 | 0.25 |

## Rooms at L35 vs neutral room (all 23 catalog builds)

kill = KillTime ratio (1.10 = fights 10% longer); HP = extra % max HP lost per fight; class spread = difference in room penalty between the most and least affected class (higher = more class nuance).

Median room: kill x0.98, HP +3.7 pts, class spread 1.3. Rooms with |HP| < 2 pts: 34/116.

| Room | kill | HP | class spread | hardest for | easiest for |
|---|---|---|---|---|---|
| Heavenly Chamber | 0.98 | +10.8 | 3.8 | Barbarian | Rogue |
| Sacred Altar | 0.97 | +10.6 | 3.7 | Barbarian | Rogue |
| Celestial Sanctum | 0.97 | +10.2 | 4.3 | Barbarian | Rogue |
| Magma Pool | 1.00 | +7.8 | 3.2 | Barbarian | Rogue |
| Frozen Cavern | 0.99 | +7.8 | 2.1 | Barbarian | Rogue |
| Glacial Chamber | 0.98 | +7.6 | 1.5 | Barbarian | Rogue |
| Sacred Grove | 1.00 | +7.0 | 2.5 | Barbarian | Rogue |
| Tomb Entrance | 0.97 | +5.9 | 1.3 | Barbarian | Rogue |
| Fire Chamber | 1.01 | +5.7 | 2.2 | Barbarian | Rogue |
| Lava Moat Bridge | 0.99 | +5.6 | 2.2 | Barbarian | Rogue |
| Blue Vent Basin | 0.99 | +5.6 | 2.4 | Barbarian | Rogue |
| Wildflower Valley | 0.97 | +5.5 | 1.5 | Barbarian | Rogue |
| … | | | | | |
| Peak Summit | 0.98 | -1.5 | 0.4 | Barbarian | Warrior |
| Mining Shaft | 0.99 | -1.5 | 1.1 | Wizard | Warrior |
| Deep Tunnel | 0.98 | -1.4 | 1.2 | Barbarian | Warrior |
| Rocky Outcrop | 0.99 | -1.4 | 1.9 | Barbarian | Warrior |
| Magic Vault | 1.05 | -0.8 | 0.4 | Rogue | Warrior |
| Magma Chamber | 1.05 | -0.8 | 0.8 | Rogue | Warrior |
| Endless Maze | 0.99 | -0.8 | 0.3 | Barbarian | Warrior |
| Volcanic Vent | 1.06 | -0.8 | 0.9 | Barbarian | Warrior |

| Material | mean kill ratio across rooms | best room ratio | worst room ratio | range |
|---|---|---|---|---|
| Glass | 1.005 | 0.94 | 1.09 | 0.15 |
| Bone | 0.995 | 0.93 | 1.09 | 0.16 |
| Leather | 1.007 | 0.93 | 1.10 | 0.17 |
| Obsidian | 0.993 | 0.92 | 1.09 | 0.18 |
| Gold | 0.991 | 0.92 | 1.10 | 0.18 |
| Shadow | 1.002 | 0.93 | 1.11 | 0.18 |
| Mithril | 0.970 | 0.89 | 1.08 | 0.18 |
| Bronze | 0.974 | 0.89 | 1.09 | 0.20 |
| Steel | 0.985 | 0.89 | 1.10 | 0.21 |
| Crystal | 0.983 | 0.89 | 1.11 | 0.21 |
| Iron | 0.996 | 0.91 | 1.13 | 0.22 |
| Willow | 1.015 | 0.92 | 1.15 | 0.24 |
| Silver | 1.004 | 0.89 | 1.15 | 0.25 |

## Deep scan (base)
```
Level summary
 level step  median turns  bot acts   clear  target   death
     1    0          10.3         -       -       -       -
     5    1           8.4       6.0     98%     75%      0%
    10    2           9.4       3.9    100%     75%      0%
    20    2          12.2       2.4    100%     65%      0%
    35    3          11.1       1.4    100%     65%      0%
    50    4          10.4       1.1    100%     65%      0%
    75    5          10.2       1.1    100%     30%      0%
   100    6           6.3         -       -       -       -
```

## Deep scan (new)
```
Level summary
 level step  median turns  bot acts   clear  target   death
     1    0          10.3         -       -       -       -
     5    1           8.4       6.2     97%     75%      0%
    10    2           9.3       4.2    100%     75%      0%
    20    2          12.1       2.6    100%     65%      0%
    35    3          10.4       1.6    100%     65%      0%
    50    4          10.1       1.1    100%     65%      0%
    75    5          10.1       1.2    100%     30%      0%
   100    6           6.3         -       -       -       -
```