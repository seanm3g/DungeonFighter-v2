# Chunked Text Reveal System

## Overview

The Chunked Text Reveal System provides progressive text display where long blocks of text are revealed chunk by chunk with delays proportional to the length of each chunk. This creates a more engaging reading experience for dungeon exploration, room descriptions, and combat encounters.

## Purpose

Traditional text systems display entire paragraphs instantly, which can be overwhelming and reduce engagement. The chunked reveal system:
- **Paces Information**: Presents text in digestible chunks
- **Creates Anticipation**: Builds suspense through timed reveals
- **Enhances Immersion**: Mimics natural story-telling rhythm
- **Proportional Timing**: Longer chunks get longer delays, maintaining natural reading pace
- **Maintains Engagement**: Players stay focused on unfolding narrative

## Features

### Multiple Chunking Strategies

1. **Sentence-based**: Splits text by sentences (periods, question marks, exclamation points)
   - Best for: Room descriptions, narrative text
   - Example: "The room is dark. Shadows dance on the walls. You hear something move."
   
2. **Paragraph-based**: Splits by double line breaks
   - Best for: Long descriptions with multiple paragraphs
   - Example: Multiple paragraphs of dungeon lore
   
3. **Line-based**: Splits by single line breaks
   - Best for: Lists, stats, combat information
   - Example: Enemy stats displayed line by line
   
4. **Semantic**: Splits by semantic sections (headers, stats blocks, separators)
   - Best for: Complex information with multiple sections
   - Example: Dungeon entry with name, level range, and room count

### Proportional Delays

The system calculates delays based on chunk length:
- **Base Delay**: Configurable milliseconds per character (default: 30ms)
- **Min Delay**: Minimum delay between chunks (default: 500ms)
- **Max Delay**: Maximum delay between chunks (default: 4000ms)

**Formula**: `delay = min(max(displayLength * baseDelayPerChar, minDelay), maxDelay)`

### Color System Integration

Chunked reveal fully supports the existing color markup system:
- Color codes are preserved during chunking
- Display length calculations exclude markup characters
- Visual effects maintained throughout reveal

## Architecture

### Core Component

```
Code/UI/ChunkedTextReveal.cs
```

The main static class providing all chunked reveal functionality.

### Key Classes

```csharp
// Configuration class
public class RevealConfig
{
    public int BaseDelayPerCharMs { get; set; } = 30;
    public int MinDelayMs { get; set; } = 500;
    public int MaxDelayMs { get; set; } = 4000;
    public ChunkStrategy Strategy { get; set; } = ChunkStrategy.Sentence;
    public bool AddBlankLineBetweenChunks { get; set; } = false;
    public bool Enabled { get; set; } = true;
}

// Chunking strategies
public enum ChunkStrategy
{
    Sentence,    // Split by sentence-ending punctuation
    Paragraph,   // Split by double newlines
    Line,        // Split by single newlines
    Semantic     // Split by semantic sections
}
```

### Integration Points

- **UIManager**: Static methods for chunked output
- **IUIManager**: Interface for custom UI implementations
- **CanvasUIManager**: GUI-specific chunked reveal
- **DungeonRunner**: Uses chunked reveal for dungeon/room descriptions

## Usage

### Quick Methods

```csharp
// Use default sentence-based chunking
ChunkedTextReveal.RevealBySentences(text);

// Use paragraph-based chunking
ChunkedTextReveal.RevealByParagraphs(text);

// Use line-based chunking
ChunkedTextReveal.RevealByLines(text);

// Use semantic chunking
ChunkedTextReveal.RevealBySemantic(text);
```

### Custom Configuration

```csharp
// Create custom configuration
var config = new ChunkedTextReveal.RevealConfig
{
    Strategy = ChunkedTextReveal.ChunkStrategy.Sentence,
    BaseDelayPerCharMs = 25,
    MinDelayMs = 800,
    MaxDelayMs = 3000,
    AddBlankLineBetweenChunks = false,
    Enabled = true
};

// Reveal with custom config
ChunkedTextReveal.RevealText(text, config);
```

### Through UIManager

```csharp
// Generic chunked reveal
UIManager.WriteChunked(text, config);

// Optimized for dungeon text
UIManager.WriteDungeonChunked(text);

// Optimized for room descriptions
UIManager.WriteRoomChunked(text);
```

