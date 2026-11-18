# Rebuild agent brief — overnight run, steps 0–2 (2026-11-18)

You are implementing the start of the core rebuild designed in
[`CORE_DESIGN_PASS_2026-11-17.md`](CORE_DESIGN_PASS_2026-11-17.md). The user and Claude spent a long
session deciding every rule in that doc. **Implement the decisions; don't redesign them.**

## Read first
1. `CORE_DESIGN_PASS_2026-11-17.md`:
   - **Section 1** (pillars), **2a–2c** (sequence and the ladder)
   - **Phase 10** (the build plan): your scope is **Step 0, Step 1, Step 2**
   - every decision (`D0.x`, `D1.x`, `D2.x`, `D4.x`, `D5.x`, `D9.6`) that those steps cite. The decision
     logs are the source of truth; the "Starting proposal" text above them is superseded wherever a
     decision differs.
2. `CLAUDE.md` (the codebase map).

## Scope tonight, in order
- **Step 0 — Foundation:** 0.1 is done (the in-flight work has landed on `v46-refactor`). Do **0.2** dev
  build flag, **0.3** `SystemUnlocks`, **0.4** text pipeline and the leaking "+{8}%" braces in skill and item
  text.
- **Step 1 — Rung 1: dice, one slot, the board** (every row of the step 1 table).
- **Step 2 — Items, the second slot, the dungeon's end** (every row of the step 2 table).

Don't start step 3 or later. If you finish early, write the status note and stop.

## Rules
- **Work on your own branch** (this session's worktree). Never touch `main`, `v46-refactor` or other
  worktrees. **Don't push, and don't open a PR.**
- **One commit per sub-step** (for example "Rebuild 1.3: one AMP formula"). End every commit message with
  the attribution line from the session's system reminder.
- **Switch off before deleting** (Phase 10, "How we build"). Cut systems go behind config or
  `SystemUnlocks` first. Delete old code only once its replacement works and tests pass.
- **Old saves must load.** Follow the Phase 10 save migration table for anything you retire (charms →
  bag; Commons lose their modifier and action, starters exempt).
- **No balance work:** no DUNGEONSIM, bo-solve, balance-scan or patch changes. The only number change in
  scope is D0.4 (level 1 fights about 2 hero swings), as a data knob.
- **Tests after every sub-step:** `dotnet build Code/Tests/Tests.csproj`, then
  `Code/bin/Debug/net8.0/DF.exe --run-tests` (or `Scripts/run-tests.ps1`). Fix what you broke; update
  tests whose expectations the decisions changed, and say so in the commit.
- **After each step (0, 1, 2), walk the first hour** with the `/playtest` skill or the MCP gameplay tools.
  First check that the MCP server is running *your worktree's* build; if it isn't, say so in the status
  note and rely on tests.
- If `DF.exe` is locked by a running game, build to a scratch output folder instead.
- **Player-facing text** follows the design: plain, one line per rule, mechanics only in descriptions.
  Use the public name **Demon Fighter** in player-facing text only; never rename code.

## Defaults already settled
- Item card layout: comparison first, one line per attribute, next set step only, details behind a toggle.
- Placeholder rules or text the doc only gives as examples: use them, and mark them `REVIEW`.
- No lore renames tonight (step 11).

## Stop conditions
Stop the current sub-step, note it, and move to the next independent one (or stop entirely if nothing
independent is left) when:
- tests fail twice after real fix attempts,
- the doc doesn't cover a decision you'd need to make (don't invent design: write the question down), or
- a change would need balance work.

## Status note
Keep **`Documentation/REBUILD_STATUS_2026-11-18.md`** up to date and commit it with each sub-step. For
each sub-step it records: done / partial / blocked, the commit, tests run and their results, what the
playtest showed, and **questions for the user**. The user reads it in the morning.

## When you're done
Update the status note, make the final commit, and send a push notification that leads with the result
(steps finished, test status, number of open questions).
