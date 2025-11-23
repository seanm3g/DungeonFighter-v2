# Dungeon Fighter RPG v2

**Version 6.2** - *Production Ready*

A sophisticated RPG/dungeon crawler game built with .NET 8.0, featuring a modern **Avalonia-based GUI** with ASCII canvas rendering, advanced turn-based combat, dynamic character progression, comprehensive inventory management, procedurally generated dungeons, and an intelligent battle narrative system.

## Features

### GUI & Visual Systems ✨ *NEW in v6.2*
- **Modern GUI**: Avalonia-based interface with ASCII canvas rendering
- **Animated Title Screen**: Smooth 30 FPS color transition animation
- **Item Color System**: Rarity-based visual feedback (Common → Transcendent)
- **Color Configuration**: Data-driven JSON-based color system (166 templates, 200+ keywords)
- **Persistent Layout**: Always-visible character panel with stats, health, and equipment
- **1920×1080 Resolution**: Optimized for modern displays
- **Mouse & Keyboard Support**: Full input support with clickable UI elements

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
- **Dynamic Tuning System**: Real-time parameter adjustment with FormulaEvaluator and ScalingManager
- **Data-Driven Architecture**: All game data stored in structured JSON files
- **Comprehensive Testing**: Built-in test suite with 27+ test categories
- **Balance Analysis**: Automated DPS calculations and combat balance testing
- **Cross-Platform**: Runs on Windows, macOS, and Linux

## Requirements

- .NET 8.0 SDK or Runtime
- macOS, Windows, or Linux

## Installation & Running

### Quick Start (Players)
```bash
# Clone and navigate
git clone <your-repo-url>
cd DungeonFighter-v2/Code

# Run the game
dotnet run
```

### Developer Setup
```bash
# Clone the repository
git clone <your-repo-url>
cd DungeonFighter-v2

# Read essential documentation first
# 1. OVERVIEW.md - Feature overview and quick start
# 2. Documentation/01-Core/ARCHITECTURE.md - System design
# 3. Documentation/02-Development/DEVELOPMENT_GUIDE.md - Development workflow

# Then run the game
cd Code
dotnet run
```

### Building from Source
```bash
cd Code
dotnet build              # Build the project
dotnet run               # Run the game
dotnet run test-all      # Run comprehensive tests (or use in-game menu)
```

### Fixing Build Cache Issues

If you encounter build errors where the compiler can't find methods that clearly exist in your code, use these commands:

```bash
# Quick fix (fastest)
.\Scripts\quick-clean.bat

# Thorough fix (when quick doesn't work)
.\Scripts\fix-build.bat

# Most thorough (last resort)
.\Scripts\clean-all.bat

# Interactive menu
.\Scripts\build-fix.bat
```

**When to use:** Build errors that randomly fix themselves, after Git branch switches, or when IDE and command line builds disagree. See `Scripts/README_BUILD_COMMANDS.md` for details.

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
DungeonFighter/
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
├── Documentation/                 # Project documentation (organized in subfolders)
│   ├── README.md                  # Documentation overview and folder structure
│   ├── 01-Core/                   # Essential project documentation
│   │   ├── README.md              # Main project overview
│   │   ├── ARCHITECTURE.md        # System architecture and design patterns
│   │   ├── OVERVIEW.md            # Comprehensive system overview
│   │   └── TASKLIST.md            # Development tasks and progress
│   ├── 02-Development/            # Development guides and patterns
│   │   ├── DEVELOPMENT_GUIDE.md   # Comprehensive development guide
│   │   ├── CODE_PATTERNS.md       # Code patterns and conventions
│   │   ├── DEVELOPMENT_WORKFLOW.md # Step-by-step development process
│   │   └── Balance documentation  # Balance tuning and changes
│   ├── 03-Quality/                # Testing, debugging, and performance
│   │   ├── TESTING_STRATEGY.md    # Testing approaches and verification
│   │   ├── DEBUGGING_GUIDE.md     # Debugging techniques and tools
│   │   ├── PERFORMANCE_NOTES.md   # Performance considerations
│   │   └── PROBLEM_SOLUTIONS.md   # Solutions to common problems
│   ├── 04-Reference/              # Quick reference and change tracking
│   │   ├── QUICK_REFERENCE.md     # Fast lookup for key information
│   │   ├── INDEX.md               # Complete documentation index
│   │   └── Change tracking docs   # Change logs and tracking
│   ├── 05-Systems/                # System-specific documentation
│   │   └── Feature documentation  # UI, text display, tuning analysis
│   └── 06-Archive/                # Historical notes and reference materials
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

### Quick Test (In-Game)
1. Run the game: `dotnet run`
2. Select "Settings" from the main menu
3. Choose "Tests" from the settings menu
4. Choose from 27+ test categories or select "All Tests"

### Available Test Categories (27+)
- **Core Systems**: Character Leveling & Stats, Item Creation & Properties
- **Combat**: Combat Mechanics, Combo System Tests, Damage Balance
- **Advanced**: Battle Narrative Generation, Enemy Scaling & AI, Intelligent Delay System
- **Mechanics**: Dice Rolling, Action System, Weapon-Based Classes
- **Balance**: Loot Generation, Tuning System, Combo Amplification
- **And 12+ more categories...**

### Performance Targets
All performance targets are verified during testing:

