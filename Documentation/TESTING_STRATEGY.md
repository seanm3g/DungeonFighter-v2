# Testing Strategy - DungeonFighter-v2

Comprehensive testing approach and verification methods for the DungeonFighter-v2 codebase.

## Testing Philosophy

### Core Principles
1. **Test-Driven Development**: Write tests before implementing features
2. **Comprehensive Coverage**: Test all major systems and edge cases
3. **Automated Testing**: Minimize manual testing through automation
4. **Balance Verification**: Ensure game balance through mathematical analysis
5. **Regression Prevention**: Catch breaking changes early

### Testing Levels
1. **Unit Tests**: Individual components and methods
2. **Integration Tests**: System interactions and data flow
3. **Balance Tests**: Game balance and mathematical correctness
4. **Performance Tests**: Execution time and memory usage
5. **User Acceptance Tests**: End-to-end gameplay scenarios

## Test Categories

### 1. System Tests
**Purpose**: Verify core system functionality

**Test Areas**:
- Character creation and initialization
- Equipment system and stat calculations
- Inventory management
- Save/load functionality
- Game state management

**Example**:
```csharp
[Test]
public void TestCharacterCreation()
{
    var character = new Character("TestHero");
    
    Assert.AreEqual("TestHero", character.Name);
    Assert.AreEqual(1, character.Level);
    Assert.IsTrue(character.Health > 0);
    Assert.IsTrue(character.Strength > 0);
}
```

### 2. Combat Tests
**Purpose**: Verify combat mechanics and calculations

**Test Areas**:
- Damage calculations
- Action execution
- Combo system
- Turn management
- Status effects

**Example**:
```csharp
[Test]
public void TestDamageCalculation()
{
    var player = new Character("TestPlayer");
    var enemy = new Enemy("TestEnemy", 1);
    var action = new Action("TestAction");
    
    var damage = CombatCalculator.CalculateDamage(player, enemy, action);
    
    Assert.IsTrue(damage > 0, "Damage should be positive");
    Assert.IsTrue(damage <= player.Strength + player.GetHighestAttribute(), 
                  "Damage should not exceed maximum possible");
}
```

### 3. Balance Tests
**Purpose**: Verify game balance and mathematical correctness

**Test Areas**:
- DPS calculations
- Enemy scaling
- Item progression
- Experience curves
- Combat duration

**Example**:
```csharp
[Test]
public void TestEnemyScaling()
{
    var enemy1 = EnemyFactory.CreateEnemy("Goblin", 1);
    var enemy5 = EnemyFactory.CreateEnemy("Goblin", 5);
    
    Assert.IsTrue(enemy5.Health > enemy1.Health, "Higher level enemies should have more health");
    Assert.IsTrue(enemy5.Strength > enemy1.Strength, "Higher level enemies should have more strength");
    
    // Verify scaling formula
    var expectedHealth = enemy1.Health + (4 * 3); // +3 health per level
    Assert.AreEqual(expectedHealth, enemy5.Health, "Health scaling should follow formula");
}
```

### 4. Data Loading Tests
**Purpose**: Verify JSON data loading and parsing

**Test Areas**:
- Action loading
- Enemy data loading
- Item data loading
- Configuration loading
- Error handling

**Example**:
```csharp
[Test]
public void TestActionLoading()
{
    var actions = ActionLoader.LoadActions();
    
    Assert.IsNotNull(actions, "Actions should be loaded");
    Assert.IsTrue(actions.Count > 0, "Should have actions loaded");
    
    var critAction = actions.FirstOrDefault(a => a.Name == "CRIT");
    Assert.IsNotNull(critAction, "CRIT action should be loaded");
    Assert.AreEqual(3.0, critAction.DamageMultiplier, "CRIT should have 3x damage");
}
```

### 5. Advanced System Tests
**Purpose**: Verify complex system interactions

**Test Areas**:
- Combo system mechanics
- Battle narrative generation
- Enemy AI behavior
- Environmental effects
- Dynamic tuning system

**Example**:
```csharp
[Test]
public void TestComboSystem()
{
    var player = new Character("TestPlayer");
    var enemy = new Enemy("TestEnemy", 1);
    
    // Set up combo sequence
    player.SetComboSequence(new List<string> { "JAB", "CRIT" });
    
    // Execute first action
    var result1 = player.ExecuteAction("JAB", enemy);
    Assert.IsTrue(result1.Success, "First combo action should succeed");
    
    // Execute second action
    var result2 = player.ExecuteAction("CRIT", enemy);
    Assert.IsTrue(result2.Success, "Second combo action should succeed");
    Assert.IsTrue(result2.Damage > result1.Damage, "Combo damage should increase");
}
```

## Testing Framework

### Built-in Test Suite
The game includes a comprehensive test suite accessible through:
- **Settings → Tests**: Interactive test menu
- **Program.cs**: Automated test execution
- **Test Classes**: Organized test categories

