# Problem Solutions - DungeonFighter

This document contains solutions to common problems encountered during development. Use this as a quick reference when similar issues arise.

## Recent Fixes

### Issue: Combat Freeze During Battle (November 20, 2025)
**Symptoms:**
- Game shows "not responding" during combat
- Enemy encounter screen freezes at start of combat
- No error message, just hangs

**Root Causes (2 Issues):**

**Issue #1 - Non-existent Method Calls:**
- CombatManager.cs was calling `WaitForMessageQueueCompletionAsync()` method that doesn't exist
- Called 4 times during combat loop causing runtime errors

**Issue #2 - Async/Await Deadlock (PRIMARY CAUSE):**
- DungeonRunnerManager called synchronous `RunCombat()` wrapper from UI thread
- `RunCombat()` used `Task.Run()` to move to background thread
- `RunCombatAsync()` on background thread tried to access/post to UI
- UI thread blocked waiting for `Task.Run()` to complete
- Background thread blocked waiting for UI operations
- **CIRCULAR DEADLOCK = FREEZE**

**Solutions:**

1. **CombatManager.cs (4 locations):**
   - Replaced all `await canvasUI.WaitForMessageQueueCompletionAsync()` with `await Task.Delay(50)`
   - Lines affected: Player turn (~200), Enemy turn (~220), Environment turn (~240), Battle end (~257)

2. **DungeonRunnerManager.cs (Line 215):**
   - Changed from: `combatManager.RunCombat(...)`
   - Changed to: `await combatManager.RunCombatAsync(...)`
   - Eliminates the `Task.Run()` wrapper that caused the deadlock

**Why This Works:**
- Calling `RunCombatAsync()` directly allows proper async flow without deadlock
- No circular wait between UI thread and background thread
- UI synchronization works naturally through await chains

**Testing:**
- Combat now flows without freezing
- All actions display properly
- Game responds to input during combat
- Multiple consecutive combats work correctly

## Combat Balance Issues

### Problem: Enemies dying in 1 hit instead of target 10 actions
**Symptoms:**
- Hero dealing 32+ damage to enemies with 26 health
- Combat ending too quickly
- Enemy STR showing 30+ instead of expected 7-8

**Root Causes & Solutions:**
1. **Double Scaling**: Enemy stats scaled twice (EnemyLoader.cs + Enemy.cs constructor)
   - **Fix**: Remove scaling in Enemy.cs constructor
2. **Weapon Damage Too High**: Weapon scaling formulas adding massive multipliers (1.55x)
   - **Fix**: Reduce weapon scaling to 0.42x
3. **Enemy DPS System Mismatch**: Using old level scaling formula
   - **Fix**: Update EnemyDPSSystem to match current system

**Verification:**
- Check enemy stats in combat display
- Verify weapon damage scaling in TuningConfig.json
- Run balance analysis in Tuning Console

### Problem: Combat feels too fast/slow
**Solutions:**
- Adjust `CombatSpeed` in settings (0.5 = slow, 2.0 = fast)
- Modify `EnableTextDisplayDelays` for pacing
- Tune `NarrativeBalance` (0.0 = action-by-action, 1.0 = full narrative)

## Null Reference Issues

### Problem: NullReferenceException in Dungeon Selection Renderer
**Symptoms:**
- Application crashes with `System.NullReferenceException` in `DungeonSelectionRenderer.RenderDungeonSelection()`
- Stack trace shows error at line 69
- Occurs when attempting to render dungeon selection screen

**Root Causes:**
1. Missing null validation on `dungeons` parameter passed to renderer
2. No check for null dungeon objects within the list
3. `SequenceEqual()` method called on potentially null collections

**Solution:**
1. **Add Parameter Validation**: Validate dungeons list is not null at method entry
2. **Add Element Validation**: Check each dungeon object before accessing properties
3. **Throw Appropriate Exceptions**: Use ArgumentNullException for null parameter, InvalidOperationException for null elements

