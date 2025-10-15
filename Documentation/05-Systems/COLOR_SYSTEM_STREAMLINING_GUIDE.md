# Color System Streamlining Guide

**Quick Reference for Implementing Improvements**  
**Based on:** COLOR_COMBAT_GUI_INTEGRATION_ANALYSIS.md  
**Date:** October 12, 2025

---

## Visual Data Flow

### Current System (Before Optimization)

```
┌─────────────────────────────────────────────────────────────────────┐
│                        COMBAT EVENT OCCURS                          │
└────────────────────────────────┬────────────────────────────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │   CombatResults.cs      │
                    │  FormatDamageDisplay()  │
                    │ Adds &R codes manually  │
                    └────────────┬────────────┘
                                 │ "hits for &R50&y damage"
                    ┌────────────▼────────────┐
                    │ TextDisplayIntegration  │
                    │ ApplyKeywordColoring()  │
                    │  (skipped - has markup) │
                    └────────────┬────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │    ColorParser.Parse()  │
                    │  ExpandTemplates() ←────┼─── Regex Pass #1
                    │  ParseColorCodes() ←────┼─── Regex Pass #2
                    └────────────┬────────────┘
                                 │ List<ColoredSegment>
                    ┌────────────▼────────────┐
                    │ CanvasUIManager         │
                    │ WriteLineColored()      │
                    │ Iterates segments       │
                    └────────────┬────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │  GameCanvasControl      │
                    │  AddText() per segment  │
                    └─────────────────────────┘
                                 │
                              DISPLAY
```

**Total Steps:** 6  
**Regex Passes:** 2  
**Color Application Points:** 2 (CombatResults + Keyword System)

---

### Optimized System (After Phase 1 + 2)

```
┌─────────────────────────────────────────────────────────────────────┐
│                        COMBAT EVENT OCCURS                          │
└────────────────────────────────┬────────────────────────────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │   CombatResults.cs      │
                    │ Uses ColorThemeManager  │
                    │ Returns template markup │
                    └────────────┬────────────┘
                                 │ "hits for {{damage|50}} damage"
                    ┌────────────▼────────────┐
                    │ ColorParser.ParseSingle()│
                    │   Combined parse pass   │◄─── Single Regex Pass
                    │    (templates + codes)  │
                    └────────────┬────────────┘
                                 │ List<ColoredSegment> (cached)
                    ┌────────────▼────────────┐
                    │ CanvasUIManager         │
                    │ WriteLineColored()      │
                    │ Uses cached segments    │
                    └────────────┬────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │  GameCanvasControl      │
                    │  AddText() per segment  │
                    └─────────────────────────┘
                                 │
                              DISPLAY
```

**Total Steps:** 4  
**Regex Passes:** 1  
**Color Application Points:** 1 (ColorThemeManager)  
**Performance Gain:** ~40% faster, cleaner code

---

## Phase 1 Implementation: Quick Wins

### Task 1: Template Consolidation (2 hours)

**Goal:** Reduce from 100+ templates to 30 core templates

#### Step 1: Identify Core Templates (30 min)

Create a list of which templates are actually used in combat:

```bash
# Search for template usage in code
grep -r "{{" Code/ --include="*.cs" | grep -v "//" > template_usage.txt
```

#### Step 2: Update ColorTemplates.json (1 hour)

Keep only these 28 templates:

