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
- **`Code/Game/DungeonRunnerManager.cs`** - Manages the main game loop, dungeon selection, and game progression (replaced GameLoopManager)

### **Combat System (Refactored Architecture)**
- **`Code/Combat/CombatManager.cs`** - Orchestrates combat flow and turn management (refactored to use specialized managers)
- **`Code/Combat/CombatStateManager.cs`** - Manages combat state, battle narrative, and entity management
- **`Code/Combat/CombatTurnHandlerSimplified.cs`** - Simplified turn processing logic (high-performance turn handler)
- **`Code/Combat/CombatCalculator.cs`** - Centralized damage, speed, and stat calculations
- **`Code/Combat/CombatEffectsSimplified.cs`** - Simplified status effects management (optimized effects system)
- **`Code/Combat/EffectHandlerRegistry.cs`** - Strategy pattern for handling different combat effects
- **`Code/Combat/StunProcessor.cs`** - Generic stun processing logic
- **`Code/Combat/CombatResults.cs`** - Handles UI display and result formatting
- **`Code/Combat/TurnManager.cs`** - Manages turn-based combat logic
- **`Code/Combat/BattleNarrative.cs`** - Event-driven battle descriptions
- **`Code/Combat/BattleHealthTracker.cs`** - Health tracking for battle narrative system

### **Character System (Refactored Architecture)**
- **`Code/Entity/Character.cs`** - Main player character coordinator class (refactored from 539 to 250 lines)
- **`Code/Entity/CharacterFacade.cs`** - Simplified facade interface for character operations
- **`Code/Entity/EquipmentManager.cs`** - Equipment management and stat bonuses
- **`Code/Entity/LevelUpManager.cs`** - Level up and progression logic
- **`Code/Entity/CharacterBuilder.cs`** - Complex character initialization logic
- **`Code/Entity/CharacterStats.cs`** - Character statistics and leveling system
- **`Code/Entity/CharacterEquipment.cs`** - Equipment management and stat bonuses
- **`Code/Entity/CharacterEffects.cs`** - Character-specific effects and buffs/debuffs
- **`Code/Entity/CharacterProgression.cs`** - Experience, leveling, and skill progression
- **`Code/Entity/CharacterHealthManager.cs`** - Health management, damage, and healing logic
- **`Code/Entity/CharacterCombatCalculator.cs`** - Combat calculations and stat computations
- **`Code/Entity/CharacterSaveManager.cs`** - Save/load functionality for character data

### **Character Actions System (Phase 1 Refactoring ✅ COMPLETE)**
The CharacterActions system has been successfully refactored from a 828-line monolithic class into 5 focused, testable managers using the Facade pattern. **Cleanup completed** - old code removed, facade now 170 lines.

#### Character Actions Facade
- **`Code/Entity/CharacterActions.cs`** - Facade coordinator (170 lines, down from 828)
  - Simple public interface for action management
  - Delegates to specialized managers
  - 100% backward compatible
  - All old monolithic code removed

#### Specialized Action Managers (5 Focused Components)
- **`Code/Entity/Managers/GearActionManager.cs`** (327 lines) - Manages weapon and armor actions
  - AddWeaponActions, AddArmorActions
  - RemoveWeaponActions, RemoveArmorActions
  - Roll bonus application and removal
  - Handles equipment-based action pools

- **`Code/Entity/Managers/ClassActionManager.cs`** (199 lines) - Manages class-specific actions (Barbarian, Warrior, Rogue, Wizard)
  - AddClassActions, RemoveClassActions
  - Per-class action logic with level gating
  - Handles all character progression-based abilities

- **`Code/Entity/Managers/ComboSequenceManager.cs`** (184 lines) - Manages combo sequences and ordering
  - GetComboActions, AddToCombo, RemoveFromCombo
  - Automatic reordering logic with combo order system
  - Default combo initialization

- **`Code/Entity/Managers/DefaultActionManager.cs`** (135 lines) - Manages default actions
  - AddDefaultActions, EnsureBasicAttackAvailable
  - Handles unique action availability per equipment
  - Combo bonus management

- **`Code/Entity/Managers/EnvironmentActionManager.cs`** (101 lines) - Manages environment-specific actions
  - AddEnvironmentActions, ClearEnvironmentActions
  - Environment-specific action filtering and detection
  - Per-environment action pooling

