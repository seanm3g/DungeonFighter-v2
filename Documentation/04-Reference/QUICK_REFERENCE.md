# Quick Reference - DungeonFighter

Essential information for rapid development and problem-solving.

## Key File Locations

### Core Game Files
- **Entry Point**: `Code/Game/Program.cs`
- **Main Game Loop**: `Code/Game/Game.cs`
- **Game Menu Management**: `Code/UI/GameMenuManager.cs`
- **Game Loop Management**: `Code/Game/GameLoopManager.cs`
- **Combat System**: `Code/Combat/CombatManager.cs` (orchestrator)
- **Character System**: `Code/Entity/Character.cs` (coordinator) + specialized managers
- **Enemy System**: `Code/Entity/Enemy.cs`

### Configuration Files
- **Game Settings**: `GameData/gamesettings.json`
- **Tuning Config**: `GameData/TuningConfig.json`
- **Actions**: `GameData/Actions.json`
- **Enemies**: `GameData/Enemies.json`
- **Weapons**: `GameData/Weapons.json`
- **Armor**: `GameData/Armor.json`
- **UI Configuration**: `GameData/UIConfiguration.json`
- **Dungeon Config**: `GameData/DungeonConfig.json`
- **Rooms**: `GameData/Rooms.json`
- **Starting Gear**: `GameData/StartingGear.json`

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
- **`CombatManager.cs`**: Combat flow orchestration (refactored to use specialized managers)
- **`CombatStateManager.cs`**: Manages combat state, battle narrative, and entity management
- **`CombatTurnProcessor.cs`**: Handles turn processing logic for player, enemy, and environment entities
- **`CombatCalculator.cs`**: Damage, speed, stat calculations
- **`CombatEffects.cs`**: Status effects and environmental effects
- **`CombatResults.cs`**: Handles UI display and result formatting
- **`TurnManager.cs`**: Turn-based combat logic
- **`BattleNarrative.cs`**: Event-driven battle descriptions
- **`BattleHealthTracker.cs`**: Health tracking for battle narrative system

### Character System
- **`Character.cs`**: Main player coordinator class (refactored from 737 to 539 lines)
- **`CharacterStats.cs`**: Statistics and leveling system
- **`CharacterEquipment.cs`**: Equipment management and stat bonuses
- **`CharacterProgression.cs`**: Experience, leveling, skill progression
- **`CharacterActions.cs`**: Facade coordinator for character actions (refactored 828 → 171 lines)
  - **`ClassActionManager.cs`**: Manages class-specific actions and abilities
  - **`GearActionManager.cs`**: Manages weapon/armor actions and roll bonuses
  - **`ComboSequenceManager.cs`**: Manages combo sequences and ordering
  - **`EnvironmentActionManager.cs`**: Manages environment-specific actions
  - **`DefaultActionManager.cs`**: Ensures default/basic actions are available
- **`CharacterHealthManager.cs`**: Health management, damage, and healing logic
- **`CharacterCombatCalculator.cs`**: Combat calculations and stat computations
- **`CharacterDisplayManager.cs`**: Character information display and formatting
- **`CharacterSaveManager.cs`**: Save/load functionality
- **`CharacterEffects.cs`**: Character-specific effects and buffs/debuffs

### Enemy System
- **`Enemy.cs`**: Enemy entity with AI and combat behavior
- **`EnemyFactory.cs`**: Creates enemies based on type and level
- **`EnemyLoader.cs`**: Loads enemy data from JSON
- **`EnemyDPSCalculator.cs`**: Calculates enemy damage per second
- **`EnemyBalanceCalculator.cs`**: Balance testing and analysis

### UI System
- **`UIManager.cs`**: Centralized UI output and display with beat-based timing
- **`GameMenuManager.cs`**: Manages game menus, UI interactions, and game flow
- **`InventoryDisplayManager.cs`**: Manages all display logic for inventory, character stats, and equipment
- **`MenuConfiguration.cs`**: Centralized configuration for all menu options throughout the game
- **`TextDisplayIntegration.cs`**: Integration layer for displaying text using the new UIManager beat-based timing system
- **`TextDisplaySettings.cs`**: Configuration for text display timing and formatting
- **`UIConfiguration.cs`**: UI configuration management

