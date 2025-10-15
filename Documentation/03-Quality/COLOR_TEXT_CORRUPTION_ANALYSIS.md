# Color Text Corruption Analysis
**Date:** October 12, 2025  
**Issue:** Text characters appear corrupted when color templates are applied  
**Status:** 🔍 INVESTIGATING

---

## Problem Description

When dungeon names are displayed in the dungeon selection menu with color templates applied, the text appears garbled or corrupted. Characters seem to be missing, duplicated, or replaced with strange symbols.

### Example Issue
```
Expected: [1] Celestial Observatory (lvl 1)
Actual:   [1] C●st●alObs●rvat●ry (lvl 1) [garbled/corrupted]
```

---

## System Architecture

### Color Application Pipeline

```
1. DUNGEON RENDERER
   └─> DungeonRenderer.RenderDungeonSelection()
       - Gets dungeon theme (e.g., "Astral")
       - Maps theme to template name (e.g., "astral")
       - Calls ColorParser.Colorize(dungeonName, templateName)

2. COLOR PARSER - MARKUP CREATION
   └─> ColorParser.Colorize(text, templateOrColorCode)
       - Wraps text in template markup: {{astral|Celestial Observatory}}
       - Returns: "{{astral|Celestial Observatory}}"

3. COLOR PARSER - PARSING
   └─> ColorParser.Parse(markup)
       a. ExpandTemplates()
          - Finds: {{astral|Celestial Observatory}}
          - Gets template from ColorTemplateLibrary
          - Calls template.Apply("Celestial Observatory")
          - Converts result back to color codes: &MC&Be&Yl&Ce&Ms...
          - Returns expanded string
       
       b. ParseColorCodes()
          - Parses: &MC&Be&Yl&Ce&Ms...
          - Creates segments for each character
          - Returns List<ColoredSegment>

4. COLOR TEMPLATE APPLICATION
   └─> ColorTemplate.ApplySequence(text)
       - For each character in text:
         - If whitespace: Create segment WITHOUT color
         - If non-whitespace: Create segment with next color in sequence
       - Returns List<ColoredSegment>

5. TEXT RENDERING
   └─> ColoredTextWriter.WriteLineColored(text, x, y)
       - Detects per-character coloring (many small segments)
       - Uses character-by-character positioning
       - Calls canvas.AddText() for each segment

6. CANVAS RENDERING
   └─> GameCanvasControl.AddText(x, y, text, color)
       - Creates FormattedText with Avalonia
       - Renders to canvas
```

---

## Potential Issues

### 1. Template Expansion Character Loss

**Location:** `ColorParser.ExpandTemplates()` (lines 40-85)

**Issue:** When converting template segments back to color code markup, characters might be lost if:
- `FindColorCode()` returns null (color not found in map)
- Segment text is null or empty
- Color codes are malformed

**Example:**
```csharp
// If a color in the template doesn't have a matching color code:
segment.Foreground = RGB(100, 100, 100)  // Custom color
FindColorCode(RGB(100, 100, 100)) -> null
// Result: No color code added, character rendered with previous color
```

### 2. Empty Segments

**Location:** `ColorTemplate.ApplySequence()` (lines 57-85)

**Issue:** If empty segments are created (text is null or empty string), they might cause:
- Position miscalculation in rendering
- Missing characters in reconstruction
- Extra color code sequences

**Check:** Count of empty segments vs. expected segments

### 3. Character-by-Character Positioning

**Location:** `ColoredTextWriter.WriteLineColored()` (lines 41-57)

**Issue:** When many small segments are detected, character-by-character positioning is used:
```csharp
int charPosition = 0;
foreach (var segment in segments)
{
    if (!string.IsNullOrEmpty(segment.Text))
    {
        canvas.AddText(x + charPosition, y, segment.Text, color);
        charPosition += segment.Text.Length;
    }
}
```

**Problem:** If empty segments exist, they're skipped but don't affect `charPosition`, which should be correct... BUT if segments have incorrect text lengths or null text, positioning breaks.

### 4. Color Code Parsing Loop

**Location:** `ColorParser.ParseColorCodes()` (lines 110-174)

**Issue:** The parsing loop uses `i++` to skip color code characters:
```csharp
if ((c == '&' || c == '^') && i + 1 < text.Length)
{
    char nextChar = text[i + 1];
    if (ColorDefinitions.IsValidColorCode(nextChar))
    {
        // Process color change
        i++;  // Skip the color code character
        continue;
    }
}
```

**Problem:** If the loop logic is off by one, or if `continue` doesn't work as expected, characters might be:
- Skipped (missing from output)
- Duplicated (processed twice)
- Rendered as color codes (not skipped)

### 5. Template Color Sequence Issues

**Location:** Template definitions in `GameData/ColorTemplates.json`

**Example Astral Template:**
```json
{
  "name": "astral",
  "shaderType": "sequence",
  "colors": ["M", "B", "Y", "C"]
}
```

**Issue:** If any color code in the sequence is invalid or doesn't exist in `ColorDefinitions.colorMap`, the pipeline breaks.

**Validation:**
- M = magenta ✓ Valid
- B = blue ✓ Valid
- Y = white ✓ Valid
- C = cyan ✓ Valid

### 6. Whitespace Handling

**Location:** `ColorTemplate.ApplySequence()` (lines 64-68)

**Code:**
```csharp
if (char.IsWhiteSpace(text[i]))
{
    // Add whitespace without coloring to preserve spacing
    segments.Add(new ColorDefinitions.ColoredSegment(text[i].ToString()));
}
```

