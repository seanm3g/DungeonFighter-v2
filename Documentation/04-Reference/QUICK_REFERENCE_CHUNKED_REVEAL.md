# Quick Reference - Chunked Text Reveal

## Common Usage

### Quick Methods

```csharp
// Sentence-based (default)
ChunkedTextReveal.RevealBySentences(text);

// Dungeon text (optimized - loads from TextDelayConfig.json)
UIManager.WriteDungeonChunked(text);

// Room description (optimized - loads from TextDelayConfig.json)
UIManager.WriteRoomChunked(text);

// Custom configuration
UIManager.WriteChunked(text, new RevealConfig { /* ... */ });
```

### Configuration

All delay settings are configurable via `GameData/TextDelayConfig.json`. The presets (Combat, Dungeon, Room, Narrative) are automatically loaded from this file.

### Chunking Strategies

| Strategy | Best For | Example |
|----------|----------|---------|
| `Sentence` | Room descriptions, narrative | "First. Second. Third." |
| `Paragraph` | Long multi-paragraph text | "Para 1\n\nPara 2" |
| `Line` | Stats, lists, combat info | "Line 1\nLine 2\nLine 3" |
| `Semantic` | Complex structured text | Headers + stats + info |

### Configuration Presets

```csharp
// Fast (Combat)
new RevealConfig
{
    BaseDelayPerCharMs = 20,
    MinDelayMs = 500,
    MaxDelayMs = 2000,
    Strategy = ChunkStrategy.Line
}

// Normal (Exploration)
new RevealConfig
{
    BaseDelayPerCharMs = 30,
    MinDelayMs = 1000,
    MaxDelayMs = 3000,
    Strategy = ChunkStrategy.Sentence
}

// Slow (Story)
new RevealConfig
{
    BaseDelayPerCharMs = 35,
    MinDelayMs = 1500,
    MaxDelayMs = 4000,
    Strategy = ChunkStrategy.Paragraph
}
```

### Disable Chunked Reveal

```csharp
var config = new RevealConfig { Enabled = false };
ChunkedTextReveal.RevealText(text, config);
// Text displays instantly
```

## Delay Calculation

**Formula**: `delay = min(max(displayLength * baseDelayPerChar, minDelay), maxDelay)`

**Example**:
- Text: "Hello world" (11 characters)
- BaseDelayPerCharMs: 30
- MinDelayMs: 500
- MaxDelayMs: 4000
- **Calculated**: 11 * 30 = 330ms → **Clamped to 500ms** (min)

## Integration Examples

### Dungeon Entry
```csharp
UIManager.WriteDungeonChunked(
    $"==== ENTERING DUNGEON ====\n\n" +
    $"Dungeon: {name}\n" +
    $"Level Range: {min} - {max}\n" +
    $"Total Rooms: {count}"
);
```

### Room Description
```csharp
UIManager.WriteRoomChunked(
    $"Entering room: {roomName}\n\n{description}"
);
```

### Custom Text
```csharp
UIManager.WriteChunked(text, new RevealConfig
{
    Strategy = ChunkStrategy.Line,
    BaseDelayPerCharMs = 25,
    MinDelayMs = 800,
    MaxDelayMs = 3000
});
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Too fast | Increase `BaseDelayPerCharMs` or `MinDelayMs` |
| Too slow | Decrease `BaseDelayPerCharMs` or `MaxDelayMs` |
| Not chunking | Check strategy matches text structure |
| Colors broken | Verify ColorParser is working |

## Common Patterns

### Conditional Reveal
```csharp
if (settings.EnableChunkedReveal)
{
    UIManager.WriteChunked(text, config);
}
else
{
    UIManager.WriteLine(text);
}
```

### Speed-Based Configuration
```csharp
var config = new RevealConfig
{
    BaseDelayPerCharMs = settings.TextSpeed switch
    {
        "slow" => 40,
        "normal" => 30,
        "fast" => 20,
        _ => 30
    }
};
```

### Context-Aware Delays
```csharp
var config = new RevealConfig();
if (inCombat)
{
    config.BaseDelayPerCharMs = 20;
    config.MaxDelayMs = 2000;
}
else
{
    config.BaseDelayPerCharMs = 30;
    config.MaxDelayMs = 3000;
}
```

## See Also

- **CHUNKED_TEXT_REVEAL.md** - Full documentation
- **TEXT_FADE_ANIMATION_SYSTEM.md** - Text fade animations
- **COLOR_SYSTEM.md** - Color markup system

