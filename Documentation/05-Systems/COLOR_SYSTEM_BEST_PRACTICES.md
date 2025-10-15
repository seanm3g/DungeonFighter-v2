# Color System Best Practices
**Last Updated:** October 12, 2025

---

## Overview

DungeonFighter-v2's color system has three layers:
1. **Explicit Color Codes** - Manual control
2. **Color Templates** - Named color patterns
3. **Keyword Coloring** - Automatic word matching

This guide shows you how to use each layer effectively and avoid common pitfalls.

---

## Quick Reference

### When to Use Each Method

| Use Case | Method | Example |
|----------|--------|---------|
| Damage numbers | Explicit codes | `"for &R15&y damage"` |
| Entity names | Templates | `"{{damage\|Goblin}}"` |
| Special effects | Templates | `"{{fiery\|Blazing}}"` |
| Narrative text | Keywords | Auto-colors "fire", "frozen", etc. |
| Stats/Roll info | Nothing | Keep plain white |

---

## Layer 1: Explicit Color Codes

### When to Use
- Specific numbers (damage, healing, etc.)
- Precise color control needed
- Performance-critical sections

### Syntax
```
&X = Foreground color
^X = Background color
&y = Reset to default grey
```

### Examples
```csharp
// Damage number
string text = $"for &R{damage}&y damage";
// Result: "for 15 damage" (15 is red)

// Healing number
string text = $"healed &G{heal}&y health";
// Result: "healed 25 health" (25 is green)

// Critical hit
string text = $"&YCRITICAL HIT&y for &R{damage}&y!";
// Result: "CRITICAL HIT for 50!" (CRITICAL is white, 50 is red)
```

### Color Code Reference
```
r = dark red       R = bright red
g = dark green     G = bright green
b = dark blue      B = bright blue
c = dark cyan      C = bright cyan
m = dark magenta   M = bright magenta
o = dark orange    O = bright orange
w = brown          W = yellow
k = very dark      K = dark grey
y = light grey     Y = white
```

### ✅ Do's
```csharp
// Good: Simple, direct coloring
string text = $"&R{damage}&y damage dealt";

// Good: Multiple colors
string text = $"&G{heal}&y health, &B{mana}&y mana restored";
```

### ❌ Don'ts
```csharp
// Bad: Don't color common words with codes
string text = $"&Rdamage&y dealt"; // Use keywords instead

// Bad: Don't nest codes unnecessarily
string text = $"&R&R{damage}&y&y"; // Redundant codes
```

---

## Layer 2: Color Templates

### When to Use
- Named entities (items, enemies, locations)
- Multi-color effects (fiery, icy, etc.)
- Consistent themed coloring

### Syntax
```
{{template|text}}
```

### Available Templates
```
Elemental:    fiery, icy, toxic, electric
Magical:      crystalline, holy, demonic, arcane, ethereal
Nature:       natural, shadow, golden
Status:       bloodied, corrupted, poisoned, stunned, burning, frozen, bleeding
Combat:       critical, damage, heal, mana, miss
Rarity:       common, uncommon, rare, epic, legendary
```

### Examples
```csharp
// Item name
string item = "{{legendary|Sword of Destiny}}";
// Result: Orange multi-color sequence

// Enemy with effect
string enemy = "{{fiery|Burning Demon}}";
// Result: Fire effect (red-orange-yellow sequence)

// Location
string room = "{{icy|Frozen Chamber}}";
// Result: Ice effect (cyan-blue-white sequence)

// Mixed
string text = $"Found {{legendary|Flaming Sword}} in {{icy|Ice Cave}}!";
// Result: Both templates applied independently
```

### ✅ Do's
```csharp
// Good: Named entities
string text = $"{{damage|{enemy.Name}}} appears!";

// Good: Thematic descriptions
string text = $"The {{fiery|flames}} burn bright";

// Good: Multiple templates
string text = $"{{legendary|Epic}} {{holy|Divine Sword}}";
```

### ❌ Don'ts
```csharp
// Bad: Don't template common words
string text = "You {{damage|hit}} the enemy"; // Use keywords instead

// Bad: Don't nest templates
string text = "{{fiery|{{legendary|Sword}}}}"; // Doesn't work

// Bad: Don't mix with codes inside
string text = "{{fiery|&RFlames&y}}"; // Redundant
```

---

## Layer 3: Keyword Coloring

### When to Use
- Narrative text
- Combat descriptions
- Automatic coloring of game terms

### How It Works
- Automatically colors recognized keywords
- Only applies to text without explicit colors
- Matches whole words only

