# Fun Moments Tracking System

## Overview

The Fun Moments Tracking System monitors gameplay indicators that suggest engaging, dynamic combat. This data is used to understand which weapons, classes, and configurations create the most compelling gameplay experiences.

## Fun Indicators Tracked

### 1. **Big Damage Spikes**
- **What**: Damage significantly above average (50%+ above average)
- **Why it's fun**: Big numbers feel satisfying and create memorable moments
- **Intensity**: Based on how much above average (up to 1.0)

### 2. **Health Lead Changes**
- **What**: Momentum shifts when health advantage changes hands
- **Why it's fun**: Creates tension and dynamic back-and-forth gameplay
- **Intensity**: 0.6 (moderate-high)

### 3. **Close Calls**
- **What**: Health drops below 10% but then recovers above 20%
- **Why it's fun**: Creates tension and relief - "I almost died but survived!"
- **Intensity**: 0.8 (high)

### 4. **Comebacks**
- **What**: Win after dropping to low health (<25%)
- **Why it's fun**: Epic recovery stories - "I was almost dead but came back to win!"
- **Intensity**: Based on how low health dropped (higher intensity for lower health)

### 5. **Dominant Streaks**
- **What**: 3+ consecutive successful actions
- **Why it's fun**: Feeling powerful and in control
- **Intensity**: 0.6-0.9 (increases with streak length)

### 6. **Action Variety**
- **What**: Switching between different actions (not just BASIC ATTACK)
- **Why it's fun**: Keeps gameplay fresh and requires decision-making
- **Intensity**: 0.3 (moderate)

### 7. **Turn Variance**
- **What**: Variance in damage per turn (optimal: 30-50% of average)
- **Why it's fun**: Some unpredictability keeps things interesting, but too much is frustrating
- **Score**: Calculated as standard deviation of damage per turn

### 8. **Critical Sequences**
- **What**: 2+ critical hits in a row
- **Why it's fun**: Lucky streaks feel exciting
- **Intensity**: 0.8-1.0 (increases with sequence length)

### 9. **Combo Chains**
- **What**: Extended combo sequences (3+ steps)
- **Why it's fun**: Rewards skill and creates satisfying sequences
- **Intensity**: 0.5-1.0 (increases with combo length)

### 10. **Health Swings**
- **What**: Large health percentage changes in one turn (>15%)
- **Why it's fun**: Dramatic moments that change the battle state quickly
- **Intensity**: Based on percentage change (up to 1.0)

## Fun Score Calculation

The overall **Fun Score** (0-100) is calculated from:

- **30 points**: Number of fun moments (up to 30)
- **20 points**: Average intensity of moments
- **15 points**: Action variety score (entropy-based)
- **15 points**: Turn variance (optimal range gets full points)
- **20 points**: Special moments (health lead changes, comebacks, close calls)

**Interpretation:**
- **70-100**: High engagement - gameplay is dynamic and compelling
- **50-69**: Medium engagement - good but could be improved
- **<50**: Low engagement - needs work to increase dynamic gameplay

## MCP Tools

### `analyze_fun_moments`
Analyzes fun moment data from the most recent battle simulation. Shows which weapons/classes create the most engaging gameplay.

**Returns:**
- Overall fun statistics
- Per-weapon analysis (fun score, action variety, health lead changes)
- Per-enemy analysis
- Top weapons by fun score
- Interpretation and recommendations

**Example:**
```
After running simulation:
analyze_fun_moments()
```

### `get_fun_moment_summary`
Gets detailed summary of fun moments with breakdown by type and intensity.

**Returns:**
- Overall statistics (average fun score, intensity, variety, variance)
- Moments by type (count and average per battle)
- Top 10 most intense moments
- Weapon comparison (fun scores by weapon)

**Example:**
```
get_fun_moment_summary()
```

## Using Fun Moment Data for Tuning

### Goal: Maximize Fun Score While Maintaining Balance

1. **Run Simulation**: `run_battle_simulation(100)`
2. **Check Fun Score**: `get_fun_moment_summary()`
3. **Analyze Results**: `analyze_fun_moments()`
4. **Identify Issues**:
   - Low action variety → Add more unique actions or encourage action switching
   - Low health lead changes → Adjust damage/health to create more back-and-forth
   - Low turn variance → Add more damage variation (criticals, status effects)
   - Few fun moments → Increase action variety, damage spikes, or combo potential
5. **Make Adjustments**: Use balance adjustment tools
6. **Re-test**: Compare fun scores before/after

### Example Workflow

```
1. Run baseline: run_battle_simulation(100)
   Result: Fun Score 45.2

2. Analyze: analyze_fun_moments()
   Found: Sword has highest fun score (62.3), Dagger lowest (38.1)
   Issue: Dagger has low action variety (0.2)

3. Adjust: Increase Dagger's unique action availability
   (via weapon configuration or class actions)

4. Re-test: run_battle_simulation(100)
   Result: Fun Score 58.7 - Improved!

5. Compare: get_fun_moment_summary()
   See improvement in action variety and overall fun moments
```

## Integration with Battle Simulations

Fun moment data is automatically collected during all battle simulations:

- **Included in**: `run_battle_simulation()` results
- **Per battle**: Each battle result includes `funMomentSummary`
- **Aggregated**: Overall statistics include fun moment averages
- **Per weapon**: Weapon statistics include average fun scores
- **Per enemy**: Enemy statistics include average fun scores

## Data Structure

### FunMomentSummary
```json
{
  "totalFunMoments": 15,
  "averageIntensity": 0.65,
  "funScore": 72.3,
  "momentsByType": {
    "BigDamageSpike": 3,
    "HealthLeadChange": 2,
    "CloseCall": 1,
    "ComboChain": 5,
    "CriticalSequence": 2,
    "ActionVariety": 2
  },
  "topMoments": [...],
  "turnVariance": 12.5,
  "actionVarietyScore": 0.75,
  "healthLeadChanges": 2,
  "comebacks": 0,
  "closeCalls": 1
}
```

## Best Practices

1. **Target Fun Score**: Aim for 60+ for engaging gameplay
2. **Action Variety**: Higher is better (0.5+ is good)
3. **Health Lead Changes**: 1-3 per battle is ideal (creates tension)
4. **Turn Variance**: 30-50% of average damage is optimal
5. **Balance vs Fun**: Don't sacrifice balance for fun, but prioritize configurations that have both

## Example Analysis

```
Weapon Comparison by Fun Score:
- Sword: 68.5 (High action variety, frequent health lead changes)
- Mace: 55.2 (Good damage spikes, moderate variety)
- Dagger: 42.1 (Low variety, few fun moments)
- Wand: 61.3 (Good combos, decent variety)

Recommendation: 
- Buff Dagger's action variety (add more unique actions)
- Consider increasing health lead change frequency globally
- Wand and Sword are performing well - use as benchmarks
```
