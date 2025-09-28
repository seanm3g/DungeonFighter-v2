# Dungeon Fighter RPG v2 - Documentation

A sophisticated turn-based RPG game written in C# with advanced combat mechanics, dynamic character progression, and comprehensive testing framework.

## How to Run

1. **Compile the project:**
   ```bash
   cd Code
   dotnet build
   ```

2. **Run the game:**
   ```bash
   dotnet run
   ```

## Main Menu Options

### 1. New Game
Start a fresh character with:
- Random name generation
- Starting equipment from StartingGear.json
- Level 1 with base stats
- Default action combos from starting weapon

### 2. Load Game
Continue with saved character:
- Shows character name and level in menu
- Loads from character_save.json
- Restores all progress, equipment, and stats

### 3. Settings
Comprehensive game configuration:
- **Narrative Balance**: Control mix between action-by-action (0.0) and full narrative (1.0)
- **Combat Speed**: Adjust combat timing (0.5 = slow, 2.0 = fast)
- **Difficulty Settings**: Player/enemy health and damage multipliers
- **Combat Display**: Health bars, damage numbers, action details
- **Gameplay Options**: Auto-save, combo system, narrative events
- **Tests**: Access 27+ test categories for system verification
- **Delete Saved Characters**: Remove saved game data
- **Reset to Defaults**: Restore all settings to original values

### 4. Tuning Console
Advanced real-time parameter adjustment:
- **Combat Parameters**: Damage, health, scaling formulas
- **Item Scaling**: Weapon/armor tier scaling formulas
- **Rarity System**: Drop rates and magic find mechanics
- **Progression Curves**: XP, level scaling, stat growth
- **XP Reward System**: Enemy XP and level scaling
- **Enemy DPS Analysis**: Automated balance testing
- **Formula Testing**: Test mathematical expressions
- **Balance Analysis**: Full system balance verification
- **Export/Import**: Save and load configuration presets
- **Reload Config**: Refresh tuning parameters from files

### 5. Exit
Quit the game

## In-Game Menu Flow

### Game Session Menu
After starting/loading a game, you'll see:
1. **Choose a Dungeon**: Select from available dungeons based on your level
2. **Inventory**: Manage equipment, view stats, configure action combos
3. **Exit Game and Save**: Save progress and return to main menu

### Inventory Management
The inventory system provides comprehensive character management:
- **Character Stats Display**: Health, armor, attack stats, class progression
- **Current Equipment**: Shows equipped weapon and armor with stats
- **Combo Information**: Current combo sequence and amplification
- **Inventory List**: All items with detailed stats and bonuses
- **Action Pool**: All available actions from equipped gear

### Inventory Options
1. **Equip Item**: Equip weapons and armor from inventory
2. **Unequip Item**: Remove currently equipped items
3. **Discard Item**: Remove items from inventory permanently
4. **Manage Combo Actions**: Configure action sequences for combat
5. **Continue to Dungeon**: Proceed to dungeon selection
6. **Return to Main Menu**: Exit inventory and return to game menu

## Advanced Systems

### Intelligent Delay System
Optimizes user experience with smart timing:
- **Fast Full Narrative Mode**: Quick background calculations for narrative mode
- **Action-Length Matching**: Delays match action length for natural pacing
- **Configurable**: Disable delays entirely for maximum speed
- **Settings**: Enable Text Display Delays, Combat Speed multiplier

### Battle Narrative System
Event-driven narrative for significant moments:
- **Informational Summaries**: Clear, factual combat reporting
- **Poetic Highlights**: Evocative descriptions for important events
- **Significant Events**: First blood, health reversals, near death, good combos
- **Configurable Balance**: Adjust mix between narrative and informational display

## Game Systems

### Combat System
- **Turn-based Combat**: Strategic combat with cooldown-based actions
- **Action Combo System**: Chain actions for escalating damage (1.85x multiplier per step)
- **Dice Mechanics**: 1d20 roll system with thresholds (1-5 fail, 6-15 normal, 16-20 combo)
- **Enemy Scaling**: Dynamic stat scaling based on level and primary attributes
- **Environmental Effects**: Room-specific actions that modify combat

### Character Progression
- **Weapon-based Classes**: Barbarian, Warrior, Rogue, Wizard with unique progression
- **Level-based Stats**: Automatic stat increases and health restoration
- **Equipment System**: Weapons and armor with tier-based stats and special abilities
- **Action Pool Management**: Dynamic action selection from equipped gear
- **Experience Points**: Gain XP from defeating enemies

### Advanced Features
- **Dynamic Tuning System**: Real-time parameter adjustment with FormulaEvaluator
- **Balance Analysis**: Automated DPS calculations and combat balance testing
- **Comprehensive Testing**: 27+ test categories covering all major systems
- **Data-driven Design**: All game content defined in JSON files

## Testing Framework

Access the comprehensive test suite through Settings → Tests:
- **System Tests**: Character, items, dice, actions, combat mechanics
- **Advanced Tests**: Combo system, battle narrative, enemy scaling, delay system
- **Balance Tests**: DPS analysis, damage balance, enemy thresholds
- **Integration Tests**: Loot generation, weapon classes, tuning system
- **All Tests**: Run complete test suite for full system verification

## Configuration Files

The game uses multiple configuration files:
- **gamesettings.json**: Player settings and preferences (auto-saved)
- **character_save.json**: Player character data (auto-saved)
- **TuningConfig.json**: Dynamic tuning parameters
- **GameData/**: All game content (actions, enemies, rooms, items)

## Development Notes

- **Built with .NET 8.0**: Latest framework with modern C# features
- **Test-driven Development**: Comprehensive test suite with 27+ categories
- **Modular Architecture**: Clear separation of concerns with well-defined interfaces
- **Data-driven Design**: Easy content modification through JSON files
- **Comprehensive Documentation**: Detailed system overviews and task tracking 