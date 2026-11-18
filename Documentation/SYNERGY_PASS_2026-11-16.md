# Synergy pass (2026-11-16)

Goal (user): items, actions and class skills should synergize in meaningful ways and support diverse, mixed builds.
Built on branch `v45-item-gen-redesign`, uncommitted. **Unbalanced: no sims run** (implement first, measure when asked).

## What changed

### 1. One rule grammar for everything (`Code/Combat/CrossWires.cs`)

Skill nodes, item prefixes and base modifiers all speak this language now. New pieces:

| Piece | Syntax | Example |
|---|---|---|
| Gates | `WHEN [EVERY n] [ONCE] [CHANCE n%] IF ...` | `ONCRITICAL CHANCE 35% IF WEAPON mace -> APPLY stun 1` |
| Conditions | `WEAPON mace`, `PREVTAG t` / `NEXTTAG t` (strip neighbour, wraps), self keys `STR AGI TEC INT HITSTREAK EXTRASLOTS` | `WHILE IF TAG barbarian AND PREVTAG bludgeon -> DAMAGE 12%` |
| Scaling | `PER TARGET status`, `PER SELF key` or bare key (`PER SPENT`, `PER ROLL`, `PER RAGE`), `PER STAT INT 10`, `PER WORN barbarian`, `PER TAGS wizard`, `PER QUALITY a\|b`; trailing number = divisor (whole steps) | `WHILE IF WEAPON wand -> AMP 2% PER STAT INT 10` |
| Passive stats | `AMP`, `MULTIHIT`, `STR`, `AGI`, `TEC`, `INT` (plus DAMAGE ARMOR SPEED HIT COMBO CRIT POTENCY) | `WHILE -> STR 1 PER WORN barbarian` |
| Sequence verb | `NEXT stat n[%] [FOR k] [RAMP\|DECAY] [IFTAG tag]`: a TURN grant on your next roll(s); IFTAG waits for (and only counts down on) an action with that tag | `ONCOMBO IF TAG focus -> NEXT AMP 40% IFTAG status` |
| Rank scaling | skill-node rule amounts × rank × node `power` | ranked rule nodes |

Attribute passives fold into `GetSkillAttributeBonus`; nested attribute reads skip rule attributes (no recursion).
`CrossWires.LearnedNodeRules`, `NodeCardBonuses` (action-card preview), `WornCount`, `PerValue` are the helpers.

### 2. Base modifiers change behavior (`Scripts/build-item-mods.py --modifiers-only`)

> Superseded 2026-11-17: base modifiers no longer grant any % bonus; see `Documentation/BASE_MODIFIERS_2026-11-17.md`.

Every base still has one fixed modifier; the shapes per tier are now:

| Tier | Shapes |
|---|---|
| 1 | class DAMAGE / SPEED, DoT POTENCY (replaced 2026-11-17, see BASE_MODIFIERS_2026-11-17.md) |
| 2 | tag DAMAGE / SPEED, **tag after another tag in the strip** (`PREVTAG`) (replaced 2026-11-17) |
| 3 | tag actions **apply a status** (EVERY 2), **feed the next action** (`NEXT DAMAGE`), **grant a resource** (Haste / Focus / Harden), **mint a class store** |
| 4 | NEXT with a condition, a **setup that waits for another tag** (`NEXT AMP ... IFTAG`), tag-before-tag SPEED, crits apply 2 of a status |
| 5 | **producer + payoff pair** (APPLY brittle + DAMAGE PER TARGET brittle), ramping NEXT + same-tag chain SPEED, resource + CRIT while it is up |

Bases name **tags** (class, hybrid, theme: bludgeon / swift / focus / status / verb / store / finisher), not single actions, so a
piece pulls toward a family of actions; hybrid tags in each class's pool pull toward mixing. `BaseModifiers.json` `tags` holds
the per-class pools. Counts over 409 bases: DAMAGE 112, SPEED 97, NEXT 92, APPLY 69, ADD 45, POTENCY 28, CRIT 26, MINT 21.
The bot values triggered modifiers by event rate × strip share (`ItemValueModel.TriggeredValue`).

