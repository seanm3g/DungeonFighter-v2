# Quick Reference - DungeonFighter-v2

Essential information for rapid development and problem-solving.

## Key File Locations

### Core Game Files
- **Entry Point**: `Code/Program.cs`
- **Main Game Loop**: `Code/Game.cs`
- **Combat System**: `Code/CombatManager.cs`
- **Character System**: `Code/Character.cs` (coordinator) + specialized managers
- **Enemy System**: `Code/Enemy.cs`

### Configuration Files
- **Game Settings**: `Code/gamesettings.json`
- **Tuning Config**: `GameData/TuningConfig.json`
- **Actions**: `GameData/Actions.json`
- **Enemies**: `GameData/Enemies.json`
- **Weapons**: `GameData/Weapons.json`
- **Armor**: `GameData/Armor.json`

### Documentation
- **Architecture**: `Documentation/ARCHITECTURE.md`
- **Overview**: `Documentation/OVERVIEW.md`
- **Task List**: `Documentation/TASKLIST.md`
- **Problem Solutions**: `Documentation/PROBLEM_SOLUTIONS.md`

## Critical Classes & Their Responsibilities

### Game Management
- **`Game.cs`**: Main orchestrator, coordinates all systems
- **`GameInitializer.cs`**: Game setup, character creation
- **`GameMenuManager.cs`**: Menu navigation and user interactions
- **`GameTicker.cs`**: Game time management (singleton)

### Combat System
- **`CombatManager.cs`**: Combat flow orchestration
- **`CombatActions.cs`**: Action execution for players/enemies
- **`CombatCalculator.cs`**: Damage, speed, stat calculations
- **`CombatEffects.cs`**: Status effects and environmental effects
- **`TurnManager.cs`**: Turn-based combat logic
- **`BattleNarrative.cs`**: Event-driven battle descriptions

### Character System
- **`Character.cs`**: Main player coordinator class (refactored from 737 to 539 lines)
- **`CharacterStats.cs`**: Statistics and leveling system
- **`CharacterEquipment.cs`**: Equipment management and stat bonuses
- **`CharacterProgression.cs`**: Experience, leveling, skill progression
- **`CharacterActions.cs`**: Character action management and combo sequences
- **`CharacterHealthManager.cs`**: Health management, damage, and healing logic
- **`CharacterCombatCalculator.cs`**: Combat calculations and stat computations
- **`CharacterDisplayManager.cs`**: Character information display and formatting
- **`CharacterSaveManager.cs`**: Save/load functionality

### Enemy System
- **`Enemy.cs`**: Enemy entity with AI and combat behavior
- **`EnemyFactory.cs`**: Creates enemies based on type and level
- **`EnemyLoader.cs`**: Loads enemy data from JSON
- **`EnemyDPSCalculator.cs`**: Calculates enemy damage per second
- **`EnemyBalanceCalculator.cs`**: Balance testing and analysis

### Data & Configuration
- **`ActionLoader.cs`**: Loads and manages action data
- **`JsonLoader.cs`**: Common JSON loading operations
- **`GameConfiguration.cs`**: Centralized configuration management
- **`TuningConfig.cs`**: Dynamic tuning system (if exists)
- **`ScalingManager.cs`**: Handles scaling calculations

### Utility Systems
- **`UIManager.cs`**: Centralized UI output and display
- **`ErrorHandler.cs`**: Centralized error handling and logging
- **`RandomUtility.cs`**: Consistent random number generation
- **`GameConstants.cs`**: Common constants, file paths, strings
- **`DebugLogger.cs`**: Centralized debug output

## Key Configuration Parameters

### Combat Balance (TuningConfig.json)
```json
{
  "PlayerBaseHealth": 50,
  "HealthPerLevel": 3,
  "EnemyBaseHealth": 50,
  "EnemyHealthPerLevel": 3,
  "CriticalHitThreshold": 20,
  "CriticalHitMultiplier": 2.0,
  "MinimumDamage": 1
}
```

### Action System
- **Combo Threshold**: 16-20 (triggers combo mode)
- **Basic Attack**: 6-15 (normal attacks)
- **Miss Threshold**: 1-5 (failed attacks)
- **Combo Amplifier**: 1.05x base, scales with Technique

### Enemy Scaling
- **Health Scaling**: +3 per level
- **Attribute Scaling**: +1 per level, +2 for primary attribute
- **Primary Attributes**: STR (Orcs, Skeletons), AGI (Goblins, Spiders), TEC (Cultists, Wraiths)

