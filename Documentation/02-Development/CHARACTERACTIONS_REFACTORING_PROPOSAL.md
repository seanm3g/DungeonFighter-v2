# CharacterActions.cs Refactoring Proposal

## 📊 Current State Analysis

**File**: `Code/Entity/CharacterActions.cs`  
**Current Size**: 828 lines  
**Current Responsibilities**: 11 major areas

### Current Responsibilities
1. **Class Action Management** (105-177) - Barbarian, Warrior, Rogue, Wizard actions
2. **Gear Action Management** (197-615) - Weapon and armor actions
3. **Action Loading** (380-414) - Loading actions from JSON
4. **Action Factory/Creation** (416-461) - Creating Action objects from data
5. **Action Enhancement** (463-535) - Enhancing descriptions with modifiers
6. **Action Parsing** (537-560) - Parsing action type and target type
7. **Roll Bonus Management** (562-637) - Applying/removing roll bonuses
8. **Armor Action Selection** (298-378) - Random armor action selection
9. **Combo Management** (670-825) - Managing combo sequences
10. **Default Actions** (26-86) - Ensuring BASIC ATTACK availability
11. **Environment Actions** (639-668) - Adding/removing environment actions

---

## 🎯 Refactoring Goals

| Goal | Benefit |
|------|---------|
| **Single Responsibility** | Each class has one reason to change |
| **Improved Testability** | Smaller classes easier to unit test |
| **Better Maintainability** | Easier to locate and modify specific logic |
| **Reduced Complexity** | Cognitive load reduced from 828 to ~200 per file |
| **Reusability** | Managers can be composed differently |
| **Consistency** | Follows established patterns in codebase |

---

## 📐 Proposed Architecture

### NEW FILE STRUCTURE

```
Code/Entity/
├── CharacterActions.cs (REFACTORED - ~250 lines)
│   └── Orchestrator for action-related operations
├── ClassActionManager.cs (NEW - ~150 lines)
│   └── Manages class-specific actions (Barbarian, Warrior, Rogue, Wizard)
├── GearActionManager.cs (NEW - ~180 lines)
│   └── Manages weapon and armor actions
├── ComboSequenceManager.cs (NEW - ~120 lines)
│   └── Manages combo sequence and ordering
├── ActionEnhancer.cs (NEW - ~80 lines)
│   └── Enhances action descriptions with modifiers
├── ActionFactory.cs (REFACTORED - ~80 lines)
│   └── Creates Action objects from data
└── EnvironmentActionManager.cs (NEW - ~60 lines)
    └── Manages environment-specific actions
```

### Total Refactored Size
- **Original**: 828 lines in 1 file
- **Refactored**: ~920 lines across 7 focused files
- **Gain**: Better organization, single responsibility, easier testing

---

## 🔧 Detailed Manager Descriptions

### 1. ClassActionManager (NEW)
**Responsibility**: Manage class-specific actions (Barbarian, Warrior, Rogue, Wizard)

**Methods**:
```csharp
public class ClassActionManager
{
    public void AddClassActions(Actor actor, CharacterProgression progression, WeaponType? weaponType)
    public void RemoveClassActions(Actor actor)
    
    // Private helpers
    private void AddBarbarianActions(Actor actor, CharacterProgression progression)
    private void AddWarriorActions(Actor actor, CharacterProgression progression)
    private void AddRogueActions(Actor actor, CharacterProgression progression)
    private void AddWizardActions(Actor actor, CharacterProgression progression, WeaponType? weaponType)
}
```

**Source Lines**: 105-177 from original (73 lines)

---

### 2. GearActionManager (NEW)
**Responsibility**: Manage weapon and armor actions, including loading and applying bonuses

**Methods**:
```csharp
public class GearActionManager
{
    public void AddWeaponActions(Actor actor, WeaponItem weapon)
    public void AddArmorActions(Actor actor, Item armor)
    public void RemoveWeaponActions(Actor actor, WeaponItem? weapon = null)
    public void RemoveArmorActions(Actor actor, Item? armor = null)
    public void ApplyRollBonusesFromGear(Actor actor, Item gear)
    public void RemoveRollBonusesFromGear(Actor actor, Item gear)
    
    // Private helpers
    private void AddGearActions(Actor actor, Item gear)
    private List<string> GetGearActions(Item gear)
    private List<string> GetWeaponActionsFromJson(WeaponType weaponType)
    private List<string> GetRandomArmorActionFromJson(Item armor)
    private string? GetRandomArmorActionName()
    private bool HasSpecialArmorActions(Item armor)
    private void LoadGearActionFromJson(Actor actor, string actionName)
}
```

