# Card rules: one effect column (plan, 2026-11-23)

## Goal

A card is its **body** plus its **rules**.

- **Body**: name, damage, hits, speed, block, tags, tier, rarity, description.
- **Rules**: a list of rule strings in the same grammar items and skills use (`CrossWireRule`). They fire only on the card's own swing, as card rules do today (`ActionRules`).

Every effect a card has today moves into `rules`. Then the engine has one path for card effects, and the sheet, the editor, the bot and tooltips each read one column.

## Where card effects live today (Actions.json, 221 cards)

| Column(s) | Cards | Engine path | Notes |
|---|---|---|---|
| `verb` (APPLY 23, CASH 15, RELEASE 2, DETONATE 1) | 41 | card rules (`ActionRules.FromVerb`) | already a rule |
| `triggerBundlesJson` | 31 | card rules (`ActionRules`) | WHEN + `mechanics` id + count; 21 mechanics ids (heal, strip_*, retrigger_*, replace_next_roll, crit_face_min, salvage_miss, hero_action_damage) |
| `cadenceBundlesJson` | 110 | timing system (`CadenceTiming`, `CadenceScopedBuffApplicator`) | TURN 66, ACTION 27, DUNGEON 10, FIGHT 7; `timing` options on 18 |
| `actionAttackBonusesJson` | 83 | timing system | **derived**: all 83 match the stat columns plus the cadence bundle exactly (0 mismatches), so it is a cache, not data |
| `damageMod` `speedMod` `ampMod` `multiHitMod` `weaponDamageMod` `weaponSpeedMod` `heroHit` `heroCombo` `heroCrit` `heroSTR/AGI/TECH/INT` `heroHeal` | ~90 | timing system | the amounts the cadence bundle's `mechanics` names point at |
| status columns `weaken` `expose` `harden` `slow` `pierce` `disrupt` `confuse` + `selfTargetEffects` | ~25 | whole-row apply (skipped for parts a card rule owns, `ActionTriggerOwnership`) | the amount sits in the column; the cadence bundle gives the duration |
| `materialScale` + `keywordScale` + `scaleFormula` | 18 | convert-card damage scaling | three columns for "+n damage per unit of a currency" |
| old event columns `onCrit` `onKill` `onMiss` `onRoomsCleared` `onRollValue`, `triggerConditions` | ~9 | old trigger path | ROLL FIFTEEN, ROOM CLEAR HEAL, … |
| `cost` (HP 5), `lifesteal`, `opener`, `finisher`, `target` | ~8 | each its own code | |

The code behind the extra columns: ~6,900 lines across the sheet column families, spreadsheet DTOs and converters, cadence editor sync, the attack-bonus model and the action model converter, plus 11 files that read the row-apply / cadence buff path.

## What the rule grammar is missing

Each gap is listed with the smallest change that closes it. "Reuse" means an existing verb takes it on.

1. **Timed bonuses (the big one).** Cards grant stat bonuses over a span: this swing, the next N rolls (TURN), the next N strip slots (ACTION), the fight, the dungeon. Optional timing: slot, delay, shape ramp/decay/charge, every, perSlot, slotIf, untilMiss, ifTag, perTag. The rule grammar now spells part of this three ways (`SWING`, `NEXT … FOR k RAMP|DECAY IFTAG`, `BUFF … FOR fight|dungeon`), plus `CRITFACE`.
   **Change:** one verb, `GRANT stat n FOR swing | rolls k | slots k | fight | dungeon [timing options]`, compiled onto the existing `CadenceTiming` grant code, so there is one engine for timed bonuses. SWING, NEXT, BUFF and CRITFACE become spellings of GRANT. Stats: DAMAGE SPEED AMP MULTIHIT HIT COMBO CRIT STR AGI TEC INT WEAPONDAMAGE WEAPONSPEED HEAL.
2. **Statuses.** Reuse: `APPLY weaken 1` (target) and `ADD harden 1` (self; replaces `selfTargetEffects`). A status's duration comes from the status, as it does when items and skills apply it.
3. **Currency scaling on converts.** Reuse CASH with a per-card rate: `CASH GRAZE all INTO DAMAGE 5` replaces the three scale columns, so converts and cash-ins become one thing.
4. **Old event columns.** Plain rules: `ONCRITICAL -> …`, `ONKILL -> …`. With IF available on cards, ROLL FIFTEEN becomes `ONATTACK IF ROLL=15 -> HEAL 6`, and NATURAL SEVEN becomes `ONATTACK IF NATROLL=7 -> HEAL 10` (new self key NATROLL, the die face). ONROLLVALUE and ONNATURALROLL can then retire.
5. **Cost.** Either a body field (kept as it is) or a verb `PAY HP n%` that raises ONSELFCOST. **Your call** (decision D3).
6. **One-offs with no home yet.** Each gets a store or verb, or is cut if it isn't worth one:
   - pierce (3 cards)
   - disrupt (3)
   - confuse (1)
   - advantage / disadvantage (1 each)
   - exploding dice (1)
   - lifesteal (1; `HEAL n%` of damage may already cover it)
   - opener / finisher (`IF SLOT=1` / `IF SLOTSLEFT=0` cover it)
   - target

