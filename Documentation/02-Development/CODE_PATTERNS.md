# Code Patterns & Conventions - DungeonFighter

Documentation of common code patterns, conventions, and architectural decisions used throughout the codebase.

## Naming Conventions

### Classes
- **PascalCase**: `Character`, `CombatManager`, `EnemyFactory`
- **Descriptive Names**: Clear purpose and responsibility
- **Manager Suffix**: For orchestration classes (`CombatManager`, `DungeonManager`)
- **Loader Suffix**: For data loading classes (`ActionLoader`, `EnemyLoader`)
- **Calculator Suffix**: For calculation classes (`CombatCalculator`, `EnemyDPSCalculator`)

### Methods
- **PascalCase**: `CalculateDamage()`, `LoadActions()`, `ExecuteAction()`
- **Verb-Noun Pattern**: `GetTotalArmor()`, `SetCharacterStats()`, `CreateEnemy()`
- **Boolean Methods**: `IsAlive()`, `CanExecuteAction()`, `HasEquipment()`

### Properties
- **PascalCase**: `Health`, `Strength`, `ActionPool`
- **Get/Set Pattern**: `GetTotalDamage()`, `SetHealth()`
- **Computed Properties**: `MaxHealth`, `TotalArmor`, `IsAlive`

### Variables
- **camelCase**: `currentHealth`, `enemyCount`, `damageDealt`
- **Descriptive Names**: `playerCharacter`, `selectedAction`, `combatResult`

### Constants
- **PascalCase**: `MAX_LEVEL`, `BASE_HEALTH`, `CRITICAL_HIT_THRESHOLD`
- **UPPER_CASE**: For truly constant values

### Item Naming Patterns
- **Stat Bonus Integration**: Stat bonuses are automatically included in item names
- **Name Structure**: `[Rarity] [Stat Bonuses] [Modifications] [Base Item Name]`
- **Stat Bonus Names**: Use descriptive suffixes from `StatBonuses.json` (e.g., "of Protection", "of Vitality")
- **Multiple Bonuses**: Concatenate multiple stat bonus names with spaces
- **Example**: "Rare Steel Sword of Power of Protection" (has +3 damage and +2 armor bonuses)

## Architectural Patterns

### 1. Registry Pattern (NEW)
**Purpose**: Provide extensible, maintainable effect systems using strategy pattern

**Example**:
```csharp
public class EffectHandlerRegistry
{
    private readonly Dictionary<string, IEffectHandler> _handlers = new();
    
    public EffectHandlerRegistry()
    {
        RegisterHandler("Bleed", new BleedEffectHandler());
        RegisterHandler("Weaken", new WeakenEffectHandler());
        // ... other handlers
    }
    
    public void ApplyEffect(string effectType, Entity target, int intensity)
    {
        if (_handlers.TryGetValue(effectType, out var handler))
        {
            handler.ApplyEffect(target, intensity);
        }
    }
}
```

**Benefits**:
- Eliminates large switch statements
- Easy to add new effect types
- Testable individual handlers
- Follows Open/Closed Principle

### 2. Facade Pattern (NEW)
**Purpose**: Provide simplified interface to complex subsystems

**Example**:
```csharp
public class CharacterFacade
{
    private readonly Character _character;
    private readonly EquipmentManager _equipmentManager;
    private readonly LevelUpManager _levelUpManager;
    
    public CharacterFacade(Character character)
    {
        _character = character;
        _equipmentManager = new EquipmentManager(character);
        _levelUpManager = new LevelUpManager(character);
    }
    
    // Simplified interface methods
    public void AddXP(int amount) => _levelUpManager.AddXP(amount);
    public Item? EquipItem(Item item, string slot) => _equipmentManager.EquipItem(item, slot);
}
```

**Benefits**:
- Hides complexity of subsystems
- Provides consistent interface
- Reduces coupling between components

### 3. Builder Pattern (NEW)
**Purpose**: Handle complex object initialization without cluttering entity classes

