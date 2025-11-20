# CharacterActions Refactoring - Quick Reference

## One-Minute Summary

**Current**: CharacterActions.cs (828 lines) doing 11 things  
**Problem**: Too large, hard to maintain, hard to test  
**Solution**: Split into 7 focused classes  
**Result**: 828 lines → 7 files averaging ~140 lines each  
**Time**: ~7-11 hours  
**Risk**: Low (Facade pattern, backward compatible)  
**Gain**: Easier maintenance, better testing, cleaner code

---

## File Mapping

| Current Location | New File | New Size |
|------------------|----------|----------|
| Lines 105-177 | ClassActionManager.cs | ~150 |
| Lines 197-615 | GearActionManager.cs | ~180 |
| Lines 670-825 | ComboSequenceManager.cs | ~120 |
| Lines 463-535 | ActionEnhancer.cs | ~80 |
| Lines 416-461 | ActionFactory.cs | ~80 |
| Lines 639-668 | EnvironmentActionManager.cs | ~60 |
| Lines 26-86, 88-103 | CharacterActions (remain) | ~250 |

---

## New Class Locations

```
Code/Entity/
├── CharacterActions.cs (REFACTORED)
├── ClassActionManager.cs (NEW)
├── GearActionManager.cs (NEW)
├── ComboSequenceManager.cs (NEW)
├── ActionEnhancer.cs (NEW)
├── ActionFactory.cs (NEW)
└── EnvironmentActionManager.cs (NEW)
```

---

## What Each Manager Does

| Manager | Responsibility | Key Methods |
|---------|---|---|
| **ClassActionManager** | Barbarian, Warrior, Rogue, Wizard actions | AddClassActions, AddBarbarianActions, AddWarriorActions, AddRogueActions, AddWizardActions, RemoveClassActions |
| **GearActionManager** | Weapon & armor actions | AddWeaponActions, AddArmorActions, RemoveWeaponActions, RemoveArmorActions, ApplyRollBonusesFromGear, RemoveRollBonusesFromGear |
| **ComboSequenceManager** | Combo system | GetComboActions, AddToCombo, RemoveFromCombo, InitializeDefaultCombo, UpdateComboSequenceAfterGearChange |
| **ActionEnhancer** | Description enhancement | EnhanceActionDescription |
| **ActionFactory** | Create actions | CreateActionFromData, CreateBasicAttack |
| **EnvironmentActionManager** | Environment actions | AddEnvironmentActions, ClearEnvironmentActions |
| **CharacterActions** | Orchestrate all | Facade methods delegating to managers |

---

## Implementation Checklist

### Phase 1: Foundation (Independent Managers)
- [ ] Create ActionFactory.cs
  - `CreateActionFromData(ActionData data)`
  - `CreateBasicAttack()`
  - `ParseActionType(string type)`
  - `ParseTargetType(string targetType)`
  - `ApplyActionProperties(Action action, ActionData data)`

- [ ] Create ActionEnhancer.cs
  - `EnhanceActionDescription(ActionData data)`
  - Private helpers for each modifier type

- [ ] Create ClassActionManager.cs
  - `AddClassActions(Actor actor, CharacterProgression progression, WeaponType? weaponType)`
  - `RemoveClassActions(Actor actor)`
  - `AddBarbarianActions(Actor actor, CharacterProgression progression)`
  - `AddWarriorActions(Actor actor, CharacterProgression progression)`
  - `AddRogueActions(Actor actor, CharacterProgression progression)`
  - `AddWizardActions(Actor actor, CharacterProgression progression, WeaponType? weaponType)`

- [ ] Create EnvironmentActionManager.cs
  - `AddEnvironmentActions(Actor actor, Environment environment)`
  - `ClearEnvironmentActions(Actor actor)`
  - `IsEnvironmentAction(Action action)`

### Phase 2: Complex Managers
- [ ] Create GearActionManager.cs
  - `AddWeaponActions(Actor actor, WeaponItem weapon)`
  - `AddArmorActions(Actor actor, Item armor)`
  - `RemoveWeaponActions(Actor actor, WeaponItem? weapon)`
  - `RemoveArmorActions(Actor actor, Item? armor)`
  - `ApplyRollBonusesFromGear(Actor actor, Item gear)`
  - `RemoveRollBonusesFromGear(Actor actor, Item gear)`
  - Private helpers

- [ ] Create ComboSequenceManager.cs
  - `GetComboActions()`
  - `AddToCombo(Action action)`
  - `RemoveFromCombo(Action action)`
  - `InitializeDefaultCombo(Actor actor, WeaponItem? weapon)`
  - `UpdateComboSequenceAfterGearChange(Actor actor)`
  - `ReorderComboSequence()`

