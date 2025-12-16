# Color Template Usage Guide

## Quick Start

### Template Syntax
```
{{template_name|Your Text Here}}
```

## Common Use Cases

### 1. Item Display

#### Basic Item
```csharp
string itemName = "{{sturdy|Sturdy}} Iron Sword";
```
**Result**: Grey "Sturdy" prefix

#### Uncommon Item
```csharp
string itemName = "{{sharp|Sharp}} Steel Blade {{protection|of Protection}}";
```
**Result**: Grey/white shimmer "Sharp", grey "of Protection"

#### Rare Item
```csharp
string itemName = "{{keen|Keen}} Mithril Sword {{precision|of Precision}}";
```
**Result**: White/cyan shimmer for both

#### Epic Item
```csharp
string itemName = "{{epic|Epic}} {{brutal|Brutal}} Greatsword {{destruction|of Destruction}}";
```
**Result**: Purple rarity, red/white brutal, red/white destruction

#### Legendary Item
```csharp
string itemName = "{{legendary|Legendary}} {{masterwork|Masterwork}} Blade {{immortality|of Immortality}}";
```
**Result**: Gold shimmer legendary, orange/gold/white masterwork, gold/white/magenta immortality

#### Transcendent Item
```csharp
string itemName = "{{transcendent|TRANSCENDENT}} {{cosmic|Cosmic}} Greatblade {{gods|of the Gods}}";
```
**Result**: Full prismatic effect throughout

### 2. Environment Descriptions

#### Forest Area
```csharp
string description = "You enter the {{forest|Ancient Forest}}. " +
                    "The {{nature|natural}} canopy blocks most sunlight.";
```
**Result**: Green/brown "Ancient Forest", green/white "natural"

#### Lava Zone
```csharp
string description = "You descend into the {{lava|Lava Caves}}. " +
                    "The heat from the {{volcano|volcanic}} rock is intense.";
```
**Result**: Red/orange flickering for both

#### Crystal Caverns
```csharp
string description = "The {{crystal|Crystal Caverns}} shimmer with " +
                    "{{arcane|arcane}} energy. Beautiful but dangerous.";
```
**Result**: Magenta/cyan "Crystal Caverns", magenta/blue "arcane"

#### Shadow Realm
```csharp
string description = "You step into the {{shadow|Shadow Realm}}. " +
                    "The {{void|void}} surrounds you.";
```
**Result**: Dark grey/magenta "Shadow Realm", dark/magenta "void"

#### Temporal Zone
```csharp
string description = "Time distorts in the {{temporal|Time Distortion}}. " +
                    "The {{dimensional|dimensional}} barriers are weak here.";
```
**Result**: Cyan/magenta/white for both

### 3. Status Effects

#### Single Effect
```csharp
string message = "Enemy is {{poisoned|POISONED}}!";
```
**Result**: Green pulsing "POISONED"

#### Multiple Effects
```csharp
string message = "Enemy is {{poisoned|POISONED}}, {{weakened|WEAKENED}}, and {{slowed|SLOWED}}!";
```
**Result**: Green, dark grey, and blue/cyan pulsing

#### Damage Over Time
```csharp
string message = "Enemy takes {{damage|5}} damage from {{burning|BURNING}}!";
```
**Result**: Red "5", red/orange "BURNING"

#### Freeze Effect
```csharp
string message = "Enemy is {{frozen|FROZEN}} solid! They can't move!";
```
**Result**: Cyan/blue/white "FROZEN"

#### Bleed Effect
```csharp
string message = "Enemy is {{bleeding|BLEEDING}} heavily!";
```
**Result**: Dark red pulsing "BLEEDING"

### 4. Combat Messages

#### Critical Hit
```csharp
string message = "{{critical|CRITICAL HIT}}! You deal {{damage|" + damageAmount + "}} damage!";
```
**Result**: Red/orange/white "CRITICAL HIT", red damage number

#### Healing
```csharp
string message = "You heal for {{heal|" + healAmount + "}} health!";
```
**Result**: Green heal number

#### Mana Usage
```csharp
string message = "You spend {{mana|" + manaCost + "}} mana!";
```
**Result**: Blue mana number

### 5. Item Generation

#### Building Item Names
```csharp
public string BuildItemName(Item item)
{
    string name = "";
    
    // Add modifier prefix
    if (item.HasModifier)
    {
        string modifierTemplate = GetModifierTemplate(item.Modifier);
        name += "{{" + modifierTemplate + "|" + item.Modifier + "}} ";
    }
    
    // Add rarity
    string rarityTemplate = GetRarityTemplate(item.Rarity);
    name += "{{" + rarityTemplate + "|" + item.Rarity + "}} ";
    
    // Add base name
    name += item.BaseName;
    
    // Add stat bonus suffix
    if (item.HasBonus)
    {
        string bonusTemplate = GetBonusTemplate(item.Bonus);
        name += " {{" + bonusTemplate + "|" + item.Bonus + "}}";
    }
    
    return name;
}
```