### Current Keywords (After Streamlining)
```
Combat:       slash, pierce, crush, wound, injure
Status:       critical, crit, stunned, burning, frozen, bleeding
Elements:     fire, flame, ice, frost, lightning, thunder, poison
Magic:        magic, spell, arcane, enchanted
Rarity:       common, uncommon, rare, epic, legendary
Actions:      jab, flurry, cleave, backstab (30+ abilities)
Character:    you, your, hero, champion
Environment:  chamber, room, dungeon, crypt, temple
Types:        boss, monster, creature, beast
```

### Examples
```csharp
// Narrative text - auto-colored
string text = "You enter the frozen chamber";
// Result: "frozen" → icy template, "chamber" → natural template

// Combat action
string text = "You slash the beast with fury";
// Result: "slash" → damage template, "beast" → damage template

// Status effect
string text = "Enemy is stunned and burning";
// Result: "stunned" → stunned template, "burning" → burning template
```

### ✅ Do's
```csharp
// Good: Let keywords handle descriptive text
string text = "The warrior slashes through shadows";
UIManager.WriteLine(KeywordColorSystem.Colorize(text));

// Good: Narrative descriptions
string text = "You discover a legendary artifact in the frozen crypt";
UIManager.WriteLine(KeywordColorSystem.Colorize(text));
```

### ❌ Don'ts
```csharp
// Bad: Don't use keywords for stats
string text = "(attack: 15 | armor: 5)";
UIManager.WriteLine(KeywordColorSystem.Colorize(text)); // Keep stats white!

// Bad: Don't re-color already colored text
string text = "for &R15&y damage";
UIManager.WriteLine(KeywordColorSystem.Colorize(text)); // Use HasColorMarkup check!
```

---

## Common Patterns

### Pattern 1: Combat Damage Message
```csharp
// Explicit colors for numbers, keywords for narrative
string text = $"{attacker.Name} hits {target.Name} for &R{damage}&y";
// "hits" will be auto-colored by keywords
// damage number is explicitly red
UIManager.WriteLine(ApplyKeywordColoring(text), UIMessageType.Combat);
```

### Pattern 2: Roll Information
```csharp
// NO COLORING - keep stats plain white
string rollInfo = $"(roll: {roll} | attack {attack} - {armor} armor | speed: {speed}s)";
UIManager.WriteLine($"    {rollInfo}", UIMessageType.RollInfo);
// Do NOT apply keywords to technical stats!
```

### Pattern 3: Status Effects
```csharp
// Keywords handle status terms
string effect = $"{entity.Name} is stunned for {turns} turns";
UIManager.WriteLine(ApplyKeywordColoring(effect), UIMessageType.EffectMessage);
// "stunned" will be auto-colored
```

### Pattern 4: Item Display
```csharp
// Templates for item names, explicit codes for stats
string text = $"{{legendary|{item.Name}}} - Attack: &R{attack}&y, Defense: &B{defense}&y";
UIManager.WriteLine(text);
```

### Pattern 5: Narrative Text
```csharp
// Full keyword coloring for immersive descriptions
string text = "You enter a frozen chamber. The ice crystals reflect the holy light.";
UIManager.WriteLine(KeywordColorSystem.Colorize(text));
// "frozen", "ice", "holy", "light" all auto-colored
```

---

## The Safeguard Pattern

### Always Check for Existing Colors
```csharp
private static string ApplyKeywordColoring(string text)
{
    // Skip keyword coloring if text already has explicit color codes
    if (ColorParser.HasColorMarkup(text))
    {
        return text;
    }
    return KeywordColorSystem.Colorize(text);
}
```

### Why This Matters
```csharp
// Without check:
string text = "for &R15&y damage";
string colored = KeywordColorSystem.Colorize(text);
// Result: "for &R15&y {{damage|damage}}" ← DOUBLE COLORING! ❌

// With check:
string text = "for &R15&y damage";
string colored = ApplyKeywordColoring(text);
// Result: "for &R15&y damage" ← Clean! ✅
```

---

## Dos and Don'ts Summary

### ✅ DO
1. **Use explicit codes for numbers**
   ```csharp
   $"for &R{damage}&y damage"
   ```

2. **Use templates for named entities**
   ```csharp
   $"{{legendary|{itemName}}}"
   ```

3. **Let keywords handle narrative text**
   ```csharp
   KeywordColorSystem.Colorize("You slash the burning beast")
   ```

4. **Check for existing colors before applying keywords**
   ```csharp
   if (ColorParser.HasColorMarkup(text)) return text;
   ```

5. **Keep technical stats plain**
   ```csharp
   UIManager.WriteLine($"    {rollInfo}", ...); // No coloring
   ```

