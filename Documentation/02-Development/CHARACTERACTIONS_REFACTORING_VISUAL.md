# CharacterActions Refactoring - Visual Guide

## Current Architecture (828 lines - Monolithic)

```
┌─────────────────────────────────────────────────────────────────┐
│                    CharacterActions.cs (828 lines)               │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  • AddDefaultActions() ─────────┐                                │
│  • EnsureBasicAttackAvailable() │ Default Action Logic           │
│                                 │                                │
│  • AddClassActions()            │                                │
│  • AddBarbarianActions()        │                                │
│  • AddWarriorActions()          ├─ Class Action Logic (105-177)  │
│  • AddRogueActions()            │                                │
│  • AddWizardActions()           │                                │
│  • RemoveClassActions()         │                                │
│                                 │                                │
│  • AddWeaponActions()           │                                │
│  • AddArmorActions()            │                                │
│  • RemoveWeaponActions()        │                                │
│  • RemoveArmorActions()         ├─ Gear Action Logic (197-615)   │
│  • AddGearActions()             │                                │
│  • GetGearActions()             │                                │
│  • GetWeaponActionsFromJson()   │                                │
│  • GetRandomArmorActionFromJson()│                               │
│  • HasSpecialArmorActions()     │                                │
│  • LoadGearActionFromJson()     │                                │
│  • ApplyRollBonusesFromGear()   │                                │
│  • RemoveRollBonusesFromGear()  │                                │
│                                 │                                │
│  • CreateActionFromData()       ├─ Action Creation Logic         │
│  • EnhanceActionDescription()   │ (380-535)                      │
│  • ParseActionType()            │                                │
│  • ParseTargetType()            │                                │
│                                 │                                │
│  • AddEnvironmentActions()      ├─ Environment Logic (639-668)   │
│  • ClearEnvironmentActions()    │                                │
│                                 │                                │
│  • GetComboActions()            │                                │
│  • GetActionPool()              ├─ Combo System Logic (670-825)  │
│  • AddToCombo()                 │                                │
│  • RemoveFromCombo()            │                                │
│  • ReorderComboSequence()       │                                │
│  • InitializeDefaultCombo()     │                                │
│  • UpdateComboSequenceAfterGearChange()│                         │
│                                 │                                │
│  • GetAvailableUniqueActions()  ├─ Utility Methods              │
│  • UpdateComboBonus()           │                                │
│                                 ▼                                │
└─────────────────────────────────────────────────────────────────┘

PROBLEMS:
❌ 828 lines - too large
❌ 11 different responsibilities
❌ Hard to test individual features
❌ Hard to find specific logic
❌ Changes to one area risk others
```

---

## Proposed Architecture (7 focused files)

