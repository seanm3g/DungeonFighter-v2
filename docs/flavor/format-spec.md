# Demon Fighter — Flavor Format Spec

Living reference for flavor-text format work. Lore-free on purpose: it describes the slots and pipes, not the world. If this doc disagrees with the code, the code wins; note the difference at the bottom.

---

## 1. How Joey describes a format

Joey types a mockup of what the screen should show. Square-bracket words are blanks filled from data:

```
[ROOM_DESCRIPTION]

[TRIGGER_EVENT]. You [EFFECT_ON_PLAYER].

A [SIZE_TAG] [ENEMY_NAME] with [WEAPON_NAME] appears.
```

Cursor's job: figure out where each blank comes from (section 3), make the screen match the mockup, and handle missing data per section 4. If a blank has no source yet, say so and use a placeholder. Do not create a new data field without a Max sign-off (see rules file).

Spacing, line breaks, punctuation, and capitalization in the mockup are part of the format. Match them.

---

## 2. What the game shows today (baseline)

The live game is compact:
1. A one-to-two sentence room description
2. A short trigger beat (where present)
3. A templated enemy line: `A [enemy] with [weapon] appears.`
4. A one-line loot result, e.g. `After a careful examination, you find nothing useful.`
5. Combat log lines, with poetic `combatNarratives` mixed in

New formats should add energy to this, not bury it. Long text is fine when Joey asks for it, but the default target is short.

---

## 3. Slot vocabulary

Status key: **LIVE** = player can see it now. **SANDBOX** = working on `joey_sandbox_v1` only. **DORMANT** = authored, not wired. **PROPOSED** = does not exist yet (needs Max sign-off before it becomes a real field).

### 3a. Encounter blanks (from the Madlib Spectrum)

| Blank | Source | Status |
|---|---|---|
| `[ROOM_DESCRIPTION]` | `ENVIRONMENTS.description` → `Rooms.json` `description`. Some rooms already have 2 variants (the pattern to extend). | LIVE |
| `[TRIGGER_EVENT]` | No field yet. Proposed as a 2–4 line pool per location. | PROPOSED |
| `[EFFECT_ON_PLAYER]` | `ACTIONS` status-effect columns (Stun, Poison, Burn, Bleed, Expose, Weaken, Slow, Vulnerability, Harden, Silence, Confuse, Pierce) or `CONSUMABLE` stat effects. Hazard text: `EnvironmentalActions.json` `name`/`description`, shown via `{effect}`. | LIVE (hazards) |
| `[SIZE_TAG]` | `ENEMIES.tags` (tiny, giant, frail, young...). May be sparse — format must read fine without it. | LIVE data, sparse |
| `[ENEMY_NAME]` | `ENEMIES.name` | LIVE |
| `[WEAPON_NAME]` | Enemy's item: `WEAPONS.name`. Player's own weapon: hero gear. | LIVE |
| `[ARCHETYPE_TELL]` | No field yet. Proposed: one short behavior phrase per `ENEMIES.archetype` (Trickster, Assassin, Brute, Warlord, Sage, Berserker, Knight, Acrobat, Duelist). | PROPOSED |
| `[LOOT_LINE]` | Item naming: Prefix + item + Suffix(es). Rarity shows as name length (see `rarity` sheet). | LIVE |

Tiers (from the Madlib Spectrum — useful as a ladder for trials):
- **Tier 1:** room description, trigger + effect, enemy appears
- **Tier 2:** adds archetype tell to the enemy line (`...appears, [ARCHETYPE_TELL].`)
- **Tier 3:** adds the loot line

### 3b. Flavor banks (spreadsheet `flavor` tab → `FlavorText.json`)

The `flavor` tab has columns `section`, `bank`, `key`, `text`. One row = one line.

