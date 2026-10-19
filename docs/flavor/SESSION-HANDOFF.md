# Demon Fighter — Flavor Text Session Handoff

**Last updated:** 2026-10-19 (fourth session) · branch `joey_sandbox_v2`

**New session? Read this file first, then `.cursor/rules/demon-fighter-flavor.mdc`.**
That covers everything. Don't re-derive it.

**START HERE, in this order:**
1. **THE NARRATOR SYSTEM** (below) — the architecture, the two display modes, and how to verify.
2. **Decisions locked** — especially 10-14, which supersede parts of 1-9.
3. **Open items.**
4. **The target voice** (bottom) — both voices, worked.

The sections dated 2026-10-15 are kept for history. Where they conflict with the narrator
section or decisions 10-14, **the later text wins**.

---

## Who / what

Joey is narrative lead. Max owns architecture. Sean is creative/product lead.
Goal of this branch: a playable demo showing the game with Joey's narrative and lore in it,
for Sean. Not a merge candidate.

Joey's working preference: **ask plain, direct questions.** Avoid hedged or compound phrasing.

---

## Branch state

`joey_sandbox_v2` was branched fresh off `main` on 2026-10-15.
`joey_sandbox_v1` was abandoned — it had drifted 2,902 files from `main`.

**Things that exist only on `joey_sandbox_v1` and did NOT come forward:**
- The 555-line combat-voice rewrite of `GameData/FlavorText.json` (F-01)
- `docs/flavor/format-spec.md` and `format-log.md`
- `Scripts/sync-flavor-text-from-xlsx.py` and `Scripts/apply-flavor-content-package.py`
- The ~10-line `RoomInfoBuilder.cs` patch that wires `locationDescriptions` into room entry

**UPDATE 2026-10-19:** the F-01 bank rewrite HAS now been brought forward, read straight off
`joey_sandbox_v1` with `git show joey_sandbox_v1:GameData/FlavorText.json`. The branch is still
live and readable — do not rewrite v1 content from scratch, port it.

---

## Decisions locked this session

1. **Version B, escalation-driven.** Combat prose tracks a story across the fight (enemy
   learns a weakness, player adapts, player wins by changing approach) rather than just
   narrating blows.
2. **Separated format** — prose on its own line beneath the mechanical readout, not woven
   into it. Rationale: lets a player skip to the numbers when they stop caring about prose,
   which avoids building resentment toward the text. A woven variant is wanted in a LATER
   session; not now.
3. **Budget flavor by turn duration, not by percentage.** Actions carry real durations
   (CUT 4.1s, STAB 5.4s). At ~200 wpm that is ~14 words on a 4s action, ~18 on 5.4s.
   Longer than that and the text stalls the fight.
4. **Fire on variable intervals, never a fixed cadence.** Variable-ratio reinforcement
   sustains attention; a fixed "every Nth turn" trains players to skim. Roughly 40-60% of
   beats should carry no flavor at all.
5. **The proper-noun ban is retired.** Houses, places, eras, named events, dated history are
   all usable now. Canon lives in `docs/lore/`.
6. **The banned-word list is PERMANENT and is not a guardrail — it is the setting.**
   See rule 6 in the .mdc. Never name the machines.
7. **Narrator-as-character is approved** (extra-diegetic, WWE-commentator register, can break
   the fourth wall). Not yet built — that is task 3.
8. **Fix the magic-string crit filter as part of writing the crit bank** (Joey approved
   2026-10-15). Content and gate get designed together, not in separate passes. See open
   item 1 below.
9. **Draft 2-3 banks first, get Joey's reaction, then write the rest** (Joey approved
   2026-10-15). Do NOT write all 13 banks up front — the voice gets approved on a small
   sample before several hundred lines are committed to it.

### Locked 2026-10-19

10. **The narrator is a mystical overseer, not a sports commentator.** Decision 7's
    "WWE-commentator register" is SUPERSEDED. The reference is the Watcher (Uatu): omniscient,
    present, detached, bound by a refusal to intervene. Joey's phrasing: "the voice, tone and
    cadence of a wizard, an elf and a dwarf all rolled into one." Commentator energy survives
    only as a rare dry glimmer, about one bank in ten.
11. **Two voices, split by tense and person.**
    - Beat tier = the world. Third person, PRESENT tense, no narrator present.
    - Set piece = the overseer. First person, PAST tense. He recounts; he does not commentate.
    The split is what keeps them distinguishable without labels. Past tense is the single
    highest-leverage rule for keeping him out of the commentator's chair (stolen from Bastion,
    where Rucks narrates present action in past tense all game and never grates).
12. **Tier by significance, never by dice.** Randomness picks WHICH line a tier draws, never
    WHETHER a moment speaks. A fight where the crit is muted and a stance change speaks is
    worse than no pacing at all. The narrator is claimed before the pacing gate runs.
13. **Emphasis is colour, not position.** Joey asked whether the narrator should sit left
    (against right-aligned mechanics) or centred. Answer: NEITHER — this log already spends the
    horizontal axis on *who is acting* (player left, enemy right, scene/system centred). All
    three positions are taken. Treisman's feature-integration work: a target with a unique
    COLOUR is found pre-attentively, while a target defined by position among other
    position-varying items needs serial search. Plus von Restorff (isolation) for rarity, and
    Bringhurst against centring anything over one line. The overseer gets `ColorPalette.Purple`,
    flat across the line, alone in its paragraph. Disco Elysium is the shipped precedent:
    many voices, one column, separated by colour.
