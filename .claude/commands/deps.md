# Dependency Analyzer Agent

Analyze and optimize project dependencies, find vulnerabilities, and manage packages.

## Commands

### Analyze All Dependencies
```
/deps analyze
```
Comprehensive analysis of project dependency health.

**Output includes:**
- Complete dependency list (direct and transitive)
- Circular dependency detection
- Unused dependency identification
- Security vulnerability scan
- Outdated package detection
- High-risk dependency assessment
- Overall health score (0-100)

### Find Outdated Packages
```
/deps outdated
```
Identifies packages with available updates.

**Output includes:**
- Current versions
- Latest versions available
- Breaking change detection
- Update safety assessment
- Recommended update order

### Find Unused Dependencies
```
/deps unused
```
Detects dependencies that aren't being used in code.

**Output includes:**
- Unused NuGet packages
- Unused namespace imports
- Usage confidence levels
- Safe removal recommendations

### Check Security Vulnerabilities
```
/deps security
```
Scans dependencies for known security vulnerabilities.

**Output includes:**
- Vulnerability list with severity (Critical, High, Medium, Low)
- CVE identifiers
- Affected functionality
- Available patches
- Risk assessment
- Mitigation steps

## Dependency Health Scores

- **90-100:** Excellent - Very healthy dependencies
- **75-90:** Good - Minor improvements recommended
- **60-75:** Fair - Some cleanup needed
- **<60:** Poor - Significant work required

## Security Severity Levels

### Critical
- Requires immediate action
- High exploitability
- Severe impact if exploited
- Action: Update immediately

### High
- Should update soon
- Moderate-to-high exploitability
- Significant impact if exploited
- Action: Update within 1 week

### Medium
- Plan to update
- Low-to-moderate exploitability
- Moderate impact if exploited
- Action: Update within 1 month

### Low
- Monitor for fixes
- Low exploitability
- Minor impact if exploited
- Action: Update at next scheduled maintenance

## Common Issues

### Circular Dependencies
When Package A depends on Package B, and Package B depends on Package A.

**Impact:** Causes build issues and makes refactoring difficult
**Solution:** Extract shared code to third package

### Outdated Packages
Packages with available updates (major, minor, or patch).

**Risk levels:**
- **Patch updates:** Very safe, apply immediately
- **Minor updates:** Usually safe, test before applying
- **Major updates:** May have breaking changes, review carefully

### Unused Dependencies
Packages referenced but never used in code.

**Benefits of removing:**
- Smaller package size
- Fewer security vulnerabilities to monitor
- Cleaner dependency graph
- Faster builds

### Security Vulnerabilities
Known CVEs (Common Vulnerabilities and Exposures) in packages.

**Action:** Always update when patches available

## Workflow

### 1. Analyze Dependencies
```
/deps analyze
```
Get overall health assessment.

### 2. Address Security Issues
```
/deps security
```
Fix any critical or high vulnerabilities first.

### 3. Check for Outdated Packages
```
/deps outdated
```
Plan updates for minor versions and patches.

### 4. Find Unused Dependencies
```
/deps unused
```
Remove bloat from project.

### 5. Monitor for Changes
Run analysis regularly (weekly/monthly) to catch issues early.

## Best Practices

1. **Keep dependencies up to date** - Apply patch updates immediately
2. **Review security advisories** - Subscribe to NuGet security updates
3. **Remove unused dependencies** - Reduce surface area
4. **Monitor for breaking changes** - Test major version updates
5. **Document dependencies** - Explain why each dependency is needed
6. **Use dependency lock files** - Ensure reproducible builds

## Update Strategy

### Patch Updates (1.2.3 → 1.2.4)
- **Safety:** Very safe
- **Action:** Apply immediately
- **Testing:** Run unit tests

### Minor Updates (1.2.0 → 1.3.0)
- **Safety:** Usually safe
- **Action:** Apply monthly
- **Testing:** Run full test suite
- **Review:** Check release notes for deprecations

### Major Updates (1.0.0 → 2.0.0)
- **Safety:** May have breaking changes
- **Action:** Plan carefully
- **Testing:** Extensive testing required
- **Review:** Carefully review breaking changes
- **Timeline:** Quarterly or as needed

## Vulnerability Assessment

### For Each Vulnerability
1. **Identify:** Check if you're affected
2. **Evaluate:** Assess the risk for your use case
3. **Patch:** Update to fixed version if available
4. **Mitigate:** If no patch, implement workaround
5. **Monitor:** Track for permanent fix

### Risk Factors
- Does the vulnerability affect your code paths?
- Is the functionality you use affected?
- Can the vulnerability be exploited in your environment?
- What's the impact if exploited?

## Circular Dependency Resolution

### Identify the Cycle
```
Package A → Package B → Package C → Package A
```

### Extract Common Code
Create Package X with shared code:
- Package A depends on X
- Package B depends on X
- Package C depends on X
- No circular dependency

### Refactor to Dependency Injection
Use interfaces and dependency injection to break cycles.

## Dependency Downgrades

Sometimes you need to downgrade a dependency (e.g., due to compatibility).

**Process:**
1. Identify why downgrade is needed
2. Document the reason clearly
3. Test thoroughly with downgraded version
4. Plan to upgrade when blocking issue is resolved
5. Review quarterly for upgrade possibility

## Monitoring

Regular dependency health checks:

- **Weekly:** Security vulnerability scan
- **Monthly:** Check for outdated packages
- **Quarterly:** Full dependency analysis
- **Annually:** Architecture review of dependencies

## Tools and Resources

- **NuGet.org** - Package repository and vulnerability database
- **GitHub Security Alerts** - Notifications for vulnerable dependencies
- **Dependabot** - Automated dependency updates
- **Snyk** - Advanced vulnerability scanning

## Troubleshooting

**Q: A package has no update for a critical vulnerability**
A: Use workaround if available, consider alternative package

**Q: Updating breaks something**
A: Revert to previous version, check breaking changes, file issue with maintainer

**Q: Circular dependency detected**
A: Extract shared code or use dependency injection to break cycle

**Q: Performance degraded after update**
A: Profile the code, may need to revert and report issue

**Q: Package deprecated**
A: Plan migration to recommended replacement, test thoroughly