```csharp
// Validate input - dungeons list must not be null
if (dungeons == null)
{
    throw new ArgumentNullException(nameof(dungeons), "Dungeon list cannot be null");
}

// Inside loop - validate dungeon object is not null
if (dungeon == null)
{
    throw new InvalidOperationException($"Dungeon at index {i} is null");
}
```

**Files Modified:**
- `Code/UI/Avalonia/Renderers/DungeonSelectionRenderer.cs` (lines 68-98)

**Prevention:**
- Always validate parameters at method entry, especially collections
- Add element validation in iteration loops
- Use defensive programming for UI rendering operations
- Test with null and invalid inputs

## Data Generation Issues

### Problem: Armor generation creating massive numbers (30,130,992)
**Symptoms:**
- Armor values in Armor.json showing extremely large numbers
- Tier 2 armor showing 30+ million instead of reasonable values like 4-8
- Data generation amplifying existing corrupted values

**Root Cause:**
- `GenerateArmorFromConfig` method multiplying existing armor values by tier multipliers
- System using corrupted existing values as base instead of generating clean base values

**Solution:**
1. **Replace Amplification Logic**: Use base value generation instead of multiplying existing values
2. **Create Base Value Lookup**: Implement `GetBaseArmorForTierAndSlot` with predefined values
3. **Define Proper Progression**: 
   - Tier 1: Head=2, Chest=4, Feet=2
   - Tier 2: Head=4, Chest=8, Feet=4
   - Tier 3: Head=6, Chest=12, Feet=6
   - etc.
4. **Add Fallback**: Simple tier-based calculation for unknown combinations

**Prevention:**
- Always use base value generation instead of amplifying existing values
- Test data generation with clean/corrupted input files
- Validate generated values are within reasonable ranges

**Files to Check:**
- `Code/GameDataGenerator.cs` (GenerateArmorFromConfig method)
- `GameData/Armor.json` (generated values)

## Data Loading Issues

### Problem: JSON files not loading properly
**Symptoms:**
- Actions not appearing in game
- Default values being used instead of JSON data
- File not found errors

**Solutions:**
1. **Check File Paths**: Verify GameData/ folder structure
2. **Validate JSON Syntax**: Use JSON validator for syntax errors
3. **Check JsonLoader.cs**: Ensure proper error handling
4. **Verify File Permissions**: Ensure read access to GameData files

**Common JSON Issues:**
- Missing commas between objects
- Trailing commas in arrays
- Unescaped quotes in strings
- Invalid number formats

### Problem: Actions not appearing in Action Pool
**Root Causes:**
1. **Equipment Not Providing Actions**: Check weapon/armor action chances
2. **Action Loading Failure**: Verify Actions.json structure
3. **Action Pool Not Updated**: Check InventoryManager action pool updates

**Solutions:**
- Verify weapon has `"actions": ["ACTION_NAME"]` in JSON
- Check armor action chance percentages
- Ensure ActionLoader.cs is loading actions correctly

### Problem: Some enemies can't deal damage (only utility/debuff actions)
**Symptoms:**
- Encounters where enemies never reduce player health
- Examples: `Prism Spider` had only `LIGHT REFRACTION` and `WEB TRAP`

**Solution:**
1. Added a data fix ensuring all enemies include at least one damaging action in `GameData/Enemies.json` (e.g., added `POISON BITE` to `Prism Spider`).
2. Added a loader-time safeguard in `EnemyLoader.CreateEnemyFromData` that injects `BASIC ATTACK` if no damaging actions are present or resolvable.
3. Added `EnemyTests.TestEnemiesHaveDamagingAction()` and a CLI hook `--test-enemies` to validate configurations.

**Verification:**
- Run `Code.exe --test-enemies` to see validation results.
- In combat, previously non-damaging enemies now attack and can win fights.

## Character Progression Issues

### Problem: Character stats not scaling properly
**Symptoms:**
- Stats not increasing on level up
- Health not restoring on level up
- Class points not being awarded