```
                    ┌────────────────────────────────────┐
                    │  CharacterActions.cs (250 lines)    │
                    │  ── Facade & Orchestrator ──        │
                    │                                     │
                    │  • AddDefaultActions()              │
                    │  • AddClassActions()                │
                    │  • AddWeaponActions()               │
                    │  • RemoveWeaponActions()            │
                    │  • AddToCombo()                     │
                    │  • ... (facade methods)             │
                    └────────┬─────────────────────────────┘
                             │
         ┌───────────────────┼───────────────────┬──────────────────┐
         │                   │                   │                  │
    ┌────▼──────────┐  ┌────▼──────────┐  ┌────▼──────────┐  ┌────▼──────────┐
    │ ClassAction   │  │ GearAction    │  │ ComboSequence │  │ Environment   │
    │ Manager       │  │ Manager       │  │ Manager       │  │ Action Mgr    │
    │ (150 lines)   │  │ (180 lines)   │  │ (120 lines)   │  │ (60 lines)    │
    ├───────────────┤  ├───────────────┤  ├───────────────┤  ├───────────────┤
    │               │  │               │  │               │  │               │
    │ • Add...      │  │ • Add...      │  │ • GetCombo    │  │ • Add...      │
    │ • Remove...   │  │ • Remove...   │  │ • AddToCombo  │  │ • Clear...    │
    │              │  │ • Apply...    │  │ • RemoveFrom  │  │              │
    │ Responsible  │  │ • LoadGear    │  │ • Initialize  │  │ Responsible  │
    │ for:         │  │              │  │ • Update...   │  │ for:         │
    │             │  │ Responsible   │  │              │  │             │
    │ - Barbarian  │  │ for:         │  │ Responsible  │  │ - Adding    │
    │ - Warrior    │  │             │  │ for:         │  │   env actions│
    │ - Rogue      │  │ - Weapon    │  │             │  │ - Removing  │
    │ - Wizard     │  │   actions   │  │ - Combo     │  │   env actions│
    │   actions    │  │ - Armor     │  │   management│  │             │
    │             │  │   actions   │  │ - Ordering  │  │             │
    │             │  │ - Roll      │  │ - Sequencing│  │             │
    │             │  │   bonuses   │  │             │  │             │
    │             │  │             │  │             │  │             │
    └───────────────┘  └───────────────┘  └───────────────┘  └───────────────┘


    ┌────────────────────┐      ┌────────────────────┐
    │ ActionFactory      │      │ ActionEnhancer     │
    │ (80 lines)         │      │ (80 lines)         │
    ├────────────────────┤      ├────────────────────┤
    │                    │      │                    │
    │ • CreateAction     │      │ • Enhance...       │
    │ • CreateBasicAtk   │      │ • AddRollBonus     │
    │ • ParseActionType  │      │ • AddDamageInfo    │
    │ • ParseTargetType  │      │ • AddComboBonus    │
    │ • Apply...         │      │ • AddStatusEffects │
    │                    │      │ • Add...           │
    │ Responsible for:   │      │                    │
    │                    │      │ Responsible for:   │
    │ - Creating Action  │      │                    │
    │   objects          │      │ - Enhancing action │
    │ - Configuring      │      │   descriptions     │
    │   properties       │      │ - Adding modifier  │
    │ - Type conversion  │      │   information      │
    │                    │      │                    │
    └────────────────────┘      └────────────────────┘


BENEFITS:
✅ Reduced complexity (828 → 250 in main file)
✅ Single Responsibility Principle
✅ Easier to test (unit tests per manager)
✅ Easier to maintain (changes isolated)
✅ Easy to find code (clear organization)
✅ Reusable components (compose as needed)
✅ No breaking changes (Facade pattern)
```

---

## Data Flow Diagram

### BEFORE: Monolithic Approach

```
Actor: "Equip Sword"
    │
    └──> CharacterActions.AddWeaponActions()
          │
          ├─ GetWeaponActionsFromJson()       ──> ActionLoader
          ├─ GetGearActions()                 
          ├─ LoadGearActionFromJson()        ──> ActionLoader (again)
          ├─ ApplyRollBonusesFromGear()       
          ├─ UpdateComboSequenceAfterGearChange()
          ├─ GetComboActions()
          ├─ ReorderComboSequence()
          └─> Actor: Actions added & Combo updated

PROBLEMS:
- Multiple responsibilities in one method
- Hard to trace data flow
- Difficult to debug issues
```

### AFTER: Modular Approach

```
Actor: "Equip Sword"
    │
    └──> CharacterActions.AddWeaponActions() [Facade]
          │
          └──> GearActionManager.AddWeaponActions()
                │
                ├─ GetWeaponActionsFromJson()    ──> ActionLoader
                ├─ LoadGearActionFromJson()      ──> ActionFactory
                ├─ ApplyRollBonusesFromGear()
                │
                └──> ComboSequenceManager.UpdateComboSequenceAfterGearChange()
                      │
                      └──> ReorderComboSequence()
                           └──> Actor: Combat Ready!

BENEFITS:
- Clear separation of concerns
- Easy to trace data flow
- Simple to debug issues
- Can test each step independently
```

---

## Responsibility Matrix

| Responsibility | Current | After | Manager |
|---|---|---|---|
| **Class Actions** | CharacterActions | ClassActionManager | ✓ |
| **Weapon Actions** | CharacterActions | GearActionManager | ✓ |
| **Armor Actions** | CharacterActions | GearActionManager | ✓ |
| **Roll Bonuses** | CharacterActions | GearActionManager | ✓ |
| **Combo Management** | CharacterActions | ComboSequenceManager | ✓ |
| **Environment Actions** | CharacterActions | EnvironmentActionManager | ✓ |
| **Action Creation** | CharacterActions | ActionFactory | ✓ |
| **Description Enhancement** | CharacterActions | ActionEnhancer | ✓ |
| **Orchestration** | CharacterActions | CharacterActions | ✓ |

---

## Integration Points

```
CharacterActions (Facade)
    │
    ├─ References
    │  ├─ ClassActionManager
    │  ├─ GearActionManager
    │  ├─ ComboSequenceManager
    │  ├─ EnvironmentActionManager
    │  ├─ ActionFactory
    │  └─ ActionEnhancer
    │
    ├─ Composed by
    │  ├─ Character class
    │  ├─ CharacterEquipment class
    │  └─ InventoryManager class
    │
    └─ Dependencies
       ├─ ActionLoader (static)
       ├─ Actor (parameter)
       ├─ Item hierarchy (parameter)
       ├─ CharacterProgression (parameter)
       └─ Environment (parameter)
```

