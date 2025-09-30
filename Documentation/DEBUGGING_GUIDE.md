# Debugging Guide - DungeonFighter-v2

Comprehensive guide for debugging issues in the DungeonFighter-v2 codebase.

## Debugging Tools & Techniques

### Built-in Debugging Tools

#### 1. Debug Logger
```csharp
// Enable debug mode
DebugLogger.EnableDebugMode();

// Log debug information
DebugLogger.Log("Debug message");
DebugLogger.LogError("Error message");
DebugLogger.LogWarning("Warning message");

// Check if debug mode is enabled
if (DebugLogger.IsDebugModeEnabled)
{
    // Debug-specific code
}
```

#### 2. Error Handler
```csharp
// Log errors with context
ErrorHandler.LogError($"Operation failed: {ex.Message}");
ErrorHandler.LogError($"File: {filePath}, Error: {ex.Message}");

// Get error history
var errors = ErrorHandler.GetErrorHistory();
```

#### 3. Tuning Console
- Access via: Main Menu → Tuning Console
- Real-time parameter adjustment
- Balance analysis and testing
- Configuration export/import

#### 4. Test Suite
- Access via: Settings → Tests
- 27+ test categories
- Automated balance verification
- System integrity checks

### Debugging Strategies

#### 1. Systematic Debugging Approach

**Step 1: Reproduce the Issue**
- Identify exact conditions that trigger the problem
- Note any error messages or unexpected behavior
- Document the steps to reproduce

**Step 2: Isolate the Problem**
- Use debug logging to trace execution flow
- Check relevant configuration files
- Verify data integrity

**Step 3: Analyze the Root Cause**
- Check related systems and dependencies
- Review recent changes
- Compare with known working states

**Step 4: Implement Fix**
- Make minimal changes to fix the issue
- Test the fix thoroughly
- Document the solution

#### 2. Combat System Debugging

**Common Combat Issues:**
- Damage calculations incorrect
- Actions not executing properly
- Combo system not working
- Enemy behavior issues

**Debugging Steps:**
```csharp
// Enable combat debug logging
DebugLogger.Log($"Combat: {source.Name} vs {target.Name}");
DebugLogger.Log($"Action: {action.Name}, Damage: {damage}");
DebugLogger.Log($"Source Stats: STR={source.Strength}, AGI={source.Agility}");
DebugLogger.Log($"Target Stats: Health={target.Health}, Armor={target.Armor}");
```

**Combat Balance Debugging:**
```csharp
// Run balance analysis
var analysis = EnemyBalanceCalculator.AnalyzeBalance();
DebugLogger.Log($"DPS Analysis: {analysis}");

// Check enemy scaling
var enemy = EnemyFactory.CreateEnemy("Goblin", 5);
DebugLogger.Log($"Enemy Stats: {enemy.GetStatsString()}");
```

#### 3. Data Loading Debugging

**JSON Loading Issues:**
```csharp
// Check file existence
if (!File.Exists(filePath))
{
    DebugLogger.LogError($"File not found: {filePath}");
    return;
}

// Validate JSON syntax
try
{
    var data = JsonSerializer.Deserialize<T>(json);
    DebugLogger.Log($"Successfully loaded {typeof(T).Name} from {filePath}");
}
catch (JsonException ex)
{
    DebugLogger.LogError($"JSON parsing error in {filePath}: {ex.Message}");
}
```

**Action Loading Debugging:**
```csharp
// Check action loading
var actions = ActionLoader.LoadActions();
DebugLogger.Log($"Loaded {actions.Count} actions");

// Verify specific action
var action = actions.FirstOrDefault(a => a.Name == "CRIT");
if (action == null)
{
    DebugLogger.LogError("CRIT action not found in loaded actions");
}
```

#### 4. Character System Debugging

**Character Progression Issues:**
```csharp
// Check character stats
DebugLogger.Log($"Character Level: {character.Level}");
DebugLogger.Log($"Character Stats: STR={character.Strength}, AGI={character.Agility}");
DebugLogger.Log($"Character Health: {character.Health}/{character.MaxHealth}");

// Check equipment
DebugLogger.Log($"Equipped Weapon: {character.EquippedWeapon?.Name}");
DebugLogger.Log($"Equipped Armor: {character.EquippedArmor?.Count} pieces");
```

**Equipment Debugging:**
```csharp
// Check equipment bonuses
var totalArmor = character.GetTotalArmor();
var totalDamage = character.GetTotalDamage();
DebugLogger.Log($"Total Armor: {totalArmor}, Total Damage: {totalDamage}");

// Check action pool
var actionPool = character.GetActionPool();
DebugLogger.Log($"Action Pool: {string.Join(", ", actionPool.Select(a => a.Name))}");
```

### Common Debugging Scenarios

#### Scenario 1: Combat Not Working Properly

**Symptoms:**
- Actions not executing
- Damage not being dealt
- Combat flow interrupted

**Debugging Steps:**
1. Check combat manager initialization
2. Verify action loading and availability
3. Check turn manager state
4. Verify entity stats and health

**Debug Code:**
```csharp
// In CombatManager.cs
DebugLogger.Log($"Combat started: {player.Name} vs {enemy.Name}");
DebugLogger.Log($"Player actions available: {player.GetActionPool().Count}");
DebugLogger.Log($"Enemy actions available: {enemy.GetActionPool().Count}");
```