## Common Code Patterns

### JSON Loading Pattern
```csharp
public static T LoadJson<T>(string filePath)
{
    try
    {
        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<T>(json);
    }
    catch (Exception ex)
    {
        ErrorHandler.LogError($"Failed to load {filePath}: {ex.Message}");
        return default(T);
    }
}
```

### Action Execution Pattern
```csharp
public void ExecuteAction(Action action, Entity source, Entity target)
{
    // Calculate damage
    int damage = CombatCalculator.CalculateDamage(source, target, action);
    
    // Apply damage
    target.TakeDamage(damage);
    
    // Log result
    UIManager.DisplayCombatResult(source, target, action, damage);
}
```

### Scaling Calculation Pattern
```csharp
public int CalculateScaledValue(int baseValue, int level, float scalingFactor)
{
    return baseValue + (int)(level * scalingFactor);
}
```

## Testing Commands

### Run All Tests
- Access through: Settings → Tests → All Tests
- Or programmatically: `Program.RunAllTests()`

### Specific Test Categories
- **Combat Tests**: `CombatTest.cs`
- **Balance Tests**: `EnemyBalanceTest.cs`
- **System Tests**: Various test methods in `Program.cs`

## Debugging Commands

### Enable Debug Logging
```csharp
DebugLogger.EnableDebugMode();
DebugLogger.Log("Debug message");
```

### Check Game State
```csharp
// Character stats
character.DisplayStats();

// Enemy stats
enemy.DisplayStats();

// Combat balance
EnemyBalanceCalculator.AnalyzeBalance();
```

## Common File Paths

### GameData Files
- Actions: `GameData/Actions.json`
- Enemies: `GameData/Enemies.json`
- Weapons: `GameData/Weapons.json`
- Armor: `GameData/Armor.json`
- Tuning: `GameData/TuningConfig.json`

### Save Files
- Character: `GameData/character_save.json`
- Settings: `Code/gamesettings.json`

### Build Output
- Debug: `Code/bin/Debug/net8.0/`
- Release: `Code/bin/Release/net8.0/`

## Performance Considerations

### Optimization Tips
1. **Lazy Loading**: JSON data loaded only when needed
2. **Object Pooling**: Reuse enemies and items when possible
3. **Centralized Random**: Single RandomUtility instance
4. **Efficient Data Structures**: Dictionaries for fast lookups

### Memory Management
- Dispose of resources properly
- Avoid creating objects in tight loops
- Use StringBuilder for string concatenation
- Monitor JSON loading and caching

## Error Handling Patterns

### Standard Error Handling
```csharp
try
{
    // Operation
}
catch (Exception ex)
{
    ErrorHandler.LogError($"Operation failed: {ex.Message}");
    // Fallback behavior
}
```

### JSON Loading Error Handling
```csharp
var data = JsonLoader.LoadJson<DataType>(filePath);
if (data == null)
{
    // Use default values or show error
    UIManager.DisplayError("Failed to load data");
}
```

## Quick Fixes

### Reset Game State
1. Delete `GameData/character_save.json`
2. Restart application
3. Create new character

### Reload Configuration
1. Use Tuning Console → Reload Config
2. Or restart application

### Clear Debug Logs
1. Check `DebugLogger.cs` for log clearing methods
2. Or restart application

## Development Workflow

### Making Changes
1. Read relevant architecture documentation
2. Make incremental changes
3. Test changes immediately
4. Update documentation if needed
5. Commit working state

### Testing Changes
1. Run relevant test categories
2. Check balance analysis
3. Verify JSON syntax
4. Test in-game functionality

### Debugging Issues
1. Check PROBLEM_SOLUTIONS.md
2. Enable debug logging
3. Use Tuning Console for parameter adjustment
4. Run balance analysis

## Related Documentation

- **`ARCHITECTURE.md`**: Detailed system architecture and design patterns
- **`CODE_PATTERNS.md`**: Code patterns, conventions, and best practices
- **`PROBLEM_SOLUTIONS.md`**: Solutions to common problems
- **`DEBUGGING_GUIDE.md`**: Debugging techniques and tools
- **`DEVELOPMENT_WORKFLOW.md`**: Step-by-step development process

---

*This reference should be updated as the codebase evolves.*
