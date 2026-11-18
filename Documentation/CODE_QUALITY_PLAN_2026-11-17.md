# Code Quality Plan (2026-11-17)

Goal: move the codebase from about a B− to a solid A− without changing game behavior. The weak spots are global
state, swallowed exceptions, an unchecked string rule language, and oversized files. Documentation, tests and
tooling are already strong, so we build on them.

## Baseline (2026-11-17, `main` at b2c15efb plus the uncommitted synergy pass)

| Metric | Now | Target |
|---|---|---|
| Game code | 1,280 files / 255k lines | shrink, don't grow |
| Tests | 459 suites, ~3,050 test methods, 110k lines | keep, run in CI |
| Compiler warnings (`Code.csproj`) | 10 (CS0649 ×6, CS0414 ×4) | 0, warnings as errors |
| `GameConfiguration.Instance` | 629 refs in 198 files (78 are test files) | combat core: 0 |
| `GameSettings.Instance` | 241 refs | outside UI: 0 |
| Other `.Instance` singletons | 12 more types (GameTicker 68, SkillEffectRouter 45, CombatEventBus 37, ...) | per-session or per-combat |
| `catch (Exception)` + bare `catch` | 667 (UI 216, Game 157, Data 75, Audio 70, Entity 34, Combat 29, MCP 28) | ≤150, all at boundaries |
| Empty catch blocks | 47 | 0 |
| Non-test files > 800 lines | 27 (13 over 1,000) | 0 over 800 |
| Rule parser failure paths | 34 `return null` with no reason | each reports why |
| Analyzers / `.editorconfig` / CI | none | on |

## Ground rules

1. **Behavior stays the same.** Every phase except Phase 2 is a pure refactor. Seeded DUNGEONSIM runs are bit-identical,
   so a **golden-run diff** (Phase 0) proves each change: the same seeds must give the same numbers. Phase 2 may change
   behavior on purpose (a hidden exception now surfaces), and every difference becomes a bug ticket, never a silent update
   to the golden files.
2. **One phase per branch, in its own worktree.** Other sessions edit this checkout at the same time, so no `git stash`
   here (see the memory rule). Merge small: one subsystem per PR.
3. **Measure as we go.** `Scripts/quality-metrics.py` (Phase 0) prints the table above. The counts may only go down;
   the gate fails if one rises.
4. **No feature freeze, except during Phase 3's combat-core step**, which touches the hot path that balance work uses.

## Phase 0: Safety net (about 1 day)

- Land or shelve the uncommitted synergy pass first (24 files). Refactoring on top of it would mix two kinds of change.
- **Golden runs:** `Scripts/golden-run.ps1` runs a fixed set of seeded DUNGEONSIM shards (for example L3, L8, L13, L28,
  L53, all classes, about 2 min locally), FUNDMATRIX on one seed, and the full test suite. It stores the outputs under
  `GameData/TuningRuns/golden/` and `--check` diffs against them. Pin the balance patch with `--patch`, so a patch switch
  can't fail the check.
- **Metrics script:** `Scripts/quality-metrics.py` reports the baseline table (singleton refs per type, catches per
  folder, empty catches, files > 800 lines, warnings).
- **Compiler hygiene:** fix the 10 warnings. Add `Directory.Build.props` with `TreatWarningsAsErrors`, an
  `.editorconfig`, and `AnalysisLevel` set to `latest-recommended`. Raise only these analyzer rules to warnings at first:
  CA1031 (catch-all), CA2016, CA1822, IDE0051/IDE0052 (unused private members). Suppress per folder with
  `.editorconfig` until each phase reaches that folder.
- **CI:** a GitHub Actions workflow on `windows-latest` runs build, tests, rule lint (Phase 1) and the quick golden run.
  The Spark stays local. (Decision: CI on GitHub Actions or a local pre-push gate only?)

Done when: `golden-run.ps1 --check` passes twice in a row on `main`, the build has 0 warnings, and CI is green.

## Phase 1: Check the rule language at load time (2-3 days, highest payoff for the effort)

The `CrossWires` rule grammar, the action `verb` / `cost` / `timing` columns, skill-node `persist`, and modifier
templates are small languages written as strings. Today a typo makes `CrossWireRule.Parse` return `null` (34 different
paths) and the effect quietly does nothing. `SynergyPassTests` checks skill nodes and base modifiers, but nothing else
is covered and the failure message doesn't say why.

