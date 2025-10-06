# UI Configuration Guide

## Overview

The DungeonFighter-v2 UI system uses a **beat-based timing system** that provides consistent, relative pacing across all game elements. Instead of setting individual delays, you control the overall game rhythm through beat multipliers and a base beat length.

## Beat-Based Timing System

### Core Concept
- **Base Beat Length**: The fundamental timing unit (in milliseconds)
- **Beat Multipliers**: Relative durations for different message types
- **Menu Speed**: Independent timing for responsive menus

### Example
- Base Beat Length: 100ms
- Combat: 1.0 beats = 100ms
- Stun: 0.5 beats = 50ms  
- Environmental: 1.5 beats = 150ms
- Title: 4.0 beats = 400ms

## Configuration Files

### Primary Configuration: `GameData/UIConfiguration.json`

This is the main configuration file that controls all UI behavior:

```json
{
  "EnableDelays": true,
  "FastCombat": false,
  "DisableAllOutput": false,
  "AddBlankLinesBetweenEntities": true,
  "AddBlankLinesAfterEnvironmentalActions": true,
  "AddBlankLinesAfterStunMessages": true,
  "AddBlankLinesAfterDamageOverTime": true,
  "BeatTiming": {
    "BaseBeatLengthMs": 100,
    "MenuSpeedMs": 25,
    "BeatMultipliers": {
      "Combat": 1.0,
      "System": 1.0,
      "Environmental": 1.5,
      "StunMessage": 0.5,
      "DamageOverTime": 0.5,
      "Title": 4.0
    }
  },
  "ShowTimingInfo": false,
  "ShowEntityNames": true,
  "ShowDetailedCombatInfo": true,
  "GroupRelatedMessages": true,
  "ShowStatusEffectIndentation": true
}
```

## Configuration Options

### Display Control
- **`EnableDelays`**: Master switch for all delays (true/false)
- **`FastCombat`**: When true, sets combat delays to zero
- **`DisableAllOutput`**: Completely disables all UI output (useful for testing)

### Spacing Control
- **`AddBlankLinesBetweenEntities`**: Adds blank lines when different entities act
- **`AddBlankLinesAfterEnvironmentalActions`**: Adds blank lines after environmental actions
- **`AddBlankLinesAfterStunMessages`**: Adds blank lines after stun status messages
- **`AddBlankLinesAfterDamageOverTime`**: Adds blank lines after damage over time messages

### Beat-Based Timing Configuration
- **`BaseBeatLengthMs`**: The fundamental timing unit (e.g., 100ms = 1 beat)
- **`MenuSpeedMs`**: Independent menu timing (not affected by beat system)
- **`BeatMultipliers`**: Relative durations for different message types:
  - **`Combat`**: 1.0 beats (standard timing)
  - **`System`**: 1.0 beats (same as combat)
  - **`Environmental`**: 1.5 beats (slightly longer for dramatic effect)
  - **`StunMessage`**: 0.5 beats (quick, snappy)
  - **`DamageOverTime`**: 0.5 beats (quick, like stun)
  - **`Title`**: 4.0 beats (dramatic, long pause)

### Display Formatting
- **`ShowTimingInfo`**: Shows timing information in messages
- **`ShowEntityNames`**: Shows entity names in messages
- **`ShowDetailedCombatInfo`**: Shows detailed combat information
- **`GroupRelatedMessages`**: Groups related messages together
- **`ShowStatusEffectIndentation`**: Indents status effect messages

## Preset Configurations

The system includes several preset configurations for common use cases:

### Balanced (Default)
```csharp
var config = UIConfiguration.CreatePreset(UIConfigurationPreset.Balanced);
```
- Base Beat Length: 100ms
- Combat: 1.0 beats (100ms)
- Environmental: 1.5 beats (150ms)
- Stun: 0.5 beats (50ms)
- Title: 4.0 beats (400ms)
- Menu Speed: 25ms

### Fast Combat
```csharp
var config = UIConfiguration.CreatePreset(UIConfigurationPreset.Fast);
```
- All delays set to 0
- No blank lines between entities
- Optimized for speed

### Cinematic
```csharp
var config = UIConfiguration.CreatePreset(UIConfigurationPreset.Cinematic);
```
- Base Beat Length: 200ms (slower beats)
- Combat: 1.5 beats (300ms)
- Environmental: 2.0 beats (400ms)
- Title: 2.5 beats (500ms)
- All spacing enabled
- Optimized for dramatic presentation

### Snappy
```csharp
var config = UIConfiguration.CreatePreset(UIConfigurationPreset.Snappy);
```
- Base Beat Length: 50ms (quick beats)
- Combat: 1.0 beats (50ms)
- Environmental: 1.2 beats (60ms)
- Stun: 0.4 beats (20ms)
- Title: 2.0 beats (100ms)
- Menu Speed: 15ms

