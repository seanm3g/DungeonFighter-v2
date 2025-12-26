# Animation System Documentation

## Overview

The animation system provides visual effects for text rendering in the Avalonia UI. It consists of two main components:

1. **Brightness Mask**: A wave effect that creates cloud-like light and dark spots drifting across text
2. **Undulation**: A global brightness pulsing effect that makes text appear to breathe or shimmer

Both effects are implemented using sine wave calculations and are applied character-by-character during rendering.

---

## Architecture

### Base Class

**`BaseAnimationState`** (`Code/UI/Avalonia/Managers/BaseAnimationState.cs`)
- Abstract base class containing shared animation logic
- Provides brightness mask and undulation functionality
- Thread-safe implementation using locks
- Configuration loaded via abstract `LoadConfiguration()` method

### Derived Classes

**`CritAnimationState`** (`Code/UI/Avalonia/Managers/CritAnimationState.cs`)
- Singleton for critical hit line animations
- Inherits from `BaseAnimationState`
- Uses `DungeonSelectionAnimation` configuration

**`DungeonSelectionAnimationState`** (`Code/UI/Avalonia/Managers/DungeonSelectionAnimationState.cs`)
- Singleton for dungeon selection screen animations
- Inherits from `BaseAnimationState`
- Uses `DungeonSelectionAnimation` configuration

### Usage in Renderers

The animation state is used by:
- `DisplayRenderer` - For "ENTERING DUNGEON" lines
- `SegmentRenderer` - For critical hit lines
- `DungeonSelectionRenderer` - For dungeon name display

---

## Brightness Mask System

### How It Works

The brightness mask creates a wave effect across text by:
1. Using two sine waves at different frequencies
2. Combining them for an organic, cloud-like appearance
3. Moving the pattern over time by incrementing an offset
4. Applying brightness adjustments per character based on position

### Mathematical Formula

```csharp
wave1 = sin((position + offset + lineOffset) * π / waveLength)
wave2 = sin((position + offset * 0.7 + lineOffset * 0.8) * π / (waveLength * 1.5)) * 0.5
combinedWave = (wave1 + wave2) / 1.5
brightnessAdjustment = combinedWave * intensity
```

### Configuration

Located in `GameData/UIConfiguration.json`:

```json
"DungeonSelectionAnimation": {
  "BrightnessMask": {
    "Enabled": false,        // Enable/disable the effect
    "Intensity": 10.0,        // Maximum brightness adjustment (+/- percent)
    "WaveLength": 5.0,        // Length of wave pattern (higher = slower changes)
    "UpdateIntervalMs": 1000  // How often the mask moves (milliseconds)
  }
}
```

### Parameters

- **Enabled**: Turn the effect on/off
- **Intensity**: Maximum brightness adjustment in percent (e.g., 10.0 = ±10% brightness)
  - Lower values = more subtle effect
  - Higher values = more dramatic effect
- **WaveLength**: Controls wave frequency
  - Lower values = tighter waves, more flickering
  - Higher values = wider waves, smoother effect
- **UpdateIntervalMs**: Animation speed
  - Lower values = faster movement
  - Higher values = slower movement

---

## Undulation System

### How It Works

Undulation creates a global brightness pulsing effect by:
1. Maintaining a phase value that increments over time
2. Using sine wave to calculate brightness adjustment
3. Applying the same adjustment to all characters simultaneously
4. Creating a "breathing" or "shimmering" effect

### Mathematical Formula

```csharp
phase += speed (each update)
brightnessAdjustment = sin(phase) * 0.3
```

The adjustment ranges from -0.3 to +0.3, which is then multiplied by 3.0 in renderers for a total range of -0.9 to +0.9 brightness factor.

### Configuration

Located in `GameData/UIConfiguration.json`:

```json
"DungeonSelectionAnimation": {
  "UndulationSpeed": 0.05,        // Speed of phase increment
  "UndulationIntervalMs": 50       // Update frequency (milliseconds)
}
```

### Parameters

- **UndulationSpeed**: How fast the phase advances
  - Lower values = slower, more subtle pulsing
  - Higher values = faster, more noticeable pulsing
- **UndulationIntervalMs**: How often the animation updates
  - Lower values = smoother animation (more CPU usage)
  - Higher values = choppier animation (less CPU usage)

---

## Usage Examples

### In DisplayRenderer

```csharp
// Get brightness mask adjustment from centralized state
float brightnessAdjustment = animationState.GetBrightnessAt(startCharPosition, lineOffset);
double brightnessFactor = 1.0 + (brightnessAdjustment / 100.0) * 2.0;
brightnessFactor = Math.Max(0.3, Math.Min(2.0, brightnessFactor));

// Get undulation brightness from centralized state
double undulationBrightness = animationState.GetUndulationBrightness();
brightnessFactor += undulationBrightness * 3.0;
brightnessFactor = Math.Max(0.3, Math.Min(2.0, brightnessFactor));

// Apply brightness adjustments to color
Color adjustedColor = AdjustColorBrightness(baseColor, brightnessFactor);
```

### Animation Updates

Animations are updated by `CanvasAnimationManager` using timers:

```csharp
// Brightness mask timer
brightnessMaskTimer = new Timer(UpdateBrightnessMask, null, interval, interval);

// Undulation timer
undulationTimer = new Timer(UpdateUndulation, null, interval, interval);
```

The timers call `AdvanceBrightnessMask()` and `AdvanceUndulation()` on the animation state instances.

---

## Thread Safety

All animation state operations are thread-safe:
- Separate locks for brightness mask and undulation state
- Lock-free reads where possible (copying values)
- Singleton pattern ensures single instance per animation type

---

## Performance Considerations

1. **Character-by-Character Calculation**: Brightness mask is calculated per character, which is necessary for the wave effect
2. **Sine Wave Calculations**: Fast operations, but called frequently during rendering
3. **Timer Frequency**: Lower intervals = smoother animation but more CPU usage
4. **Thread Safety Overhead**: Minimal - locks are only held briefly

---

## Configuration Tips

### Subtle Effects
- Brightness Mask: `Intensity: 5.0`, `WaveLength: 8.0`, `UpdateIntervalMs: 2000`
- Undulation: `Speed: 0.02`, `IntervalMs: 100`

### Dramatic Effects
- Brightness Mask: `Intensity: 20.0`, `WaveLength: 3.0`, `UpdateIntervalMs: 500`
- Undulation: `Speed: 0.1`, `IntervalMs: 30`

### Disable Effects
- Set `BrightnessMask.Enabled: false` to disable brightness mask
- Set `UndulationSpeed: 0` to disable undulation (or don't start the timer)

---

## Related Documentation

- [UNDULATE_FEATURE.md](UNDULATE_FEATURE.md) - Documents color pattern undulation (different from brightness undulation)
- [COLOR_SYSTEM.md](COLOR_SYSTEM.md) - Color system overview
- [COLOR_SYSTEM_IMPLEMENTATION_SUMMARY.md](COLOR_SYSTEM_IMPLEMENTATION_SUMMARY.md) - Color system details

---

**Date:** Current  
**System:** Animation System - Brightness Mask & Undulation  
**Status:** ✅ PRODUCTION READY  
**Version:** 2.0 (Refactored with BaseAnimationState)

