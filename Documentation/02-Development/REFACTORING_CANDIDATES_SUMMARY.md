# Quick Reference: Refactoring Candidates

## Top 19 Large Classes - Analysis Summary

### 🔴 HIGH PRIORITY - Ready to Refactor Now

| File | Lines | Issue | Solution | Benefit | Effort |
|------|-------|-------|----------|---------|--------|
| **TestManager.cs** | 1,065 | 6 test suites in 1 file | Split into 6 runners | Clear test organization | Medium |
| **CharacterActions.cs** | 828 | 4 classes + combos mixed | Extract into 8 classes | Testable ability system | Medium-High |
| **BattleNarrative.cs** | 754 | Narrative + formatting mixed | Split into 3 classes | Reusable narrative | Low-Medium |
| **LootGenerator.cs** | 608 | Multiple generators mixed | Extract into 4 classes | Balanced loot system | Medium |
| **UIManager.cs** | 634 | UI display + logic mixed | Split into 4 classes | Better UI control | Medium |
| **CharacterEquipment.cs** | 554 | Equipment logic scattered | Extract into 4 classes | Cleaner equip system | Medium |
| **BattleNarrativeColoredText.cs** | 549 | Color logic + narrative | Extract color palette | Reusable colors | LOW ⭐ |

**Subtotal**: 4,992 lines → Could become ~35-40 smaller files (avg 140-160 lines)

---

### 🟡 MEDIUM PRIORITY - Suggested Improvements

| File | Lines | Suggestion | Benefit | Effort |
|------|-------|-----------|---------|--------|
| GameSystemTestRunner.cs | 958 | Split into 5 category runners | Better test organization | Medium |
| Environment.cs | 732 | Extract room/enemy/event logic | Cleaner dungeon code | Medium-High |
| MenuRenderer.cs | 491 | Split by menu type | Easier menu modifications | Medium |

**Subtotal**: 2,181 lines → Could become ~12-15 smaller files (avg 150-200 lines)

---

### 🟢 LOWER PRIORITY - Legitimate Size

| File | Lines | Reason | Action |
|------|-------|--------|--------|
| Game.cs | 1,383 | ✅ ALREADY REFACTORED | Complete ✅ |
| EnemyConfig.cs | 489 | Configuration data | Keep as-is |
| CombatResults.cs | 484 | Complex report structure | Keep as-is |
| SessionStatistics.cs | 452 | Statistics tracking | Keep as-is |
| EnemyLoader.cs | 426 | Data loading logic | Keep as-is |
| ComboManager.cs | 420 | Complex combo system | Keep as-is |
| ItemDisplayColoredText.cs | 411 | Heavy formatting file | Keep as-is |
| CharacterSaveManager.cs | 403 | Save/load logic | Keep as-is |

**Note**: These are well-organized and don't need refactoring.

---

## Quick Reference Table

### Before vs After Estimates

```
BEFORE REFACTORING:
├── Files > 400 lines: 19
├── Largest: 1,383 lines
├── Avg size: 713 lines
└── Organization: Mixed concerns

AFTER RECOMMENDED REFACTORING:
├── Files > 400 lines: ~5 (config/data only)
├── Largest: ~350 lines
├── Avg size: 250-300 lines ✅
└── Organization: Clear separation ✅
```

---

## Refactoring Examples (What to Extract)

### From TestManager.cs (1,065 → 6 runners)
```
✂️ Extract:
├── ItemGenerationTestRunner.cs (200 lines)
├── CommonItemModificationTestRunner.cs (150 lines)
├── ItemNamingTestRunner.cs (150 lines)
├── ColorParserTestRunner.cs (200 lines)
├── ColorDebugTestRunner.cs (100 lines)
└── TestHarnessBase.cs (150 lines)

Result: 6 focused files, each <250 lines
```

### From CharacterActions.cs (828 → 8 classes)
```
✂️ Extract:
├── CharacterActionsBase.cs (300 lines)
├── BarbarianActionLoader.cs (80 lines)
├── WarriorActionLoader.cs (80 lines)
├── RogueActionLoader.cs (80 lines)
├── WizardActionLoader.cs (80 lines)
├── GearActionManager.cs (150 lines)
├── ComboSystem.cs (100 lines)
└── ActionPoolManager.cs (100 lines)

Result: 8 focused files, each <300 lines
```