**Issue:** Whitespace segments have no color. When converted back to markup, they're just added as-is:
```csharp
result.Append(segment.Text);  // Just the space, no color code
```

**Potential Problem:** The reconstruction might look like:
```
&MC&Be&Yl&Ce&Ms&Bt&Bi&Ba&Yl &MO&Bb...
```
Note the space between "l" and "O" has no color code prefix. This SHOULD be fine since the previous color state is maintained... but what if it's not?

---

## Diagnostic Steps

### Step 1: Verify Text Reconstruction

Run the diagnostic tool to check if text is preserved through the pipeline:

```csharp
ColorTextAnalysis.AnalyzeFullPipeline("Celestial Observatory", "astral");
```

**Expected Output:**
- Markup created correctly
- All segments account for all characters
- Reconstructed text matches original
- No empty segments

### Step 2: Check Segment Counts

For "Celestial Observatory" (21 characters including space):
- Expected segments: 21 (one per character)
- Check for empty segments: should be 0
- Check for null text: should be 0

### Step 3: Verify Color Code Expansion

Check the intermediate markup after template expansion:
```
Original: {{astral|Celestial Observatory}}
Expanded: &MC&Be&Yl&Ce&Ms&Bt&Bi&Ba&Yl &MO&Bb&Bs&Ce&Mr&Yv&Ca&Mt&Bo&Yr&Cy
```

Count color codes: Should be 20 (one before each non-space character)

### Step 4: Monitor Rendering

Add debug output to `ColoredTextWriter.WriteLineColored()`:
```csharp
Console.WriteLine($"Rendering {segments.Count} segments:");
foreach (var seg in segments)
{
    Console.WriteLine($"  '{seg.Text}' at pos {charPosition}");
}
```

### Step 5: Check Font Rendering

Some fonts might not render certain character combinations correctly. Test with:
- Consolas (current)
- Courier New
- Cascadia Mono
- DejaVu Sans Mono

---

## Possible Solutions

### Solution 1: Skip Template Expansion for Short Text

For short text (< 10 chars), use solid color instead of sequence:
```csharp
if (text.Length < 10 && template.ShaderType == ColorShaderType.Sequence)
{
    // Use solid color instead
    return ColorParser.ColorizeRaw(text, template.ColorSequence[0]);
}
```

### Solution 2: Optimize Template Expansion

Instead of converting template segments back to color codes, keep them as segments:
```csharp
// In ColorParser.Parse():
private static List<ColoredSegment> Parse(string text)
{
    // Check for template markup first
    var templateMatch = templatePattern.Match(text);
    if (templateMatch.Success)
    {
        // Apply template directly, don't convert to color codes
        var template = ColorTemplateLibrary.GetTemplate(templateMatch.Groups[1].Value);
        if (template != null)
        {
            return template.Apply(templateMatch.Groups[2].Value);
        }
    }
    
    // Otherwise parse color codes normally
    return ParseColorCodes(text);
}
```

### Solution 3: Use Word-Based Coloring

Instead of coloring each character, color each word:
```csharp
private List<ColoredSegment> ApplySequenceByWord(string text)
{
    var words = text.Split(' ');
    var segments = new List<ColoredSegment>();
    
    for (int i = 0; i < words.Length; i++)
    {
        if (i > 0) segments.Add(new ColoredSegment(" "));  // Add space
        
        char colorCode = ColorSequence[i % ColorSequence.Count];
        var color = ColorDefinitions.GetColor(colorCode);
        segments.Add(new ColoredSegment(words[i], color));
    }
    
    return segments;
}
```

### Solution 4: Fallback to Solid Color

If template application fails or produces too many segments, fallback:
```csharp
var segments = template.Apply(content);

// Safety check
if (segments.Count > content.Length * 2)
{
    // Too many segments, something went wrong
    return new List<ColoredSegment> { 
        new ColoredSegment(content, template.ColorSequence[0]) 
    };
}
```

---

## Next Steps

1. ✅ Create diagnostic tools (`ColorSystemDiagnostic.cs`, `ColorTextAnalysis.cs`)
2. ⏳ Run diagnostics to identify exact failure point
3. ⏳ Implement appropriate solution based on findings
4. ⏳ Add unit tests for color template application
5. ⏳ Test with various dungeon names and themes

---

## Testing Plan

### Test Cases

1. **Short names:** "Test", "Cave", "Ocean"
2. **Medium names:** "Crystal Caverns", "Ocean Depths"
3. **Long names:** "Celestial Observatory", "Underground City"
4. **With spaces:** "Ancient Library", "Mystical Garden"
5. **Special cases:** Single character, all spaces, empty string

### Validation

For each test case:
- ✓ Original text length = Reconstructed text length
- ✓ Original text = Reconstructed text (exact match)
- ✓ No empty segments (or expected number)
- ✓ All characters accounted for
- ✓ Rendering matches expected visual output

---

## Related Files

- `Code/UI/ColorParser.cs` - Main color parsing logic
- `Code/UI/ColorTemplate.cs` - Template application
- `Code/UI/Avalonia/Renderers/ColoredTextWriter.cs` - Text rendering
- `Code/UI/Avalonia/Renderers/DungeonRenderer.cs` - Dungeon display
- `Code/UI/ColorDefinitions.cs` - Color code definitions
- `GameData/ColorTemplates.json` - Template configuration

---

## References

- [COLOR_SYSTEM.md](../05-Systems/COLOR_SYSTEM.md)
- [COLOR_TEXT_RENDERING_FIX.md](../05-Systems/COLOR_TEXT_RENDERING_FIX.md)
- [COMBAT_TEXT_ARCHITECTURE.md](COMBAT_TEXT_ARCHITECTURE.md)