| Bank | Keyed by | Status |
|---|---|---|
| `environments.locationDescriptions` | dungeon theme (biome), with `Generic` fallback | SANDBOX (appended on room entry) |
| `environments.roomContexts` | `Biome/roomType` (armory, boss, chamber, kitchen, library, sanctum, shrine, treasure, plus Generic-only types such as guard, trap, dining) | SANDBOX. Wired: `RoomInfoBuilder` appends the biome/roomType bank on room entry when it exists, else falls back |
| `combatNarratives.*` | event banks: firstBlood, healthRecovery, below50Percent, below10Percent, criticalHit, criticalMiss, environmentalAction, healthLeadChange, intenseBattle, enemyDefeated, playerDefeated, playerTaunt, enemyTaunt, plus `_biome` taunt pairs and `_{creatureTier}` banks | Wired. `GetTriggeredNarrativesIfSignificant` returns the lines the analyzer built. An older note that `BattleNarrative.cs` blocks most lines is stale — see section 7 |
| `names.*`, `classQualifiers.*` | hero names, level-up titles | LIVE |
| `TravelEvents.json` `title`/`narrative` | road events between dungeons | LIVE |
| Item affix descriptions (`ModificationTables.json` / `StatBonuses.json`) | per affix | LIVE channel, mostly unwritten |

Biome taunt selection works by substring match on the room's display name ("grove" → `_forest`, "tomb" → `_crypt`, etc.), not by tags.

### 3c. Tokens inside lines

| Token | Means |
|---|---|
| `{name}` | Depends on the bank. Not always the actor. See the fill table. |
| `{player}` | the player character |
| `{enemy}` | the opposing enemy |
| `{effect}` | the environmental effect text passed in with the event |

Who `{name}` is, from the code that fills the bank (`BattleEventAnalyzer`, `NarrativeTriggerEvaluator`, `TauntSystem`):

| Bank | Filled tokens | `{name}` is |
|---|---|---|
| `firstBlood` | none | — |
| `firstBlood_{tier}` | `{name}` | the enemy, even when the player landed the hit |
| `criticalHit`, `criticalMiss` | `{name}` | the actor. The player always uses the unsuffixed bank |
| `criticalHit_{tier}`, `criticalMiss_{tier}` | `{name}` | the enemy, and only when the enemy is the actor |
| `healthRecovery` | `{name}` | the one who was healed |
| `below50Percent`, `below10Percent` | `{name}` | whoever crossed that line on the player path |
| `below50Percent_{tier}`, `below10Percent_{tier}` | `{name}` | the enemy |
| `healthLeadChange` | `{name}` | whoever just took the health lead |
| `intenseBattle` | `{player}`, `{enemy}` | — |
| `enemyDefeated`, `enemyDefeated_{tier}` | `{name}`, `{player}` | the enemy |
| `playerDefeated` | `{enemy}` | — |
| `playerTaunt`, `playerTaunt_{place}` | `{name}`, `{enemy}` | the player |
| `enemyTaunt`, `enemyTaunt_{place}`, `enemyTaunt_{tier}` | `{name}`, `{player}` | the enemy |
| `environmentalAction` | `{effect}` | — |

Creature-tier suffix (`nativeFauna`, `feralStock`, `technoEcho`) wins over the unsuffixed bank when the enemy's tier canonicalizes and that suffixed bank is non-empty. Tier banks that exist in code: `firstBlood`, `criticalHit`, `criticalMiss`, `below50Percent`, `below10Percent`, `enemyDefeated`, `enemyTaunt`. A non-empty `enemyTaunt_{tier}` also wins over biome taunts.

New tokens get added to this table before use.

---

## 4. Scale rules (every format, every slot)

1. **Any size pool.** Works with 1 line or thousands. No hardcoded counts.
2. **Fallback chain.** Most specific → more general → `Generic` → drop the line cleanly. Example: `Forest/kitchen` → `Forest` → `Generic` → nothing.
3. **Never show a broken blank.** No raw `[SLOT]`, `{token}`, "null", double spaces, or orphan punctuation when something is missing. Write the format so the sentence still reads if an optional blank is empty (e.g. `A [SIZE_TAG] Wight` → `A Wight`; fix "a/an").
4. **No back-to-back repeats** when the pool has more than one option. The live picker does not enforce this (section 7). Vary the opening of neighboring lines so an accidental repeat is less obvious.
5. **Content changes never need code changes.** New lines go in data only.
6. **Weighting is optional and PROPOSED.** Flat random pick unless Max approves weighting.

---

## 5. Open questions (do not decide — these belong to Max/Sean)

