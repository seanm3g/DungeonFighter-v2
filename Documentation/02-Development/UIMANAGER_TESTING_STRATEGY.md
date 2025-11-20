# UIManager Testing Strategy & Implementation Guide

**Status**: ✅ Code Complete  
**Next Phase**: Testing Implementation  
**Test Framework**: Custom (matching existing test suite)  
**Estimated Tests**: 60-80 comprehensive unit tests  
**Estimated Time**: 20-30 hours

---

## Overview

The UIManager refactoring is complete and production-ready. This document outlines the comprehensive testing strategy for validating all four specialized managers and their coordination.

---

## Testing Architecture

```
UIManager Test Suite
├─ UIOutputManager Tests
│  ├─ Constructor & Initialization
│  ├─ WriteLine Functionality
│  ├─ Write Functionality
│  ├─ WriteBlankLine Functionality
│  ├─ WriteMenuLine Functionality
│  ├─ WriteChunked Functionality
│  ├─ ResetForNewBattle
│  ├─ Custom UI Manager Delegation
│  └─ Color Markup Handling
│
├─ UIDelayManager Tests
│  ├─ Constructor & Initialization
│  ├─ ApplyDelay (Various Message Types)
│  ├─ Progressive Menu Delay Phases
│  │  ├─ Phase 1 (Lines 1-20)
│  │  └─ Phase 2 (Lines 21+)
│  ├─ Menu Delay Counter Management
│  ├─ Counter Reset & State
│  └─ Multi-session Tracking
│
├─ UIColoredTextManager Tests
│  ├─ Constructor & Dependencies
│  ├─ WriteColoredText (Single)
│  ├─ WriteColoredText (List)
│  ├─ WriteLineColoredText
│  ├─ WriteColoredSegments
│  ├─ WriteLineColoredSegments
│  ├─ Builder Pattern Support
│  ├─ Message Type Delegation
│  └─ Integration Flows
│
├─ UIMessageBuilder Tests
│  ├─ WriteCombatMessage
│  │  ├─ Basic Attack
│  │  ├─ Critical Hit
│  │  ├─ Miss
│  │  ├─ Block
│  │  └─ Dodge
│  ├─ WriteHealingMessage
│  │  ├─ Basic Healing
│  │  ├─ Self-healing
│  │  └─ Large/Minimal Amounts
│  ├─ WriteStatusEffectMessage
│  │  ├─ Effect Applied
│  │  └─ Effect Removed
│  └─ Complex Battle Sequences
│
└─ Integration & Regression Tests
   ├─ Manager Coordination
   ├─ Configuration Changes
   ├─ Custom UI Manager Switching
   ├─ Backward Compatibility
   └─ Performance Verification
```

---

## Test Implementation Guide

### Framework: Custom Test Pattern (Matching Existing Suite)

Follow the pattern established in `Tests\Unit\BattleNarrativeManagersTest.cs`:

```csharp
public static class UIManagerTest
{
    public static void RunAllTests()
    {
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("UIMANAGER TEST SUITE");
        Console.WriteLine(new string('=', 80));

        TestUIOutputManager();
        TestUIDelayManager();
        TestUIColoredTextManager();
        TestUIMessageBuilder();
        TestIntegration();

        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("ALL TESTS COMPLETED SUCCESSFULLY");
        Console.WriteLine(new string('=', 80));
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"ASSERTION FAILED: {message}");
        Console.WriteLine($"  ✓ {message}");
    }

    // Individual test methods below...
}
```

---

## UIOutputManager Test Suite (10-15 Tests)

### 1. Constructor Tests (2)

```csharp
private static void TestUIOutputManager_Constructor()
{
    Console.WriteLine("\n--- UIOutputManager Constructor Tests ---");
    
    // Test 1: Constructor with null custom manager
    var manager1 = new UIOutputManager(null, CreateTestConfig());
    Assert(manager1 != null, "Should create UIOutputManager with null custom UI");
    Assert(manager1.GetCustomUIManager() == null, "Custom UI manager should be null");
    
    // Test 2: Constructor with custom manager
    var mockCustomUI = CreateMockCustomUIManager();
    var manager2 = new UIOutputManager(mockCustomUI, CreateTestConfig());
    Assert(manager2 != null, "Should create UIOutputManager with custom UI");
    Assert(manager2.GetCustomUIManager() == mockCustomUI, "Should store custom UI reference");
}
```