14. **Prose quality rule, from Joey's critique of "it is always smaller than people expect":**
    never write an ABSTRACT COMPARATIVE — a vague noun measured against a vague expectation.
    It has the shape of wisdom with nothing to point at, and it is the AI tell. Build on
    antithesis and balance instead ("win from worse and lose from better"), concrete nouns,
    almost no adjectives.

---

## Lore

`docs/lore/` holds 22 files imported 2026-10-15 from Joey's claude.ai project knowledge.
**`DemonFighter_File_Precedence_2026-09-16_FINAL.md` is the tiebreaker — read it first.**

Key canon:
- Antem **extinct**. Kin **cut entirely**. Vessels **extinct**. Empty folded into the Bestiary.
- Three creature tiers, matching the code's bank suffixes exactly:
  `nativeFauna` (evolved here), `feralStock` (Earth animals gone wild, faint memory of being
  cared for), `technoEcho` / Genesis-adjacent (constructs, undead, rare on purpose).
- Houses: Dray, Colehurst, Thorne, Marlow. Breach-era paramounts are locked:
  Torvin Dray, Harroc Colehurst, Maren Thorne, Denna Marlow.
- Timeline spans 63,800 years, Genesis Era → Present Day (yr 275-300). Year Zero is the
  Breach of Antarkeia.
- The Genesis Era and Great Forgetting are marked **"Creator only"**; Deep Dream and Age of
  Clans **"unknown in-world."** That is why the banned-word list exists.

The lore source of truth still lives in Joey's claude.ai project ("Demon Fighter",
25 context files). `docs/lore/` is a snapshot, not the master.

---

## Code work completed this session

**Fixed: the combat narrative display-gate bug.** `Code/Combat/BattleNarrative.cs`.

`BattleEventAnalyzer.AnalyzeEvent` consumes one-shot state flags (first blood, defeats,
environmental, health thresholds) at the moment it generates a line. The display path then
re-derived significance from those same already-consumed flags, always got false, and
silently discarded the line. Net effect: **zero combat narratives reached the screen, ever.**

Fix: display no longer re-derives significance. If generation produced a line, it is
significant by construction.

Verified by controlled A/B — regression test `TestFirstBloodNarrativeReachesDisplay` fails
without the fix, passes with it. Full suite compared before/after: 17 pre-existing failures,
identical both ways, no regressions.

Run it: `cd Code && dotnet run -- --run-test-filter BattleNarrative`

**Fixed: the magic-string crit filter (open item 1, now closed).** `Code/Combat/BattleNarrative.cs`.

`FilterSignificantNarratives` identified critical-hit lines by searching the prose for the
literal strings `"devastating blow"` / `"strikes true"`, then re-ran `IsSignificantEvent` on
the ones it caught. Two problems, and the handoff's original description of the risk was
backwards:

- Only 2 of the 8 shipped `criticalHit` lines contained either phrase. The other 6 skipped the
  check entirely. The gate was already reading content, not event type.
- For the 2 it did catch, the check could only ever fail. `AnalyzeEvent` calls
  `SetRecentCriticalHitNarrative()` (cooldown = 3) at the moment it generates the line, and
  `IsSignificantEvent`'s crit branch requires `!HasRecentCriticalHitNarrative`. So those lines
  were silently discarded — the same display-gate bug as above, left behind in the crit path.

Removing the sniff does **not** make crit flavor fire on every crit. The real gate —
`(NarrativeBalance >= 0.7 || Roll >= 18)` plus the 3-crit cooldown — lives in `AnalyzeEvent`
and runs before a line is produced. No bank tagging was needed; significance is already
decided once, at generation. `FilterSignificantNarratives` now only drops empty entries.

Verified by controlled A/B with new regression test `TestCriticalHitNarrativeReachesDisplay`,
run 25 times each way:
- old code + old bank: **18 pass / 7 fail** (intermittent — depends which line the random
  picker draws, matching the expected 2-in-8 rate)
- new code + new bank: **25 pass / 0 fail**

Filtered suites `Narrative`, `FlavorText` and `Combat` are fully green after the change.

---

## The two prose paths — read this before writing any more banks

**There are two separate combat text systems, and they are not the same pipeline.**

**1. The `sequence*` banks — this is what is on screen.** 15 banks, 73 lines. The F7 prose
log builds one paragraph per swing by walking `CombatSequenceFlavorTokens.NarrativeBeatOrder`
and emitting one sentence per step: Attacker → Outcome → Roll → Action → Critical → Defense →
Damage → Effect → Stance → Tempo. An ordinary hit produces **eight sentences**. Every swing.
Assembled in `CombatSequenceFlavorPresenter.PlanNarrativeBeats`.

**2. The `combatNarratives` one-shot banks** (`firstBlood`, `criticalHit`, `enemyDefeated`,
and the other 10). **The live combat loop generates these and throws them away.**
`CombatTurnHandlerSimplified.cs:96` and `:265` call `GetTriggeredNarrativesIfSignificant()`
purely to drain the queue, and discard the return value. The comment there says so:
*"Overview combat log shows action blocks only (no flavor); F7 is the prose path."*
The only surviving consumer is the post-battle "Battle Highlights" block
(`CombatMessageHandler.cs:44`, `CombatRenderer.cs:217`), which calls `GetTriggeredNarratives()`
— that returns the **last event's** lines only, so in practice just the `enemyDefeated` line.
`firstBlood` and `criticalHit` render nowhere.