#### Scenario 2: Character Stats Not Updating

**Symptoms:**
- Stats not increasing on level up
- Equipment bonuses not applied
- Health not restoring

**Debugging Steps:**
1. Check character progression system
2. Verify equipment stat calculations
3. Check level up logic
4. Verify save/load functionality

**Debug Code:**
```csharp
// In CharacterProgression.cs
DebugLogger.Log($"Level up: {oldLevel} -> {newLevel}");
DebugLogger.Log($"Stat increases: STR +{strIncrease}, AGI +{agiIncrease}");
DebugLogger.Log($"Health restored: {healthRestored}");
```

#### Scenario 3: JSON Data Not Loading

**Symptoms:**
- Default values being used
- Actions not appearing
- Configuration not taking effect

**Debugging Steps:**
1. Check file paths and existence
2. Validate JSON syntax
3. Verify deserialization
4. Check error handling

**Debug Code:**
```csharp
// In JsonLoader.cs
DebugLogger.Log($"Attempting to load: {filePath}");
DebugLogger.Log($"File exists: {File.Exists(filePath)}");
DebugLogger.Log($"File size: {new FileInfo(filePath).Length} bytes");
```

### Performance Debugging

#### Memory Usage
```csharp
// Check memory usage
var memoryBefore = GC.GetTotalMemory(false);
// ... operation ...
var memoryAfter = GC.GetTotalMemory(false);
DebugLogger.Log($"Memory usage: {memoryAfter - memoryBefore} bytes");
```

#### Execution Time
```csharp
// Measure execution time
var stopwatch = Stopwatch.StartNew();
// ... operation ...
stopwatch.Stop();
DebugLogger.Log($"Operation took: {stopwatch.ElapsedMilliseconds}ms");
```

### Debugging Configuration

#### Enable Verbose Logging
```csharp
// In GameSettings.cs or similar
public static bool EnableVerboseLogging = true;
public static bool LogCombatDetails = true;
public static bool LogDataLoading = true;
```

#### Debug Output Control
```csharp
// Conditional debug logging
if (DebugLogger.IsDebugModeEnabled && GameSettings.EnableVerboseLogging)
{
    DebugLogger.Log($"Verbose: {detailedMessage}");
}
```

### Testing as Debugging

#### Unit Testing for Debugging
```csharp
// Create test cases for problematic scenarios
[Test]
public void TestCombatDamageCalculation()
{
    var player = new Character("TestPlayer");
    var enemy = new Enemy("TestEnemy", 1);
    var action = new Action("TestAction");
    
    var damage = CombatCalculator.CalculateDamage(player, enemy, action);
    
    Assert.IsTrue(damage > 0, "Damage should be positive");
    DebugLogger.Log($"Test damage: {damage}");
}
```

#### Integration Testing
```csharp
// Test complete systems
[Test]
public void TestCombatFlow()
{
    var game = new Game();
    game.Initialize();
    
    var result = game.StartCombat();
    
    Assert.IsNotNull(result);
    DebugLogger.Log($"Combat result: {result}");
}
```

### Debugging Best Practices

#### 1. Use Descriptive Log Messages
```csharp
// Good
DebugLogger.Log($"Combat: {source.Name} uses {action.Name} on {target.Name} for {damage} damage");

// Bad
DebugLogger.Log("Combat action");
```

#### 2. Include Context Information
```csharp
// Include relevant state information
DebugLogger.Log($"Character: {character.Name}, Level: {character.Level}, Health: {character.Health}/{character.MaxHealth}");
```

#### 3. Use Appropriate Log Levels
```csharp
// Use different levels for different types of information
DebugLogger.Log("Normal operation");
DebugLogger.LogWarning("Potential issue");
DebugLogger.LogError("Error occurred");
```

#### 4. Clean Up Debug Code
- Remove debug logging from production builds
- Use conditional compilation for debug code
- Keep debug code organized and documented

### Debugging Tools Integration

#### Visual Studio Debugging
- Set breakpoints in critical code paths
- Use watch windows for variable inspection
- Step through code execution
- Use call stack to trace execution flow

#### External Tools
- JSON validators for configuration files
- Memory profilers for performance issues
- Log analyzers for pattern detection

### Emergency Debugging

#### When All Else Fails
1. **Reset to Known Good State**
   - Restore from backup
   - Delete save files
   - Restart application

2. **Minimal Reproduction**
   - Create simplest possible test case
   - Isolate the problem
   - Test incrementally

3. **Get Help**
   - Check PROBLEM_SOLUTIONS.md
   - Review recent changes
   - Document the issue thoroughly

## Related Documentation

- **`PROBLEM_SOLUTIONS.md`**: Solutions to common problems and issues
- **`QUICK_REFERENCE.md`**: Fast lookup for debugging commands and tools
- **`TESTING_STRATEGY.md`**: Testing approaches for verification
- **`CODE_PATTERNS.md`**: Code patterns and error handling conventions
- **`PERFORMANCE_NOTES.md`**: Performance debugging and monitoring

---

*This guide should be updated as new debugging techniques are discovered.*
