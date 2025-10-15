# Color System Quick Reference
**Date:** October 12, 2025  
**Purpose:** Quick lookup for color system information

---

## Current System (What We Have Now)

### Color Codes
```csharp
&R  = Bright red
&r  = Dark red
&G  = Bright green
&g  = Dark green
&B  = Bright blue
&b  = Dark blue
&Y  = Bright yellow/white
&y  = Yellow (reset to default)
&W  = Gold/warm white
&O  = Orange
&C  = Cyan
&M  = Magenta
&k  = Black
```

### Usage (Current)
```csharp
// Inline color codes:
string text = "&RDanger&y ahead!";

// Templates:
string text = "{{fiery|Blazing Sword}}";

// Keywords (automatic):
string text = "attack hits for damage";
// Becomes: "{{damage|attack}} {{damage|hits}} for {{damage|damage}}"
```

### Problems (Current)
- ❌ Color codes embedded in strings (hard to read)
- ❌ Spacing issues from code insertion
- ❌ Hard to modify (find all code locations)
- ❌ Automatic keyword coloring causes surprises
- ❌ Multi-phase processing (slow, error-prone)

---

## Proposed System (What We're Moving To)

### Color Names
```csharp
Colors.Red          = Bright red (215, 66, 0)
Colors.DarkRed      = Dark red (166, 74, 46)
Colors.Green        = Bright green (0, 196, 32)
Colors.DarkGreen    = Dark green (0, 148, 3)
Colors.Blue         = Bright blue (0, 150, 255)
Colors.DarkBlue     = Dark blue (0, 72, 189)
Colors.White        = White (255, 255, 255)
Colors.Yellow       = Yellow (255, 255, 0)
Colors.Gold         = Gold (255, 215, 0)
Colors.Orange       = Orange (255, 165, 0)
Colors.Black        = Black (15, 59, 58)

// Semantic colors:
Colors.Damage       = DarkRed
Colors.Heal         = Green
Colors.Mana         = Blue
```

### Usage (Proposed)
```csharp
// Structured approach:
var text = new ColoredText()
    .Red("Danger")
    .Plain(" ahead!");

// Templates:
var text = ColorTemplates.Fiery("Blazing Sword");

// Keywords (explicit):
var text = ColoredText.WithKeywords("attack hits for damage");
// Only colors keywords when you explicitly request it
```

### Benefits (Proposed)
- ✅ Readable code (can see what text says)
- ✅ No spacing issues (no embedded codes)
- ✅ Easy to modify (change colors by name)
- ✅ Explicit coloring (no surprises)
- ✅ Single-pass rendering (fast, reliable)

---

## Side-by-Side Comparison

### Combat Message

**Current:**
```csharp
string msg = $"{name} &rhits&y {target} for &R{dmg}&y &rdamage&y";
// Cryptic, hard to read
```

**Proposed:**
```csharp
var msg = new ColoredText()
    .Plain(name)
    .DarkRed(" hits ")
    .Plain(target)
    .Plain(" for ")
    .Red(dmg.ToString())
    .DarkRed(" damage");
// Clear, readable
```

### Stats Display

**Current:**
```csharp
string hp = $"HP: &R{current}&y/{max}";
// Embedded codes
```

**Proposed:**
```csharp
var hp = new ColoredText()
    .Plain("HP: ")
    .Red(current.ToString())
    .Plain("/")
    .Plain(max.ToString());
// Structured data
```

### Title Frame

**Current:**
```csharp
frameList.Add($"&k                                {line}");
// Hard to count spaces
```

**Proposed:**
```csharp
var frame = new ColoredText()
    .Add(new string(' ', 32), Colors.Black)
    .Add(line, Colors.Red);
// Easy to modify
```

---

## When to Use Each Approach

### Use Current System (For Now)
- ✅ Existing code that works
- ✅ Data files (JSON configs)
- ✅ Quick prototypes

### Use Proposed System (After Migration)
- ✅ New combat messages
- ✅ New UI displays
- ✅ Title/animation systems
- ✅ Anything needing precise spacing

---

## Common Tasks

### Change a Color

**Current:**
1. Find all instances of `&R` in codebase
2. Replace with `&O` (or whatever)
3. Hope you didn't break anything
4. Test and debug spacing issues

**Proposed:**
1. Change `Colors.Red` to `Colors.Orange`
2. Done! (Or change specific uses by name)

### Add New Color

**Current:**
1. Add to ColorDefinitions.cs
2. Add code mapping
3. Update ColorParser
4. Test with all color code combinations

**Proposed:**
1. Add to Colors class: `public static readonly ColorRGB Purple = new(128, 0, 128);`
2. Done! Auto-available everywhere

### Fix Spacing Issue

**Current:**
1. Find where color codes are added
2. Try to figure out why spaces appear
3. Adjust codes/spacing
4. Test
5. Repeat until fixed
6. Hope it doesn't break in other contexts

**Proposed:**
1. Check ColoredText structure
2. Adjust text segments (not codes)
3. Test
4. Done! (No codes = no spacing issues)

### Help AI Understand

**Current:**
"The color system uses embedded codes like `&R` which are parsed through templates and keyword systems, causing multi-phase transformations that can affect spacing..."
(AI confused)