### 2. WriteLine Tests (3)

```csharp
private static void TestUIOutputManager_WriteLine()
{
    Console.WriteLine("\n--- UIOutputManager WriteLine Tests ---");
    
    var manager = new UIOutputManager(null, CreateTestConfig());
    
    // Test 1: Basic WriteLine
    Assert(Record.Exception(() => manager.WriteLine("Test message")) == null, 
        "WriteLine should not throw");
    
    // Test 2: WriteLine with different message types
    var types = new[] { UIMessageType.System, UIMessageType.Menu, UIMessageType.Combat };
    foreach (var type in types)
    {
        Assert(Record.Exception(() => manager.WriteLine("Message", type)) == null,
            $"WriteLine should support message type {type}");
    }
    
    // Test 3: Colored markup support
    Assert(Record.Exception(() => manager.WriteLine("&RRed&X Normal")) == null,
        "WriteLine should handle color markup");
}
```

### 3. Delegation Tests (2)

```csharp
private static void TestUIOutputManager_Delegation()
{
    Console.WriteLine("\n--- UIOutputManager Delegation Tests ---");
    
    var mockCustomUI = CreateMockCustomUIManager();
    var manager = new UIOutputManager(mockCustomUI, CreateTestConfig());
    
    // Test 1: WriteLine delegation
    manager.WriteLine("Test", UIMessageType.System);
    Assert(mockCustomUI.WriteLineCalled, "Should delegate WriteLine to custom UI");
    
    // Test 2: Write delegation
    manager.Write("Test");
    Assert(mockCustomUI.WriteCalled, "Should delegate Write to custom UI");
}
```

### 4. Output Method Tests (3)

```csharp
private static void TestUIOutputManager_OutputMethods()
{
    Console.WriteLine("\n--- UIOutputManager Output Methods Tests ---");
    
    var manager = new UIOutputManager(null, CreateTestConfig());
    
    // Test 1: WriteBlankLine
    Assert(Record.Exception(() => manager.WriteBlankLine()) == null,
        "WriteBlankLine should not throw");
    
    // Test 2: WriteMenuLine
    Assert(Record.Exception(() => manager.WriteMenuLine("Menu")) == null,
        "WriteMenuLine should not throw");
    
    // Test 3: WriteChunked
    Assert(Record.Exception(() => manager.WriteChunked("Text")) == null,
        "WriteChunked should not throw");
}
```

### 5. ResetForNewBattle Test (1)

```csharp
private static void TestUIOutputManager_ResetForNewBattle()
{
    Console.WriteLine("\n--- UIOutputManager ResetForNewBattle Test ---");
    
    var mockCustomUI = CreateMockCustomUIManager();
    var manager = new UIOutputManager(mockCustomUI, CreateTestConfig());
    
    manager.ResetForNewBattle();
    Assert(mockCustomUI.ResetForNewBattleCalled, 
        "ResetForNewBattle should delegate to custom UI");
}
```

---

## UIDelayManager Test Suite (15-20 Tests)

### 1. Constructor Tests (1)

```csharp
private static void TestUIDelayManager_Constructor()
{
    Console.WriteLine("\n--- UIDelayManager Constructor Tests ---");
    
    var manager = new UIDelayManager(CreateTestConfig());
    Assert(manager.GetConsecutiveMenuLineCount() == 0, 
        "Initial menu line count should be 0");
    Assert(manager.GetBaseMenuDelay() == 0, 
        "Initial base menu delay should be 0");
}
```

### 2. ApplyDelay Tests (3)