---

## Method Mapping (Refactoring Guide)

### CharacterActions → New Homes

```
CharacterActions.AddDefaultActions()
  └─ CharacterActions (facade) → calls ClassActionManager

CharacterActions.AddClassActions()
  └─ ClassActionManager.AddClassActions()
     ├─ AddBarbarianActions()
     ├─ AddWarriorActions()
     ├─ AddRogueActions()
     └─ AddWizardActions()

CharacterActions.AddWeaponActions()
  └─ GearActionManager.AddWeaponActions()
     ├─ GetWeaponActionsFromJson()
     ├─ LoadGearActionFromJson()
     └─ ApplyRollBonusesFromGear()

CharacterActions.AddArmorActions()
  └─ GearActionManager.AddArmorActions()

CharacterActions.CreateActionFromData()
  └─ ActionFactory.CreateActionFromData()
     ├─ ApplyActionProperties()
     ├─ ParseActionType()
     └─ ParseTargetType()

CharacterActions.EnhanceActionDescription()
  └─ ActionEnhancer.EnhanceActionDescription()
     ├─ AddRollBonusInfo()
     ├─ AddDamageMultiplierInfo()
     ├─ AddComboBonusInfo()
     └─ ... (7 more helpers)

CharacterActions.AddToCombo()
  └─ ComboSequenceManager.AddToCombo()
     └─ ReorderComboSequence()

CharacterActions.AddEnvironmentActions()
  └─ EnvironmentActionManager.AddEnvironmentActions()
```

---

## Implementation Timeline

```
┌─────────────────────────────────────────────────────────┐
│ Phase 1: Foundation (Create independent managers)       │
├─────────────────────────────────────────────────────────┤
│ Day 1-2                                                  │
│ ✓ ActionFactory.cs                                       │
│ ✓ ActionEnhancer.cs                                      │
│ ✓ ClassActionManager.cs                                  │
│ ✓ EnvironmentActionManager.cs                            │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ Phase 2: Complex Managers (Build core functionality)    │
├─────────────────────────────────────────────────────────┤
│ Day 3-4                                                  │
│ ✓ GearActionManager.cs                                   │
│ ✓ ComboSequenceManager.cs                                │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ Phase 3: Integration (Refactor main class)              │
├─────────────────────────────────────────────────────────┤
│ Day 5                                                    │
│ ✓ CharacterActions.cs (refactored as facade)            │
│ ✓ Unit tests                                             │
│ ✓ Integration tests                                      │
│ ✓ Documentation                                          │
└─────────────────────────────────────────────────────────┘
```

---

## Example Usage (No Changes for Clients)

```csharp
// EXISTING CODE - Works as before (backward compatible)
var characterActions = new CharacterActions();

// Add weapon actions
characterActions.AddWeaponActions(character, ironSword);

// Manage combo
characterActions.AddToCombo(slashAction);
characterActions.RemoveFromCombo(slashAction);
var combo = characterActions.GetComboActions();

// Add class actions
characterActions.AddClassActions(character, progression, WeaponType.Sword);

// Environment actions
characterActions.AddEnvironmentActions(character, forest);
characterActions.ClearEnvironmentActions(character);

// ALL CODE ABOVE CONTINUES TO WORK WITHOUT CHANGES ✓

// OPTIONAL: Direct manager access (if needed)
var gearManager = new GearActionManager();
gearManager.AddWeaponActions(character, ironSword);
```

---

## Success Metrics

| Metric | Current | Target | Status |
|--------|---------|--------|--------|
| **Lines in CharacterActions** | 828 | <300 | 📊 |
| **Max lines per method** | ~150 | <50 | 📊 |
| **Avg method complexity** | High | Low | 📊 |
| **Test coverage** | Partial | >90% | 📊 |
| **Time to find code** | ~5 min | <1 min | 📊 |
| **Time to add feature** | ~30 min | ~10 min | 📊 |

---

## Related Files to Update

- [ ] Character.cs (verify integration)
- [ ] CharacterEquipment.cs (verify integration)
- [ ] InventoryManager.cs (verify integration)
- [ ] Unit tests (create/update)
- [ ] CODE_PATTERNS.md (document pattern)
- [ ] ARCHITECTURE.md (update diagrams)


