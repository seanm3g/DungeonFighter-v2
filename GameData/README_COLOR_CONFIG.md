# Color Configuration System

## Overview

DungeonFighter-v2 uses a comprehensive, data-driven color system inspired by Caves of Qud. All color presets and keyword mappings are stored in JSON configuration files in this directory, making them easy to modify without touching any code.

## Configuration Files

### 1. ColorTemplates.json

Defines color templates (presets) that can be applied to text.

**Structure:**
```json
{
  "colorCodes": {
    "description": "Single-letter color codes",
    "codes": {
      "R": "red / scarlet (#d74200)",
      "G": "green (#00c420)",
      ...
    }
  },
  "templates": [
    {
      "name": "fiery",
      "shaderType": "sequence",
      "colors": ["R", "O", "W", "Y", "W", "O", "R"],
      "description": "Blazing fire effect"
    }
  ]
}
```

**Shader Types:**
- `solid` - All text uses the same color
- `sequence` - Each character gets the next color in sequence (cycles)
- `alternation` - Characters alternate between colors (skips whitespace)

**Color Codes:** Single letters that map to RGB colors
- `r` = dark red, `R` = red
- `g` = dark green, `G` = green
- `b` = dark blue, `B` = blue
- `c` = dark cyan, `C` = cyan
- `m` = dark magenta, `M` = magenta
- `o` = dark orange, `O` = orange
- `w` = brown, `W` = gold/yellow
- `k` = very dark, `K` = dark grey
- `y` = grey, `Y` = white

**Pre-defined Templates:**
- **Elements:** `fiery`, `icy`, `toxic`, `electric`
- **Magic:** `arcane`, `holy`, `demonic`, `ethereal`, `corrupted`
- **Nature:** `natural`, `crystalline`, `shadow`
- **Combat:** `critical`, `damage`, `heal`, `bloodied`, `bleeding`
- **Rarities:** `common`, `uncommon`, `rare`, `epic`, `legendary`
- **Status:** `poisoned`, `stunned`, `burning`, `frozen`
- **Special:** `golden`, `rainbow`

### 2. KeywordColorGroups.json

Defines keyword groups and their associated color patterns. Keywords in text are automatically colored based on these groups.

**Structure:**
```json
{
  "groups": [
    {
      "name": "damage",
      "colorPattern": "damage",
      "caseSensitive": false,
      "keywords": [
        "damage", "hit", "strike", "attack", "slash", "pierce"
      ]
    }
  ]
}
```

**Properties:**
- `name` - Unique identifier for the group
- `colorPattern` - Template name from ColorTemplates.json (e.g., "natural", "crystalline", "fiery") or legacy color code
- `caseSensitive` - Whether keyword matching is case-sensitive
- `keywords` - Array of keywords to match (whole words only)

**Note:** The `colorPattern` can reference any template defined in `ColorTemplates.json`. The system will automatically extract a representative color from the template for keyword highlighting. This allows keyword groups to use the same rich color templates used elsewhere in the game.

**Pre-defined Groups:**
- **Combat:** `damage`, `critical`, `heal`, `action`, `stun`, `blood`
- **Elements:** `fire`, `ice`, `lightning`, `poison`, `shadow`, `holy`
- **Status:** `buff`, `debuff`, `death`
- **Items:** `common`, `uncommon`, `rare`, `epic`, `legendary`
- **World:** `enemy`, `class`, `character`, `environment`, `theme`
- **Resources:** `magic`, `nature`, `gold`, `experience`

## How to Add New Color Templates

1. Open `ColorTemplates.json`
2. Add a new entry to the `templates` array:

```json
{
  "name": "volcanic",
  "shaderType": "sequence",
  "colors": ["r", "R", "O", "W", "O", "R", "r"],
  "description": "Volcanic lava effect"
}
```

3. The template is immediately available for use in game text:
   - In markup: `{{volcanic|Volcanic Hammer}}`
   - In keyword groups: Set `colorPattern` to `"volcanic"`

## How to Add Keywords

1. Open `KeywordColorGroups.json`
2. Find the appropriate group or create a new one
3. Add keywords to the `keywords` array:

```json
{
  "name": "enemy",
  "colorPattern": "red",
  "caseSensitive": false,
  "keywords": [
    "goblin", "orc", "skeleton",
    "my-new-enemy"  // Add your keyword here
  ]
}
```

## How to Create New Keyword Groups

1. Open `KeywordColorGroups.json`
2. Add a new group to the `groups` array:

```json
{
  "name": "my-custom-group",
  "colorPattern": "crystalline",
  "caseSensitive": false,
  "keywords": [
    "custom1", "custom2", "custom3"
  ]
}
```

3. The group is immediately available and will automatically color matching keywords

## Usage in Game

### Automatic Keyword Coloring

