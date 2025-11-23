# Color System Documentation

## Overview

DungeonFighter-v2 implements a comprehensive Caves of Qud-inspired color system that provides rich text coloring capabilities. The system includes:

1. **Color Definitions** - Single-letter color codes with RGB values
2. **Color Templates** - Pre-defined color patterns (fiery, icy, crystalline, etc.)
3. **Color Parser** - Markup language parser for `&X` and `{{template|text}}` syntax
4. **Color Layer System** - Brightness/saturation adjustment based on event significance
5. **Keyword Color System** - Automatic coloring of keywords based on groups

## Architecture

### Core Components

```
Code/UI/
├── ColorDefinitions.cs          # Color codes and RGB mappings
├── ColorTemplate.cs             # Template system with shaders
├── ColorParser.cs               # Markup parser
├── ColorLayerSystem.cs          # Significance and depth effects
├── KeywordColorSystem.cs        # Automatic keyword coloring
├── ColoredConsoleWriter.cs      # Console output with colors
├── ColorSystemExamples.cs       # Basic examples
├── ColorLayerExamples.cs        # Layer system examples
└── KeywordColorExamples.cs      # Keyword system examples

GameData/
├── ColorTemplates.json          # Template definitions
└── KeywordColorGroups.json      # Keyword group definitions
```

## Color Code System

### Basic Color Codes

The system uses single-letter codes inspired by Caves of Qud:

| Code | Color Name | Hex | Usage |
|------|-----------|-----|-------|
| `r` | dark red / crimson | #a64a2e | &r |
| `R` | red / scarlet | #d74200 | &R |
| `o` | dark orange | #f15f22 | &o |
| `O` | orange | #e99f10 | &O |
| `w` | brown | #98875f | &w |
| `W` | gold / yellow | #cfc041 | &W |
| `g` | dark green | #009403 | &g |
| `G` | green | #00c420 | &G |
| `b` | dark blue | #0048bd | &b |
| `B` | blue / azure | #0096ff | &B |
| `c` | dark cyan / teal | #40a4b9 | &c |
| `C` | cyan | #77bfcf | &C |
| `m` | dark magenta / purple | #b154cf | &m |
| `M` | magenta | #da5bd6 | &M |
| `k` | very dark | #0f3b3a | &k |
| `K` | dark grey / black | #155352 | &K |
| `y` | grey | #b1c9c3 | &y |
| `Y` | white | #ffffff | &Y |

### Color Markup Syntax

#### Foreground Color
```
&RThis text is red
```

#### Background Color
```
^gGreen background (use with &R for red text on green)
```

#### Combined
```
&R^gRed text on green background
```

#### Reset to Default
```
&RRed text&y then grey text
```

## Color Templates

### Template Syntax

```
{{template|text to color}}
```

### Available Templates

#### Elemental Effects
- `fiery` - Blazing fire effect (R-O-W-Y-W-O-R)
- `icy` - Frozen ice effect (C-B-Y-C-b-C-Y)
- `toxic` - Poisonous green (g-G-Y-G-g)
- `electric` - Electrical discharge (C-Y-W-Y-C)

#### Magical Effects
- `crystalline` - Crystal prism (m-M-B-Y-B-M-m)
- `holy` - Divine light (W-Y-W-Y-W)
- `demonic` - Hellfire (r-R-K-r-R)
- `arcane` - Magical energy (m-M-C-M-m)
- `ethereal` - Otherworldly glow (C-M-Y-M-C)

#### Nature & Environment
- `natural` - Nature's essence (g-G-w-G-g)
- `shadow` - Dark shadows (K-k-y-k-K)
- `golden` - Golden gleam (W-O-W-O-W)

#### Status Effects
- `bloodied` - Blood-soaked (r-R-r-K)
- `corrupted` - Dark corruption (m-K-r-K-m)
- `poisoned` - Poison effect (g-G-g-k)
- `stunned` - Stun effect (W-Y-W-y)
- `burning` - Burn effect (R-O-R-r)
- `frozen` - Freeze effect (C-B-Y-C)
- `bleeding` - Bleed effect (r-R-r-r)

