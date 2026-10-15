# Demon Fighter — Flavor Text Session Handoff

**Last updated:** 2026-10-15 · branch `joey_sandbox_v2`

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

---

## Open items

**Next task:** draft 2-3 combat banks in the agreed voice (see decision 9 — not all 13),
show Joey, get the voice approved, then write the rest. The pattern is the Ghoul example
at the bottom of this file.

1. **Magic-string coupling — APPROVED TO FIX, do it with the crit bank.**
   `BattleNarrative.cs:242` identifies critical-hit lines by searching the prose for the
   literal strings `"devastating blow"` / `"strikes true"`. New crit prose contains neither,
   so it would bypass the significance-and-cooldown filter and fire on EVERY crit — the
   opposite of the variable-interval pacing in decision 4. Replace the string sniff with a
   real signal (e.g. have the analyzer tag which bank a line came from) at the same time the
   crit bank is written.
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
