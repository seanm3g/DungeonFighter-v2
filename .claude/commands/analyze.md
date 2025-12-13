Deep-dive analysis of balance metrics and problem identification.

Usage: /analyze [focus]
- focus: 'balance' (overall), 'weapons' (weapon variance), 'enemies' (enemy matchups), 'engagement' (fun moments)
- Default: balance

This command launches the Analysis Agent which:
1. Runs detailed battle simulation with turn logs
2. Analyzes battle results and identifies issues
3. Provides balance quality score
4. Analyzes parameter sensitivity for high-impact changes
5. Generates problem report with recommendations
6. Suggests targeted fixes

The agent will:
- Identify which weapon-enemy combinations are problematic
- Find imbalanced archetypes or stats
- Detect which parameters have most impact
- Recommend specific adjustments with confidence levels
- Track metrics over time

Examples:
  /analyze
  /analyze balance
  /analyze weapons
  /analyze enemies
  /analyze engagement
