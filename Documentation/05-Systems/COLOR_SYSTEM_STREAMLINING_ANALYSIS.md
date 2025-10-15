# Color System Streamlining Analysis
**Date:** October 12, 2025  
**Issue:** Spacing issues when color is applied  
**Goal:** Streamline color system to apply only to intended keywords

---

## Current Color System Architecture

### Three Layers of Coloring

1. **Explicit Color Codes** (Manual)
   - Added directly in code: `&R{damage}&y`
   - Used for: Damage numbers, specific highlighting
   - Example: `"for &R6&y damage"` → 6 is red, rest is white

2. **Color Templates** (Manual)
   - Markup syntax: `{{template|text}}`
   - Used for: Named entities, special effects
   - Example: `"{{fiery|Blazing Sword}}"` → multi-color sequence

3. **Keyword Coloring** (Automatic)
   - Regex-based word matching
   - Used for: Auto-coloring common game terms
   - Example: `"damage"` → automatically wrapped in `{{damage|damage}}`

---

## The Spacing Problem

### Root Cause: Multiple Color Layers Conflicting

**Scenario 1: Double Coloring**
```
Original:    "for &R6&y damage"
After Keywords: "for &R6&y {{damage|damage}}"
After Expansion: "for &R6&y &rdamage&y"
Result: ❌ Extra color codes, potential spacing issues
```

**Scenario 2: Keywords in Stats**
```
Original:    "(roll: 9 | attack 4 - 2 armor | speed: 8.5s)"
After Keywords: "(roll: 9 | {{damage|attack}} 4 - 2 {{damage|armor}} | {{damage|speed}}: 8.5s)"
Result: ❌ Technical stats get colored when they should be plain
```

**Scenario 3: Over-Aggressive Keyword Matching**
```
Original:    "Slime is stunned"
Keywords Match: "Slime" (enemy), "stunned" (status)
After Keywords: "{{enemy|Slime}} is {{stunned|stunned}}"
Result: ⚠️ Works, but may color unintended text
```

---

## Current Safeguards

### 1. HasColorMarkup Check
**Location:** `BlockDisplayManager.cs` line 22-24

```csharp
if (ColorParser.HasColorMarkup(text))
{
    return text; // Skip keyword coloring
}
```

**Purpose:** Prevent keyword coloring on text with explicit codes  
**Status:** ✅ Working as designed  
**Limitation:** Only works if explicit codes exist

### 2. IsInsideColorMarkup Check
**Location:** `KeywordColorSystem.cs` line 274

```csharp
if (IsInsideColorMarkup(result, match.Index))
{
    return match.Value; // Don't color this keyword
}
```

**Purpose:** Skip keywords already inside `{{template|...}}`  
**Status:** ✅ Working as designed  
**Limitation:** Only checks templates, not adjacent color codes

### 3. No Coloring on Roll Info
**Location:** `BlockDisplayManager.cs` line 89

```csharp
UIManager.WriteLine($"    {rollInfo}", UIMessageType.RollInfo);
```

**Purpose:** Keep technical stats white  
**Status:** ✅ Fixed as of Oct 12, 2025  
**Result:** Roll info stays plain white

---

## Where Keyword Coloring is Applied

### Current Application Points

| Location | Method | What Gets Colored | Issue? |
|----------|--------|-------------------|--------|
| `BlockDisplayManager.cs:86` | `DisplayActionBlock()` | Combat action text | ✅ Correct |
| `BlockDisplayManager.cs:89` | `DisplayActionBlock()` | Roll info stats | ✅ Fixed (no coloring) |
| `BlockDisplayManager.cs:95` | `DisplayActionBlock()` | Status effects | ⚠️ May double-color |
| `BlockDisplayManager.cs:124` | `DisplayEffectBlock()` | Effect text | ⚠️ May double-color |
| `BlockDisplayManager.cs:129` | `DisplayEffectBlock()` | Effect details | ⚠️ May double-color |
| `BlockDisplayManager.cs:152` | `DisplayNarrativeBlock()` | Narrative text | ✅ Correct |
| `BlockDisplayManager.cs:174` | `DisplayEnvironmentalBlock()` | Environmental text | ✅ Correct |
| `BlockDisplayManager.cs:180` | `DisplayEnvironmentalBlock()` | Environmental effects | ⚠️ May double-color |
| `BlockDisplayManager.cs:199` | `DisplaySystemBlock()` | System messages | ✅ Correct |
| `BlockDisplayManager.cs:217` | `DisplayStatsBlock()` | Stats display | ⚠️ Should stats be colored? |
| `BlockDisplayManager.cs:235` | `DisplayMenuBlock()` | Menu text | ✅ Correct |
| `TextDisplayIntegration.cs:83` | `DisplayMenu()` | Menu titles | ✅ Correct |
| `TextDisplayIntegration.cs:108` | `DisplayMenu()` | Menu options | ✅ Correct |
| `TextDisplayIntegration.cs:225` | `DisplaySystem()` | System messages | ✅ Correct |
| `TextDisplayIntegration.cs:235` | `DisplayTitle()` | Title messages | ✅ Correct |