### Relaxed
```csharp
var config = UIConfiguration.CreatePreset(UIConfigurationPreset.Relaxed);
```
- Base Beat Length: 150ms (slower beats)
- Combat: 1.0 beats (150ms)
- Environmental: 2.0 beats (300ms)
- Stun: 0.7 beats (105ms)
- Title: 5.0 beats (750ms)
- Menu Speed: 50ms

### Debug
```csharp
var config = UIConfiguration.CreatePreset(UIConfigurationPreset.Debug);
```
- All delays set to 0
- All information displayed
- Optimized for debugging

### Instant
```csharp
var config = UIConfiguration.CreatePreset(UIConfigurationPreset.Instant);
```
- All delays set to 0
- No spacing
- Maximum speed

## Message Types

The system recognizes different message types for appropriate delay and spacing:

- **`Combat`**: Regular combat actions
- **`Menu`**: Menu interactions
- **`System`**: System messages
- **`Title`**: Title screens
- **`Environmental`**: Environmental actions (room effects)
- **`StunMessage`**: Stun status messages
- **`DamageOverTime`**: Damage over time effects (bleed, poison, etc.)

## Usage Examples

### Adjusting Overall Game Pace
To make the entire game faster or slower, adjust the base beat length:
```json
{
  "BeatTiming": {
    "BaseBeatLengthMs": 50,  // Faster game (was 100ms)
    "BeatMultipliers": {
      "Combat": 1.0,        // Now 50ms
      "Environmental": 1.5, // Now 75ms
      "StunMessage": 0.5,   // Now 25ms
      "Title": 4.0          // Now 200ms
    }
  }
}
```

### Creating Snappy Combat
For quick, responsive combat:
```json
{
  "BeatTiming": {
    "BaseBeatLengthMs": 30,
    "BeatMultipliers": {
      "Combat": 1.0,        // 30ms
      "Environmental": 1.0, // 30ms (same as combat)
      "StunMessage": 0.3,   // 9ms
      "DamageOverTime": 0.3 // 9ms
    }
  }
}
```

### Creating Cinematic Experience
For a more dramatic, slower-paced experience:
```json
{
  "BeatTiming": {
    "BaseBeatLengthMs": 200, // Slower beats
    "BeatMultipliers": {
      "Combat": 1.5,        // 300ms
      "Environmental": 2.0, // 400ms
      "StunMessage": 1.0,   // 200ms
      "Title": 3.0          // 600ms
    }
  }
}
```

### Disabling Spacing
To remove blank lines between entities:
```json
{
  "AddBlankLinesBetweenEntities": false,
  "AddBlankLinesAfterEnvironmentalActions": false,
  "AddBlankLinesAfterStunMessages": false,
  "AddBlankLinesAfterDamageOverTime": false
}
```

## Runtime Configuration

You can also change configuration at runtime:

```csharp
// Reload configuration from file
UIManager.ReloadConfiguration();

// Create and apply a preset
var config = UIConfiguration.CreatePreset(UIConfigurationPreset.Fast);
config.SaveToFile("GameData/UIConfiguration.json");
UIManager.ReloadConfiguration();
```

## Integration with Existing Systems

The new UI configuration system integrates seamlessly with existing systems:

- **CombatManager**: Uses configurable methods for all message types
- **UIManager**: All methods now use the configuration system
- **TurnManager**: Damage over time messages use configurable delays
- **TextDisplaySystem**: Compatible with the new configuration system

## Backward Compatibility

The system maintains backward compatibility with existing code:
- All existing `UIManager` methods continue to work
- Old delay systems are automatically mapped to new configuration
- No breaking changes to existing functionality

## Troubleshooting

### Configuration Not Loading
- Check that `GameData/UIConfiguration.json` exists and is valid JSON
- Verify file permissions
- Check console for error messages

### Delays Not Working
- Ensure `EnableDelays` is set to `true`
- Check that `FastCombat` is not overriding delays
- Verify delay values are positive numbers

### Spacing Issues
- Check spacing configuration flags
- Ensure `AddBlankLinesBetweenEntities` is enabled for entity spacing
- Verify specific spacing flags for different message types

## Advanced Configuration

### Custom Message Types
You can extend the system by adding new message types to `UIMessageType` enum and corresponding delay configuration.

### Dynamic Configuration
The system supports runtime configuration changes, allowing for dynamic UI adjustments based on game state or user preferences.

### Performance Considerations
- Set delays to 0 for maximum performance
- Disable spacing for faster display
- Use `DisableAllOutput` for headless operation

This configuration system provides complete control over the UI experience while maintaining simplicity and ease of use.
