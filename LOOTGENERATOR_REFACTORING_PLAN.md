# LootGenerator - Comprehensive Refactoring Plan

**Status**: Ready for Implementation  
**Target Reduction**: 75% (608 → 150 lines in main file)  
**New Managers**: 4 specialized + 1 data cache  

---

## Visual Architecture Transformation

### BEFORE: Monolithic Design
```
┌─────────────────────────────────────────┐
│      LootGenerator (608 lines)          │
├─────────────────────────────────────────┤
│ Data Loading (150 lines)                │
│  • LoadTierDistributions()              │
│  • LoadWeaponData()                     │
│  • LoadArmorData()                      │
│  • LoadStatBonuses()                    │
│  • etc...                               │
├─────────────────────────────────────────┤
│ Tier Calculation (80 lines)             │
│  • RollTier()                           │
│  • GetTierDistribution()                │
│  • Tier lookups & calculations          │
├─────────────────────────────────────────┤
│ Item Selection (120 lines)              │
│  • RollWeapon()                         │
│  • RollArmor()                          │
│  • Item filtering                       │
├─────────────────────────────────────────┤
│ Rarity Processing (100 lines)           │
│  • RollRarity()                         │
│  • Rarity scaling                       │
│  • Name generation                      │
├─────────────────────────────────────────┤
│ Bonus Application (100 lines)           │
│  • ApplyBonuses()                       │
│  • ApplyStatBonus()                     │
│  • ApplyActionBonus()                   │
│  • ApplyModification()                  │
├─────────────────────────────────────────┤
│ Main Logic (50 lines)                   │
│  • Loot chance calculation              │
│  • Item type selection                  │
│  • Coordination logic                   │
└─────────────────────────────────────────┘
```

### AFTER: Modular Design
```
LootGenerator (Static Facade - 150 lines)
├─ GenerateLoot() - Main entry point
├─ Initialize() - Setup managers
├─ GetLootChance() - Utility
└─ Manager coordination

    ↓ coordinates ↓

┌─────────────────┐ ┌──────────────────┐
│ LootDataCache   │ │ LootTierCalc     │
│ (80 lines)      │ │ (100 lines)      │
├─────────────────┤ ├──────────────────┤
│ • Tier Distrib. │ │ • CalculateLoot  │
│ • Weapons       │ │   Level()        │
│ • Armor         │ │ • RollTier()     │
│ • StatBonuses   │ │ • GetTierDist()  │
│ • ActionBonuses │ │ • Clamp levels   │
│ • Modific.      │ │                  │
│ • Rarities      │ │                  │
└─────────────────┘ └──────────────────┘

┌──────────────────┐ ┌─────────────────┐
│ LootItemSelector │ │ LootRarityProc   │
│ (120 lines)      │ │ (100 lines)      │
├──────────────────┤ ├─────────────────┤
│ • IsWeapon()     │ │ • RollRarity()   │
│ • SelectItem()   │ │ • ApplyRarityScl │
│ • RollWeapon()   │ │ • Rarity lookup  │
│ • RollArmor()    │ │ • Scaling logic  │
│ • Filtering      │ │                  │
│ • Selection      │ │                  │
└──────────────────┘ └─────────────────┘

        ↓ uses ↓

┌──────────────────┐
│ LootBonusApplier │
│ (120 lines)      │
├──────────────────┤
│ • ApplyBonuses() │
│ • ApplyStatBonus │
│ • ApplyActionBon │
│ • ApplyModif()   │
│ • Name generation│
└──────────────────┘
```

---

## Component Specifications

### LootDataCache - Centralized Data Management

**Purpose**: Single source of truth for all loot data loading and caching

**Location**: `Code/Data/LootDataCache.cs`

**Key Fields**:
```csharp
public class LootDataCache
{
    public List<TierDistribution> TierDistributions { get; }
    public List<WeaponData> WeaponData { get; }
    public List<ArmorData> ArmorData { get; }
    public List<StatBonus> StatBonuses { get; }
    public List<ActionBonus> ActionBonuses { get; }
    public List<Modification> Modifications { get; }
    public List<RarityData> RarityData { get; }
}
```

**Key Methods**:
```csharp
public static LootDataCache Load()           // Load all data
public void Reload()                         // Clear and reload
public void Clear()                          // Clear cache
```

**Size**: ~80 lines

---

### LootTierCalculator - Tier Determination

**Purpose**: Calculate appropriate loot tier based on player/dungeon levels

**Location**: `Code/Data/LootTierCalculator.cs`

**Responsibilities**:
- Determine loot level from player vs dungeon level
- Clamp loot level to valid range (1-100)
- Select tier based on loot level
- Look up tier distribution

**Key Methods**:
```csharp
public class LootTierCalculator
{
    // Calculate adjusted loot level
    public int CalculateLootLevel(int playerLevel, int dungeonLevel)
    
    // Roll tier based on loot level
    public int RollTier(int lootLevel)
    
    // Get tier distribution
    public TierDistribution? GetTierDistribution(int lootLevel)
    
    // Clamp to valid range
    private int ClampLootLevel(int level)
}
```