### Data & Configuration
- **`ActionLoader.cs`**: Loads and manages action data
- **`JsonLoader.cs`**: Common JSON loading operations
- **`GameConfiguration.cs`**: Centralized configuration management
- **`SettingsManager.cs`**: Game settings management
- **`ItemGenerator.cs`**: Procedural item generation
- **`LootGenerator.cs`**: Procedural loot generation
- **`RoomGenerator.cs`**: Procedural room generation
- **`RoomLoader.cs`**: Loads room definitions from JSON

### Action System
- **`Action.cs`**: Base action class with properties and effects
- **`ActionSelector.cs`**: Handles action selection logic for different entity types
- **`ActionExecutor.cs`**: Handles action execution logic, damage application, and effect processing
- **`ActionFactory.cs`**: Creates and manages action instances
- **`ActionEnhancer.cs`**: Enhances action descriptions with modifiers (damage, bonuses, effects)
- **`ActionUtilities.cs`**: Shared utilities for action-related operations
- **`ActionSpeedSystem.cs`**: Intelligent delay system for optimal user experience

### World & Environment System
- **`Dungeon.cs`**: Procedurally generates themed room sequences and manages progression
- **`DungeonManager.cs`**: Manages dungeon-related operations including selection, generation, and completion rewards
- **`Environment.cs`**: Room/environment system with environmental effects
- **`EnvironmentalActionHandler.cs`**: Handles environmental actions and effects
- **`StatusEffectManager.cs`**: Manages status effects and their application
- **`DamageEffectManager.cs`**: Manages damage effects and calculations
- **`DebuffEffectManager.cs`**: Manages debuff effects and their application

### Items & Equipment System
- **`Item.cs`**: Base item class with tier scaling and properties
- **`InventoryManager.cs`**: Inventory management system
- **`ComboManager.cs`**: Manages combo sequences and bonuses
- **`BasicGearConfig.cs`**: Configuration for basic gear and equipment

### Utility Systems
- **`ErrorHandler.cs`**: Centralized error handling and logging
- **`RandomUtility.cs`**: Consistent random number generation
- **`GameConstants.cs`**: Common constants, file paths, strings
- **`DebugLogger.cs`**: Centralized debug output
- **`FlavorText.cs`**: Procedural generation of names and descriptions
- **`Dice.cs`**: Random number generation and dice rolling

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
- **Combo Threshold**: 14-20 (triggers combo mode)
- **Basic Attack**: 6-13 (normal attacks)
- **Miss Threshold**: 1-5 (failed attacks)
- **Combo Amplifier**: 1.05x base, scales with Technique

### Enemy Scaling
- **Health Scaling**: +3 per level
- **Attribute Scaling**: +1 per level, +2 for primary attribute
- **Primary Attributes**: STR (Orcs, Skeletons), AGI (Goblins, Spiders), TEC (Cultists, Wraiths)

## CharacterActions Manager System (Phase 1 Refactoring)

### Overview
The CharacterActions system was refactored from a 828-line monolith into a well-organized facade with 6 specialized managers. This system manages all character actions, abilities, and combat moves.

### Managers and Their Responsibilities

| Manager | Purpose | Key Methods |
|---------|---------|------------|
| **ClassActionManager** | Class-specific abilities | `AddClassActions()`, `RemoveClassActions()` |
| **GearActionManager** | Weapon/armor actions | `AddWeaponActions()`, `AddArmorActions()`, `ApplyRollBonus()` |
| **ComboSequenceManager** | Combo system | `AddToCombo()`, `RemoveFromCombo()`, `GetComboActions()` |
| **EnvironmentActionManager** | Environment effects | `AddEnvironmentActions()`, `ClearEnvironmentActions()` |
| **DefaultActionManager** | Basic attacks | `EnsureDefaultActions()` |
| **ActionFactory** | Action creation | `CreateBasicAttack()`, `CreateEmergencyComboAction()` |

### Usage Example

```csharp
// Initialize
var characterActions = new CharacterActions();

// Add class-specific abilities
characterActions.AddClassActions(actor, progression, weaponType);

// Add weapon actions
characterActions.AddWeaponActions(actor, weapon);

// Manage combo
characterActions.AddToCombo(action);
var combos = characterActions.GetComboActions();
```