### ❌ DON'T
1. **Don't color stats with keywords**
   ```csharp
   // Bad: ApplyKeywordColoring("(attack: 15 | armor: 5)")
   ```

2. **Don't rely on keywords for entity names**
   ```csharp
   // Bad: "You hit the goblin" (goblin no longer auto-colored)
   // Good: "You hit the {{damage|goblin}}"
   ```

3. **Don't mix all three layers on the same word**
   ```csharp
   // Bad: "{{fiery|&RFlame&y}}"
   // Good: "{{fiery|Flame}}" OR "&RFlame&y"
   ```

4. **Don't color common words with explicit codes**
   ```csharp
   // Bad: "&Rdamage&y dealt"
   // Good: "{{damage|damage}} dealt" or rely on keywords
   ```

5. **Don't add keywords to roll info or stats**
   ```csharp
   // Bad: ApplyKeywordColoring(rollInfo)
   // Good: Use rollInfo directly
   ```

---

## Performance Tips

### Minimize Keyword Operations
```csharp
// Good: Single color check
if (ColorParser.HasColorMarkup(text))
{
    UIManager.WriteLine(text);
}
else
{
    UIManager.WriteLine(KeywordColorSystem.Colorize(text));
}

// Bad: Multiple keyword passes
string text = KeywordColorSystem.Colorize(actionText);
text = KeywordColorSystem.Colorize(text); // Unnecessary second pass
```

### Cache Template Results
```csharp
// Good: Cache colored names
private string coloredName;
public string GetColoredName()
{
    if (coloredName == null)
    {
        coloredName = $"{{damage|{Name}}}";
    }
    return coloredName;
}
```

---

## Troubleshooting

### Problem: Spacing issues in text
**Cause:** Double coloring - keywords applied after explicit codes  
**Solution:** Use `ApplyKeywordColoring()` with `HasColorMarkup()` check

### Problem: Stats are colored
**Cause:** Keywords matching words like "attack", "armor"  
**Solution:** Don't apply `KeywordColorSystem.Colorize()` to stats

### Problem: Entity names not colored
**Cause:** Removed from keyword lists (as of Oct 12, 2025)  
**Solution:** Use explicit templates: `"{{damage|{enemy.Name}}}"`

### Problem: Word should be colored but isn't
**Cause:** May have been removed from keyword list  
**Solution:** Check `KeywordColorGroups.json` or add explicit color

---

## Examples by Context

### Combat System
```csharp
// Damage message
string action = $"{attacker.Name} hits {target.Name} for &R{damage}&y";
string rollInfo = $"(roll: {roll} | attack {attack} - {armor} armor)";
UIManager.WriteLine(ApplyKeywordColoring(action), UIMessageType.Combat);
UIManager.WriteLine($"    {rollInfo}", UIMessageType.RollInfo); // No keywords!

// Status effect
string effect = $"{entity.Name} is stunned";
UIManager.WriteLine(ApplyKeywordColoring(effect), UIMessageType.EffectMessage);
```

### Item System
```csharp
// Item with stats
string text = $"{{legendary|{item.Name}}}\n" +
              $"Attack: &R{item.Attack}&y\n" +
              $"Defense: &B{item.Defense}&y";
UIManager.WriteLine(text);
```

### Dungeon System
```csharp
// Room description
string desc = "You enter a frozen chamber. Ice crystals line the walls.";
UIManager.WriteLine(KeywordColorSystem.Colorize(desc)); // Auto-colors "frozen", "ice"
```

### Menu System
```csharp
// Menu with colored options
string title = "{{golden|SHOP}}";
string item1 = $"1. {{legendary|Mystic Sword}} - &W500&y gold";
string item2 = $"2. {{rare|Steel Armor}} - &W250&y gold";
UIManager.WriteLine(title);
UIManager.WriteLine(item1);
UIManager.WriteLine(item2);
```

---

## Summary

### Three-Layer Priority
1. **Explicit Codes** - Highest priority, always applied first
2. **Templates** - Medium priority, applied to marked text
3. **Keywords** - Lowest priority, fills in the gaps

### Golden Rules
1. Numbers get explicit codes (`&R15&y`)
2. Names get templates (`{{legendary|Sword}}`)
3. Narrative gets keywords (auto)
4. Stats get nothing (plain white)
5. Always check `HasColorMarkup()` before applying keywords

### When in Doubt
- **Important?** → Explicit codes
- **Named entity?** → Template
- **Descriptive text?** → Keywords
- **Technical data?** → Nothing

---

*Last Updated: October 12, 2025*  
*See also: COLOR_SYSTEM.md, COLOR_SYSTEM_STREAMLINING_ANALYSIS.md*

