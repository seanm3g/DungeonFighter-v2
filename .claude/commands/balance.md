Run automated balance tuning to reach target metrics.

Usage: /balance [target-winrate] [max-variance]
- target-winrate: Target win rate percentage (default: 90)
- max-variance: Maximize enemy variance (true/false, default: true)

This command launches the Balance Tuner Agent which:
1. Analyzes current balance state
2. Suggests targeted adjustments based on metrics
3. Tests adjustments with what-if scenarios
4. Applies high-confidence changes
5. Iterates until target is reached
6. Verifies no regressions occurred
7. Saves successful configuration

The agent will:
- Skip weapon-type specific adjustments (per your feedback)
- Focus on global multipliers and archetypes
- Prioritize enemy variance to make matchups unique
- Run tests between iterations to verify impact

Examples:
  /balance
  /balance 90 true
  /balance 85 false
  /balance 95 true
