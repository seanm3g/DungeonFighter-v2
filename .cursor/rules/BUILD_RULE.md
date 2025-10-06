# Build Rule for DungeonFighter-v2

## Rule: Always Run `dotnet build` Before Testing

### Purpose
Before running the game with `dotnet run`, always run `dotnet build` to check for compilation errors.

### Command
```bash
cd "D:\Code Projects\github projects\DungeonFighter-v2\Code"
dotnet build
```

### Expected Output
```
Build succeeded.
0 Warning(s)
0 Error(s)
```

### Why This Rule Exists
1. **Catch Compilation Errors Early**: Find syntax errors, missing references, and type mismatches before running
2. **Verify Refactoring**: Ensure code changes compile correctly after refactoring
3. **Prevent Runtime Issues**: Avoid crashes from compilation errors that would only show at runtime
4. **Maintain Code Quality**: Keep the codebase in a compilable state

### When to Use
- After making code changes
- Before running `dotnet run`
- After refactoring or restructuring code
- When adding new files or dependencies
- Before committing changes

### What to Do If Build Fails
1. Read the error messages carefully
2. Fix the compilation errors
3. Run `dotnet build` again
4. Repeat until build succeeds
5. Only then run `dotnet run`

### Example Workflow
```bash
# 1. Make code changes
# 2. Check compilation
cd "D:\Code Projects\github projects\DungeonFighter-v2\Code"
dotnet build

# 3. If build succeeds, then run the game
dotnet run
```

### Last Successful Build
✅ **Build succeeded** - All refactored code compiles correctly
- GameMenuManager refactored
- GameLoopManager created
- MenuConfiguration centralized
- All "0. Exit" options added
- No compilation errors
