# DungeonFighter Architecture Overview

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
DungeonFighter/
├── Code/                    # Main application code
├── GameData/               # JSON configuration and data files
├── Documentation/          # Project documentation
├── reference images/       # Visual assets
└── Distribution files      # Build artifacts
```

## 🎯 Core Architecture

### **Entry Point**
- **`Code/Game/Program.cs`** - Application entry point, initializes and runs the game

### **Main Game Loop**
- **`Code/Game/Game.cs`** - Central orchestrator, coordinates all game systems
- **`Code/Game/GameInitializer.cs`** - Handles game setup, character creation, and initial state
- **`Code/UI/GameMenuManager.cs`** - Manages main menu navigation and user interactions
- **`Code/Game/GameLoopManager.cs`** - Manages the main game loop, dungeon selection, and game progression

### **Combat System**
- **`Code/Combat/CombatManager.cs`** - Orchestrates combat flow and turn management (refactored to use specialized managers)
- **`Code/Combat/CombatStateManager.cs`** - Manages combat state, battle narrative, and entity management
- **`Code/Combat/CombatTurnProcessor.cs`** - Handles turn processing logic for player, enemy, and environment entities
- **`Code/Combat/CombatCalculator.cs`** - Centralized damage, speed, and stat calculations
- **`Code/Combat/CombatEffects.cs`** - Manages status effects, debuffs, and environmental effects
- **`Code/Combat/CombatResults.cs`** - Handles UI display and result formatting
- **`Code/Combat/TurnManager.cs`** - Manages turn-based combat logic
- **`Code/Combat/BattleNarrative.cs`** - Event-driven battle descriptions
- **`Code/Combat/BattleHealthTracker.cs`** - Health tracking for battle narrative system

### **Character System**
- **`Code/Entity/Character.cs`** - Main player character coordinator class (refactored from 737 to 539 lines)
- **`Code/Entity/CharacterStats.cs`** - Character statistics and leveling system
- **`Code/Entity/CharacterEquipment.cs`** - Equipment management and stat bonuses
- **`Code/Entity/CharacterEffects.cs`** - Character-specific effects and buffs/debuffs
- **`Code/Entity/CharacterProgression.cs`** - Experience, leveling, and skill progression
- **`Code/Entity/CharacterActions.cs`** - Character action management and combo sequences
- **`Code/Entity/CharacterHealthManager.cs`** - Health management, damage, and healing logic
- **`Code/Entity/CharacterCombatCalculator.cs`** - Combat calculations and stat computations
- **`Code/Entity/CharacterDisplayManager.cs`** - Character information display and formatting
- **`Code/Entity/CharacterSaveManager.cs`** - Save/load functionality for character data

### **Enemy System**
- **`Code/Entity/Enemy.cs`** - Enemy entity with AI and combat behavior
- **`Code/Entity/EnemyFactory.cs`** - Creates enemies based on type and level
- **`Code/Data/EnemyLoader.cs`** - Loads enemy data from JSON files

### **Action System**
- **`Code/Actions/Action.cs`** - Base action class with properties and effects
- **`Code/Actions/ActionSelector.cs`** - Handles action selection logic for different entity types
- **`Code/Actions/ActionExecutor.cs`** - Handles action execution logic, damage application, and effect processing
- **`Code/Actions/ActionFactory.cs`** - Creates and manages action instances
- **`Code/Actions/ActionUtilities.cs`** - Shared utilities for action-related operations
- **`Code/Actions/ActionSpeedSystem.cs`** - Intelligent delay system for optimal user experience
- **`Code/Actions/ClassActionManager.cs`** - Manages class-specific actions and abilities

### **World & Environment System**
- **`Code/World/Dungeon.cs`** - Procedurally generates themed room sequences and manages progression
- **`Code/World/DungeonManager.cs`** - Manages dungeon-related operations including selection, generation, and completion rewards
- **`Code/World/Environment.cs`** - Room/environment system with environmental effects
- **`Code/World/EnvironmentalActionHandler.cs`** - Handles environmental actions and effects
- **`Code/World/StatusEffectManager.cs`** - Manages status effects and their application
- **`Code/World/DamageEffectManager.cs`** - Manages damage effects and calculations
- **`Code/World/DebuffEffectManager.cs`** - Manages debuff effects and their application
- **`Code/Data/RoomGenerator.cs`** - Creates room layouts and content
- **`Code/Data/RoomLoader.cs`** - Loads room data from JSON files

### **Items & Equipment System**
- **`Code/Items/Item.cs`** - Base item class with tier scaling and properties
- **`Code/Items/InventoryManager.cs`** - Inventory management system
- **`Code/Items/ComboManager.cs`** - Manages combo sequences and bonuses
- **`Code/Items/BasicGearConfig.cs`** - Configuration for basic gear and equipment

### **UI System**
- **`Code/UI/UIManager.cs`** - Centralized UI output and display with beat-based timing
- **`Code/UI/GameMenuManager.cs`** - Manages game menus, UI interactions, and game flow
- **`Code/UI/InventoryDisplayManager.cs`** - Manages all display logic for inventory, character stats, and equipment
- **`Code/UI/MenuConfiguration.cs`** - Centralized configuration for all menu options throughout the game
- **`Code/UI/TextDisplayIntegration.cs`** - Integration layer for displaying text using the new UIManager beat-based timing system
- **`Code/UI/TextDisplaySettings.cs`** - Configuration for text display timing and formatting
- **`Code/UI/UIConfiguration.cs`** - UI configuration management

### **Data & Configuration**
- **`Code/Data/ActionLoader.cs`** - Loads and manages action data from JSON
- **`Code/Game/GameDataGenerator.cs`** - Generates and manages game data files
- **`Code/Data/LootGenerator.cs`** - Creates randomized loot and items
- **`Code/Data/ItemGenerator.cs`** - Base class for item generation logic
- **`Code/Game/GameConfiguration.cs`** - Main configuration orchestrator (refactored from 1000+ lines)
- **`Code/Config/`** - Configuration classes organized by domain:
  - **`Code/Config/CharacterConfig.cs`** - Character, attributes, progression, and class balance
  - **`Code/Config/CombatConfig.cs`** - Combat, status effects, and roll systems
  - **`Code/Config/EnemyConfig.cs`** - Enemy scaling, balance, and archetypes
  - **`Code/Config/ItemConfig.cs`** - Item scaling, rarity, and loot systems
  - **`Code/Config/DungeonConfig.cs`** - Dungeon generation and scaling
  - **`Code/Config/UIConfig.cs`** - UI customization and messages
  - **`Code/Config/SystemConfig.cs`** - System settings, debug, and balance analysis
- **`Code/Game/GameSettings.cs`** - User settings and preferences
- **`Code/Data/SettingsManager.cs`** - Game settings management

### **Utility Systems**
- **`Code/Utils/ErrorHandler.cs`** - Centralized error handling and logging
- **`Code/Utils/RandomUtility.cs`** - Consistent random number generation
- **`Code/Game/GameConstants.cs`** - Common constants, file paths, and strings
- **`Code/Utils/DebugLogger.cs`** - Centralized debug output
- **`Code/Utils/FlavorText.cs`** - Dynamic text generation for descriptions
- **`Code/Utils/Dice.cs`** - Dice rolling and probability systems
- **`Code/Utils/TestManager.cs`** - Test execution and analysis framework
- **`Code/Data/JsonLoader.cs`** - Common JSON loading and file operations
- **`Code/Game/GameTicker.cs`** - Game time management and ticker system

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
├── UIConfiguration.json  # UI configuration and timing settings
├── UIConfiguration_Examples.json # Example UI configurations
├── gamesettings.json     # Game settings and preferences
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
Code/Game/Program.cs → Code/Game/Game.cs → Code/Game/GameInitializer.cs
├── Load game settings
├── Create/load character
├── Initialize inventory
└── Generate starting dungeons
```