### Test Categories Available
1. **Character Tests**: Character creation, stats, progression
2. **Item Tests**: Equipment, inventory, stat bonuses
3. **Dice Tests**: Random number generation, combo mechanics
4. **Action Tests**: Action execution, damage calculations
5. **Combat Tests**: Combat flow, turn management
6. **Enemy Tests**: Enemy creation, scaling, AI
7. **Dungeon Tests**: Room generation, progression
8. **Balance Tests**: DPS analysis, scaling verification
9. **Data Tests**: JSON loading, configuration
10. **Advanced Tests**: Combo system, narrative, tuning

### Running Tests

#### Interactive Testing
```csharp
// Access through game menu
Settings → Tests → [Test Category] → Run Tests
```

#### Programmatic Testing
```csharp
// Run specific test category
Program.RunCharacterTests();
Program.RunCombatTests();
Program.RunBalanceTests();

// Run all tests
Program.RunAllTests();
```

#### Automated Testing
```csharp
// Run tests with results
var results = Program.RunAllTestsWithResults();
foreach (var result in results)
{
    Console.WriteLine($"{result.TestName}: {result.Passed ? "PASS" : "FAIL"}");
}
```

## Balance Testing

### DPS Analysis
**Purpose**: Verify combat balance and scaling

**Method**:
```csharp
[Test]
public void TestDPSBalance()
{
    var analysis = EnemyBalanceCalculator.AnalyzeBalance();
    
    // Verify level 1 DPS targets
    Assert.IsTrue(analysis.PlayerDPS >= 1.5 && analysis.PlayerDPS <= 2.5, 
                  "Player DPS should be balanced");
    Assert.IsTrue(analysis.EnemyDPS >= 1.0 && analysis.EnemyDPS <= 2.0, 
                  "Enemy DPS should be balanced");
    
    // Verify actions to kill
    Assert.IsTrue(analysis.ActionsToKill >= 8 && analysis.ActionsToKill <= 15, 
                  "Combat should last appropriate number of actions");
}
```

### Scaling Verification
**Purpose**: Ensure proper level scaling

**Method**:
```csharp
[Test]
public void TestLevelScaling()
{
    for (int level = 1; level <= 10; level++)
    {
        var character = new Character("TestHero");
        character.SetLevel(level);
        
        var enemy = EnemyFactory.CreateEnemy("Goblin", level);
        
        // Verify scaling formulas
        var expectedHealth = 50 + (level - 1) * 3;
        Assert.AreEqual(expectedHealth, character.Health, 
                       $"Level {level} health should match formula");
        
        var expectedEnemyHealth = 50 + (level - 1) * 3;
        Assert.AreEqual(expectedEnemyHealth, enemy.Health, 
                       $"Level {level} enemy health should match formula");
    }
}
```

## Performance Testing

### Execution Time Tests
**Purpose**: Ensure acceptable performance

**Method**:
```csharp
[Test]
public void TestCombatPerformance()
{
    var stopwatch = Stopwatch.StartNew();
    
    // Run 100 combat simulations
    for (int i = 0; i < 100; i++)
    {
        var player = new Character("TestPlayer");
        var enemy = new Enemy("TestEnemy", 1);
        var combat = new Combat(player, enemy);
        combat.ExecuteCombat();
    }
    
    stopwatch.Stop();
    
    Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, 
                  "100 combats should complete in under 5 seconds");
}
```

### Memory Usage Tests
**Purpose**: Monitor memory consumption

**Method**:
```csharp
[Test]
public void TestMemoryUsage()
{
    var memoryBefore = GC.GetTotalMemory(false);
    
    // Create many objects
    var characters = new List<Character>();
    for (int i = 0; i < 1000; i++)
    {
        characters.Add(new Character($"TestHero{i}"));
    }
    
    var memoryAfter = GC.GetTotalMemory(false);
    var memoryUsed = memoryAfter - memoryBefore;
    
    Assert.IsTrue(memoryUsed < 50 * 1024 * 1024, // 50MB
                  "Memory usage should be reasonable");
}
```

## Integration Testing

### End-to-End Scenarios
**Purpose**: Test complete gameplay flows

**Example**:
```csharp
[Test]
public void TestCompleteGameplayFlow()
{
    // Create new game
    var game = new Game();
    game.Initialize();
    
    // Create character
    var character = game.CreateCharacter("TestHero");
    Assert.IsNotNull(character);
    
    // Enter dungeon
    var dungeon = game.SelectDungeon("Forest");
    Assert.IsNotNull(dungeon);
    
    // Complete dungeon
    var result = game.CompleteDungeon(dungeon);
    Assert.IsTrue(result.Success);
    
    // Verify character progression
    Assert.IsTrue(character.Level > 1 || character.Experience > 0);
}
```

### Data Flow Testing
**Purpose**: Verify data consistency across systems

