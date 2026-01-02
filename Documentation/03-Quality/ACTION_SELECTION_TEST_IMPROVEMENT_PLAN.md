# Action Selection Test Improvement Plan

## Problem Statement

A bug was discovered where base roll 12 with bonuses was incorrectly triggering combo actions. The existing tests did not catch this bug because:
1. Dice is not testable (static class with private Random)
2. Tests are too generic (only check methods don't crash)
3. Missing critical tests for base roll vs total roll distinction
4. No boundary testing for specific roll values

## Objectives

1. Make Dice testable to allow controlled roll values in tests
2. Add comprehensive tests for roll-based action selection
3. Verify base roll vs total roll distinction
4. Test boundary conditions (rolls 12, 13, 14)
5. Ensure tests catch similar bugs in the future

## Implementation Plan

### Phase 1: Make Dice Testable

**Task 1.1: Add Test Mode to Dice Class**
- [ ] Add `_testRollValue` static field (nullable int)
- [ ] Add `_testModeEnabled` static flag (bool)
- [ ] Add `SetTestRoll(int? rollValue)` public method
- [ ] Add `ClearTestRoll()` public method
- [ ] Add `IsTestModeEnabled` public property
- [ ] Modify `Roll()` method to check test mode first
- [ ] Ensure test mode only affects single die rolls (1d20)
- [ ] Add XML documentation for test methods
- [ ] Verify existing code still works (no breaking changes)

**File**: `Code/Utils/Dice.cs`

**Acceptance Criteria**:
- Test mode can be enabled/disabled
- Setting test roll returns that value for 1d20 rolls
- Clearing test roll returns to normal random behavior
- No impact on production code when test mode is off

---

### Phase 2: Create Comprehensive Tests

**Task 2.1: Create ActionSelectorRollBasedTests.cs**
- [ ] Create new test file in `Code/Tests/Unit/Actions/`
- [ ] Add test infrastructure (counters, summary)
- [ ] Implement `RunAllTests()` method
- [ ] Add helper method `CreateTestCharacterWithBothActionTypes()`
- [ ] Ensure test cleanup (clear test mode after tests)

**Task 2.2: Implement Base Roll Threshold Tests**
- [ ] Test roll 12 → should select normal action
- [ ] Test roll 13 → should select normal action
- [ ] Test roll 14 → should select combo action
- [ ] Test roll 15 → should select combo action
- [ ] Verify `IsComboAction` flag is correct for each

**Task 2.3: Implement Base Roll vs Total Roll Tests (Critical)**
- [ ] Test roll 12 with bonuses → should still be normal action
- [ ] Test roll 13 with bonuses → should still be normal action
- [ ] Test roll 14 with bonuses → should be combo action
- [ ] Verify bonuses don't affect action type selection
- [ ] Add character with high Intelligence (roll bonus) for testing

**Task 2.4: Implement Natural 20 Tests**
- [ ] Test roll 20 → should always select combo action
- [ ] Verify natural 20 bypasses threshold check

**Task 2.5: Implement Edge Case Tests**
- [ ] Test roll 1 → should not be combo (or null if fail)
- [ ] Test roll 6 → should be normal (minimum normal range)
- [ ] Test roll 19 → should be combo (below natural 20)

**Task 2.6: Implement Boundary Tests**
- [ ] Test roll 13 (last normal) → verify normal action
- [ ] Test roll 14 (first combo) → verify combo action
- [ ] Verify clear boundary between normal and combo

**File**: `Code/Tests/Unit/Actions/ActionSelectorRollBasedTests.cs`

**Acceptance Criteria**:
- All tests use `Dice.SetTestRoll()` to control roll values
- Tests verify both action selection and `IsComboAction` flag
- Tests cover the specific bug scenario (roll 12 with bonuses)
- Tests are isolated (clear test mode between tests)

---

### Phase 3: Integrate Tests into Test Suite

**Task 3.1: Add to ActionsSystemTestRunner**
- [ ] Open `Code/Tests/Runners/ActionsSystemTestRunner.cs`
- [ ] Add `ActionSelectorRollBasedTests.RunAllTests()` call
- [ ] Place after `ActionSelectorTests.RunAllTests()`
- [ ] Ensure proper Console.WriteLine spacing

**Task 3.2: Verify Test Execution**
- [ ] Run ActionsSystemTestRunner manually
- [ ] Verify all new tests execute
- [ ] Verify tests pass with current (fixed) code
- [ ] Verify test cleanup works (no test mode leakage)

**File**: `Code/Tests/Runners/ActionsSystemTestRunner.cs`

**Acceptance Criteria**:
- New tests run as part of Actions system test suite
- Tests execute without errors
- Test mode is properly cleaned up

---

### Phase 4: Documentation

**Task 4.1: Create Test Evaluation Document**
- [ ] Document why bug wasn't caught
- [ ] Document test gaps identified
- [ ] Document root cause analysis
- [ ] Document recommended solutions
- [ ] Include code examples of proper tests

**Task 4.2: Update Testing Strategy Documentation**
- [ ] Add section on testable Dice pattern
- [ ] Document test mode usage
- [ ] Add examples of roll-based testing
- [ ] Update test coverage requirements

**Files**: 
- `Documentation/03-Quality/ACTION_SELECTION_TEST_EVALUATION.md`
- `Documentation/03-Quality/TESTING_STRATEGY.md`

**Acceptance Criteria**:
- Documentation explains the problem and solution
- Future developers understand how to test roll-based logic
- Test patterns are documented for reuse

---

## Testing Strategy

### Test the Tests (Meta-Testing)

**Before Fix Verification**:
- Temporarily revert the bug fix in `ActionSelector.cs`
- Run the new tests
- Verify tests FAIL (catch the bug)
- Re-apply the fix
- Verify tests PASS

**Test Coverage Verification**:
- Ensure tests cover:
  - ✅ Base roll thresholds (12, 13, 14, 15)
  - ✅ Base roll vs total roll distinction
  - ✅ Natural 20 behavior
  - ✅ Edge cases (1, 6, 19, 20)
  - ✅ Boundary conditions (13 vs 14)

### Regression Testing

- Run full test suite after changes
- Verify no existing tests break
- Verify Dice test mode doesn't affect production code
- Verify test cleanup prevents mode leakage

---

## Success Criteria

1. ✅ Dice is testable (can set specific roll values)
2. ✅ Comprehensive tests exist for roll-based action selection
3. ✅ Tests verify base roll vs total roll distinction
4. ✅ Tests would catch the original bug
5. ✅ Tests are integrated into test suite
6. ✅ Documentation explains the problem and solution
7. ✅ No regressions in existing functionality

---

## Implementation Notes

### Dice Test Mode Design

**Why Test Mode Flag Instead of Dependency Injection?**
- Simpler implementation (no breaking changes)
- Minimal code changes required
- Easy to use in tests
- Can be extended later if needed

**Test Mode Behavior**:
- Only affects `Roll(1, 20)` calls (single die)
- Multiple dice rolls use test value × number of dice
- Test mode persists until explicitly cleared
- Production code unaffected when test mode is off

### Test Helper Pattern

```csharp
// Setup
Dice.SetTestRoll(12);
ActionSelector.ClearStoredRolls();

// Execute
var action = ActionSelector.SelectActionBasedOnRoll(character);

// Verify
Assert.IsFalse(action.IsComboAction, "Roll 12 should be normal");

// Cleanup
Dice.ClearTestRoll();
```

---

## Timeline Estimate

- **Phase 1** (Make Dice Testable): 30 minutes
- **Phase 2** (Create Tests): 1-2 hours
- **Phase 3** (Integration): 15 minutes
- **Phase 4** (Documentation): 30 minutes

**Total Estimated Time**: 2-3 hours

---

## Risk Assessment

**Low Risk**:
- Dice test mode is opt-in (doesn't affect production)
- Tests are isolated and don't affect existing code
- Changes are additive (no breaking changes)

**Mitigation**:
- Test mode only active during test execution
- Always clear test mode after tests
- Verify existing tests still pass
- Run full test suite before committing

---

## Future Improvements

1. **Consider Dependency Injection**: If more testability needed, consider IDiceRoller interface
2. **Expand Test Coverage**: Add tests for enemy action selection
3. **Test Other Roll-Based Systems**: Apply pattern to other systems using Dice
4. **Automated Test Generation**: Generate tests for all roll thresholds
5. **Performance Testing**: Verify test mode doesn't impact performance

---

## References

- Original Bug: Base roll 12 with bonuses triggering combo actions
- Fixed In: `Code/Actions/ActionSelector.cs` (uses base roll only for threshold)
- Related Tests: `Code/Tests/Unit/Actions/ActionSelectorTests.cs`
- Related Documentation: `Documentation/03-Quality/TESTING_STRATEGY.md`
