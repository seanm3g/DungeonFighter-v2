# Action Selection Test Evaluation

## Issue Summary

A bug was discovered where a base roll of 12 was incorrectly triggering combo actions when bonuses pushed the total roll to 14+. This bug should have been caught by tests but wasn't. This document evaluates the testing coverage and identifies gaps.

## Bug Details

**Problem**: `ActionSelector.SelectActionBasedOnRoll()` was using `totalRoll` (baseRoll + bonuses) instead of `baseRoll` to determine combo vs normal attack selection.

**Expected Behavior**: 
- Base roll 1-13 → Normal attack (non-combo)
- Base roll 14-19 → Combo action
- Base roll 20 → Combo action (natural 20)

**Actual Behavior (Before Fix)**:
- Base roll 12 + bonuses (e.g., +2) = total 14 → Incorrectly triggered combo action

**Impact**: Players with roll bonuses could trigger combo actions with base rolls that should have been normal attacks.

## Current Test Coverage Analysis

### Existing Tests

#### 1. `ActionSelectorTests.cs`
**Status**: ⚠️ **Insufficient**

**What it tests**:
- Methods don't crash
- Methods return null or an action
- Basic null checks

**What it doesn't test**:
- ❌ Specific roll values (e.g., roll 12)
- ❌ Combo vs normal action selection
- ❌ Base roll vs total roll distinction
- ❌ Roll threshold boundaries (13 vs 14)
- ❌ Behavior with roll bonuses

**Code Example**:
```csharp
// Current test - only checks it doesn't crash
var selected2 = ActionSelector.SelectActionBasedOnRoll(character);
TestBase.AssertTrue(selected2 == null || selected2 != null,
    "SelectActionBasedOnRoll should return action or null",
    ref _testsRun, ref _testsPassed, ref _testsFailed);
```

#### 2. `ComboDiceRollTests.ActionSelection.cs`
**Status**: ⚠️ **Acknowledges Limitation But Doesn't Fix It**

**What it tests**:
- Basic action selection works
- Natural 20 selection

**What it doesn't test**:
- ❌ Specific roll values (acknowledges: "Would need to mock dice to test specific roll ranges")
- ❌ Roll 12 specifically
- ❌ Base roll vs total roll with bonuses
- ❌ Edge cases (roll 13 vs 14)

**Code Example**:
```csharp
// Test acknowledges it can't test specific rolls
TestBase.AssertTrue(action == null, "Action should not be selected for rolls 6-13", 
    ref _testsRun, ref _testsPassed, ref _testsFailed);
// Note: Would need to mock dice to test specific roll ranges
```

## Root Cause: Non-Testable Dice

### The Problem

`Dice.Roll()` is a static class with a private `Random` instance:

```csharp
public class Dice
{
    private static readonly Random _random = new Random();
    
    public static int Roll(int numberOfDice, int sides)
    {
        // Uses _random which can't be controlled
        return _random.Next(1, sides + 1);
    }
}
```

**Consequences**:
1. Cannot inject a mock Random for testing
2. Cannot control specific roll values
3. Tests must rely on probabilistic verification (run many times and check distribution)
4. Cannot test edge cases (e.g., exactly roll 12, exactly roll 14)
5. Cannot test the base roll vs total roll distinction

### Why This Matters

The bug was specifically about **base roll 12 with bonuses** triggering combos. Without the ability to:
- Set a specific base roll (12)
- Add specific bonuses (+2)
- Verify the result (should be normal attack, not combo)

The tests couldn't catch this bug.

## Test Gaps Identified

### Critical Gaps

1. **No Roll Value Control**
   - Cannot test specific roll values
   - Cannot test edge cases (12, 13, 14)
   - Cannot test boundary conditions

2. **No Base Roll vs Total Roll Testing**
   - Cannot verify that base roll (not total) determines action type
   - Cannot test that bonuses don't affect action selection

3. **No Threshold Boundary Testing**
   - Cannot verify roll 13 → normal attack
   - Cannot verify roll 14 → combo action
   - Cannot verify roll 12 with +2 bonus → still normal attack

4. **No Action Type Verification**
   - Tests don't verify if selected action is combo or normal
   - Tests don't verify `IsComboAction` flag

