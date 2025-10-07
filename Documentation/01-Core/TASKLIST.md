# Task List - DungeonFighter

1. Get narrative working again
2. Get environmental actions working and implemented again
3. ✅ Add an item generation tool - **COMPLETED**: Refactored GameDataGenerator with safety features
4. A way to tweak balance, save, update those settings.

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