### **2. Main Menu Loop**
```
Code/UI/GameMenuManager.cs
├── Display main menu
├── Handle user input
├── Navigate to inventory/dungeon
└── Manage game state
```

### **3. Game Loop Management**
```
Code/Game/GameLoopManager.cs
├── Code/World/DungeonManager.cs (dungeon selection)
├── Code/Items/InventoryManager.cs (inventory management)
├── Code/UI/InventoryDisplayManager.cs (display)
└── Code/Items/ComboManager.cs (combo actions)
```

### **4. Dungeon Exploration**
```
Code/World/DungeonManager.cs → Code/World/Dungeon.cs → Code/World/Environment.cs
├── Select dungeon
├── Generate rooms
├── Handle room progression
└── Manage rewards
```

### **5. Combat System**
```
Code/Combat/CombatManager.cs
├── Code/Combat/CombatStateManager.cs (state management)
├── Code/Combat/CombatTurnProcessor.cs (turn processing)
├── Code/Combat/TurnManager.cs (turn management)
├── Code/Actions/ActionExecutor.cs (action execution)
├── Code/Combat/CombatCalculator.cs (damage/stat calculations)
├── Code/Combat/CombatEffects.cs (status effects)
└── Code/Combat/CombatResults.cs (UI display)
```

### **6. Character System Architecture (Refactored)**
```
Code/Entity/Character.cs (Coordinator - 539 lines)
├── Code/Entity/CharacterStats.cs (statistics and leveling)
├── Code/Entity/CharacterEffects.cs (effects and buffs/debuffs)
├── Code/Entity/CharacterEquipment.cs (equipment management)
├── Code/Entity/CharacterProgression.cs (experience and progression)
├── Code/Entity/CharacterActions.cs (action management)
├── Code/Entity/CharacterHealthManager.cs (health, damage, healing)
├── Code/Entity/CharacterCombatCalculator.cs (combat calculations)
└── Code/Entity/CharacterDisplayManager.cs (display and formatting)
```

