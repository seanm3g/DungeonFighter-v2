# Dungeon Display Unified System

## Overview

The Dungeon Display Unified System refactors how dungeon information, room information, enemy information, and combat logs are managed and displayed. Previously, this information was scattered across multiple systems (GameNarrativeManager, display buffer, context manager) requiring manual syncing and leading to bugs.

## Architecture

### DungeonDisplayManager

The new `DungeonDisplayManager` class provides a single source of truth for all dungeon-related display information:

- **Dungeon Header**: Dungeon name, level range, total rooms
- **Room Info**: Room name, description, room number
- **Enemy Info**: Enemy name, stats, weapon information
- **Combat Log**: All combat events and messages

### Key Benefits

1. **Single Source of Truth**: All dungeon display information is managed in one place
2. **Automatic Syncing**: Automatically syncs to GameNarrativeManager, display buffer, and context manager
3. **Clean Lifecycle**: Provides clear methods for dungeon lifecycle (StartDungeon, EnterRoom, StartEnemyEncounter)
4. **No Duplication**: Eliminates the need to manually sync between multiple systems
5. **Consistent Ordering**: Ensures information is always displayed in the correct order

## Usage

### Starting a Dungeon

```csharp
displayManager.StartDungeon(dungeon, player);
```

This:
- Clears all previous information
- Builds dungeon header
- Syncs to narrative manager
- Sets UI context
- Adds to display buffer

### Entering a Room

```csharp
displayManager.EnterRoom(room, roomNumber, totalRooms, isFirstRoom);
```

This:
- Clears room-specific info (keeps dungeon header)
- Builds room information
- Syncs to narrative manager
- Sets UI context
- Adds to display buffer

### Starting Enemy Encounter

```csharp
displayManager.StartEnemyEncounter(enemy);
```

This:
- Stores current enemy
- Builds enemy information
- Syncs to narrative manager (combines header + room + enemy)
- Sets UI context
- Adds to display buffer

### Adding Combat Events

```csharp
displayManager.AddCombatEvent(message);
```

This:
- Adds to combat log
- Adds to narrative manager's dungeon log
- Adds to display buffer

## Integration with Existing Systems

### GameNarrativeManager

The DungeonDisplayManager automatically syncs to GameNarrativeManager:
- `DungeonHeaderInfo` - synced when dungeon starts
- `CurrentRoomInfo` - synced when entering a room
- `DungeonLog` - synced with complete display log (header + room + enemy + combat)

### Display Buffer

The DungeonDisplayManager automatically adds information to the display buffer through the UI manager, ensuring it's visible immediately.

### Context Manager

The DungeonDisplayManager automatically sets UI context:
- Character
- Dungeon name
- Room name
- Current enemy
- Dungeon context (complete display log)

## Refactored Code

### DungeonRunnerManager

The `DungeonRunnerManager` has been significantly simplified:

**Before**: Manual building of dungeon header, room info, enemy info, manual syncing to multiple systems, duplication of code

**After**: Simple calls to display manager methods:
- `displayManager.StartDungeon()` - replaces ~30 lines of manual header building
- `displayManager.EnterRoom()` - replaces ~40 lines of manual room info building
- `displayManager.StartEnemyEncounter()` - replaces ~50 lines of manual enemy info building and syncing

## Display Order

The unified system ensures information is always displayed in the correct order:

1. **Dungeon Header** (shown once at start)
   - Dungeon name
   - Level range
   - Total rooms

2. **Room Info** (shown when entering each room)
   - Room header
   - Room number
   - Room name
   - Room description

3. **Enemy Info** (shown when encountering enemy)
   - Enemy name and weapon
   - Enemy stats
   - Enemy attack stats

4. **Combat Log** (shown during combat)
   - All combat events and messages

## Benefits

1. **Eliminates Bugs**: No more syncing issues between systems
2. **Easier Maintenance**: All display logic in one place
3. **Consistent Behavior**: Information always displays correctly
4. **Cleaner Code**: Removed ~120 lines of duplicate/syncing code from DungeonRunnerManager
5. **Better Testing**: Single system to test instead of multiple interacting systems

## Future Improvements

Potential enhancements:
- Add methods for room completion messages
- Add methods for dungeon completion summary
- Add support for custom formatting per section
- Add event notifications when display state changes