#### Item Rarities
- `common` - Grey (y)
- `uncommon` - Green (G)
- `rare` - Blue (B)
- `epic` - Purple/Magenta (M)
- `legendary` - Orange (O)

#### Combat
- `critical` - Critical hit (R-O-Y-O-R)
- `damage` - Damage numbers (R)
- `heal` - Healing (G)
- `mana` - Mana/magic (B)
- `miss` - Missed attack (K)

### Template Shader Types

1. **Sequence** - Each character gets the next color in sequence
2. **Alternation** - Characters alternate between colors (skips whitespace)
3. **Solid** - All text uses the same color

## Usage Examples

### Basic Color Markup

```csharp
// Simple foreground color
UIManager.WriteLine("&RThis text is red");

// Foreground and background
UIManager.WriteLine("&R^gRed text on green background");

// Multiple colors in one line
UIManager.WriteLine("&RRed &Ggreen &Bblue &yback to grey");
```

### Template Usage

```csharp
// Single template
UIManager.WriteLine("{{fiery|Blazing Sword of Fire}}");

// Multiple templates
UIManager.WriteLine("You found a {{legendary|Legendary}} {{fiery|Flaming Sword}}!");

// Mixing templates and codes
UIManager.WriteLine("{{critical|CRITICAL HIT}}! You deal &R55&y damage!");
```

### Programmatic Color Application

```csharp
// Using ColorParser
var segments = ColorParser.Parse("&RRed text");
ColoredConsoleWriter.WriteSegments(segments);

// Using ColorParser.Colorize helper
string colored = ColorParser.Colorize("Sword", "fiery");
// Returns: "{{fiery|Sword}}"

// Strip color markup
string plain = ColorParser.StripColorMarkup("&RRed {{fiery|Fire}}");
// Returns: "Red Fire"
```

## Color Layer System

### Event Significance

The Color Layer System adjusts brightness and saturation based on event importance:

```csharp
public enum EventSignificance
{
    Trivial,      // 0.4x brightness, 0.3x saturation
    Minor,        // 0.6x brightness, 0.5x saturation
    Normal,       // 1.0x brightness, 1.0x saturation
    Important,    // 1.2x brightness, 1.2x saturation
    Critical      // 1.5x brightness, 1.4x saturation
}
```

#### Usage

```csharp
var baseColor = ColorDefinitions.GetColor('R').Value;

// Create significant segment
var segment = ColorLayerSystem.CreateSignificantSegment(
    "CRITICAL HIT",
    baseColor,
    EventSignificance.Critical
);

ColoredConsoleWriter.WriteSegments(new[] { segment }.ToList());
```

#### Apply to Parsed Segments

```csharp
var segments = ColorParser.Parse("{{fiery|Fire Attack}}");
ColorLayerSystem.ApplySignificanceToSegments(segments, EventSignificance.Important);
```

### Dungeon Depth Progression

White text gradually shifts from warm (yellowish) to cool (bluish) as you progress deeper:

```csharp
// Warm white (early dungeon): RGB(255, 250, 220)
// Neutral white (mid dungeon): RGB(255, 255, 255)
// Cool white (deep dungeon): RGB(220, 230, 255)

// Get white based on current room
var whiteColor = ColorLayerSystem.GetWhiteByDepth(
    currentRoom: 5,
    totalRooms: 10,
    brightness: 1.0f
);

// Create segment with depth-based white
var segment = ColorLayerSystem.CreateDepthWhiteSegment(
    "You explore deeper...",
    currentRoom: 5,
    totalRooms: 10
);
```

#### White Temperature Types

```csharp
public enum WhiteTemperature
{
    Warm,      // Yellowish tint - early dungeon
    Neutral,   // Pure white - mid dungeon
    Cool       // Bluish tint - deep dungeon
}

// Get specific temperature
var warmWhite = ColorLayerSystem.GetWhite(WhiteTemperature.Warm, brightness: 1.0f);
```