#### Action Utilities
- **`Code/Actions/ActionFactory.cs`** - Creates Action objects from data
  - CreateBasicAttack
  - 13 comprehensive unit tests

- **`Code/Actions/ActionEnhancer.cs`** - Enhances action descriptions with modifiers
  - Roll bonuses, damage multipliers, combo bonuses
  - Status effects, multi-hit, stat bonuses
  - 19 comprehensive unit tests

### **CharacterActions Refactoring Summary - ✅ COMPLETE**
- **Original**: 828 lines, 11 mixed responsibilities
- **Refactored**: 170-line facade + 5 focused managers (946 lines total)
- **Size Reduction**: 79.5% smaller main file (828 → 170 lines)
- **Code Distribution**: 
  - Facade (170 lines) coordinates all action management
  - Managers handle specific domains (101-327 lines each)
- **Total Manager Code**: 946 lines (well-organized, testable components)
- **Pattern**: Facade pattern with composition
- **Status**: ✅ Phase 1 (Refactoring) + ✅ Phase 2 (Testing) + ✅ Phase 3 (Cleanup)
- **Cleanup**: Old monolithic code removed, production code metrics updated

### **Enemy System (Refactored Architecture)**
- **`Code/Entity/Enemy.cs`** - Enemy entity with AI and combat behavior (refactored from 493 to 321 lines)
- **`Code/Entity/EnemyData.cs`** - Enemy data structures, enums, and attack profiles
- **`Code/Entity/ArchetypeManager.cs`** - Enemy archetype logic and profile management
- **`Code/Entity/EnemyCombatManager.cs`** - Enemy-specific combat logic and action attempts
- **`Code/Entity/EnemyBuilder.cs`** - Complex enemy initialization logic
- **`Code/Entity/EnemyFactory.cs`** - Factory methods for creating different enemy types
- **`Code/Data/EnemyLoader.cs`** - Loads enemy data from JSON files

### **Action System**
- **`Code/Actions/Action.cs`** - Base action class with properties and effects
- **`Code/Actions/ActionSelector.cs`** - Handles action selection logic for different entity types
- **`Code/Actions/ActionExecutor.cs`** - Handles action execution logic, damage application, and effect processing
- **`Code/Actions/ActionFactory.cs`** - Creates and manages action instances (see CharacterActions system for refactoring)
- **`Code/Actions/ActionUtilities.cs`** - Shared utilities for action-related operations
- **`Code/Actions/ActionSpeedSystem.cs`** - Intelligent delay system for optimal user experience
- **`Code/Actions/ActionEnhancer.cs`** - Enhances action descriptions with modifier information (NEW)

### **Advanced Action Mechanics System (v7.0+)**

#### Roll Modification System
- **`Code/Actions/RollModification/IRollModifier.cs`** - Interface for roll modifiers
- **`Code/Actions/RollModification/RollModifierRegistry.cs`** - Registry pattern for managing roll modifiers
- **`Code/Actions/RollModification/RollModifiers.cs`** - Concrete implementations:
  - AdditiveRollModifier - Flat +/- values
  - MultiplicativeRollModifier - Multipliers
  - ClampRollModifier - Min/max clamping
  - RerollModifier - Conditional rerolls
  - ExplodingDiceModifier - Exploding dice mechanics
- **`Code/Actions/RollModification/MultiDiceRoller.cs`** - Multiple dice handling (take lowest/highest/average/sum)
- **`Code/Actions/RollModification/RollModificationManager.cs`** - Integration manager for roll modifications
- **`Code/Actions/RollModification/RollModificationContext.cs`** - Context object for modifier execution

#### Event System
- **`Code/Combat/Events/CombatEventBus.cs`** - Event bus for conditional triggers (Observer pattern, Singleton)
- **`Code/Combat/Events/CombatEventTypes.cs`** - Event type definitions and base event class

#### Conditional Triggers
- **`Code/Actions/Conditional/ConditionalTriggerEvaluator.cs`** - Evaluates trigger conditions
- **`Code/Actions/Conditional/TriggerConditions.cs`** - Condition definitions and factory

#### Threshold Management
- **`Code/Combat/ThresholdManager.cs`** - Dynamic threshold adjustment (crit, combo, hit) per actor