**Algorithm**:
```
LootLevel = DungeonLevel - (PlayerLevel - DungeonLevel)
If LootLevel < 1: LootLevel = 1
If LootLevel > 100: LootLevel = 100
```

**Size**: ~100 lines

---

### LootItemSelector - Item Selection

**Purpose**: Select specific item (weapon/armor) based on tier

**Location**: `Code/Data/LootItemSelector.cs`

**Responsibilities**:
- Determine if loot is weapon or armor
- Filter items by tier
- Randomly select from filtered items
- Load item data

**Key Methods**:
```csharp
public class LootItemSelector
{
    // 25% weapon, 75% armor
    public bool DetermineIsWeapon()
    
    // Select appropriate item
    public Item? SelectItem(int tier, bool isWeapon)
    
    // Get random weapon of tier
    public WeaponItem? RollWeapon(int tier)
    
    // Get random armor of tier
    public ArmorItem? RollArmor(int tier)
    
    // Filter items by tier
    private List<T> FilterByTier<T>(List<T> items, int tier)
}
```

**Logic**:
- Filter weapons/armor by tier
- Randomly select from filtered list
- Return selected item

**Size**: ~120 lines

---

### LootRarityProcessor - Rarity Handling

**Purpose**: Determine rarity and apply rarity-based scaling

**Location**: `Code/Data/LootRarityProcessor.cs`

**Responsibilities**:
- Roll for rarity (considering magic find)
- Apply rarity scaling to items
- Handle rarity-based name generation
- Lookup rarity data

**Key Methods**:
```csharp
public class LootRarityProcessor
{
    // Roll for rarity level
    public RarityData RollRarity(double magicFind, int playerLevel)
    
    // Apply rarity scaling
    public void ApplyRarityScaling(Item item, RarityData rarity)
    
    // Get rarity name
    public string GetRarityName(RarityData rarity)
}
```

**Rarity Calculation**:
- Base rarity chance from table
- Adjusted by magic find
- Scaled by player level

**Size**: ~100 lines

---

### LootBonusApplier - Bonus Application

**Purpose**: Apply stat bonuses, action bonuses, and modifications

**Location**: `Code/Data/LootBonusApplier.cs`

**Responsibilities**:
- Select bonuses based on rarity
- Apply stat bonuses
- Apply action bonuses
- Apply modifications
- Generate item names

**Key Methods**:
```csharp
public class LootBonusApplier
{
    // Apply all bonuses for rarity
    public void ApplyBonuses(Item item, RarityData rarity)
    
    // Apply single stat bonus
    public void ApplyStatBonus(Item item, StatBonus bonus)
    
    // Apply single action bonus
    public void ApplyActionBonus(Item item, ActionBonus bonus)
    
    // Apply single modification
    public void ApplyModification(Item item, Modification modification)
    
    // Generate item name from bonuses
    public string GenerateItemName(Item item, List<StatBonus> bonuses)
}
```

**Flow**:
1. Get bonuses for rarity level
2. Apply each bonus to item
3. Update item name with bonuses
4. Apply modifications

**Size**: ~120 lines

---

### LootGenerator (Refactored Facade)

**Purpose**: Coordinate managers and provide public static API

**Location**: `Code/Data/LootGenerator.cs` (refactored)

**Key Changes**:
- 608 → 150 lines
- All public API unchanged (100% compatible)
- Delegates to managers internally
- Lazy manager initialization

**Public API (UNCHANGED)**:
```csharp
public static class LootGenerator
{
    // Main generation method
    public static Item? GenerateLoot(
        int playerLevel, 
        int dungeonLevel, 
        Character? player = null, 
        bool guaranteedLoot = false)
    
    // Initialization
    public static void Initialize()
}
```

**Implementation**:
```csharp
private static LootDataCache? _dataCache;
private static LootTierCalculator? _tierCalculator;
private static LootItemSelector? _itemSelector;
private static LootRarityProcessor? _rarityProcessor;
private static LootBonusApplier? _bonusApplier;
private static Random _random = new();

public static Item? GenerateLoot(...)
{
    // 1. Calculate loot chance
    double lootChance = CalculateLootChance(...);
    if (_random.NextDouble() >= lootChance) return null;
    
    // 2. Calculate tier
    int lootLevel = _tierCalculator.CalculateLootLevel(...);
    int tier = _tierCalculator.RollTier(lootLevel);
    
    // 3. Select item
    bool isWeapon = _itemSelector.DetermineIsWeapon();
    Item? item = _itemSelector.SelectItem(tier, isWeapon);
    if (item == null) return null;
    
    // 4. Apply rarity
    var rarity = _rarityProcessor.RollRarity(...);
    _rarityProcessor.ApplyRarityScaling(item, rarity);
    
    // 5. Apply bonuses
    _bonusApplier.ApplyBonuses(item, rarity);
    
    return item;
}
```