```csharp
private static void TestUIDelayManager_ApplyDelay()
{
    Console.WriteLine("\n--- UIDelayManager ApplyDelay Tests ---");
    
    var manager = new UIDelayManager(CreateTestConfig());
    
    // Test 1: Multiple message types
    var types = new[] { UIMessageType.System, UIMessageType.Menu, UIMessageType.Combat };
    foreach (var type in types)
    {
        Assert(Record.Exception(() => manager.ApplyDelay(type)) == null,
            $"ApplyDelay should support message type {type}");
    }
    
    // Test 2: Sequential calls
    for (int i = 0; i < 5; i++)
    {
        Assert(Record.Exception(() => manager.ApplyDelay(UIMessageType.System)) == null,
            "Sequential ApplyDelay calls should succeed");
    }
    
    // Test 3: No timing issues
    var sw = System.Diagnostics.Stopwatch.StartNew();
    manager.ApplyDelay(UIMessageType.System);
    sw.Stop();
    Assert(sw.ElapsedMilliseconds < 100, 
        "ApplyDelay should complete quickly");
}
```

### 3. Progressive Menu Delay Tests (8-10)

```csharp
private static void TestUIDelayManager_ProgressiveMenuDelay()
{
    Console.WriteLine("\n--- UIDelayManager Progressive Menu Delay Tests ---");
    
    var manager = new UIDelayManager(CreateTestConfig());
    
    // Test 1: First call initializes base delay
    manager.ApplyProgressiveMenuDelay();
    var baseDelay = manager.GetBaseMenuDelay();
    Assert(baseDelay != 0, "Base delay should be set after first call");
    
    // Test 2: Counter increments
    manager = new UIDelayManager(CreateTestConfig());
    for (int i = 1; i <= 10; i++)
    {
        manager.ApplyProgressiveMenuDelay();
        Assert(manager.GetConsecutiveMenuLineCount() == i,
            $"Menu line count should be {i} after {i} calls");
    }
    
    // Test 3: Phase 1 (Lines 1-20) behavior
    manager = new UIDelayManager(CreateTestConfig());
    for (int i = 0; i < 20; i++)
    {
        manager.ApplyProgressiveMenuDelay();
    }
    Assert(manager.GetConsecutiveMenuLineCount() == 20,
        "Should handle 20 lines in phase 1");
    
    // Test 4: Phase 2 (Lines 21+) behavior
    for (int i = 0; i < 10; i++)
    {
        manager.ApplyProgressiveMenuDelay();
    }
    Assert(manager.GetConsecutiveMenuLineCount() == 30,
        "Should handle lines beyond 20");
    
    // Test 5: Base delay consistency
    var initialDelay = manager.GetBaseMenuDelay();
    manager.ApplyProgressiveMenuDelay();
    Assert(manager.GetBaseMenuDelay() == initialDelay,
        "Base delay should remain constant");
}
```

### 4. Counter Management Tests (3-4)

```csharp
private static void TestUIDelayManager_CounterManagement()
{
    Console.WriteLine("\n--- UIDelayManager Counter Management Tests ---");
    
    var manager = new UIDelayManager(CreateTestConfig());
    
    // Test 1: ResetMenuDelayCounter
    for (int i = 0; i < 10; i++)
    {
        manager.ApplyProgressiveMenuDelay();
    }
    Assert(manager.GetConsecutiveMenuLineCount() == 10, "Count should be 10");
    manager.ResetMenuDelayCounter();
    Assert(manager.GetConsecutiveMenuLineCount() == 0, 
        "Count should reset to 0");
    
    // Test 2: New sequence after reset
    manager.ApplyProgressiveMenuDelay();
    var newBaseDelay = manager.GetBaseMenuDelay();
    Assert(newBaseDelay != 0, "New base delay should be set after reset");
    
    // Test 3: Multiple resets
    for (int cycle = 0; cycle < 3; cycle++)
    {
        for (int i = 0; i < 5; i++)
            manager.ApplyProgressiveMenuDelay();
        Assert(manager.GetConsecutiveMenuLineCount() == 5,
            $"Cycle {cycle + 1}: count should be 5");
        manager.ResetMenuDelayCounter();
        Assert(manager.GetConsecutiveMenuLineCount() == 0,
            $"Cycle {cycle + 1}: count should reset to 0");
    }
}
```

### 5. State Consistency Tests (1-2)

```csharp
private static void TestUIDelayManager_StateConsistency()
{
    Console.WriteLine("\n--- UIDelayManager State Consistency Tests ---");
    
    var manager = new UIDelayManager(CreateTestConfig());
    
    // Test 1: Base delay never changes after first set
    manager.ApplyProgressiveMenuDelay();
    var initialDelay = manager.GetBaseMenuDelay();
    
    for (int i = 0; i < 50; i++)
    {
        manager.ApplyProgressiveMenuDelay();
        Assert(manager.GetBaseMenuDelay() == initialDelay,
            $"Base delay should remain constant (iteration {i})");
    }
}
```