#### Modifier Template Mapping
```csharp
private string GetModifierTemplate(string modifier)
{
    return modifier.ToLower().Replace(" ", "");
    // "Worn" → "worn"
    // "Sharp" → "sharp"
    // "Keen" → "keen"
    // "Reality Breaker" → "realitybreaker"
    // "Time Warp" → "timewarp"
}
```

#### Stat Bonus Template Mapping
```csharp
private string GetBonusTemplate(string bonus)
{
    // Remove "of" and "the" and convert to lowercase
    string template = bonus
        .Replace("of ", "")
        .Replace("the ", "")
        .Replace(" ", "")
        .ToLower();
    
    return template;
    // "of Protection" → "protection"
    // "of the Bear" → "bear"
    // "of the Gods" → "gods"
    // "of Legendary Fortune" → "legendaryfortune"
}
```

### 6. Room Descriptions

#### Building Dynamic Room Descriptions
```csharp
public string GetRoomDescription(Room room)
{
    string themeTemplate = room.Theme.ToLower();
    string description = "You enter a ";
    
    // Add themed descriptor
    description += "{{" + themeTemplate + "|" + room.Theme + "}} themed room. ";
    
    // Add environmental details
    if (room.IsHostile)
    {
        description += "The atmosphere feels dangerous.";
    }
    
    return description;
}
```

**Examples**:
- `"You enter a {{forest|Forest}} themed room."`
- `"You enter a {{crystal|Crystal}} themed room."`
- `"You enter a {{shadow|Shadow}} themed room."`

### 7. Dungeon Names

```csharp
public string GetDungeonName(Dungeon dungeon)
{
    string themeTemplate = dungeon.Theme.ToLower();
    return "{{" + themeTemplate + "|" + dungeon.Name + "}}";
}
```

**Examples**:
- `{{forest|Ancient Forest}}`
- `{{lava|Lava Caves}}`
- `{{crystal|Crystal Caverns}}`
- `{{shadow|Shadow Realm}}`
- `{{temporal|Time Distortion}}`

## Template Name Reference

### Quick Lookup Table

#### Item Modifiers
| Modifier Name | Template Name | Quality |
|--------------|---------------|---------|
| Worn | `worn` | Negative |
| Dull | `dull` | Negative |
| Sturdy | `sturdy` | Common |
| Balanced | `balanced` | Common |
| Sharp | `sharp` | Uncommon |
| Swift | `swift` | Uncommon |
| Precise | `precise` | Uncommon |
| Reinforced | `reinforced` | Uncommon |
| Keen | `keen` | Rare |
| Agile | `agile` | Rare |
| Lucky | `lucky` | Rare |
| Vampiric | `vampiric` | Rare |
| Brutal | `brutal` | Epic |
| Lightning | `lightning` | Epic |
| Blessed | `blessed` | Epic |
| Venomous | `venomous` | Epic |
| Masterwork | `masterwork` | Legendary |
| Ethereal | `ethereal` | Legendary |
| Enchanted | `enchanted` | Legendary |
| Godlike | `godlike` | Legendary |
| Annihilation | `annihilation` | Mythic |
| Time Warp | `timewarp` | Mythic |
| Perfect | `perfect` | Mythic |
| Divine | `divine` | Mythic |
| Reality Breaker | `realitybreaker` | Transcendent |
| Omnipotent | `omnipotent` | Transcendent |
| Infinite | `infinite` | Transcendent |
| Cosmic | `cosmic` | Transcendent |

#### Stat Bonuses (Suffixes)
| Bonus Name | Template Name |
|-----------|---------------|
| of Protection | `protection` |
| of Vitality | `vitality` |
| of Swiftness | `swiftness` |
| of Power | `power` |
| of Recovery | `recovery` |
| of Intelligence | `intelligence` |
| of Strength | `strength` |
| of Agility | `agility` |
| of Technique | `technique` |
| of Accuracy | `accuracy` |
| of the Bear | `bear` |
| of the Cat | `cat` |
| of the Owl | `owl` |
| of the Hawk | `hawk` |
| of the Titan | `titan` |
| of the Wind | `wind` |
| of the Sage | `sage` |
| of the Master | `master` |
| of the Phoenix | `phoenix` |
| of the Colossus | `colossus` |
| of the Storm | `storm` |
| of the Archmage | `archmage` |
| of the Grandmaster | `grandmaster` |
| of the Gods | `gods` |
| of Discovery | `discovery` |
| of Fortune | `fortune` |
| of the Treasure Hunter | `treasurehunter` |
| of the Collector | `collector` |
| of Legendary Fortune | `legendaryfortune` |
| of the Loot Master | `lootmaster` |

