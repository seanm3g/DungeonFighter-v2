# Game Class Duplicate Error - Resolution Guide

## Problem
Compiler error: `CS0101: The namespace 'RPGGame' already contains a definition for 'Game'`

## Root Cause Analysis
After extensive investigation, only ONE `Game` class exists in `Code/Game/Game.cs` with `namespace RPGGame`. However, the compiler persists in reporting a duplicate.

## Possible Causes (to investigate manually)

1. **Stale Build Artifacts**: Already cleaned with `dotnet clean` and manual deletion of `obj/` and `bin/` folders
2. **Hidden/Duplicate File**: A file named `Game.cs` might exist in a different location
3. **Project File Issue**: The `.csproj` might be including the file twice (unlikely with SDK-style projects)
4. **Case Sensitivity**: Windows file system case-insensitivity might be causing path resolution issues
5. **Generated Files**: Avalonia or other tools might be generating a `Game` class

## Immediate Solution

Since the source code appears correct, try these steps in order:

1. **Complete Clean Rebuild**:
   ```powershell
   cd Code
   Remove-Item -Recurse -Force obj, bin -ErrorAction SilentlyContinue
   dotnet clean
   dotnet build
   ```

2. **Check for Hidden Files**:
   ```powershell
   Get-ChildItem -Path Code -Recurse -Force -Filter "*Game*.cs" | Select-Object FullName
   ```

3. **Verify File Uniqueness**:
   ```powershell
   Get-ChildItem -Path Code -Recurse -Filter "Game.cs" | Select-Object FullName, Length, LastWriteTime
   ```

4. **Check Project References**: Ensure no other projects reference this one and create a `Game` class

5. **Temporary Workaround**: If the issue persists, consider:
   - Renaming the class temporarily to verify the duplicate disappears
   - Checking if any Avalonia-generated files create a `Game` namespace

## Files Checked
- ✅ `Code/Game/Game.cs` - Only one `class Game` found
- ✅ No `partial class Game` found
- ✅ No nested `Game` classes found
- ✅ No `namespace Game` found (only `DungeonFighter.Game.*` which is different)

## Next Steps
If the error persists after a complete clean rebuild, the issue is likely:
- A hidden/backup file not visible in normal searches
- A build system caching issue
- A case-sensitivity problem with file paths

Consider using a different build tool or IDE to get more detailed error information about which files are being compiled.

