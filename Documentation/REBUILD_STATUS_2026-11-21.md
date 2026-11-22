# Rebuild status — evening run, Phase 11 and steps 3+ (2026-11-21)

Branch `phase11-item-rolls` (from `v47-reimagine` at `4869c669`), main checkout. Decisions:
[`CORE_DESIGN_PASS_2026-11-17.md`](CORE_DESIGN_PASS_2026-11-17.md). Same rules as the 11-18 run
([`REBUILD_AGENT_BRIEF_2026-11-18.md`](REBUILD_AGENT_BRIEF_2026-11-18.md)): implement the decisions, don't
redesign; one commit per sub-step; switch off before deleting; old saves load; no balance work (no sims, no patch
edits); tests after every sub-step. Builds and test runs go to a scratch folder so a running `DF.exe` isn't locked.
Nothing pushed, no PR.

Already done before tonight (see [`REBUILD_STATUS_2026-11-18.md`](REBUILD_STATUS_2026-11-18.md) and git log):
steps 0, 1, 2; step 6's engine merge and fixed resolution order; step 8's verb cut (CASH IN / DETONATE).

## When you're back

**Done tonight:** Phase 11 (item roll table) and the code side of build steps 3–9, one commit each on
`phase11-item-rolls`. Last full run: **15138 passed**; the remaining failures were 4 tree-shape expectations, since
fixed and rerun, plus 1 known flaky test. Nothing pushed. **Not started:** steps 10 (Fragile cards, Jam / Burn out),
11 (the ~100-action content and lore pass) and 12 (deleting the switched-off code). 12 should wait until you've
played with the switches on.

**Partial steps:** much of what's left in steps 3–9 is board or screen work I couldn't check by eye (Fernis's
shoulder sprite, the status panel and NOW card, NEW cards, strip-editor arrows) and content rewrites (region
rosters, enemy skins, materials onto chain moments, cutting filler skill nodes). Each step's "Not done" list below
names them.

**Things that change how the game plays** (no sims were run; over-v57 was calibrated before these):
- Class hits apply their status (SLAM Stagger, STRIKE Guard, STAB Bleed, MAGIC MISSILE Burn).
- Room enemies show one modifier: a prefix or Rare affix replaces the region affix, so they're a bit weaker.
- Prefixes have one rule each (Giant +30% HP only, Frozen Staggers, Venomous Bleeds, ...).
- Poison content Bleeds, Stun / Slow Stagger, and so on (`DF_LEGACY_STATUSES=1` reverts).
- Wood / Cloth / Unknown no longer drop or form sets (`DF_LEGACY_MATERIALS=1` reverts).
- 1 in 4 empty rooms has an encounter (a shrine heals 15%, a merchant gives a consumable, ...), in sims too.
- Mythic items carry a class-bending rule; Core slot nodes add strip slots; material branches need 2 pieces worn.
- Below Epic, base modifiers no longer use RANK / TAGAS / SETCOUNT.

**REVIEW items** (placeholders the 11-18 defaults allow): action roles seeded by a rule, Fernis's lines, family
matchups, prefix rules, Mythic rules, encounter outcomes, region levels, Weaken → Rattle.

**Test-runner side effect:** every full `--run-tests` rewrote the active patch `over-v57.json`. I restored it from
git after each run; it is in no commit.

## Summary

