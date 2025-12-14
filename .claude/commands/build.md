# Feature Builder Agent

Rapid feature implementation scaffolding and code generation from specifications.

## Commands

### Build Feature from Specification
```
/build feature [spec]
```
Generates complete implementation plan for a feature.

**Examples:**
- `/build feature "Add new weapon type"`
- `/build feature "Implement enemy AI behavior tree"`
- `/build feature "Add player attribute system"`

**Output includes:**
- Feature blueprint with all files to create
- Classes to generate
- Methods to implement
- Tests to create
- Configuration changes needed
- Documentation items
- Estimated implementation time
- Phase-by-phase roadmap

### Generate Class
```
/build class [name] [properties]
```
Generates a C# class with properties, methods, and tests.

**Examples:**
- `/build class Weapon "int damage, int speed, string name"`
- `/build class Enemy "int health, int strength, string type"`
- `/build class Item "string name, int rarity, double value"`

**Output includes:**
- Generated class code
- Auto-implemented properties
- Basic method templates
- Unit test template
- Ready to copy-paste

### Scaffold System
```
/build system [name]
```
Creates complete file structure for a new game system.

**Examples:**
- `/build system Inventory`
- `/build system Crafting`
- `/build system Quests`

**Output includes:**
- Recommended directory structure
- Core files to create (System, Manager, Factory)
- Model classes
- Handler/Utility classes
- Integration steps
- Effort estimate

### Generate API Endpoint
```
/build endpoint [path] [method]
```
Generates an API endpoint with request/response handling.

**Examples:**
- `/build endpoint /api/player Get`
- `/build endpoint /api/battle Post`
- `/build endpoint /api/items/{id} Delete`

**Output includes:**
- Endpoint method code
- Request/response models
- Error handling
- Unit test template
- Integration guidance

## Feature Specifications

When using `/build feature`, describe what you want to build:

### Weapon Systems
```
/build feature "Add new weapon type with scaling"
```
Generates:
- WeaponSystem.cs - Core weapon logic
- ItemManager.cs - Item operations
- ItemFactory.cs - Object creation
- Configuration for weapon types
- Comprehensive tests

### Enemy AI Systems
```
/build feature "Implement behavior tree AI"
```
Generates:
- EnemyBehavior.cs - Behavior base class
- BehaviorTree.cs - Decision logic
- BehaviorFactory.cs - Behavior creation
- Configuration for behavior weights
- AI tests and validation

### Attribute Systems
```
/build feature "Add character attributes (strength, agility, etc.)"
```
Generates:
- AttributeSystem.cs - Attribute management
- StatCalculator.cs - Stat computation
- BuffSystem.cs - Temporary modifiers
- Configuration for base stats
- Attribute calculation tests

## Generated Code Structure

### Basic Class Pattern
```csharp
public class {Name}
{
    public {Type} {Property} { get; set; }

    public {Name}()
    {
        // Initialize default values
    }

    public void Initialize() { }
    public void Update() { }
    public void Execute() { }
}
```

### System Pattern
```
System/
├── {Name}System.cs      (core functionality)
├── {Name}Manager.cs     (management layer)
├── {Name}Factory.cs     (object creation)
├── Models/
│   ├── {Name}State.cs
│   └── {Name}Config.cs
├── Handlers/
│   └── {Name}Handler.cs
└── Utils/
    └── {Name}Utilities.cs
```

### Test Pattern
```csharp
[Fact]
public void {Method}_WithValidInput_Succeeds()
{
    var {name} = new {Name}();
    {name}.Initialize();
    Assert.NotNull({name});
}
```

## Implementation Roadmap

All generated features follow this 5-phase implementation:

### Phase 1: File Structure (15 min)
- Create directory structure
- Create empty class files
- Set up namespaces

### Phase 2: Class Generation (30 min)
- Generate class templates
- Add properties
- Add basic method stubs

### Phase 3: Implementation (45-60 min)
- Implement actual logic
- Add error handling
- Integrate with systems

### Phase 4: Testing (20-30 min)
- Create unit tests
- Run test suite
- Fix any issues

### Phase 5: Documentation (15 min)
- Add XML comments
- Create usage examples
- Update README

**Total: 2-3 hours per feature typically**

## Property Types

Common property types to use:

```
int, double, string, bool          // Basic types
List<T>, Dictionary<K,V>            // Collections
Enemy, Player, Item                 // Custom types
Action<T>, Func<T,R>               // Delegates
```

## Integration After Generation

After generating code:

1. **Copy generated code** from output
2. **Create file** with appropriate path
3. **Add namespace** and using statements
4. **Implement logic** beyond stubs
5. **Add tests** for specific behavior
6. **Integrate** with existing systems
7. **Update config** if needed
8. **Run tests** to verify

## Example Workflow

### Step 1: Get Feature Plan
```
/build feature "Add item rarity system"
```
Review the blueprint and estimate.

### Step 2: Create Classes
```
/build class ItemRarity "string name, int bonusPercent"
/build class RarityModifier "double damageBonus, double hpBonus"
```

### Step 3: Scaffold System
```
/build system ItemRarities
```

### Step 4: Generate Endpoint (if needed)
```
/build endpoint /api/items/rarity Post
```

### Step 5: Implement
Follow the generated code and roadmap to implement.

## Tips and Tricks

1. **Start with class generation** - Gets you familiar with pattern
2. **Use descriptive names** - Makes code self-documenting
3. **Follow conventions** - Naming, folder structure, etc.
4. **Generate tests first** - Test-driven development
5. **Implement incrementally** - One method at a time
6. **Keep it simple** - Stub methods, expand later
7. **Test constantly** - Run tests after each phase

## Best Practices

1. **Review generated code** before using - May need adjustments
2. **Don't auto-generate everything** - Add your own logic
3. **Test thoroughly** - Generated tests are stubs
4. **Document extensively** - Generated code has basic comments
5. **Refactor as needed** - Generated code isn't final
6. **Keep related code together** - Use proper folder structure

## Common Patterns

### Repository Pattern
```
/build system DataRepository
```
Creates repository for data access.

### Factory Pattern
```
/build class EnemyFactory "string type, int level"
```
Creates factory for object creation.

### Strategy Pattern
```
/build system BehaviorStrategy
```
Creates strategy pattern implementation.

### Observer Pattern
```
/build system EventManager
```
Creates event notification system.

## Troubleshooting

**Q: Generated code doesn't compile**
A: Check namespaces, using statements, and type references

**Q: Too much boilerplate**
A: Generate just the class and manually create the rest

**Q: Not enough scaffolding**
A: Use `/build system` to get full structure

**Q: Tests don't pass**
A: Generated tests are stubs - implement actual logic

**Q: How do I integrate with existing code?**
A: Add to existing Manager class or create new one

## Time Estimates

| Task | Time |
|------|------|
| Generate class | 5-10 min |
| Implement class | 30-45 min |
| Create tests | 20-30 min |
| Scaffold system | 15-20 min |
| Implement system | 60-120 min |
| Full feature | 120-180 min |

Use these to estimate feature completion time.