Corollary: the display-gate fix logged above, and the crit-filter fix, both make the function
**return** the right lines. No live caller renders them mid-fight. The regression tests assert
against the function, not against the screen.

Also: `BattleNarrative.narrativeEvents` is written on every event and **never read by anything.**

Also: decision 2's format (prose beneath the mechanical readout) does not exist. F7 **swaps**
between prose and mechanical rather than stacking them — `CombatLogDualView.cs:8`. The Ghoul
mockup needs a wiring change, not just content.

---

## PROTOTYPE: the narrative pacing gate (Joey approved 2026-10-15)

**New files:** `GameData/NarrativePacing.json`, `Code/Combat/Sequence/NarrativePacing.cs`,
`Code/Tests/Unit/Combat/NarrativePacingTests.cs`.
**Touched:** `CombatSequenceFlavorPresenter.PlanNarrativeBeats` (three small gates).

**Why it had to be code.** `CombatSequenceFlavorTokens.ShouldInclude` was the only gate, and it
answers *"does this step have a value to report"*, not *"should we say it out loud."* The
include flags are derived straight from whether the mechanical value is non-empty
(`includeTempo = !string.IsNullOrWhiteSpace(tempoName)` etc.). So every step with a value spoke,
every swing. **Decision 3 (≈14 words per 4s action) is unmeetable when eight sentences share
that window, and decision 4 (40-60% of beats silent) is not expressible in the banks at all** —
nothing in the JSON can make a step stay quiet. That is why this is code and not content.