From the Madlib Spectrum and pipeline notes:
1. Should `TRIGGER_EVENT` become a real pooled field on ENVIRONMENTS?
2. Should `EFFECT_ON_PLAYER` be limited to effects that make sense as room hazards?
3. Does `SIZE_TAG` populate for every enemy?
4. Does the player's trigger line need to branch by weapon type (ranged vs. melee)?
5. Should description/trigger pools be weighted by rarity?
6. Long-term: append, replace, or fallback between `Rooms.json` descriptions and the `flavor` banks?
7. Should the `flavor` tab join the in-game Spreadsheet Import tool?

---

## 6. Pipeline (how text gets from sheet to screen)

1. Text written into the workbook's `flavor` tab (or via `Scripts/apply-flavor-content-package.py`)
2. `python Scripts/sync-flavor-text-from-xlsx.py --xlsx <path>` (dry run) → check summary
3. Same command with `--write` (backs up `FlavorText.json` first)
4. Rebuild, launch, look at a room or two

Other tabs (Environments, Enemies, etc.) sync through the in-game Spreadsheet Import tool.

---

## 7. Spec vs. code differences found

The code wins. Notes from the voice pass (2026-10-14):

1. **`{name}` is not always the actor.** Section 3c used to say that. First blood's tier banks fill `{name}` with the enemy (the one who bleeds), even if the player struck first. Health recovery fills the healed body. Health-lead fills whoever just took the lead. See the fill table.
2. **The old "BattleNarrative blocks most lines" note is stale.** `GetTriggeredNarrativesIfSignificant` returns the non-empty strings from the last event. A comment in that method says an earlier re-check of one-shot flags used to drop lines after they were generated. That re-check is gone.
3. **The picker does not remember the last line.** `NarrativeTextProvider` uses `Random.Next`. Scale rule 4 is a writing rule, not a code guarantee. Do not change the picker (Max).
4. **Critical-miss keyword sniff.** `TextDisplayIntegration` treats a narrative as a critical-miss line if its text contains `wild swing`, `misses completely`, `goes wide`, `fails spectacularly`, or `critical miss`. If the action line does not contain `CRITICAL MISS`, that narrative is dropped. If the action line does, the first matching narrative is folded into the action block and later matches are dropped. New critical-miss lines do not use those phrases, so they travel as ordinary narrative lines. The locked `criticalMiss_nativeFauna` line that contains `goes wide` still hits the sniff.
5. **Environmental flavor is stored and not shown on the room's turn.** `AddEnvironmentalAction` can generate the `environmentalAction` bank. `ProcessEnvironmentTurnAsync` then displays the room action with null narrative slots. Max decision.
6. **Taunts match the room's display name, not the dungeon theme.** `TauntSystem.GetLocationType` substrings the environment name (`grove` → forest, `tomb` → crypt, `fire` → lava, and so on). A non-empty `enemyTaunt_{tier}` wins over that biome bank.
7. **No one-line cap was found.** One event can return several voice lines, and the combat display passes the list through. Whether to cap at one voice line per turn is still open (Max).
8. **Combo voice is not in this file.** A 3-step combo writes two hardcoded sentences in `BattleEventAnalyzer`. Fallback sentences in `NarrativeTextProvider.GetFallbackNarrative` only fire when a bank is missing. Both are C#. Left alone.
9. **`forms.roomEnter` template** is `You step inside. <intro>. <detail>..` The extra period is in the template. Category lines are written without a terminal period so the template's period can land. The template was not rewritten.
10. **Character budget.** `DisplayBuffer` / `BufferStorage` truncate at 152 columns (ellipsis at 149). At the 210-column base grid, the center panel is 145 columns and `CenterPanelTextColumnWidth` is 141. `RoomInfoBuilderTests` require room-flavor lines `<= 152`. Longest live `{effect}` description in `EnvironmentalActions.json` is 77 characters. This pass keeps raw lines under 100 and estimated fills (18-character names, 80-character effects) under 152.

---

## 8. Voice (one narrator, every tier)

One chronicler at the edge of the fight. Commentates this exchange in real time. Omniscient about the shape of the blow that just landed. Occasionally leans closer. Does not cast, advise, or step in as a third fighter. Does not know why the player came or how the fight ends. Same personality in every tier. A tier may keep a physical fact already in that tier's lines (animal body; drilled, waiting-for-a-hand; a cut that does not behave like living flesh, a rhythm that does not change). It does not get a new personality, and it does not get a name for what it is.

Taunts are speech from someone in the fight. Same standard. Not the narrator's mouth. Narrator lines do not wear quotation marks.