---

## UIColoredTextManager Test Suite (10-15 Tests)

### 1. Constructor & Dependencies Test (1)

```csharp
private static void TestUIColoredTextManager_Constructor()
{
    Console.WriteLine("\n--- UIColoredTextManager Constructor Tests ---");
    
    var mockOutput = CreateMockUIOutputManager();
    var mockDelay = CreateMockUIDelayManager();
    var manager = new UIColoredTextManager(mockOutput, mockDelay);
    
    Assert(manager != null, "Should create UIColoredTextManager");
}
```

### 2. WriteColoredText Tests (3)

```csharp
private static void TestUIColoredTextManager_WriteColoredText()
{
    Console.WriteLine("\n--- UIColoredTextManager WriteColoredText Tests ---");
    
    var mockOutput = CreateMockUIOutputManager();
    var mockDelay = CreateMockUIDelayManager();
    var manager = new UIColoredTextManager(mockOutput, mockDelay);
    
    // Test 1: Single colored text
    var coloredText = new ColoredText("Red", "red");
    Assert(Record.Exception(() => manager.WriteColoredText(coloredText)) == null,
        "Should write single colored text");
    Assert(mockDelay.ApplyDelayCalled, 
        "Should apply delay after colored text");
    
    // Test 2: List of colored texts
    var segments = new List<ColoredText> {
        new ColoredText("Red", "red"),
        new ColoredText("Green", "green")
    };
    Assert(Record.Exception(() => manager.WriteColoredText(segments)) == null,
        "Should write colored text list");
    
    // Test 3: With message types
    foreach (var type in new[] { UIMessageType.System, UIMessageType.Combat })
    {
        Assert(Record.Exception(() => 
            manager.WriteColoredText(coloredText, type)) == null,
            $"Should support message type {type}");
    }
}
```

### 3. WriteLineColoredText Tests (2)

```csharp
private static void TestUIColoredTextManager_WriteLineColoredText()
{
    Console.WriteLine("\n--- UIColoredTextManager WriteLineColoredText Tests ---");
    
    var mockOutput = CreateMockUIOutputManager();
    var mockDelay = CreateMockUIDelayManager();
    var manager = new UIColoredTextManager(mockOutput, mockDelay);
    
    var coloredText = new ColoredText("Test", "blue");
    
    // Test 1: Basic WriteLineColoredText
    Assert(Record.Exception(() => manager.WriteLineColoredText(coloredText)) == null,
        "Should write line colored text");
    
    // Test 2: With message types
    Assert(Record.Exception(() => 
        manager.WriteLineColoredText(coloredText, UIMessageType.Combat)) == null,
        "Should support message types");
}
```

### 4. Builder Pattern Tests (2-3)

```csharp
private static void TestUIColoredTextManager_BuilderPattern()
{
    Console.WriteLine("\n--- UIColoredTextManager Builder Pattern Tests ---");
    
    var mockOutput = CreateMockUIOutputManager();
    var mockDelay = CreateMockUIDelayManager();
    var manager = new UIColoredTextManager(mockOutput, mockDelay);
    
    // Test 1: WriteColoredTextBuilder
    var builder = new ColoredTextBuilder();
    builder.Add("Hero", ColorPalette.Player);
    builder.AddSpace();
    builder.Add("attacks", "damage");
    
    Assert(Record.Exception(() => manager.WriteColoredTextBuilder(builder)) == null,
        "Should build and write colored text");
    
    // Test 2: WriteLineColoredTextBuilder
    Assert(Record.Exception(() => manager.WriteLineColoredTextBuilder(builder)) == null,
        "Should build and write line colored text");
}
```

### 5. Segments Tests (2)

