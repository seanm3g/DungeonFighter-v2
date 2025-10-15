# File Structure and Data Location

## Overview
This document defines the canonical locations for all project files. **There should be NO duplicate files in parent directories.**

## Project Root Structure
```
DungeonFighter-v2/
├── Code/                           # All C# source code
│   ├── Game/                      # Game logic
│   ├── UI/                        # UI systems
│   ├── World/                     # World/dungeon systems
│   └── Data/                      # Data loading utilities
├── GameData/                       # ⚠️ CANONICAL LOCATION FOR ALL GAME DATA
│   ├── UIConfiguration.json       # UI settings (timers, animations, etc.)
│   ├── ColorTemplates.json        # Color definitions and templates
│   ├── TitleAnimationConfig.json  # Title screen animation config
│   ├── Actions.json               # Combat actions
│   ├── Dungeons.json             # Dungeon definitions
│   └── [other game data files]
├── Documentation/                  # Project documentation
└── README.md                      # Project overview

⚠️ IMPORTANT: Parent directory (github projects/) should NOT contain:
   - GameData/ folder
   - Any .json configuration files
   - Any duplicate project files
```

## Configuration File Locations

### ✅ CORRECT Location (DungeonFighter-v2/GameData/)
All game data files MUST be in this location:
- `DungeonFighter-v2/GameData/UIConfiguration.json`
- `DungeonFighter-v2/GameData/ColorTemplates.json`
- `DungeonFighter-v2/GameData/Dungeons.json`
- etc.

### ❌ INCORRECT Locations
These locations should NOT exist:
- ~~`github projects/GameData/UIConfiguration.json`~~ (parent directory - DELETE)
- ~~`Code/GameData/`~~ (wrong location)
- ~~Any duplicate copies~~

## Path Resolution

The game uses `JsonLoader.FindGameDataFile()` which searches in this order:
1. `../GameData/` (relative to executable)
2. `../../GameData/` (for nested builds)
3. Current directory

**Expected behavior:**
- When running from `Code/bin/Debug/net8.0/`, it resolves to `DungeonFighter-v2/GameData/`
- Should NEVER resolve to parent directory's GameData

## Common Issues and Solutions

### Issue: Old Configuration Being Loaded
**Symptom:** Changes to `UIConfiguration.json` don't take effect

**Cause:** Duplicate file in parent directory being loaded instead

**Solution:**
1. Check for duplicate: `Test-Path "D:\Code Projects\github projects\GameData\"`
2. Delete if exists: `Remove-Item "D:\Code Projects\github projects\GameData\" -Recurse -Force`
3. Verify correct file is being loaded (check console debug output)

### Issue: Configuration Not Loading
**Symptom:** All values are zero or defaults

**Cause:** JSON deserialization failure or wrong file path

**Solution:**
1. Check console output for `[CONFIG] Loading UIConfiguration from: [path]`
2. Verify file exists at that path
3. Check JSON syntax is valid
4. Ensure no default values are masking the issue in C# classes

## Build Output Locations

### Debug Build
- Executable: `Code/bin/Debug/net8.0/DF.exe`
- Looks for GameData at: `../../../../GameData/` → `DungeonFighter-v2/GameData/`

### Release Build  
- Executable: `Code/bin/Release/net8.0/DF.exe`
- Looks for GameData at: `../../../../GameData/` → `DungeonFighter-v2/GameData/`

## Verification Checklist

Before committing or deploying, verify:
- [ ] All game data files are in `DungeonFighter-v2/GameData/`
- [ ] NO duplicate GameData folder in parent directory
- [ ] NO duplicate .json files anywhere
- [ ] Console debug output shows correct file path
- [ ] Configuration values are being loaded (not zeros)
- [ ] Changes to config files take effect immediately

## Developer Guidelines

### When Adding New Configuration Files:
1. Place ONLY in `DungeonFighter-v2/GameData/`
2. Update this documentation
3. Add console logging for loading verification
4. Remove any default values in C# to force JSON loading

### When Editing Configuration:
1. Edit ONLY the file in `DungeonFighter-v2/GameData/`
2. Close any running instances
3. Rebuild if C# config classes changed
4. Verify changes with console debug output

### When Troubleshooting:
1. Check console output for `[CONFIG]` messages
2. Verify file path in debug output
3. Check file modification timestamps
4. Delete any duplicate files found

## Related Documentation
- `ARCHITECTURE.md` - Overall system architecture
- `CODE_PATTERNS.md` - Coding standards and patterns
- `QUICK_REFERENCE.md` - Quick reference for common tasks