### In Game Code

```csharp
// Dungeon entry (from DungeonRunner)
UIManager.WriteDungeonChunked($"==== ENTERING DUNGEON ====\n\n" +
    $"Dungeon: {dungeonName}\n" +
    $"Level Range: {minLevel} - {maxLevel}\n" +
    $"Total Rooms: {roomCount}");

// Room description
UIManager.WriteRoomChunked($"Entering room: {roomName}\n\n{description}");

// Enemy encounter
UIManager.WriteChunked(encounterText, new RevealConfig
{
    Strategy = ChunkStrategy.Line,
    BaseDelayPerCharMs = 20,
    MinDelayMs = 600,
    MaxDelayMs = 2000
});
```

## Practical Examples

### Dungeon Entry

**Before (instant display)**:
```
Entering Mountain Summit...
```

**After (chunked reveal)**:
```
==== ENTERING DUNGEON ====

Dungeon: Mountain Summit
[800ms delay]
Level Range: 5 - 5
[800ms delay]
Total Rooms: 3
```

### Room Description

**Before (instant display)**:
```
Entering room: Rocky Outcrop
Jagged rocks and boulders create a treacherous landscape of stone and shadow.
```

**After (chunked reveal)**:
```
Entering room: Rocky Outcrop

[1200ms delay]

Jagged rocks and boulders create a treacherous landscape of stone and shadow.
```

### Enemy Encounter

**Before (instant display)**:
```
Encountered [Rock Elemental] with Iron Staff!
Enemy Stats - Health: 69/69, Armor: 1
 Attack: STR 11, AGI 10, TEC 10, INT 10
```

**After (chunked reveal)**:
```
Encountered [Rock Elemental] with Iron Staff!

[800ms delay]

Enemy Stats - Health: 69/69, Armor: 1
[700ms delay]
 Attack: STR 11, AGI 10, TEC 10, INT 10
```

## Configuration Reference

### RevealConfig Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `BaseDelayPerCharMs` | `int` | 30 | Milliseconds delay per character |
| `MinDelayMs` | `int` | 500 | Minimum delay between chunks |
| `MaxDelayMs` | `int` | 4000 | Maximum delay between chunks |
| `Strategy` | `ChunkStrategy` | `Sentence` | Chunking strategy to use |
| `AddBlankLineBetweenChunks` | `bool` | `false` | Add blank line between chunks |
| `Enabled` | `bool` | `true` | Enable/disable chunked reveal |

### Recommended Settings

#### Fast-Paced Action (Combat)
```csharp
new RevealConfig
{
    BaseDelayPerCharMs = 20,
    MinDelayMs = 500,
    MaxDelayMs = 2000,
    Strategy = ChunkStrategy.Line
}
```

#### Standard Exploration (Rooms)
```csharp
new RevealConfig
{
    BaseDelayPerCharMs = 30,
    MinDelayMs = 1000,
    MaxDelayMs = 3000,
    Strategy = ChunkStrategy.Sentence
}
```

#### Story-Heavy (Narrative)
```csharp
new RevealConfig
{
    BaseDelayPerCharMs = 35,
    MinDelayMs = 1500,
    MaxDelayMs = 4000,
    Strategy = ChunkStrategy.Paragraph
}
```

#### Information-Dense (Stats/Lists)
```csharp
new RevealConfig
{
    BaseDelayPerCharMs = 25,
    MinDelayMs = 800,
    MaxDelayMs = 3000,
    Strategy = ChunkStrategy.Semantic
}
```

## Performance Considerations

### Best Practices

1. **Chunk Size**: Keep chunks to 1-3 sentences for optimal pacing
2. **Delay Tuning**: Adjust delays based on gameplay context
3. **Disable Option**: Provide option to disable for speed runners
4. **Color Integration**: Color markup is automatically handled

### Optimization Tips

```csharp
// Disable for fast mode
if (settings.FastMode)
{
    config.Enabled = false;
}

// Reduce delays for combat
if (inCombat)
{
    config.BaseDelayPerCharMs = 15;
    config.MaxDelayMs = 1500;
}

// Increase delays for story moments
if (isStoryMoment)
{
    config.BaseDelayPerCharMs = 40;
    config.MinDelayMs = 2000;
}
```

