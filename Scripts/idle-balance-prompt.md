You are running unattended while the user's PC is idle. Nobody will answer questions: decide, act, and write down
anything that needs the user's call. You may be killed at any moment when the user comes back, so commit each verified
step as soon as it is done.

You are in the worktree `DungeonFighter-v2-idle` on branch `idle-balance`. Work only inside this folder. Never push,
stash, reset, checkout, merge or touch the main checkout (`D:\Code Projects\github projects\DungeonFighter-v2`).
Do not edit `Scripts/idle-balance*`.

## Before you start

1. Read the balance memories in `C:\Users\Seanye\.claude\projects\D--Code-Projects-github-projects-DungeonFighter-v2\memory\`
   (MEMORY.md and every file it lists). They hold the user's standing decisions; follow them. Read only, don't edit.
2. Read `Documentation/BALANCE_PROCESS.md` and `Documentation/IDLE_BALANCE_LOG.md` (create the log if missing). The log's
   last entry says what the previous pass did and what it planned next.
3. `git status`: uncommitted changes are a pass that was killed mid-way. Finish and verify that step if the log explains
   it, otherwise revert those files with `git diff` + Edit (not `git checkout`).

## One pass

Follow the scan → focus → gate loop:

1. Build: `dotnet build Code/Tests/Tests.csproj` (builds the game too). Fix build errors you caused; if the tree was
   already broken, log it and stop.
2. Scan: `python Scripts/balance-scan.py`, with `--compare` against the newest earlier scan JSON in `GameData/TuningRuns/`.
   Check the first line shows the patch you expect.
3. Pick the top one to three problem areas that are tuning (patch values, curve anchors, enemy/action numbers), run the
   focus commands the scan printed, and apply the fix.
4. Re-scan with `--compare`. Keep a change only if it fixes its finding without new FAILs; otherwise undo it and log why.
5. When the pass kept changes, run `powershell -File Scripts/balance-check.ps1` once as the gate, and the unit tests
   (`Code/bin/Debug/net8.0/DF.exe --run-tests`).
6. Commit with a message in the repo's style (`Balance: …`). Patch files under `GameData/Patches/` are gitignored:
   `git add -f` them.

Scope: tuning only. Don't redesign systems, add mechanics, or change UI. A finding that needs a design decision or code
change goes in the log under **Needs your call** with the evidence. If the scan finds nothing actionable, log that and
end the pass without a commit.

## Log entry

Append to `Documentation/IDLE_BALANCE_LOG.md` and commit it with your work:

```
## <date time> — <patch name>, HEAD <short sha>
- Scan: <key findings, before → after>
- Changed: <what and why, or "nothing">
- Gate: <balance-check result, tests pass/fail>
- Needs your call: <decisions for the user, or "none">
- Next: <what the next pass should look at>
```

Run long commands (scan, gate) in the foreground with a long timeout; don't background them.