### Phase 3: Refactor Main Class
- [ ] Refactor CharacterActions.cs
  - Add manager fields
  - Initialize managers in constructor
  - Convert methods to facade pattern
  - Keep public API identical
  - Update DebugLogger calls as needed

- [ ] Create unit tests
  - ClassActionManagerTests
  - GearActionManagerTests
  - ComboSequenceManagerTests
  - ActionEnhancerTests
  - ActionFactoryTests
  - EnvironmentActionManagerTests
  - CharacterActionsTests (integration)

- [ ] Run full test suite
  - All existing tests pass
  - New tests pass
  - No regressions

- [ ] Update documentation
  - ARCHITECTURE.md
  - CODE_PATTERNS.md
  - This refactoring folder

---

## Code Template: Manager Pattern

### Template for new manager:
```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages [specific responsibility]
    /// </summary>
    public class [ManagerName]Manager
    {
        public [ManagerName]Manager()
        {
        }

        // Public methods (clear interface)
        public void [PublicMethod]()
        {
            DebugLogger.LogMethodEntry("[ManagerName]Manager", "[PublicMethod]");
            // Implementation
        }

        // Private methods (implementation details)
        private void [PrivateHelper]()
        {
            // Helper logic
        }
    }
}
```

### Template for facade method:
```csharp
public void [MethodName](params...)
{
    _[manager].{MethodName}(params...);
}
```

---

## Key Dependencies

Each manager only needs:
- **ActionLoader** (static, for loading actions from JSON)
- **DebugLogger** (static, for logging)
- **Actor** (parameter, entity to modify)
- **Item** / **WeaponItem** (parameters)
- **Environment** (parameter, for environment actions)
- **CharacterProgression** (parameter, for class actions)

---

## Backward Compatibility

```csharp
// This code continues to work EXACTLY as before:
var characterActions = new CharacterActions();
characterActions.AddWeaponActions(actor, sword);
characterActions.AddToCombo(slashAction);
characterActions.GetComboActions();

// NO CHANGES NEEDED in:
// - Character.cs
// - CharacterEquipment.cs
// - InventoryManager.cs
// - Combat.cs
// - Any other file using CharacterActions
```

---

## Common Issues to Avoid

❌ **Don't**: Duplicate ActionLoader calls  
✅ **Do**: Load once, pass result, or cache

❌ **Don't**: Add dependencies between managers (circular)  
✅ **Do**: Let CharacterActions orchestrate

❌ **Don't**: Change method signatures  
✅ **Do**: Keep public API identical

❌ **Don't**: Remove ComboSequence public property  
✅ **Do**: Keep it accessible via facade

❌ **Don't**: Forget DebugLogger calls  
✅ **Do**: Add for debugging (follow existing pattern)

---

## Testing Quick Start

### Basic test structure:
```csharp
[TestFixture]
public class [ManagerName]Tests
{
    private [ManagerName] _manager;
    private Actor _testActor;

    [SetUp]
    public void SetUp()
    {
        _manager = new [ManagerName]();
        _testActor = CreateTestActor();
    }

    [Test]
    public void [MethodName]_[Scenario]_[ExpectedResult]()
    {
        // Arrange
        
        // Act
        _manager.[MethodName](_testActor);
        
        // Assert
    }

    private Actor CreateTestActor()
    {
        // Create test actor with necessary setup
    }
}
```

---

## Documentation Updates

After refactoring, update:
- [ ] ARCHITECTURE.md (add new managers to section 2.4)
- [ ] CODE_PATTERNS.md (add facade pattern example)
- [ ] QUICK_REFERENCE.md (add manager descriptions)
- [ ] This refactoring folder (move to 06-Archive when complete)

---

## Rollback Plan

If issues arise:
1. Git stash changes
2. Revert to previous commit
3. Keep documentation for reference
4. Can be attempted again with adjustments

---

## Success Indicators

✅ All tests pass  
✅ No performance degradation  
✅ Code is easier to read  
✅ Easier to make changes  
✅ Easier to add tests  
✅ Team satisfaction with design  

---

## Need Help?

Refer to:
- **CHARACTERACTIONS_REFACTORING_PROPOSAL.md** - Detailed design
- **CHARACTERACTIONS_REFACTORING_VISUAL.md** - Visual architecture
- **CHARACTERACTIONS_REFACTORING_SUMMARY.md** - Complete overview
- **CODE_PATTERNS.md** - Pattern examples in codebase

---

**Last Updated**: 2025-11-20  
**Status**: Ready for Implementation  
**Next**: Start Phase 1 when approved

