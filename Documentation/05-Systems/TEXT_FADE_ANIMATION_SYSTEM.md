# Text Fade Animation System

## Overview

The Text Fade Animation System provides smooth, configurable text fade effects for DungeonFighter-v2. It integrates seamlessly with the existing color system to create dynamic visual effects where text fades from the screen using various patterns and color progressions.

## Features

- **Multiple Fade Patterns**: Alternating, Sequential, Center Collapse/Expand, Uniform, Random
- **Color Integration**: Works with color templates and custom color progressions
- **Configurable Speed**: Adjustable frame count and delay between frames
- **Easy to Use**: Simple API with sensible defaults
- **Color System Integration**: Fully compatible with existing color markup and templates

## Architecture

### Core Component

```
Code/UI/
├── TextFadeAnimator.cs          # Main fade animation engine
└── TextFadeAnimatorExamples.cs  # Demonstrations and examples
```

### Integration Points

- **ColorParser**: Parses color markup in text
- **ColoredConsoleWriter**: Renders colored text to console
- **ColorTemplate**: Provides color template patterns for fade effects
- **ColorDefinitions**: Color code definitions for fade progressions

## Fade Patterns

### 1. Alternating Pattern

Every other letter fades first, then the remaining letters fade.

**Use Case**: General fade effect, visually interesting for short to medium text

```csharp
TextFadeAnimator.FadeOutAlternating("The enemy has been defeated!", frames: 6, delayMs: 150);
```

**Visual Example**:
```
Frame 1: The enemy has been defeated!
Frame 2: Thd dnemy has bddn ddfdatdd!  (every other letter dims)
Frame 3: The enemy has been defeated!  (all dim)
Frame 4: Th nmy hs bn dftd!            (every other fades more)
Frame 5: T  m  h  b  d f t d!           (continuing fade)
Frame 6: (cleared)
```

### 2. Sequential Pattern

Letters fade from left to right in order.

**Use Case**: Text appearing to be "erased" or "consumed" from left to right

```csharp
TextFadeAnimator.FadeOut("Experience gained: 150 XP", new TextFadeAnimator.FadeConfig
{
    Pattern = TextFadeAnimator.FadePattern.Sequential,
    Direction = TextFadeAnimator.FadeDirection.ToDark,
    TotalFrames = 8,
    FrameDelayMs = 80
});
```

### 3. Center Collapse Pattern

Letters fade from outside edges toward the center.

**Use Case**: Portal closing, something being crushed or compressed

```csharp
TextFadeAnimator.FadeOut("The portal closes before you", new TextFadeAnimator.FadeConfig
{
    Pattern = TextFadeAnimator.FadePattern.CenterCollapse,
    Direction = TextFadeAnimator.FadeDirection.ToDark,
    TotalFrames = 7,
    FrameDelayMs = 100
});
```

### 4. Center Expand Pattern

Letters fade from center toward the outside edges.

**Use Case**: Explosion effect, something spreading outward, darkness spreading

```csharp
TextFadeAnimator.FadeOut("The darkness spreads outward", new TextFadeAnimator.FadeConfig
{
    Pattern = TextFadeAnimator.FadePattern.CenterExpand,
    Direction = TextFadeAnimator.FadeDirection.ToDark,
    TotalFrames = 7,
    FrameDelayMs = 100
});
```

### 5. Uniform Pattern

All letters fade at the same rate together.

**Use Case**: General fading, buff/debuff expiration, simple transitions

```csharp
TextFadeAnimator.FadeOut("Your protection fades...", new TextFadeAnimator.FadeConfig
{
    Pattern = TextFadeAnimator.FadePattern.Uniform,
    Direction = TextFadeAnimator.FadeDirection.ToDark,
    TotalFrames = 4,
    FrameDelayMs = 150
});
```

### 6. Random Pattern

Letters fade in random order.

**Use Case**: Chaotic effects, disintegration, scrambling

```csharp
TextFadeAnimator.FadeOut("Reality fragments around you", new TextFadeAnimator.FadeConfig
{
    Pattern = TextFadeAnimator.FadePattern.Random,
    Direction = TextFadeAnimator.FadeDirection.ToDark,
    TotalFrames = 6,
    FrameDelayMs = 100
});
```