### 3. Statuses (`Code/Combat/TempoStatuses.cs`, stores in `Stores.cs`)

| Status | On | Effect | Spent by |
|---|---|---|---|
| **Brittle** | foe | next crit taken +25% per stack (`statusBrittleCritDamagePerStack`) | that crit |
| **Stagger** | foe | its next action takes +15% longer per stack (`statusStaggerLengthPerStack`) | its next action |
| **Haste** | you | your actions 10% faster per stack (`statusHasteSpeedPerStack`) | one stack per own action |
| **Rattle** | foe | −1 to its next attack roll per stack (the roll-penalty status) | its next attack |
| **Mark** (fixed) | foe | next hit that lands is a crit (was never enforced in combat; now it is) | that hit |

Max 5 stacks (`statusExtraMaxStacks`). All are stores, so `APPLY`, `CASH`, `SCALE`, `DETONATE`, `TARGET x`, `PER TARGET x` and
`ONSTATUSAPPLY:x` work on them. Stronger existing statuses: stack cap 3 → 4, Expose / Focus / Harden 0.15 → 0.20 per stack,
status cash 6% → 8% per unit.

### 4. Actions (29 new rows at the end of `GameData/Actions.json`)

- Status appliers in every class and most hybrids: STAGGERING BLOW, BLOOD RUSH, SUNDER, GUT CHECK, HAMSTRING, RATTLING BLOW,
  QUICKSTEP, BARGE, FIND WEAKNESS, FADE STEP, HEX, JINX, ENERVATE, TIME WARP, CRYSTALLIZE.
- Status payoffs (spend for damage): TOPPLE (Stagger), SHATTERPOINT (Brittle), FLASH CUT (Haste), MIND BREAK (Rattle).
- Sequence setups that wait for a tag (`timing.ifTag`): COILED STRIKE → next Bludgeon, DRILL → next 2 Swift, PRIMER → next Focus,
  FEINT → next Rogue, SET THE TRAP → next Status action.
- Strip composition (`timing.perTag`): WAR BAND (per Warbrute action), CHORUS (per Wizard action), PACK TACTICS (per Status action).
- **RELEASE** verb (UNLEASH 25%, CODA 40%): every queued TURN grant pays all its remaining rolls now, and strip-slot grants waiting
  on other slots move here (`CharacterEffects.ReleaseQueuedBonuses`). The bank-then-cash sequence play.

### 5. Skills: custom code → rules

57 nodes moved from `SkillEffectRouter` code to `crossWire` rules (`customEffectId: "rule"`): Ages, Kinships, Rites, roll
passives, streaks, quality prefers, heals, Deep Cut / Toxin Pressure, convert masteries (now `ONCASH:<store>`, so any spend of
that store triggers them, not only the convert action), status-trigger skills (`ONSTATUSAPPLY:x -> BUFF stat n FOR dungeon`),
Concussive Crit, Perfect Aim, Arcane Weave, Concentrated Chaos, Precision. Their router code is deleted.

Left as code (29): loot bias (4), class-tag roots (4), cadence steps (4), Synthesis mint feed (4), and genuinely unique state
machines (Iron Hide, Titan, Living Alloy, Cadence Keeper, Flow State, Second Wind, Second Chance, Snake Eyes, Technical Debt,
Dagger Mastery, Spell Memory, Archmage, Grand Equation).

### 6. Skill trees diverge

- **Tier bands on every class branch** (`SkillTreePrerequisites.GetBandPredecessorOptions`): same-tier skills on a branch are
  parallel options; each needs any one skill from the nearest lower tier (Core already worked this way). Hybrid trees unchanged.
