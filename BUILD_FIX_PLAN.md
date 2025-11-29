# Build Error Fix Plan

## Current Build Errors

1. **CS0101**: Duplicate class definitions in namespace 'RPGGame':
   - `Game` - Location: `Code/Game/Game.cs(37,18)`
   - `CharacterSaveData` - Location: `Code/Entity/Save/CharacterSaveData.cs(8,18)`
   - `ItemTypeConverter` - Location: `Code/Entity/Save/ItemTypeConverter.cs(8,25)`

2. **CS0111**: Duplicate method definitions in `ItemTypeConverter`:
   - `ConvertItemToProperType`
   - `ConvertItemsToProperTypes`
   - `ConvertToWeaponItem`
   - `ConvertToHeadItem`
   - `ConvertToChestItem`
   - `ConvertToFeetItem`
   - `CopyBaseItemProperties`

3. ✅ **FIXED**: `DungeonDisplayBuffer` type not found - Fixed by using fully qualified name

## Root Cause

These appear to be classes that were split/moved during refactoring but the original definitions were not removed, causing duplicate class definitions in the same namespace.

## Fix Strategy

1. **Find all duplicate class definitions**
2. **Identify which is the "source of truth"** (usually the one in the dedicated file)
3. **Remove duplicates from original locations**
4. **Verify no references are broken**

## Implementation Steps

### Step 1: Find and Fix CharacterSaveData Duplicate
- Search for all `CharacterSaveData` class definitions
- Keep the one in `Code/Entity/Save/CharacterSaveData.cs`
- Remove from other locations

### Step 2: Find and Fix ItemTypeConverter Duplicate  
- Search for all `ItemTypeConverter` class definitions
- Keep the one in `Code/Entity/Save/ItemTypeConverter.cs`
- Remove from other locations

### Step 3: Find and Fix Game Class Duplicate
- Search for all `Game` class definitions
- Keep the one in `Code/Game/Game.cs`
- Remove from other locations (if any found)

### Step 4: Verify Build
- Run `dotnet clean`
- Run `dotnet build Code\Code.csproj`
- Verify all errors are resolved

## Status

- ✅ Plan created
- ✅ DungeonDisplayBuffer fixed
- ✅ Found duplicate class definitions:
  - `CharacterSaveData` - exists in `Code/Entity/Save/CharacterSaveData.cs` (correct location)
  - `ItemTypeConverter` - exists in `Code/Entity/Save/ItemTypeConverter.cs` (correct location)  
  - `Game` - exists in `Code/Game/Game.cs` (correct location)
- ⏳ Need to find where duplicates are defined (error line numbers don't match current file lengths)
- ⏳ May need to clean build cache and rebuild
- ⏳ Verifying build

## Next Steps

1. Clean build completely: `dotnet clean` and delete `obj` and `bin` folders
2. Search for any nested class definitions or partial classes
3. Check if there are generated files causing conflicts
4. Rebuild and verify errors are resolved