```json
{
  "templates": [
    // Elements (6)
    {"name": "fiery", ...},
    {"name": "icy", ...},
    {"name": "toxic", ...},
    {"name": "electric", ...},
    {"name": "shadow", ...},
    {"name": "holy", ...},
    
    // Rarities (5)
    {"name": "common", ...},
    {"name": "uncommon", ...},
    {"name": "rare", ...},
    {"name": "epic", ...},
    {"name": "legendary", ...},
    
    // Combat (5)
    {"name": "critical", ...},
    {"name": "damage", ...},
    {"name": "heal", ...},
    {"name": "miss", ...},
    {"name": "bleeding", ...},
    
    // Status (5)
    {"name": "poisoned", ...},
    {"name": "stunned", ...},
    {"name": "burning", ...},
    {"name": "frozen", ...},
    {"name": "weakened", ...},
    
    // Magic (4)
    {"name": "arcane", ...},
    {"name": "ethereal", ...},
    {"name": "corrupted", ...},
    {"name": "crystalline", ...},
    
    // Environment (2)
    {"name": "natural", ...},
    {"name": "golden", ...},
    
    // Special (1)
    {"name": "rainbow", ...}
  ]
}
```

#### Step 3: Update Code References (30 min)

Replace removed templates with rarity equivalents:

```csharp
// Before
string colored = ColorParser.Colorize(modifier, "sharp");  // specific template

// After
string colored = ColorParser.Colorize(modifier, GetRarityTemplate(item.Rarity));
```

**Testing:** Run game, check all text renders correctly.

---

### Task 2: Pre-Colored Battle Narratives (2 hours)

**Goal:** Add color markup to narrative messages

#### Step 1: Add Markup to FlavorText.json (1 hour)

```json
{
  "CombatNarratives": {
    "firstBlood": [
      "{{bloodied|The first drop of blood}} is drawn! The battle has {{critical|truly begun}}.",
      "First strike! {{damage|Blood flows}} from the initial wound."
    ],
    "criticalHit": [
      "{{critical|DEVASTATING BLOW!}} {name} strikes with {{fiery|overwhelming force}}!",
      "A {{electric|perfect hit}}! {name}'s attack {{damage|finds its mark}}!"
    ],
    "below50Percent": [
      "{name} {{weakened|staggers}} under the weight of their injuries!",
      "{{damage|Blood loss}} is taking its toll on {name}."
    ]
  }
}
```

#### Step 2: Update BattleNarrative.cs (30 min)

No code changes needed! The `GetRandomNarrative()` method already returns the text, which now includes markup.

#### Step 3: Test (30 min)

- Run battles
- Verify narrative text is colored correctly
- Check different narrative types (first blood, critical, defeat, etc.)

**Expected Result:** Narrative messages now have rich, thematic colors.

---

### Task 3: Markup Caching (2 hours)

**Goal:** Cache parsed ColoredSegment lists for repeated messages

#### Step 1: Add Cache to ColorParser.cs (1 hour)

```csharp
public static class ColorParser
{
    // Add cache dictionary
    private static Dictionary<string, List<ColorDefinitions.ColoredSegment>> _parseCache 
        = new Dictionary<string, List<ColorDefinitions.ColoredSegment>>();
    
    // Add cache size limit
    private const int MAX_CACHE_SIZE = 200;
    
    /// <summary>
    /// Parses text with color markup into colored segments (cached)
    /// </summary>
    public static List<ColorDefinitions.ColoredSegment> Parse(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new List<ColorDefinitions.ColoredSegment>();
        }
        
        // Check cache first
        if (_parseCache.TryGetValue(text, out var cached))
        {
            return cached;
        }
        
        // Parse if not cached
        text = ExpandTemplates(text);
        var segments = ParseColorCodes(text);
        
        // Add to cache (with size limit)
        if (_parseCache.Count < MAX_CACHE_SIZE)
        {
            _parseCache[text] = segments;
        }
        else
        {
            // Cache full - clear oldest entries (simple LRU alternative)
            _parseCache.Clear();
            _parseCache[text] = segments;
        }
        
        return segments;
    }
    
    /// <summary>
    /// Clears the parse cache (useful for testing or memory management)
    /// </summary>
    public static void ClearCache()
    {
        _parseCache.Clear();
    }
}
```

#### Step 2: Add Cache Clearing (30 min)

Clear cache at appropriate points:

```csharp
// In CombatManager.cs, after battle ends
public void EndCombat()
{
    // ... existing code ...
    
    // Clear color parse cache to free memory
    ColorParser.ClearCache();
}
```

#### Step 3: Test & Benchmark (30 min)

```csharp
// Add to TestManager.cs
public static void BenchmarkColorParsing()
{
    string testMessage = "You hit enemy for {{damage|50}} damage!";
    
    // Without cache
    ColorParser.ClearCache();
    var sw = System.Diagnostics.Stopwatch.StartNew();
    for (int i = 0; i < 1000; i++)
    {
        ColorParser.Parse(testMessage);
    }
    sw.Stop();
    Console.WriteLine($"Without cache: {sw.ElapsedMilliseconds}ms");
    
    // With cache
    ColorParser.ClearCache();
    sw.Restart();
    for (int i = 0; i < 1000; i++)
    {
        ColorParser.Parse(testMessage);
    }
    sw.Stop();
    Console.WriteLine($"With cache: {sw.ElapsedMilliseconds}ms");
    Console.WriteLine($"Speedup: {(sw.ElapsedMilliseconds > 0 ? (1000.0 / sw.ElapsedMilliseconds) : 0)}x faster");
}
```

**Expected Result:** 5-10x faster for repeated messages (typical in combat).

---

## Phase 2 Implementation: Structural Improvements

### Task 4: Centralized Color Theme Manager (6-8 hours)

**Goal:** Create single source of truth for all color decisions

#### Step 1: Create ColorThemeManager.cs (2 hours)