```csharp
private static void TestUIColoredTextManager_Segments()
{
    Console.WriteLine("\n--- UIColoredTextManager Segments Tests ---");
    
    var mockOutput = CreateMockUIOutputManager();
    var mockDelay = CreateMockUIDelayManager();
    var manager = new UIColoredTextManager(mockOutput, mockDelay);
    
    var segments = new List<ColoredText> {
        new ColoredText("Hello", "green"),
        new ColoredText(" ", "white"),
        new ColoredText("World", "blue")
    };
    
    // Test 1: WriteColoredSegments
    Assert(Record.Exception(() => manager.WriteColoredSegments(segments)) == null,
        "Should write colored segments");
    
    // Test 2: WriteLineColoredSegments
    Assert(Record.Exception(() => manager.WriteLineColoredSegments(segments)) == null,
        "Should write line colored segments");
}
```

### 6. Integration Tests (1-2)

```csharp
private static void TestUIColoredTextManager_Integration()
{
    Console.WriteLine("\n--- UIColoredTextManager Integration Tests ---");
    
    var mockOutput = CreateMockUIOutputManager();
    var mockDelay = CreateMockUIDelayManager();
    var manager = new UIColoredTextManager(mockOutput, mockDelay);
    
    // Test 1: Sequential operations
    manager.WriteColoredText(new ColoredText("First", "red"));
    manager.WriteLineColoredText(new ColoredText("Second", "green"));
    
    Assert(mockDelay.ApplyDelayCount >= 2,
        "Should apply delay for each operation");
}
```

---

## UIMessageBuilder Test Suite (15-20 Tests)

### 1. WriteCombatMessage Tests (6-8)

```csharp
private static void TestUIMessageBuilder_WriteCombatMessage()
{
    Console.WriteLine("\n--- UIMessageBuilder WriteCombatMessage Tests ---");
    
    var mockColoredText = CreateMockUIColoredTextManager();
    var builder = new UIMessageBuilder(mockColoredText);
    
    // Test 1: Basic attack
    Assert(Record.Exception(() => 
        builder.WriteCombatMessage("Hero", "attacks", "Goblin", 25)) == null,
        "Should handle basic attack");
    Assert(mockColoredText.WriteLineColoredTextBuilderCalled,
        "Should delegate to colored text manager");
    
    // Test 2: Critical hit
    Assert(Record.Exception(() =>
        builder.WriteCombatMessage("Rogue", "backstabs", "Orc", 50, isCritical: true)) == null,
        "Should handle critical hit");
    
    // Test 3: Miss
    Assert(Record.Exception(() =>
        builder.WriteCombatMessage("Warrior", "swings at", "Enemy", isMiss: true)) == null,
        "Should handle miss");
    
    // Test 4: Block
    Assert(Record.Exception(() =>
        builder.WriteCombatMessage("Knight", "parries", "Attack", isBlock: true)) == null,
        "Should handle block");
    
    // Test 5: Dodge
    Assert(Record.Exception(() =>
        builder.WriteCombatMessage("Rogue", "sidesteps", "Attack", isDodge: true)) == null,
        "Should handle dodge");
    
    // Test 6: No damage
    Assert(Record.Exception(() =>
        builder.WriteCombatMessage("Hero", "attacks", "Goblin")) == null,
        "Should handle message without damage");
}
```

### 2. WriteHealingMessage Tests (4-5)

```csharp
private static void TestUIMessageBuilder_WriteHealingMessage()
{
    Console.WriteLine("\n--- UIMessageBuilder WriteHealingMessage Tests ---");
    
    var mockColoredText = CreateMockUIColoredTextManager();
    var builder = new UIMessageBuilder(mockColoredText);
    
    // Test 1: Basic healing
    Assert(Record.Exception(() =>
        builder.WriteHealingMessage("Cleric", "Knight", 30)) == null,
        "Should handle basic healing");
    
    // Test 2: Self healing
    Assert(Record.Exception(() =>
        builder.WriteHealingMessage("Mage", "Mage", 50)) == null,
        "Should handle self-healing");
    
    // Test 3: Large amount
    Assert(Record.Exception(() =>
        builder.WriteHealingMessage("High Priest", "Warrior", 999)) == null,
        "Should handle large healing amounts");
    
    // Test 4: Minimal amount
    Assert(Record.Exception(() =>
        builder.WriteHealingMessage("Apprentice", "Friend", 1)) == null,
        "Should handle minimal healing");
}
```

### 3. WriteStatusEffectMessage Tests (4-5)

