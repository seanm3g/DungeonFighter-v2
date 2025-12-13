# Code Review Agent

Automated code quality analysis and peer review.

## Commands

### Review a File
```
/review file [path]
```
Analyzes a specific C# file for quality issues.

**Examples:**
- `/review file Code/Combat/CombatManager.cs`
- `/review file Code/Actions/ActionExecutor.cs`
- `/review file Code/Game/Program.cs`

**Output includes:**
- Quality score (0-100)
- Style violations
- Complexity issues
- Security concerns
- Performance issues
- Documentation gaps
- Best practice violations
- Actionable recommendations

### Review Uncommitted Changes
```
/review diff
```
Analyzes uncommitted git changes and provides quality feedback.

**Use when:**
- Reviewing your own code before committing
- Quick quality check on recent changes
- Pre-commit validation

### Review Pull Request
```
/review pr
```
Reviews current branch against main branch.

**Use when:**
- Creating a pull request
- Comparing changes to main
- Full branch quality assessment

## Quality Score Interpretation

- **80-100:** Excellent - Ready for production
- **70-79:** Good - Minor issues to address
- **60-69:** Fair - Should review and improve
- **50-59:** Poor - Significant issues
- **<50:** Critical - Major refactoring needed

## Common Issues Detected

### Style Violations
- Trailing whitespace
- Inconsistent indentation (4-space standard)
- Line length exceeding 120 characters
- Naming convention issues

### Complexity Issues
- Deep nesting (>4 levels)
- High parameter counts (>5)
- Large methods (>50 lines)
- Cyclomatic complexity

### Security Concerns
- Hardcoded credentials
- SQL injection patterns
- Deprecated cryptography
- Unsafe string handling

### Performance Issues
- String concatenation in loops
- LINQ inefficiencies
- Excessive allocations
- Missing null checks

### Documentation
- Public methods without XML docs
- Missing type documentation
- Unclear variable names
- No usage examples

## Tips

1. **Fix style issues first** - They're quickest to resolve
2. **Focus on complexity** - Refactor large/complex methods
3. **Add tests** - Cover edge cases found during review
4. **Document public APIs** - Essential for maintainability
5. **Use recommendations** - They're ranked by impact

## Integration with Development Workflow

Review code:
```
/review file Code/NewFeature/MyClass.cs
```

Fix issues:
- Address style violations
- Refactor complex sections
- Add documentation
- Optimize performance

Verify quality:
```
/review file Code/NewFeature/MyClass.cs
```

Commit when quality score is 80+.