```csharp
using System;
using RPGGame.UI;

namespace RPGGame
{
    /// <summary>
    /// Centralized manager for all color theme decisions
    /// Provides consistent color application across the entire game
    /// </summary>
    public static class ColorThemeManager
    {
        // ═══ ENTITY COLORING ═══
        
        /// <summary>
        /// Colors an entity name based on type (player vs enemy)
        /// </summary>
        public static string ColorEntityName(Entity entity)
        {
            if (entity is Character)
                return ColorPlayerName(entity.Name);
            else if (entity is Enemy)
                return ColorEnemyName(entity.Name);
            else
                return entity.Name;
        }
        
        /// <summary>
        /// Colors player/hero name with golden theme
        /// </summary>
        public static string ColorPlayerName(string name)
        {
            return ColorParser.Colorize(name, "golden");
        }
        
        /// <summary>
        /// Colors enemy name with red color
        /// </summary>
        public static string ColorEnemyName(string name)
        {
            return $"&R{name}&y";
        }
        
        // ═══ ITEM COLORING ═══
        
        /// <summary>
        /// Colors an item name based on rarity
        /// </summary>
        public static string ColorItemName(Item item)
        {
            string template = item.Rarity switch
            {
                ItemRarity.Common => "common",
                ItemRarity.Uncommon => "uncommon",
                ItemRarity.Rare => "rare",
                ItemRarity.Epic => "epic",
                ItemRarity.Legendary => "legendary",
                _ => "common"
            };
            
            return ColorParser.Colorize(item.Name, template);
        }
        
        /// <summary>
        /// Colors a modifier based on rarity tier
        /// </summary>
        public static string ColorModifier(string modifier, ItemRarity rarity)
        {
            // Use same template as rarity
            string template = rarity switch
            {
                ItemRarity.Uncommon => "uncommon",
                ItemRarity.Rare => "rare",
                ItemRarity.Epic => "epic",
                ItemRarity.Legendary => "legendary",
                _ => "common"
            };
            
            return ColorParser.Colorize(modifier, template);
        }
        
        // ═══ ENVIRONMENT COLORING ═══
        
        /// <summary>
        /// Colors a dungeon name based on theme
        /// </summary>
        public static string ColorDungeonName(string name, string theme)
        {
            // Map theme to color template
            string template = theme.ToLower() switch
            {
                "forest" or "nature" => "natural",
                "lava" or "volcano" => "fiery",
                "ice" or "frozen" => "icy",
                "shadow" or "void" => "shadow",
                "crystal" or "cave" => "crystalline",
                "holy" or "temple" => "holy",
                "toxic" or "swamp" => "toxic",
                "electric" or "storm" => "electric",
                "arcane" or "magic" => "arcane",
                _ => "golden"  // Default
            };
            
            return ColorParser.Colorize(name, template);
        }
        
        /// <summary>
        /// Colors a room name (typically neutral)
        /// </summary>
        public static string ColorRoomName(string name, string theme)
        {
            // Rooms use subtle coloring
            return $"&Y{name}&y";  // White/bright
        }
        
        // ═══ COMBAT COLORING ═══
        
        /// <summary>
        /// Colors damage numbers
        /// </summary>
        public static string ColorDamage(int damage, bool isCritical = false)
        {
            if (isCritical)
                return ColorParser.Colorize(damage.ToString(), "critical");
            else
                return $"&R{damage}&y";  // Red
        }
        
        /// <summary>
        /// Colors an action name based on type
        /// </summary>
        public static string ColorAction(string actionName, ActionType type)
        {
            string template = type switch
            {
                ActionType.Attack => "damage",
                ActionType.Heal => "heal",
                ActionType.Buff => "holy",
                ActionType.Debuff => "corrupted",
                ActionType.Special => "electric",
                _ => "natural"
            };
            
            return ColorParser.Colorize(actionName, template);
        }
        
        /// <summary>
        /// Colors a status effect name
        /// </summary>
        public static string ColorStatusEffect(string effectName)
        {
            string template = effectName.ToLower() switch
            {
                "poison" or "poisoned" => "poisoned",
                "stun" or "stunned" => "stunned",
                "burn" or "burning" => "burning",
                "freeze" or "frozen" => "frozen",
                "bleed" or "bleeding" => "bleeding",
                "weaken" or "weakened" => "weakened",
                _ => "corrupted"  // Generic debuff
            };
            
            return ColorParser.Colorize(effectName, template);
        }
        
        // ═══ NARRATIVE COLORING ═══
        
        /// <summary>
        /// Colors narrative text based on type (for manual coloring)
        /// Note: Prefer adding markup directly to FlavorText.json instead
        /// </summary>
        public static string ColorNarrative(string text, NarrativeType type)
        {
            // In Phase 1, we added markup to FlavorText.json
            // This method is for edge cases where manual coloring is needed
            
            return type switch
            {
                NarrativeType.FirstBlood => $"{{{{bloodied|{text}}}}}",
                NarrativeType.Critical => $"{{{{critical|{text}}}}}",
                NarrativeType.Defeat => $"{{{{damage|{text}}}}}",
                NarrativeType.Victory => $"{{{{golden|{text}}}}}",
                NarrativeType.Taunt => $"{{{{electric|{text}}}}}",
                _ => text
            };
        }
    }
    
    /// <summary>
    /// Narrative types for color theming
    /// </summary>
    public enum NarrativeType
    {
        FirstBlood,
        Critical,
        Miss,
        Defeat,
        Victory,
        Taunt,
        Environmental
    }
    
    /// <summary>
    /// Action types for color theming
    /// </summary>
    public enum ActionType
    {
        Attack,
        Heal,
        Buff,
        Debuff,
        Special
    }
}
```

#### Step 2: Refactor CombatResults.cs (2 hours)

Replace inline color codes with ColorThemeManager:

```csharp
// Before
string damageText = $"{attacker.Name} hits {target.Name} with {actionName} for &R{actualDamage}&y damage";

// After
string playerName = ColorThemeManager.ColorEntityName(attacker);
string targetName = ColorThemeManager.ColorEntityName(target);
string coloredAction = ColorThemeManager.ColorAction(actionName, ActionType.Attack);
string coloredDamage = ColorThemeManager.ColorDamage(actualDamage, isCritical);

string damageText = $"{playerName} hits {targetName} with {coloredAction} for {coloredDamage} damage";
```