| Sub-step | Status | Commit | Tests |
|---|---|---|---|
| Phase 11 item rolls | done | `6cd48a60` | 15063 passed / 0 failed |
| 3.1 Action roles, class hits | done | `9c9d9d9d` | 15074 passed / 0 failed |
| 3.2 + 3.3 Fernis, swing moments, fight results | done | `8123dd8b` | 15086 passed / 2 failed (both flaky, see below) |
| 4 Families, matchups, one modifier, prefixes, regions, encounters | partial | `234f1287` | 15106 passed / 0 failed |
| 5 Core statuses, NEW moment | partial | `4204f65f` | 15128 passed / 0 failed |
| 6 Event labels (11), modifiers scoped by rarity | partial | `b582683f` | 15124 + label fixes; affected suites 0 failed |
| 7 Mythic class-bending rules | done | `3c6fc5c9` | 15141 passed / 0 failed |
| 8 Cosmetic Wood / Cloth / Unknown | partial | `c542f44e` | 15140 passed / 1 failed (then fixed; ItemRoller 0 failed) |
| 9 Core slot nodes, material branch gate | partial | `2dc79522` | 15138 passed / 5 failed (4 tree-shape expectations fixed, 1 known flaky) |
| 10 Late rungs (Fragile cards, Jam / Burn out, callouts) | not started | — | — |
| 11 Content and lore pass | not started | — | — |
| 12 Cleanup (delete switched-off code) | not started (needs your review of the switches first) | — | — |

## Phase 11 — item rolls (D11.1–D11.12)
- **Class affinity (D11.7):** every rolled item has one of 10 affinities (`Item.ClassAffinity`, saved as
  `classAffinity`; `ClassAffinity` in `Code/Data/ItemGeneration/`). It is the base's class, or at
  `hybridAffinityShare` (0.3) one of that class's 3 hybrids. The class drop-rate bonus still acts on the base's class.
- **Material follows the affinity (D11.8):** the class-material share (`weaponClassMaterialShare` 0.9 /
  `armorClassMaterialShare` 0.5, unchanged) now picks from the affinity's materials (3 for a core class, 6 for a hybrid).
- **Slot domains (D11.4):** every family has `slots` and `classes` in `ItemMods.json`, written by
  `Scripts/build-item-mods.py` (`AFFINITY`). The roller already filtered `slots` before the roll; now the data uses it.
  Stat prefixes span two slots each so every slot keeps three or more (chest and feet would otherwise have only
  Quality) — **REVIEW**.
- **Off-slot escape (D11.5):** once per item at its drop rarity's chance (`offSlotChance`: Rare 1%, Epic 2%,
  Legendary 4%, Mythic 8%), never the first mechanic line, one tier lower, marked `offSlot` on the line and
  `offslot:<family>` in the roll spec. Measured: 2.1% of items across all rarities. The loot reveal doesn't call it
  out yet.
- **Class tilt (D11.2):** own ×3.5, related (a hybrid and its parent) ×1.2, off-class and neutral ×1; class names no
  longer count as influence tags. The first mechanic line must serve the affinity (then a related class) when one
  can roll. Measured on mechanic lines: own 63%, related 29%, off 8% (target ~70/20/10). Related is high because
  hybrid items (30%) draw mostly from their parents; that is the design, so I didn't push further.
- **Cuts and remaps (D11.9, D11.11):** the 12 class cross-wires and Venom left loot (rows kept for old saves);
  Envenomed → Bleed, Concussive / Leaden / Hourglass → Stagger, Sapping → Rattle, Hunter's / Bountied → Expose.
  Bleeding is now Rogue-tagged.