#### Advanced Status Effects
- **`Code/Combat/Effects/AdvancedStatusEffects/`** - 17 new status effect handlers:
  - VulnerabilityEffectHandler, HardenEffectHandler, FortifyEffectHandler
  - FocusEffectHandler, ExposeEffectHandler, HPRegenEffectHandler
  - ArmorBreakEffectHandler, PierceEffectHandler, ReflectEffectHandler
  - SilenceEffectHandler, StatDrainEffectHandler, AbsorbEffectHandler
  - TemporaryHPEffectHandler, ConfusionEffectHandler, CleanseEffectHandler
  - MarkEffectHandler, DisruptEffectHandler
- All handlers registered in `EffectHandlerRegistry.cs` using Strategy pattern

#### Tag System
- **`Code/World/Tags/TagRegistry.cs`** - Central repository for all tags (Singleton)
- **`Code/World/Tags/TagMatcher.cs`** - Efficient tag matching algorithms
- **`Code/World/Tags/TagAggregator.cs`** - Combines tags from multiple sources
- **`Code/World/Tags/TagModifier.cs`** - Temporary tag addition/removal with duration tracking

#### Combo Routing
- **`Code/Entity/Actions/ComboRouting/ComboRouter.cs`** - Combo flow control system:
  - Jump to slot N
  - Skip next action
  - Repeat previous action
  - Loop to slot 1
  - Stop combo early
  - Random next action

#### Outcome Handlers
- **`Code/Combat/Outcomes/OutcomeHandler.cs`** - Base interface for outcome handlers
- **`Code/Combat/Outcomes/ConditionalOutcomeHandler.cs`** - Handles conditional outcomes:
  - Enemy death triggers
  - HP threshold triggers (50%, 25%, 10%)
  - Combo end triggers

#### Meta-Progression
- **`Code/Game/Progression/ActionUsageTracker.cs`** - Tracks action usage for scaling outcomes
- **`Code/Game/Progression/ConditionalXPGain.cs`** - Conditional XP gain system

**Integration Points**:
- Roll modifications integrated into `ActionExecutor.cs` (lines 78-82)
- Threshold manager integrated into `CombatCalculator.cs` (line 69)
- Event bus integrated into `ActionExecutor.cs` (lines 101-109, 117-125, 166-173)
- Status effects registered in `EffectHandlerRegistry.cs` (lines 34-50)
- All new status effect properties added to `Actor.cs` (lines 43-82)

### **World & Environment System (Refactored Architecture)**
- **`Code/World/Dungeon.cs`** - Procedurally generates themed room sequences and manages progression
- **`Code/World/DungeonManagerWithRegistry.cs`** - Simplified dungeon management using registry pattern
- **`Code/World/DungeonRunner.cs`** - Manages dungeon execution flow and room progression
- **`Code/World/RewardManager.cs`** - Handles loot and XP rewards after dungeon completion
- **`Code/World/DungeonData.cs`** - Dungeon data structure definitions
- **`Code/World/Environment.cs`** - Room/environment system with environmental effects
- **`Code/World/EnvironmentalEffectRegistry.cs`** - Strategy pattern for environmental effects
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

### **UI System (Refactored Architecture - Coordinator Pattern)**

#### Block Display System (Refactored)
- **`Code/UI/BlockDisplayManager.cs`** - Facade coordinator for block-based display (258 lines, down from 629)
  - Delegates to specialized renderers, message collector, and delay manager
- **`Code/UI/BlockDisplay/`** - Extracted components:
  - **`IBlockRenderer.cs`** - Interface for rendering message groups
  - **`BlockRendererFactory.cs`** - Factory for creating appropriate renderers
  - **`BlockMessageCollector.cs`** - Collects messages for action blocks
  - **`EntityNameExtractor.cs`** - Extracts entity names from messages
  - **`BlockDelayManager.cs`** - Manages delays for block display
  - **`Renderers/CanvasUIRenderer.cs`** - Renderer for CanvasUICoordinator
  - **`Renderers/GenericUIRenderer.cs`** - Renderer for generic UI managers
  - **`Renderers/ConsoleRenderer.cs`** - Renderer for console output