## Fade Directions

### ToDark (Default)

Text fades from bright to dark: `Y → y → K → k` (White → Grey → Dark Grey → Very Dark)

```csharp
Direction = TextFadeAnimator.FadeDirection.ToDark
```

### ToBright

Text fades from dark to bright: `k → K → y → Y` (Very Dark → Dark Grey → Grey → White)

```csharp
Direction = TextFadeAnimator.FadeDirection.ToBright
```

### TemplateProgression

Text fades through a color template's color sequence.

```csharp
Direction = TextFadeAnimator.FadeDirection.TemplateProgression,
ColorTemplate = "fiery"  // Uses fiery template colors
```

## Usage Examples

### Quick Fade

```csharp
// Simple alternating fade with defaults
TextFadeAnimator.FadeOutAlternating("The enemy has been defeated!");

// With custom parameters
TextFadeAnimator.FadeOutAlternating("Victory!", frames: 6, delayMs: 150);
```

### Template-Based Fade

```csharp
// Fade through fiery colors
TextFadeAnimator.FadeOutWithTemplate("Burning flames diminish", "fiery");

// Fade through icy colors with sequential pattern
TextFadeAnimator.FadeOutWithTemplate("Frozen solid melts away", "icy", 
    TextFadeAnimator.FadePattern.Sequential);

// Fade through shadow colors with center collapse
TextFadeAnimator.FadeOutWithTemplate("Fading into shadows", "shadow", 
    TextFadeAnimator.FadePattern.CenterCollapse);
```

### Custom Color Progression

```csharp
// Blood effect: Red to dark red to black
TextFadeAnimator.FadeOutCustom(
    "Bloodstained weapon",
    new List<char> { 'R', 'r', 'K', 'k' },
    TextFadeAnimator.FadePattern.Alternating
);

// Gold effect: Gold to brown to dark
TextFadeAnimator.FadeOutCustom(
    "Golden treasure vanishes",
    new List<char> { 'W', 'O', 'w', 'K' },
    TextFadeAnimator.FadePattern.Sequential
);

// Rainbow effect
TextFadeAnimator.FadeOutCustom(
    "Magical rainbow effect",
    new List<char> { 'R', 'O', 'W', 'G', 'B', 'M', 'k' },
    TextFadeAnimator.FadePattern.CenterExpand
);
```

### Full Configuration

```csharp
TextFadeAnimator.FadeOut("Custom fade effect", new TextFadeAnimator.FadeConfig
{
    Pattern = TextFadeAnimator.FadePattern.Alternating,
    Direction = TextFadeAnimator.FadeDirection.ToDark,
    ColorTemplate = null,           // Optional template name
    FrameDelayMs = 100,             // Delay between frames
    TotalFrames = 5,                // Number of animation frames
    ClearAfterFade = true           // Clear line when done
});
```

## Practical Game Integration

### Combat System

```csharp
// Enemy defeated
var defeatedText = ColorParser.Colorize("Goblin", "enemy") + " has been " + 
                  ColorParser.Colorize("defeated", "death") + "!";
TextFadeAnimator.FadeOut(defeatedText, new TextFadeAnimator.FadeConfig
{
    Pattern = TextFadeAnimator.FadePattern.Alternating,
    TotalFrames = 5,
    FrameDelayMs = 120
});

// Critical hit announcement
var critText = ColorParser.Colorize("CRITICAL HIT", "critical") + "!";
TextFadeAnimator.FadeOutWithTemplate(critText, "fiery", 
    TextFadeAnimator.FadePattern.CenterExpand);

// Miss message
var missText = ColorParser.Colorize("MISS", "miss") + "!";
TextFadeAnimator.FadeOut(missText, new TextFadeAnimator.FadeConfig
{
    Pattern = TextFadeAnimator.FadePattern.Random,
    TotalFrames = 3,
    FrameDelayMs = 80
});
```

### Status Effects