The mechanical line still carries who, hit or miss, the number, and the speed. The flavor line does not restate the number.

### 8a. Citations and the rule each one became

Digestibility:

- Nelson Cowan, 2001, "The magical number 4 in short-term memory," *Behavioral and Brain Sciences* 24(1), 87–114. The focus of attention holds about four chunks. **Rule:** a line carries at most four chunks: who, the blow, the consequence, the turn.
- John Sweller, 1988, "Cognitive load during problem solving: Effects on learning," *Cognitive Science* 12(2), 257–285. Extra processing steals capacity from the task. John Sweller, Jeroen J. G. van Merriënboer, and Fred Paas, 1998, "Cognitive architecture and instructional design," *Educational Psychology Review* 10, 251–296, name that waste extraneous load. **Rule:** do not restate the mechanical line. Restating who, hit or miss, and the number is extraneous load.
- Richard E. Mayer, Julie Heiser, and Steve Lonn, 2001, "Cognitive constraints on multimedia learning: When presenting more material results in less understanding," *Journal of Educational Psychology* 93, 187–198. The coherence principle (also Mayer, *Multimedia Learning*, 2001): interesting material that is not the task hurts. **Rule:** no aside that is not this exchange. No second scene, no history lesson.
- Keith Rayner, 1998, "Eye movements in reading and information processing: 20 years of research," *Psychological Bulletin* 124(3), 372–422. Less frequent words take longer fixations; long lines send the eye backward. **Rule:** plain high-frequency words, and the line should fit one combat-column row (about 141 at the base layout, hard truncate at 152).
- Walter Kintsch, 1988, "The role of knowledge in discourse comprehension: A construction-integration model," *Psychological Review* 95(2), 163–182. Comprehension builds propositions, then integrates them. **Rule:** one proposition, then one turn. Two sentences is the ceiling, because the slot is one glance.

Wanting the next line:

- Melanie C. Green and Timothy C. Brock, 2000, "The role of transportation in the persuasiveness of public narratives," *Journal of Personality and Social Psychology* 79(5), 701–721. Transportation is absorption through imagery, feeling, and focus. **Rule:** one concrete image (blood, breath, stone, the stance) so attention stays inside the exchange.
- George Loewenstein, 1994, "The psychology of curiosity: A review and reinterpretation," *Psychological Bulletin* 116(1), 75–98. Curiosity is a felt gap between what is known and one specific missing piece. **Rule:** end on that gap — what the next blow will cost, or what just changed and has not paid off.
- William F. Brewer and Edward H. Lichtenstein, 1982, "Stories are to entertain: A structural-affect theory of stories," Center for the Study of Reading Technical Report No. 265. Suspense puts the outcome still ahead; curiosity signals that something important is missing; surprise reveals a fact that re-reads what just happened. **Rule:** each line is one of those three, in miniature, and then it stops.

Craft (books and a lecture, not papers):

- Stephen King, *On Writing* (2000). Simple words carry the reader into the scene; an -ly adverb that only explains the verb is fear. **Rule:** the verb does the work.
- George R. R. Martin, *A Game of Thrones* (1996). No standalone craft essay was verified for the phrase Joey uses. **Rule, as Joey framed it:** the blow leaves a consequence you can see.
- J. R. R. Tolkien, *The Lord of the Rings* (1954–1955). **Rule, as Joey framed the cadence:** the sentence turns once, at the end.
- Frank Herbert, *Dune* (1965). No standalone craft essay was verified for "pressure in a short line." **Rule, as Joey framed it:** the pressure lives in the short line, not in a second paragraph.
- Brandon Sanderson, 2025 Writing Lecture #2, "Promise, Progress, and Payoff" (brandonsanderson.com; the lecture of 2025). **Rule:** inside the one or two sentences, promise a next beat, show the exchange move, and pay it with a concrete change.

### 8b. Writing rules that follow

- One or two sentences. The personality is diction, rhythm, and the turn at the end.
- Neighbors in a pool do not share an opening.
- No new proper nouns. No timeline. No biography. The player is a blank slate.
- Banned words stay banned (see the flavor ground rules). Do not name a tier's "meaning."
- Do not write `wild swing`, `misses completely`, `goes wide`, `fails spectacularly`, or `critical miss` into a line you want the player to see on an ordinary turn.