#### Step 3: Refactor ItemDisplayFormatter.cs (1 hour)

```csharp
// Before
public static string GetColoredItemName(Item item)
{
    string templateName = GetRarityTemplate(item.Rarity);
    return ColorParser.Colorize(item.Name, templateName);
}

// After
public static string GetColoredItemName(Item item)
{
    return ColorThemeManager.ColorItemName(item);
}
```

#### Step 4: Update All Color Calls (2-3 hours)

Search and replace all manual color calls:

```bash
# Find all manual color applications
grep -r "&R\|&G\|&B\|&Y" Code/ --include="*.cs" > manual_colors.txt
```

Update each to use `ColorThemeManager` instead.

#### Step 5: Test (1 hour)

- Play through combat
- Check item displays
- Verify dungeon/room names
- Ensure all colors match original

**Expected Result:** All color logic centralized, easier to modify themes globally.

---

### Task 5: Unified Color Strategy (4-6 hours)

**Goal:** Define and enforce clear rules for when to use each coloring method

#### Step 1: Document Strategy (1 hour)

Create `COLOR_STRATEGY.md`:

```markdown
# Color Application Strategy

## When to Use Each Method

### Method 1: ColorThemeManager (Primary)
**Use for:** Structured game entities and combat elements

- Entity names (player, enemies)
- Item names and modifiers
- Dungeon and room names
- Damage/heal numbers
- Action names
- Status effects

**Example:**
```csharp
string name = ColorThemeManager.ColorItemName(item);
```

### Method 2: Templates (Secondary)
**Use for:** Rich text with multiple colors

- Item descriptions with multiple elements
- Complex narrative text
- Environmental descriptions

**Example:**
```csharp
string text = "A {{fiery|blazing}} sword of {{golden|divine}} power";
```

### Method 3: Inline Codes (Rare)
**Use for:** Quick emphasis in temporary strings

- Debug messages
- Test output
- One-off special cases

**Example:**
```csharp
string debug = $"&RDEBUG:&y value = {x}";
```

### Method 4: Keyword System (Automatic)
**Use for:** Plain text that needs auto-coloring

- User input responses
- Generated descriptions without markup
- Legacy text without explicit colors

**Example:**
```csharp
// No explicit coloring needed
string text = "You hit the goblin for 25 damage";
// Keywords "hit", "goblin", "damage" auto-colored
```

## Priority Order

If multiple methods could apply, use this priority:

1. ColorThemeManager (if entity/combat element)
2. Templates (if rich multi-color text)
3. Keyword System (if plain text)
4. Inline Codes (only as last resort)
```

#### Step 2: Apply Strategy to CombatResults.cs (2 hours)

Refactor all methods to follow strategy:

```csharp
// FormatDamageDisplay() - use ColorThemeManager
public static string FormatDamageDisplay(Entity attacker, Entity target, int rawDamage, int actualDamage, Action? action = null)
{
    string attackerName = ColorThemeManager.ColorEntityName(attacker);
    string targetName = ColorThemeManager.ColorEntityName(target);
    string actionName = action != null ? ColorThemeManager.ColorAction(action.Name, ActionType.Attack) : "attack";
    string damage = ColorThemeManager.ColorDamage(actualDamage);
    
    return $"{attackerName} hits {targetName} with {actionName} for {damage} damage";
}
```

#### Step 3: Apply to ItemDisplayFormatter.cs (1 hour)

#### Step 4: Apply to DungeonManager and Environment (1 hour)

#### Step 5: Update Developer Documentation (1 hour)

Add to `CODE_PATTERNS.md`:

```markdown
## Color Application Pattern

Always use `ColorThemeManager` for game entities:

```csharp
// ✅ Correct
string name = ColorThemeManager.ColorEntityName(entity);
string item = ColorThemeManager.ColorItemName(myItem);
string damage = ColorThemeManager.ColorDamage(dmg, isCrit);