- **Composites (D11.10):** Chimera, Lynx, Djinn (suffix creatures), Rampant, Fencing, Arcing (prefixes), and four
  mechanic composites (Executioner's Eye, Bookended, Hexed, Quickening). Every hybrid has 2+ lines.
- **13 gap lines (D11.12):** in `Modifications.json` with poetic suffix names. Guard = Harden for now.
- **Tests:** 7 new roller tests (affinity + material on every item, slot gate, off-slot rare/single/never below Rare,
  first mechanic line on class, class tilt shares, every hybrid has lines, every item rule parses). Two tests changed
  with the decisions: material-pool cross-wires (18 → 6) and weapon materials (now "the affinity's materials").
- **Not done:** runtime checks of the new rules beyond parsing (they use existing verbs; `PER SELF harden` reads the
  hero's Harden through `Stores`); the loot reveal and item card don't show the affinity or "rare circumstance" yet.
- **Side effect seen:** a full `--run-tests` rewrote the active patch `over-v57.json` (it wrote the new knobs and
  dropped the description). I restored it from git; it is not in any commit. Same pre-existing issue as 11-18.

## Step 3 — Action interactions, roles, Fernis

### 3.1 Action roles and class hits — done
- New `role` column in `Actions.json` (setup / sequence / payoff, D5.1), carried through the sheet → data → action
  pipeline (`Action.Role`, `ActionData.Role`). `ActionRoles.Of` reads it and infers one when it's empty (enemy kits,
  environments, old data). All 221 rows were seeded by a feature rule: payoff verbs and converts → payoff; strip and
  retrigger cards → sequence; anything that buffs the next action or applies a status → setup; else payoff. Result:
  142 setup, 62 payoff, 17 sequence. **REVIEW**: the rule is mechanical; many "setup" rows are setup only because
  they carry a modifier column. The ~100-action content pass (step 11) is where roles get authored by hand.
- Cards lead with the role: `Payoff · applies 1 Bleed` (`ActionRoles.CardLine`, in the strip card body). A card with
  no authored text shows its verb.
- **Class hits apply the class status** (D5.1 / D3.23): SLAM `APPLY stagger 1`, STRIKE `APPLY harden 1` (Guard =
  Harden for now, so it reads "gains 1 Guard"), STAB `APPLY bleed 1`, MAGIC MISSILE `APPLY burn 1`. These are card
  rules through the existing verb path (`ActionRules.FromVerb`).
- **Not done:** the arrows between linked cards in the strip editor (needs a screen I can't check tonight).
- Tests: new `ActionRolesTests` (13 checks). One card-body test now expects the role word first.

### 3.2 + 3.3 Fernis D Man, swing moments and fight result lines — done
- **Line bank** `GameData/Fernis.json` (D9.5, D9.9): 10 moments (fight won, comeback, death, chain complete, chain
  broken, overkill, close call, threat, NEW concept, dungeon summary), lines by class with an "any" fallback, tokens
  `{hero} {enemy} {turns} {links} {cause} {hp} {status}`. All lines are drafts in his voice (true facts, bad
  advice, D9.7): **REVIEW** in the step 11 lore pass.
- **Budget** (`FernisNarrator`): at most one line per hero swing; big moments (death, comeback, chain complete,
  close call, NEW) always speak, small ones 35%.
- **Swing moments** (`FernisFightSession`, D9.1), from the combat bus for this fight's hero and enemy only: chain
  broken with its cause (Rattle / a natural 1 / a missed roll), the whole strip completed, overkill (a killing blow
  worth half the enemy's max HP), close call (hero at 20% HP or less, once), enemy threat (enemy chain at 3+, once).
- **Fight result line:** death, a comeback after a close call, or a plain win with the swing count.
- **Where it shows:** the combat log, as `Fernis: ...`, queued and written after each step so a line never lands
  before its swing; and one line on the dungeon summary (best chain). Live, unmuted fights only (never in sims or
  muted tests); `DF_FERNIS=0` turns him off. `FernisNarrator.LastLine` is there for the shoulder sprite.
- **Not done:** the shoulder sprite and speech bubble on the board (D9.8) — needs the canvas, which I can't check
  tonight; the dungeon-select line (step 4) and NEW cards (step 5).
- **Tests:** new `FernisTests` (12 checks). Full run: 15086 passed, 2 failed. Both are known flaky: Stable "both
  fighters are out at once" (a fighter dies in room 1; listed in the 11-21 overnight report) and MCP "Expected
  DungeonSelection, got Death" (the hero died in the straight-in first dungeon); the MCP suite passed 4 of 4 reruns.

## Step 4 — Dungeons, rooms, bosses, regions (partial)

### Done
- **Six enemy families (D8.1)** in `GameData/EnemyFamilies.json` (`EnemyFamilies`): Brute (Brute, Berserker,
  Warlord), Knight (Knight, Duelist), Assassin, Sage, Trickster (Trickster, Acrobat) and Beast (a living Berserker
  without hands: Wolf, Bear, Boar, hounds). Each has "plays like" / "punished by" in hero words and a Fernis line.
  **Presentation only:** an enemy's stats still come from its 9-archetype profile, so no numbers move. All 128
  enemies map to a family. Family text and class matchups are **REVIEW**.
- **Matchup lines on dungeon select (D0.8)** (`DungeonMatchup`, hub right panel): a ✓ / ✗ / ~ for the hero's class
  with the dungeon's families ("✓ Brute ×2, Beast"), what the main family is weak to, the dungeon's lesson (its
  identity affix) and material, and Fernis's line, which gives the opposite advice (D9.7) while the mark stays
  true. Class matchups: each family is punished by one class and punishes one (Brute: Warrior / Rogue, Knight:
  Wizard / Barbarian, Assassin: Warrior / Wizard, Sage: Barbarian / Warrior, Trickster: Rogue / Barbarian, Beast:
  Wizard / Rogue) — **REVIEW**. Not checked on screen.
- **One visible modifier (D8.2)** (`AffixFeatures.OneModifier`, on; key `onemodifier` turns it off in
  `--features-off`): a room enemy keeps one of prefix > dungeon affix > region family. The Demon is exempt.
  **Gameplay effect:** prefixed and Rare enemies lose their region affix, so they are somewhat weaker than in
  over-v57's calibration.
- **Prefixes reworked to one rule each (D8.5, D8.9)** in `EnemyPrefixes.json`, on core statuses: Undead = its
  affinity only (immune to Bleed, Burn ×1.5), Giant = +30% HP, Tiny = evasion, Armored = 3 Guard, Ancient = one more
  signature, Corrupted → Rattle on hit, Frozen → Stagger on hit, Venomous → Bleed on hit, Hallowed = 1 Guard;
  Skeleton, Ghost, Feral, Blazing, Cursed unchanged. **REVIEW** (the rules are placeholders the doc allows).
- **Region requirements (D1.4)**: `Regions.json` `requirements` ("level n", "STR n", ...), checked by
  `RegionRequirements` before travel ("Rimewater requires level 5"). Seeded with level = the region's minimum − 1;
  no attribute requirements yet (**REVIEW**).
- **Non-hostile encounters (D8.10)** (`RoomEncounters`): 1 in 4 rooms without enemies holds a wanderer (a true hint
  about the boss's family), a shrine (+15% HP), a trapped creature (a consumable, or a 5% HP bite) or a merchant (a
  consumable), each with a Fernis line. They play out on entry; the design pictures choices (**REVIEW**). They also
  run in sims (their HP effects are real); only the text is muted.

### Not done (needs content or a screen)
- Region → dungeons → rooms → enemies restructure and regional rosters (D8.8): a content rewrite of Rooms.json /
  Dungeons.json; today dungeons still list `possibleEnemies`.
- Enemies as skins over archetype × kit × strip length (D8.8): needs the ~10 enemy kit templates of step 11.
- Room twists from the first dungeon (D8.11): room affixes already roll at 40% with no level gate, so this looks
  already true; not changed.
- Enemy strips short, Demons longest (D8.4): 124 of 128 enemies run 2 cards and the Demon gets up to 5; already in
  line, not changed.

## Step 5 — Statuses (partial)

### Done
- **The 7-status core** (`CoreStatuses`, D3.13–D3.16): Stagger, Guard, Bleed, Burn, Focus, Rattle, Expose. Guard is
  today's Harden store (shown as "Guard"). Retired ids resolve to their replacement wherever a status is applied
  (`EffectHandlerRegistry.ApplyEffect`) or read / added through `Stores`: Poison → Bleed, Stun / Slow → Stagger,
  Acid / Pierce / Vulnerability / Armor Break → Expose, Roll Penalty → Rattle, Fortify / Temp HP / Absorb → Guard
  (status applications only), Weaken → Rattle (**REVIEW**: not in the migration table), Brittle → Mark. Dropped:
  Silence, Stat Drain, Reflect, HP Regen, Disrupt, Confusion. Handler-less core statuses (Stagger, Rattle) apply one
  stack through their store.
- **Kept beyond the core:** Mark (the Glass material spends into it, so a rung needs it, D3.6) and Haste (tempo
  grants). Hero resources stay themselves in `Stores` (Temp HP cash-ins would otherwise become 143 Guard stacks).
- **Switch:** `DF_LEGACY_STATUSES=1` brings the old set back. 11 test suites that cover the retired statuses' code
  run with the legacy set pinned (`CoreStatuses.Pin(false)`), since that code stays until step 12; the new
  `CoreStatusesTests` (24 checks) pin the core on.
- **NEW moment (D9.3):** the first time a status lands in a live fight, `SystemUnlocks.OnStatusApplied` records it
  (opening the Statuses rung) and Fernis names it ("Rattle. New toy. I'd be furious.").
- **Gameplay effect:** Poison content now Bleeds (so Undead, immune to Bleed, shrug it off), Stun / Slow cards
  Stagger, and so on. Some cards stack the same status twice now (OPEN VEIN queues 8 Bleed where it queued 2 + Poison).

### Not done (needs the board)
- The status panel showing statuses only, and the NOW card (D3.7).
- NEW cards drawn on the enemy and the loot (D9.3); only Fernis's line exists.
- Direct code paths that apply a retired status without going through the registry or `Stores` (some older combat
  helpers call `ApplyPoison`-style methods directly) are not migrated.
- Affinities as immune / weak: `StatusAffinities.json` already does this by tag; not changed.

## Step 6 — Synergies and the one rule engine (rest)

### Done
- **11 player-facing events (D6.1):** every trigger event's card label now reads as one of hit, miss, crit, crit
  miss, action, chain end, kill, take hit, status, turn, with roll details as conditions ("On hit, right after a
  miss", "On hit or miss, odd roll", "On status: Stagger", "On action, 2+ hits in a row"). Tokens and aliases are
  unchanged, so no rule changes meaning. Three label tests updated.
- **Base modifiers scoped by rarity (D4.2, D6.5):** below Epic a base modifier is self-contained; RANK / TAGAS /
  SETCOUNT parts drop, and a base left with nothing rolls a self-contained class modifier instead
  (`BaseModifierRoller.ScopeToRarity`). New roller test.

### Not done
- **On chain complete** as its own event: Fernis already reacts to it (step 3), but no rule token raises it yet.
- Cutting the internal token set (the 30 tokens stay; the material earn rules that use ONODD, ONFIRSTHIT,
  ONHEALTHTHRESHOLD... move onto chain moments in step 8, which is a design-and-balance pass).

## Step 7 — Loot depth

- **Mythic = one class-bending rule per class (D4.1, D4.9):** every Mythic item carries its class's rule (a hybrid
  item: one of its parents' by roll), outside the line count: Barbarian **Worldsplitter's** (when a short chain of
  2 links or fewer ends, your opener fires again), Warrior **Bulwark-Born** (taking a hit counts as an action link),
  Rogue **Fatebreaker's** (crit on natural 18–20 this fight), Wizard **Spiral-Bound** (a missed link doesn't end
  your chain). Rows in `Modifications.json` (never rolled as lines; `build-item-mods.py` skips them). **REVIEW**:
  placeholders, as the 11-18 defaults allow.
- Six rarities kept; trade-up stays as the salvage sink (no change needed).
- Tests: new roller test (every Mythic has exactly one, the rules parse, nothing below Mythic carries one).

## Step 8 — Materials, sets, stores (partial)

### Done
- **16 materials (D4.3, D4.8):** Wood, Cloth and Unknown are cosmetic (`ItemMaterialRules.CosmeticMaterials`). They
  no longer drop, form no set (`MaterialBuildsLoader.GetActive`, used by the set controller, the rule book and CASH
  IN's fullest store), and Unknown no longer joins other sets as a wildcard. Old items keep the material name.
  `DF_LEGACY_MATERIALS=1` brings them back; `MaterialSetControllerTests` pins the old sets (their code stays until
  step 12).
- Already in place before tonight: a material creates its currency, stores bank until the dungeon exit (Dungeon
  scope), each class's three materials earn on different events (D6.3), verbs are CASH IN / DETONATE (D6.4, D6.6).

### Not done
- Moving the material earn rules onto chain moments (proposal 3: Willow on action, Crystal on chain complete, ...)
  and turning roll-detail events into conditions: a design-and-balance pass, not a rename.
- Spend vs scale on every consumer (D6.9): no consumer data was changed.

## Step 9 — Skills and classes (partial)

### Done
- **Buyable Core slot nodes (D5.3, D7.8):** two per core class tree, "Core: Extra Slot I" (tier 4, 2 points) and
  "II" (tier 8, 3 points), `customEffectId: core_slot`. Each adds one strip slot (`ComboSequenceMaxHelper`), outside
  the level cap like gear slots. They sit in existing tiers so no current node gains a new requirement; two
  tree-shape tests now count the bigger trunk.
- **Material branches open on 2 pieces** (Phase 7 proposal 5): `SkillTreeService.TryLearn` refuses a Bone / Iron /
  ... branch node until 2 pieces of that material are worn (`MaterialSetMissing`). The sim bots learn through the
  progression directly, so sims are not gated — the play path and the sims differ here (**REVIEW**).

### Not done
- Cutting filler nodes and making most nodes one-time picks (133 of 212 already are): a content pass on
  `SkillTrees.json` with balance impact.
- A prompt when a skill point arrives, Core-first presentation, and the class-points level-up moment: screens.
- The Wizard sequence vocabulary (MODIFY / TRIGGER / FLOW): content (step 11). Phase 11's Cascading, Foretelling,
  Recurring and Rekindling lines give Wizard items some of it already.

## Playtest (MCP, after the run)

The `dungeonfighter` MCP server didn't connect in this session, so I drove the scratch build's `DF.exe MCP` over
stdio with a small client (recommended choice each step, ~500 steps over several runs, all four classes reached
through the hub; the client never equips gear or edits the strip, so deaths are pessimistic).

**Working:** class hits apply their status ("applied stagger on Demonic Nectar Drone"); action cards lead with the
role ("Payoff · applies 1 Bleed · Damage: 0.4x, Multi-hit: 3 attacks", "Setup · Next action: +1 hit."); region
travel refuses a too-high region ("Mirrormere Shallows requires level 5."); empty-room encounters play (a shrine
+51 HP, a wanderer naming the boss's family); Fernis speaks on new statuses, chain breaks, close calls, comebacks,
deaths and wins. One run reached level 12 in 32 dungeons.

**Fixed after the playtest** (this commit):
- Fernis was off in MCP play: MCP runs its fights muted like a sim. New `FernisFightSession.McpPlay`, set by the
  MCP game wrapper; empty-room encounter text uses it too.
- Action cards dropped the class-hit verb when a card had other text, and showed a stray "|". The verb now leads.
- Fernis was too chatty: chain-broken lines need a chain of 2+ links, and an overkill needs a killing blow worth the
  enemy's full max HP (half fired on most kills). Line grammar: "one swing" / "6 swings", capitalized, no "Jr..".

**Found, not changed:**
- **Level 1 deaths in the straight-in first dungeon** (a Demonic or Rare enemy hit a 100-HP hero for 53 then 28).
  The pre-tonight build does it too (1 of 6 runs vs 2 of 6 tonight, small samples); D0.4 ("level 1 fights about 2
  swings") wants a look.
- The loot reveal doesn't show an item's class affinity or an off-slot line as a "rare circumstance" yet.
- Dungeon-select matchup lines live on the canvas hub panel only; MCP's dungeon-select screen has no body, so I
  checked them through tests, not play.
