Manage balance patches: list, show the active one, create a candidate, diff, switch, commit.

Usage: /patch [list | active | new <name> [from <base>] | diff <a> <b> | use <name> | commit <name>]

## How patches work
- A balance patch is a full config JSON in `GameData/Patches/Balance/<name>.json`. The game and every headless tool
  load the one named by `activeBalancePatch` in `GameData/PatchProfile.json` (gitignored, per machine). The same file
  also picks `activeGameSettingsPatch` and `activeAudioPatch`; leave those alone.
- Any headless tool accepts `--patch <name|path>` to use another patch for that run without switching. Prefer this for
  testing candidates.
- `GameData/Patches/Balance` is gitignored: commit a patch with `git add -f`.
- Naming: the current line is `fastbot-vN` (the version after the active one); experiments get a descriptive name
  (e.g. `classcurve-v1`). Old patches stay as records; don't delete them.
- Not the patch system: `GameData/BalancePatches/` and the MCP `save_patch` / `load_patch` / `list_patches` /
  `save_configuration` tools are legacy. Writes from those are blocked by `LegacyTuningWriteGuard`.

## Actions
- **list**: list `GameData/Patches/Balance/*.json` (skip `.bak`) by modified date, marking the active one.
- **active**: print `activeBalancePatch` from `PatchProfile.json` and the last commit touching that file
  (`git log -1 --format='%h %ad %s' --date=short -- GameData/Patches/Balance/<name>.json`).
- **new <name> [from <base>]**: copy `<base>` (default: the active patch) to `<name>.json`. Refuse if `<name>` exists.
  Do not activate it.
- **diff <a> <b>**: compare the two JSONs leaf by leaf (a short Python `json` walk) and print the changed paths with
  old → new values. Group by top-level section; for `damageCurve` / `healthCurve` anchors, print level: a → b (ratio).
- **use <name>**: ask the user first, then set `activeBalancePatch` in `PatchProfile.json`. Note that DF.exe must be
  restarted to pick it up.
- **commit <name>**: `git add -f GameData/Patches/Balance/<name>.json` and commit it with a message that says what
  changed and what it was checked with (levels, seed, clear rates before → after). Ask before committing if the user
  hasn't asked for it.

Edit candidate patches only, never the active patch in place. The measurement loop is `/balance`.