| Metric | Target | Status |
|--------|--------|--------|
| Combat Response Time | <100ms for simple actions | ✅ Verified |
| Menu Navigation | <50ms response time | ✅ Verified |
| Data Loading | <500ms for all game data | ✅ Verified |
| Memory Usage | <200MB peak usage | ✅ Verified |
| Startup Time | <5 seconds total | ✅ Verified |
| Animation Frame Rate | 30+ FPS | ✅ Verified |
| Combat Duration | 10-15 actions per fight | ✅ Verified |

## Development

### Developer Quick Start (5 Minutes)
1. **Understand the project**: Read `OVERVIEW.md` (2 min)
2. **Understand the architecture**: Read `Documentation/01-Core/ARCHITECTURE.md` (1 min skim)
3. **Learn the workflow**: Read `Documentation/02-Development/DEVELOPMENT_GUIDE.md` (2 min)
4. **Follow code patterns**: Reference `Documentation/02-Development/CODE_PATTERNS.md` while coding

### Documentation Structure

**Essential (Start Here)**:
- `OVERVIEW.md` - Game overview and feature list
- `Documentation/01-Core/ARCHITECTURE.md` - System architecture and design patterns
- `Documentation/01-Core/TASKLIST.md` - Current development tasks

**Development Guides**:
- `Documentation/02-Development/DEVELOPMENT_GUIDE.md` - Comprehensive development guide
- `Documentation/02-Development/CODE_PATTERNS.md` - Code patterns and conventions
- `Documentation/02-Development/DEVELOPMENT_WORKFLOW.md` - Step-by-step development process
- `Documentation/02-Development/REFACTORING_HISTORY.md` - Recent refactoring changes

**Problem Solving**:
- `Documentation/03-Quality/PROBLEM_SOLUTIONS.md` - Solutions to common problems
- `Documentation/03-Quality/DEBUGGING_GUIDE.md` - Debugging techniques and tools
- `Documentation/03-Quality/PERFORMANCE_NOTES.md` - Performance considerations and optimizations

**Quick Reference**:
- `Documentation/04-Reference/QUICK_REFERENCE.md` - Fast lookup for key information
- `Documentation/04-Reference/INDEX.md` - Complete documentation index

**System Deep Dives**:
- `Documentation/05-Systems/` - Feature-specific documentation

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

## What's New in v6.2

### Major Features ✨
- ✅ **Animated Title Screen** - Professional color transitions at 30 FPS
- ✅ **Complete Inventory System** - All 7 actions fully functional in GUI
- ✅ **Item Color System** - Rarity-based visual excitement
- ✅ **Data-Driven Colors** - 166 templates, 200+ keywords in JSON
- ✅ **1920×1080 Resolution** - Optimized layout and readability
- ✅ **Combat Text Polish** - Smart wrapping, clean display, proper formatting

### Code Quality Improvements 🏗️
- ✅ **Comprehensive Refactoring** - ~1500+ lines eliminated through design patterns
- ✅ **12+ Design Patterns** - Factory, Registry, Facade, Builder, Strategy, Composition, etc.
- ✅ **Clean Architecture** - Organized into 11 focused subsystems
- ✅ **Well-Documented** - 90+ comprehensive documentation files
- ✅ **Root Level Docs** - OVERVIEW.md and TASKLIST.md at project root

### Architecture Highlights 🎯
- BattleNarrative: 550 → 118 lines (78.5% reduction)
- Environment: 763 → 182 lines (76% reduction)
- CharacterEquipment: 590 → 112 lines (81% reduction)
- GameDataGenerator: 684 → 68 lines (90% reduction)

### Documentation
- `Documentation/02-Development/CHANGELOG.md` - Complete version history
- `Documentation/02-Development/OCTOBER_2025_IMPLEMENTATION_SUMMARY.md` - Detailed features
- `Documentation/02-Development/REFACTORING_HISTORY.md` - Recent refactoring
- `OVERVIEW.md` - Quick game overview
- `TASKLIST.md` - Current development tasks

## Architecture Overview

```
DungeonFighter uses established design patterns:

├── Facade Pattern
│   ├── Character coordinates specialized managers
│   ├── CombatManager orchestrates combat systems
│   └── GameDisplayManager handles all display
├── Factory Pattern
│   ├── ActionFactory creates action instances
│   ├── EnemyFactory generates enemies
│   └── ItemGenerator creates loot
├── Registry Pattern
│   ├── EffectHandlerRegistry manages combat effects
│   └── EnvironmentalEffectRegistry manages room effects
├── Builder Pattern
│   ├── CharacterBuilder handles initialization
│   └── EnemyBuilder creates complex enemies
├── Strategy Pattern
│   ├── ActionSelector for different entity types
│   └── Effect handlers for different effect types
└── Composition Pattern
    ├── Character uses specialized managers
    ├── CombatManager delegates to specialized classes
    └── Environment uses 4 focused managers
```

## Future Enhancements

### Near Term
- [ ] Equipment comparison tooltips
- [ ] Advanced combo management UI
- [ ] Sound effects and audio

### Long Term
- [ ] Additional dungeon themes
- [ ] More enemy types (20+)
- [ ] Quest system
- [ ] Achievement system
- [ ] Leaderboards

### Platform Support
- [ ] Unity port for enhanced graphics
- [ ] Mobile platform support
- [ ] Web version
- [ ] Console port