- **Either-or choices** (`excludes`, checked both ways): 32 new rule nodes, 8 per class, each an alternative to an existing node
  on the same branch and tier: a tempo / status variant, a sequence variant, and on the Rite branch a **three-way cross-class
  pick** (e.g. Barbarian: Gut Instinct / Borrowed Fury `ONCASH:GRAZE -> MINT RAGE 50%` / Stormcaller's Pact
  `ONCASH:CHAIN -> ADD harden 1`). Card text shows "Either-or with ...".

## Tests

`SynergyPassTests` (62 checks, filter `SynergyPass`): grammar, every catalog rule parses, PER / rank scaling, adjacency, NEXT,
IFTAG, RELEASE, new stores, Mark, Brittle, Stagger / Haste, either-or, new actions. Updated: ClassTreeEffects, SkillEffectRankScaling,
StatusTriggerSkill, ClassTags (Concussive), ItemRoller, ActionBonusLoadedActions. Full run: no failures beyond those HEAD code
shows on the same GameData (convert damage off in the active patch, max health, audio, UI widths, travel text, "Hit without combo").

## Bot awareness

The synergy bot (`RulesSynergy`, `PlaythroughRulesPolicy.SynergyStrip`) now plays the sequence pieces:
- `StripContextAdjust` re-scores picks against the rest of the strip (two rounds): an ifTag setup's grant counts only with
  a tagged action on the strip, a perTag grant scales with the tagged picks, RELEASE needs (and grows with) grant sources,
  a status CASH needs an applier of that status.
- `VerbSynergy`: status CASH / SCALE is gated on owning an applier (not on a formed set); RELEASE on owning a grant source.
- `SequenceOrder` after timing order: payoffs after their appliers, setups right before the first action with their tag,
  RELEASE last.

- Skill picks: an either-or node the mined order doesn't list shares its partner's mined score, and `RuleFit` (share of
  the tags / statuses / stores / weapon its rules name that the hero's strip and gear have, × `ruleFitScale` 0.5) decides
  between partners. Fit applies only to either-or nodes.

## Measured (paired, seed 1, L8/18/38/58, 4 shards × 2 heroes × 6 dungeons per class, local)

HEAD code (before) vs this pass with the bot changes. Files: `GameData/TuningRuns/synergy-pass-2026-11-16/`.

| | L8 | L18 | L38 | L58 |
|---|---|---|---|---|
| clear | 80 → 77 | 74 → 74 | 76 → 72 | 47 → 48 |
| status_eff | 0.12 → 0.16 | 0.16 → 0.26 | 0.95 → 1.29 | 0.83 → 1.19 |
| skill_eff | 3.8 → 3.9 | 9.1 → 8.7 | 9.4 → 8.9 | 8.1 → 8.9 |
| mod_eff | 9.9 → 6.4 | 12.4 → 17.6 | 23.6 → 21.4 | 11.4 → 13.9 |

- 21% of fights carry a new action; Brittle became the most-applied status (3549 vs bleed 746), from CRYSTALLIZE / SUNDER
  and Fault Lines.
- Either-or pairs **flip, they don't split**: every hero of a class takes the same side (r-feint 12 / r-grease 0; before
  0 / 11). Most new nodes name the class's own tags, so they always fit. The three-way cross-class Rite picks are never taken.
- Warrior clear fell at L38 / L58 (78 → 52, 46 → 22; ~30 dungeons each) after swapping Wind / Ex-Mithril for Hamstrung /
  Flowing: probably weaker alternatives, unconfirmed.

## Not done / next

- Make either-or partners ask for different builds (one needs status appliers, the other bludgeon / verbs / another
  class's store) so heroes split; make the cross-class Rite picks reachable.

- No balance pass: every new number is a first guess. Balance with `/balance`.
- Not playtested in the UI: the skill tree screen now has more same-tier nodes per branch.
- Measure divergence: `skill_eff`, `offclass_share`, `status_eff` in the fun report before / after.