### Testing
- **Unit Tests**: 122 comprehensive tests across 7 test suites
- **Code Coverage**: 95%+
- **Test Files**: `Code/Tests/Unit/CharacterActionsIntegrationTest.cs` and manager-specific test files

### Refactoring Metrics
- **Size Reduction**: 79% (828 → 171 lines)
- **Test Count**: 122 tests
- **Backward Compatibility**: 100%
- **Files Created**: 7 new manager/utility classes

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
public static string ExecuteAction(Entity source, Entity target, Environment? environment = null, Action? lastPlayerAction = null, Action? forcedAction = null, BattleNarrative? battleNarrative = null)
{
    // Select action based on entity type
    var selectedAction = forcedAction ?? ActionSelector.SelectActionByEntityType(source);
    if (selectedAction == null)
        return $"[{source.Name}] has no actions available.";
    
    // Calculate roll bonus and attack roll
    int rollBonus = ActionUtilities.CalculateRollBonus(source, selectedAction);
    int baseRoll = ActionSelector.GetActionRoll(source);
    int attackRoll = baseRoll + rollBonus;
    
    // Execute action with all effects
    return ExecuteActionInternal(source, target, environment, lastPlayerAction, forcedAction, battleNarrative, actionResults);
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
- **Character Tests**: Character creation, progression, and equipment
- **Combat Tests**: Combat mechanics, damage calculations, and turn management
- **Balance Tests**: Enemy balance analysis and DPS calculations
- **Data Loading Tests**: JSON loading and configuration validation
- **Advanced System Tests**: Integration tests and system interactions

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

// Debug combat actions
DebugLogger.WriteCombatDebug("CombatManager", "Starting combat");

// Check UI configuration
UIManager.ReloadConfiguration();
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
- Settings: `GameData/gamesettings.json`

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

## Code Analysis Commands

### Get Total Lines and Breakdown
Use this command to get comprehensive code statistics:

```powershell
$files = Get-ChildItem -Path "Code" -Recurse -Filter "*.cs"; $totalLines = 0; $commentLines = 0; $emptyLines = 0; foreach ($file in $files) { $content = Get-Content $file.FullName; $totalLines += $content.Count; foreach ($line in $content) { $trimmed = $line.Trim(); if ($trimmed -eq "") { $emptyLines++ } elseif ($trimmed.StartsWith("//") -or $trimmed.StartsWith("/*") -or $trimmed.StartsWith("*")) { $commentLines++ } } }; Write-Host "Total lines: $totalLines"; Write-Host "Comment lines: $commentLines"; Write-Host "Empty lines: $emptyLines"; Write-Host "Code lines: $($totalLines - $commentLines - $emptyLines)"; Write-Host "Comment percentage: $([math]::Round(($commentLines / $totalLines) * 100, 1))%"
```

### Get Lines by Directory
Use this command to see line counts by system:

```powershell
$directories = @("Actions", "Combat", "Config", "Data", "Entity", "Game", "Items", "UI", "Utils", "World"); foreach ($dir in $directories) { $files = Get-ChildItem -Path "Code\$dir" -Filter "*.cs" -ErrorAction SilentlyContinue; if ($files) { $lines = ($files | Get-Content | Measure-Object -Line).Lines; Write-Host "$dir`: $lines lines" } }
```

### Get Individual File Line Counts
Use this command to see line counts for each file:

```powershell
$files = Get-ChildItem -Path "Code" -Recurse -Filter "*.cs"; foreach ($file in $files) { $lines = (Get-Content $file.FullName | Measure-Object -Line).Lines; Write-Host "$($file.Name): $lines lines" }
```

## Related Documentation

- **`ARCHITECTURE.md`**: Detailed system architecture and design patterns
- **`CODE_PATTERNS.md`**: Code patterns, conventions, and best practices
- **`PROBLEM_SOLUTIONS.md`**: Solutions to common problems
- **`DEBUGGING_GUIDE.md`**: Debugging techniques and tools
- **`DEVELOPMENT_WORKFLOW.md`**: Step-by-step development process

---

*This reference should be updated as the codebase evolves.*
