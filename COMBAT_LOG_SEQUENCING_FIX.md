# Combat Log Sequencing Fix

## Problem
When picking a dungeon, the combat log was showing information in the wrong order:
- First combat: Would show "rooms cleared" messages from previous sessions
- Second combat: Would show all accumulated information from the first room/enemy encounter
- The dungeon information was appearing late or not at all in the correct sequence
- The combat log was confusing because it accumulated historical data

## Root Cause
The `dungeonLog` list was being used as an accumulator throughout the entire dungeon run:
1. Dungeon info was added to `dungeonLog` at the start
2. Each room's info was appended to `dungeonLog`
3. Each enemy's info was appended to `dungeonLog`
4. By the second encounter, `dungeonLog` contained all previous information

When `ResetForNewBattle()` was called, it would display the entire accumulated `dungeonLog`, which included:
- Dungeon info
- Room 1 info
- Enemy 1 info
- Room 2 info
- Enemy 2 info
- etc.

This created a confusing experience where old information persisted and appeared in the wrong context.

## Solution
Changed the approach to build a **fresh context for each encounter**:

1. **Separated dungeon header info**: Created `dungeonHeaderInfo` list to store dungeon information separately
2. **Separated room info**: Created `currentRoomInfo` list to store current room information separately
3. **Rebuild context per encounter**: For each enemy encounter, the `dungeonLog` is now rebuilt from scratch:
   - Clear `dungeonLog`
   - Add `dungeonHeaderInfo` (constant for the dungeon)
   - Add `currentRoomInfo` (current room only)
   - Add current enemy info

This ensures each encounter shows only the relevant context:
- Dungeon: Name, Level Range, Total Rooms
- Current Room: Name, Description
- Current Enemy: Name, Stats, Attack info

## Files Changed
- **`Code/Game/Game.cs`**:
  - Added `dungeonHeaderInfo` field to store dungeon information
  - Added `currentRoomInfo` field to store current room information
  - Modified `RunDungeon()` to populate `dungeonHeaderInfo` instead of `dungeonLog`
  - Modified `ProcessRoom()` to populate `currentRoomInfo` instead of `dungeonLog`
  - Modified `ProcessEnemyEncounter()` to rebuild `dungeonLog` from scratch for each encounter

## Benefits
1. **Clear Context**: Each combat encounter shows exactly the relevant information
2. **No Accumulation**: Historical data doesn't persist into later encounters
3. **Consistent Display**: Every encounter follows the same pattern (dungeon → room → enemy)
4. **Better UX**: Players can easily understand where they are and what they're facing

## Testing
- Build succeeded with no errors or warnings
- Code compiles correctly
- No linting errors introduced

## Related Systems
- Combat System (`CombatManager`, `CombatStateManager`)
- UI System (`CanvasUIManager`)
- Dungeon System (`Dungeon`, `DungeonRunner`)
- Enemy Encounter System

## Implementation Notes
The fix maintains the existing flow but changes how information is organized:
- Information is stored in separate buckets (header, room, enemy)
- Context is rebuilt for each display rather than accumulated
- Original display logic in `CanvasUIManager` remains unchanged

