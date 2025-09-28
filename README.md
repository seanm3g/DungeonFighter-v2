# Dungeon Fighter RPG v2

A sophisticated console-based RPG/dungeon crawler game built with .NET 8.0, featuring advanced turn-based combat, dynamic character progression, comprehensive inventory management, and procedurally generated dungeons with intelligent battle narrative.

## Features

### Advanced Combat System
- **Turn-based Combat**: Strategic combat with cooldown-based actions and intelligent delay system
- **Action Combo System**: Chain actions together for escalating damage (1.85x multiplier per combo step)
- **Dice-based Mechanics**: 1d20 roll system with thresholds (1-5 fail, 6-15 normal, 16-20 combo trigger)
- **Environmental Actions**: Room-specific effects that impact combat
- **Battle Narrative System**: Event-driven narrative for significant moments with informational summaries

### Character & Progression System
- **Character Stats**: Strength, Agility, Technique, Intelligence with level-based scaling
- **XP & Leveling**: Automatic stat increases and health restoration on level up
- **Equipment System**: Weapons, armor with tier-based stats and special abilities
- **Action Pool Management**: Dynamic action selection from equipped gear
- **Weapon-based Classes**: Barbarian, Warrior, Rogue, Wizard with unique progression paths

### Enemy & AI System
- **18+ Enemy Types**: Each with unique stats, abilities, and specializations
- **Primary Attribute System**: Enemies specialize in Strength, Agility, or Technique
- **Level Scaling**: Dynamic stat scaling based on enemy level with proper DPS balance
- **Environment-specific Spawning**: Different enemy types appear in themed dungeons

### Dungeon & Environment System
- **Procedural Generation**: 1-3 rooms per dungeon based on level
- **10 Themed Dungeons**: Forest, Lava, Crypt, Cavern, Swamp, Desert, Ice, Ruins, Castle, Graveyard
- **15+ Room Types**: Each with unique environmental actions and effects
- **Boss Chambers**: Special final rooms with powerful enemies

### Advanced Technical Features
- **Dynamic Tuning System**: Real-time parameter adjustment with FormulaEvaluator
- **Data-Driven Architecture**: All game data stored in structured JSON files
- **Comprehensive Testing**: Built-in test suite with 27+ test categories
- **Balance Analysis**: Automated DPS calculations and combat balance testing
- **Cross-Platform**: Runs on Windows, macOS, and Linux

## Requirements

- .NET 8.0 SDK or Runtime
- macOS, Windows, or Linux

## Installation & Running

1. **Clone the repository**:
   ```bash
   git clone <your-repo-url>
   cd DungeonFighter-v2
   ```

2. **Navigate to the Code directory**:
   ```bash
   cd Code
   ```

3. **Run the game**:
   ```bash
   dotnet run
   ```

## Game Controls

### Main Menu
- **New Game**: Start a fresh character
- **Load Game**: Continue with saved character (shows character name and level)
- **Settings**: Configure narrative balance, combat speed, difficulty, and display options
- **Tuning Console**: Advanced real-time parameter adjustment for developers
- **Exit**: Quit the game

### In-Game Menu
- **Choose a Dungeon**: Select from available dungeons based on your level
- **Inventory**: Manage equipment, view stats, configure action combos
- **Exit Game and Save**: Save progress and return to main menu

### Inventory Management
- **Equip Item**: Equip weapons and armor from inventory
- **Unequip Item**: Remove currently equipped items
- **Discard Item**: Remove items from inventory permanently
- **Manage Combo Actions**: Configure action sequences for combat
- **Continue to Dungeon**: Proceed to dungeon selection
- **Return to Main Menu**: Exit inventory and return to game menu

## Project Structure

```
DungeonFighter-v2/
├── Code/                           # Main source code
│   ├── Program.cs                 # Entry point with comprehensive test suite
│   ├── Game.cs                    # Main game logic and flow
│   ├── Character.cs               # Player character with class progression
│   ├── Combat.cs                  # Advanced combat mechanics
│   ├── Action.cs                  # Action system with combo mechanics
│   ├── Enemy.cs                   # Enemy base class with scaling
│   ├── EnemyFactory.cs            # Enemy creation and specialization
│   ├── EnemyLoader.cs             # JSON enemy loading
│   ├── ActionLoader.cs            # JSON action loading
│   ├── RoomLoader.cs              # JSON room loading
│   ├── Environment.cs             # Room/environment system
│   ├── Item.cs                    # Item system with tier scaling
│   ├── Dice.cs                    # Random number generation
│   ├── FlavorText.cs              # Text generation
│   ├── BattleNarrative.cs         # Event-driven battle narrative
│   ├── ManageGear.cs              # Inventory management system
│   ├── LootGenerator.cs           # Procedural loot generation
│   ├── TuningConfig.cs            # Dynamic configuration system
│   ├── TuningConsole.cs           # Real-time parameter adjustment
│   ├── FormulaEvaluator.cs        # Mathematical expression evaluator
│   ├── ScalingManager.cs          # Centralized scaling calculations
│   ├── BalanceAnalyzer.cs         # Automated balance testing
│   └── GameSettings.cs            # Game configuration management
├── GameData/                      # JSON configuration files
│   ├── Actions.json               # All game actions with properties
│   ├── Enemies.json               # Enemy definitions with scaling
│   ├── Rooms.json                 # Room definitions with environmental actions
│   ├── Weapons.json               # Weapon items with tier scaling
│   ├── Armor.json                 # Armor items with tier scaling
│   ├── StartingGear.json          # Starting equipment
│   ├── Dungeons.json              # Dungeon configurations
│   ├── TuningConfig.json          # Dynamic tuning parameters
│   ├── ItemScalingConfig.json     # Item scaling formulas
│   └── StatBonuses.json           # Stat bonus definitions
├── Documentation/                 # Project documentation
│   ├── README.md                  # This file
│   ├── OVERVIEW.md                # Comprehensive system overview
│   ├── TASKLIST.md                # Development tasks and progress
│   └── Various feature docs       # Detailed feature documentation
└── character_save.json            # Player save file (auto-generated)
```

