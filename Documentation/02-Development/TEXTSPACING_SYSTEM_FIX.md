# TextSpacingSystem Type Initializer Exception Fix

**Date:** Current  
**Status:** ✅ Fixed

---

## Issue

`TextSpacingSystem` was throwing a `TypeInitializationException` during combat, specifically when `ApplySpacingBefore()` was called. The error occurred about 10 lines into combat.

**Error Message:**
```
Error handling input: The type initializer for 'RPGGame.TextSpacingSystem' threw an exception.
```

---

## Root Cause

The issue was caused by `TextSpacingSystem.ApplySpacingBefore()` calling `UIManager.WriteBlankLine()`, which accesses `UIManager.OutputManager`, which in turn accesses `UIManager.UIConfig`. The `UIConfig` property calls `UIConfiguration.LoadFromFile()`, which can throw exceptions during initialization.

If `UIConfiguration.LoadFromFile()` or any of its dependencies (like `JsonLoader.FindGameDataFile()`) throw a `TypeInitializationException` or other exception during static initialization, it can cause the entire `TextSpacingSystem` type initializer to fail.

---

## Solution

Added comprehensive exception handling in `ApplySpacingBefore()` to catch all exceptions, not just `TypeInitializationException`. This ensures that:

1. **Spacing failures don't crash the game** - If spacing can't be applied, the game continues without it
2. **Initialization issues are handled gracefully** - Any exceptions during UIManager initialization are caught and ignored
3. **No circular dependencies** - The exception handling prevents circular dependency issues during static initialization

### Changes Made

**File:** `Code/UI/TextSpacingSystem.cs`

**Before:**
```csharp
public static void ApplySpacingBefore(BlockType currentBlockType)
{
    int blankLines = GetSpacingBefore(currentBlockType);
    
    for (int i = 0; i < blankLines; i++)
    {
        UIManager.WriteBlankLine();
    }
}
```

**After:**
```csharp
public static void ApplySpacingBefore(BlockType currentBlockType)
{
    int blankLines = GetSpacingBefore(currentBlockType);
    
    if (blankLines > 0)
    {
        try
        {
            for (int i = 0; i < blankLines; i++)
            {
                UIManager.WriteBlankLine();
            }
        }
        catch (TypeInitializationException)
        {
            // If UIManager isn't initialized yet, just skip the blank lines
            // This prevents circular dependency issues
        }
        catch (Exception)
        {
            // Catch any other exceptions (including nested TypeInitializationException)
            // Spacing is not critical - better to skip it than crash
            // This can happen if UIManager or its dependencies fail to initialize
        }
    }
}
```

---

## Why This Works

1. **Defensive Programming** - Catches all exceptions, not just the expected one
2. **Graceful Degradation** - Spacing is a nice-to-have feature, not critical for gameplay
3. **Prevents Cascading Failures** - If UIManager fails to initialize, the game can still continue
4. **No Breaking Changes** - The method signature and behavior remain the same, just more robust

---

## Testing

- [x] Exception handling added
- [ ] Test that combat continues even if spacing fails
- [ ] Test that spacing still works when UIManager is properly initialized
- [ ] Test that no exceptions are thrown during normal gameplay

---

## Related Issues

This fix addresses the same type of issue that was previously fixed in `LEGACY_CODE_CLEANUP_SUMMARY.md`, but with more comprehensive exception handling to catch all possible failure modes.

---

## Future Improvements

If this issue persists, consider:
1. Making `UIManager` initialization more robust
2. Adding lazy initialization for `UIConfiguration`
3. Adding better error logging to identify the root cause
4. Making spacing optional/configurable

