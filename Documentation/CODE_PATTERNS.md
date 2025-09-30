# Code Patterns & Conventions - DungeonFighter-v2

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

## Architectural Patterns

### 1. Manager Pattern
**Purpose**: Orchestrate complex subsystems and provide clean interfaces

**Example**:
```csharp
public class CombatManager
{
    private readonly TurnManager _turnManager;
    private readonly CombatCalculator _calculator;
    private readonly CombatEffects _effects;
    
    public CombatResult ExecuteCombat(Character player, Enemy enemy)
    {
        // Orchestrate combat flow
        var result = new CombatResult();
        
        while (!IsCombatOver(player, enemy))
        {
            var action = _turnManager.GetNextAction();
            var damage = _calculator.CalculateDamage(action);
            _effects.ApplyEffects(action);
            
            result.AddAction(action);
        }
        
        return result;
    }
}
```

**Usage**: `CombatManager`, `DungeonManager`, `InventoryManager`

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
```

**Usage**: `EnemyFactory`, `ItemGenerator`

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
public abstract class Action
{
    public abstract void Execute(Entity source, Entity target);
    public abstract int CalculateDamage(Entity source, Entity target);
}

public class BasicAttack : Action
{
    public override void Execute(Entity source, Entity target)
    {
        var damage = CalculateDamage(source, target);
        target.TakeDamage(damage);
    }
    
    public override int CalculateDamage(Entity source, Entity target)
    {
        return source.Strength + source.GetHighestAttribute();
    }
}
```

**Usage**: `Action` subclasses, `CharacterActions`

### 5. Observer Pattern
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

## UI Patterns

### 1. Display Manager Pattern
**Purpose**: Centralized UI output management

**Example**:
```csharp
public static class UIManager
{
    public static void DisplayCombatResult(Entity source, Entity target, Action action, int damage)
    {
        var message = FormatCombatMessage(source, target, action, damage);
        Console.WriteLine(message);
    }
    
    public static void DisplayCharacterStats(Character character)
    {
        var stats = FormatCharacterStats(character);
        Console.WriteLine(stats);
    }
    
    private static string FormatCombatMessage(Entity source, Entity target, Action action, int damage)
    {
        return $"[{source.Name}] uses [{action.Name}] on [{target.Name}]: deals {damage} damage";
    }
}
```

### 2. Menu Pattern
**Purpose**: Consistent menu navigation

**Example**:
```csharp
public class MenuManager
{
    private readonly List<MenuOption> _options = new List<MenuOption>();
    
    public void AddOption(string text, Action action)
    {
        _options.Add(new MenuOption(text, action));
    }
    
    public void DisplayMenu()
    {
        for (int i = 0; i < _options.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {_options[i].Text}");
        }
    }
    
    public void HandleInput(int choice)
    {
        if (choice >= 1 && choice <= _options.Count)
        {
            _options[choice - 1].Action();
        }
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
