# Text Display System

## Overview

The new TextDisplaySystem provides a unified, configurable approach to managing all text output in the game. It replaces the scattered text display logic across UIManager, CombatResults, BattleNarrative, and other components.

## Key Features

### 1. **Unified Display Control**
- Single system for all text output
- Consistent formatting and timing
- Easy to customize display order and behavior

### 2. **Configurable Display Order**
- Messages are displayed in a specific order (CombatResult → Narrative → StatusEffect → etc.)
- Order can be customized via configuration
- Ensures consistent message flow

### 3. **Flexible Delay System**
- Different delays for different message types
- Fast combat mode to disable combat delays
- Preset configurations for different play styles

### 4. **Entity Tracking**
- Automatic blank lines between different entities
- Consistent combat message formatting
- Easy to enable/disable

## Message Types

| Type | Description | Default Delay | Usage |
|------|-------------|---------------|-------|
| `CombatResult` | Combat action results | 100ms | "[Player] hits [Enemy] for 10 damage" |
| `Narrative` | Flavor text and story | 150ms | "The first drop of blood is drawn!" |
| `StatusEffect` | Status effect messages | 50ms | "Player is bleeding for 2 turns" |
| `HealthUpdate` | Health change notifications | 75ms | "Player health: 45/60" |
| `SystemMessage` | General system messages | 100ms | "Battle begins!" |
| `Menu` | Menu items and options | 50ms | "1. Attack" |
| `Title` | Title and header text | 200ms | "DUNGEON FIGHTER" |

## Configuration

### Basic Configuration
```json
{
  "EnableDelays": true,
  "FastCombat": false,
  "DisplayOrder": [
    "CombatResult",
    "Narrative", 
    "StatusEffect",
    "HealthUpdate",
    "SystemMessage"
  ],
  "Delays": {
    "CombatResult": 100,
    "Narrative": 150,
    "StatusEffect": 50
  }
}
```

### Preset Configurations

#### Fast Mode
- No delays for combat messages
- Quick menu navigation
- Good for testing and speed runs

#### Cinematic Mode
- Longer delays for dramatic effect
- Emphasizes narrative moments
- Good for immersive gameplay

#### Balanced Mode (Default)
- Moderate delays
- Good balance of speed and readability
- Recommended for most players

#### Debug Mode
- No delays
- Extra timing information
- Good for development and debugging

## Usage Examples

### Displaying a Combat Action
```csharp
// Old way (scattered across multiple files)
UIManager.WriteCombatLine(combatResult);
battleNarrative.DisplayPendingNarrativeEvents();

// New way (unified)
TextDisplaySystem.DisplayCombatAction(
    combatResult, 
    narrativeMessages, 
    statusEffects, 
    entityName
);
```

### Displaying a Menu
```csharp
// Old way
UIManager.WriteTitleLine("Main Menu");
UIManager.WriteMenuLine("1. New Game");
UIManager.WriteMenuLine("2. Load Game");

// New way
TextDisplaySystem.DisplayMenu("Main Menu", new List<string>
{
    "1. New Game",
    "2. Load Game"
});
```

### Customizing Display Order
```csharp
// Change the order so narrative appears before combat results
var newOrder = new List<TextDisplayType>
{
    TextDisplayType.Narrative,
    TextDisplayType.CombatResult,
    TextDisplayType.StatusEffect
};
TextDisplaySystem.SetDisplayOrder(newOrder);
```

### Using Presets
```csharp
// Load a preset configuration
var config = TextDisplayConfig.CreatePreset(TextDisplayPreset.Fast);
config.ApplyToSystem();
```

## Migration Guide

### Step 1: Replace UIManager Calls
```csharp
// Old
UIManager.WriteCombatLine(message);
UIManager.WriteMenuLine(message);
UIManager.WriteSystemLine(message);

// New
TextDisplaySystem.Display(message, TextDisplayType.CombatResult);
TextDisplaySystem.Display(message, TextDisplayType.Menu);
TextDisplaySystem.DisplaySystem(message);
```

### Step 2: Update Combat Flow
```csharp
// Old (in CombatManager)
string result = CombatResults.ExecuteActionWithUI(...);
UIManager.WriteCombatLine(result);
battleNarrative.DisplayPendingNarrativeEvents();

// New
string result = CombatResults.ExecuteActionWithUI(...);
var narratives = battleNarrative.GetTriggeredNarratives();
var effects = GetStatusEffects();
TextDisplaySystem.DisplayCombatAction(result, narratives, effects, entityName);
```

### Step 3: Update Menu Systems
```csharp
// Old (in GameMenuManager)
UIManager.WriteTitleLine("Main Menu");
UIManager.WriteMenuLine("1. New Game");

// New
TextDisplaySystem.DisplayMenu("Main Menu", new List<string> { "1. New Game" });
```

## Benefits

1. **Consistency**: All text follows the same display rules
2. **Customization**: Easy to change display order, delays, and formatting
3. **Maintainability**: Single system to modify instead of scattered code
4. **Performance**: Centralized delay management prevents accumulation
5. **Flexibility**: Easy to add new message types or change behavior

## Configuration Files

- `GameData/TextDisplayConfig.json` - Main configuration file
- Can be edited by players to customize their experience
- Supports hot-reloading during development

## Future Enhancements

- Color coding for different message types
- Sound effects tied to message types
- Animation support for text display
- Localization support
- Accessibility options (larger text, high contrast)
