# Color Spacing Fix

## Issue
Colored text was appearing without proper spacing before it, causing text like:
- "Adevastating blow" instead of "A devastating blow"
- "for42 damage" instead of "for 42 damage"

## Root Causes

### 1. Template Sequence Shader (Fixed)
**File**: `Code/UI/ColorTemplate.cs`
**Problem**: The `ApplySequence` method was applying color codes to ALL characters including spaces, which caused spaces to be rendered incorrectly.

**Solution**: Modified `ApplySequence` to skip whitespace characters when applying colors:
```csharp
if (char.IsWhiteSpace(text[i]))
{
    // Add whitespace without coloring to preserve spacing
    segments.Add(new ColorDefinitions.ColoredSegment(text[i].ToString()));
}
else
{
    // Apply color sequence to non-whitespace characters
    // ...
}
```

### 2. Damage Display Formatting (Fixed)
**File**: `Code/Combat/CombatResults.cs`
**Problem**: Color codes were placed directly adjacent to interpolated values without proper spacing:
```csharp
// BEFORE (incorrect):
for &R{actualDamage}&y damage  // Creates "for &R42&y damage" - missing space before 42

// AFTER (correct):
for &R{actualDamage} &ydamage   // Creates "for &R42 &ydamage" - space preserved
```

**Solution**: Added a space before the second color code to preserve the space between the number and the word "damage".

### 3. Critical Miss Display (Fixed)
**File**: `Code/Combat/CombatResults.cs`
**Problem**: "CRITICAL MISS" was appearing without space after it:
```csharp
// BEFORE (incorrect):
{{{{miss|MISS}}}}&y on  // Creates "MISS&yon" - missing space

// AFTER (correct):
{{{{miss|MISS}}}} &yon  // Creates "MISS &yon" - space preserved
```

### 4. Status Effect Display (Fixed)
**File**: `Code/World/EnvironmentalActionHandler.cs`
**Problem**: Status effects like BLEEDING, WEAKENED, etc. had color codes directly adjacent:
```csharp
// BEFORE (incorrect):
&RBLEEDING&Y for  // Creates "BLEEDING&Yfor" - missing space after BLEEDING

// AFTER (correct):
&RBLEEDING &Yfor  // Creates "BLEEDING &Yfor" - space preserved
```

**Solution**: Added space before the following color code for all status effects (BLEEDING, WEAKENED, SLOWED, POISONED, STUNNED).

## Files Modified
1. `Code/UI/ColorTemplate.cs` - Fixed `ApplySequence` method to preserve whitespace
2. `Code/Combat/CombatResults.cs` - Fixed damage display formatting (4 occurrences)
3. `Code/Combat/CombatResults.cs` - Fixed "CRITICAL MISS" spacing (missing space after MISS)
4. `Code/World/EnvironmentalActionHandler.cs` - Fixed status effect spacing:
   - BLEEDING (missing space after)
   - WEAKENED (missing space after)
   - SLOWED (missing space after)
   - POISONED (missing space after)
   - STUNNED (missing space after)

## Testing
After these fixes, all colored text should maintain proper spacing:
- ✅ Narrative events with colored keywords
- ✅ Damage numbers with colored values
- ✅ Combat messages with multiple colored elements
- ✅ Template-colored text (fiery, critical, electric, etc.)

## Key Principle
**Always preserve whitespace when applying color codes**. Spaces should either:
1. Be left uncolored (preferred for sequence templates)
2. Have color codes placed AFTER them, not before

## Related Systems
- Color Template System (`Code/UI/ColorTemplate.cs`)
- Color Parser (`Code/UI/ColorParser.cs`)
- Keyword Color System (`Code/UI/KeywordColorSystem.cs`)
- Combat Results Formatting (`Code/Combat/CombatResults.cs`)