---

## Identified Issues

### Issue 1: Status Effects with Explicit Colors
**Location:** Environmental effects, status messages

**Problem:**
```csharp
// CombatEffectsSimplified.cs or similar
string effect = "&YSTUNNED&y for 2 turns";
// Later in BlockDisplayManager:
UIManager.WriteLine($"    {ApplyKeywordColoring(effect)}", ...);
```

**What Happens:**
1. Text has explicit colors: `&YSTUNNED&y`
2. `HasColorMarkup()` returns `true`
3. Keyword coloring is skipped ✅
4. **BUT**: If no explicit colors, keywords apply
5. Keywords match: "STUNNED" → `{{stunned|STUNNED}}`
6. Keywords match: "turns" → `{{...}}`
7. Result: Over-coloring

**Solution:** See recommendations below

### Issue 2: Over-Aggressive Keyword Lists
**Location:** `GameData/KeywordColorGroups.json`

**Problem:**
- Too many common words are keywords
- Example: "hit", "attack", "damage", "turn", "level"
- These appear in BOTH narrative AND technical contexts

**Current Keywords in "damage" Group:**
```json
"damage", "hit", "strike", "attack", "slash", "pierce", 
"crush", "wound", "injure", "harm", "hurt"
```

**Where This Causes Issues:**
- Narrative: "You hit the goblin" ✅ Should color "hit"
- Technical: "attack 15 - 5 armor" ❌ Should NOT color "attack"
- Stats: "damage dealt: 234" ❌ Should NOT color "damage"

### Issue 3: Entity Names as Keywords
**Location:** "enemy" keyword group

**Problem:**
```json
"keywords": [
  "goblin", "orc", "skeleton", "zombie", "dragon", "demon", 
  "wraith", "spider", "bat", "slime", ...
]
```

**Issue:**
- Matches enemy names in ALL contexts
- Even in places where we have entity-specific coloring
- May conflict with formatted entity names

---

## Streamlining Recommendations

### Option A: Selective Keyword Application (Recommended)
**Goal:** Only apply keywords to specific text types

**Implementation:**
```csharp
// BlockDisplayManager.cs
private static string ApplyKeywordColoring(string text, bool enable = true)
{
    if (!enable) return text;
    
    if (ColorParser.HasColorMarkup(text))
    {
        return text;
    }
    return KeywordColorSystem.Colorize(text);
}

// Then specify where to use it:
UIManager.WriteLine(ApplyKeywordColoring(actionText, enable: true), ...);
UIManager.WriteLine($"    {rollInfo}", ...); // No coloring
UIManager.WriteLine($"    {ApplyKeywordColoring(effect, enable: false)}", ...); // No coloring
```

**Pros:**
- Fine-grained control
- Easy to disable for specific cases
- Backward compatible

**Cons:**
- Need to update each call site
- More manual management

### Option B: Reduce Keyword Lists (Recommended)
**Goal:** Only color truly distinctive keywords

**Changes to `KeywordColorGroups.json`:**

1. **Remove common words from "damage" group:**
   ```json
   // Before:
   "keywords": ["damage", "hit", "strike", "attack", "slash", "pierce", ...]
   
   // After:
   "keywords": ["slash", "pierce", "crush", "wound", "injure"]
   // Remove: "damage", "hit", "strike", "attack" (too common)
   ```

2. **Remove enemy names:**
   ```json
   // Remove entire "enemy" group
   // Reason: Enemy names should be colored explicitly in code, not by keywords
   ```

3. **Keep only distinctive terms:**
   - ✅ Keep: "CRITICAL", "STUNNED", "BLEEDING" (distinctive game terms)
   - ❌ Remove: "hit", "attack", "turn", "level" (too generic)

**Pros:**
- Less aggressive coloring
- Fewer conflicts
- Better performance

**Cons:**
- Less automatic coloring
- Need to add explicit colors in more places

### Option C: Context-Aware Keyword Groups
**Goal:** Different keywords for different contexts

**Implementation:**
```csharp
// Add context parameter to keyword system
public static string ColorizeForContext(string text, string context)
{
    switch (context)
    {
        case "narrative":
            // Full keyword set
            return ApplyKeywordColors(text);
        
        case "combat":
            // Only action and effect keywords
            return ApplyKeywordColors(text, "action", "critical", "fire", "ice", ...);
        
        case "menu":
            // Only UI keywords
            return ApplyKeywordColors(text, "rarity", "class");
        
        case "stats":
            // NO keywords
            return text;
        
        default:
            return text;
    }
}
```

**Pros:**
- Context-appropriate coloring
- Maximum control
- Clean separation