**Example**:
```csharp
public class CharacterBuilder
{
    private string? _name;
    private int _level = 1;
    private int _strength = 8;
    // ... other properties
    
    public CharacterBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    
    public CharacterBuilder WithLevel(int level)
    {
        _level = level;
        return this;
    }
    
    public Character Build()
    {
        var character = new Character(_name, _level);
        // Complex initialization logic here
        return character;
    }
}
```

**Benefits**:
- Separates construction from representation
- Fluent interface for readability
- Handles complex initialization logic

### 4. Manager Pattern
**Purpose**: Orchestrate complex subsystems and provide clean interfaces

**Example**:
```csharp
public class CombatManager
{
    // Specialized managers using composition pattern
    private readonly CombatStateManager stateManager;
    private readonly CombatTurnHandlerSimplified turnHandler;

    public CombatManager()
    {
        stateManager = new CombatStateManager();
        turnProcessor = new CombatTurnProcessor(stateManager);
    }

    public bool RunCombat(Character player, Enemy currentEnemy, Environment room)
    {
        // Start battle narrative and initialize action speed system
        StartBattleNarrative(player.Name, currentEnemy.Name, room.Name, player.CurrentHealth, currentEnemy.CurrentHealth);
        InitializeCombatEntities(player, currentEnemy, room);
        
        // Combat Loop with action speed system
        while (player.IsAlive && currentEnemy.IsAlive)
        {
            Entity? nextEntity = GetNextEntityToAct();
            if (nextEntity == null) break;
            
            // Process turn based on entity type
            if (nextEntity is Character character)
            {
                if (!turnProcessor.ProcessPlayerTurn(character, currentEnemy, room))
                    return false; // Player died
            }
            else if (nextEntity is Enemy enemy)
            {
                turnProcessor.ProcessEnemyTurn(enemy, player, room);
            }
            else if (nextEntity is Environment environment)
            {
                turnProcessor.ProcessEnvironmentTurn(environment, player, currentEnemy);
            }
        }
        
        return player.IsAlive;
    }
}
```

**Usage**: `CombatManager`, `DungeonManager`, `InventoryManager`, `CharacterHealthManager`, `CharacterCombatCalculator`, `CharacterDisplayManager`, `CombatStateManager`, `CombatTurnProcessor`

**Advanced Example - CharacterActions Refactoring**:
The CharacterActions system demonstrates the Manager Pattern at scale, refactored from a 828-line monolith into a well-organized facade with 6 specialized managers:

```csharp
public class CharacterActions
{
    // Specialized managers - each with single responsibility
    private readonly ClassActionManager _classManager;
    private readonly GearActionManager _gearManager;
    private readonly ComboSequenceManager _comboManager;
    private readonly EnvironmentActionManager _environmentManager;
    private readonly DefaultActionManager _defaultManager;
    private readonly ActionFactory _actionFactory;

    public CharacterActions()
    {
        _classManager = new ClassActionManager();
        _gearManager = new GearActionManager();
        _comboManager = new ComboSequenceManager();
        _environmentManager = new EnvironmentActionManager();
        _defaultManager = new DefaultActionManager();
        _actionFactory = new ActionFactory();
    }

    // Public API delegates to appropriate managers
    public void AddClassActions(Actor actor, CharacterProgression progression, WeaponType? weaponType)
        => _classManager.AddClassActions(actor, progression, weaponType);

    public void AddWeaponActions(Actor actor, WeaponItem weapon)
        => _gearManager.AddWeaponActions(actor, weapon);

    public void AddToCombo(Action action)
        => _comboManager.AddToCombo(action);

    public void AddEnvironmentActions(Actor actor, Environment environment)
        => _environmentManager.AddEnvironmentActions(actor, environment);

    public List<Action> GetComboActions()
        => _comboManager.GetComboActions();
}
```

