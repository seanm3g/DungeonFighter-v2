Run comprehensive balance testing and verification.

Usage: /test [mode]
- mode: 'full' (all tests), 'quick' (core metrics only), 'regression' (compare to baseline)
- Default: full

This command launches the Tester Agent which:
1. Runs battle simulation (900 battles across 36 weapon-enemy combinations)
2. Validates balance against constraints
3. Analyzes fun moments and engagement
4. Compares results with baseline if available
5. Generates detailed test report

Examples:
  /test
  /test full
  /test quick
  /test regression