**Proposed:**
"Use `ColoredText` to build text with explicit colors. Example: `.Red(text)`"
(AI understands immediately)

---

## Migration Status

### Phase 0: Preparation ✅
- Analysis complete
- Proposal written
- Roadmap defined

### Phase 1: Core Infrastructure ⏳
- ColoredText class
- Colors static class
- Backward compatibility
- Unit tests

### Phase 2: Renderer Updates ⏳
- IColoredTextRenderer
- Canvas renderer
- Console renderer
- UIManager updates

### Phase 3: High-Impact ⏳
- Combat messages
- Title screen
- Roll info

### Phase 4-8: Complete Migration ⏳
- Templates
- Keywords
- Menus
- Deprecation
- Cleanup

---

## Key Concepts

### Separation of Concerns
**Bad:** Content mixed with presentation
```csharp
string text = "&RDanger&y ahead!";  // What's the actual text?
```

**Good:** Content separate from presentation
```csharp
var text = new ColoredText()
    .Red("Danger")
    .Plain(" ahead!");  // Text is clear
```

### Single Responsibility
**Bad:** String does multiple things
```csharp
// This string is both content AND color codes
string text = "&R H&R I";
```

**Good:** Classes have single purpose
```csharp
// ColoredText manages structure
// Renderer handles display
var text = new ColoredText().Red("HI");
```

### Explicit vs Implicit
**Bad:** Automatic, uncontrolled coloring
```csharp
// Keywords automatically colored - surprise!
string text = "attack hits";
```

**Good:** Explicit, controlled coloring
```csharp
// Only colored if you request it
var text = ColoredText.WithKeywords("attack hits");
// Or no coloring:
var text = ColoredText.Plain("attack hits");
```

---

## Troubleshooting

### "I'm seeing extra spaces in colored text"
**Current System:** Color codes like `&R` can cause spacing issues. Check if keyword coloring is being applied unintentionally.

**Proposed System:** Won't happen - no embedded codes to cause spacing.

### "I can't figure out what the actual text says"
**Current System:** You have to mentally strip all `&X` codes. Try using ColorParser.StripColorMarkup().

**Proposed System:** Just read the `.Plain()` and `.Red()` calls - that's the actual text.

### "Changing a color broke my layout"
**Current System:** Color codes can affect spacing and layout. You may need to adjust surrounding spaces.

**Proposed System:** Colors don't affect layout - change colors freely.

### "AI changed the wrong color"
**Current System:** Cryptic codes (`&R`, `&r`, `&y`) are hard for AI to distinguish.

**Proposed System:** Named colors (`Colors.Red`, `Colors.DarkRed`) are clear and unambiguous.

---

## Best Practices

### Current System (Until Migration)

1. **Document color codes** - Add comments explaining what `&R` means
2. **Avoid nested codes** - Minimize `&R&O&W` complexity
3. **Test spacing** - Always check for extra spaces
4. **Explicit coloring** - Don't rely on automatic keyword coloring for important text
5. **Use ColorParser.StripColorMarkup()** - To get plain text

### Proposed System (After Migration)

1. **Use semantic colors** - `Colors.Damage` instead of `Colors.DarkRed` for meaning
2. **Build incrementally** - Add segments one at a time
3. **Keep it flat** - Don't nest ColoredText objects deeply
4. **Test structure** - Use `.GetPlainText()` to verify content
5. **Name instances** - Use descriptive variable names

---

## Resources

### Documentation
- [Color System Redesign Proposal](../02-Development/COLOR_SYSTEM_REDESIGN_PROPOSAL.md)
- [Color System Problems - Examples](../02-Development/COLOR_SYSTEM_PROBLEMS_EXAMPLES.md)
- [Color System Migration Roadmap](../02-Development/COLOR_SYSTEM_MIGRATION_ROADMAP.md)
- [Color System Analysis Summary](../../COLOR_SYSTEM_ANALYSIS_SUMMARY.md)

### Code Files (Current)
- `Code/UI/ColorParser.cs` - Parses color markup
- `Code/UI/ColorDefinitions.cs` - Color code mappings
- `Code/UI/ColorTemplate.cs` - Template system
- `Code/UI/KeywordColorSystem.cs` - Automatic keyword coloring

### Code Files (Proposed)
- `Code/UI/ColoredText.cs` - Structured color text (to be created)
- `Code/UI/Colors.cs` - Named color constants (to be created)
- `Code/UI/ColorTemplates.cs` - Template applicators (to be created)

---

## FAQ

### Q: Why change the system?
**A:** Current system has fundamental issues with spacing, readability, and reliability.

### Q: Will my existing code break?
**A:** No - we maintain backward compatibility during migration.

### Q: How long will migration take?
**A:** 40-45 hours total, can be done incrementally over 3-4 weeks.

### Q: What if I need to use old color codes?
**A:** `ColoredText.FromMarkup()` converts old format to new.

### Q: Can I still use templates like {{fiery|text}}?
**A:** Yes, but they'll use the new system: `ColorTemplates.Fiery("text")`

### Q: What about mod support?
**A:** Mods can use either system - we auto-convert old format.

---

**Last Updated:** October 12, 2025  
**Status:** Current system documented, proposed system designed, migration planned  
**Next:** Awaiting approval to proceed with Phase 1