**Refactoring Achievement**:
- **Size Reduction**: 828 lines → 171 lines main file (79% reduction)
- **Testability**: 122 comprehensive unit tests (95%+ coverage)
- **Maintainability**: 6 focused managers, each with single responsibility
- **Backward Compatibility**: 100% - all existing code works unchanged
- **Managers Created**:
  - `ClassActionManager` - Class-specific actions (Barbarian, Warrior, Rogue, Wizard)
  - `GearActionManager` - Weapon/armor actions and roll bonuses
  - `ComboSequenceManager` - Combo sequence management and ordering
  - `EnvironmentActionManager` - Environment-specific actions
  - `DefaultActionManager` - Default and basic actions
  - `ActionFactory` - Action object creation

**Why This Pattern Works**:
- Each manager handles ONE responsibility
- Facade provides simple public interface
- Managers are independently testable
- Easy to modify individual systems
- Clear delegation and composition

### 2. Factory Pattern
**Purpose**: Create objects with complex initialization logic

**Example**:
```csharp
public class EnemyFactory
{
    public static Enemy CreateEnemy(string enemyType, int level)
    {
        var enemyData = EnemyLoader.LoadEnemyData(enemyType);
        var enemy = new Enemy(enemyData);
        
        // Apply level scaling
        enemy.ScaleToLevel(level);
        
        // Apply primary attribute bonus
        enemy.ApplyPrimaryAttributeBonus();
        
        return enemy;
    }
}

public class ActionFactory
{
    public static Action CreateAction(string actionName)
    {
        var actionData = ActionLoader.GetAction(actionName);
        if (actionData == null)
        {
            ErrorHandler.LogError($"Action not found: {actionName}");
            return GetDefaultAction();
        }
        
        return new Action(actionData);
    }
    
    private static Action GetDefaultAction()
    {
        return new Action("BASIC ATTACK", "A basic attack", 1.0, new List<string>());
    }
}
```

**Usage**: `EnemyFactory`, `ItemGenerator`, `ActionFactory`

### 3. Singleton Pattern
**Purpose**: Ensure single instance for global resources

**Example**:
```csharp
public class GameTicker
{
    private static GameTicker _instance;
    private static readonly object _lock = new object();
    
    public static GameTicker Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new GameTicker();
                }
            }
            return _instance;
        }
    }
    
    private GameTicker() { }
}
```

**Usage**: `GameTicker`, `RandomUtility`

### 4. Strategy Pattern
**Purpose**: Different behaviors for similar operations

**Example**:
```csharp
public static class ActionSelector
{
    /// <summary>
    /// Selects an action based on entity type - heroes use roll-based logic, enemies use random selection
    /// </summary>
    public static Action? SelectActionByEntityType(Entity source)
    {
        // Heroes/Characters use advanced roll-based system with combos
        if (source is Character character && !(character is Enemy))
        {
            return SelectActionBasedOnRoll(source);
        }
        // Enemies use simple random probability-based selection
        else
        {
            return SelectEnemyActionBasedOnRoll(source);
        }
    }
    
    private static Action? SelectActionBasedOnRoll(Entity source)
    {
        // Roll-based selection for heroes (6+ = BASIC ATTACK, 14+ = COMBO)
        int roll = RandomUtility.Roll(1, 20);
        
        if (roll >= 14)
        {
            return GetComboAction(source);
        }
        else if (roll >= 6)
        {
            return GetBasicAttack(source);
        }
        else
        {
            return null; // Miss
        }
    }
    
    private static Action? SelectEnemyActionBasedOnRoll(Entity source)
    {
        // Simple random selection for enemies
        if (source.ActionPool.Count == 0) return null;
        
        var randomAction = source.ActionPool[RandomUtility.Roll(0, source.ActionPool.Count)];
        return randomAction.action;
    }
}
```

**Usage**: `ActionSelector`, `ActionExecutor`, `CharacterActions`

### 5. Composition Pattern
**Purpose**: Use composition over inheritance to build complex objects from simpler components