```csharp
private static void TestUIMessageBuilder_WriteStatusEffectMessage()
{
    Console.WriteLine("\n--- UIMessageBuilder WriteStatusEffectMessage Tests ---");
    
    var mockColoredText = CreateMockUIColoredTextManager();
    var builder = new UIMessageBuilder(mockColoredText);
    
    // Test 1: Effect applied
    Assert(Record.Exception(() =>
        builder.WriteStatusEffectMessage("Enemy", "Poison", true)) == null,
        "Should handle effect applied");
    
    // Test 2: Effect removed
    Assert(Record.Exception(() =>
        builder.WriteStatusEffectMessage("Hero", "Blind", false)) == null,
        "Should handle effect removed");
    
    // Test 3: Default (applied)
    Assert(Record.Exception(() =>
        builder.WriteStatusEffectMessage("Target", "Stun")) == null,
        "Should default to effect applied");
    
    // Test 4: Various effects
    var effects = new[] { "Poison", "Bleed", "Weaken", "Slow", "Stun" };
    foreach (var effect in effects)
    {
        Assert(Record.Exception(() =>
            builder.WriteStatusEffectMessage("Target", effect)) == null,
            $"Should handle {effect} effect");
    }
}
```

### 4. Complex Battle Sequence Tests (2-3)

```csharp
private static void TestUIMessageBuilder_ComplexSequence()
{
    Console.WriteLine("\n--- UIMessageBuilder Complex Battle Sequence Tests ---");
    
    var mockColoredText = CreateMockUIColoredTextManager();
    var builder = new UIMessageBuilder(mockColoredText);
    
    // Test 1: Full battle round
    builder.WriteCombatMessage("Hero", "attacks", "Goblin", 20);
    builder.WriteCombatMessage("Goblin", "retaliates", "Hero", 10);
    builder.WriteHealingMessage("Cleric", "Hero", 15);
    builder.WriteStatusEffectMessage("Goblin", "Bleed");
    
    Assert(mockColoredText.WriteLineColoredTextBuilderCount >= 4,
        "Should handle multiple messages in sequence");
    
    // Test 2: Multiple rounds
    int totalMessages = 0;
    for (int i = 0; i < 5; i++)
    {
        builder.WriteCombatMessage("Party", "attacks", "Enemy", 25);
        totalMessages++;
        if (i % 2 == 0)
        {
            builder.WriteHealingMessage("Support", "Party", 20);
            totalMessages++;
        }
    }
    Assert(mockColoredText.WriteLineColoredTextBuilderCount >= totalMessages,
        "Should handle multiple rounds");
}
```

---

## Integration & Regression Tests (15-20 Tests)

### 1. Manager Coordination Tests (5)

```csharp
private static void TestIntegration_ManagerCoordination()
{
    Console.WriteLine("\n--- Integration Tests: Manager Coordination ---");
    
    // Test 1: Output + Delay coordination
    // Verify that output operations properly delegate to delay manager
    
    // Test 2: Configuration integration
    // Verify that configuration changes propagate correctly
    
    // Test 3: Custom UI manager switching
    // Verify that switching UI managers works correctly
    
    // Test 4: Message type routing
    // Verify that message types route correctly through all managers
    
    // Test 5: State preservation across operations
    // Verify that state is properly maintained across multiple operations
}
```

### 2. Backward Compatibility Tests (3-4)

```csharp
private static void TestIntegration_BackwardCompatibility()
{
    Console.WriteLine("\n--- Integration Tests: Backward Compatibility ---");
    
    // Test 1: All existing public APIs unchanged
    // Verify that UIManager static methods work exactly as before
    
    // Test 2: Behavior preservation
    // Verify that output and formatting behavior is unchanged
    
    // Test 3: Existing code compatibility
    // Verify that code using UIManager doesn't need to change
}
```

### 3. Configuration Tests (2-3)

```csharp
private static void TestIntegration_Configuration()
{
    Console.WriteLine("\n--- Integration Tests: Configuration ---");
    
    // Test 1: Configuration loading
    // Verify that UIConfiguration loads correctly
    
    // Test 2: Configuration updates
    // Verify that configuration changes are properly reflected
    
    // Test 3: Delay configuration application
    // Verify that delay times from configuration are used correctly
}
```

