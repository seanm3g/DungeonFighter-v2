# Text Delay Configuration System

## Overview

The unified text delay configuration system provides a single JSON file (`GameData/TextDelayConfig.json`) to control all text delays in the game. This replaces the previous scattered delay systems and makes it easy to tweak delays for different message types and text reveal categories.

## Configuration File

All delays are configured in `GameData/TextDelayConfig.json`:

```json
{
  "MessageTypeDelays": {
    "Combat": 100,
    "System": 100,
    "Menu": 25,
    "Title": 400,
    "MainTitle": 400,
    "Environmental": 150,
    "EffectMessage": 50,
    "DamageOverTime": 50,
    "Encounter": 67,
    "RollInfo": 5
  },
  "ChunkedTextReveal": {
    "Combat": {
      "BaseDelayPerCharMs": 20,
      "MinDelayMs": 500,
      "MaxDelayMs": 2000,
      "Strategy": "Line"
    },
    "Dungeon": {
      "BaseDelayPerCharMs": 25,
      "MinDelayMs": 800,
      "MaxDelayMs": 3000,
      "Strategy": "Semantic"
    },
    "Room": {
      "BaseDelayPerCharMs": 30,
      "MinDelayMs": 1000,
      "MaxDelayMs": 3000,
      "Strategy": "Sentence"
    },
    "Narrative": {
      "BaseDelayPerCharMs": 25,
      "MinDelayMs": 400,
      "MaxDelayMs": 2000,
      "Strategy": "Sentence"
    },
    "Default": {
      "BaseDelayPerCharMs": 30,
      "MinDelayMs": 500,
      "MaxDelayMs": 4000,
      "Strategy": "Sentence"
    }
  },
  "CombatDelays": {
    "ActionDelayMs": 1000,
    "MessageDelayMs": 200
  },
  "ProgressiveMenuDelays": {
    "BaseMenuDelay": 25,
    "ProgressiveReductionRate": 1,
    "ProgressiveThreshold": 20
  },
  "EnableGuiDelays": true,
  "EnableConsoleDelays": true
}
```

## Configuration Categories

### MessageTypeDelays

Controls delays for different UI message types (in milliseconds):

- **Combat**: Delay for combat action messages (default: 100ms)
- **System**: Delay for system messages (default: 100ms)
- **Menu**: Delay for menu items (default: 25ms)
- **Title**: Delay for title screens (default: 400ms)
- **MainTitle**: Delay for main title screen (default: 400ms)
- **Environmental**: Delay for environmental action messages (default: 150ms)
- **EffectMessage**: Delay for status effect messages (default: 50ms)
- **DamageOverTime**: Delay for damage over time messages (default: 50ms)
- **Encounter**: Delay for encounter messages (default: 67ms)
- **RollInfo**: Delay for roll information (default: 5ms)

### ChunkedTextReveal

Controls chunked text reveal presets. Each preset has:

- **BaseDelayPerCharMs**: Base delay per character in milliseconds
- **MinDelayMs**: Minimum delay between chunks
- **MaxDelayMs**: Maximum delay between chunks
- **Strategy**: Chunking strategy ("Sentence", "Line", "Paragraph", or "Semantic")

Available presets:
- **Combat**: Fast-paced combat text (Line strategy, 20ms/char)
- **Dungeon**: Dungeon exploration text (Semantic strategy, 25ms/char)
- **Room**: Room descriptions (Sentence strategy, 30ms/char)
- **Narrative**: Story and narrative text (Sentence strategy, 25ms/char)
- **Default**: Fallback configuration (Sentence strategy, 30ms/char)

### CombatDelays

Controls combat-specific delays:

- **ActionDelayMs**: Delay after a complete action is processed (default: 1000ms)
- **MessageDelayMs**: Delay between individual messages within an action (default: 200ms)

### ProgressiveMenuDelays

Controls progressive menu delay system:

- **BaseMenuDelay**: Initial delay for menu items (default: 25ms)
- **ProgressiveReductionRate**: How much delay reduces per line (default: 1ms)
- **ProgressiveThreshold**: Line count where reduction behavior changes (default: 20)

### Enable Flags

- **EnableGuiDelays**: Whether delays are enabled for GUI mode (default: true)
- **EnableConsoleDelays**: Whether delays are enabled for console mode (default: true)

## Usage Examples

### Making Combat Faster

```json
{
  "MessageTypeDelays": {
    "Combat": 50
  },
  "ChunkedTextReveal": {
    "Combat": {
      "BaseDelayPerCharMs": 10,
      "MinDelayMs": 200,
      "MaxDelayMs": 1000
    }
  },
  "CombatDelays": {
    "ActionDelayMs": 500,
    "MessageDelayMs": 100
  }
}
```

### Making Text Display Slower (More Dramatic)

```json
{
  "ChunkedTextReveal": {
    "Dungeon": {
      "BaseDelayPerCharMs": 40,
      "MinDelayMs": 1500,
      "MaxDelayMs": 5000
    },
    "Room": {
      "BaseDelayPerCharMs": 50,
      "MinDelayMs": 2000,
      "MaxDelayMs": 6000
    }
  }
}
```

### Disabling All Delays

```json
{
  "EnableGuiDelays": false,
  "EnableConsoleDelays": false
}
```

## Implementation Details

The configuration is loaded by `TextDelayConfiguration` class (`Code/Config/TextDelayConfiguration.cs`) and used by:

- **UIDelayManager**: Uses `MessageTypeDelays` and `ProgressiveMenuDelays`
- **ChunkedTextReveal**: Uses `ChunkedTextReveal` presets
- **CombatDelayManager**: Uses `CombatDelays` and enable flags

## Migration Notes

- The old `CombatDelayConfig.json` is deprecated. All delays are now in `TextDelayConfig.json`
- Default values are used if the JSON file is missing or invalid
- All existing delay behavior is preserved, just configurable now

## See Also

- `CHUNKED_TEXT_REVEAL.md` - Chunked text reveal system details
- `TEXT_DISPLAY_SYSTEM.md` - Text display system overview
- `UI_CONFIGURATION_GUIDE.md` - UI configuration guide

