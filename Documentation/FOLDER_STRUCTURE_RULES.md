# DungeonFighter-v2 Folder Structure Rules

## 🚨 CRITICAL: Folder Structure Requirements

### ✅ CORRECT Structure:
```
DungeonFighter-v2/
├── Code/
│   ├── BalanceAnalysis/          ← Balance analysis output files
│   ├── DebugAnalysis/            ← Debug analysis output files  
│   ├── Balance/                  ← Balance tuning code
│   ├── [other .cs files]
│   └── Code.csproj
├── GameData/
├── Documentation/
└── [other project files]
```

### ❌ INCORRECT Structure (NEVER CREATE):
```
DungeonFighter-v2/
├── Code/
│   ├── Code/                     ← WRONG! This creates redundancy
│   │   ├── BalanceAnalysis/      ← WRONG! Duplicate folder
│   │   ├── DebugAnalysis/        ← WRONG! Duplicate folder
│   │   └── Balance/              ← WRONG! Duplicate folder
│   ├── BalanceAnalysis/          ← Correct location
│   ├── DebugAnalysis/            ← Correct location
│   └── Balance/                  ← Correct location
```

## 🔧 Why This Matters:

1. **Redundancy**: Creates duplicate folders with the same purpose
2. **Confusion**: Makes it unclear which folder is the "real" one
3. **File Management**: Files get scattered between correct and incorrect locations
4. **Debug Issues**: Analysis tools may look in the wrong folder

## 📋 Rules to Follow:

1. **NEVER** create a `Code/Code/` nested structure
2. **ALWAYS** place analysis files directly in `Code/BalanceAnalysis/` and `Code/DebugAnalysis/`
3. **ALWAYS** place balance code directly in `Code/Balance/`
4. **IF** you see a `Code/Code/` folder, **IMMEDIATELY** remove it
5. **VERIFY** folder structure after any file operations

## 🛠️ How to Fix If This Happens:

```powershell
# Remove the incorrect nested folder
Remove-Item "Code\Code" -Recurse -Force

# Verify correct structure exists
Get-ChildItem "Code" -Directory
```

## 📝 History:

This issue has occurred multiple times:
- **Date**: 2025-10-01
- **Issue**: `Code/Code/BalanceAnalysis/` and `Code/Code/DebugAnalysis/` created
- **Resolution**: Removed nested folders, kept correct `Code/BalanceAnalysis/` and `Code/DebugAnalysis/`
- **Prevention**: This documentation file created to prevent recurrence

## ⚠️ Warning:

**DO NOT** create nested `Code/Code/` folders. This is a recurring issue that must be prevented through careful attention to folder structure.