**Solutions:**
1. **Check CharacterProgression.cs**: Verify level up logic
2. **Validate Stat Formulas**: Check TuningConfig.json scaling
3. **Verify XP System**: Ensure XP is being awarded and calculated correctly

### Problem: Equipment not providing expected bonuses
**Solutions:**
- Check item tier and rarity in inventory display
- Verify stat bonus calculations in CharacterEquipment.cs
- Ensure proper equipment scaling in ScalingManager.cs

## UI/Display Issues

### Problem: Damage display showing wrong values
**Symptoms:**
- Showing raw damage instead of actual damage
- Armor calculations not visible
- Inconsistent damage formatting

**Solutions:**
- Use `FormatDamageDisplay()` method in Combat.cs
- Ensure `CalculateDamage()` is called for actual damage
- Check armor reduction calculations

### Problem: Inventory display formatting issues
**Solutions:**
- Check InventoryDisplayManager.cs formatting methods
- Verify item stat calculations
- Ensure proper indentation and spacing

## Testing Issues

### Problem: Tests failing unexpectedly
**Solutions:**
1. **Check Test Data**: Ensure test data matches current game balance
2. **Update Expected Values**: Balance changes may require test updates
3. **Verify Test Environment**: Ensure tests run in clean state

### Problem: Balance tests showing incorrect DPS
**Solutions:**
- Update DPS calculations in EnemyBalanceCalculator.cs
- Verify scaling formulas in TuningConfig.json
- Check enemy stat scaling in EnemyLoader.cs

## Performance Issues

### Problem: Game running slowly
**Solutions:**
1. **Disable Delays**: Set `EnableTextDisplayDelays = false`
2. **Reduce Narrative**: Set `NarrativeBalance = 1.0` for full narrative mode
3. **Optimize Calculations**: Check for expensive operations in combat loops

### Problem: Memory usage issues
**Solutions:**
- Check for object creation in tight loops
- Verify proper disposal of resources
- Monitor JSON loading and caching

## Configuration Issues

### Problem: Tuning changes not taking effect
**Solutions:**
1. **Reload Configuration**: Use "Reload Config" in Tuning Console
2. **Check JSON Syntax**: Validate TuningConfig.json format
3. **Restart Application**: Some changes require full restart

### Problem: Formula evaluation errors
**Solutions:**
- Check variable names match exactly (case-sensitive)
- Ensure parentheses are balanced
- Verify mathematical operators are supported
- Use FormulaEvaluator test function

## Common Error Patterns

### "Something went wrong" in combat
**Cause**: Action execution failure
**Solution**: Check action definitions in Actions.json, verify action properties

### "File not found" errors
**Cause**: Missing or moved GameData files
**Solution**: Verify file paths in GameConstants.cs, check file existence

### Null reference exceptions
**Cause**: Uninitialized objects or missing data
**Solution**: Add null checks, verify object initialization order

## Quick Fixes

### Reset to Known Good State
1. Restore TuningConfig.json from backup
2. Reload configuration in Tuning Console
3. Run balance analysis to verify

### Clear Cache Issues
1. Delete character_save.json to reset character
2. Restart application
3. Create new character to test

### Verify System Integrity
1. Run all tests in Settings → Tests
2. Check balance analysis in Tuning Console
3. Verify JSON file syntax

## Prevention Strategies

1. **Always Test Changes**: Run relevant tests after modifications
2. **Backup Configurations**: Export tuning configs before major changes
3. **Incremental Changes**: Make small changes and test frequently
4. **Document Changes**: Note what was changed and why
5. **Use Version Control**: Commit working states before major changes

## Related Documentation

- **`DEBUGGING_GUIDE.md`**: Systematic debugging approaches and tools
- **`QUICK_REFERENCE.md`**: Fast lookup for key information and commands
- **`KNOWN_ISSUES.md`**: Current status of known problems
- **`TESTING_STRATEGY.md`**: Testing approaches for verification
- **`DEVELOPMENT_WORKFLOW.md`**: Step-by-step development process

---

*This document should be updated when new problems are encountered and solved.*