5. **No Roll Bonus Interaction Testing**
   - Cannot test how bonuses affect (or don't affect) action selection
   - Cannot test intelligence/equipment bonuses

## Recommended Solutions

### Solution 1: Make Dice Testable (Recommended)

**Approach**: Add a testable interface or allow dependency injection

**Option A: Test Mode Flag**
```csharp
public class Dice
{
    private static readonly Random _random = new Random();
    private static int? _testRoll = null; // For testing
    
    public static void SetTestRoll(int roll)
    {
        _testRoll = roll;
    }
    
    public static void ClearTestRoll()
    {
        _testRoll = null;
    }
    
    public static int Roll(int numberOfDice, int sides)
    {
        if (_testRoll.HasValue)
        {
            return _testRoll.Value;
        }
        // Normal random behavior
        return _random.Next(1, sides + 1);
    }
}
```

**Option B: Dependency Injection (Better for long-term)**
```csharp
public interface IDiceRoller
{
    int Roll(int numberOfDice, int sides);
}

public class Dice : IDiceRoller
{
    private readonly Random _random;
    
    public Dice(Random random = null)
    {
        _random = random ?? new Random();
    }
    
    public int Roll(int numberOfDice, int sides)
    {
        return _random.Next(1, sides + 1);
    }
}

// Static wrapper for backward compatibility
public static class DiceStatic
{
    private static IDiceRoller _instance = new Dice();
    
    public static void SetInstance(IDiceRoller instance)
    {
        _instance = instance;
    }
    
    public static int Roll(int numberOfDice, int sides)
    {
        return _instance.Roll(numberOfDice, sides);
    }
}
```

### Solution 2: Test ActionSelector Directly with Controlled Rolls

**Approach**: Modify `ActionSelector` to accept an optional roll parameter for testing

```csharp
public static Action? SelectActionBasedOnRoll(Actor source, int? testRoll = null)
{
    int baseRoll = testRoll ?? Dice.Roll(1, 20);
    // ... rest of logic
}
```

### Solution 3: Use Reflection to Mock Random (Not Recommended)

**Approach**: Use reflection to replace the Random instance in Dice

**Pros**: No code changes needed
**Cons**: Fragile, breaks encapsulation, hard to maintain

## Recommended Test Cases

Once dice is testable, add these tests:

### 1. Base Roll Threshold Tests

```csharp
[Test]
public void TestRoll12_ShouldSelectNormalAction()
{
    Dice.SetTestRoll(12);
    var character = CreateTestCharacterWithBothActionTypes();
    
    var action = ActionSelector.SelectActionBasedOnRoll(character);
    
    Assert.IsNotNull(action);
    Assert.IsFalse(action.IsComboAction, "Roll 12 should select normal action");
}

[Test]
public void TestRoll13_ShouldSelectNormalAction()
{
    Dice.SetTestRoll(13);
    var character = CreateTestCharacterWithBothActionTypes();
    
    var action = ActionSelector.SelectActionBasedOnRoll(character);
    
    Assert.IsNotNull(action);
    Assert.IsFalse(action.IsComboAction, "Roll 13 should select normal action");
}

[Test]
public void TestRoll14_ShouldSelectComboAction()
{
    Dice.SetTestRoll(14);
    var character = CreateTestCharacterWithBothActionTypes();
    
    var action = ActionSelector.SelectActionBasedOnRoll(character);
    
    Assert.IsNotNull(action);
    Assert.IsTrue(action.IsComboAction, "Roll 14 should select combo action");
}
```

### 2. Base Roll vs Total Roll Tests

```csharp
[Test]
public void TestRoll12_WithBonus2_ShouldStillBeNormalAction()
{
    Dice.SetTestRoll(12);
    var character = CreateTestCharacterWithBothActionTypes();
    character.Stats.Intelligence = 20; // Adds +2 roll bonus
    
    var action = ActionSelector.SelectActionBasedOnRoll(character);
    
    Assert.IsNotNull(action);
    Assert.IsFalse(action.IsComboAction, 
        "Base roll 12 with bonuses should still be normal action (base roll determines type)");
}

[Test]
public void TestRoll13_WithBonus1_ShouldStillBeNormalAction()
{
    Dice.SetTestRoll(13);
    var character = CreateTestCharacterWithBothActionTypes();
    character.Stats.Intelligence = 10; // Adds +1 roll bonus
    
    var action = ActionSelector.SelectActionBasedOnRoll(character);
    
    Assert.IsNotNull(action);
    Assert.IsFalse(action.IsComboAction, 
        "Base roll 13 with bonuses should still be normal action");
}
```

### 3. Natural 20 Test

```csharp
[Test]
public void TestRoll20_ShouldAlwaysSelectComboAction()
{
    Dice.SetTestRoll(20);
    var character = CreateTestCharacterWithBothActionTypes();
    
    var action = ActionSelector.SelectActionBasedOnRoll(character);
    
    Assert.IsNotNull(action);
    Assert.IsTrue(action.IsComboAction, "Natural 20 should always select combo action");
}
```

### 4. Edge Case Tests

```csharp
[Test]
public void TestRoll1_ShouldSelectNormalAction()
{
    Dice.SetTestRoll(1);
    var character = CreateTestCharacterWithBothActionTypes();
    
    var action = ActionSelector.SelectActionBasedOnRoll(character);
    
    // Roll 1-5 might return null (fail), but if it returns an action, should be normal
    if (action != null)
    {
        Assert.IsFalse(action.IsComboAction, "Roll 1 should not select combo action");
    }
}

[Test]
public void TestRoll19_ShouldSelectComboAction()
{
    Dice.SetTestRoll(19);
    var character = CreateTestCharacterWithBothActionTypes();
    
    var action = ActionSelector.SelectActionBasedOnRoll(character);
    
    Assert.IsNotNull(action);
    Assert.IsTrue(action.IsComboAction, "Roll 19 should select combo action");
}
```

## Implementation Priority

1. **High Priority**: Make Dice testable (Solution 1 or 2)
2. **High Priority**: Add base roll threshold tests (rolls 12, 13, 14)
3. **High Priority**: Add base roll vs total roll tests
4. **Medium Priority**: Add edge case tests (1, 20, boundaries)
5. **Low Priority**: Add probabilistic distribution tests (verify randomness when not in test mode)

## Test Execution Strategy

### Before Implementation
- Document current test gaps
- Get approval for Dice testability changes

### During Implementation
- Add test mode to Dice
- Write tests incrementally
- Verify tests catch the original bug

### After Implementation
- Run full test suite
- Verify no regressions
- Document new test coverage

## Conclusion

The bug was not caught because:
1. **Dice is not testable** - cannot control roll values
2. **Tests are too generic** - only check that methods don't crash
3. **Missing critical tests** - no tests for base roll vs total roll distinction
4. **No boundary testing** - cannot test roll 12, 13, 14 specifically

**Recommendation**: Implement Solution 1 (test mode flag) as it's the simplest and least disruptive, then add comprehensive roll-based tests.
