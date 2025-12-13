# Test Engineer Agent

Automated test generation, execution, and coverage optimization.

## Commands

### Generate Tests for Feature
```
/test-engineer generate [feature]
```
Generates unit tests, edge case tests, and integration tests for a feature.

**Examples:**
- `/test-engineer generate CombatSystem`
- `/test-engineer generate ActionExecutor`
- `/test-engineer generate DamageCalculator`

**Output includes:**
- Generated unit test names
- Edge case test scenarios
- Integration test descriptions
- Estimated code coverage
- Coverage gaps by system
- Recommendations for critical tests

### Run Tests
```
/test-engineer run [category]
```
Executes test suites and reports results.

**Examples:**
- `/test-engineer run Combat`
- `/test-engineer run Character`
- `/test-engineer run All`

**Output includes:**
- Test count
- Pass/fail rates
- Execution time
- Coverage percentage
- Failed test details

### Analyze Coverage
```
/test-engineer coverage
```
Identifies code coverage gaps and priorities by system.

**Use when:**
- Identifying untested code paths
- Prioritizing which tests to write first
- Assessing overall test coverage

**Output includes:**
- Coverage by system
- Untested methods
- Critical gaps
- Coverage improvement recommendations

### Generate Integration Tests
```
/test-engineer integration [system]
```
Creates integration test scenarios for a system.

**Examples:**
- `/test-engineer integration Combat`
- `/test-engineer integration Enemy`
- `/test-engineer integration Game`

**Output includes:**
- Integration test descriptions
- System interaction scenarios
- Data flow tests
- State management tests

## Test Generation Strategy

### Unit Tests
- Test each public method
- Test with valid inputs
- Test return values
- Test state changes

### Edge Case Tests
- Null inputs
- Empty collections
- Boundary values (min/max)
- Invalid types
- Zero/negative numbers

### Integration Tests
- System A → System B interactions
- Data flow between systems
- State consistency across systems
- Error propagation

## Coverage Targets

- **Critical paths:** 100% coverage
- **Core systems:** 90%+ coverage
- **Utilities:** 80%+ coverage
- **Overall:** 85%+ coverage

## Development Workflow

1. **Generate tests** for your new feature
   ```
   /test-engineer generate MyNewFeature
   ```

2. **Run the generated tests** to verify they work
   ```
   /test-engineer run MyNewFeature
   ```

3. **Check coverage gaps**
   ```
   /test-engineer coverage
   ```

4. **Generate integration tests** to test system interactions
   ```
   /test-engineer integration CombatSystem
   ```

5. **Commit** your changes once all tests pass

## Tips

1. **Generate early** - Tests help design the code
2. **Edge cases matter** - They catch real bugs
3. **Integration tests** - Verify systems work together
4. **Run often** - Catch regressions immediately
5. **Aim for 85%+** - Coverage indicates confidence

## Benefits

- **3× faster** test creation (10 min → 3 min)
- **Fewer regressions** with comprehensive tests
- **Documentation** - Tests show expected behavior
- **Confidence** - High coverage means safer changes
- **Quality** - Better tested code is better code

## Troubleshooting

**Q: Generated tests are too simplistic?**
A: They're a starting point. Add more complex scenarios for critical code.

**Q: Coverage is low?**
A: Run `/test-engineer coverage` to identify gaps, then generate tests for those areas.

**Q: Tests take too long to run?**
A: Use `/test-engineer run [specific-category]` to run subsets faster.

**Q: Want to see test examples?**
A: Check the generated test files for patterns and examples.
