# Build Cache Fix Commands

When you encounter build errors where the compiler can't find methods that clearly exist in your code, use these commands to fix build cache issues.

## Quick Commands

### Quick Clean (Fast)
```batch
.\Scripts\quick-clean.bat
```
or
```powershell
.\Scripts\quick-clean.ps1
```
**Use when:** Build errors appear but code looks correct. Fastest option.

### Fix Build (Thorough)
```batch
.\Scripts\fix-build.bat
```
or
```powershell
.\Scripts\fix-build-cache.ps1
```
**Use when:** Quick clean didn't work. Clears NuGet cache too.

### Clean All (Most Thorough)
```batch
.\Scripts\clean-all.bat
```
**Use when:** Nothing else works. Most aggressive cleanup.

## What They Do

1. **Remove** `bin` and `obj` folders
2. **Run** `dotnet clean`
3. **Clear** NuGet cache (thorough versions)
4. **Rebuild** the project
5. **Show** build results

## When to Use

- Compiler says "method doesn't exist" but you can see it in the code
- Build errors that randomly fix themselves later
- After switching Git branches
- After major refactoring
- When IDE and command line builds disagree

## Tips

- Try **quick-clean** first (fastest)
- If that doesn't work, try **fix-build** (more thorough)
- If still failing, try **clean-all** and restart your IDE
- These scripts are safe - they only remove build artifacts, not source code