#### Item Display System (Refactored)
- **`Code/UI/ColorSystem/Applications/ItemDisplayColoredText.cs`** - Facade for item formatting (258 lines, down from 599)
  - Delegates to specialized formatters and parsers
- **`Code/UI/ColorSystem/Applications/ItemFormatting/`** - Extracted components:
  - **`ItemKeywordExtractor.cs`** - Shared utility for extracting keywords from modifications
  - **`ItemNameParser.cs`** - Parses item names to extract components
  - **`ItemNameFormatter.cs`** - Formats item names with proper coloring
  - **`ItemStatsFormatter.cs`** - Formats item statistics and bonuses
  - **`ItemComparisonFormatter.cs`** - Formats item comparisons for equip decisions
- **`Code/UI/ColorSystem/Themes/`** - Extracted theme components:
  - **`ItemThemeProvider.cs`** - Provides color themes for item properties
  - **`ItemThemeFormatter.cs`** - Formats item names using color themes

#### Combat Results Formatting (Refactored)
- **`Code/Combat/CombatResultsColoredText.cs`** - Facade for combat result formatting (~200 lines, down from 459)
  - Delegates to specialized formatters
- **`Code/Combat/Formatting/`** - Extracted components:
  - **`DamageFormatter.cs`** - Formats damage display messages
  - **`RollInfoFormatter.cs`** - Formats roll information for combat results
  - **`ActionSpeedCalculator.cs`** - Calculates actual action speed

#### Action Execution System (Refactored)
- **`Code/Actions/ActionExecutor.cs`** - Orchestrator for action execution (~300 lines, down from 576)
  - Delegates to specialized executors and trackers
- **`Code/Actions/Execution/`** - Extracted components:
  - **`ActionStatisticsTracker.cs`** - Tracks statistics for action execution
  - **`ActionStatusEffectApplier.cs`** - Applies status effects from actions
  - **`AttackActionExecutor.cs`** - Executes attack actions
  - **`HealActionExecutor.cs`** - Executes heal actions

#### Dungeon Display System (Refactored)
- **`Code/Game/DungeonDisplayManager.cs`** - Coordinator for dungeon display (~350 lines, down from 573)
  - Uses extracted builders and display buffer
- **`Code/Game/Display/Dungeon/`** - Extracted components:
  - **`DungeonHeaderBuilder.cs`** - Builds dungeon header display
  - **`RoomInfoBuilder.cs`** - Builds room information display
  - **`EnemyInfoBuilder.cs`** - Builds enemy information display
  - **`DungeonDisplayBuffer.cs`** - Manages display buffer for dungeon information

#### Configuration Organization (Refactored)
- **`Code/Config/EnemyConfig.cs`** - Main enemy configuration file (reorganized)
- **`Code/Config/Enemy/`** - Split configuration classes:
  - **`EnemyScalingConfig.cs`** - Enemy scaling configuration
  - **`EnemyBalanceConfig.cs`** - Enemy balance configuration
  - **`EnemyArchetypesConfig.cs`** - Enemy archetypes configuration
  - **`EnemyDPSConfig.cs`** - Enemy DPS configuration
- **`Code/UI/UIManager.cs`** - Centralized UI output and display with beat-based timing
- **`Code/UI/GameMenuManager.cs`** - Manages game menus, UI interactions, and game flow
- **`Code/UI/GameDisplayManager.cs`** - Unified display manager for inventory, character stats, and equipment
- **`Code/UI/ItemDisplayFormatter.cs`** - Centralized item display formatting utilities
- **`Code/UI/EquipmentDisplayService.cs`** - Equipment display logic and comparison services
- **`Code/UI/MenuConfiguration.cs`** - Centralized configuration for all menu options throughout the game
- **`Code/UI/TextDisplayIntegration.cs`** - Integration layer for displaying text using the new UIManager beat-based timing system
- **`Code/UI/TextDisplaySettings.cs`** - Configuration for text display timing and formatting
- **`Code/UI/UIConfiguration.cs`** - UI configuration management
- **`Code/UI/DungeonThemeColors.cs`** - Theme-based color mapping for dungeons (24 unique dungeon themes)