## Keyword Color System

### Overview

Automatically colors keywords based on predefined groups. Keywords are matched as whole words and colored with their group's pattern.

### Default Keyword Groups

| Group | Pattern | Keywords (sample) |
|-------|---------|-------------------|
| damage | damage (red) | damage, hit, strike, attack, slash, pierce, crush |
| critical | critical (fiery) | critical, crit, devastating, crushing blow |
| heal | heal (green) | heal, restore, regenerate, recover, mend |
| fire | fiery | fire, flame, burn, blaze, inferno |
| ice | icy | ice, frost, frozen, freeze, chill, cold |
| lightning | electric | lightning, thunder, electric, shock, zap |
| poison | poisoned | poison, venom, toxic, toxin |
| shadow | shadow | shadow, darkness, dark, void, abyss |
| holy | holy | holy, divine, sacred, blessed, light |
| death | bloodied | death, die, killed, slain, defeated |
| blood | bleeding | blood, bleed, bleeding, hemorrhage |
| stun | stunned | stun, stunned, daze, paralyzed |
| magic | arcane | magic, spell, arcane, mystical, enchanted |
| nature | natural | nature, earth, growth, plant, forest |
| gold | golden | gold, coins, currency, treasure, wealth |
| experience | white | experience, xp, level, level up |
| enemy | red | goblin, orc, skeleton, zombie, dragon, demon, wolf, bear, treant, golem, lich, wyrm |
| class | cyan | warrior, mage, rogue, wizard, barbarian, paladin, ranger |
| action | electric | jab, taunt, flurry, cleave, shield bash, precision strike, war cry, backstab |
| character | golden | you, your, yourself, hero, champion, adventurer |
| environment | natural | entrance, chamber, room, hall, cavern, tunnel, dungeon, crypt, tomb, temple |
| theme | crystalline | forest, lava, frozen, ice, shadow, crystal, astral, underground, swamp, storm |
| common | common (grey) | common |
| uncommon | uncommon (green) | uncommon |
| rare | rare (blue) | rare |
| epic | epic (purple) | epic |
| legendary | legendary (orange) | legendary, mythic, artifact |

### Usage

#### Automatic All Keywords

```csharp
string text = "You hit the goblin for 25 damage!";
string colored = KeywordColorSystem.Colorize(text);
UIManager.WriteLine(colored);
// Output: You {{damage|hit}} the {{enemy|goblin}} for 25 {{damage|damage}}!
```

#### Specific Groups

```csharp
string text = "The warrior casts fire and deals damage!";
string colored = KeywordColorSystem.ColorizeWithGroups(text, "fire", "damage");
UIManager.WriteLine(colored);
// Only "fire" and "damage" keywords are colored
```

#### Create Custom Groups

```csharp
// Create a new keyword group
KeywordColorSystem.CreateGroup(
    name: "player-actions",
    colorPattern: "cyan",
    caseSensitive: false,
    keywords: "defend", "dodge", "parry", "block", "evade"
);

// Use the custom group
string text = "You dodge and parry the attack!";
string colored = KeywordColorSystem.Colorize(text);
```

#### Add Keywords to Existing Group

```csharp
KeywordColorSystem.AddKeywordsToGroup("enemy", "vampire", "werewolf", "lich");
```

#### Manage Groups

```csharp
// Get all group names
var groupNames = KeywordColorSystem.GetAllGroupNames();

// Get specific group
var group = KeywordColorSystem.GetKeywordGroup("damage");

// Remove a group
KeywordColorSystem.RemoveGroup("custom-group");

// Reset to defaults
KeywordColorSystem.ResetToDefaults();
```

### Integration with Existing Markup

The keyword system works seamlessly with manual markup:

```csharp
// Manual markup is preserved, keywords are auto-colored
string text = "&Y[BOSS FIGHT]&y The goblin attacks for critical damage!";
string colored = KeywordColorSystem.Colorize(text);
// "&Y[BOSS FIGHT]&y The {{enemy|goblin}} {{damage|attacks}} for {{critical|critical}} {{damage|damage}}!"
```

