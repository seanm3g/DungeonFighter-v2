# Demon Fighter — Format Trial Log

One entry per format tried. Newest at the top. This is the handoff note between Cursor chats and the record to show Sean.

**Starting a new Cursor chat?** First message: "Read docs/flavor/format-log.md and docs/flavor/format-spec.md, then pick up where the log leaves off."

---

## Current state
- Working on: voice rewrite of existing FlavorText.json banks (joey_sandbox_v1). Prose is in. Not committed.
- Last commit: none for this pass.
- Next step: Joey reads a few fights and a few rooms. If the heat is right, commit. If a locked bank should join the new voice, that is a test-pin change and it belongs to Max.

---

## Entry template (copy for each new format)

### F-00 — [short name]
- **Date:**
- **Where it shows:** (room entry / encounter / combat log / loot / travel / other)
- **Mockup (as typed):**
```
paste Joey's mockup here
```
- **In-game result:** (screenshot file name or paste of the actual screen text)
- **Data it pulls from:** (slots/banks from format-spec.md)
- **Handles empty/small pools?** (yes / no / not tested)
- **Files changed:**
- **Commit:**
- **Verdict:** KEEP / KILL / ITERATE
- **Notes for Sean/Max:**

---

## Entries

### F-01 — Voice rewrite of existing banks
- **Date:** 2026-10-14
- **Branch:** `joey_sandbox_v1`. Nothing committed.
- **Where it shows:** combat log flavor lines, room-entry location and room-context lines, room-enter category pools, taunts. Same moments as before.
- **Mockup (as typed):** Joey said "Go." Same slots, same tokens, same fire moments. Popular-fiction heat, one ringside chronicler, one or two sentences, a turn that pulls the next line.
- **In-game result:** not screenshotted this pass. Tests below exercise the banks the code loads.
- **Data it pulls from:** `GameData/FlavorText.json` narrative banks. Fill rules are in `docs/flavor/format-spec.md` section 3c and section 8.
- **Handles empty/small pools?** Pool sizes were not changed. Empty-pool behavior is still the code's.
- **Backup:** `docs/flavor/FlavorText.pre-voice-rewrite.json` (not in a folder the game loads). Restore by copying that file over `GameData/FlavorText.json`.
- **Files changed:** `GameData/FlavorText.json`, `docs/flavor/format-spec.md`, `docs/flavor/format-log.md`, plus the backup.
- **Commit:** none.
- **Verdict:** ITERATE until Joey hears it in a fight.
- **Counts:** 555 strings replaced with new prose. 1 sentence kept verbatim inside a rewritten bank. 21 strings in six banks left byte for byte because tests pin them. Name generators, consumable labels, location names, class titles, and the room-enter form template were not narration and were left alone.

#### Banks rewritten (every string in the pool, except the one pin noted)
- `environments.locationDescriptions`: Forest 9, and 8 each for Lava, Crypt, Crystal, Temple, Cavern, Swamp, Desert, Ice, Ruins, Castle, Graveyard, Generic (105).
- `environments.roomContexts`: Forest boss 3, treasure 4, library 5, armory 3, kitchen 3, chamber 3, sanctum 4, shrine 4; Lava, Crypt, Crystal, Temple at 3 per room type (boss, treasure, library, armory, kitchen, chamber, sanctum, shrine); Generic at 3 for boss, treasure, guard, trap, puzzle, rest, storage, library, armory, kitchen, dining, chamber, hall, vault, sanctum, grotto, catacomb, shrine, laboratory, observatory, throne (185).
- `categories.intro` 25, `categories.detail` 25.
- Combat, all lines new except the pin: `firstBlood` 5; `firstBlood_nativeFauna` 3; `firstBlood_feralStock` 3; `firstBlood_technoEcho` 2 new + 1 kept; `healthRecovery` 5; `below50Percent` 8; `below50Percent_feralStock` 3; `below50Percent_technoEcho` 3; `below10Percent` 8; tier below-10 banks 3 each; `criticalHit` 8 and three tier banks of 3; `criticalMiss` 8; `criticalMiss_feralStock` 3; `criticalMiss_technoEcho` 3; `environmentalAction` 5; `healthLeadChange` 5; `intenseBattle` 8; `enemyDefeated` 8; `enemyDefeated_feralStock` 3; `enemyDefeated_technoEcho` 3; `playerDefeated` 8; `playerTaunt` 10; `enemyTaunt` 10; biome taunt pairs (library, underwater, lava, crypt, crystal, temple, forest, ice, swamp) at 4 each.