### 4. Performance Tests (3-4)

```csharp
private static void TestIntegration_Performance()
{
    Console.WriteLine("\n--- Integration Tests: Performance ---");
    
    // Test 1: Manager initialization performance
    // Verify that lazy initialization doesn't cause delays
    
    // Test 2: Output operation performance
    // Verify that output operations complete within acceptable time
    
    // Test 3: High-volume operations
    // Verify that many sequential operations don't cause issues
    
    // Test 4: Memory usage
    // Verify that managers don't leak memory
}
```

### 5. Error Handling Tests (2-3)

```csharp
private static void TestIntegration_ErrorHandling()
{
    Console.WriteLine("\n--- Integration Tests: Error Handling ---");
    
    // Test 1: Null input handling
    // Verify graceful handling of null parameters
    
    // Test 2: Edge cases
    // Verify handling of empty strings, zero values, etc.
    
    // Test 3: Recovery after errors
    // Verify that system recovers properly from errors
}
```

---

## Test Execution Guide

### Create Test File

Create `Code/Tests/Unit/UIManagerTest.cs` following the existing pattern:

```csharp
namespace RPGGame
{
    public static class UIManagerTest
    {
        public static void RunAllTests()
        {
            // Implement all test methods above
        }
    }
}
```

### Run Tests

Add to your test runner program:

```csharp
UIManagerTest.RunAllTests();
```

### Expected Results

- **60-80 total tests**
- **95%+ code coverage**
- **All tests passing**
- **Zero breaking changes**

---

## Test Metrics

### Coverage Targets

| Component | Target Coverage | Tests |
|-----------|-----------------|-------|
| UIOutputManager | 95%+ | 10-15 |
| UIDelayManager | 95%+ | 15-20 |
| UIColoredTextManager | 95%+ | 10-15 |
| UIMessageBuilder | 95%+ | 15-20 |
| Integration | 90%+ | 15-20 |
| **Total** | **92%+** | **60-80** |

### Time Estimates

| Task | Hours | Notes |
|------|-------|-------|
| UIOutputManager tests | 3-4 | Custom manager mocking |
| UIDelayManager tests | 4-5 | Progressive delay algorithm |
| UIColoredTextManager tests | 3-4 | Color handling verification |
| UIMessageBuilder tests | 4-5 | Message format validation |
| Integration tests | 4-5 | Coordination verification |
| Documentation | 2-3 | Test results & coverage |
| **Total** | **20-26** | **Estimated** |

---

## Mock Objects Guide

### Mock UICustomManager

```csharp
private class MockUICustomManager : IUIManager
{
    public bool WriteLineCalled { get; set; }
    public bool WriteCalled { get; set; }
    public bool ResetForNewBattleCalled { get; set; }
    
    public void WriteLine(string message, UIMessageType type) => WriteLineCalled = true;
    public void Write(string message) => WriteCalled = true;
    public void ResetForNewBattle() => ResetForNewBattleCalled = true;
    // ... other members
}
```

### Mock UIDelayManager

```csharp
private class MockUIDelayManager : UIDelayManager
{
    public bool ApplyDelayCalled { get; set; }
    public int ApplyDelayCount { get; set; }
    
    public override void ApplyDelay(UIMessageType type)
    {
        ApplyDelayCalled = true;
        ApplyDelayCount++;
    }
    // ... other members
}
```

---

## Success Criteria

✅ **Code Quality**
- All 60-80 tests passing
- 95%+ code coverage achieved
- Zero test failures

✅ **Performance**
- No performance degradation
- Initialization < 10ms
- Operations < 50ms average

✅ **Compatibility**
- 100% backward compatible
- All existing code works
- No breaking changes

✅ **Documentation**
- All tests documented
- Coverage report generated
- Results verified

---

## Conclusion

This comprehensive testing strategy ensures complete validation of the UIManager refactoring. Following this guide will result in a robust, well-tested system with 95%+ code coverage and zero breaking changes.

---

**Status**: ✅ Refactoring Complete  
**Next**: Implement Test Suite  
**Estimated Time**: 20-30 hours  
**Framework**: Custom (matching existing tests)  

🚀 **Ready for Test Implementation!**