**Example**:
```csharp
[Test]
public void TestDataFlow()
{
    // Load actions
    var actions = ActionLoader.LoadActions();
    Assert.IsNotNull(actions);
    
    // Create character with actions
    var character = new Character("TestHero");
    character.EquipWeapon(actions.First().Weapon);
    
    // Verify action pool
    var actionPool = character.GetActionPool();
    Assert.IsTrue(actionPool.Count > 0);
    
    // Execute action
    var result = character.ExecuteAction(actionPool.First().Name, new Enemy("TestEnemy", 1));
    Assert.IsTrue(result.Success);
}
```

## Test Data Management

### Test Data Builders
**Purpose**: Create consistent test objects

**Example**:
```csharp
public class TestDataBuilder
{
    public static Character CreateTestCharacter(int level = 1)
    {
        var character = new Character("TestHero");
        character.SetLevel(level);
        character.SetStrength(10);
        character.SetAgility(10);
        character.SetTechnique(10);
        character.SetIntelligence(10);
        return character;
    }
    
    public static Enemy CreateTestEnemy(string type = "Goblin", int level = 1)
    {
        return EnemyFactory.CreateEnemy(type, level);
    }
    
    public static Action CreateTestAction(string name = "TestAction")
    {
        return new Action
        {
            Name = name,
            DamageMultiplier = 1.0,
            Length = 1.0,
            Description = "Test action"
        };
    }
}
```

### Mock Objects
**Purpose**: Isolate units under test

**Example**:
```csharp
public class MockRandom : IRandom
{
    private readonly int _fixedValue;
    
    public MockRandom(int fixedValue)
    {
        _fixedValue = fixedValue;
    }
    
    public int Next(int minValue, int maxValue)
    {
        return _fixedValue;
    }
}

// Usage in tests
[Test]
public void TestDiceWithMockRandom()
{
    var mockRandom = new MockRandom(15);
    var dice = new Dice(mockRandom);
    
    var result = dice.Roll();
    Assert.AreEqual(15, result);
}
```

## Continuous Testing

### Automated Test Execution
**Purpose**: Run tests automatically during development

**Implementation**:
```csharp
public static class ContinuousTesting
{
    public static void RunTestsOnChange()
    {
        // Run quick tests on file changes
        RunQuickTests();
        
        // Run full test suite on significant changes
        if (HasSignificantChanges())
        {
            RunFullTestSuite();
        }
    }
    
    private static void RunQuickTests()
    {
        Program.RunCharacterTests();
        Program.RunCombatTests();
    }
    
    private static void RunFullTestSuite()
    {
        Program.RunAllTests();
    }
}
```

### Test Results Tracking
**Purpose**: Monitor test results over time

**Implementation**:
```csharp
public class TestResults
{
    public string TestName { get; set; }
    public bool Passed { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; }
}

public static class TestTracker
{
    private static readonly List<TestResults> _results = new List<TestResults>();
    
    public static void RecordTestResult(TestResults result)
    {
        _results.Add(result);
        
        // Log results
        var status = result.Passed ? "PASS" : "FAIL";
        Console.WriteLine($"{result.TestName}: {status} ({result.ExecutionTime.TotalMilliseconds}ms)");
        
        if (!result.Passed)
        {
            Console.WriteLine($"  Error: {result.ErrorMessage}");
        }
    }
    
    public static void GenerateTestReport()
    {
        var totalTests = _results.Count;
        var passedTests = _results.Count(r => r.Passed);
        var failedTests = totalTests - passedTests;
        
        Console.WriteLine($"Test Report: {passedTests}/{totalTests} passed ({failedTests} failed)");
    }
}
```

## Best Practices

### 1. Test Organization
- Group related tests together
- Use descriptive test names
- Keep tests focused and simple
- Avoid test interdependencies

### 2. Test Data
- Use consistent test data
- Create reusable test builders
- Avoid hardcoded values where possible
- Clean up test data after tests

### 3. Assertions
- Use specific assertions
- Include descriptive failure messages
- Test both positive and negative cases
- Verify edge cases and boundary conditions

### 4. Performance
- Keep tests fast and efficient
- Use mocks for external dependencies
- Avoid unnecessary object creation
- Run performance tests separately

### 5. Maintenance
- Update tests when requirements change
- Remove obsolete tests
- Refactor tests to improve readability
- Document complex test scenarios

## Related Documentation

- **`PROBLEM_SOLUTIONS.md`**: Solutions to testing problems and issues
- **`DEBUGGING_GUIDE.md`**: Debugging techniques for test failures
- **`CODE_PATTERNS.md`**: Testing patterns and conventions
- **`PERFORMANCE_NOTES.md`**: Performance testing approaches
- **`DEVELOPMENT_WORKFLOW.md`**: Testing in development process

---

*This testing strategy should be updated as new testing requirements emerge.*