**Source Lines**: 197-615 from original (419 lines) - LARGEST SECTION

---

### 3. ComboSequenceManager (NEW)
**Responsibility**: Manage combo sequences, ordering, and validation

**Methods**:
```csharp
public class ComboSequenceManager
{
    public List<Action> ComboSequence { get; private set; }
    
    public List<Action> GetComboActions()
    public void AddToCombo(Action action)
    public void RemoveFromCombo(Action action)
    public void InitializeDefaultCombo(Actor actor, WeaponItem? weapon)
    public void UpdateComboSequenceAfterGearChange(Actor actor)
    
    // Private helpers
    private void ReorderComboSequence()
}
```

**Source Lines**: 670-825 from original (156 lines)

---

### 4. ActionEnhancer (NEW)
**Responsibility**: Enhance action descriptions with modifier information

**Methods**:
```csharp
public class ActionEnhancer
{
    public string EnhanceActionDescription(ActionData data)
    
    // Private helpers for specific modifiers
    private string AddRollBonusInfo(ActionData data)
    private string AddDamageMultiplierInfo(ActionData data)
    private string AddComboBonusInfo(ActionData data)
    private string AddStatusEffectInfo(ActionData data)
    private string AddMultiHitInfo(ActionData data)
    private string AddSelfDamageInfo(ActionData data)
    private string AddStatBonusInfo(ActionData data)
    private string AddSpecialEffectInfo(ActionData data)
}
```

**Source Lines**: 463-535 from original (73 lines)

---

### 5. ActionFactory (REFACTORED)
**Responsibility**: Create Action objects from data with proper configuration

**Changes**:
- Move `CreateActionFromData()` method here
- Move parsing methods: `ParseActionType()`, `ParseTargetType()`
- Remove enhancement logic (delegate to ActionEnhancer)

**Methods**:
```csharp
public class ActionFactory
{
    public Action CreateActionFromData(ActionData data)
    public Action CreateBasicAttack()
    
    // Private helpers
    private ActionType ParseActionType(string type)
    private TargetType ParseTargetType(string targetType)
    private void ApplyActionProperties(Action action, ActionData data)
}
```

**Source Lines**: 416-461 + parsing (68 lines)

---

### 6. EnvironmentActionManager (NEW)
**Responsibility**: Manage environment-specific actions

**Methods**:
```csharp
public class EnvironmentActionManager
{
    public void AddEnvironmentActions(Actor actor, Environment environment)
    public void ClearEnvironmentActions(Actor actor)
    
    // Private helpers
    private bool IsEnvironmentAction(Action action)
}
```

**Source Lines**: 639-668 from original (30 lines)

---

### 7. CharacterActions (REFACTORED)
**Responsibility**: Orchestrate action management (Facade pattern)

**New Structure** (~250 lines):
```csharp
public class CharacterActions
{
    // Composition: Use new managers
    private readonly ClassActionManager _classActionManager;
    private readonly GearActionManager _gearActionManager;
    private readonly ComboSequenceManager _comboSequenceManager;
    private readonly EnvironmentActionManager _environmentActionManager;
    private readonly ActionFactory _actionFactory;
    
    public CharacterActions()
    {
        _classActionManager = new ClassActionManager();
        _gearActionManager = new GearActionManager();
        _comboSequenceManager = new ComboSequenceManager();
        _environmentActionManager = new EnvironmentActionManager();
        _actionFactory = new ActionFactory();
    }
    
    // PUBLIC API - Facade methods
    public void AddDefaultActions(Actor actor)
    public void EnsureBasicAttackAvailable(Actor actor)
    public void AddClassActions(Actor actor, CharacterProgression progression, WeaponType? weaponType)
    public void AddWeaponActions(Actor actor, WeaponItem weapon)
    public void AddArmorActions(Actor actor, Item armor)
    public void RemoveWeaponActions(Actor actor, WeaponItem? weapon = null)
    public void RemoveArmorActions(Actor actor, Item? armor = null)
    public void RemoveItemActions(Actor actor)
    public void ApplyRollBonusesFromGear(Actor actor, Item gear)
    public void RemoveRollBonusesFromGear(Actor actor, Item gear)
    public void AddEnvironmentActions(Actor actor, Environment environment)
    public void ClearEnvironmentActions(Actor actor)
    
    // Combo system - delegated to ComboSequenceManager
    public List<Action> GetComboActions() => _comboSequenceManager.GetComboActions();
    public List<Action> GetActionPool(Actor actor)
    public void AddToCombo(Action action) => _comboSequenceManager.AddToCombo(action);
    public void RemoveFromCombo(Action action) => _comboSequenceManager.RemoveFromCombo(action);
    public void InitializeDefaultCombo(Actor actor, WeaponItem? weapon)
    public void UpdateComboSequenceAfterGearChange(Actor actor)
    
    // Utility methods
    public List<Action> GetAvailableUniqueActions(WeaponItem? weapon)
    public void UpdateComboBonus(CharacterEquipment equipment)
    
    // Exposed for compatibility
    public List<Action> ComboSequence => _comboSequenceManager.ComboSequence;
}
```