// ❌ Incorrect
string name = $"&R{entity.Name}&y";  // Don't use inline codes
string item = ColorParser.Colorize(myItem.Name, "rare");  // Use ColorThemeManager instead
```
```

**Expected Result:** Clear, consistent color strategy across entire codebase.

---

## Phase 3 Implementation: Performance Optimization

### Task 6: Single-Pass Parser (4-6 hours)

**Goal:** Combine template expansion and code parsing into one regex pass

#### Step 1: Design State Machine (1 hour)

```
State Machine for Single-Pass Parser:
───────────────────────────────────

STATE: TEXT
  - Default state
  - Accumulate characters
  - If '{{': → STATE: TEMPLATE_START
  - If '&' or '^': → STATE: COLOR_CODE
  - If end: emit segment

STATE: TEMPLATE_START
  - Read until '|'
  - Store template name
  - → STATE: TEMPLATE_CONTENT

STATE: TEMPLATE_CONTENT
  - Read until '}}'
  - Apply template to content
  - Emit colored segments
  - → STATE: TEXT

STATE: COLOR_CODE
  - Read next char
  - Apply color to subsequent text
  - → STATE: TEXT
```

#### Step 2: Implement ParseSingle() (3-4 hours)

```csharp
/// <summary>
/// Single-pass parser for color markup (faster than two-pass)
/// </summary>
public static List<ColorDefinitions.ColoredSegment> ParseSingle(string text)
{
    var segments = new List<ColorDefinitions.ColoredSegment>();
    var currentText = new StringBuilder();
    ColorDefinitions.ColorRGB? currentForeground = null;
    ColorDefinitions.ColorRGB? currentBackground = null;
    
    int i = 0;
    while (i < text.Length)
    {
        // Check for template start {{
        if (i < text.Length - 1 && text[i] == '{' && text[i + 1] == '{')
        {
            // Emit accumulated text
            if (currentText.Length > 0)
            {
                segments.Add(new ColorDefinitions.ColoredSegment(
                    currentText.ToString(), currentForeground, currentBackground));
                currentText.Clear();
            }
            
            // Parse template
            i += 2; // Skip {{
            int templateEnd = text.IndexOf('|', i);
            int contentEnd = text.IndexOf("}}", i);
            
            if (templateEnd > 0 && contentEnd > templateEnd)
            {
                string templateName = text.Substring(i, templateEnd - i);
                string content = text.Substring(templateEnd + 1, contentEnd - templateEnd - 1);
                
                // Apply template
                var template = ColorTemplateLibrary.GetTemplate(templateName);
                if (template != null)
                {
                    var templateSegments = template.Apply(content);
                    segments.AddRange(templateSegments);
                }
                else
                {
                    // Unknown template, add as plain text
                    segments.Add(new ColorDefinitions.ColoredSegment(content));
                }
                
                i = contentEnd + 2; // Skip }}
                continue;
            }
        }
        
        // Check for color code & or ^
        else if (text[i] == '&' || text[i] == '^')
        {
            bool isForeground = text[i] == '&';
            
            // Emit accumulated text
            if (currentText.Length > 0)
            {
                segments.Add(new ColorDefinitions.ColoredSegment(
                    currentText.ToString(), currentForeground, currentBackground));
                currentText.Clear();
            }
            
            // Read color code
            if (i + 1 < text.Length)
            {
                char code = text[i + 1];
                var color = ColorDefinitions.GetColor(code);
                
                if (color.HasValue)
                {
                    if (isForeground)
                        currentForeground = color;
                    else
                        currentBackground = color;
                }
                
                i += 2; // Skip & or ^ and code
                continue;
            }
        }
        
        // Regular character
        currentText.Append(text[i]);
        i++;
    }
    
    // Emit remaining text
    if (currentText.Length > 0)
    {
        segments.Add(new ColorDefinitions.ColoredSegment(
            currentText.ToString(), currentForeground, currentBackground));
    }
    
    return segments;
}
```

