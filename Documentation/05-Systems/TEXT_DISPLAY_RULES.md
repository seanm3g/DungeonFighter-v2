# Text Display Rules and Formatting Guidelines

## Overview

This document defines the rules for how text is displayed in DungeonFighter-v2, including spacing, blank lines, and section separation. These rules ensure consistent, readable output throughout the game.

## Core Principles

1. **Section Separation**: Major sections are separated by blank lines
2. **Actor Changes**: Blank lines appear when different actors take actions
3. **Narrative Beats**: Narrative text is separated from action text
4. **Consecutive Actions**: No blank lines between consecutive actions by the same actor

## Blank Line Rules

### 1. Section Boundaries

**Rule**: Add a blank line between major sections.

**Sections include:**
- Dungeon entry header
- Room entry
- Enemy encounter
- Combat actions
- Room cleared message
- Next room entry

**Example:**
```
= ENTERING DUNGEON =
Dungeon: Ancient Forest
Level Range: 1 - 1
Total Rooms: 3

= ENTERING ROOM =
Room Number: 1 of 3
Room: Forest Clearing
A peaceful clearing in the woods, dappled with sunlight.

A Blessed Phantom with Iron Sword appears.
Enemy Stats - Health: 14/14, Armor: 0
Attack: 2, Attack Speed: 6s
STR 3, AGI 6, TEC 3, INT 3

Hero Stats - Health: 20/25, Armor: 2
Attack: 2, Attack Speed: 6s
STR 3, AGI 6, TEC 3, INT 3

Blessed Phantom uses DIVINE BLESSING on Pax Stormcaller
     (roll: 19 | attack 0 - 0 armor | speed: 8.4s)
     (*any status effects from the action*)
```

### 2. Actor Changes

**Rule**: Add a blank line when the acting character changes (player ↔ enemy).

**Exception**: No blank line between consecutive actions by the same actor.

**Example - Actor Change:**
```
Pax Stormcaller hits Blessed Phantom with CRUSHING BLOW for 7 damage
     (roll: 15 | attack 7 - 0 armor | speed: 8.5s | amp: 1.0x)

Blessed Phantom hits Pax Stormcaller for 2 damage
     (roll: 12 | attack 4 - 2 armor | speed: 8.4s)
```

**Example - Same Actor (No Blank Line):**
```
Pax Stormcaller hits Blessed Phantom with CRUSHING BLOW for 7 damage
     (roll: 15 | attack 7 -> 0 armor | speed: 8.5s | amp: 1.0x)
Pax Stormcaller hits Blessed Phantom for 7 damage
     (roll: 11 | attack 7 -> 0 armor | speed: 8.5s)
```


### 3. Narrative Beats

**Rule**: Add a blank line before narrative text that appears separately from action blocks.

**Narrative beats include:**
- Battle highlights
- Critical miss descriptions
- Environmental effects
- Story moments

**Example:**
```
Pax Stormcaller hits Blessed Phantom for 7 damage
     (roll: 11 | attack 7 - 0 armor | speed: 8.5s)

The battle rages on, each strike echoing through the ancient hall.

Blessed Phantom hits Pax Stormcaller for 2 damage
     (roll: 12 | attack 4 - 2 armor | speed: 8.4s)
```

### 4. Combat Action Blocks

**Rule**: Each combat action block is a single unit with no internal blank lines.

**Action block structure:**
```
[Action Text]
     (Roll Information)
     [Status Effects if any]
     [Narrative if part of this action]
```

**Example:**
```
Pax Stormcaller hits Blessed Phantom with CRUSHING BLOW for 7 damage
     (roll: 15 | attack 7 - 0 armor | speed: 8.5s | amp: 1.0x)
     Blessed Phantom is stunned!
     The crushing blow sends shockwaves through the enemy's defenses.
```

### 5. Room Transitions

**Rule**: Add blank lines around room cleared messages and before entering new rooms.

**Example:**
```
Pax Stormcaller hits Blessed Phantom for 7 damage
     (roll: 11 | attack 7 - 0 armor | speed: 8.5s)

Room cleared!
========================================
Remaining Health: 58/60

= ENTERING ROOM =
Room Number: 2 of 3
Room: Eternal Hall
Time has no meaning in this hallowed space, where the divine presence is eternal.
```

### 6. Safe Rooms

**Rule**: Add a blank line before the "safe" message when entering an empty room.