Keywords are automatically colored when text passes through the KeywordColorSystem:

```csharp
string text = "You hit the goblin for 25 damage!";
string colored = KeywordColorSystem.Colorize(text);
// Result: "You hit the goblin for 25 damage!" (with appropriate colors)
```

### Using Color Templates

Apply templates directly in text using markup:

```csharp
string text = "You found a {{legendary|Sword of Fire}}!";
```

### Color Codes

Use single-letter codes for inline coloring:

```csharp
string text = "&RRed text&y normal text &GGreen text&y";
```

## Configuration Loading

The system automatically loads these configuration files on startup:

1. **ColorTemplates.json** → `ColorTemplateLibrary`
2. **KeywordColorGroups.json** → `KeywordColorSystem`

If loading fails, the system falls back to hardcoded defaults.

## Reloading Configuration

To reload configuration at runtime (useful for testing):

```csharp
// Reload color templates (also clears KeywordColorSystem cache)
ColorTemplateLibrary.Reload();

// Reload keyword groups
KeywordColorLoader.Reload();
KeywordColorLoader.LoadAndRegisterKeywordGroups();

// Or clear keyword cache manually if templates changed
KeywordColorSystem.ClearColorPatternCache();
```

## Best Practices

### Color Templates
- Use descriptive names (e.g., `"volcanic"` not `"template1"`)
- Keep color sequences between 3-7 colors for best visual effect
- Use `solid` for simple, consistent coloring
- Use `sequence` for gradients and effects
- Use `alternation` for rainbow/alternating patterns

### Keyword Groups
- Group related keywords together (e.g., all fire-related words in `"fire"` group)
- Use lowercase keywords unless case-sensitive matching is needed
- Avoid overlapping keywords between groups (first match wins)
- Put more specific groups before general ones in the file

### Performance
- The configuration files are cached after first load
- Keywords use whole-word matching (efficient)
- Case-insensitive matching is optimized

## Examples

### Adding a New Element Type

1. Add color template in `ColorTemplates.json`:
```json
{
  "name": "earth",
  "shaderType": "sequence",
  "colors": ["w", "W", "G", "g", "w"],
  "description": "Earth and stone effect"
}
```

2. Add keyword group in `KeywordColorGroups.json`:
```json
{
  "name": "earth",
  "colorPattern": "earth",
  "caseSensitive": false,
  "keywords": [
    "earth", "stone", "rock", "boulder", "quake", "tremor"
  ]
}
```

### Changing Rarity Colors

Edit the rarity templates in `ColorTemplates.json`:

```json
{
  "name": "legendary",
  "shaderType": "solid",
  "colors": ["M"],  // Change to 'M' for magenta instead of 'O' for orange
  "description": "Legendary rarity (magenta)"
}
```

### Adding Enemy Names

Simply add to the `enemy` group in `KeywordColorGroups.json`:

```json
{
  "name": "enemy",
  "colorPattern": "red",
  "caseSensitive": false,
  "keywords": [
    "goblin", "orc", "skeleton",
    "new-boss", "elite-guard", "shadow-beast"
  ]
}
```

## Troubleshooting

### Configuration not loading?
- Check that JSON syntax is valid (use a JSON validator)
- Ensure files are in the `GameData` folder
- Check console for error messages
- Try clearing cache and restarting

### Keywords not coloring?
- Verify keyword exists in `KeywordColorGroups.json`
- Check that `colorPattern` references a valid template in `ColorTemplates.json`
- Ensure keywords are lowercase (unless case-sensitive)
- Keywords must match whole words only

### Color template not found?
- Verify template name matches exactly (case-insensitive)
- Check that template has valid color codes
- Ensure `shaderType` is one of: `solid`, `sequence`, `alternation`

## Technical Details

### File Locations
- Config files: `GameData/ColorTemplates.json` and `GameData/KeywordColorGroups.json`
- Loader code: `Code/Data/ColorTemplateLoader.cs` and `Code/Data/KeywordColorLoader.cs`
- Color systems: `Code/UI/ColorTemplate.cs`, `Code/UI/KeywordColorSystem.cs`, `Code/UI/ColorDefinitions.cs`

### Architecture
```
GameData JSON Files
    ↓
Loader Classes (ColorTemplateLoader, KeywordColorLoader)
    ↓
Color Systems (ColorTemplateLibrary, KeywordColorSystem)
    ↓
Game Text Rendering
```

### Fallback Behavior
If JSON loading fails, the system uses hardcoded defaults to ensure the game remains playable.

## See Also

- `Documentation/05-Systems/COLOR_SYSTEM.md` - Complete color system documentation
- `Documentation/05-Systems/KEYWORD_PATTERNS_QUICKSTART.md` - Quick-start guide
- `ColorTemplates.json` - Color template definitions
- `KeywordColorGroups.json` - Keyword group definitions

