# Settings System Architecture

## Overview

The settings system provides a comprehensive UI for managing game configuration, including gameplay settings, difficulty adjustments, text delays, animations, and developer tools.

## Architecture

### Component Structure

```
SettingsPanel (UI)
├── SettingsManager (Core Settings)
├── SettingsPersistenceManager (Save/Load)
│   ├── SettingsLoader
│   └── SettingsSaver
├── SettingsEventWiring (Event Management)
├── SettingsInitialization (Tab Initialization)
│   └── SettingsTabInitializer
├── SettingsTabManager (Tab Navigation & Persistence)
├── SettingsActionTestGenerator (Action Test Generation)
├── GameVariablesTabManager
├── ActionsTabManager
├── BattleStatisticsTabManager
└── SettingsTestExecutor
    └── TestExecutionManager
```

### Key Components

#### 1. SettingsPanel (UI Layer)
- **File**: `Code/UI/Avalonia/SettingsPanel.axaml` / `SettingsPanel.axaml.cs`
- **Responsibility**: Main UI container with tabbed interface
- **Features**:
  - 8 tabs: Gameplay, Difficulty, Testing, Game Variables, Actions, Battle Statistics, Text Delays, About
  - Keyboard navigation (arrow keys for tabs) - delegated to `SettingsTabManager`
  - Tab persistence (remembers last viewed tab) - delegated to `SettingsTabManager`
  - Two-click confirmation for destructive operations (Reset)
  - Action test generation - delegated to `SettingsActionTestGenerator`

#### 2. SettingsManager (Core Logic)
- **File**: `Code/UI/Avalonia/Managers/SettingsManager.cs`
- **Responsibility**: Core settings management and validation
- **Features**:
  - Load/save main settings
  - Validation and error recovery
  - Transaction-like save operations (rollback on failure)
  - Delegates to specialized managers for text delays and animations

#### 3. SettingsPersistenceManager (Persistence Layer)
- **File**: `Code/UI/Avalonia/Managers/SettingsPersistenceManager.cs`
- **Responsibility**: Facade for loading and saving settings
- **Delegates to**:
  - `SettingsLoader`: Loads settings from GameSettings/Config into UI
  - `SettingsSaver`: Saves settings from UI to GameSettings/Config

#### 4. SettingsEventWiring (Event Management)
- **File**: `Code/UI/Avalonia/Managers/SettingsEventWiring.cs`
- **Responsibility**: Unified event wiring system
- **Features**:
  - Wires slider ↔ textbox synchronization
  - Wires button click events
  - Handles test execution events
  - Null-safe event wiring with validation

#### 5. SettingsTabManager (Tab Management)
- **File**: `Code/UI/Avalonia/Managers/SettingsTabManager.cs`
- **Responsibility**: Tab navigation and persistence
- **Features**:
  - Keyboard navigation (arrow keys for tabs)
  - Tab persistence (remembers last viewed tab)
  - Tab selection change handling
- **Extracted from**: SettingsPanel.axaml.cs (v6.2+ refactoring)

#### 6. SettingsActionTestGenerator (Action Test Generation)
- **File**: `Code/UI/Avalonia/Managers/SettingsActionTestGenerator.cs`
- **Responsibility**: Generates and displays action test blocks
- **Features**:
  - Generates random action blocks for testing
  - Generates multiple action tests (character, environment, status effect)
  - Formats action text with colored output
  - Displays test results in canvas UI
- **Extracted from**: SettingsPanel.axaml.cs (v6.2+ refactoring)

#### 7. GameSettings (Data Model)
- **File**: `Code/Game/GameSettings.cs`
- **Responsibility**: Settings data model and persistence
- **Features**:
  - Singleton pattern
  - JSON serialization/deserialization
  - Atomic file writes (temp file + replace)
  - Corrupted file recovery (backup + recreate)
  - Settings validation (`ValidateAndFix()`)

### Data Flow

#### Loading Settings
```
GameSettings.LoadSettings()
  ↓
SettingsPersistenceManager.LoadSettings()
  ↓
SettingsLoader.LoadMainSettings()
  ↓
UI Controls (Sliders, CheckBoxes, TextBoxes)
```

#### Saving Settings
```
User clicks Save
  ↓
SettingsPanel.SaveSettings()
  ↓
SettingsPersistenceManager.SaveSettings()
  ↓
SettingsSaver.SaveMainSettings()
  ↓
SettingsManager.SaveSettings() [with validation & rollback]
  ↓
GameSettings.SaveSettings() [atomic write]
  ↓
gamesettings.json
```

### Error Handling

1. **Control Validation**: All controls checked for null before use
2. **Settings Validation**: Values clamped to valid ranges before save
3. **Transaction-like Saves**: Original values stored, rollback on failure
4. **File Corruption Recovery**: Corrupted files backed up, defaults loaded
5. **Atomic Writes**: Temp file → replace (prevents partial writes)

### Event Wiring Flow

```
SettingsPanel.WireUpEvents()
  ↓
SettingsEventWiring.WireUpAllEvents()
  ↓
WireUpSliderEvents() → Slider.ValueChanged → TextBox.Text
WireUpTextBoxEvents() → TextBox.LostFocus → Slider.Value
WireUpButtonEvents() → Button.Click → Action handlers
```

### Tab Management

- **Gameplay**: Narrative, Combat, Gameplay settings
- **Difficulty**: Enemy/Player multipliers
- **Testing**: Test execution buttons
- **Game Variables**: Variable editor
- **Actions**: Action editor
- **Battle Statistics**: Battle test runner
- **Text Delays**: Text delay configuration
- **About**: System information

### Keyboard Navigation

- **Tab Key**: Standard Avalonia focus navigation
- **Left/Right Arrows**: Navigate between tabs (when TabControl focused)
- **Enter**: Activate focused control

### Settings Categories

#### Main Settings (GameSettings)
- Narrative balance
- Combat speed
- Difficulty multipliers
- UI preferences
- Auto-save settings

#### Text Delay Settings (TextDelayConfiguration)
- Action delays
- Message delays
- Preset configurations
- Progressive reduction

#### Animation Settings (UIConfiguration)
- Brightness mask
- Undulation effects

### Best Practices

1. **Always validate** settings before saving
2. **Use null checks** for all UI controls
3. **Handle errors gracefully** with user feedback
4. **Log errors** for debugging
5. **Provide rollback** for failed saves
6. **Use atomic file operations** for persistence

## Recent Improvements (v6.2+)

1. ✅ Consolidated SettingsEventWiring and SettingsEventManager
2. ✅ Added comprehensive null checks and error handling
3. ✅ Implemented transaction-like save operations
4. ✅ Added settings validation and corruption recovery
5. ✅ Added keyboard navigation support
6. ✅ Added tab persistence infrastructure
7. ✅ Added two-click confirmation for Reset
8. ✅ Improved error messages and user feedback
9. ✅ **Extracted SettingsActionTestGenerator** - Separated action test generation logic (~400 lines) from SettingsPanel
10. ✅ **Extracted SettingsTabManager** - Separated tab management logic from SettingsPanel
11. ✅ **Reduced SettingsPanel size** - From 1377 lines to ~900 lines through extraction

## Future Enhancements

- [ ] Persistent tab index storage (user preferences file)
- [ ] Search/filter for Game Variables and Actions lists
- [ ] Auto-save on control change (with debouncing)
- [ ] Settings import/export
- [ ] Settings presets/profiles

