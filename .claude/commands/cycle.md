Automated full balance cycle - analyze, tune, test, verify, and save.

Usage: /cycle [target-winrate] [iterations]
- target-winrate: Target win rate (default: 90)
- iterations: Number of tuning iterations (default: 5)

This is the MASTER COMMAND that orchestrates ALL agents in sequence:

PHASE 1: ANALYSIS (Analysis Agent)
  - Runs comprehensive diagnostics
  - Identifies current problems
  - Generates problem report

PHASE 2: TUNING (Balance Tuner Agent)
  - Applies targeted adjustments
  - Uses analysis report to prioritize changes
  - Tests changes with what-if scenarios
  - Iterates toward target win rate

PHASE 3: VERIFICATION (Tester Agent)
  - Runs full test suite
  - Validates all metrics
  - Checks for regressions
  - Compares to baseline
  - Generates test report

PHASE 4: GAMEPLAY (Game Tester Agent)
  - Plays one dungeon run with random weapon
  - Verifies fun factor
  - Spots any edge cases
  - Provides qualitative feedback

PHASE 5: SAVE (Config Manager Agent)
  - Saves successful configuration as patch
  - Creates timestamped backup
  - Documents changes made
  - Updates version history

This single command replaces the need to call individual agents sequentially.

The agents communicate:
- Analysis → Tuner (problem report guides adjustments)
- Tuner → Tester (test results confirm or reject changes)
- Tester → Tuner (failures trigger re-analysis)
- Final → Config Manager (successful cycle is saved)

Examples:
  /cycle
  /cycle 90 5
  /cycle 85 10
  /cycle 95 3