**Character System Benefits:**
- **Single Responsibility**: Each manager handles one specific concern
- **Maintainability**: Health logic changes only affect `CharacterHealthManager`
- **Testability**: Each manager can be unit tested independently
- **Composition**: Character delegates to specialized managers instead of doing everything
- **Reduced Complexity**: Main Character class reduced from 737 to 539 lines (27% reduction)

## 🎨 Key Design Patterns

### **1. Manager Pattern**
- **`CombatManager`**, **`DungeonManager`**, **`InventoryManager`** - Orchestrate complex subsystems
- **`CharacterHealthManager`**, **`CharacterCombatCalculator`**, **`CharacterDisplayManager`** - Specialized character managers
- **`CombatStateManager`**, **`CombatTurnProcessor`** - Specialized combat managers
- Centralize related functionality and provide clean interfaces

### **2. Factory Pattern**
- **`EnemyFactory`** - Creates enemies based on type and level
- **`ItemGenerator`** - Generates items with randomized properties
- **`ActionFactory`** - Creates and manages action instances

### **3. Singleton Pattern**
- **`GameTicker`** - Single instance for game time management
- **`GameConfiguration`** - Global configuration access

### **4. Strategy Pattern**
- **`ActionSelector`** - Different action selection strategies for heroes vs enemies
- **`ActionExecutor`** - Different action execution strategies
- **`CharacterActions`** - Class-specific action behaviors

### **5. Observer Pattern**
- **`BattleHealthTracker`** - Monitors health milestones during combat
- **`CombatResults`** - Displays combat outcomes and notifications

### **6. Utility Pattern**
- **`UIManager`**, **`JsonLoader`**, **`ErrorHandler`** - Centralized utility functions
- **`RandomUtility`**, **`GameConstants`** - Shared resources and constants
- **`ActionUtilities`** - Shared utilities for action-related operations

### **7. Configuration Pattern**
- **`GameConfiguration`** - Centralized game balance with 8 configurable systems
- **`UIConfiguration`** - UI timing and display configuration
- **`MenuConfiguration`** - Centralized menu option configuration

### **8. Composition Pattern**
- **`Character`** - Uses composition with specialized managers instead of inheritance
- **`CharacterHealthManager`**, **`CharacterCombatCalculator`**, **`CharacterDisplayManager`** - Composed managers
- **`CombatManager`** - Uses composition with `CombatStateManager` and `CombatTurnProcessor`
- Delegates complex operations to specialized components while maintaining a clean interface
- Follows "composition over inheritance" principle for better maintainability

### **9. Integration Pattern**
- **`TextDisplayIntegration`** - Integration layer for displaying text using the new UIManager beat-based timing system
- Provides a clean interface for game components to display messages
- Bridges the gap between game logic and UI display systems

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
- Character system refactored into specialized managers (Health, Combat, Display)

### **2. Modularity**
- Systems can be modified independently
- Easy to add new features without affecting existing code

### **3. Maintainability**
- Centralized utilities reduce code duplication
- Clear naming conventions and documentation
- Large classes refactored into manageable, focused components
- Character class reduced from 737 to 539 lines through composition pattern
- GameConfiguration refactored from 1000+ lines to clean orchestrator with domain-specific config files

### **4. Extensibility**
- New enemy types, actions, and items can be added via JSON
- Plugin-like architecture for new features

### **5. Testability**
- Utility classes can be easily unit tested
- Manager classes provide clear interfaces for testing
- Character managers can be tested independently (Health, Combat, Display)
- Composition pattern enables better mocking and isolation testing

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