**Three knobs, all data-side** (ground rule 2 — pacing now lives in JSON, not C#):
- `swingChance` — chance this swing gets any prose at all. Gives decision 4's silent beats.
- `maxSentencesPerSwing` — cap on a narrated swing. Keeps it inside decision 3's budget.
- `stepChance` per step kind — which steps are eligible to speak.

**Default is passthrough.** Shipped as `"enabled": false`, profile `full`. Missing file,
unreadable file, unknown profile, unknown step name — all fall back to pre-prototype behaviour.
A step absent from `stepChance` keeps speaking, so a new step kind never goes silent by
omission.

**To try the sparse pacing:** set `"enabled": true` in `GameData/NarrativePacing.json`
(`activeProfile` is already `"sparse"`). Tune the numbers there — no rebuild needed for the
values, they are read on load.

A fully suppressed swing renders **nothing**, not a blank line: `PlayPendingAsync` already had
a clean `beats.Count == 0` bail, and `TryBuildSilentParagraphFromPending` already rejects an
indent-only paragraph. The architecture supported silent beats all along; nothing ever produced
one.

**Verified:**
- 9 new pacing tests pass (`--run-test-filter NarrativePacing`).
- Shipped config asserts as passthrough, so the default is provably unchanged.
- Flipping the file to `enabled: true` / `sparse` makes that assertion fail — the config is
  genuinely live, not inert.
- `Combat` filter: **17 failures before my changes, the same 17 after** — byte-identical
  failure lists, diffed against a clean stash. No regressions. (Those 17 are open item 2.)

**This is a prototype and Max has not seen it.** It changes live combat display density, which
is adjacent to rule 5. Two things worth his eye: whether pacing belongs in its own file or in
the workbook pipeline, and whether the sentence cap should prefer *which* steps survive rather
than simply keeping the earliest.

---

## Three bugs found by running it (2026-10-15, Joey's screenshot)

**1. Duplicated, ever-growing paragraphs. FIXED.**
Paragraph continuation works by re-seeding the previous paragraph and calling
`CanvasUICoordinator.ReplaceLastColoredSegments` — which only lands on the right line while
that paragraph is still **the last thing in the display buffer**. A swing that writes a
mechanical block but no prose breaks that invariant, so the next same-attacker swing reprinted
the whole previous paragraph plus its new sentences as a fresh entry. Latent all along; the
pacing prototype made silent swings common and exposed it. Two fixes:
- `ClearOpenParagraph()` on a suppressed swing.
- With pacing on, `TrySeedOpenParagraph` never continues — each narrated swing gets its own
  short paragraph. This also stops unbounded growth: `maxSentencesPerSwing` only caps sentences
  *added this swing*, so a continued paragraph grew past the cap forever.

**2. "Sluggish." FIXED, and this was the real one.**
`GameConstants.NarrativeParagraphTargetMs = 8000`, and
`CharacterRevealRhythmCalculator.BuildParagraphSchedule` divides that budget by the paragraph's
**sentence count**:

```csharp
double sentenceBudget = (double)budget / sentences.Count;
```

So a paragraph takes 8 seconds whether it holds eight sentences or two. **Cutting the prose
down does not make the log faster — it types the surviving words more slowly.** That is why
sparse pacing changed the word count and not the feel. Added `paragraphTargetMs` to the pacing
profile, wired at the two `BuildNarrativeParagraphSchedule` call sites in the presenter.
`0` leaves the shipping 8000 alone; `sparse` now sets `1200`.

**3. Back-to-back identical lines. FIXED.**
`NarrativeTextProvider.GetRandomNarrative` drew uniformly at random with no memory, so a
5-entry bank repeated itself ~20% of the time. Ground rule 2 required a no-immediate-repeat
guard and it had never been implemented. Added one: per-bank memory of the last line, drawing
uniformly from the remaining entries (not a retry loop, which can still collide). Single-entry
banks still repeat, which the rule allows. Test covers every multi-entry bank, 100 draws each.

**Note for whoever turns `sparse` on by default:** five tests in
`Code/Tests/Unit/Combat/CombatSequenceFlavorPresenterTests.cs` encode the old behaviour
(paragraph growth across swings, every step firing). They pass with pacing off and fail with it
on. They need rewriting before sparse can ship as the default.

**Still open: "not very dynamic."** The `sequence*` system is structurally a per-step
mechanical paraphrase — `"The blow leaves {damage}."` It describes the swing, one clause per
HUD column. It cannot become a narrator by rewording, because it has no state across swings and
no license to editorialize. That is task 3 and it is still not started. See below.

---

## Flavor work completed this session

**Three banks rewritten in the agreed voice** (decision 9 — sample first, not all 13).
`GameData/FlavorText.json` → `combatNarratives`. 8 lines each, all 9–15 words, inside the
turn-duration budget from decision 3. No exclamation marks, no banned words, no crutch words.

- `firstBlood` — **must stay token-free.** `AnalyzeEvent` calls `GetRandomNarrative("firstBlood")`
  with no `ReplacePlaceholders` call, so any `{token}` in this bank prints raw on screen.
  Also fires on the first damaging hit by *either* side, so lines must be actor-neutral.
- `criticalHit` — `{name}` is `evt.Actor`, which can be the player **or** the enemy. Every line
  has to read correctly both ways. No gendered pronouns.
- `enemyDefeated` — `{name}` is the enemy, `{player}` is the player.

**Known ceiling on these three banks:** the Ghoul worked example gets its power from
enemy-specific canon (*a Ghoul goes for what a corpse carries*). These banks are generic —
one pool per event, shared across all enemies and all three creature tiers — so the lines hit
the register but not the specificity. Closing that gap needs tier- or enemy-suffixed banks
plus a suffix-aware lookup in `AnalyzeEvent` (the `playerTaunt_lava` / `_crypt` pattern is the
existing precedent, handled in `TauntSystem`). Not built — waiting on Joey's call.

---

## THE NARRATOR SYSTEM (built 2026-10-19) — read this before touching combat text

### How a line gets to the screen

```
BattleEventAnalyzer.AnalyzeEventTagged()    -> List<TriggeredNarrative>(Bank, Text)
NarrativeTiers.For(bank)                    -> Ledger | Beat | SetPiece
NarrativeTierPresenter.TryGetFlavorLine()   -> the loudest tier wins, one line per swing
   |
   +-- F7 OFF (overview): CombatTurnHandlerSimplified -> DisplayCombatActionAsync(narratives:)
   |        -> BlockMessageCollector renders it under the mechanical block
   |
   +-- F7 ON (narrative): CombatSequenceFlavorPresenter.SetPendingNarratorLine()
            -> PlanNarrativeBeats emits it; a SetPiece returns early and speaks alone
```

**Every line now carries its bank** (`TriggeredNarrative`). This replaced BOTH magic-string
sniffs — the crit filter and `IsCriticalMissFlavorText`, which is now deleted. Never classify a
flavor line by searching its prose again; the bank is on the line.

### THE TWO MODES — the single most important thing in this document

**F7 OFF (overview) is the mode Joey wants and the one to tune.** Mechanical block every swing,
one short line underneath when the moment earns it. Ledger plus highlight.

**F7 ON (narrative) is prose-only.** `DisplayActionBlockAsync` takes an early-return branch,
collects the mechanical block ONLY as hover-tooltip content, and hands all visible output to
`CombatSequenceFlavorPresenter`. **There is no ledger in that view by design.** Three sessions
were burned pointing Joey at F7 ON and wondering where the ledger went. Do not repeat this.

Both modes render the narrator, and both must agree on his colour — they are different code
paths and they silently diverged once already.

### Tiers

| Tier | Banks | Voice |
|---|---|---|
| SetPiece | criticalHit, enemyDefeated, playerDefeated, below10Percent | Overseer. 1st person, PAST. Purple. Alone. |
| Beat | firstBlood, below50Percent, healthLeadChange, intenseBattle, criticalMiss, environmentalAction | The world. 3rd person, PRESENT. Dim. |
| Ledger | healthRecovery, taunts | Silent. |

Unknown banks fall to **Beat**, not Ledger — silence by omission is the harder failure to spot.
Biome-suffixed banks (`playerTaunt_lava`) inherit the family tier.

### Config: `GameData/NarrativePacing.json`

`enabled: false` ships as the default and reproduces pre-prototype behaviour exactly.
Profile `sparse` holds the live tuning. Knobs:

- `swingChance` — chance of a per-step texture clause. **Does not gate the narrator.**
- `maxSentencesPerSwing` — cap for a narrated swing.
- `stepChance` — per `CombatSequenceStepKind`. Stance/Tempo/Roll are 0.0 (bookkeeping);
  Critical/CriticalMiss are 0.0 because the narrator owns those moments.
- `paragraphTargetMs` — **overrides `GameConstants.NarrativeParagraphTargetMs` (8000).**
  `BuildParagraphSchedule` divides that budget by SENTENCE COUNT, so without this a two-sentence
  paragraph takes the same eight seconds as an eight-sentence one. Shortening prose without this
  makes the log type slower, not faster. This was the real cause of "it feels sluggish".

### Content

Beat banks are v1's F-01 rewrite, ported as written (ground rule 1 — approved text goes in
untouched). Set-piece banks are new, in the overseer's voice.