#### **Avalonia UI System (New Modular Architecture)**
- **`Code/UI/Avalonia/CanvasUICoordinator.cs`** - Main coordinator implementing IUIManager, delegates to specialized managers
- **`Code/UI/Avalonia/CanvasUITypes.cs`** - Shared types (ClickableElement, ElementType) for UI interactions
- **`Code/UI/Avalonia/Managers/ICanvasContextManager.cs`** - Interface for managing UI state and context
- **`Code/UI/Avalonia/Managers/CanvasContextManager.cs`** - Implementation of UI context management
- **`Code/UI/Avalonia/Managers/ICanvasTextManager.cs`** - Interface for text display and formatting
- **`Code/UI/Avalonia/Managers/CanvasTextManager.cs`** - Implementation of text management
- **`Code/UI/Avalonia/Managers/ICanvasInteractionManager.cs`** - Interface for mouse interactions
- **`Code/UI/Avalonia/Managers/CanvasInteractionManager.cs`** - Implementation of interaction management
- **`Code/UI/Avalonia/Managers/ICanvasAnimationManager.cs`** - Interface for animations
- **`Code/UI/Avalonia/Managers/CanvasAnimationManager.cs`** - Implementation of animation management
- **`Code/UI/Avalonia/Managers/CanvasLayoutManager.cs`** - Utility class for layout calculations
- **`Code/UI/Avalonia/Renderers/ICanvasRenderer.cs`** - Interface for unified rendering
- **`Code/UI/Avalonia/Renderers/CanvasRenderer.cs`** - Main renderer that delegates to specialized renderers
- **`Code/UI/Avalonia/Renderers/CombatMessageHandler.cs`** - Handles combat-related messages
- **`Code/UI/Avalonia/Renderers/MenuRenderer.cs`** - Menu rendering coordinator (165 lines, down from 485) - Delegates to screen-specific renderers
  - **`Code/UI/Avalonia/Renderers/Menu/`** - Extracted screen renderers:
    - **`MainMenuRenderer.cs`** - Main menu rendering
    - **`SettingsMenuRenderer.cs`** - Settings menu rendering
    - **`WeaponSelectionRenderer.cs`** - Weapon selection rendering
    - **`GameMenuRenderer.cs`** - In-game menu rendering
    - **`TestingMenuRenderer.cs`** - Testing menu rendering
    - **`MenuLayoutCalculator.cs`** - Layout calculation utilities
- **`Code/UI/Avalonia/Renderers/InventoryRenderer.cs`** - Specialized inventory rendering
- **`Code/UI/Avalonia/Renderers/CombatRenderer.cs`** - Specialized combat rendering
- **`Code/UI/Avalonia/Renderers/DungeonRenderer.cs`** - Specialized dungeon rendering