**Cons:**
- More complex system
- Need to define contexts
- More maintenance

### Option D: Whitelist Instead of Blacklist
**Goal:** Explicitly mark what SHOULD be colored

**Implementation:**
```csharp
// Add markup for keyword zones
"You {{keywords|hit the goblin for}} 6 damage"
// Only "hit" and "goblin" get keyword coloring

// Or use a special marker
"[KC]You hit the goblin[/KC] for 6 damage"
// Only text in [KC]...[/KC] gets keyword coloring
```

**Pros:**
- Explicit control
- No conflicts
- Predictable results

**Cons:**
- More markup in code
- More manual work
- Not backward compatible

---

## Recommended Solution: Hybrid Approach

### Phase 1: Quick Fixes (Immediate)

1. **Keep existing safeguards** ✅ Already in place
   - `HasColorMarkup()` check
   - No coloring on roll info

2. **Remove over-aggressive keywords**
   ```json
   // KeywordColorGroups.json
   "damage": {
     // Remove: "hit", "attack", "damage"
     "keywords": ["slash", "pierce", "crush", "wound", "injure"]
   }
   ```

3. **Remove enemy name keywords**
   ```json
   // Remove entire "enemy" group
   // Enemy names should use explicit coloring or templates
   ```

4. **Add color pattern validation**
   - Ensure all colorPattern values are valid templates or color codes
   - Fix any invalid patterns (e.g., "red", "cyan" are not templates)

### Phase 2: Selective Application (Near Term)

1. **Disable keyword coloring for specific contexts:**
   ```csharp
   // Status effects - usually have explicit colors
   if (ColorParser.HasColorMarkup(effect))
   {
       UIManager.WriteLine($"    {effect}", ...);
   }
   else
   {
       UIManager.WriteLine($"    {ApplyKeywordColoring(effect)}", ...);
   }
   ```

2. **Add context parameter to ApplyKeywordColoring:**
   ```csharp
   private static string ApplyKeywordColoring(string text, bool forceDisable = false)
   {
       if (forceDisable) return text;
       if (ColorParser.HasColorMarkup(text)) return text;
       return KeywordColorSystem.Colorize(text);
   }
   ```

### Phase 3: Long-Term Refinement (Future)

1. **Context-aware keyword sets**
2. **Performance optimization**
3. **User-configurable keyword colors**

---

## Testing Strategy

### Test Cases

1. **Combat Text:**
   ```
   Input: "Yorin hits Slime for 6 damage"
   Expected: "Yorin" (white), "hits" (keyword?), "Slime" (white), "6" (white), "damage" (white)
   ```

2. **Explicit Colors:**
   ```
   Input: "for &R6&y damage"
   Expected: "for " (white), "6" (red), " damage" (white)
   Should NOT: Add {{damage|damage}} wrapper
   ```

3. **Roll Info:**
   ```
   Input: "(roll: 12 | attack 15 - 5 armor | speed: 8.8s)"
   Expected: All white, no coloring
   ```

4. **Status Effects:**
   ```
   Input: "STUNNED for 2 turns"
   Expected: "STUNNED" (colored), "for 2 turns" (white)
   ```

5. **Templates:**
   ```
   Input: "{{fiery|Blazing Sword}}"
   Expected: Multi-color sequence
   Should NOT: Add more keyword wrapping
   ```

---

## Implementation Priority

### High Priority (Fix Now)
1. ✅ Remove "damage" keyword from "damage" group
2. ✅ Remove "attack" keyword from "damage" group
3. ✅ Remove enemy names from keyword groups
4. ✅ Validate all colorPattern values
5. ⚠️ Test combat text for spacing issues

### Medium Priority (Fix Soon)
1. Add context-aware coloring
2. Refine keyword lists
3. Add configuration for keyword enable/disable

### Low Priority (Future Enhancement)
1. User-configurable keywords
2. Performance optimization
3. Advanced context detection

---

## Files to Modify

| File | Changes | Priority |
|------|---------|----------|
| `GameData/KeywordColorGroups.json` | Remove over-aggressive keywords | High |
| `Code/UI/KeywordColorSystem.cs` | Add context awareness | Medium |
| `Code/UI/BlockDisplayManager.cs` | Selective keyword application | Medium |
| `Documentation` | Update best practices | Low |

---

## Summary

**The Problem:**
- Keyword coloring is too aggressive
- Conflicts with explicit colors
- Colors technical stats that should be plain
- Over-colors common words

**The Solution:**
1. **Immediate:** Reduce keyword lists, remove common words
2. **Near-term:** Add selective/context-aware coloring
3. **Long-term:** Make system configurable and context-aware

**Expected Result:**
- Cleaner, more predictable coloring
- No spacing issues from color conflicts
- Better visual hierarchy
- Improved performance

---

*Analysis Date: October 12, 2025*