**Example**:
```csharp
public class Character : Entity, IComboMemory
{
    // Core components using composition
    public CharacterStats Stats { get; private set; }
    public CharacterEffects Effects { get; private set; }
    public CharacterEquipment Equipment { get; private set; }
    public CharacterProgression Progression { get; private set; }
    public CharacterActions Actions { get; private set; }

    // NEW: Extracted managers using composition
    public CharacterHealthManager Health { get; private set; }
    public CharacterCombatCalculator Combat { get; private set; }
    public CharacterDisplayManager Display { get; private set; }

    public Character(string? name = null, int level = 1) : base(name ?? FlavorText.GenerateCharacterName())
    {
        // Initialize all components
        Stats = new CharacterStats(level);
        Effects = new CharacterEffects();
        Equipment = new CharacterEquipment();
        Progression = new CharacterProgression(level);
        Actions = new CharacterActions();

        // Initialize new managers
        Health = new CharacterHealthManager(this);
        Combat = new CharacterCombatCalculator(this);
        Display = new CharacterDisplayManager(this);
    }

    // Delegate complex operations to specialized managers
    public void TakeDamage(int amount) => Health.TakeDamage(amount);
    public double GetComboAmplifier() => Combat.GetComboAmplifier();
    public void DisplayCharacterInfo() => Display.DisplayCharacterInfo();
}
```

**Benefits**:
- **Single Responsibility**: Each manager handles one specific concern
- **Maintainability**: Changes to health logic only affect `CharacterHealthManager`
- **Testability**: Each manager can be unit tested independently
- **Flexibility**: Easy to add new managers or modify existing ones
- **Reduced Complexity**: Main class becomes a coordinator instead of doing everything

**Usage**: `Character` class with `CharacterHealthManager`, `CharacterCombatCalculator`, `CharacterDisplayManager`

### 6. Observer Pattern
**Purpose**: Notify multiple objects of state changes

**Example**:
```csharp
public class BattleHealthTracker
{
    private readonly List<IHealthObserver> _observers = new List<IHealthObserver>();
    
    public void RegisterObserver(IHealthObserver observer)
    {
        _observers.Add(observer);
    }
    
    public void OnHealthChanged(Entity entity, int oldHealth, int newHealth)
    {
        foreach (var observer in _observers)
        {
            observer.OnHealthChanged(entity, oldHealth, newHealth);
        }
    }
}
```

**Usage**: `BattleHealthTracker`, `CombatResults`

### 7. Integration Pattern
**Purpose**: Provide a clean interface between different systems

**Example**:
```csharp
public static class TextDisplayIntegration
{
    /// <summary>
    /// Integration layer for displaying text using the new UIManager beat-based timing system
    /// Provides a clean interface for game components to display messages
    /// </summary>
    public static void DisplayCombatAction(string combatResult, List<string> narrativeMessages, List<string> statusEffects, string entityName)
    {
        // Use the new UIManager system with beat-based timing instead of the old TextDisplaySystem
        UIManager.WriteCombatLine(combatResult);
        
        // Display narrative messages with appropriate delays
        foreach (var narrative in narrativeMessages)
        {
            UIManager.WriteNarrativeLine(narrative);
        }
        
        // Display status effects with shorter delays
        foreach (var effect in statusEffects)
        {
            UIManager.WriteStatusLine(effect);
        }
    }
    
    public static void DisplayMenu(string title, List<string> options)
    {
        // Reset menu delay counter at the start of menu display
        UIManager.ResetMenuDelayCounter();
        
        UIManager.WriteTitleLine(title);
        
        // Add separator line between title and options
        if (!string.IsNullOrWhiteSpace(title))
        {
            UIManager.WriteMenuLine(new string('-', Math.Max(8, title.Length)));
        }
        
        foreach (var option in options)
        {
            UIManager.WriteMenuLine(option);
        }
        
        // Reset menu delay counter after menu display is complete
        UIManager.ResetMenuDelayCounter();
    }
}
```

**Usage**: `TextDisplayIntegration`, `MenuConfiguration`

## Data Access Patterns

### 1. JSON Loading Pattern
**Purpose**: Consistent JSON data loading with error handling