#### Kept verbatim (do not treat as the new voice)
`BattleNarrativeTests.AssertExactCombatBank` pins these. Rewriting them fails that test. Left untouched:
- `enemyTaunt_nativeFauna` (4), `enemyTaunt_feralStock` (4), `enemyTaunt_technoEcho` (4)
- `enemyDefeated_nativeFauna` (3)
- `criticalMiss_nativeFauna` (3) — line 2 contains `goes wide`, so `TextDisplayIntegration` can swallow it unless the action line says `CRITICAL MISS`
- `below50Percent_nativeFauna` (3)

Also kept, because `BattleEventAnalyzerTests` requires the substring: `The cut opens clean and even on {name}, like something measured it first.` It now sits first in `firstBlood_technoEcho`. The other two lines in that bank are new. No line in any `firstBlood_{tier}` bank says `draws blood` or `like {name} measured it first`.

No Joey "insert exactly" package was found. The locks above are test pins, not an approved content package.

#### Left alone, and why
- `names.*`, `environments.locationNames`, `items.consumableNames`: name and item generators, not narration.
- `classQualifiers.*`: class titles and aliases, not narration.
- `forms.roomEnter` and `forms.TEST`: format skeleton. The room-enter template still reads `You step inside. <intro>. <detail>..` (the extra period is in the template). Category lines have no terminal punctuation so that period can land.
- Combo sentences and `GetFallbackNarrative` live in C#. Not this file.
- `TravelEvents.json`, `Rooms.json`, affix tables: not this pass.

#### Tier physical facts kept (not new lore)
- `nativeFauna`: an animal body. Blood, teeth, legs, a hunter's opening. Taunts in the locked bank stay non-verbal.
- `feralStock`: too controlled for wild. A drilled line, a flinch as if a hand might correct it, an order that does not come, a stillness like being let go.
- `technoEcho`: a cut that opens clean, a wound that does not spread like living blood, a rhythm that does not change, warmth that leaves too fast, a stillness too even for a death. The locked taunts stay quoted speech. No banned word is used to name what the tier "is."

#### Citations actually verified
Recorded with the one-sentence rule in `format-spec.md` section 8. Cowan 2001; Sweller 1988 and Sweller, van Merriënboer, and Paas 1998; Mayer, Heiser, and Lonn 2001 (coherence); Rayner 1998; Kintsch 1988; Green and Brock 2000; Loewenstein 1994; Brewer and Lichtenstein 1982 (Technical Report 265). Craft: King, *On Writing* (2000); Sanderson's 2025 Promise / Progress / Payoff lecture. Martin (*A Game of Thrones*, 1996), Tolkien (*The Lord of the Rings*, 1954–1955), and Herbert (*Dune*, 1965) are the books Joey named. No standalone craft essay was verified for Martin's "the blow matters," Tolkien's cadence, or Herbert's short-line pressure, so those three rules are labeled as Joey's framing, not as papers.

#### Max decisions still open
- Whether to cap voice at one line per turn. No cap was found in the display path.
- Environmental narrative is generated and then not shown on the room turn (`ProcessEnvironmentTurnAsync` passes null narratives).
- Last-line memory. The picker is `Random.Next`.
- Critical-miss keyword sniff in `TextDisplayIntegration`.
- Taunts match the room display name, not the dungeon theme. Tier taunts override biome taunts when the tier bank is non-empty.
- Updating the verbatim test pins if Joey wants those six banks in the new voice.
- The extra period on the room-enter form template.

#### Risks
- `enemyTaunt_crystal` must keep an em dash (`—`). `BattleNarrativeTests` checks that the character is still in the bank. One line has it.
- `criticalMiss_nativeFauna` line 2 (`goes wide`) can vanish unless the action text contains `CRITICAL MISS`.
- `enemyTaunt_feralStock` line 2 has no `{player}`. The animal-taunt test also requires the filled line to contain the player name, and the picker is random, so that assertion can fail when it draws that line. The line is test-pinned verbatim, so it was not edited.
- Any enemy with a creature tier shows the tier bank, not the generic one, for first blood, crits, misses, the two health thresholds, defeat, and enemy taunts.
- `enemyTaunt_{tier}` replaces biome taunts for that enemy. The new biome taunts still fire for enemies with no tier, or when the tier taunt bank is empty.
- Room-name substring taunts can surprise: `fire` selects lava, `grove` selects forest, `tomb` selects crypt.
- Category lines are written for the template `You step inside. <intro>. <detail>..` If something displays a category raw, it will not have a closing period.
