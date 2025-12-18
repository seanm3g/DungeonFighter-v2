# Combat Text Reveal - Progressive Line-by-Line Display

## Overview 🎮

Combat text now displays **line-by-line with natural delays** instead of all at once! This creates a more engaging and readable combat experience where each action reveals progressively, giving players time to absorb the combat information.

## What Changed

### Before (Instant Display)
All combat text appeared simultaneously:
```
Hero hits Enemy for 25 damage
    (roll: 15 | attack 18 - 10 armor = 8 | speed: 2.5s)
    Enemy is weakened!
```

### After (Progressive Reveal with Delays)
Combat text appears line-by-line with natural pacing:
```
Hero hits Enemy for 25 damage
[pause: 500-1000ms]
    (roll: 15 | attack 18 - 10 armor = 8 | speed: 2.5s)
[pause: 300-800ms]
    Enemy is weakened!
```

## Features ✨

✅ **Combat Actions** - Action text reveals line by line  
✅ **Narrative Events** - Significant combat moments display dramatically  
✅ **Environmental Actions** - Room effects appear progressively  
✅ **Status Effects** - Debuffs and buffs display with timing  
✅ **Natural Pacing** - Delays proportional to text length  
✅ **Color Support** - Full integration with color system  
✅ **Configurable** - Can be adjusted in code if needed  

## How It Works

The system uses **ChunkedTextReveal** to split combat text into logical chunks:

1. **Splits** text by lines (optimal for combat info)
2. **Calculates** delays based on text length
3. **Reveals** each chunk with natural timing
4. **Applies** to action blocks, narratives, and effects

### Timing Details

#### Combat Actions
- **Strategy**: Line-by-line
- **Base Delay**: 20ms per character (fast)
- **Min Delay**: 300ms (quick reveal)
- **Max Delay**: 1500ms

#### Narrative Events
- **Strategy**: Sentence-by-sentence
- **Base Delay**: 25ms per character (dramatic)
- **Min Delay**: 400ms
- **Max Delay**: 2000ms (impactful)

#### Environmental Actions
- **Strategy**: Line-by-line
- **Base Delay**: 22ms per character
- **Min Delay**: 350ms
- **Max Delay**: 1500ms

#### Status Effects
- **Strategy**: Line-by-line
- **Base Delay**: 15ms per character (quick)
- **Min Delay**: 200ms
- **Max Delay**: 800ms (immediate)

## Example Combat Sequence

### Initial Attack
```
[Show immediately]
Warrior hits Goblin for 25 damage
[500-1000ms delay]
    (roll: 14 | attack 16 - 5 armor = 11 | speed: 1.2s)
[300-600ms delay]
    Goblin is weakened!
```

### Combo Attack
```
[Show immediately]
Warrior uses POWER STRIKE on Goblin for 45 damage
[600-1200ms delay]
    (roll: 18 | attack 20 - 5 armor = 15 | speed: 1.8s)
[400-800ms delay]
    Goblin is poisoned for 3 turns!
```

### Narrative Event
```
[Show immediately - First chunk]
Warrior draws first blood!
[400-800ms delay]
Warrior's relentless assault has brought the enemy to its knees!
[600-1200ms delay]
```

## Technical Details

### Files Modified

1. **`Code/UI/BlockDisplayManager.cs`**
   - Enhanced `DisplayActionBlock()` with chunked reveal
   - Enhanced `DisplayNarrativeBlock()` with dramatic pacing
   - Enhanced `DisplayEnvironmentalBlock()` with progressive display

### Integration Points

The chunked reveal is automatically applied to:
- ✅ All combat actions (basic attacks, combos, abilities)
- ✅ Narrative events (first blood, health reversals, near death)
- ✅ Environmental effects (room hazards, traps)
- ✅ Status effects (poison, weaken, stun, etc.)

### How Combat Text Flows

```
CombatTurnHandler
  ↓
  ExecuteActionWithUIAndStatusEffects()
  ↓
  TextDisplayIntegration.DisplayCombatAction()
  ↓
  BlockDisplayManager.DisplayActionBlock()
  ├─ UIManager.WriteChunked() [Combat Action]
  ├─ UIManager.WriteLine() [Roll Info]
  ├─ UIManager.WriteChunked() [Status Effects - each]
  ↓
  BlockDisplayManager.DisplayNarrativeBlock()
  └─ UIManager.WriteChunked() [Narrative Text]
```

## Configuration

### Unified Delay Configuration System

All text delays are now configurable via `GameData/TextDelayConfig.json`. This includes:
- Message type delays (Combat, System, Menu, Title, etc.)
- Chunked text reveal presets (Combat, Dungeon, Room, Narrative)
- Combat action delays
- Progressive menu delays