**Example**:
```csharp
public static class JsonLoader
{
    public static T LoadJson<T>(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                ErrorHandler.LogError($"File not found: {filePath}");
                return default(T);
            }
            
            string json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch (JsonException ex)
        {
            ErrorHandler.LogError($"JSON parsing error in {filePath}: {ex.Message}");
            return default(T);
        }
        catch (Exception ex)
        {
            ErrorHandler.LogError($"Error loading {filePath}: {ex.Message}");
            return default(T);
        }
    }
}
```

### 2. Configuration Pattern
**Purpose**: Centralized configuration management

**Example**:
```csharp
public class GameConfiguration
{
    private static GameConfiguration _instance;
    private readonly Dictionary<string, object> _config;
    
    public static GameConfiguration Instance => _instance ??= new GameConfiguration();
    
    private GameConfiguration()
    {
        _config = LoadConfiguration();
    }
    
    public T GetValue<T>(string key, T defaultValue = default(T))
    {
        if (_config.TryGetValue(key, out var value))
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        return defaultValue;
    }
}
```

## Error Handling Patterns

### 1. Centralized Error Handling
**Purpose**: Consistent error logging and handling

**Example**:
```csharp
public static class ErrorHandler
{
    private static readonly List<string> _errorLog = new List<string>();
    
    public static void LogError(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var logEntry = $"[{timestamp}] ERROR: {message}";
        
        _errorLog.Add(logEntry);
        Console.WriteLine(logEntry);
        
        // Optionally write to file
        WriteToLogFile(logEntry);
    }
    
    public static void LogWarning(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var logEntry = $"[{timestamp}] WARNING: {message}";
        
        Console.WriteLine(logEntry);
    }
}
```

### 2. Graceful Degradation
**Purpose**: Continue operation when non-critical errors occur

**Example**:
```csharp
public List<Action> LoadActions()
{
    try
    {
        return JsonLoader.LoadJson<List<Action>>("GameData/Actions.json");
    }
    catch (Exception ex)
    {
        ErrorHandler.LogError($"Failed to load actions: {ex.Message}");
        
        // Return default actions as fallback
        return GetDefaultActions();
    }
}
```

## Validation Patterns

### 1. Input Validation
**Purpose**: Validate data before processing

**Example**:
```csharp
public bool EquipItem(Item item)
{
    if (item == null)
    {
        ErrorHandler.LogWarning("Cannot equip null item");
        return false;
    }
    
    if (!CanEquipItem(item))
    {
        ErrorHandler.LogWarning($"Cannot equip {item.Name}: requirements not met");
        return false;
    }
    
    // Proceed with equipping
    return true;
}
```

### 2. State Validation
**Purpose**: Ensure object is in valid state before operations

**Example**:
```csharp
public void ExecuteAction(Action action)
{
    if (!IsAlive)
    {
        ErrorHandler.LogWarning("Cannot execute action: character is dead");
        return;
    }
    
    if (action == null)
    {
        ErrorHandler.LogWarning("Cannot execute null action");
        return;
    }
    
    if (!CanExecuteAction(action))
    {
        ErrorHandler.LogWarning($"Cannot execute {action.Name}: requirements not met");
        return;
    }
    
    // Execute action
}
```

## Performance Patterns

### 1. Lazy Loading
**Purpose**: Load data only when needed

**Example**:
```csharp
public class ActionLoader
{
    private static Dictionary<string, Action> _actionCache;
    
    public static Action GetAction(string actionName)
    {
        if (_actionCache == null)
        {
            LoadAllActions();
        }
        
        return _actionCache.TryGetValue(actionName, out var action) ? action : null;
    }
    
    private static void LoadAllActions()
    {
        _actionCache = new Dictionary<string, Action>();
        var actions = JsonLoader.LoadJson<List<Action>>("GameData/Actions.json");
        
        foreach (var action in actions)
        {
            _actionCache[action.Name] = action;
        }
    }
}
```

### 2. Object Pooling
**Purpose**: Reuse objects to reduce garbage collection

