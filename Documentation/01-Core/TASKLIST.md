# Task List - DungeonFighter

1. Get narrative working again
2. Get environmental actions working and implemented again
3. ✅ Add an item generation tool - **COMPLETED**: Refactored GameDataGenerator with safety features
4. A way to tweak balance, save, update those settings.

## ✅ MAJOR REFACTORING COMPLETED

### **Code Duplication Elimination (COMPLETED)**
- **Issue**: Significant code duplication across display managers and effect systems
- **Solution**: Comprehensive refactoring with design patterns and architecture improvements
- **Achievements**:
  - **GameDataGenerator**: 684 → 68 lines (90% reduction)
  - **Character**: 539 → 250 lines (54% reduction)  
  - **Enemy**: 493 → 321 lines (35% reduction)
  - **Display System**: 908 → ~400 lines (56% reduction)
  - **Total**: ~1,500+ lines eliminated through refactoring

### **New Architecture Patterns Implemented**
- **Registry Pattern**: EffectHandlerRegistry, EnvironmentalEffectRegistry
- **Facade Pattern**: CharacterFacade, GameDisplayManager
- **Builder Pattern**: CharacterBuilder, EnemyBuilder
- **Strategy Pattern**: Effect handlers, environmental effects
- **Template Method Pattern**: ActionAdditionTemplate
- **Composition Pattern**: Enhanced throughout codebase

### **Files Refactored**
- **Display System**: InventoryDisplayManager + CharacterDisplayManager → GameDisplayManager
- **Combat System**: CombatEffects → CombatEffectsSimplified + EffectHandlerRegistry
- **Dungeon System**: DungeonManager → DungeonManagerWithRegistry + DungeonRunner + RewardManager
- **Character System**: Character → Character + CharacterFacade + EquipmentManager + LevelUpManager
- **Enemy System**: Enemy → Enemy + EnemyData + ArchetypeManager + EnemyCombatManager
- **Data Generation**: GameDataGenerator → GameDataGenerator + 6 specialized classes

## Recent Updates

### GameDataGenerator Refactoring (COMPLETED)
- **Issue**: GameDataGenerator was automatically overwriting files during development
- **Solution**: Refactored with comprehensive safety checks and configurable automatic generation
- **Features Added**:
  - **Automatic generation at startup** (configurable via TuningConfig.json)
  - Comprehensive safety checks and validation
  - Automatic backup creation before file writes
  - Detailed result reporting with errors/warnings
  - Force overwrite option for when needed
  - Simplified path resolution using existing JsonLoader logic
  - Command-line tools for testing and generation
  - **New configuration options**:
    - `AutoGenerateOnLaunch`: Enable/disable automatic generation at startup
    - `CreateBackupsOnAutoGenerate`: Create backups during automatic generation
    - `ForceOverwriteOnAutoGenerate`: Force regeneration even when no changes needed

### Usage

#### **Automatic Generation (Default)**
- **Enabled by default** - GameDataGenerator runs automatically at startup
- **Safe by default** - Only updates files when changes are needed
- **Configurable** - Control behavior via `GameData/TuningConfig.json`:
  ```json
  "GameData": {
    "AutoGenerateOnLaunch": true,           // Enable/disable automatic generation
    "ShowGenerationMessages": false,        // Show generation messages in game
    "CreateBackupsOnAutoGenerate": true,    // Create backups during auto generation
    "ForceOverwriteOnAutoGenerate": false   // Force regeneration even when no changes
  }
  ```

#### **Manual Generation**
- **Test Generator**: `Scripts/test-generator.bat` or `dotnet run test-generator`
- **Generate Data**: `Scripts/generate-data.bat` or `dotnet run generate-data`
- **Force Overwrite**: `Scripts/generate-data.bat --force` or `dotnet run generate-data --force`