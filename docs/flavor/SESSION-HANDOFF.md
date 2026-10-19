# Demon Fighter — Flavor Text Session Handoff

**Last updated:** 2026-10-15 (second session) · branch `joey_sandbox_v2`

**New session? Read this file first, then `.cursor/rules/demon-fighter-flavor.mdc`.**
That covers everything. Don't re-derive it.

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

So `FlavorText.json` on this branch holds the OLD placeholder combat text
("The first drop of blood is drawn! The battle has truly begun.") — the wordy register
Joey is trying to get away from.

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

## Open items

**Next task:** rewrite 2-3 of the `sequence*` banks in the agreed voice, with the pacing
prototype switched on, and show Joey. Those are the banks that actually reach the screen.
The three one-shot banks already written stay as a voice reference but do not render.

**Note on the register for `sequence*`:** these banks are a *woven* format by construction —
each line is a fragment with a mechanical token interpolated (`{outcome}`, `{damage}`,
`{stance}`). Decision 2 chose the separated format and deferred woven to a later session. The
pacing gate narrows the gap (fewer fragments per swing) but does not close it. Getting true
separated format is the `CombatLogDualView` swap-vs-stack change — a Max call under rule 5.

1. ~~**Magic-string coupling.**~~ **DONE 2026-10-15** — see "Code work completed" above.

1a. **A second magic-string sniff is still live: `BattleEventAnalyzer.IsCriticalMissFlavorText`.**
   It matches `criticalMiss` lines against 14 literal phrases (`"wild swing"`, `"goes wide"`,
   `"empty space"`, …). It is **only called from tests**, not from the shipping display path,
   so it is not a live display bug — but `BattleNarrativeTests.cs:157` asserts that a natural 1
   surfaces a line matching it. **Rewriting the `criticalMiss` bank will break that test.**
   Fix it the same way, in the same pass that writes that bank. Not touched this session,
   because the `criticalMiss` bank was not part of the three-bank sample.
2. **`--run-tests` dies early.** A pre-existing `NullReferenceException` during
   *Equipped Item Trigger Tests* crashes the comprehensive runner before it reaches any
   combat suite. Pre-existing, not from this session's work. Probably a Max item.
3. **Rooms are dark on this branch.** `locationDescriptions` content exists and is good, but
   the `RoomInfoBuilder.cs` wiring lived only on v1. ~10 lines to port.
4. **Spreadsheet vs JSON.** Precedence doc says `DEMON FIGHTER - DATA (1).xlsx` (`flavor`
   sheet) is authoritative; the game reads `FlavorText.json`. Decision made: write to the
   JSON for this demo, raise the pipeline question with Max before anything goes near `main`.
5. **Task 3 (narrator persona)** and **task 4 (three research-backed prototypes)** not started.

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