**Example**:
```csharp
public class EnemyPool
{
    private readonly Queue<Enemy> _availableEnemies = new Queue<Enemy>();
    private readonly List<Enemy> _allEnemies = new List<Enemy>();
    
    public Enemy GetEnemy(string enemyType, int level)
    {
        Enemy enemy;
        
        if (_availableEnemies.Count > 0)
        {
            enemy = _availableEnemies.Dequeue();
            enemy.Reset(enemyType, level);
        }
        else
        {
            enemy = EnemyFactory.CreateEnemy(enemyType, level);
            _allEnemies.Add(enemy);
        }
        
        return enemy;
    }
    
    public void ReturnEnemy(Enemy enemy)
    {
        enemy.Reset();
        _availableEnemies.Enqueue(enemy);
    }
}
```

### 3. Non-Blocking Delays
**Purpose**: Avoid blocking the UI thread or game loop with synchronous delays

**CRITICAL RULE**: **NEVER use `Thread.Sleep()` for delays** - it blocks the thread and causes UI freezing, unresponsive gameplay, and poor user experience.

**Why Avoid Thread.Sleep**:
- Blocks the entire thread, freezing the UI
- Makes the application unresponsive
- Prevents other operations from executing
- Causes poor user experience in GUI applications
- Can cause timing issues in game loops

**Correct Approaches**:

**For Async Methods** - Use `Task.Delay()`:
```csharp
// ✅ CORRECT: Non-blocking async delay
public async Task ProcessCombatTurn()
{
    await Task.Delay(1000); // Non-blocking, allows other operations
    // Continue processing...
}
```

**For UI Timing** - Use async timers or the beat-based timing system:
```csharp
// ✅ CORRECT: Use the existing beat-based timing system
public static void WriteCombatLine(string message)
{
    Console.WriteLine(message);
    // Timing handled by rendering system, not blocking delays
    ApplyDelay(UIMessageType.Combat);
}

// ✅ CORRECT: Use async timers for UI animations
private async Task AnimateText()
{
    var timer = new System.Timers.Timer(100);
    timer.Elapsed += (s, e) => UpdateAnimation();
    timer.Start();
    await Task.Delay(1000);
    timer.Stop();
}
```

**For Game Loops** - Use time-based updates instead of fixed delays:
```csharp
// ✅ CORRECT: Time-based updates
private DateTime _lastUpdate = DateTime.Now;
private const int UpdateIntervalMs = 100;

public void Update()
{
    var now = DateTime.Now;
    if ((now - _lastUpdate).TotalMilliseconds >= UpdateIntervalMs)
    {
        ProcessUpdate();
        _lastUpdate = now;
    }
}
```

**Legacy Code Note**: Some existing code may still use `Thread.Sleep()` for console-only operations. When refactoring or creating new code, always use non-blocking alternatives.

## Testing Patterns

### 1. Test Data Builder
**Purpose**: Create test objects with specific configurations

**Example**:
```csharp
public class CharacterBuilder
{
    private string _name = "TestCharacter";
    private int _level = 1;
    private int _strength = 10;
    private int _agility = 10;
    
    public CharacterBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    
    public CharacterBuilder WithLevel(int level)
    {
        _level = level;
        return this;
    }
    
    public CharacterBuilder WithStrength(int strength)
    {
        _strength = strength;
        return this;
    }
    
    public Character Build()
    {
        var character = new Character(_name);
        character.SetLevel(_level);
        character.SetStrength(_strength);
        character.SetAgility(_agility);
        return character;
    }
}

// Usage in tests
var character = new CharacterBuilder()
    .WithName("TestHero")
    .WithLevel(5)
    .WithStrength(15)
    .Build();
```

