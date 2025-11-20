# Environment Managers - Quick Reference

## Quick Navigation

- [EnvironmentalActionInitializer](#environmentalactioninitializer) - Action loading
- [EnemyGenerationManager](#enemygeneration manager) - Enemy spawning
- [EnvironmentCombatStateManager](#environmentcombatstate manager) - Combat state
- [EnvironmentEffectManager](#environmenteffectmanager) - Effect application

---

## EnvironmentalActionInitializer

**File**: `Code/World/EnvironmentalActionInitializer.cs`

**Purpose**: Load and initialize environmental actions for a room

### Constructor
```csharp
var initializer = new EnvironmentalActionInitializer(theme: string, roomType: string);
```

### Main Method
```csharp
// Returns list of (Action, probability) tuples
List<(Action action, double probability)> actions = initializer.InitializeActions();
```

### How It Works
1. Attempts to load actions from `GameData/Actions.json`
2. Filters by theme and "environment" tag
3. Falls back to hardcoded theme and room-type actions
4. Returns actions with 70% execution probability

### Theme Support
- forest, lava, crypt, cavern, swamp
- desert, ice, ruins, castle, graveyard

### Room Type Support
- treasure, guard, trap, puzzle, rest
- storage, library, armory, kitchen, dining
- chamber, hall, vault, sanctum, grotto
- catacomb, shrine, laboratory, observatory
- throne, boss, (default)

### Example Usage
```csharp
var initializer = new EnvironmentalActionInitializer("forest", "treasure");
var actions = initializer.InitializeActions();

foreach (var (action, probability) in actions)
{
    room.AddAction(action, probability);
}
```

---

## EnemyGenerationManager

**File**: `Code/World/EnemyGenerationManager.cs`

**Purpose**: Generate and manage enemies in an environment

### Constructor
```csharp
var generator = new EnemyGenerationManager(theme: string, isHostile: bool);
```

### Main Methods
```csharp
// Generate enemies for a room level
void GenerateEnemies(int roomLevel, List<string>? possibleEnemies = null);

// Get all enemies
List<Enemy> GetEnemies();

// Check for living enemies
bool HasLivingEnemies { get; }

// Get next living enemy (or null)
Enemy? GetNextLivingEnemy { get; }
```

### Features
- Loads enemy data from `GameData/Enemies.json`
- Scales stats based on room level
- Filters enemies by theme automatically
- Supports theme-specific enemy constraints
- Fallback to basic enemies if loading fails

### Example Usage
```csharp
var generator = new EnemyGenerationManager("crypt", isHostile: true);
generator.GenerateEnemies(roomLevel: 5);

if (generator.HasLivingEnemies)
{
    var currentEnemy = generator.GetNextLivingEnemy;
    // ... combat logic
}
```

### Theme-Enemy Mapping
Each theme maps to appropriate enemy types:
- **Forest**: Goblin, Spider, Wolf, Bear, Treant
- **Crypt**: Skeleton, Zombie, Wraith, Lich, Ghoul
- **Lava**: Wraith, Slime, Bat, Fire Elemental, Lava Golem
- (and 20+ more themes)

---

## EnvironmentCombatStateManager

**File**: `Code/World/EnvironmentCombatStateManager.cs`

**Purpose**: Manage environmental action timing during combat

### Constructor
```csharp
var manager = new EnvironmentCombatStateManager();
```

### Main Methods
```csharp
// Check if environment should act this turn
bool ShouldEnvironmentAct(bool isHostile);

// Reset state for new fight
void ResetForNewFight();

// Get current action count
int GetActionCount();

// Get maximum actions allowed
int GetMaxActions();

// Check if max actions reached
bool HasReachedActionLimit();
```

### Probability System
- **Base Chance**: 5% per turn
- **Max Actions**: 2 per fight
- **Failed Attempts**: Increase probability by 5% each
- **Max Probability**: 50% (prevents predictability)

### Example Usage
```csharp
var manager = new EnvironmentCombatStateManager();

// Start of combat
manager.ResetForNewFight();

// Each turn
if (manager.ShouldEnvironmentAct(isHostile: true))
{
    // Execute environmental action
    var action = room.SelectRandomAction();
}

// Combat end
int totalActions = manager.GetActionCount();
```

### Probability Example
```
Turn 1: 5% chance (0 failed attempts)
Turn 2: 5% chance (1 failed attempt: 5% + 5% = 10%)
Turn 3: 10% chance (2 failed attempts: 5% + 10% = 15%)
...
Max: 50% chance (capped)
```

---

## EnvironmentEffectManager

**File**: `Code/World/EnvironmentEffectManager.cs`

**Purpose**: Manage passive and active environmental effects

### Constructor
```csharp
var manager = new EnvironmentEffectManager();
```

### Main Methods
```csharp
// Apply passive effect to a value
double ApplyPassiveEffect(double value);

// Apply active effect to entities
void ApplyActiveEffect(Character player, Enemy enemy);

// Set passive effect
void SetPassiveEffect(PassiveEffectType type, double value);

// Set active effect action
void SetActiveEffectAction(Action action);

// Clear effects
void ClearPassiveEffect();
void ClearActiveEffect();
```

### Properties
```csharp
PassiveEffectType PassiveEffectType { get; set; }
double PassiveEffectValue { get; set; }
Action? ActiveEffectAction { get; private set; }
```

### Effect Types
```csharp
public enum PassiveEffectType
{
    None,
    DamageMultiplier,  // e.g., 0.9 = 90% damage
    SpeedMultiplier    // e.g., 1.25 = 125% attack speed
}
```

### Example Usage
```csharp
var manager = new EnvironmentEffectManager();

// Set damage reduction in lava environment
manager.SetPassiveEffect(PassiveEffectType.DamageMultiplier, 0.8);

// Apply to damage calculation
double damage = 100;
double modifiedDamage = manager.ApplyPassiveEffect(damage); // 80

// Set active effect
var damageAction = new Action("Environmental Damage", ...);
manager.SetActiveEffectAction(damageAction);
manager.ApplyActiveEffect(player, enemy);

// Clear effects
manager.ClearPassiveEffect();
manager.ClearActiveEffect();
```

---

## Integration Example

Here's how the managers work together in the Environment facade:

```csharp
// In Environment.cs constructor
public Environment(string name, string description, bool isHostile, 
                   string theme, string roomType = "")
    : base(name)
{
    // Create managers
    actionInitializer = new EnvironmentalActionInitializer(theme, roomType);
    enemyGenerator = new EnemyGenerationManager(theme, isHostile);
    combatStateManager = new EnvironmentCombatStateManager();
    effectManager = new EnvironmentEffectManager();
    
    // Initialize actions
    var actions = actionInitializer.InitializeActions();
    foreach (var (action, prob) in actions)
        AddAction(action, prob);
}

// Usage in combat
if (combatStateManager.ShouldEnvironmentAct(IsHostile))
{
    var action = SelectRandomAction();
    if (action != null)
        ExecuteAction(action);
}
```

---

## Common Patterns

### Initialize a Combat Round
```csharp
void InitializeCombat()
{
    room.ResetForNewFight();
    room.GenerateEnemies(roomLevel);
}
```

### Execute Combat Turn
```csharp
void ExecuteTurn()
{
    // Environment actions
    if (room.ShouldEnvironmentAct())
    {
        var envAction = room.SelectRandomAction();
        ExecuteAction(envAction);
    }
    
    // Player and enemy turns...
}
```

### Check Combat Status
```csharp
bool IsCombatOver()
{
    return !room.HasLivingEnemies();
}
```

### Apply Environmental Effects
```csharp
void ApplyEnvironmentalEffects()
{
    double damage = CalculateDamage();
    damage = room.ApplyPassiveEffect(damage); // Apply modifiers
    return damage;
}
```

---

## Performance Considerations

1. **Action Loading**: Cached after first load
2. **Enemy Generation**: Lazy loaded only when needed
3. **Theme Filtering**: Pre-mapped for fast lookups
4. **Probability Calculation**: Lightweight, runs every turn

---

## Error Handling

All managers include graceful fallbacks:

- If JSON missing: Use hardcoded defaults
- If enemy data missing: Create basic enemies
- If no matching enemies: Use all available
- Invalid JSON: Log error and continue

---

## Testing Checklist

### EnvironmentalActionInitializer
- [ ] Actions load from JSON
- [ ] Fallback to defaults works
- [ ] Theme filtering accurate
- [ ] Room type filtering accurate

### EnemyGenerationManager
- [ ] Correct number of enemies generated
- [ ] Stats scale with level
- [ ] Theme-appropriate enemies
- [ ] Fallback enemies created

### EnvironmentCombatStateManager
- [ ] State resets properly
- [ ] Probability increases correctly
- [ ] Max actions enforced
- [ ] Max probability capped

### EnvironmentEffectManager
- [ ] Passive effects applied
- [ ] Active effects applied
- [ ] Effects clear properly
- [ ] All effect types supported

---

## See Also

- [Environment Refactoring Complete](./ENVIRONMENT_REFACTORING_COMPLETE.md)
- [ARCHITECTURE.md](../01-Core/ARCHITECTURE.md)
- [CODE_PATTERNS.md](./CODE_PATTERNS.md)