**Benefits of Facade Pattern**:
- ✅ Public API remains identical - **no breaking changes**
- ✅ Internal complexity hidden
- ✅ Easy to swap implementations
- ✅ Clients don't need to know about internal managers

---

## 📋 Implementation Sequence

### Phase 1: Create New Managers (No Breaking Changes)
1. Create `ActionFactory.cs` (no current file exists, new)
2. Create `ClassActionManager.cs` (extract from CharacterActions)
3. Create `ActionEnhancer.cs` (extract from CharacterActions)
4. Create `EnvironmentActionManager.cs` (extract from CharacterActions)

### Phase 2: Create Complex Managers
5. Create `GearActionManager.cs` (extract largest section)
6. Create `ComboSequenceManager.cs` (extract combo system)

### Phase 3: Refactor CharacterActions
7. Update `CharacterActions.cs` to use new managers (Facade pattern)
8. Ensure all tests pass
9. Update documentation

---

## 🧪 Testing Strategy

### Before Refactoring
```bash
# Run existing tests to establish baseline
dotnet test
```

### During Refactoring (per phase)
```bash
# After each phase, run incremental tests
dotnet test --filter "ClassName"
```

### Unit Tests to Create/Update
1. **ClassActionManagerTests** - Test class action addition/removal
2. **GearActionManagerTests** - Test gear action handling
3. **ComboSequenceManagerTests** - Test combo operations
4. **ActionEnhancerTests** - Test description enhancement
5. **ActionFactoryTests** - Test action creation
6. **EnvironmentActionManagerTests** - Test environment actions
7. **CharacterActionsTests** - Test facade pattern integration

---

## 📊 Impact Analysis

### Complexity Reduction
| Metric | Before | After | Reduction |
|--------|--------|-------|-----------|
| **Main File Size** | 828 lines | 250 lines | 70% |
| **Average Manager Size** | - | ~140 lines | - |
| **Cognitive Load** | Very High | Low | ~75% |
| **Number of Methods** | 45 | 8 (facade) | 82% |

### Maintainability Improvements
- ✅ **Class Action Changes** - Only modify `ClassActionManager`
- ✅ **Gear Action Changes** - Only modify `GearActionManager`
- ✅ **Combo Logic Changes** - Only modify `ComboSequenceManager`
- ✅ **Description Enhancement** - Only modify `ActionEnhancer`

### Testing Benefits
- ✅ Easier to unit test individual managers
- ✅ Reduced mock objects needed per test
- ✅ Can test edge cases in isolation
- ✅ Faster test execution time

---

## ⚠️ Breaking Changes

**NONE** - Facade pattern ensures full backward compatibility:
- ✅ All public methods remain accessible
- ✅ All method signatures identical
- ✅ All return types identical
- ✅ Composition replaces complexity

---

## 🔄 Migration Path

### For Existing Code
```csharp
// Old way (still works - no changes needed)
var characterActions = new CharacterActions();
characterActions.AddWeaponActions(actor, weapon);

// New way (if you want to work with managers directly)
var gearActionManager = new GearActionManager();
gearActionManager.AddWeaponActions(actor, weapon);
```

---

## 📚 Related Documentation

- **ARCHITECTURE.md** - System architecture overview
- **CODE_PATTERNS.md** - Design patterns used in project
- **PROBLEM_SOLUTIONS.md** - Solutions to known issues
- **DEVELOPMENT_WORKFLOW.md** - Development process

---

## ✅ Acceptance Criteria

- [ ] All new managers created with clear responsibilities
- [ ] CharacterActions refactored as facade with composition
- [ ] All public APIs remain unchanged
- [ ] 100% test pass rate
- [ ] Code review approval
- [ ] Documentation updated
- [ ] No performance regression

---

**Status**: Ready for Implementation  
**Priority**: Medium (Code Quality Improvement)  
**Effort**: ~4-6 hours  
**Risk**: Low (Facade pattern, full backward compatibility)