### 2. Mock Objects
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
var mockRandom = new MockRandom(15);
var dice = new Dice(mockRandom);
var result = dice.Roll();
Assert.AreEqual(15, result);
```

### 3. Arrange-Act-Assert (AAA) Pattern
**Purpose**: Write clear, maintainable tests with distinct phases

**Pattern Structure**:
```csharp
// Arrange: Setup test data and objects
// Act: Execute the code being tested
// Assert: Verify the results
```

**Example**:
```csharp
[Fact]
public void AddClassActions_BarbarianClass_AddsBarbarianSkills()
{
    // Arrange
    var actor = new Actor("Barbarian");
    var progression = new CharacterProgression { CurrentClass = "Barbarian" };
    var manager = new ClassActionManager();
    
    // Act
    manager.AddClassActions(actor, progression, null);
    
    // Assert
    Assert.Contains("Rage", actor.Actions.Select(a => a.Name));
    Assert.Contains("Cleave", actor.Actions.Select(a => a.Name));
}
```

**Benefits**:
- Tests are easy to read and understand
- Distinct phases make debugging easier
- Clear expectations for test behavior

### 4. Manager Testing Approach
**Purpose**: Test complex manager systems comprehensively

**Strategy for CharacterActions System**:

Each manager has three test levels:

**Unit Tests** (Test individual methods):
```csharp
[Fact]
public void AddWeaponActions_SteelSword_AppliesCorrectActions()
{
    var actor = new Actor("Warrior");
    var weapon = new WeaponItem { Name = "Steel Sword", Type = WeaponType.Sword };
    var manager = new GearActionManager();
    
    manager.AddWeaponActions(actor, weapon);
    
    Assert.NotEmpty(actor.Actions);
    Assert.True(actor.Actions.Any(a => a.Name.Contains("Slash")));
}
```

**Integration Tests** (Test manager interactions):
```csharp
[Fact]
public void CharacterActions_CompleteWorkflow_HandlesBothClassAndGear()
{
    var characterActions = new CharacterActions();
    var actor = new Actor("Warrior");
    var progression = new CharacterProgression { CurrentClass = "Warrior" };
    var weapon = new WeaponItem { Name = "Steel Sword", Type = WeaponType.Sword };
    
    characterActions.AddClassActions(actor, progression, WeaponType.Sword);
    characterActions.AddWeaponActions(actor, weapon);
    
    Assert.Contains("Parry", actor.Actions.Select(a => a.Name)); // From class
    Assert.Contains("Slash", actor.Actions.Select(a => a.Name)); // From weapon
}
```

**End-to-End Tests** (Test complete scenarios):
```csharp
[Fact]
public void CharacterActions_FullCharacterSetup_CreatesCompleteActionSet()
{
    var characterActions = new CharacterActions();
    var actor = new Actor("Warrior");
    
    characterActions.InitializeCharacter(actor);
    characterActions.AddClassActions(actor, progression, WeaponType.Sword);
    characterActions.AddWeaponActions(actor, weapon);
    characterActions.AddEnvironmentActions(actor, environment);
    
    Assert.True(actor.Actions.Count > 5);
    Assert.NotNull(characterActions.GetComboActions());
    Assert.True(characterActions.GetComboActions().Count > 0);
}
```

**Test Coverage Metrics**:
- Unit Tests: 70-80 tests for individual managers
- Integration Tests: 20-30 tests for interactions
- End-to-End Tests: 10-15 tests for complete workflows
- Target Coverage: 95%+

## UI Patterns

### 1. Beat-Based Timing Pattern
**Purpose**: Consistent, configurable UI timing with beat-based delays

**Example**:
```csharp
public static class UIManager
{
    private static UIMessageType _currentMessageType = UIMessageType.System;
    private static int _menuDelayCounter = 0;
    
    public static void WriteCombatLine(string message)
    {
        Console.WriteLine(message);
        ApplyDelay(UIMessageType.Combat);
    }
    
    public static void WriteNarrativeLine(string message)
    {
        Console.WriteLine(message);
        ApplyDelay(UIMessageType.Narrative);
    }
    
    public static void WriteTitleLine(string message)
    {
        Console.WriteLine(message);
        ApplyDelay(UIMessageType.Title);
    }
    
    private static void ApplyDelay(UIMessageType messageType)
    {
        if (!UIConfiguration.Instance.EnableDelays) return;
        
        var delay = CalculateDelay(messageType);
        if (delay > 0)
        {
            Thread.Sleep(delay);
        }
    }
    