1. **Errors with reasons:** add `CrossWireRule.TryParse(spec, source, out rule, out RuleError error)`. `RuleError` gives
   the token position and the problem, for example `unknown NEXT stat "BOGUS" (expected DAMAGE, SPEED, AMP, ...)`.
   `Parse` stays as a thin wrapper, so callers don't change.
2. **One keyword registry:** move `TriggeredVerbs`, `PassiveStats`, `NextStats`, `SelfKeys`, `BuffStats`, ... and the
   WHEN tokens from `TriggerEvents` into one `RuleVocabulary`. The parser, the lint and the tooltips/preview all read
   it, so a new keyword is added in one place.
3. **Check that names exist:** a rule can parse and still point at nothing. Check store ids against `Stores`, status ids
   against the effect registry, tags against the action tag set, `RANK node_id` against the skill trees, and
   `IF ACTION Name` against `ActionTables.json`.
4. **`RuleLint` in `Code/Data/Validation/`:** walks every rule source (skill nodes, CROSSWIRE/RULE prefixes, base
   modifier catalog rows in Weapons/Armor.json, `BaseModifiers.json` pools, action `verb` / `cost` / `timing`,
   `Conversions.json`, node `persist`). It reports `file › id › text › error`. Hook it into `GameDataValidator`, run it
   at startup (log in release, throw in Debug/tests/sims), in CI, and as a `--lint-rules` flag. `build-item-mods.py`
   calls it after writing the catalogs.
5. **Split `CrossWires.cs` (1,424 lines)** while it's open: `RuleModel` (condition/per/rule types), `RuleParser`,
   `RuleEvaluator` (conditions, PER, passive totals), `RuleSources` (gathering rules from skills/items). This is the one
   large-file split pulled forward from Phase 4.

Done when: lint runs clean on all GameData, a fixture file of deliberately broken rules gives one precise error each,
and the golden run is unchanged.

## Phase 2: Exception policy (about 1 week, spread over subsystems)

Policy, written into the `CONTRIBUTING` section of `CLAUDE.md`:

| Layer | Rule |
|---|---|
| **Boundaries**: audio device, file/HTTP I/O, Sheets sync, the MCP tool entry point, Avalonia event handlers, the live-feed server | May catch broad exceptions. Must log through `ErrorHandler` with context. Never an empty block. |
| **Data load** (`Data/`, `Config/`) | Catch, report the file and field, use the fallback, and **throw in strict mode** |
| **Game logic** (`Combat/`, `Entity/`, `Items/`, `Actions/`, `World/`, the rules in `Game/`) | No catch-all. Catch only specific expected exceptions. |

Steps:
1. Add `ErrorHandler.Report(ex, context)` with per-context counters, and a **strict mode** (`DF_STRICT=1`, on by default
   for tests, DUNGEONSIM, BUILDLAB and CI) that rethrows after logging. Sims will then surface the bugs they currently
   hide. Print the counter totals in the sim `Provenance:` block.
2. Work through the folders in risk order: Combat + Entity + Items + Actions (about 66), then Data + Config (97),
   then Game (157), then MCP (28). Then audit UI (216) and Audio (70): most are legitimate boundaries, so the work there
   is mainly logging and removing the 47 empty blocks.
3. Run the golden run with strict mode on after each folder. A newly thrown exception is a real bug: fix it in its own
   commit, and only then refresh the golden files, with a note on why.
4. Turn CA1031 into an error for the game-logic folders.

Done when: at most 150 broad catches remain, all at boundaries, there are 0 empty catches, and the strict-mode golden run
is clean.

## Phase 3: Pass config explicitly instead of using global singletons (2-3 weeks, gradual)

Why it matters beyond style: the singletons are why sims inherited the difficulty sliders, why tests leaked the active
patch (`TestConfigScope` patches over this), and why one process can't run two balance patches. That last one blocks
running bo-solve rounds in-process.

1. **Split `GameSettings`:** move the game-rule values (difficulty sliders, anything that changes combat numbers) into
   a `DifficultyConfig` owned by the tuning config. Leave `GameSettings` for UI and audio preferences only. Combat code
   must not read `GameSettings` at all.
2. **A `GameContext` object** (config snapshot, difficulty, `Random`, event bus, ticker) created per session or sim run.
   `GameConfiguration.Instance` stays as the default for UI code during the migration.
3. **Combat core first** (Combat 22 files, Entity 16, Data 12, Items 4, Actions 3, World 3): `CombatManager`,
   calculators, `ItemRoller` and builders take the context through their constructor or method parameters.
   `DungeonSimRunner` builds its own context from `--patch`. **Feature work on combat stops while this step is in
   review.**
