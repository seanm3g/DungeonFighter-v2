# DungeonFighter-v2 Architecture Overview

## 📋 Table of Contents
1. [Project Structure](#project-structure)
2. [Core Architecture](#core-architecture)
3. [Code Organization](#code-organization)
4. [Data Management](#data-management)
5. [Game Flow](#game-flow)
6. [Key Design Patterns](#key-design-patterns)
7. [Dependencies](#dependencies)

## 🏗️ Project Structure

```
DungeonFighter-v2/
├── Code/                    # Main application code
├── GameData/               # JSON configuration and data files
├── Documentation/          # Project documentation
├── reference images/       # Visual assets
└── Distribution files      # Build artifacts
```

## 🎯 Core Architecture

### **Entry Point**
- **`Program.cs`** - Application entry point, initializes and runs the game

### **Main Game Loop**
- **`Game.cs`** - Central orchestrator, coordinates all game systems
- **`GameInitializer.cs`** - Handles game setup, character creation, and initial state
- **`GameMenuManager.cs`** - Manages main menu navigation and user interactions

### **Combat System**
- **`CombatManager.cs`** - Orchestrates combat flow and turn management
- **`CombatActions.cs`** - Handles action execution for both players and enemies
- **`CombatCalculator.cs`** - Centralized damage, speed, and stat calculations
- **`CombatEffects.cs`** - Manages status effects, debuffs, and environmental effects
- **`CombatResults.cs`** - Handles UI display and result formatting
- **`TurnManager.cs`** - Manages turn-based combat logic

### **Character System**
- **`Character.cs`** - Main player character class with stats, equipment, and progression
- **`CharacterStats.cs`** - Character statistics and leveling system
- **`CharacterEquipment.cs`** - Equipment management and stat bonuses
- **`CharacterEffects.cs`** - Character-specific effects and buffs/debuffs
- **`CharacterProgression.cs`** - Experience, leveling, and skill progression
- **`CharacterSaveManager.cs`** - Save/load functionality for character data

### **Enemy System**
- **`Enemy.cs`** - Enemy entity with AI and combat behavior
- **`EnemyFactory.cs`** - Creates enemies based on type and level
- **`EnemyLoader.cs`** - Loads enemy data from JSON files

### **Dungeon System**
- **`DungeonManager.cs`** - Manages dungeon selection, generation, and completion
- **`Dungeon.cs`** - Individual dungeon instance with rooms and progression
- **`Environment.cs`** - Room generation and environmental effects
- **`RoomGenerator.cs`** - Creates room layouts and content
- **`RoomLoader.cs`** - Loads room data from JSON files

### **Inventory & Equipment**
- **`InventoryManager.cs`** - Consolidated inventory management (equipment, display, combos)
- **`InventoryDisplayManager.cs`** - UI for inventory browsing and management
- **`ComboManager.cs`** - Manages combo actions and sequences
- **`Item.cs`** - Base item class for weapons, armor, and consumables

### **Data & Configuration**
- **`ActionLoader.cs`** - Loads and manages action data from JSON
- **`GameDataGenerator.cs`** - Generates and manages game data files
- **`LootGenerator.cs`** - Creates randomized loot and items
- **`ItemGenerator.cs`** - Base class for item generation logic
- **`TuningConfig.cs`** - Game balance and configuration management
- **`GameSettings.cs`** - User settings and preferences
- **`ScalingManager.cs`** - Handles game difficulty and content scaling

### **Utility Systems**
- **`UIManager.cs`** - Centralized UI output and display management
- **`JsonLoader.cs`** - Common JSON loading and file operations
- **`ErrorHandler.cs`** - Centralized error handling and logging
- **`RandomUtility.cs`** - Consistent random number generation
- **`GameConstants.cs`** - Common constants, file paths, and strings
- **`BasicGearConfig.cs`** - Basic gear names and configuration
- **`ScalingManager.cs`** - Handles game difficulty and content scaling
- **`SettingsManager.cs`** - User settings management
- **`FlavorText.cs`** - Dynamic text generation for descriptions
- **`FormulaEvaluator.cs`** - Mathematical formula evaluation
- **`Dice.cs`** - Dice rolling and probability systems
- **`ActionSpeedSystem.cs`** - Action timing and speed calculations
- **`GameTicker.cs`** - Game time management and ticker system
- **`BattleHealthTracker.cs`** - Health milestone tracking during combat
- **`BattleNarrative.cs`** - Combat narrative and storytelling

## 📊 Data Management

### **GameData/ Folder Structure**
```
GameData/
├── Actions.json           # Action definitions and properties
├── Armor.json            # Armor data and statistics
├── Enemies.json          # Enemy types and configurations
├── Weapons.json          # Weapon data and statistics
├── FlavorText.json       # Dynamic text templates
├── Modifications.json    # Item modification data
├── RarityTable.json      # Loot rarity distributions
├── Rooms.json            # Room templates and layouts
├── Dungeons.json         # Dungeon configurations
├── StatBonuses.json      # Stat bonus definitions
├── TierDistribution.json # Item tier distributions
├── TuningConfig.json     # Game balance parameters
├── StartingGear.json     # Initial character equipment
├── DungeonConfig.json    # Dungeon generation settings
└── character_save.json   # Player save data
```

### **Data Classes**
- **`DungeonConfig`** & **`DungeonGenerationConfig`** - Located in `DungeonManager.cs`
- **`StartingGearData`**, **`StartingWeapon`**, **`StartingArmor`** - Located in `GameInitializer.cs`
- **`ActionData`** - Located in `ActionLoader.cs`
- **`Item`** - Base item class in `Item.cs`

### **Configuration Classes**
- **`CombatBalanceConfig`** - Advanced combat mechanics (critical hits, armor reduction, block/dodge/parry)
- **`ExperienceSystemConfig`** - Character progression and experience formulas
- **`LootSystemConfig`** - Loot drop rates and economy settings
- **`DungeonScalingConfig`** - Dungeon generation and scaling parameters
- **`StatusEffectsConfig`** - Status effect configurations and balance
- **`EquipmentScalingConfig`** - Equipment progression and scaling
- **`ClassBalanceConfig`** - Character class balance multipliers
- **`DifficultySettingsConfig`** - Difficulty level multipliers for game balance

## 🔄 Game Flow

### **1. Initialization**
```
Program.cs → Game.cs → GameInitializer.cs
├── Load game settings
├── Create/load character
├── Initialize inventory
└── Generate starting dungeons
```

### **2. Main Menu Loop**
```
GameMenuManager.cs
├── Display main menu
├── Handle user input
├── Navigate to inventory/dungeon
└── Manage game state
```

### **3. Inventory Management**
```
InventoryManager.cs
├── InventoryDisplayManager.cs (display)
├── Equipment operations (equip/unequip/discard)
└── ComboManager.cs (combo actions)
```

### **4. Dungeon Exploration**
```
DungeonManager.cs → Dungeon.cs → Environment.cs
├── Select dungeon
├── Generate rooms
├── Handle room progression
└── Manage rewards
```

### **5. Combat System**
```
CombatManager.cs
├── TurnManager.cs (turn management)
├── CombatActions.cs (action execution)
├── CombatCalculator.cs (damage/stat calculations)
├── CombatEffects.cs (status effects)
└── CombatResults.cs (UI display)
```

## 🎨 Key Design Patterns

### **1. Manager Pattern**
- **`CombatManager`**, **`DungeonManager`**, **`InventoryManager`** - Orchestrate complex subsystems
- Centralize related functionality and provide clean interfaces

### **2. Factory Pattern**
- **`EnemyFactory`** - Creates enemies based on type and level
- **`ItemGenerator`** - Generates items with randomized properties

### **3. Singleton Pattern**
- **`GameTicker`** - Single instance for game time management
- **`TuningConfig`** - Global configuration access

### **4. Strategy Pattern**
- **`CombatActions`** - Different action types with varying execution strategies
- **`CharacterActions`** - Class-specific action behaviors

### **5. Observer Pattern**
- **`BattleHealthTracker`** - Monitors health milestones during combat
- **`CombatResults`** - Displays combat outcomes and notifications

### **6. Utility Pattern**
- **`UIManager`**, **`JsonLoader`**, **`ErrorHandler`** - Centralized utility functions
- **`RandomUtility`**, **`GameConstants`** - Shared resources and constants

### **7. Configuration Pattern**
- **`TuningConfig`** - Centralized game balance with 8 configurable systems
- **`FormulaEvaluator`** - Evaluates mathematical formulas for dynamic configuration
- **`ScalingManager`** - Applies configuration-based scaling to game content

## ⚙️ Configuration Systems

### **Implemented Configurable Systems**
1. **CombatBalance** - Critical hits, armor reduction, block/dodge/parry mechanics
2. **ExperienceSystem** - Character progression and experience formulas
3. **LootSystem** - Drop chances, magic find, and economy settings
4. **DungeonScaling** - Room counts, enemy spawns, and generation parameters
5. **StatusEffects** - Bleed, burn, freeze, stun configurations
6. **EquipmentScaling** - Weapon/armor progression and tier scaling
7. **ClassBalance** - Character class multipliers (Barbarian, Warrior, Rogue, Wizard)
8. **DifficultySettings** - Easy/Normal/Hard mode multipliers

### **Configuration Benefits**
- **Dynamic Balancing** - Game balance can be adjusted without code changes
- **Modding Support** - Players can modify game behavior via JSON files
- **A/B Testing** - Different balance configurations can be tested easily
- **Localization** - Different regions can have different balance settings

## 🔗 Dependencies

### **Core Dependencies**
- **System.Text.Json** - JSON serialization/deserialization
- **System.Collections.Generic** - Data structures and collections
- **System.IO** - File system operations
- **System.Threading** - Threading and timing operations

### **Internal Dependencies**
- **Entity Base Class** - `Character` and `Enemy` inherit from `Entity`
- **Action System** - Actions are loaded and managed by `ActionLoader`
- **Item System** - Items are generated by `LootGenerator` and managed by `InventoryManager`
- **Combat System** - All combat components work together through `CombatManager`

### **Data Flow**
```
JSON Files → Loaders → Data Classes → Managers → Game Logic → UI Display
```

## 🎯 Architecture Benefits

### **1. Separation of Concerns**
- Each class has a single, well-defined responsibility
- Clear boundaries between systems (combat, inventory, dungeon, etc.)

### **2. Modularity**
- Systems can be modified independently
- Easy to add new features without affecting existing code

### **3. Maintainability**
- Centralized utilities reduce code duplication
- Clear naming conventions and documentation

### **4. Extensibility**
- New enemy types, actions, and items can be added via JSON
- Plugin-like architecture for new features

### **5. Testability**
- Utility classes can be easily unit tested
- Manager classes provide clear interfaces for testing

## 📈 Performance Considerations

### **1. Lazy Loading**
- JSON data is loaded only when needed
- Actions and items are cached after first load

### **2. Object Pooling**
- Enemies and items are reused when possible
- Reduces garbage collection pressure

### **3. Centralized Random Generation**
- Single `RandomUtility` instance for consistent behavior
- Thread-safe random number generation

### **4. Efficient Data Structures**
- Dictionaries for fast lookups (actions, items, enemies)
- Lists for ordered collections (inventory, dungeons)

This architecture provides a solid foundation for a turn-based RPG with room for future expansion and modification.

## Related Documentation

- **`DEVELOPMENT_GUIDE.md`**: Comprehensive development guide and quick start
- **`CODE_PATTERNS.md`**: Code patterns and conventions based on this architecture
- **`DEVELOPMENT_WORKFLOW.md`**: Development process using this architecture
- **`QUICK_REFERENCE.md`**: Fast lookup for architecture components
- **`INDEX.md`**: Complete documentation index