## Phases

Each phase ends green: tests pass, plus the equivalence check from phase 0.

**Phase 0: inventory and oracle.**
- Write a card snapshot tool. For every card it runs one scripted swing on fixed seeds (Action Lab / SEQLAB style) and records what happened: damage, grants queued (stat, span, amounts per roll), statuses applied, stores changed, strip moves, HP paid.
- That snapshot is the oracle for every later phase. Seeded DUNGEONSIM runs are bit-identical when nothing changes, so a fixed-seed sim pair is the second oracle.
- List the bot and pricing readers of card columns: RulesSynergy, ItemValueModel, ACTIONVALUE / SEQLAB, MCP `get_action_details`, tooltips, the Action Lab.

**Phase 1: grammar.**
- Add GRANT (over `CadenceTiming`), the CASH per-card rate, NATROLL, and PAY if chosen.
- Map SWING, NEXT, BUFF and CRITFACE onto GRANT.
- Give each one-off a home.
- Unit tests only; no data changes.

**Phase 2: converter, side by side.**
- A one-time converter writes a `rules` array on every card from its columns and keeps the columns.
- The loader builds the card from `rules` when present (flag `cardRulesOnly`, off by default).
- Run the snapshot both ways: every card must match. Then run the seeded sim pair: outputs must be identical.

**Phase 3: readers.**
- Switch the bot, pricing, tooltips, MCP details and the Action Lab to read card rules.
- Check with the paired first-hub ScoreFinal (bot-change protocol) that the bot didn't drift.

**Phase 4: authoring.**
- The sheet gets one RULES column (pull and push). The in-game action editor gets a rules text box that shows parse errors.
- The per-event column families, cadence editor sync and attack-bonus JSON leave the sheet code.

**Phase 5: delete.**
- Drop the old columns from Actions.json, the row-apply path, `ActionTriggerOwnership`, `actionAttackBonusesJson`, the spreadsheet trigger fields and the old event columns.
- Retire ONROLLVALUE and ONNATURALROLL.
- Saves name actions rather than store columns, so no save migration (to confirm in phase 0).
- Expected: a few thousand lines out.

**Phase 6: confirm.**
- Seeded DUNGEONSIM pair, old vs new, must be identical. Any difference is a bug, not a balance change.
- Update CLAUDE.md and The Wiring: card mechanics (section 4h) collapses to "body + card rules".

## Phase 0 results (2026-11-23)

**The oracle: `DF.exe CARDSNAP [--level 30] [--fights 3] [--seed 1] [--card NAME] [--out dir] [--compare old-summary.tsv]`** (`Code/Game/Tuning/Profiles/CardSnapshotRunner.cs`)

- **What it runs:** every hero card from Actions.json (218; the 3 enemy-tagged swarm rows are skipped) in two strips: card first, and card last, with two plain weapon basics around it.
- **Setup:**
  - three seeded fights per strip at L30 vs Goblin
  - hero HP ×20 and enemy HP ×0.3, so fights run long enough for effects to show
  - every material currency seeded at 5
  - a material's convert card runs in typical gear made of that material, so its set is formed
- **Output:** `cardsnap.txt` (24 MB) and `cardsnap-summary.tsv` (a hash per card). The text holds the per-event combat log plus a state line on every change: hero HP and attributes, every non-zero store on both sides, and queued grants (roll grants `T[…]`, slot grants `S[…]`, the next-action bank `N[…]`). The bank needed a read-only accessor, `CharacterEffects.PeekNextActionBank()`.
- **Repeatable:** two full runs match on all 218 cards. A run takes about 85 s.
- **Sensitive:** WIND UP's +25% changed to +26% in a scratch data copy flips exactly one card (WIND UP).
- **Baseline:** `GameData/TuningRuns/cardsnap-baseline-2026-11-23/cardsnap-summary.tsv` (gitignored; `git add -f` to commit it). It holds 233 entries: 218 hero cards and 15 enemy kits.
- **How to use it:** take a baseline before a phase, then run with `--compare` against the baseline summary after. Exit code 1 lists the cards that changed.

**Readers of card effect columns: up to 82 files** (some property names, such as DamageMod and Cost, are shared with other classes, so this is an upper bound). Phase 3 must move these to card rules:

- **Bot and pricing:** `PlaythroughRulesPolicy`, `PlaythroughSmartPolicy`, `RulesSynergy`, `ItemValueModel`, `BuildLadderRunner`, `SkillValueRunner`
- **Tooltips and the strip:** `CombatActionStripBuilder` (+ `.Tooltips`, `.LegacyLines`), `ActionSynergy`, `ActionCardExternalBonusCollector`, `HoverEffectCopy`, `ModifierTooltipFormatter`, `ActionDetailRenderer`, `SkillTreeRenderer`
- **Editor:** `ActionFormSectionBuilders` (+ `.CadenceMechanics`), `ActionsTabManager`, `CreateActionFormRenderer`, `Game/Editors` (2)
- **MCP:** `ScreenDescriber`
- **Engine:**
  - `Actions/Execution` (4), `Actions/Conditional` (3), `Actions/RollModification`, `ActionCadenceDurationResolver`
  - `Combat/Verbs` (2), `Combat/Rules` (2), `Combat/Calculators` (2), `StatusEffectActionResolver`, `EffectHandlerRegistry`, `CombatEffectsSimplified`, `ActionMechanicTagProcessor`, `MaterialSetController`, `CrossWires`, `Combat/Narrative`
  - `Entity/CharacterEffects(State)`, `Game/SkillTree` (3), `World/*Affix*`, `World/EnvironmentalActions`
- **Data:** about 20 load and sheet files (they go away in phases 4–5)

**Scope found in phase 0:**

- **Enemy kits use the same columns.** EnemyActions.json's 15 templates carry `triggerBundlesJson` (4), `expose`, `lifesteal`, `weaken`, `slow`, `disrupt`, `harden`, `selfTargetEffects` and `pierce`, so they are in scope.
- **CARDSNAP's enemy pass covers them (added 2026-11-24).** Each template is the Goblin's only action, against a Sword hero with STRIKE > SLAM > STAB, on the same seeds; entries are named `enemy:<TEMPLATE>`. It uses a new diagnostics hook, `ActionLabEncounterSimulator.EnemySetup`.
  - A full run is now 233 entries, and two runs match on all of them.
  - The 218 hero entries are unchanged from the hero-only baseline.
  - Removing ENEMY SHELL's self-target Harden in a scratch copy flips only `enemy:ENEMY SHELL`.
- **Status columns are flags, not amounts, and can be dead.** `harden`, `weaken`, `slow`, `expose` and the rest load as `Causes*` booleans, so "2" means the same as "1". On ENEMY SHELL the `harden` flag does nothing at all: changing it, or removing it, leaves the snapshot identical, because `selfTargetEffects: harden` is what applies the Harden. The converter (phase 2) should therefore follow what the engine actually reads, not the column names. The snapshot is what proves it did.
- **Room hazards are out of scope.** EnvironmentalActions.json (30 rows) has its own format (`causesWeaken`, `damageMultiplier`, …).
- **Saves don't store card columns.** Saves keep card names only: `comboStripActionNames`, and each item's `gearAction` (`"SLAM"`). Phase 5 needs no save migration.
- **A behaviour worth checking:** WIND UP's next-action bank reaches +100% (`N[DAMAGE_MOD100]`) when grants pile up before a card spends them. The snapshot records today's behaviour as it is; whether stacking is intended is a design question.

## Decisions for you

- **D1 When card rules fire.** Today, effects fire when the card's slot fires, which is a landed action roll. Proposal: they stay card rules that fire on the card's own swing, with `ONCONNECT` as the default WHEN (a bare effect line with no WHEN means ONCONNECT).
- **D2 Authoring.** Does the Google Sheet stay an authoring tool (RULES column, pull and push), or do Actions.json and the in-game editor become the only places cards are written? Recent card edits have been direct Actions.json commits.
- **D3 Cost.** A body field, or the `PAY` verb.
- **D4 Descriptions.** Keep the 217 hand-written descriptions, or generate them from the rules (always accurate and mechanics-only, which matches the card-description rule) with an optional override.
- **D5 Verb name.** `GRANT` for the merged timed bonus, or another word.
- **D6 One-offs.** Keep or cut pierce, disrupt, confuse, advantage/disadvantage, exploding dice and lifesteal (about 10 cards in total).

## Risks

- **Timing.** The timing system is the largest piece (110 cards, 9 timing options). GRANT has to compile onto it exactly; phase 0's snapshot is what catches a drift.
- **Bot.** The synergy bot is the calibration bot. A reader change that shifts its picks moves every balance number, hence the paired check in phase 3.
- **Sheet.** Data still in the old sheet columns is ignored once phase 4 lands. Pull the sheet once and convert before that.