4. **Per-combat services:** `CombatEventBus` and `HeroEventHub` belong to one fight. `SkillEffectRouter` becomes
   stateless or lives in the session, and `GameTicker` lives in the session.
5. **Tests:** suites build a context (`TestContext.CodeDefaults()`) instead of swapping the global. Remove
   `UseCodeDefaultsForTests` and `RestoreInstanceForTests`. Reduce the 8 `ResetInstance()` callers to one reload in the
   session.
6. **Proof:** a test runs two DUNGEONSIM shards with different patches in parallel in one process, and each matches its
   single-process golden output.

Done when: Combat/Entity/Items/Actions/Data/World have 0 `.Instance` refs, sims run in parallel in-process, and the
golden run is unchanged.

## Phase 4: Split the large files (1-2 weeks, can run alongside Phases 2-3 because it's UI-heavy)

Order by recent churn × size (commits in the last 60 days / lines):

| File | Churn | Lines |
|---|---|---|
| `UI/.../Inventory/InventoryScreenRenderer.cs` | 25 | 1,622 |
| `UI/Avalonia/Layout/CharacterPanelRenderer.cs` | 22 | 937 |
| `Combat/MaterialSetController.cs` | 21 | 869 |
| `UI/Avalonia/Canvas/CanvasPrimitivesRenderer.cs` | 20 | 1,426 |
| `UI/Avalonia/GameCanvasControl.cs` | 20 | 1,219 |
| `UI/Avalonia/Handlers/MouseInteractionHandler.cs` | 19 | 1,189 |
| `UI/LeftPanelTooltipBuilder.cs` | 18 | 843 |
| `UI/.../DungeonRenderer.CombatBoard.cs` | 17 | 1,473 |
| `Config/CombatConfig.cs` | 26 | 713 (split by system, not size) |

Rules: extract **cohesive classes** with their own state (a layout model, a hit-test map, a tooltip section builder).
Don't cut a file into `partial` slices: 129 partial classes already hide size this way. That includes the
`DungeonRenderer.*` partials, which should become collaborators. Soft cap 600 lines, hard cap 800, enforced by the
metrics gate. UI files can't be checked by the golden run, so take a screenshot of each affected screen before and after
(the `run` skill / MCP `get_agent_context` text dump).

## Phase 5: Test infrastructure (about 1 week)

- Keep the 459 suites and `TestBase` assertions. Rewriting 3,000 tests isn't worth it.
- Add a `[TestSuite]` attribute and reflection discovery, so the game's `--run-tests`, Settings → Testing and the MCP
  tools find suites by attribute instead of name strings (fixes the silent break on rename).
- Add a thin xUnit project (`Code/Tests.Xunit`) with one `[Theory]` per discovered suite, so `dotnet test`, IDE test
  explorers and CI work. (Decision: thin adapter (recommended) or full xUnit migration?)
- Coverage report (coverlet) in CI. No target at first: use it to find untested rule verbs and parser paths.

## Phase 6: Ongoing hygiene

- Move the 64 date-stamped code comments (`(2026-11-15)` ...) into the design docs and git history. Code comments say
  what the code is, not when it changed.
- Set a save-format version and delete legacy migrations (`MaterialTriggerMerge.MigrateLegacyImplicit`, Expose aliases,
  ...) once the version they serve has aged out.
- Before each new system: check `minimize-new-mechanics` (express it in existing rules first) and add its keywords to
  `RuleVocabulary`, so the lint covers it from day one.
- Monthly: run `quality-metrics.py`, `/code-review` the month's biggest diff, and do a dead-code sweep (the existing
  pass-1 / pass-2 method).

## Order and parallelism

```
Phase 0 ──► Phase 1 ──► Phase 2 (folder by folder) ──► Phase 3 (combat core, freeze) ──► Phase 5
              │                                                                       ▲
              └──► Phase 4 (UI splits; separate worktree, independent) ───────────────┘
Phase 6: ongoing
```

Phases 1 and 4 touch different files and can run as parallel sessions. Phases 2 and 3 both touch combat and must run
one after the other.

## Open decisions

1. CI: GitHub Actions (`windows-latest`) or a local pre-push gate only?
2. Tests: thin xUnit adapter over the current suites, or a full migration?
3. Strict mode: on by default for all sims and tools (recommended), or opt-in?
4. Is a short freeze on combat feature work acceptable during Phase 3's combat-core step?
