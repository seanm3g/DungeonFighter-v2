# Chunked Text Reveal Feature

## What's New? 📖

Dungeon exploration text now appears **chunk by chunk** with natural timing! Instead of walls of text appearing instantly, messages are revealed progressively, creating a more engaging and immersive experience.

## What You'll See

When exploring dungeons:

### Dungeon Entry
```
==== ENTERING DUNGEON ====

Dungeon: Mountain Summit
[pause...]
Level Range: 5 - 5
[pause...]
Total Rooms: 3
```

### Room Descriptions
```
Entering room: Rocky Outcrop

[pause...]

Jagged rocks and boulders create a treacherous landscape of stone and shadow.
```

### Enemy Encounters
```
Encountered [Rock Elemental] with Iron Staff!

[pause...]

Enemy Stats - Health: 69/69, Armor: 1
[pause...]
 Attack: STR 11, AGI 10, TEC 10, INT 10
```

## Key Features

✅ **Progressive Reveal**: Text appears chunk by chunk  
✅ **Natural Pacing**: Longer text gets longer pauses (proportional timing)  
✅ **Smart Chunking**: Automatically splits by sentences, lines, or sections  
✅ **Color Support**: Full integration with existing color system  
✅ **Performance**: Minimal overhead, smooth gameplay  

## How It Works

The system:
1. **Splits** text into logical chunks (sentences, paragraphs, or lines)
2. **Calculates** delays based on chunk length (longer = longer pause)
3. **Reveals** each chunk sequentially with natural timing
4. **Maintains** all color markup and formatting

**Delay Formula**: `30ms per character, min 500ms, max 4000ms`

Example:
- Short chunk (20 chars): 600ms pause
- Medium chunk (60 chars): 1800ms pause  
- Long chunk (150 chars): 3000ms pause (capped)

## Try It Now!

### Run the Game
```bash
dotnet run --project Code/Code.csproj
```
or
```bash
DF.exe
```

### Start a Dungeon
1. Create or load a character
2. Select "Enter Dungeon" from main menu
3. Choose any dungeon
4. **Watch** as text reveals chunk by chunk!

## Configuration Options

### For Developers

You can customize the reveal behavior:

```csharp
// Fast reveal (combat)
UIManager.WriteChunked(text, new RevealConfig
{
    BaseDelayPerCharMs = 20,  // Faster
    MinDelayMs = 500,
    MaxDelayMs = 2000
});

// Slow reveal (story)
UIManager.WriteChunked(text, new RevealConfig
{
    BaseDelayPerCharMs = 40,  // Slower
    MinDelayMs = 1500,
    MaxDelayMs = 4000
});

// Disable chunked reveal
UIManager.WriteChunked(text, new RevealConfig
{
    Enabled = false  // Instant display
});
```

### Chunking Strategies

| Strategy | Use Case | Example |
|----------|----------|---------|
| **Sentence** | Room descriptions | "First sentence. Second sentence." |
| **Paragraph** | Long text | Multiple paragraphs |
| **Line** | Stats/Lists | Line-by-line reveal |
| **Semantic** | Structured text | Headers + content |

## Integration

The chunked reveal is automatically used for:
- ✅ Dungeon entry messages
- ✅ Room descriptions
- ✅ Enemy encounters
- ✅ Combat narrative (can be customized)
- ✅ All dungeon exploration text

## Performance

- **Overhead**: Minimal (text split once, simple delays)
- **Memory**: Very low (temporary List<string>)
- **Compatibility**: Works with console and GUI modes
- **Color System**: Fully integrated

## Customization

Want different timing? Edit `GameData/TextDelayConfig.json`:

```json
{
  "ChunkedTextReveal": {
    "Combat": {
      "BaseDelayPerCharMs": 20,
      "MinDelayMs": 500,
      "MaxDelayMs": 2000,
      "Strategy": "Line"
    },
    "Dungeon": {
      "BaseDelayPerCharMs": 25,
      "MinDelayMs": 800,
      "MaxDelayMs": 3000,
      "Strategy": "Semantic"
    }
  }
}
```

**Or** use programmatic configuration:

```csharp
ChunkedTextReveal.DefaultConfig = new RevealConfig
{
    BaseDelayPerCharMs = 25,  // Your preferred speed
    MinDelayMs = 800,
    MaxDelayMs = 3500
};
```

## Examples

### Default Behavior (Dungeon Text)
```csharp
// Automatically chunks by semantic sections with optimal timing
UIManager.WriteDungeonChunked(dungeonInfo);
```

### Custom Speed (Combat)
```csharp
// Fast chunking for action sequences
UIManager.WriteChunked(combatText, new RevealConfig
{
    Strategy = ChunkStrategy.Line,
    BaseDelayPerCharMs = 20,  // Fast
    MinDelayMs = 600,
    MaxDelayMs = 2000
});
```

### Disable for Speed Run
```csharp
// Instant text display
UIManager.WriteChunked(text, new RevealConfig
{
    Enabled = false
});
```

## Comparison

### Before (Instant Display)
```
Entering Mountain Summit...
Room: Rocky Outcrop
Jagged rocks and boulders create a treacherous landscape.
Encountered [Rock Elemental]!
Enemy Stats - Health: 69/69, Armor: 1
[All appears at once]
```

### After (Chunked Reveal)
```
Entering Mountain Summit...
[pause]
Room: Rocky Outcrop
[pause]
Jagged rocks and boulders create a treacherous landscape.
[pause]
Encountered [Rock Elemental]!
[pause]
Enemy Stats - Health: 69/69, Armor: 1
[Natural reading rhythm]
```

## Technical Details

**Architecture**:
- `ChunkedTextReveal.cs`: Core reveal logic
- `UIManager`: Integration and convenience methods
- `DungeonRunner`: Uses chunked reveal for exploration
- `CanvasUIManager`: GUI-specific implementation

**Strategies**:
- **Sentence**: Splits on `.`, `!`, `?` followed by space/newline
- **Paragraph**: Splits on double newlines (`\n\n`)
- **Line**: Splits on single newlines (`\n`)
- **Semantic**: Intelligent splitting by headers, stats, sections

## Documentation

- **Full Documentation**: `Documentation/05-Systems/CHUNKED_TEXT_REVEAL.md`
- **Quick Reference**: `Documentation/04-Reference/QUICK_REFERENCE_CHUNKED_REVEAL.md`
- **Architecture**: `Documentation/01-Core/ARCHITECTURE.md`

## Benefits

🎮 **Better Gameplay**: Text is easier to read and follow  
📖 **Enhanced Immersion**: Natural story-telling rhythm  
⚡ **Maintains Pace**: Doesn't slow down gameplay  
🎨 **Visual Appeal**: Creates anticipation and engagement  
🔧 **Configurable**: Adjust to your preference  

## Feedback

The chunked reveal system is designed to enhance your dungeon exploration experience. If you find the timing too fast or slow, adjust the configuration values to match your reading speed!

---

**Feature**: Chunked Text Reveal System  
**Version**: 1.0  
**Status**: ✅ Ready to Use  
**Build**: ✅ Passing  
**Documentation**: ✅ Complete

Enjoy your enhanced dungeon exploration! 🏰✨