**Size**: ~150 lines

---

## Implementation Steps

### Step 1: Create LootDataCache
- Extract all data loading logic
- Consolidate into single class
- Add Load/Reload/Clear methods

### Step 2: Create LootTierCalculator
- Extract tier-related methods
- Add level calculation logic
- Refactor RollTier()

### Step 3: Create LootItemSelector
- Extract weapon/armor selection
- Add DetermineIsWeapon()
- Refactor RollWeapon/RollArmor()

### Step 4: Create LootRarityProcessor
- Extract rarity logic
- Add RollRarity() logic
- Add scaling methods

### Step 5: Create LootBonusApplier
- Extract bonus application
- Consolidate all ApplyBonus methods
- Add name generation

### Step 6: Refactor LootGenerator
- Remove all extracted logic
- Implement lazy managers
- Coordinate through facade
- Verify all APIs unchanged

### Step 7: Testing
- Unit tests for each manager
- Integration tests
- Regression tests
- Backward compatibility verification

---

## Metrics & Goals

### Line Count Target
```
Before:  608 lines (LootGenerator only)
After:   150 lines (LootGenerator)
         + 80 lines (LootDataCache)
         + 100 lines (LootTierCalculator)
         + 120 lines (LootItemSelector)
         + 100 lines (LootRarityProcessor)
         + 120 lines (LootBonusApplier)
         ─────────────────────────
         Total:  670 lines

Main file reduction: 75% (608 → 150)
Total increase: +62 lines (better organization)
```

### Quality Goals
- ✅ 75% reduction in main file
- ✅ 5 focused, single-responsibility managers
- ✅ 100% backward compatible
- ✅ 0 errors, 0 warnings at build
- ✅ 95%+ test coverage (future phase)

---

## Risk Mitigation

### Risk 1: Static Methods Hard to Test
**Mitigation**: Create instance managers, lazy-initialize them

### Risk 2: Breaking Changes
**Mitigation**: All public APIs unchanged, refactoring is internal only

### Risk 3: Performance Impact
**Mitigation**: Lazy initialization, minimal overhead

### Risk 4: Complexity Increase
**Mitigation**: Clear documentation, established patterns (from UIManager)

---

## Success Criteria

✅ **Code Quality**
- [x] Architecture designed
- [ ] 75% main file reduction
- [ ] 5 focused managers
- [ ] Single responsibility each

✅ **Compatibility**
- [ ] All existing calls work
- [ ] No API changes
- [ ] 100% backward compatible

✅ **Build Status**
- [ ] 0 errors
- [ ] 0 warnings
- [ ] Compiles successfully

✅ **Testing**
- [ ] Unit tests written
- [ ] Integration tests
- [ ] Regression tests passing

---

## Timeline

| Phase | Task | Hours | Status |
|-------|------|-------|--------|
| 1 | Analysis & Plan | 2-3 | ✅ DONE |
| 2 | Implement managers | 6-8 | ⏳ NEXT |
| 2 | Refactor main class | 2-3 | ⏳ NEXT |
| 2 | Verification | 1-2 | ⏳ NEXT |
| 3 | Write tests | 20-25 | ⏳ LATER |
| **Total** | | **30-40** | |

---

## Documentation Structure

```
Documentation/02-Development/
├─ LOOTGENERATOR_REFACTORING_ANALYSIS.md  (WHY & WHAT)
├─ LOOTGENERATOR_REFACTORING_PLAN.md      (THIS FILE - HOW)
├─ LOOTGENERATOR_ARCHITECTURE.md          (DETAILED DESIGN)
├─ LOOTGENERATOR_TESTING_STRATEGY.md      (TESTING PLAN)
└─ LOOTGENERATOR_REFACTORING_COMPLETE.md  (RESULTS - AFTER)
```

---

## Comparison: Similar Refactorings

### UIManager (Completed)
- 634 lines → 463 lines (-27%)
- 4 managers created
- 100% backward compatible
- Ready for testing

### LootGenerator (Current)
- 608 lines → 150 lines (-75%)
- 5 managers + cache created
- 100% backward compatible
- More complex than UIManager

### Environment (Completed)
- 732 lines → 365 lines (-50%)
- Data-driven approach
- Eliminated switch statements
- 100% backward compatible

---

## Success Story Projection

Once complete, LootGenerator will have:
✅ 75% reduction in main file  
✅ Clear, focused managers  
✅ Easy to maintain and extend  
✅ Ready for comprehensive testing  
✅ Better code organization  
✅ Established pattern for future refactorings  

---

**Status**: ✅ Plan Complete, Ready for Implementation  
**Next Action**: Begin Phase 2 implementation  
**Estimated Completion**: 2-3 days (with testing)  

🚀 **Ready to start implementation!**