### Default Settings (Optimized for Combat)

All timing is pre-optimized for good readability during combat. Current settings in `TextDelayConfig.json`:

```json
{
  "ChunkedTextReveal": {
    "Combat": {
      "BaseDelayPerCharMs": 20,
      "MinDelayMs": 500,
      "MaxDelayMs": 2000,
      "Strategy": "Line"
    },
    "Narrative": {
      "BaseDelayPerCharMs": 25,
      "MinDelayMs": 400,
      "MaxDelayMs": 2000,
      "Strategy": "Sentence"
    }
  }
}
```

### Custom Configuration

To customize delays, edit `GameData/TextDelayConfig.json`:

```json
{
  "ChunkedTextReveal": {
    "Combat": {
      "BaseDelayPerCharMs": 15,  // Faster
      "MinDelayMs": 200,
      "MaxDelayMs": 1000,
      "Strategy": "Line"
    }
  }
}
```

For programmatic customization, you can still pass custom `RevealConfig`:

```csharp
// Custom configuration in code
UIManager.WriteChunked(text, new ChunkedTextReveal.RevealConfig
{
    Strategy = ChunkedTextReveal.ChunkStrategy.Line,
    BaseDelayPerCharMs = 15,
    MinDelayMs = 200,
    MaxDelayMs = 1000
});

// To disable (instant display)
UIManager.WriteChunked(text, new ChunkedTextReveal.RevealConfig
{
    Enabled = false
});
```

## User Experience Benefits

🎯 **Better Readability** - Don't get overwhelmed by walls of text  
⚡ **Natural Pacing** - Matches reading speed and combat rhythm  
🎭 **Enhanced Drama** - Significant moments feel more impactful  
🧠 **Improved Comprehension** - Time to process each combat detail  
🎮 **More Engaging** - Combat feels less mechanical, more cinematic  

## Performance Impact

- **Overhead**: Minimal (text split once, simple delays)
- **Memory**: Very low (temporary List<string>)
- **CPU**: Negligible (Thread.Sleep is very efficient)
- **Compatibility**: Works with console and GUI modes
- **Color System**: Full integration, no conflicts

## Comparison with Dungeon Exploration

Similar to the **Chunked Text Reveal** system used for dungeon exploration, but optimized for combat:

| Feature | Dungeon Exploration | Combat Display |
|---------|-------------------|-----------------|
| Strategy | Semantic (smart) | Line (fast) |
| Base Delay | 30ms per char | 20ms per char |
| Min Delay | 500ms | 300ms |
| Max Delay | 4000ms | 1500ms |
| Purpose | Storytelling | Action clarity |

## Testing

The feature has been thoroughly tested:
- ✅ Builds without errors
- ✅ Integrates with existing UI system
- ✅ Works with color markup
- ✅ Compatible with console and GUI modes
- ✅ Natural delays feel right during gameplay

## Related Documentation

- **Chunked Text Reveal System**: `README_CHUNKED_TEXT_REVEAL.md`
- **Architecture**: `Documentation/01-Core/ARCHITECTURE.md`
- **UI System**: `Code/UI/Avalonia/`
- **Combat System**: `Code/Combat/`

## Future Enhancements

Possible improvements for future versions:

1. **Configurable Timing** - User settings to adjust reveal speed
2. **Animation Effects** - Color transitions during reveal
3. **Sound Effects** - Audio feedback for each line
4. **Performance Options** - Disable for very fast combat
5. **Accessibility** - Option to disable for accessibility needs

## Troubleshooting

### Text appears too slowly
Edit `BaseDelayPerCharMs` in `DisplayActionBlock()`:
```csharp
BaseDelayPerCharMs = 15,  // Faster (was 20)
MinDelayMs = 200,          // Lower minimum
```

### Text appears too quickly
```csharp
BaseDelayPerCharMs = 30,  // Slower (was 20)
MaxDelayMs = 2500,         // Higher maximum
```

### Want instant display (speedrun mode)
```csharp
Enabled = false  // Disable chunked reveal
```

## Summary

Combat text now reveals progressively, creating a more engaging and readable experience. The system automatically applies optimal timing for:
- **Quick actions** (fast reveal)
- **Dramatic moments** (slower reveal)
- **Status effects** (immediate but paced)

All changes are backward compatible and don't affect gameplay mechanics—only the UI display timing.

---

**Feature**: Combat Text Reveal System  
**Version**: 1.0  
**Status**: ✅ Ready to Use  
**Build**: ✅ Passing  
**Documentation**: ✅ Complete

Enjoy clearer, more engaging combat! ⚔️


