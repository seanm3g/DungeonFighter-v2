# Development Workflow - DungeonFighter-v2

Step-by-step development process and best practices for working with the DungeonFighter-v2 codebase.

## Development Process Overview

### 1. Pre-Development Phase
**Before starting any work:**
1. **Read Architecture Documentation**: Review `ARCHITECTURE.md` for relevant systems
2. **Check Task List**: Review `TASKLIST.md` for current priorities
3. **Understand Requirements**: Clarify what needs to be implemented
4. **Plan Implementation**: Break down work into manageable tasks

### 2. Development Phase
**During development:**
1. **Make Incremental Changes**: Small, testable modifications
2. **Test Frequently**: Run relevant tests after each change
3. **Document Changes**: Update documentation as needed
4. **Maintain Code Quality**: Follow established patterns and conventions

### 3. Post-Development Phase
**After completing work:**
1. **Run Full Test Suite**: Verify all systems still work
2. **Update Documentation**: Reflect changes in relevant docs
3. **Commit Changes**: Save working state with descriptive messages
4. **Verify Integration**: Ensure changes work with existing systems

## Step-by-Step Development Workflow

### Step 1: Environment Setup
```bash
# Navigate to project directory
cd "D:\Code Projects\github projects\DungeonFighter-v2"

# Verify project structure
ls -la

# Check for any uncommitted changes
git status
```

### Step 2: Understanding the Codebase
1. **Read Architecture Documentation**
   - Review `Documentation/ARCHITECTURE.md`
   - Identify relevant systems and classes
   - Understand data flow and dependencies

2. **Examine Related Code**
   - Look at existing implementations
   - Check for similar patterns
   - Understand current conventions

3. **Check Configuration Files**
   - Review relevant JSON files in `GameData/`
   - Understand current parameters
   - Check for existing configurations

### Step 3: Planning Implementation
1. **Break Down Requirements**
   - Identify specific tasks needed
   - Determine dependencies
   - Plan implementation order

2. **Design Solution**
   - Follow existing patterns
   - Consider impact on other systems
   - Plan for testing and validation

3. **Create Implementation Plan**
   - List specific files to modify
   - Identify new files to create
   - Plan testing approach

### Step 4: Implementation
1. **Start with Core Logic**
   - Implement main functionality first
   - Keep it simple and focused
   - Avoid premature optimization

2. **Add Error Handling**
   - Include proper error handling
   - Use established error handling patterns
   - Add logging for debugging

3. **Follow Code Patterns**
   - Use established naming conventions
   - Follow architectural patterns
   - Maintain consistency with existing code

### Step 5: Testing
1. **Unit Testing**
   - Test individual components
   - Verify expected behavior
   - Check edge cases

2. **Integration Testing**
   - Test system interactions
   - Verify data flow
   - Check for side effects

3. **Balance Testing**
   - Run balance analysis
   - Verify mathematical correctness
   - Check performance impact

### Step 6: Documentation
1. **Update Code Comments**
   - Add inline documentation
   - Explain complex logic
   - Document public interfaces

2. **Update Architecture Documentation**
   - Reflect changes in `ARCHITECTURE.md`
   - Update class descriptions
   - Document new patterns

3. **Update Task List**
   - Mark completed tasks
   - Add new tasks if needed
   - Update progress status

### Step 7: Validation
1. **Run Full Test Suite**
   - Execute all tests
   - Verify no regressions
   - Check for new issues

2. **Manual Testing**
   - Test in-game functionality
   - Verify user experience
   - Check for edge cases

3. **Performance Check**
   - Monitor execution time
   - Check memory usage
   - Verify scalability

## Development Best Practices

### Code Quality
1. **Follow Naming Conventions**
   - Use PascalCase for classes and methods
   - Use camelCase for variables
   - Use descriptive names

2. **Maintain Consistency**
   - Follow existing patterns
   - Use established error handling
   - Keep code style consistent

3. **Write Clean Code**
   - Keep methods focused
   - Avoid deep nesting
   - Use meaningful variable names

### Testing Strategy
1. **Test Early and Often**
   - Run tests after each change
   - Fix issues immediately
   - Don't accumulate technical debt

2. **Use Appropriate Test Types**
   - Unit tests for individual components
   - Integration tests for system interactions
   - Balance tests for mathematical correctness

3. **Maintain Test Quality**
   - Keep tests simple and focused
   - Use descriptive test names
   - Avoid test interdependencies

### Documentation
1. **Keep Documentation Current**
   - Update docs with code changes
   - Document new patterns and conventions
   - Maintain accuracy and completeness

2. **Use Clear Documentation**
   - Write for the intended audience
   - Include examples where helpful
   - Keep documentation organized

### Version Control
1. **Commit Frequently**
   - Make small, focused commits
   - Use descriptive commit messages
   - Don't commit broken code

2. **Use Meaningful Messages**
   - Describe what was changed
   - Explain why changes were made
   - Reference related issues or tasks

## Common Development Scenarios

### Scenario 1: Adding New Action
**Steps:**
1. **Add to Actions.json**: Define action properties
2. **Update ActionLoader.cs**: Ensure proper loading
3. **Test Action Loading**: Verify JSON parsing
4. **Test Action Execution**: Verify combat integration
5. **Update Documentation**: Document new action

**Example:**
```json
// In Actions.json
{
  "Name": "NEW_ACTION",
  "DamageMultiplier": 2.0,
  "Length": 1.5,
  "Description": "A new powerful action"
}
```