**As of the fourth session, every bank that can speak is written in one of the two voices.**
The previous session's sample covered 8 of the 11 speaking banks; the remaining three were
still the old placeholder register and all three fire during an ordinary fight, so judging
the voice with them in place would have been judging a mix.

| Bank | Tier | State |
|---|---|---|
| `criticalHit`, `enemyDefeated`, `below10Percent` | SetPiece | Overseer (third session) |
| `playerDefeated` | SetPiece | Overseer (**fourth session**) |
| `firstBlood`, `below50Percent`, `healthLeadChange`, `intenseBattle`, `criticalMiss` | Beat | v1 F-01, ported |
| `environmentalAction` | Beat | World voice (**fourth session**) |
| `goodCombo` | Beat | World voice (**fourth session**, and moved out of C#) |
| `healthRecovery`, `playerTaunt`, `enemyTaunt` | Ledger | Silent by design, not rewritten |

Two things found while writing those three:

**`goodCombo` was two interpolated C# string literals** in `BattleEventAnalyzer`, one per
side, with exclamation marks — breaking ground rule 3 and meaning the only way to retune the
line was a rebuild. It was also absent from `NarrativeTiers`, so it fell through the unknown-bank
default to **Beat** and spoke in the log. It is now a data bank with `{name}` = the actor
(either side, so no gendered pronouns), and is classified explicitly.

**`environmentalAction` must stay free of `{effect}`,** the way `firstBlood` must stay
token-free. `CombatTurnHandlerSimplified` builds `evt.EnvironmentEffect` by rendering the
whole mechanical block to plain text — action line, then roll info, then one line per status
effect, newline-joined — and `AnalyzeEvent` passes that in as `{effect}`. All five old lines
embedded it, so every one reprinted the entire readout inside the narrator's one-line slot.
`NarrativeTierTests.EnvironmentalBeatCarriesNoEffectToken` now guards this, for the bank and
for the C# fallback.

### Bugs fixed on the way (all verified by test, not by reading)

1. **Crit magic-string filter** — see earlier section. Deleted; the gate was already in
   `AnalyzeEvent`.
2. **Duplicated growing paragraphs** — continuation re-seeds the previous paragraph and calls
   `ReplaceLastColoredSegments`, which only lands correctly while that paragraph is the LAST
   line in the buffer. A silent swing broke that invariant. Fixed by clearing the open paragraph
   on a silent swing, and by never continuing paragraphs while pacing is on.
3. **Back-to-back identical lines** — `GetRandomNarrative` drew with no memory. Added a
   no-immediate-repeat draw (ground rule 2 had always required it).
4. **The 8-second paragraph budget** — see `paragraphTargetMs` above.
5. **Narrator discarded in F7 ON** — the line was attached to the mechanical block, which that
   mode swaps out. It was landing in a hover tooltip. Now routed to the prose presenter.
6. **Narrator colour disagreed between the two paths** — block path used `Info`, prose path
   `Purple`. Now both `Purple`.
7. **`goodCombo` spoke from a C# literal** — unclassified, so it fell to Beat and rendered in
   the log, in the old exclamation-mark register, from a string that could only be changed by
   rebuilding. Fixed fourth session: data bank plus an explicit tier entry.
8. **`environmentalAction` reprinted the whole mechanical block** inside the narrator's
   one-line slot, because `{effect}` is the rendered readout, not a short phrase. Fixed fourth
   session by dropping the token; guarded by a test.
9. **BLANK `{name}` IN EVERY EVALUATOR-OWNED LINE — pre-existing, exists on `main`, tell Max.**
   `NarrativeTriggerEvaluator` is constructed inside `BattleEventAnalyzer`'s CONSTRUCTOR, before
   `Initialize()` supplies the combatant names, and `Initialize()` never rebuilt it. So
   `healthLeadChange`, `below50Percent`, `below10Percent` and `intenseBattle` have ALWAYS
   rendered with an empty name — "Pressure changes hands. is the one walking forward." It was
   invisible only because those lines were generated and discarded. Fixed here by rebuilding the
   evaluator in `Initialize`. **This is a real bug on `main`, not something this branch caused.**

### Placement is a property of the swing, not of the wording (fixed 2026-10-19, fourth session)

`CombatCenterPanelEnemyLineAlignment` decides left/right **per row**, by asking: is this row
indented with `BlockMessageCollector.ActionBlockSubsequentIndent` (5 spaces), and if not, does
its text **start with the enemy's name**. Non-indented and starts with the enemy name → right.
Everything else → left.

The narrator's lines never carried that marker, so every one of them fell through to the name
sniff and **its position on screen was decided by its first word**:

- `"{name} put the whole fight through a gap."` → right, because the bank line opens with the token.
- `"Half of {name} is gone."` → left, onto the player's side, while describing the enemy.

Two lines, same enemy, same fight, opposite sides. Rewording a bank moved it across the screen.
That is layout as a property of prose — the same magic-string coupling `TriggeredNarrative`
exists to kill, and a breach of ground rule 2: a format that only lays out correctly for one
phrasing is not a format, and this one would have degraded silently as Joey wrote more lines.

**Root cause, and it is a trap worth knowing about generally.**
`ColoredTextBuilder.Build()` runs `SpacingHelper.ProcessSegments`, which **normalizes any
whitespace-only segment to a single space**. The indent was being added as its own segment, so
it was collapsed to one space every time and could never match the 5-space marker. Any code
that builds an indented line as `.Add(indent).Add(text)` has the same silent bug. The indent has
to ride **inside** the text segment: `.Add(indent + text, colour)`.

That also satisfies decision 13's "flat across the line" — one colour, no two-tone leading run.

Pinned by `NarratorFightLogTests.PlacementComesFromTheSwingNotTheWording`, which asserts two
differently-worded lines on the same swing land on the same side, and that the identical line
on a player swing goes left.

**Not changed: the overseer is still not alone in his paragraph.** Decision 13 asks for
isolation (von Restorff) as well as colour. He currently sits directly under the roll line. This
was left alone deliberately — it is presentation taste, not a data-driven requirement, the
colour already does the pre-attentive work decision 13's own Treisman argument rests on, and a
blank line before every narrator line changes the log's vertical rhythm. **Joey's call.**

---

### How to verify narrative work — READ THIS

Four times this session a change was declared working based on reasoning about the call chain,
and four times the output surface was different from what the reasoning assumed. The call chain
being right does not mean the text reaches the screen.

- **`--run-test-filter NarratorFightLog` is the one that would have caught all of this.** It
  reads the REAL committed config rather than an override, runs a ten-swing fight, drives
  `BlockMessageCollector.CollectActionBlockMessages` — the exact list `BlockDisplayManager`
  hands the renderer — and prints every group. A test that overrides the config cannot catch
  a config that is switched off, which is what kept the narrator invisible for four sessions
  while every other suite was green.
- `--run-test-filter` suites: `NarrativeTier`, `NarrativePacing`, `CombatSequenceFlavorPresenter`.
- **The `Combat` filter matches suite NAMES, not the category.** `NarrativePacing`,
  `NarrativeTier` and `NarratorFightLog` are NOT in it. Run them by name; a green `Combat`
  run says nothing about the narrator.
- `CombatSequenceFlavorPresenter` has `RecordForTests` / `LinesWrittenForTests` /
  `LastParagraphSegmentsForTests` — these drive the REAL prose path and let you assert on the
  text and colour actually painted. Use them. `TestNarratorLineReachesProse` is the model.
- **`PLAYTODEATH` cannot verify display.** `SimulationPacing` sets
  `CombatManager.DisableCombatUIOutput = true`, so the display layer returns before rendering.
  It is fine for verifying the combat loop, useless for verifying output.
- Baseline: `--run-test-filter Combat` gives **17 pre-existing failures**. Diff the failure list
  against a clean stash rather than trusting the count. Verified again fourth session —
  byte-identical lists before and after, `git stash push -u` / `pop` around the run.
- **A filtered suite can abort mid-run and still print a summary that looks like a result.**
  The presenter suite did exactly this under `sparse`: one assertion failed, the next threw
  `IndexOutOfRangeException`, and ~240 assertions silently never executed. Grep the output for
  `Exception`, not only for `✗`.
- 12 of the 17 baseline failures live in `CombatSequencePresenter` (the HUD suite, not the
  prose one). They are identical with pacing on and off — that suite is not pacing-gated, so
  it is not a useful signal for narrative work either way.

---

## Open items

**Next task: Joey's reaction to the overseer in the F7-OFF view.** The engine is built, every
speaking bank is now in voice, and the suite no longer blocks the flip. Nothing else should be
written until the voice and rhythm are judged in motion.

**It is already on.** `GameData/NarrativePacing.json` ships `"enabled": true`, profile `sparse`.
**Leave F7 OFF**, restart the game, and fight something that survives several swings.

**The narrator is verified to reach the screen.** Not "the function returns a line" — the
actual list handed to the renderer. `--run-test-filter NarratorFightLog` runs a ten-swing
fight and prints the log:

```
  swing 1    Michael Ironthorn Attacks Magma Beast... and hits for 13 damage
                 (roll: 10)
             First blood hits the stone, and the room learns the shape of the fight.

  swing 3    Michael Ironthorn Attacks Magma Beast... and hits for 18 damage
                 (roll: 20)
  [OVERSEER] The opening stood there the whole time. Michael Ironthorn was the
             first to spend it.

  swing 5    The blows from Michael Ironthorn arrive in order, and the order is the point.

  swing 8    Hurt like this, Magma Beast can be finished by the next clean hit.

  swing 9    [OVERSEER] Magma Beast is nearly out. I will not look away.
                        That is the whole of what I can give.

  swing 10   [OVERSEER] Magma Beast fell. Michael Ironthorn is standing, and that
                        is the part that gets remembered.
```

Six lines across ten swings, four of them silent, three purple set pieces. That is the
ledger-plus-highlight shape decision 4 asks for.

**The restart is not optional.** `NarrativePacing.Active` caches the resolved profile behind
`_loaded` on first read, and nothing in product code calls `Invalidate()` — only tests do. So
the profile is fixed for the lifetime of the process. Editing the file while the game is
running changes nothing, and the symptom is indistinguishable from the content being broken:
no narrator, and the per-step prose still eight sentences deep with stance and tempo in it.
If you see that, you are looking at `Profile.Full`, not at the overseer failing to fire.

The earlier claim in this document that the flip alone was "actually sufficient" was wrong on
this point. It is sufficient to make the config correct; it is not sufficient to make a running
game read it.

The game reads the **repo** `GameData/` — `GameConstants.GetPossibleGameDataFilePaths` puts the
project root first, and nothing copies `GameData` to the build output (the csproj has no copy
rule). So the file to edit is the one in the working tree, not a copy under `bin/`.

That one-line flip is now actually sufficient. It was not before: the committed file said
`"activeProfile": "full"` while this document said the profile was already `sparse`, so
following these instructions enabled pacing with the **passthrough** profile —
`narratorEnabled: false`, every step speaking, 8000ms paragraphs. The narrator would not have
appeared at all, and the obvious conclusion would have been that the voice work had failed.
`activeProfile` is now `sparse`, which is what this document always claimed.

Flipping `enabled` deliberately fails one assertion, `NarrativePacing.json as committed must
resolve to passthrough`. That is the canary proving the config is live rather than inert — see
decision note below. Everything else stays green.

1. ~~**Magic-string coupling.**~~ DONE 2026-10-15.
1a. ~~**`IsCriticalMissFlavorText`.**~~ DONE 2026-10-19 — deleted; the bank tag replaced it.
2. **`--run-tests` dies early.** Pre-existing `NullReferenceException` in *Equipped Item Trigger
   Tests* crashes the comprehensive runner before any combat suite. Use `--run-test-filter`.
   Max item.
3. **Rooms are dark on this branch.** `locationDescriptions` content is good but the
   `RoomInfoBuilder.cs` wiring lived only on v1. ~10 lines to port.
4. **Spreadsheet vs JSON.** Precedence doc says the workbook's `flavor` sheet is authoritative;
   the game reads `FlavorText.json`. Writing to JSON for this demo. Raise the pipeline question
   with Max before anything goes near `main`.
5. ~~**Five tests encode the pre-prototype behaviour.**~~ DONE 2026-10-19 (fourth session).
   It was worse than "they fail with pacing on": a suppressed swing left `LinesWrittenForTests`
   empty, the next assertion indexed it with `[^1]`, and the `IndexOutOfRangeException` **aborted
   the suite** — so the remaining ~240 assertions never ran and nobody could have known whether
   sparse broke anything else.
   Fix: `CombatSequenceFlavorPresenterTests` now pins `NarrativePacing.Profile.Full` for the
   whole class. That suite asserts how a swing is *assembled* — token contents, beat order,
   paragraph growth, colour — which is not pacing policy; pacing policy has its own suite in
   `NarrativePacingTests`. Reading the live config made the suite's meaning depend on a config
   value. **267/267 now pass with `sparse` enabled**, so the branch default is no longer gated
   on a test rewrite — only on Joey's call about whether the demo should ship narrator-on.
6. **Decision 2's separated format still does not exist.** F7 SWAPS prose and mechanics rather
   than stacking them (`CombatLogDualView.cs:8`). The F7-OFF view is the closest thing to it and
   is good enough for the demo. True stacking is a Max call under rule 5.
7. **Blank `{name}` bug (fixed here) exists on `main`.** See the narrator section, item 7.
   Worth a separate heads-up to Max — it is not a flavor-branch issue.
8. ~~**Remaining banks unwritten.**~~ DONE 2026-10-19 (fourth session) — `playerDefeated`,
   `environmentalAction` and `goodCombo` are in voice. See the Content section.
   `sequence*` banks are still the old placeholder register, now demoted to near-silence by
   `sparse`'s `stepChance`. They are the next content job only if the per-step texture clause
   survives Joey's reaction at all; under `sparse` it fires on roughly one swing in two and
   says one thing.
9. ~~**Should `enabled: true` be the branch default?**~~ DONE — yes. The branch exists to show
   Sean the game with the narrative in it, and a default that hides the work is wrong for a
   demo branch. The passthrough canary in `NarrativePacingTests` was rewritten to match: it now
   asserts `Profile.Full` is inert (the fallback for a missing or unreadable file) AND that the
   committed file resolves to something that is *not* the fallback, with the narrator on. That
   is a stronger check than before — a file that fails to parse returns `Profile.Full` from the
   catch, so the old single "is it passthrough" assertion could not tell a deliberate default
   from a broken read.
10. **`playerDefeated` is written but has not been seen on screen.** The fight-log harness kills
   the enemy, not the player. The bank is tested for voice and tier; the render path is the same
   one the other set pieces use. Low risk, still unobserved.

---

## Fourth session (2026-10-19) — what changed

Files touched:

| File | Change |
|---|---|
| `GameData/FlavorText.json` | `playerDefeated` and `environmentalAction` rewritten; `goodCombo` bank added |
| `Code/Combat/BattleEventAnalyzer.cs` | `goodCombo` draws the bank instead of two C# literals |
| `Code/Combat/NarrativeTier.cs` | `goodCombo` classified Beat explicitly |
| `Code/Combat/NarrativeTextProvider.cs` | fallbacks for those three banks match the new voice; env fallback no longer embeds `{effect}` |
| `GameData/NarrativePacing.json` | `activeProfile` `full` → `sparse` |
| `Code/Tests/Unit/Combat/CombatSequenceFlavorPresenterTests.cs` | whole class pins the passthrough profile |
| `Code/Tests/Unit/Combat/NarrativeTierTests.cs` | `playerDefeated` added to the overseer-voice check; exclamation-mark ban extended to every beat bank; two new tests |

Verification: `NarrativeTier` 51/51, `NarrativePacing` 9/9, `CombatSequenceFlavorPresenter`
267/267 — and 267/267 again with `enabled: true` / `sparse`, which is the point of the pin.
`Combat` filter failure list byte-identical to a clean stash, 17 both ways.

**`playerDefeated`, the new set piece.** The overseer addresses the player directly here. That
is licensed by the approved sample's closing lines (*"You are not the first to stand in this
room. I will not tell you how the others did."*), and a death is the moment the register is
built for — he was always going to have to say something to the man he refused to help:

```
  You went still. The Lich did not. I have never seen it settled any other way.

  I watched it happen and I did not reach for you. That is the agreement.

  The last thing you spent was the one you needed. I saw you spend it.

  You had it, and then you did not. The Lich only had to wait for the second part.
```

**`environmentalAction`, the world's voice.** Third person, present, no narrator. Has to read
across all seven themes in `EnvironmentalActions.json`, so nothing in it names a hazard:

```
  Both of them flinch at the same thing, and it is not each other.
  Neither of them chose this, and both of them pay for it.
  The room is not neutral. It only looks that way between fights.
```

**`goodCombo`, the world's voice.** `{name}` is the actor and fires for either side.
`ComboStep >= 2` means "three or more", not "three", so no line counts the blows — and
"three" is on the crutch-word list anyway:

```
  Each hit from Fergus sets up the next. Nothing here is improvised.
  Fergus lands one, then lands the one that only works after it.
  No gap opens between Fergus's blows. That is where a guard usually lives.
```

---

## The target voice — worked example

Ghoul in Emptiness Hall. Canon detail driving it: a Ghoul *goes for what a corpse carries
before the corpse itself.* So the fight is not about killing him — it is about the pack.

```
A Ghoul comes up the hall. It looks at his pack before it looks at him.

  CUT                     10
  First blood. It doesn't flinch - it shifts its grip, the way
  a man does before lifting something heavy.

  CHEAP SHOT              12

  STAB                    3x4
  Three times, fast. The wounds open clean and stay open.
  Nothing hurries to close them.

  CHEAP SHOT              12
  It reaches past his guard for the satchel strap, not the throat.
  He would rather it wanted the throat.

  CUT                     10

  CUT                     10
  Half of it is on the floor and its pace hasn't changed.
  Whatever keeps that pace does not get tired.

  CHEAP SHOT              12
  Same angle. Same reach. Same strap.
  It isn't learning his guard. It already knew.

  STAB                    CRIT
  He stops cutting at the body and cuts the rhythm instead.
  The rhythm stops.

  CUT                     10
  It goes down still holding what it took.
  Cayden has to open the hand to get it back.
```

Nine beats, five of them silent or near-silent. Every line fits inside its turn's reading
budget. Three lines point at the machines; none names one.

---

## The overseer's voice — worked example (approved 2026-10-19)

Same fight twice. Only the voice changes. The second is the approved register.

**Rejected — commentator.** Gets its energy from volume, and becomes unbearable by the
fortieth fight:

```
  Here we go. A narrow room and nobody coming.
  First blood to Fergus. The Lich does not appear to have noticed.
  Oh, that was deliberate. He found the seam and went through it.
```

**Approved — overseer.** Gets its energy from what he knows. Nothing here asks the reader
to react, which is why it survives rereading:

```
  It was a man. It kept the shape and let the rest go.

  First blood. I have never seen a fight decided by it.

  Two unanswered. Fights turn on less.

  He is down to half. I have watched men win from worse
  and lose from better.

  He is nearly out. I will not look away.
  That is the whole of what I can give him.

  He found the seam. Men look a lifetime for one
  and walk past it twice.

  It stops. What kept it moving does not take the motion back up.

  You are not the first to stand in this room.
  I will not tell you how the others did.
```

Joey's note on the line he singled out — *"I have watched men win from worse and lose from
better"* — "it sums up the detachment I want from the character."

**The rules this voice runs on:**
- **Past tense, first person, always.** He recounts; he does not commentate.
- **Every line contains something you can point at.** Cold, seam, pace, half, floor. The moment
  a line rests on an abstract noun (*fate, moment, presence, truth*) it is the AI tell again.
- **Built on balance, not adornment.** *win from worse / lose from better.* *look a lifetime /
  walk past it twice.* The ear registers the shape before the meaning.
- **Almost no adjectives.** Adjectives are where this register dies.
- **He withholds.** He has seen it; he will not say how it goes. This is also why he never
  spoils a fight in progress.
- **No exclamation marks, ever.** Enforced by `NarrativeTierTests`.

The world's voice (Beat tier) stays third person, present tense, no narrator in it — v1's F-01
banks are already correct for this and were ported as written.
