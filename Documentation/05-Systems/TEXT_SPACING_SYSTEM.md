# Text Spacing System

## Overview

The `TextSpacingSystem` provides a centralized, context-aware spacing system that maps directly to the reference output structure. This makes it easy to adjust spacing to match the reference by defining rules based on what block type follows what (transition-based spacing).

## Key Benefits

1. **Context-Aware**: Spacing rules are based on block transitions (what came before), not just block types
2. **Declarative**: All spacing rules are defined in one place, easy to read and modify
3. **Reference-Mapped**: Rules directly correspond to the reference output structure
4. **Easy to Adjust**: To change spacing, simply update the rule dictionary

## How It Works

### Block Types

The system defines all block types in the display system:

```csharp
public enum BlockType
{
    // Section headers
    DungeonHeader,      // === ENTERING DUNGEON ===
    RoomHeader,         // === ENTERING ROOM ===
    RoomInfo,           // Room Number, Room name, description
    EnemyAppearance,    // "A Enemy appears."
    EnemyStats,         // Enemy Stats - Health: X/Y, Armor: Z
    HeroStats,          // Hero Stats - Health: X/Y, Armor: Z
    
    // Combat blocks
    CombatAction,       // "Actor hits Target for X damage" + roll info
    EnvironmentalAction, // "Room uses Action on Target!"
    StatusEffect,       // Status effect messages (part of action block)
    PoisonDamage,       // "[Actor] takes X poison damage"
    
    // Narrative blocks
    Narrative,          // Battle narrative text
    CriticalMissNarrative, // Critical miss description (part of action block)
    
    // System blocks
    RoomCleared,        // "Room cleared!" message
    SafeRoom,           // "It appears you are safe..."
    SystemMessage,      // Other system messages
    
    // UI blocks
    StatsBlock,         // Consecutive stats display
    MenuBlock           // Menu items
}
```

### Spacing Rules

Spacing rules are defined as transitions: `(PreviousBlockType, CurrentBlockType) => blank lines before current`

Example rules:
```csharp
(BlockType.DungeonHeader, BlockType.RoomHeader) = 1,  // 1 blank line
(BlockType.EnemyStats, BlockType.CombatAction) = 1,   // First combat action
(BlockType.CombatAction, BlockType.CombatAction) = 0, // No blank line between actions
(BlockType.CombatAction, BlockType.EnvironmentalAction) = 1, // Blank line before environmental
```

### Usage

1. **Before displaying a block**: Call `TextSpacingSystem.ApplySpacingBefore(BlockType)` to add appropriate spacing
2. **After displaying a block**: Call `TextSpacingSystem.RecordBlockDisplayed(BlockType)` to track what was displayed
3. **Reset for new battle**: Call `TextSpacingSystem.Reset()` when starting a new dungeon/battle

Example:
```csharp
// Before displaying combat action
TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.CombatAction);

// Display the action
UIManager.WriteLine(actionText);

// Record that it was displayed
TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.CombatAction);
```

## Reference Output Mapping

The spacing rules directly map to the reference output structure:

```
=== ENTERING DUNGEON ===          [DungeonHeader]
Dungeon: Ancient Library
Level Range: 1 - 1
Total Rooms: 3
                                    [1 blank line]
=== ENTERING ROOM ===              [RoomHeader]
Room Number: 1 of 3
Room: Magic Vault                  [RoomInfo]
Contained spells...
                                    [1 blank line]
A Scroll Guardian appears.         [EnemyAppearance]
                                    [1 blank line]
Enemy Stats - Health: 51/51        [EnemyStats]
Attack: STR 5, AGI 4...
                                    [1 blank line]
Fenris Firebrand hits...           [CombatAction]
    (roll: 19...)
Scroll Guardian hits...            [CombatAction - no blank line]
    (roll: 8...)
```

## Adjusting Spacing

To adjust spacing to match the reference:

1. **Identify the transition**: What block type comes before what?
2. **Find the rule**: Look in `TextSpacingSystem.cs` for the transition rule
3. **Update the value**: Change the number of blank lines (0, 1, 2, etc.)
4. **Add new rules**: If a transition doesn't exist, add it to the dictionary

Example: To add a blank line between combat actions:
```csharp
(BlockType.CombatAction, BlockType.CombatAction) = 1,  // Changed from 0 to 1
```

## Integration with BlockDisplayManager

`BlockDisplayManager` has been refactored to use `TextSpacingSystem`:

- All `Display*Block` methods now call `TextSpacingSystem.ApplySpacingBefore()` before displaying
- All methods call `TextSpacingSystem.RecordBlockDisplayed()` after displaying
- The old `ManageBlockSpacing()` and `ApplyBlockSpacing()` methods have been removed

## Special Cases

### Status Effects
Status effects are part of action blocks and don't affect spacing. They're not recorded as separate blocks.

### Critical Miss Narratives
Critical miss narratives are part of action blocks and don't affect spacing. They're not recorded as separate blocks.

### Poison Damage
Poison damage messages use `BlockType.PoisonDamage` and have specific spacing rules:
- 1 blank line after combat actions
- 0 blank lines before next combat action

## Testing

When adjusting spacing, verify:
- [ ] Spacing matches the reference output
- [ ] No double blank lines
- [ ] No missing blank lines between sections
- [ ] Combat actions appear consecutively (no blank lines between)
- [ ] Environmental actions have blank lines before them
- [ ] Poison damage has blank lines before it

## Future Enhancements

- Could add support for conditional spacing (e.g., different rules based on game state)
- Could add spacing rules based on content (e.g., different spacing for critical hits)
- Could make spacing rules configurable via JSON