### **Data & Configuration (Refactored Architecture)**
- **`Code/Data/ActionLoader.cs`** - Loads and manages action data from JSON
- **`Code/Game/GameDataGenerator.cs`** - Legacy wrapper for game data generation (refactored from 684 to 68 lines)
- **`Code/Game/GameDataGenerationOrchestrator.cs`** - Orchestrates game data generation
- **`Code/Game/GenerationResult.cs`** - Result classes for generation operations
- **`Code/Game/FileManager.cs`** - File I/O and backup operations
- **`Code/Game/ItemGenerators/ArmorGenerator.cs`** - Armor data generation logic
- **`Code/Game/ItemGenerators/WeaponGenerator.cs`** - Weapon data generation logic
- **`Code/Game/ItemGenerators/EnemyGenerator.cs`** - Enemy data generation logic
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
├── Code/Combat/CombatTurnHandlerSimplified.cs (turn processing)
├── Code/Combat/TurnManager.cs (turn management)
├── Code/Actions/ActionExecutor.cs (action execution)
├── Code/Combat/CombatCalculator.cs (damage/stat calculations)
├── Code/Combat/CombatEffects.cs (status effects)
└── Code/Combat/CombatResults.cs (UI display)
```

### **6. Character System Architecture (Fully Refactored)**
```
Code/Entity/Character.cs (Coordinator - 250 lines)
├── Code/Entity/CharacterFacade.cs (simplified interface)
├── Code/Entity/EquipmentManager.cs (equipment management)
├── Code/Entity/LevelUpManager.cs (level up and progression)
├── Code/Entity/CharacterBuilder.cs (complex initialization)
├── Code/Entity/CharacterStats.cs (statistics and leveling)
├── Code/Entity/CharacterEffects.cs (effects and buffs/debuffs)
├── Code/Entity/CharacterEquipment.cs (equipment management)
├── Code/Entity/CharacterProgression.cs (experience and progression)
├── Code/Entity/CharacterActions.cs (action management)
├── Code/Entity/CharacterHealthManager.cs (health, damage, healing)
├── Code/Entity/CharacterCombatCalculator.cs (combat calculations)
└── Code/Entity/CharacterSaveManager.cs (save/load functionality)
```

**Character System Benefits:**
- **Single Responsibility**: Each manager handles one specific concern
- **Maintainability**: Health logic changes only affect `CharacterHealthManager`
- **Testability**: Each manager can be unit tested independently
- **Composition**: Character delegates to specialized managers instead of doing everything
- **Facade Pattern**: `CharacterFacade` provides simplified interface to complex subsystems
- **Builder Pattern**: `CharacterBuilder` handles complex initialization logic
- **Reduced Complexity**: Main Character class reduced from 539 to 250 lines (54% reduction)

## 🎨 Key Design Patterns

### **1. Manager Pattern**
- **`CombatManager`**, **`DungeonManagerWithRegistry`**, **`InventoryManager`** - Orchestrate complex subsystems
- **`CharacterHealthManager`**, **`CharacterCombatCalculator`**, **`GameDisplayManager`** - Specialized character managers
- **`CombatStateManager`**, **`CombatTurnHandlerSimplified`** - Specialized combat managers
- **`EquipmentManager`**, **`LevelUpManager`** - Specialized character subsystem managers
- **NEW**: **`ClassActionManager`**, **`GearActionManager`**, **`ComboSequenceManager`**, **`EnvironmentActionManager`**, **`DefaultActionManager`** - Specialized action managers for CharacterActions system
  - CharacterActions acts as facade coordinating 6 specialized managers
  - Each manager handles single responsibility (class actions, gear, combos, environment, defaults)
  - Refactored from 828-line monolith to 171-line facade + 6 managers
  - 122 comprehensive unit tests verify correctness (95%+ coverage)
- Centralize related functionality and provide clean interfaces

### **2. Factory Pattern**
- **`EnemyFactory`** - Creates enemies based on type and level
- **`ItemGenerator`** - Generates items with randomized properties
- **`ActionFactory`** - Creates and manages action instances
- **`ArmorGenerator`**, **`WeaponGenerator`**, **`EnemyGenerator`** - Specialized data generators

### **3. Singleton Pattern**
- **`GameTicker`** - Single instance for game time management
- **`GameConfiguration`** - Global configuration access

### **4. Strategy Pattern**
- **`ActionSelector`** - Different action selection strategies for heroes vs enemies
- **`ActionExecutor`** - Different action execution strategies
- **`CharacterActions`** - Class-specific action behaviors
- **`EffectHandlerRegistry`** - Strategy pattern for handling different combat effects
- **`EnvironmentalEffectRegistry`** - Strategy pattern for environmental effects

### **5. Registry Pattern**
- **`EffectHandlerRegistry`** - Registry of combat effect handlers (Bleed, Weaken, Slow, Poison, Stun, Burn)
- **`EnvironmentalEffectRegistry`** - Registry of environmental effect handlers
- Provides extensible, maintainable effect system

### **6. Facade Pattern**
- **`CharacterFacade`** - Simplified interface to complex character subsystems
- **`GameDisplayManager`** - Unified interface for all display operations
- **NEW**: **`CharacterActions`** - Facade coordinating 6 specialized action managers
  - 171-line facade instead of 828-line monolith
  - Delegates to ClassActionManager, GearActionManager, ComboSequenceManager, etc.
  - Provides clean public API while hiding internal complexity
  - 100% backward compatible with existing code
  - Refactoring completed in Phase 1, verified with 122 unit tests in Phase 2
- Hides complexity and provides simple, consistent interface

### **7. Builder Pattern**
- **`CharacterBuilder`** - Handles complex character initialization
- **`EnemyBuilder`** - Handles complex enemy initialization
- Separates construction logic from entity classes

### **8. Observer Pattern**
- **`BattleHealthTracker`** - Monitors health milestones during combat
- **`CombatResults`** - Displays combat outcomes and notifications

### **9. Utility Pattern**
- **`UIManager`**, **`JsonLoader`**, **`ErrorHandler`** - Centralized utility functions
- **`RandomUtility`**, **`GameConstants`** - Shared resources and constants
- **`ActionUtilities`** - Shared utilities for action-related operations
- **`ItemDisplayFormatter`** - Centralized item display formatting
- **`StunProcessor`** - Generic stun processing logic

### **10. Configuration Pattern**
- **`GameConfiguration`** - Centralized game balance with 8 configurable systems
- **`UIConfiguration`** - UI timing and display configuration
- **`MenuConfiguration`** - Centralized menu option configuration

### **11. Composition Pattern**
- **`Character`** - Uses composition with specialized managers instead of inheritance
- **`CharacterFacade`**, **`EquipmentManager`**, **`LevelUpManager`** - Composed managers
- **`CombatManager`** - Uses composition with `CombatStateManager` and `CombatTurnHandlerSimplified`
- **`Enemy`** - Uses composition with `EnemyCombatManager` and `ArchetypeManager`
- Delegates complex operations to specialized components while maintaining a clean interface
- Follows "composition over inheritance" principle for better maintainability

### **12. Integration Pattern**
- **`TextDisplayIntegration`** - Integration layer for displaying text using the new UIManager beat-based timing system
- **`EquipmentDisplayService`** - Service layer for equipment display operations
- Provides a clean interface for game components to display messages
- Bridges the gap between game logic and UI display systems

### **13. Template Method Pattern**
- **`ActionAdditionTemplate`** - Template for adding class actions consistently
- Provides consistent behavior while allowing customization

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
- Combat effects separated into specialized handlers using Strategy pattern

### **2. Modularity**
- Systems can be modified independently
- Easy to add new features without affecting existing code
- Registry pattern allows easy addition of new effect types
- Factory pattern enables easy addition of new entity types

### **3. Maintainability**
- Centralized utilities reduce code duplication
- Clear naming conventions and documentation
- Large classes refactored into manageable, focused components
- **Major Refactoring Achievements:**
  - **GameDataGenerator**: 684 → 68 lines (90% reduction)
  - **Character**: 539 → 250 lines (54% reduction)
  - **Enemy**: 493 → 321 lines (35% reduction)
  - **GameConfiguration**: 1000+ → 205 lines (80% reduction)
  - **BlockDisplayManager**: 629 → 258 lines (59% reduction) - Extracted renderers, message collector, entity name extractor, delay manager
  - **ActionExecutor**: 576 → ~300 lines (48% reduction) - Extracted statistics tracker, status effect applier, attack/heal executors
  - **ItemDisplayColoredText**: 599 → 258 lines (57% reduction) - Extracted name parser, formatters, shared keyword extractor
  - **ItemColorThemeSystem**: 517 → ~100 lines (81% reduction) - Extracted theme provider, formatter, shared keyword extractor
  - **DungeonDisplayManager**: 573 → ~350 lines (39% reduction) - Extracted section builders, display buffer
  - **CombatResultsColoredText**: 459 → ~200 lines (56% reduction) - Extracted damage formatter, roll info formatter, speed calculator
  - **MenuRenderer**: 485 → 165 lines (66% reduction) - Extracted screen-specific renderers (MainMenu, Settings, WeaponSelection, GameMenu, Testing) and layout calculator
  - **EnemyConfig**: 489 lines → Split into separate files - Reorganized configuration classes into focused files (EnemyScalingConfig, EnemyBalanceConfig, EnemyArchetypesConfig, EnemyDPSConfig)
- Eliminated code duplication across display managers
- Registry pattern eliminates large switch statements

### **4. Extensibility**
- New enemy types, actions, and items can be added via JSON
- Plugin-like architecture for new features
- Registry pattern allows easy addition of new effect handlers
- Builder pattern enables complex entity creation without modifying core classes

### **5. Testability**
- Utility classes can be easily unit tested
- Manager classes provide clear interfaces for testing
- Character managers can be tested independently (Health, Combat, Display)
- Composition pattern enables better mocking and isolation testing
- Registry pattern allows testing individual effect handlers in isolation

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