## Dungeon Theme Colors

### Overview

The Dungeon Theme Color system automatically applies thematic colors to dungeon names based on their environment type. Each of the 24 dungeon themes has a unique color palette that helps players visually identify dungeon types.

### Theme Color Mapping

```
Code/UI/
└── DungeonThemeColors.cs          # Theme-to-color mappings
```

### Available Theme Colors

#### Natural Themes
| Theme | Color | RGB |
|-------|-------|-----|
| Forest | Vibrant Green | (0, 196, 32) |
| Nature | Dark Green | (0, 148, 3) |
| Swamp | Murky Green | (88, 129, 87) |

#### Fire/Heat Themes
| Theme | Color | RGB |
|-------|-------|-----|
| Lava | Red | (215, 66, 0) |
| Volcano | Orange | (255, 140, 0) |

#### Ice/Cold Themes
| Theme | Color | RGB |
|-------|-------|-----|
| Ice | Cyan | (119, 191, 207) |
| Mountain | Silver | (192, 192, 192) |

#### Dark Themes
| Theme | Color | RGB |
|-------|-------|-----|
| Crypt | Purple | (177, 84, 207) |
| Shadow | Indigo | (75, 0, 130) |
| Void | Dark Gray | (64, 64, 64) |
| Dream | Medium Purple | (147, 112, 219) |

#### Magical Themes
| Theme | Color | RGB |
|-------|-------|-----|
| Crystal | Bright Cyan | (0, 255, 255) |
| Arcane | Blue | (0, 150, 255) |
| Astral | Magenta | (218, 91, 214) |
| Dimensional | Blue Violet | (138, 43, 226) |
| Temporal | Cornflower Blue | (100, 149, 237) |

#### Holy/Divine Themes
| Theme | Color | RGB |
|-------|-------|-----|
| Temple | Gold | (255, 215, 0) |
| Divine | Light Yellow | (255, 255, 224) |

#### Weather Themes
| Theme | Color | RGB |
|-------|-------|-----|
| Storm | Steel Blue | (70, 130, 180) |
| Ocean | Deep Blue | (0, 105, 148) |

#### Industrial/Artificial Themes
| Theme | Color | RGB |
|-------|-------|-----|
| Steampunk | Bronze | (205, 127, 50) |
| Underground | Brown | (139, 90, 43) |

#### Desert/Arid Themes
| Theme | Color | RGB |
|-------|-------|-----|
| Desert | Sandy Beige | (237, 201, 175) |
| Ruins | Stone Gray-Brown | (160, 144, 119) |

#### Generic
| Theme | Color | RGB |
|-------|-------|-----|
| Generic | Silver | (192, 192, 192) |

### Usage

#### Basic Color Retrieval

```csharp
using RPGGame.UI.Avalonia;

// Get theme color for a dungeon
Color themeColor = DungeonThemeColors.GetThemeColor("Forest");
// Returns: Green (0, 196, 32)

// Use with Avalonia UI
canvas.AddText(x, y, dungeonName, themeColor);
```

#### Color Variations

```csharp
// Get dimmed version for hover effects
Color dimmed = DungeonThemeColors.GetDimmedThemeColor("Lava");
// Returns 70% brightness version

// Get brightened version for highlights
Color bright = DungeonThemeColors.GetBrightenedThemeColor("Shadow");
// Returns brightened version (+50 to each RGB channel)
```

#### Display All Themes

```csharp
// Get all available theme colors
var allThemes = DungeonThemeColors.GetAllThemeColors();

foreach (var theme in allThemes)
{
    Console.WriteLine($"{theme.Key}: RGB({theme.Value.R}, {theme.Value.G}, {theme.Value.B})");
}
```

### UI Integration

#### Dungeon Selection Screen

The dungeon selection screen automatically displays dungeons in their theme colors:

```csharp
// CanvasUICoordinator automatically applies theme colors
for (int i = 0; i < dungeons.Count; i++)
{
    var dungeon = dungeons[i];
    var themeColor = DungeonThemeColors.GetThemeColor(dungeon.Theme);
    canvas.AddMenuOption(x, y, i + 1, 
        $"{dungeon.Name} (lvl {dungeon.MinLevel})", 
        themeColor, 
        isHovered);
}
```

#### Dungeon Information Screens

Theme colors are also used on dungeon start and completion screens:

```csharp
// Dungeon start screen
var themeColor = DungeonThemeColors.GetThemeColor(dungeon.Theme);
canvas.AddText(x, y, "Dungeon: ", AsciiArtAssets.Colors.White);
canvas.AddText(x + 10, y, dungeon.Name, themeColor);

// Dungeon completion screen
canvas.AddText(x, y, "Completed: ", AsciiArtAssets.Colors.White);
canvas.AddText(x + 12, y, dungeon.Name, themeColor);
```

### Benefits

1. **Visual Identification**: Players can quickly recognize dungeon types by color
2. **Thematic Consistency**: Colors match dungeon environments (green for forest, blue for ice, etc.)
3. **Enhanced UI**: Adds visual richness to dungeon selection and information screens
4. **Scalability**: Easy to add new themes or adjust existing colors

### Extending the System

#### Adding New Theme Colors

```csharp
// In DungeonThemeColors.cs, add to themeColorMap:
{ "NewTheme", Color.FromRgb(255, 0, 255) },  // Magenta
```

#### Custom Color Logic

```csharp
// Create custom color logic based on dungeon level
public static Color GetLevelBasedThemeColor(string theme, int level)
{
    Color baseColor = GetThemeColor(theme);
    
    // Darken for high-level dungeons
    if (level > 50)
    {
        return Color.FromRgb(
            (byte)(baseColor.R * 0.7),
            (byte)(baseColor.G * 0.7),
            (byte)(baseColor.B * 0.7)
        );
    }
    
    return baseColor;
}
```

## Integration with Game Systems

### UIManager Integration

The UIManager automatically supports color markup when `EnableColorMarkup` is true:

```csharp
// Enable color markup parsing (default: true)
UIManager.EnableColorMarkup = true;

// All WriteLine calls support markup
UIManager.WriteLine("&RDamage: {{damage|25}}");
UIManager.WriteMenuLine("{{legendary|Legendary Item}} found!");
```

### Canvas UI Integration

The CanvasUICoordinator includes color support for Avalonia:

```csharp
var canvas = new GameCanvasControl();
var uiManager = new CanvasUICoordinator(canvas);

// Use WriteLineColored for markup support
uiManager.WriteLineColored("{{fiery|Fire Attack}}", x: 10, y: 5);
```

### Combat System Integration

```csharp
// Apply significance to combat messages
var baseColor = ColorDefinitions.GetColor('R').Value;
var significance = isCritical ? EventSignificance.Critical : EventSignificance.Normal;
var damageText = ColorLayerSystem.CreateSignificantSegment(
    $"{damage} damage",
    baseColor,
    significance
);

// Use keyword coloring for combat log
string combatMessage = $"{attacker} hits {defender} for {damage} damage!";
string colored = KeywordColorSystem.Colorize(combatMessage);
UIManager.WriteLine(colored);
```

### Dungeon System Integration

```csharp
// Apply depth-based coloring to room descriptions
for (int room = 1; room <= totalRooms; room++)
{
    var whiteSegment = ColorLayerSystem.CreateDepthWhiteSegment(
        $"Room {room}: {description}",
        room,
        totalRooms
    );
    
    ColoredConsoleWriter.WriteSegments(new[] { whiteSegment }.ToList());
}
```

## Performance Considerations

### Parsing Overhead

- Color parsing is fast (<0.1ms per message)
- Caching is automatic within segments
- Only parse when markup is detected

### Optimization Tips