#### Environments
| Environment | Template Name |
|------------|---------------|
| Forest | `forest` |
| Lava | `lava` |
| Crypt | `crypt` |
| Crystal | `crystal` |
| Temple | `temple` |
| Ice | `ice` |
| Shadow | `shadow` |
| Steampunk | `steampunk` |
| Swamp | `swamp` |
| Astral | `astral` |
| Underground | `underground` |
| Storm | `storm` |
| Nature | `nature` |
| Arcane | `arcane` |
| Desert | `desert` |
| Volcano | `volcano` |
| Ruins | `ruins` |
| Ocean | `ocean` |
| Mountain | `mountain` |
| Temporal | `temporal` |
| Dream | `dream` |
| Void | `void` |
| Dimensional | `dimensional` |
| Divine | `divine` |

#### Status Effects
| Status Effect | Template Name |
|--------------|---------------|
| Poisoned | `poisoned` |
| Stunned | `stunned` |
| Burning | `burning` |
| Frozen | `frozen` |
| Bleeding | `bleeding` |
| Weakened | `weakened` |
| Slowed | `slowed` |

## Helper Functions

### C# Implementation Example

```csharp
public static class ColorTemplateHelper
{
    /// <summary>
    /// Wraps text in a color template
    /// </summary>
    public static string Wrap(string templateName, string text)
    {
        return $"{{{{{templateName}|{text}}}}}";
    }
    
    /// <summary>
    /// Gets the appropriate modifier template name
    /// </summary>
    public static string GetModifierTemplate(string modifierName)
    {
        return modifierName.ToLower().Replace(" ", "");
    }
    
    /// <summary>
    /// Gets the appropriate bonus template name
    /// </summary>
    public static string GetBonusTemplate(string bonusName)
    {
        return bonusName
            .Replace("of ", "")
            .Replace("the ", "")
            .Replace(" ", "")
            .ToLower();
    }
    
    /// <summary>
    /// Gets the appropriate environment template name
    /// </summary>
    public static string GetEnvironmentTemplate(string environmentName)
    {
        return environmentName.ToLower().Replace(" ", "");
    }
    
    /// <summary>
    /// Gets the appropriate status effect template name
    /// </summary>
    public static string GetStatusTemplate(string statusName)
    {
        return statusName.ToLower().Replace(" ", "");
    }
}
```

### Usage Examples

```csharp
// Item with modifier and bonus
string itemName = ColorTemplateHelper.Wrap("keen", "Keen") + " Steel Sword " + 
                 ColorTemplateHelper.Wrap("precision", "of Precision");

// Environment description
string envName = ColorTemplateHelper.Wrap(
    ColorTemplateHelper.GetEnvironmentTemplate("Crystal Caverns"),
    "Crystal Caverns"
);

// Status effect message
string status = "Enemy is " + 
               ColorTemplateHelper.Wrap("poisoned", "POISONED") + " and " +
               ColorTemplateHelper.Wrap("weakened", "WEAKENED") + "!";
```

## Best Practices

1. **Always use lowercase template names** (e.g., `realitybreaker`, not `RealityBreaker`)
2. **Remove spaces from multi-word templates** (e.g., `timewarp`, not `time warp`)
3. **Match template to content quality** (don't use `cosmic` on a common item)
4. **Use environment templates for all dungeon/room themes**
5. **Use status effect templates for all combat status messages**
6. **Combine rarity + modifier + bonus** for complete item names
7. **Keep text inside templates concise** for better visual effect

## Testing

To test your color templates:

```csharp
// Test item display
Console.WriteLine(ColorTemplateHelper.Wrap("legendary", "Legendary") + " " +
                 ColorTemplateHelper.Wrap("masterwork", "Masterwork") + " Sword " +
                 ColorTemplateHelper.Wrap("gods", "of the Gods"));

// Test environment
Console.WriteLine("You enter the " + 
                 ColorTemplateHelper.Wrap("crystal", "Crystal Caverns"));

// Test status effects
Console.WriteLine("Enemy is " + 
                 ColorTemplateHelper.Wrap("poisoned", "POISONED") + ", " +
                 ColorTemplateHelper.Wrap("weakened", "WEAKENED") + ", and " +
                 ColorTemplateHelper.Wrap("slowed", "SLOWED") + "!");
```

## Troubleshooting

### Template Not Working?
1. Check template name is lowercase
2. Verify no spaces in template name
3. Ensure template exists in ColorTemplates.json
4. Check syntax: `{{templatename|text}}`

### Colors Look Wrong?
1. Verify color codes are correct in JSON
2. Check shader type (solid vs sequence)
3. Test with simple solid color first
4. Review color progression in reference guide

### Text Not Displaying?
1. Check for typos in template name
2. Verify JSON is valid
3. Ensure color rendering system is active
4. Test with basic color codes first (`&R`, `&G`, etc.)