**Example:**
```
= ENTERING ROOM =
Room Number: 1 of 3
Room: Forest Clearing
A peaceful clearing in the woods, dappled with sunlight.

It appears you are safe... for now.
```

## Complete Example: Full Combat Sequence

```
= ENTERING DUNGEON =
Dungeon: Final Sanctum
Level Range: 1 - 1
Total Rooms: 3

= ENTERING ROOM =
Room Number: 1 of 3
Room: Eternal Hall
Time has no meaning in this hallowed space, where the divine presence is eternal.

A Blessed Phantom with Iron Sword appears.
Enemy Stats - Health: 14/14, Armor: 0
Attack: STR 3, AGI 6, TEC 3, INT 3

Blessed Phantom uses DIVINE BLESSING on Pax Stormcaller
     (roll: 19 | attack 0 - 0 armor | speed: 8.4s)

Pax Stormcaller hits Blessed Phantom with CRUSHING BLOW for 7 damage
     (roll: 15 | attack 7 - 0 armor | speed: 8.5s | amp: 1.0x)

Blessed Phantom hits Pax Stormcaller for 2 damage
     (roll: 12 | attack 4 - 2 armor | speed: 8.4s)

Pax Stormcaller hits Blessed Phantom for 7 damage
     (roll: 11 | attack 7 - 0 armor | speed: 8.5s)

Total damage dealt: 14 vs 2 received.
Combos executed: 1 vs 1.

Room cleared!
========================================
Remaining Health: 58/60

= ENTERING ROOM =
Room Number: 2 of 3
Room: Eternal Hall
Time has no meaning in this hallowed space, where the divine presence is eternal.

A Shadow Wraith with Dark Blade appears.
Enemy Stats - Health: 12/12, Armor: 1
Attack: STR 4, AGI 5, TEC 4, INT 2

Shadow Wraith uses SHADOW STRIKE on Pax Stormcaller
     (roll: 18 | attack 5 - 2 armor | speed: 7.8s)

Pax Stormcaller hits Shadow Wraith with CRUSHING BLOW for 8 damage
     (roll: 16 | attack 8 - 1 armor | speed: 8.5s | amp: 1.0x)
```

## Implementation Notes

### Where Rules Are Enforced

1. **`DungeonDisplayManager.cs`**:
   - Blank line after dungeon header (before room info)
   - Blank line before enemy encounter (after room info)

2. **`BlockDisplayManager.cs`**:
   - Blank line when actor changes (in `DisplayActionBlock`)
   - Blank line before narrative blocks (in `DisplayNarrativeBlock`)

3. **`CombatMessageHandler.cs`**:
   - Blank lines around victory/defeat/room cleared messages

4. **`DungeonRunnerManager.cs`**:
   - Blank line before safe room message

### Actor Detection

The system extracts actor names from action text using `ExtractEntityNameFromMessage()`:
- Looks for patterns like `[ActorName]` or `ActorName hits`
- Compares current actor with `lastActingEntity`
- Adds blank line only when actors differ

### Section Detection

Sections are identified by:
- Method calls (`StartDungeon`, `EnterRoom`, `StartEnemyEncounter`)
- Message types (dungeon header, room info, enemy info)
- Explicit section markers (like "ENTERING DUNGEON", "ENTERING ROOM")

## Edge Cases

### Multiple Enemies in Same Room
- Each enemy encounter starts with a blank line (after previous enemy defeat)
- Combat actions follow normal actor-change rules

### Environmental Actions
- Environmental actions are treated as separate sections
- Blank line before environmental action block

### Status Effects
- Status effects are part of the action block (no blank line)
- Displayed with 4-space indentation

### Critical Miss Narratives
- Critical miss narratives are part of the action block (no blank line)
- Other narratives get blank lines before them

## Testing Checklist

When making changes to text display, verify:

- [ ] Blank line between dungeon header and room entry
- [ ] Blank line between room entry and enemy encounter
- [ ] Blank line when actor changes (player ↔ enemy)
- [ ] No blank line between consecutive actions by same actor
- [ ] Blank line before narrative beats
- [ ] Blank line before safe room message
- [ ] Blank lines around room cleared message
- [ ] Blank line before next room entry

## Future Considerations

- Consider configurable spacing rules
- May need different rules for different screen sizes
- Could add visual separators (like `=====` lines) for major sections
- Consider animation timing for blank line appearance