```csharp
// Check for markup before parsing
if (ColorParser.HasColorMarkup(text))
{
    // Only parse if needed
    var segments = ColorParser.Parse(text);
}

// Get display length without creating segments
int length = ColorParser.GetDisplayLength(text);

// Strip markup for comparison/search
string plain = ColorParser.StripColorMarkup(text);
```

## Testing & Examples

### Run Demonstrations

```csharp
// Basic color system
ColorSystemExamples.RunAllDemos();

// Layer system
ColorLayerExamples.RunAllDemos();

// Keyword system
KeywordColorExamples.RunAllDemos();
```

### Individual Tests

```csharp
// Test specific features
ColorSystemExamples.DemoBasicColors();
ColorSystemExamples.DemoTemplates();
ColorSystemExamples.DemoCombatMessages();

ColorLayerExamples.DemoEventSignificance();
ColorLayerExamples.DemoDungeonDepthProgression();

KeywordColorExamples.DemoBasicKeywordColoring();
KeywordColorExamples.DemoCombatScenarios();
```

## Best Practices

### 1. Choose the Right Tool

- **Simple coloring**: Use color codes `&R`
- **Complex effects**: Use templates `{{fiery|text}}`
- **Automatic coloring**: Use keyword system
- **Significance**: Use layer system for important events

### 2. Consistency

```csharp
// Good: Consistent use of templates
"{{damage|15}} damage to {{enemy|Goblin}}"

// Bad: Mixing unnecessarily
"&R15&y damage to {{enemy|Goblin}}"
```

### 3. Performance

```csharp
// Good: Check before parsing
if (ColorParser.HasColorMarkup(message))
{
    ColoredConsoleWriter.WriteLine(message);
}

// Bad: Always parsing
ColoredConsoleWriter.WriteLine(message); // Parses every time
```

### 4. Significance Usage

```csharp
// Reserve Critical for truly important events
EventSignificance.Trivial  // Minor flavor text
EventSignificance.Minor    // Routine events
EventSignificance.Normal   // Standard gameplay
EventSignificance.Important // Key moments
EventSignificance.Critical  // Boss fights, level ups, rare drops
```

### 5. Keyword Groups

```csharp
// Group related keywords logically
KeywordColorSystem.CreateGroup(
    "boss-names",
    "legendary",
    false,
    "Dragon Lord", "Lich King", "Demon Prince"
);
```

## Configuration Files

### ColorTemplates.json

Contains all template definitions with their shader types and color sequences.

### KeywordColorGroups.json

Contains all keyword groups with their patterns and keywords.

## Text Fade Animation System

The color system now includes an advanced **Text Fade Animation System** that makes text fade from the screen using various patterns and color progressions.

### Quick Example

```csharp
// Fade with alternating pattern (every other letter fades first)
TextFadeAnimator.FadeOutAlternating("The enemy has been defeated!");

// Fade using a color template
TextFadeAnimator.FadeOutWithTemplate("Burning flames", "fiery");
```

### Available Fade Patterns

- **Alternating**: Every other letter fades first (your requested feature!)
- **Sequential**: Left to right fade
- **Center Collapse**: Outside to center
- **Center Expand**: Center to outside
- **Uniform**: All at once
- **Random**: Random letter order

### Documentation

See **TEXT_FADE_ANIMATION_SYSTEM.md** and **TEXT_FADE_ANIMATION_QUICKSTART.md** for comprehensive documentation.

### Testing

```bash
# Run all fade animation demos
Scripts\test-fade.bat

# Interactive testing
Scripts\test-fade.bat --interactive
```

## Future Enhancements

Potential additions:

- Gradient effects across text
- Custom shader types
- Dynamic color mixing
- Mood-based color schemes
- Player-customizable palettes
- Advanced fade animations (wave, bounce, etc.)

## Related Documentation

- **ARCHITECTURE.md** - System architecture overview
- **UI_SYSTEM.md** - UI system documentation
- **CODE_PATTERNS.md** - Coding patterns and conventions

---

*This color system provides rich text formatting capabilities while maintaining performance and ease of use.*