### From BattleNarrative.cs (754 → 3 classes)
```
✂️ Extract:
├── BattleNarrativeGenerator.cs (300 lines)
├── BattleNarrativeFormatter.cs (250 lines)
└── BattleNarrativeDisplay.cs (150 lines)

Result: 3 focused files, each <300 lines
```

---

## Recommended Execution Plan

### Phase 1: Quick Win ⭐ (30 min - 1 hour)
```
1. BattleNarrativeColoredText.cs → Extract color palette
   
   Low effort, immediate benefit
   This is a quick win!
```

### Phase 2: Testing (2-3 hours)
```
1. TestManager.cs → 6 test runners
2. GameSystemTestRunner.cs → 5 test runners

   Better organized tests
   Easier to debug individual tests
```

### Phase 3: Core Systems (3-4 hours)
```
1. CharacterActions.cs → 8 ability classes
2. CharacterEquipment.cs → 4 equipment classes

   Most impactful for game systems
   Highly testable
```

### Phase 4: Combat/UI (4-6 hours)
```
1. BattleNarrative.cs → 3 narrative classes
2. UIManager.cs → 4 UI classes
3. MenuRenderer.cs → 4 renderer classes

   Better combat/UI organization
```

### Phase 5: Generation (3-4 hours)
```
1. LootGenerator.cs → 4 generator classes
2. Environment.cs → 3 room classes

   Better balance/generation control
```

---

## Expected Outcomes

### Code Quality Improvements
- ✅ 45-50 well-organized files (instead of 19 large ones)
- ✅ Average file size: 250-300 lines (instead of 713)
- ✅ Most files: <250 lines
- ✅ Clear separation of concerns
- ✅ Better testability
- ✅ SOLID principles throughout

### Organization Before/After
```
BEFORE:
- 1 file with 6 test systems → 1,065 lines
- 1 file with 4 character classes → 828 lines
- Hard to find specific logic
- Difficult to test

AFTER:
- 6 test runner files (avg 150 lines each)
- 8 character class files (avg 100 lines each)
- Easy to navigate
- Easy to test individual pieces
```

### Estimated Effort
```
Phase 1: 0.5 hours  (Quick win)
Phase 2: 2-3 hours  (Testing)
Phase 3: 3-4 hours  (Core systems)
Phase 4: 4-6 hours  (Combat/UI)
Phase 5: 3-4 hours  (Generation)
─────────────────────────────
TOTAL: 12.5-17.5 hours

Could be done in 2-3 focused sessions!
```

---

## Why This Matters

### Current Challenges
- 🔴 Large files hard to navigate
- 🔴 Mixed concerns make debugging difficult
- 🔴 Hard to write focused unit tests
- 🔴 Difficult to understand file purpose at a glance

### After Refactoring
- ✅ Small files, easy to understand
- ✅ Clear concerns, easy to debug
- ✅ Testable components
- ✅ Professional code organization

---

## Your Next Steps

### Option 1: Continue Building
Focus on new features and keep current structure.

### Option 2: Refactor Now
Execute 1-2 phases while momentum is high.

### Option 3: Hybrid Approach
Do Phase 1 quick win (30 min), continue building.
Then do Phase 2-5 when ready.

---

## Success Metrics

Once refactoring is complete:
- [ ] All files < 350 lines
- [ ] 80%+ of files < 250 lines
- [ ] 100+ unit tests possible
- [ ] Clear class/manager responsibilities
- [ ] SOLID principles applied
- [ ] Code quality: ⭐⭐⭐⭐⭐

---

## Conclusion

The Game.cs refactoring proved the approach works! The same patterns can clean up 13,547 lines across 19 files.

**Quick wins available**: Start with Phase 1 (30 min) for immediate value.

**Recommended**: Phase 1 + 2 (2.5-4 hours) for major code cleanup.

**Full transformation**: All 5 phases (12.5-17.5 hours) for industry-standard quality.

Your codebase is already good—this would make it excellent! 🚀

