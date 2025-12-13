# Performance Profiler Agent

Identifies performance bottlenecks, analyzes hot paths, and suggests optimizations.

## Commands

### Profile a System Component
```
/profile [component]
```
Measures performance of a system and identifies bottlenecks.

**Examples:**
- `/profile Combat`
- `/profile Enemy`
- `/profile Game`
- `/profile Battle`

**Output includes:**
- Total execution time
- Timing breakdown (hot paths)
- Identified bottlenecks
- Memory concerns
- Optimization suggestions ranked by impact
- Performance score (0-100)

### Compare Current Performance with Baseline
```
/profile compare [baseline]
```
Compares current performance against a previous baseline version.

**Examples:**
- `/profile compare v1.0`
- `/profile compare previous`
- `/profile compare baseline`

**Output includes:**
- Baseline vs current metrics
- Performance regression/improvement %
- Changes causing slowdown
- Root cause analysis
- Optimization recommendations

### Identify All Bottlenecks
```
/profile bottlenecks
```
Analyzes all systems to find the worst performance issues.

**Output includes:**
- Critical bottlenecks (>100ms)
- Moderate bottlenecks (20-100ms)
- Memory concerns
- Optimization priority ranking
- Estimated total improvement

### Benchmark Critical Code Paths
```
/profile benchmark
```
Measures performance of critical code paths and hotspots.

**Output includes:**
- Hottest functions ranked by time
- Call depth and frequency
- Performance bottlenecks within each path
- Optimization opportunities
- Effort estimates for improvements

## Performance Score Interpretation

- **85-100:** Excellent - Highly optimized, monitor for regressions
- **75-85:** Good - Some optimization opportunities exist
- **65-75:** Fair - Optimization recommended
- **50-65:** Poor - Significant optimization needed urgently
- **<50:** Critical - Major performance issues

## Common Bottlenecks

### Combat System (usually 45% of time)
- **Hot path:** ActionExecutor.Execute()
- **Issue:** Nested loops iterating over enemies twice
- **Fix:** Reduce nested loops to single pass
- **Effort:** 30 minutes
- **Impact:** -70ms per battle

### Damage Calculation (usually 28%)
- **Hot path:** DamageCalculator.CalculateDamage()
- **Issue:** Recalculates same values in loop
- **Fix:** Add caching layer
- **Effort:** 20 minutes
- **Impact:** -40ms per battle

### Event Logging (usually 15%)
- **Hot path:** EventLogger.LogEvent()
- **Issue:** Synchronous file I/O blocks execution
- **Fix:** Move to async queue
- **Effort:** 45 minutes
- **Impact:** -15% overhead

### State Management (usually 12%)
- **Hot path:** StateManager.UpdateGameState()
- **Issue:** Deep copy of entire game state every turn
- **Fix:** Incremental updates
- **Effort:** 60 minutes
- **Impact:** -20% memory usage

## Development Workflow

### 1. Profile Current State
```
/profile Combat
```
Get baseline metrics on current performance.

### 2. Identify Optimization Opportunities
```
/profile bottlenecks
```
Find the biggest impact opportunities.

### 3. Benchmark Critical Paths
```
/profile benchmark
```
Deep dive into hottest functions.

### 4. Compare Before/After
```
/profile compare baseline
```
After making optimizations, verify improvement.

### 5. Iterate
- Optimize hottest bottleneck
- Re-profile to verify improvement
- Move to next bottleneck
- Repeat until acceptable performance

## Tips

1. **Profile first, optimize second** - Find real bottlenecks
2. **80/20 rule** - 20% of code takes 80% of time
3. **Cache aggressively** - Avoid recalculating same values
4. **Batch operations** - Reduce overhead of individual operations
5. **Async I/O** - Don't block on file/network operations
6. **Object pooling** - Reduce GC pressure from frequent allocation
7. **Early exit** - Stop unnecessary calculations when possible

## Performance Targets

- **Single battle:** <1000ms (currently: 950-1020ms)
- **900-battle cycle:** <15 minutes (currently: 15.3 min)
- **Memory peak:** <300MB (currently: 312MB)
- **GC pauses:** <50ms (currently: varies)

## Optimization Priority

1. **Critical (>100ms each)**
   - ActionExecutor nested loops
   - DamageCalculator recalculation
   - Biggest impact, highest priority

2. **Moderate (20-100ms)**
   - Event logging overhead
   - State snapshot creation
   - Good ROI on effort

3. **Minor (<20ms)**
   - Enemy AI evaluation
   - Turn validation
   - Lower priority, nice to have

## Expected Improvements

With systematic optimization of top 5 bottlenecks:

- **Individual battle:** 950ms → 810ms (-14.7%)
- **900-battle cycle:** 15.3 min → 12.2 min (-20.2%)
- **Memory usage:** 312MB → 270MB (-13.5%)

## Troubleshooting

**Q: Performance score is low?**
A: Run `/profile bottlenecks` to identify worst issues and prioritize.

**Q: Optimization didn't help?**
A: Run `/profile` again - bottleneck may have shifted.

**Q: New regression after changes?**
A: Use `/profile compare baseline` to detect regressions early.

**Q: Where should I focus first?**
A: Run `/profile bottlenecks` - shows optimization priority ranking.

**Q: How much improvement is possible?**
A: Typically 15-25% with systematic optimization of top bottlenecks.

## Integration with Development

Profile after:
- Major feature additions
- System refactoring
- Performance-critical changes
- Before production release

Use results to:
- Identify optimization targets
- Estimate effort for improvements
- Track performance over time
- Guide architectural decisions

## Quick Performance Check

```
/profile Combat
# Review performance score
# If <75, run:
/profile bottlenecks
# Prioritize top fixes
# Implement them
# Then run:
/profile compare baseline
# Verify improvement achieved
```