#### Step 3: Replace Old Parse() (30 min)

```csharp
public static List<ColorDefinitions.ColoredSegment> Parse(string text)
{
    // Use cache
    if (_parseCache.TryGetValue(text, out var cached))
        return cached;
    
    // Use single-pass parser
    var segments = ParseSingle(text);
    
    // Cache result
    if (_parseCache.Count < MAX_CACHE_SIZE)
        _parseCache[text] = segments;
    
    return segments;
}
```

#### Step 4: Test Extensively (1-2 hours)

```csharp
public static void TestColorParsing()
{
    // Test cases
    var tests = new[]
    {
        ("Plain text", "Plain text"),
        ("&RRed text&y", "Red text"),
        ("{{fiery|Fire}}", "Fire"),
        ("Mix &R{{critical|CRIT}}&y text", "Mix CRIT text"),
        ("{{legendary|Epic {{fiery|Fire}} Sword}}", "Epic Fire Sword")
    };
    
    foreach (var (input, expected) in tests)
    {
        var segments = ColorParser.Parse(input);
        string output = string.Join("", segments.Select(s => s.Text));
        Console.WriteLine($"{(output == expected ? "✓" : "✗")} {input}");
    }
}
```

**Expected Result:** Same output as before, but ~40% faster.

---

## Testing Checklist

After implementing any phase, verify:

### Visual Tests
- [ ] Combat damage displays correctly
- [ ] Item names show proper rarity colors
- [ ] Enemy names are red
- [ ] Player name is gold
- [ ] Status effects are colored
- [ ] Battle narratives have rich colors
- [ ] Dungeon names match themes

### Performance Tests
- [ ] No noticeable lag when displaying text
- [ ] Combat messages appear instantly
- [ ] Menu rendering is smooth

### Regression Tests
- [ ] All existing tests pass
- [ ] No broken color markup
- [ ] Keyword system still works
- [ ] Templates expand correctly

### Code Quality Tests
- [ ] No linter errors
- [ ] Code follows patterns in `CODE_PATTERNS.md`
- [ ] Documentation updated
- [ ] Comments added for complex logic

---

## Rollback Plan

If issues arise, rollback steps:

### Phase 1 Rollback
1. Restore `ColorTemplates.json` from backup
2. Revert `FlavorText.json` changes
3. Remove cache from `ColorParser.cs`

### Phase 2 Rollback
1. Restore `CombatResults.cs` from backup
2. Delete `ColorThemeManager.cs`
3. Revert all files that use ColorThemeManager

### Phase 3 Rollback
1. Restore `ColorParser.cs` from backup
2. Remove `ParseSingle()` method

**Always backup files before starting each phase.**

---

## Success Metrics

### Phase 1 Goals
- ✅ Template count reduced to ~30
- ✅ Narrative text has colors
- ✅ 5-10x speedup for repeated messages

### Phase 2 Goals
- ✅ All color logic uses ColorThemeManager
- ✅ Clear strategy documented
- ✅ Easier to maintain

### Phase 3 Goals
- ✅ Single-pass parser works correctly
- ✅ 40% faster parsing
- ✅ All tests pass

---

## Quick Commands

```bash
# Backup before starting
cp GameData/ColorTemplates.json GameData/ColorTemplates.json.backup
cp Code/UI/ColorParser.cs Code/UI/ColorParser.cs.backup

# Search for manual color codes
grep -r "&R\|&G\|&B" Code/ --include="*.cs"

# Search for template usage
grep -r "{{" Code/ --include="*.cs"

# Run color tests
# (Add to TestManager.cs and call from main menu)

# Restore from backup
cp GameData/ColorTemplates.json.backup GameData/ColorTemplates.json
```

---

**Ready to start? Begin with Phase 1, Task 1: Template Consolidation**

