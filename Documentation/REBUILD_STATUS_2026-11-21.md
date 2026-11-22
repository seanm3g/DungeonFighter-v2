# Rebuild status — evening run, Phase 11 and steps 3+ (2026-11-21)

Branch `phase11-item-rolls` (from `v47-reimagine` at `4869c669`), main checkout. Decisions:
[`CORE_DESIGN_PASS_2026-11-17.md`](CORE_DESIGN_PASS_2026-11-17.md). Same rules as the 11-18 run
([`REBUILD_AGENT_BRIEF_2026-11-18.md`](REBUILD_AGENT_BRIEF_2026-11-18.md)): implement the decisions, don't
redesign; one commit per sub-step; switch off before deleting; old saves load; no balance work (no sims, no patch
edits); tests after every sub-step. Builds and test runs go to a scratch folder so a running `DF.exe` isn't locked.
Nothing pushed, no PR.

Already done before tonight (see [`REBUILD_STATUS_2026-11-18.md`](REBUILD_STATUS_2026-11-18.md) and git log):
steps 0, 1, 2; step 6's engine merge and fixed resolution order; step 8's verb cut (CASH IN / DETONATE).

## Summary

| Sub-step | Status | Commit | Tests |
|---|---|---|---|
| Phase 11 item rolls | done | `6cd48a60` | 15063 passed / 0 failed |
| 3.1 Action roles, class hits | done | `9c9d9d9d` | 15074 passed / 0 failed |
| 3.2 + 3.3 Fernis, swing moments, fight results | done | (this commit) | 15086 passed / 2 failed (both flaky, see below) |

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
