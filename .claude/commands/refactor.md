# Refactoring Agent

Safe refactoring and code modernization with systematic improvement guidance.

## Commands

### Suggest Refactoring Opportunities
```
/refactor analyze [target]
```
Identifies refactoring opportunities ranked by impact and effort.

**Examples:**
- `/refactor analyze Combat`
- `/refactor analyze ActionExecutor.cs`
- `/refactor analyze Enemy`

**Output includes:**
- Ranked refactoring opportunities (by ROI)
- Before/after code examples
- Benefits and risks of each refactoring
- Effort estimates
- Affected tests

### Apply Refactoring
```
/refactor apply [type] [target]
```
Applies specific refactoring type and creates implementation plan.

**Refactoring types:**
- `extract` - Extract method, class, or logic to new component
- `simplify` - Simplify complex conditionals or nested code
- `consolidate` - Consolidate duplicate or similar code
- `modernize` - Use modern C# features and patterns

**Examples:**
- `/refactor apply extract DamageCalculator`
- `/refactor apply simplify ActionSelector`
- `/refactor apply consolidate ValidationLogic`
- `/refactor apply modernize GameManager`

**Output includes:**
- Step-by-step refactoring plan
- Code changes to apply
- Testing strategy
- Verification checklist

### Remove Code Duplication
```
/refactor duplicates
```
Scans entire codebase for duplicated code and suggests consolidation.

**Output includes:**
- Duplication analysis by severity
- Groups of duplicated code
- Consolidation suggestions
- Impact assessment
- Effort estimates

### Simplify Complex Method
```
/refactor simplify [methodName]
```
Analyzes method complexity and suggests simplifications.

**Examples:**
- `/refactor simplify ProcessAction`
- `/refactor simplify CalculateDamage`
- `/refactor simplify SelectEnemy Action`

**Output includes:**
- Complexity metrics (cyclomatic, nesting, size)
- Specific simplification opportunities
- Before/after comparisons
- Refactoring roadmap
- Expected improvements

## Refactoring Types

### Extract (Most Common)
**Use when:** Code block is doing multiple things or used multiple times

**Before:**
```csharp
int damage = baseWeaponDamage + (strength * 2) - (enemyArmor / 3);
```

**After:**
```csharp
int damage = CalculateDamage(baseWeaponDamage, strength, enemyArmor);
```

**Benefits:**
- Reusable code
- Easier testing
- Clearer intent
- Easier to optimize

**Risks:** Very Low

### Simplify (Most Impactful for Readability)
**Use when:** Nested conditionals, complex logic, multiple branches

**Before:**
```csharp
if (condition1) {
    if (condition2) {
        if (condition3) {
            doSomething();
        }
    }
}
```

**After:**
```csharp
if (!condition1 || !condition2 || !condition3) return;
doSomething();
```

**Benefits:**
- Improved readability
- Reduced nesting
- Clearer control flow
- Easier debugging

**Risks:** Low

### Consolidate (Eliminates Duplication)
**Use when:** Similar methods or logic blocks exist in multiple places

**Before:**
```csharp
CalculatePhysicalDamage()
CalculateMagicalDamage()
CalculateStatusDamage()
```

**After:**
```csharp
CalculateDamage(DamageType type, ...)
```

**Benefits:**
- Eliminates duplication
- Single source of truth
- Easier maintenance
- Reduces bugs

**Risks:** Medium (requires careful merging)

### Modernize (Improves Code Style)
**Use when:** Using older C# patterns, verbose code

**Before:**
```csharp
if (player != null && player.Health > 0) {
```

**After:**
```csharp
if (player?.Health > 0) {
```

**Benefits:**
- Concise code
- Better null safety
- Modern idioms
- More readable

**Risks:** Very Low

## Workflow

### 1. Analyze for Opportunities
```
/refactor analyze Combat
```
See what can be improved and the ROI.

### 2. Choose Highest ROI Opportunity
Review the ranked list - start with high impact, low effort items.

### 3. Apply the Refactoring
```
/refactor apply [type] [target]
```
Get detailed plan on what to do.

### 4. Implement the Changes
Follow the step-by-step plan from the agent.

### 5. Test
Run affected tests to ensure no regressions.

### 6. Commit
Create a commit with the refactoring.

## Best Practices

1. **One refactoring at a time** - Keep commits focused
2. **Extract before consolidate** - Extract first, then consolidate similar
3. **Test thoroughly** - Refactoring should not change behavior
4. **Simplify last** - Extract and consolidate first, simplify last
5. **Start with high ROI** - Focus on items with high impact, low effort
6. **Document why** - Explain why refactoring was beneficial

## Safety Guidelines

- **Always run tests after refactoring** - Verify behavior unchanged
- **Use version control** - Easy to revert if needed
- **Pair program** - Have someone review the changes
- **Small increments** - Refactor slowly, incrementally
- **Profile after** - Verify no performance regression

## Complexity Metrics

The agent uses these metrics to prioritize:

- **Cyclomatic Complexity** - Number of decision points (goal: < 5)
- **Lines of Code** - Method size (goal: < 30 lines)
- **Nesting Depth** - Indentation levels (goal: < 3)
- **Parameter Count** - Number of parameters (goal: < 4)
- **Variable Count** - Local variables (goal: < 5)

High numbers in any metric suggest refactoring opportunity.

## ROI Calculation

```
ROI = ImpactScore / (EffortScore + 1)
```

Higher ROI = better refactoring choice. Focus on refactorings with:
- High impact (80+)
- Low effort (20-40)
- Low risk (<0.2)

## Benefits by Type

| Type | Readability | Maintainability | Performance | Testability |
|------|-------------|-----------------|-------------|------------|
| Extract | ++ | ++ | - | ++ |
| Simplify | +++ | ++ | - | + |
| Consolidate | + | ++ | + | + |
| Modernize | + | + | - | - |

## Common Refactoring Patterns

### Guard Clauses
Replace nested ifs with early returns for clearer flow.

### Parameter Object
Group related parameters into an object to reduce parameter count.

### Method Extraction
Extract complex logic into helper methods for reusability.

### Strategy Pattern
Replace switch statements with strategy objects for extensibility.

### Null-Coalescing
Use `?.` and `??` operators for cleaner null handling.

## Troubleshooting

**Q: Refactoring breaks tests**
A: Verify tests are testing behavior, not implementation. Adjust tests if needed.

**Q: Performance regressed after refactoring**
A: Profile the refactored code. Extract may have small cost, consolidate usually helps.

**Q: How do I know it's safe?**
A: Run full test suite. If all tests pass, refactoring is safe.

**Q: Should I refactor before or after feature?**
A: After feature is working, then refactor. Don't premature refactor.