```csharp
// Buff expiration
var buffText = "Your " + ColorParser.Colorize("holy", "holy") + " protection fades...";
TextFadeAnimator.FadeOut(buffText, new TextFadeAnimator.FadeConfig
{
    Pattern = TextFadeAnimator.FadePattern.Uniform,
    Direction = TextFadeAnimator.FadeDirection.ToDark,
    TotalFrames = 4,
    FrameDelayMs = 150
});

// Poison effect ending
var poisonText = ColorParser.Colorize("Poison", "poisoned") + " wears off";
TextFadeAnimator.FadeOutWithTemplate(poisonText, "toxic", 
    TextFadeAnimator.FadePattern.Sequential);

// Burn effect ending
var burnText = ColorParser.Colorize("Burning", "burning") + " flames extinguish";
TextFadeAnimator.FadeOutWithTemplate(burnText, "fiery", 
    TextFadeAnimator.FadePattern.CenterExpand);
```

### Environment Effects

```csharp
// Portal closing
TextFadeAnimator.FadeOut("The portal closes before you", new TextFadeAnimator.FadeConfig
{
    Pattern = TextFadeAnimator.FadePattern.CenterCollapse,
    TotalFrames = 7,
    FrameDelayMs = 100
});

// Darkness spreading
TextFadeAnimator.FadeOutWithTemplate("The darkness spreads", "shadow", 
    TextFadeAnimator.FadePattern.CenterExpand);

// Light fading
var lightText = ColorParser.Colorize("Light", "holy") + " diminishes...";
TextFadeAnimator.FadeOut(lightText, new TextFadeAnimator.FadeConfig
{
    Pattern = TextFadeAnimator.FadePattern.Uniform,
    TotalFrames = 5,
    FrameDelayMs = 120
});
```

### Quest and Achievement

```csharp
// Quest completion
var questText = ColorParser.Colorize("Quest Complete", "golden") + ": The Ancient Ruins";
TextFadeAnimator.FadeOut(questText, new TextFadeAnimator.FadeConfig
{
    Pattern = TextFadeAnimator.FadePattern.Sequential,
    TotalFrames = 6,
    FrameDelayMs = 100
});

// Achievement unlocked (fade in then fade out)
var achievementText = ColorParser.Colorize("Achievement Unlocked", "legendary") + "!";
// First display normally
UIManager.WriteLine(achievementText);
System.Threading.Thread.Sleep(2000); // Show for 2 seconds
// Then fade out
TextFadeAnimator.FadeOutWithTemplate(achievementText, "golden", 
    TextFadeAnimator.FadePattern.CenterCollapse);
```

## Configuration Reference

### FadeConfig Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Pattern` | `FadePattern` | `Alternating` | The pattern in which letters fade |
| `Direction` | `FadeDirection` | `ToDark` | Direction of color progression |
| `ColorTemplate` | `string?` | `null` | Optional color template to use |
| `FrameDelayMs` | `int` | `100` | Milliseconds between frames |
| `TotalFrames` | `int` | `5` | Number of animation frames |
| `ClearAfterFade` | `bool` | `true` | Clear line after animation |

### Performance Considerations

- **Frame Count**: 3-8 frames recommended for smooth animation
- **Frame Delay**: 50-200ms recommended (too fast is jarring, too slow is boring)
- **Text Length**: Works best with text under 80 characters
- **Color Markup**: Automatically stripped before animation

### Optimization Tips

```csharp
// Fast fade for frequent messages
new TextFadeAnimator.FadeConfig
{
    TotalFrames = 3,
    FrameDelayMs = 50
};

// Slow, dramatic fade for important events
new TextFadeAnimator.FadeConfig
{
    TotalFrames = 8,
    FrameDelayMs = 150
};

// Skip animation in fast mode
if (gameSettings.FastMode)
{
    UIManager.WriteLine(text); // No fade
}
else
{
    TextFadeAnimator.FadeOut(text);
}
```

## Testing and Examples

### Run All Demonstrations

```csharp
// Run all example demonstrations
TextFadeAnimatorExamples.RunAllDemos();
```