### Memory Usage

- Minimal overhead: Text is split into List<string> once
- No persistent state between reveals
- Thread.Sleep used for delays (blocking but simple)

### Threading Considerations

- Current implementation uses Thread.Sleep (blocking)
- Works well for single-threaded game loop
- For async scenarios, consider Task.Delay alternative

## Advanced Usage

### Custom Chunking Logic

You can implement custom chunking strategies by extending the enum and adding cases to the switch statement:

```csharp
// In ChunkedTextReveal.cs
public enum ChunkStrategy
{
    Sentence,
    Paragraph,
    Line,
    Semantic,
    Custom  // Your custom strategy
}

// Add custom splitting logic
case ChunkStrategy.Custom:
    chunks = MyCustomSplitLogic(text);
    break;
```

### Dynamic Configuration

Adjust configuration based on runtime conditions:

```csharp
var config = new RevealConfig();

// Adjust for text length
if (text.Length > 500)
{
    config.Strategy = ChunkStrategy.Paragraph;
}
else if (text.Length > 200)
{
    config.Strategy = ChunkStrategy.Sentence;
}
else
{
    config.Strategy = ChunkStrategy.Line;
}

// Adjust for player preferences
config.BaseDelayPerCharMs = settings.TextSpeed switch
{
    "slow" => 40,
    "normal" => 30,
    "fast" => 20,
    _ => 30
};
```

### Integration with Other Systems

```csharp
// Combine with color system
var coloredText = ColorParser.Colorize("Important Message", "warning");
ChunkedTextReveal.RevealText(coloredText, config);

// Combine with sound effects
ChunkedTextReveal.RevealText(text, config);
SoundManager.PlayEffect("reveal_complete");

// Combine with animations
ChunkedTextReveal.RevealText(text, config);
AnimationManager.PlayEffect("text_glow");
```

## Testing

### Manual Testing

Run the game and observe:
1. Dungeon entry messages appear chunk by chunk
2. Room descriptions reveal progressively
3. Enemy encounters display stats in sequence
4. Delays feel natural and proportional to text length

### Configuration Testing

Test different configurations to find optimal settings:
```csharp
// Test sentence chunking
ChunkedTextReveal.RevealBySentences("First sentence. Second sentence. Third sentence.");

// Test with short text
ChunkedTextReveal.RevealText("Short", config);

// Test with long text
ChunkedTextReveal.RevealText(longRoomDescription, config);

// Test with color markup
ChunkedTextReveal.RevealText("{{fire|Burning}} text here", config);
```

## Troubleshooting

### Text Appears Too Fast
- Increase `BaseDelayPerCharMs`
- Increase `MinDelayMs`
- Check that `Enabled` is true

### Text Appears Too Slow
- Decrease `BaseDelayPerCharMs`
- Decrease `MaxDelayMs`
- Consider faster strategy (Line instead of Sentence)

### Chunks Not Splitting Correctly
- Verify chunking strategy matches text structure
- Check for proper sentence-ending punctuation
- Try Semantic strategy for complex text

### Color Markup Not Working
- Verify ColorParser is functioning correctly
- Check that color codes are valid
- Ensure markup is preserved during chunking

## Future Enhancements

Potential additions:
- **Async Reveals**: Non-blocking async/await implementation
- **Skip Functionality**: Allow player to skip remaining chunks
- **Sound Integration**: Play sound effects between chunks
- **Visual Effects**: Add fade-in or typewriter effects
- **Adaptive Timing**: ML-based delay optimization based on player reading speed
- **Per-Chunk Callbacks**: Execute code after each chunk reveal
- **Batch Reveals**: Group multiple related chunks for faster pacing

## Related Documentation

- **TEXT_FADE_ANIMATION_SYSTEM.md** - Text fade animation system
- **COLOR_SYSTEM.md** - Color markup integration
- **UI_SYSTEM.md** - UI system documentation
- **ARCHITECTURE.md** - Overall system architecture

## Changelog

### Version 1.0 (October 2025)
- Initial implementation
- Four chunking strategies (Sentence, Paragraph, Line, Semantic)
- Proportional delay system
- Color markup integration
- UIManager integration
- DungeonRunner integration

---

*The Chunked Text Reveal System enhances player engagement through natural, paced text delivery while maintaining performance and ease of use.*

