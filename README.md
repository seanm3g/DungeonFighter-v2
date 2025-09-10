# Dungeon Crawler RPG

A console-based RPG/dungeon crawler game built with .NET 8.0, featuring turn-based combat, character progression, inventory management, and procedurally generated dungeons.

## Features

### Core Gameplay
- **Turn-based Combat System**: Strategic combat with cooldown-based actions
- **Character Progression**: Level up system with XP, stat increases, and health scaling
- **Inventory Management**: Equip weapons, armor, and manage your gear
- **Combo System**: Chain actions together for increased damage and effects
- **Data-Driven Design**: Enemies, actions, and rooms loaded from JSON files

### Dungeon System
- **Procedural Generation**: 1-3 rooms per dungeon based on level
- **Themed Dungeons**: Forest, Lava, Crypt, Cavern, Swamp, Desert, Ice, Ruins, Castle, Graveyard
- **Room Variety**: 15+ unique room types with thematic environmental actions
- **Boss Chambers**: Special final rooms with powerful enemies

### Combat Features
- **18 Unique Enemy Types**: Each with themed abilities and stats
- **Environmental Actions**: Room-specific effects that impact combat
- **Action System**: 69+ different actions including attacks, buffs, debuffs, and heals
- **Health Management**: Health only restores between dungeons, not between rooms

### Technical Features
- **Data-Driven Architecture**: All game data stored in JSON files
- **Modular Design**: Easy to add new enemies, actions, and rooms
- **Comprehensive Testing**: Built-in test suite for all game systems
- **Cross-Platform**: Runs on Windows, macOS, and Linux

## Requirements

- .NET 8.0 SDK or Runtime
- macOS, Windows, or Linux

## Installation & Running

1. **Clone the repository**:
   ```bash
   git clone <your-repo-url>
   cd DF4-CONSOLE
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

- **Main Menu**: Choose between Inventory Management, Dungeon Selection, or Exit
- **Inventory**: Equip/unequip items, view character stats
- **Dungeon Selection**: Choose from available dungeons based on your level
- **Combat**: Automatic turn-based combat with strategic action selection

## Project Structure

```
DF4 - CONSOLE/
├── Code/                    # Main source code
│   ├── Program.cs          # Entry point and test suite
│   ├── Game.cs             # Main game logic
│   ├── Character.cs        # Player character system
│   ├── Combat.cs           # Combat mechanics
│   ├── Action.cs           # Action system
│   ├── Enemy.cs            # Enemy base class
│   ├── EnemyFactory.cs     # Enemy creation
│   ├── EnemyLoader.cs      # JSON enemy loading
│   ├── ActionLoader.cs     # JSON action loading
│   ├── RoomLoader.cs       # JSON room loading
│   ├── Environment.cs      # Room/environment system
│   ├── Dungeon.cs          # Dungeon generation
│   ├── Item.cs             # Item system
│   ├── Dice.cs             # Random number generation
│   └── FlavorText.cs       # Text generation
├── GameData/               # JSON configuration files
│   ├── Actions.json        # All game actions
│   ├── Enemies.json        # Enemy definitions
│   ├── Rooms.json          # Room definitions
│   ├── Weapons.json        # Weapon items
│   ├── Armor.json          # Armor items
│   ├── StartingGear.json   # Starting equipment
│   └── Dungeons.json       # Dungeon configurations
├── Tests/                  # Test files
│   └── ComboSystemTests.cs # Combo system tests
└── Documentation/          # Project documentation
    ├── README.md           # This file
    ├── OVERVIEW.md         # System overview
    └── TASKLIST.md         # Development tasks
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

The game includes a comprehensive test suite accessible from the main menu:

1. Run the game: `dotnet run`
2. Select "Run Tests" from the main menu
3. Choose from 14 different test categories:
   - Character Leveling
   - Items
   - Dice
   - Actions
   - Entity Action Pools
   - Combat
   - Enemy Types
   - Data-Driven Enemy System
   - Enhanced Dungeon System
   - File Loading
   - Character Stats Overview
   - Data-Driven Room System
   - Room Actions in Action List
   - Combo System Tests

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

# Run tests only
dotnet test
```

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