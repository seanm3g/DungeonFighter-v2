# Bug Investigator Agent

Systematic bug reproduction, isolation, and diagnosis with fix suggestions.

## Commands

### Investigate Bug from Description
```
/debug issue [description]
```
Analyzes bug description and identifies potential root causes.

**Examples:**
- `/debug issue "Battle sometimes crashes on turn 5"`
- `/debug issue "Dagger does too little damage"`
- `/debug issue "Enemy AI doesn't make smart decisions"`

**Output includes:**
- Affected systems
- Potential source files
- Root cause hypothesis with confidence %
- Reproduction steps
- Suggested fixes ranked by impact
- Risk assessment

### Reproduce Bug with Steps
```
/debug repro [steps]
```
Executes reproduction steps and captures error state for analysis.

**Examples:**
- `/debug repro "Start battle, attack 5 times, bug triggers"`
- `/debug repro "Select Mace, fight Berserker, crash on turn 3"`

**Output includes:**
- Step-by-step execution
- Captured error state
- Stack trace location
- Minimal reproduction case
- Frequency (how reproducible)

### Isolate Root Cause in System
```
/debug isolate [system]
```
Tests hypotheses systematically to pinpoint exact root cause.

**Examples:**
- `/debug isolate Combat`
- `/debug isolate Enemy`
- `/debug isolate Game`

**Output includes:**
- Hypothesis testing results
- Root cause identified
- Confidence level
- Affected code paths
- Evidence

### Suggest Fixes for Bug
```
/debug suggest [bugid]
```
Generates ranked fix suggestions with effort estimates and risk assessment.

**Examples:**
- `/debug suggest BUG-001`
- `/debug suggest "null-reference-error"`

**Output includes:**
- Priority 1-3 fixes
- Location in code
- Confidence level
- Effort estimate (minutes)
- Risk assessment
- Testing recommendations

## Bug Analysis Process

### Phase 1: Investigation
- Analyze bug description
- Identify affected systems
- List potential source files
- Generate reproduction steps

### Phase 2: Reproduction
- Execute reproduction steps
- Capture error state
- Find minimal repro case
- Confirm frequency

### Phase 3: Isolation
- Test hypotheses
- Narrow down root cause
- Trace code execution path
- Identify exact location

### Phase 4: Fixing
- Suggest fixes ranked by impact
- Estimate effort for each fix
- Assess risk level
- Recommend regression tests

## Confidence Levels

- **95%+:** Root cause identified with certainty
- **85-95%:** High confidence, likely solution
- **70-85%:** Good confidence, probable cause
- **60-70%:** Possible cause, needs verification
- **<60%:** Speculative, needs investigation

## Risk Assessment

- **Very Low:** Simple null checks, easy validation
- **Low:** Straightforward refactoring, well-tested paths
- **Medium:** Logic changes, affects multiple systems
- **High:** Core system changes, potential regressions
- **Very High:** Fundamental architecture changes

## Development Workflow

1. **Report the bug** with clear description
   ```
   /debug issue "Describe what's wrong"
   ```

2. **Reproduce the bug** with exact steps
   ```
   /debug repro "Step 1, Step 2, Step 3"
   ```

3. **Isolate the root cause** in the affected system
   ```
   /debug isolate SystemName
   ```

4. **Get fix suggestions** ranked by impact
   ```
   /debug suggest BUG-001
   ```

5. **Implement the fix** using Priority 1 suggestion
6. **Add regression test** from Test Engineer Agent
7. **Verify** the fix works and doesn't break other things

## Quick Diagnosis Pattern

Fastest way to diagnose and fix a bug:

```
/debug issue "Crash on level 3 with Sword"
# Review the hypothesis and suggestions
/debug repro "Start game, select Sword, go to level 3"
# Confirm exact reproduction
/debug isolate Combat
# Pinpoint root cause
/debug suggest BUG-LEVEL3-SWORD
# Get ranked fixes
```

Total time: ~5-10 minutes vs. ~60 minutes manual debugging

## Tips

1. **Be specific** in bug descriptions - helps identify root cause
2. **Provide reproduction steps** - faster isolation
3. **Try Priority 1 first** - usually best risk/effort ratio
4. **Add regression test** - prevents bug from returning
5. **Test thoroughly** - ensure fix doesn't break other things

## Common Bug Types

### Null Reference Errors
- Confidence: 85%
- Priority: 1 - Add null checks
- Effort: 5-10 minutes

### Logic Errors
- Confidence: 65%
- Priority: 2 - Review logic, add tests
- Effort: 20-30 minutes

### Performance Issues
- Confidence: 70%
- Priority: 2 - Profile, optimize hot path
- Effort: 30-45 minutes

### State Management Issues
- Confidence: 80%
- Priority: 1 - Fix state initialization/updates
- Effort: 15-25 minutes

### Integration Issues
- Confidence: 75%
- Priority: 2 - Fix system communication
- Effort: 25-40 minutes

## Expected Improvements

- **Bug diagnosis speed:** 60 min → 15 min (4× faster)
- **Regression prevention:** Systematic fix suggestions
- **Code quality:** Better tested with regression tests
- **Team confidence:** Less uncertainty in fixes

## Troubleshooting

**Q: Confidence level is low?**
A: Run reproduction steps to gather more data, then isolate again.

**Q: Multiple possible causes?**
A: Test each hypothesis systematically with `/debug isolate`.

**Q: Fix causes new bugs?**
A: Priority-ranked suggestions account for risk - choose lower-risk option.

**Q: How long does diagnosis take?**
A: Typically 5-15 minutes depending on bug complexity.