### Scenario 2: Modifying Combat Balance
**Steps:**
1. **Update TuningConfig.json**: Modify balance parameters
2. **Test Balance Changes**: Run balance analysis
3. **Verify Combat Flow**: Test in-game combat
4. **Check Performance**: Ensure no performance impact
5. **Document Changes**: Update balance documentation

**Example:**
```json
// In TuningConfig.json
{
  "CriticalHitMultiplier": 2.5,  // Changed from 2.0
  "MinimumDamage": 2             // Changed from 1
}
```

### Scenario 3: Adding New Enemy Type
**Steps:**
1. **Add to Enemies.json**: Define enemy properties
2. **Update EnemyLoader.cs**: Ensure proper loading
3. **Test Enemy Creation**: Verify factory creation
4. **Test Enemy Scaling**: Verify level scaling
5. **Test Combat Integration**: Verify combat behavior

**Example:**
```json
// In Enemies.json
{
  "Name": "NewEnemy",
  "BaseHealth": 80,
  "Strength": 6,
  "Agility": 4,
  "Technique": 3,
  "Intelligence": 2,
  "PrimaryAttribute": "Strength"
}
```

### Scenario 4: Fixing Bug
**Steps:**
1. **Reproduce Issue**: Identify exact conditions
2. **Debug Problem**: Use debugging tools
3. **Identify Root Cause**: Trace through code
4. **Implement Fix**: Make minimal changes
5. **Test Fix**: Verify issue is resolved
6. **Test Regression**: Ensure no new issues

**Example:**
```csharp
// Before fix
public int CalculateDamage(Entity source, Entity target)
{
    return source.Strength; // Missing weapon damage
}

// After fix
public int CalculateDamage(Entity source, Entity target)
{
    return source.Strength + source.GetWeaponDamage(); // Include weapon damage
}
```

## Development Tools and Commands

### Essential Commands
```bash
# Build project
dotnet build

# Run project
dotnet run

# Run tests
dotnet test

# Clean build
dotnet clean
dotnet build
```

### Process Management Commands
```bash
# Kill all DungeonFighter processes (use @kill.mdc in chat)
Get-Process | Where-Object {$_.ProcessName -like "*dotnet*" -or $_.ProcessName -like "*Code*" -or $_.ProcessName -like "*DF4*"} | Stop-Process -Force

# Find running processes
Get-Process | Where-Object {$_.ProcessName -like "*dotnet*"} | Select-Object ProcessName, Id, CPU, WorkingSet

# Kill specific process by ID
Stop-Process -Id [ProcessID] -Force
```

### Debugging Commands
```csharp
// Enable debug logging
DebugLogger.EnableDebugMode();

// Log debug information
DebugLogger.Log("Debug message");

// Check game state
character.DisplayStats();
enemy.DisplayStats();
```

### Testing Commands
```csharp
// Run specific test categories
Program.RunCharacterTests();
Program.RunCombatTests();
Program.RunBalanceTests();

// Run all tests
Program.RunAllTests();

// Run balance analysis
EnemyBalanceCalculator.AnalyzeBalance();
```

## Quality Assurance Checklist

### Before Committing
- [ ] Code compiles without errors
- [ ] All relevant tests pass
- [ ] No new warnings introduced
- [ ] Code follows established patterns
- [ ] Documentation is updated
- [ ] Performance impact is acceptable

### Before Release
- [ ] Full test suite passes
- [ ] Balance analysis is satisfactory
- [ ] Manual testing completed
- [ ] Documentation is complete
- [ ] No known critical issues
- [ ] Performance is acceptable

## Troubleshooting Common Issues

### Build Issues
1. **Check .NET Version**: Ensure .NET 8.0 is installed
2. **Clean Build**: Run `dotnet clean` then `dotnet build`
3. **Check Dependencies**: Verify all NuGet packages are installed
4. **Check File Paths**: Ensure all files are in correct locations

### Test Failures
1. **Check Test Data**: Verify test data matches current game state
2. **Update Expected Values**: Balance changes may require test updates
3. **Check Dependencies**: Ensure all required systems are initialized
4. **Run Individual Tests**: Isolate failing tests

### Runtime Issues
1. **Check JSON Files**: Validate JSON syntax and structure
2. **Check File Paths**: Ensure GameData files are accessible
3. **Enable Debug Logging**: Use debug tools to trace issues
4. **Check Error Logs**: Review error messages for clues

### Process Management Issues
1. **Game Locked Error**: Use `@kill.mdc` in chat to terminate all DungeonFighter processes
2. **Multiple Instances**: Check for running dotnet processes and terminate them
3. **Stuck Processes**: Use Ctrl+C or close terminal windows to force terminate
4. **Background Processes**: Use Task Manager or PowerShell to find and kill processes

## Continuous Improvement

### Regular Reviews
1. **Code Review**: Review code for quality and consistency
2. **Architecture Review**: Ensure changes fit overall architecture
3. **Performance Review**: Monitor performance impact
4. **Documentation Review**: Keep documentation current

### Learning and Growth
1. **Study Existing Code**: Learn from established patterns
2. **Experiment Safely**: Try new approaches in isolated areas
3. **Share Knowledge**: Document new patterns and techniques
4. **Seek Feedback**: Get input from other developers

## Related Documentation

- **`ARCHITECTURE.md`**: System architecture for development planning
- **`CODE_PATTERNS.md`**: Code patterns and conventions to follow
- **`TESTING_STRATEGY.md`**: Testing approaches for verification
- **`PROBLEM_SOLUTIONS.md`**: Solutions to common development problems
- **`PERFORMANCE_NOTES.md`**: Performance considerations during development

---

*This workflow should be updated as development practices evolve.*