    private static int CalculateDelay(UIMessageType messageType)
    {
        var config = UIConfiguration.Instance;
        var baseBeat = config.BeatTiming.BaseBeatLengthMs;
        var multiplier = config.BeatTiming.BeatMultipliers[messageType.ToString()];
        
        return (int)(baseBeat * multiplier);
    }
}
```

### 2. Menu Configuration Pattern
**Purpose**: Centralized menu option management

**Example**:
```csharp
public static class MenuConfiguration
{
    /// <summary>
    /// Gets the main menu options
    /// </summary>
    public static List<string> GetMainMenuOptions(bool hasSavedGame = false, string? characterName = null, int characterLevel = 0)
    {
        var options = new List<string>
        {
            "1. New Game"
        };
        
        if (hasSavedGame && characterName != null)
        {
            options.Add($"2. Load Game - *{characterName}*");
        }
        else
        {
            options.Add("2. Load Game");
        }
        
        options.Add("3. Settings");
        options.Add("0. Exit");
        options.Add(""); // Blank line before prompt
        
        return options;
    }
    
    public static List<string> GetGameMenuOptions()
    {
        return new List<string>
        {
            "1. Choose a Dungeon",
            "2. Inventory", 
            "0. Exit",
            "" // Blank line before prompt
        };
    }
}
```

### 3. Text Display Integration Pattern
**Purpose**: Clean interface between game logic and UI display

**Example**:
```csharp
public static class TextDisplayIntegration
{
    public static void DisplaySystem(string message)
    {
        UIManager.WriteSystemLine(message);
    }
    
    public static void DisplayTitle(string message)
    {
        UIManager.WriteTitleLine(message);
    }
    
    public static void DisplayBlankLine()
    {
        UIManager.WriteBlankLine();
    }
    
    public static void ResetForNewBattle()
    {
        UIManager.ResetForNewBattle();
    }
}
```

## Configuration Patterns

### 1. Dynamic Configuration
**Purpose**: Runtime configuration changes

**Example**:
```csharp
public class TuningConfig
{
    private static TuningConfig _instance;
    private readonly Dictionary<string, object> _parameters;
    
    public static TuningConfig Instance => _instance ??= new TuningConfig();
    
    private TuningConfig()
    {
        _parameters = LoadParameters();
    }
    
    public void UpdateParameter(string key, object value)
    {
        _parameters[key] = value;
        OnParameterChanged?.Invoke(key, value);
    }
    
    public event Action<string, object> OnParameterChanged;
}
```

### 2. Formula Evaluation
**Purpose**: Dynamic mathematical expressions

**Example**:
```csharp
public class FormulaEvaluator
{
    public static double Evaluate(string formula, Dictionary<string, double> variables)
    {
        // Replace variables with values
        foreach (var variable in variables)
        {
            formula = formula.Replace(variable.Key, variable.Value.ToString());
        }
        
        // Evaluate mathematical expression
        return EvaluateExpression(formula);
    }
}
```

## Best Practices

### 1. Single Responsibility Principle
- Each class should have one reason to change
- Keep methods focused on single tasks
- Separate concerns clearly

### 2. Dependency Injection
- Pass dependencies through constructors
- Use interfaces for testability
- Avoid static dependencies where possible

### 3. Immutable Data
- Use readonly fields where possible
- Create new objects instead of modifying existing ones
- Prefer value types for simple data

### 4. Error Handling
- Fail fast with clear error messages
- Use appropriate exception types
- Log errors with sufficient context

### 5. Performance Considerations
- Use appropriate data structures
- Avoid unnecessary object creation
- Cache expensive calculations
- Use lazy loading for large datasets

## Related Documentation

- **`ARCHITECTURE.md`**: System architecture and design patterns
- **`DEVELOPMENT_WORKFLOW.md`**: Development process and best practices
- **`TESTING_STRATEGY.md`**: Testing patterns and approaches
- **`PERFORMANCE_NOTES.md`**: Performance patterns and optimizations
- **`QUICK_REFERENCE.md`**: Quick lookup for patterns and conventions

---

*This document should be updated as new patterns are established or existing patterns evolve.*