## Game Systems

### Character System
- **Stats**: Strength, Agility, Technique
- **Health**: Scales with level
- **XP**: Gain experience from defeating enemies
- **Leveling**: Automatic stat increases and health restoration

### Combat System
- **Turn-based**: Actions have cooldowns and timing
- **Combo System**: Chain actions for increased effectiveness
- **Environmental Effects**: Rooms can affect combat
- **Action Types**: Attack, Heal, Buff, Debuff, Interact, Move, UseItem

### Inventory System
- **Equipment Slots**: Weapon, Head, Body, Feet
- **Item Types**: Weapons, Head Armor, Chest Armor, Feet Armor
- **Durability**: Items can degrade over time
- **Stats**: Items provide damage, armor, and weight

### Dungeon System
- **Room Generation**: 1-3 rooms based on dungeon level
- **Enemy Scaling**: Enemy levels scale with dungeon level
- **Thematic Rooms**: Each room type has unique environmental actions
- **Boss Encounters**: Final rooms contain powerful enemies

## Data Files

### Actions.json
Contains all game actions with properties:
- `name`: Action name
- `type`: Action type (Attack, Heal, Buff, etc.)
- `targetType`: Target type (Self, SingleTarget, AreaOfEffect, Environment)
- `baseValue`: Base effect value
- `range`: Action range
- `cooldown`: Cooldown in turns
- `description`: Action description
- `damageMultiplier`: Damage scaling
- `length`: Action duration

### Enemies.json
Contains enemy definitions with:
- `name`: Enemy name
- `level`: Enemy level
- `health`: Base health
- `strength`, `agility`, `technique`: Base stats
- `actions`: List of available actions with weights
- `xpReward`: XP gained for defeating

### Rooms.json
Contains room definitions with:
- `name`: Room name
- `description`: Room description
- `theme`: Room theme
- `isHostile`: Whether room is hostile
- `actions`: Environmental actions with weights

## Testing

The game includes a comprehensive test suite accessible from the settings menu:

1. Run the game: `dotnet run`
2. Select "Settings" from the main menu
3. Choose "Tests" from the settings menu
4. Choose from 27+ different test categories:
   - Character Leveling & Stats
   - Item Creation & Properties
   - Dice Rolling Mechanics
   - Action System Functionality
   - Combat Mechanics
   - Combo System Tests
   - Battle Narrative Generation
   - Enemy Scaling & AI
   - Intelligent Delay System
   - New Dice Mechanics
   - New Action System
   - Magic Find Rarity System
   - Loot Generation System
   - Weapon-Based Classes
   - Tuning System
   - Combo Amplification
   - Combo UI
   - Enemy Armor & Stat Pools
   - Damage Balance
   - Enhanced Action Descriptions
   - Enemy 14+ Threshold
   - Guaranteed Loot
   - All Tests (runs complete suite)

## Development

### Adding New Content

**New Actions**:
1. Add action definition to `GameData/Actions.json`
2. Reference in enemy or room configurations

**New Enemies**:
1. Add enemy definition to `GameData/Enemies.json`
2. Reference in `EnemyFactory.cs` if needed

**New Rooms**:
1. Add room definition to `GameData/Rooms.json`
2. Room will be automatically available for dungeon generation

### Building and Running

```bash
# Build the project
dotnet build

# Run the game
dotnet run

# Run tests only (access through in-game menu)
dotnet run
# Then: Settings → Tests → All Tests
```

### Advanced Features

**Tuning Console**: Access real-time parameter adjustment through the main menu:
- Combat parameters (damage, health, scaling)
- Item scaling formulas
- Rarity system configuration
- Progression curves
- XP reward systems
- Enemy DPS analysis
- Configuration export/import

**Settings Configuration**: Comprehensive game customization:
- Narrative Balance (0.0 = action-by-action, 1.0 = full narrative)
- Combat Speed (0.5 = slow, 2.0 = fast)
- Difficulty multipliers
- Combat display options
- Gameplay features

## License

This project is open source. Feel free to modify and distribute according to your needs.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## Future Enhancements

- [ ] GUI version