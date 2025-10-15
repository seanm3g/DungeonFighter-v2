# Testing Chunked Text Reveal

## How to Test

### 1. Build the Project

```bash
dotnet build Code/Code.csproj
```

### 2. Run the Game

```bash
dotnet run --project Code/Code.csproj
```

Or use the executable:
```bash
DF.exe
```

### 3. Test Scenarios

#### Scenario 1: Dungeon Entry
1. Start the game
2. Create or load a character
3. Select "Enter Dungeon" from main menu
4. Choose any dungeon
5. **Expected**: Dungeon info appears chunk by chunk with pauses

**What to Look For**:
- Header appears first
- Short pause (~800ms)
- Dungeon name appears
- Short pause (~800ms)
- Level range appears
- Short pause (~800ms)
- Room count appears

#### Scenario 2: Room Description
1. Continue from Scenario 1
2. Enter first room
3. **Expected**: Room name and description appear in chunks

**What to Look For**:
- "Entering room: [Name]" appears first
- Longer pause (~1200ms)
- Room description appears
- Pauses are proportional to text length

#### Scenario 3: Enemy Encounter
1. Continue from Scenario 2
2. Wait for enemy encounter
3. **Expected**: Enemy info and stats appear chunk by chunk

**What to Look For**:
- Enemy name appears first
- Pause (~800ms)
- Enemy stats appear line by line
- Each stat line has proportional delay

#### Scenario 4: Combat Text
1. Continue from Scenario 3
2. Engage in combat
3. **Expected**: Combat messages flow naturally

**What to Look For**:
- Attack messages appear
- Status effects reveal progressively
- Narrative text has appropriate timing

## Visual Test Checklist

### Timing ✓
- [ ] Delays feel natural (not too fast, not too slow)
- [ ] Longer text has longer pauses
- [ ] Short text has minimum pause (500ms)
- [ ] Very long text caps at max pause (3-4 seconds)

### Chunking ✓
- [ ] Dungeon info splits by lines
- [ ] Room descriptions split by sentences
- [ ] Enemy stats split by lines
- [ ] No awkward mid-sentence breaks

### Formatting ✓
- [ ] Color markup is preserved
- [ ] Text alignment is correct
- [ ] No missing characters or corruption
- [ ] Blank lines appear appropriately

### Integration ✓
- [ ] GUI mode works correctly
- [ ] Console mode works correctly
- [ ] No crashes or errors
- [ ] Game remains responsive

## Expected Behavior

### Dungeon Entry Text
```
==== ENTERING DUNGEON ====
[800ms pause]
Dungeon: Mountain Summit
[800ms pause]
Level Range: 5 - 5
[800ms pause]
Total Rooms: 3
```

### Room Description
```
Entering room: Rocky Outcrop
[1200ms pause]
Jagged rocks and boulders create a treacherous landscape of stone and shadow.
```

### Enemy Encounter
```
Encountered [Rock Elemental] with Iron Staff!
[800ms pause]
Enemy Stats - Health: 69/69, Armor: 1
[700ms pause]
 Attack: STR 11, AGI 10, TEC 10, INT 10
```

## Troubleshooting

### Issue: Text Appears Too Fast
**Fix**: Increase delays in `ChunkedTextReveal.cs`:
```csharp
public int BaseDelayPerCharMs { get; set; } = 40;  // Increased from 30
public int MinDelayMs { get; set; } = 800;         // Increased from 500
```

### Issue: Text Appears Too Slow
**Fix**: Decrease delays in `ChunkedTextReveal.cs`:
```csharp
public int BaseDelayPerCharMs { get; set; } = 20;  // Decreased from 30
public int MaxDelayMs { get; set; } = 2500;        // Decreased from 4000
```

### Issue: Text Not Chunking
**Check**: Verify text contains proper punctuation or line breaks:
- Sentences should end with `.`, `!`, or `?`
- Paragraphs should be separated by double newlines
- Lines should be separated by single newlines

### Issue: Colors Not Showing
**Check**: Verify color markup is present in source text:
```csharp
// Example with color
var text = "{{fire|Burning}} text here";
```

## Performance Testing

### Memory Usage
Monitor memory usage during chunked reveals:
- Expected: Minimal increase (<5MB)
- No memory leaks after multiple reveals
- Memory returns to baseline after reveals complete

### Responsiveness
Check game remains responsive:
- Input is accepted during reveals
- No freezing or stuttering
- Smooth transitions between chunks

### CPU Usage
Monitor CPU during reveals:
- Expected: Normal game CPU usage
- Thread.Sleep is efficient and low-overhead
- No CPU spikes

## Regression Testing

Verify existing features still work:
- [ ] Normal text display (non-chunked) still works
- [ ] Color system functions correctly
- [ ] Combat system operates normally
- [ ] Inventory system works
- [ ] Menu navigation is responsive
- [ ] Save/load functionality intact

## Automated Testing

### Unit Test Ideas
```csharp
[Test]
public void ChunkedReveal_SplitsBySentences()
{
    var text = "First. Second. Third.";
    var chunks = ChunkedTextReveal.SplitIntoChunks(text, ChunkStrategy.Sentence);
    Assert.AreEqual(3, chunks.Count);
}

[Test]
public void ChunkedReveal_CalculatesDelayCorrectly()
{
    var config = new RevealConfig();
    var delay = ChunkedTextReveal.CalculateDelay("Test", config);
    Assert.GreaterOrEqual(delay, config.MinDelayMs);
    Assert.LessOrEqual(delay, config.MaxDelayMs);
}

[Test]
public void ChunkedReveal_PreservesColorMarkup()
{
    var text = "{{fire|Burning}} text";
    var chunks = ChunkedTextReveal.SplitIntoChunks(text, ChunkStrategy.Sentence);
    Assert.That(chunks[0], Does.Contain("{{fire|"));
}
```

## User Feedback

Ask testers to evaluate:
1. **Timing**: Does the text pace feel natural?
2. **Readability**: Is text easier or harder to read?
3. **Engagement**: Does it enhance immersion?
4. **Preference**: Would you prefer faster/slower/off?

## Configuration Tuning

Based on feedback, adjust:
- `BaseDelayPerCharMs`: 15-40 (20-30 recommended)
- `MinDelayMs`: 400-1000 (500-800 recommended)
- `MaxDelayMs`: 2000-5000 (3000-4000 recommended)

## Success Criteria

✅ **Pass**: Text reveals progressively with natural timing  
✅ **Pass**: No visual glitches or corruption  
✅ **Pass**: Colors are preserved  
✅ **Pass**: Performance is acceptable  
✅ **Pass**: User experience is improved  

## Next Steps

After testing confirms everything works:
1. Gather user feedback
2. Tune delays based on feedback
3. Consider adding user configuration option
4. Add to game settings menu
5. Update version number

---

**Test Status**: Ready for Testing  
**Priority**: High  
**Impact**: User Experience Enhancement