### Individual Demonstrations

```csharp
// Test specific patterns
TextFadeAnimatorExamples.DemoAlternatingFade();
TextFadeAnimatorExamples.DemoSequentialFade();
TextFadeAnimatorExamples.DemoCenterCollapseFade();
TextFadeAnimatorExamples.DemoCenterExpandFade();
TextFadeAnimatorExamples.DemoTemplateFade();
TextFadeAnimatorExamples.DemoCustomColorProgression();
TextFadeAnimatorExamples.DemoCombatUsage();
```

### Interactive Testing

```csharp
// Interactive test with user input
TextFadeAnimatorExamples.InteractiveDemo();
```

## Best Practices

### 1. Choose Appropriate Patterns

```csharp
// Good: Alternating for general fades
TextFadeAnimator.FadeOutAlternating("Standard message");

// Good: Sequential for "disappearing" text
TextFadeAnimator.FadeOut("Fading away...", new TextFadeAnimator.FadeConfig
{
    Pattern = TextFadeAnimator.FadePattern.Sequential
});

// Good: Center Collapse for closing/compressing effects
TextFadeAnimator.FadeOut("Portal closes", new TextFadeAnimator.FadeConfig
{
    Pattern = TextFadeAnimator.FadePattern.CenterCollapse
});
```

### 2. Match Speed to Content

```csharp
// Fast fade for routine messages
TextFadeAnimator.FadeOutAlternating("Enemy hit", frames: 3, delayMs: 60);

// Slow fade for dramatic moments
TextFadeAnimator.FadeOutAlternating("The dragon falls...", frames: 8, delayMs: 180);
```

### 3. Use Templates for Themed Effects

```csharp
// Fire-related text
TextFadeAnimator.FadeOutWithTemplate("Flames die down", "fiery");

// Ice-related text
TextFadeAnimator.FadeOutWithTemplate("Ice melts away", "icy");

// Shadow-related text
TextFadeAnimator.FadeOutWithTemplate("Darkness fades", "shadow");
```

### 4. Preserve Color Markup

```csharp
// Color markup is automatically handled
var coloredText = ColorParser.Colorize("Critical Hit", "critical");
TextFadeAnimator.FadeOut(coloredText); // Colors preserved during fade
```

### 5. Don't Overuse

```csharp
// Good: Fade important messages
if (isImportantMessage)
{
    TextFadeAnimator.FadeOut(message);
}
else
{
    UIManager.WriteLine(message);
}

// Bad: Fading every message (too slow)
TextFadeAnimator.FadeOut(everyMessage); // Don't do this
```

## Integration with Game Settings

```csharp
// Respect game settings for animation speed
public static void FadeOutWithSettings(string text, GameSettings settings)
{
    if (settings.AnimationsEnabled)
    {
        var config = new TextFadeAnimator.FadeConfig
        {
            Pattern = TextFadeAnimator.FadePattern.Alternating,
            TotalFrames = settings.AnimationSpeed switch
            {
                "slow" => 8,
                "normal" => 5,
                "fast" => 3,
                _ => 5
            },
            FrameDelayMs = settings.AnimationSpeed switch
            {
                "slow" => 150,
                "normal" => 100,
                "fast" => 50,
                _ => 100
            }
        };
        TextFadeAnimator.FadeOut(text, config);
    }
    else
    {
        UIManager.WriteLine(text);
    }
}
```

## Future Enhancements

Potential additions:
- **Fade In**: Reverse fade effect (text appearing)
- **Wave Effects**: Letters oscillate while fading
- **Bounce Effects**: Letters bounce before fading
- **Color Cycling**: Cycle through colors before fading
- **Canvas Integration**: Support for Avalonia UI canvas
- **Sound Integration**: Tie fades to sound effects
- **Async Animation**: Non-blocking fade animations

## Related Documentation

- **COLOR_SYSTEM.md** - Color system integration
- **UI_SYSTEM.md** - UI system documentation
- **ARCHITECTURE.md** - Overall system architecture

---

*The Text Fade Animation System provides dynamic visual feedback while maintaining performance and ease of use.*

