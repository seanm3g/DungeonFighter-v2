# Text Fade Animation System - README

## Quick Start

I've implemented a text fade animation system that makes text fade from the screen with various patterns, including the alternating letter effect you requested!

### Simplest Usage

```csharp
using RPGGame;

// Every other letter fades first, then all fade away
TextFadeAnimator.FadeOutAlternating("The enemy has been defeated!");
```

### Test It Now

Run the demonstrations to see all fade patterns:

```bash
# From project root directory
Scripts\test-fade.bat
```

Or try the interactive mode:

```bash
Scripts\test-fade.bat --interactive
```

## What You Get

### 6 Fade Patterns

1. **Alternating** (your requested feature!) - Every other letter fades first
2. **Sequential** - Left to right fade
3. **Center Collapse** - Outside edges to center
4. **Center Expand** - Center to outside edges
5. **Uniform** - All letters fade together
6. **Random** - Random letter order

### Integration with Color System

Works seamlessly with your existing color templates:

```csharp
// Fade through fiery colors
TextFadeAnimator.FadeOutWithTemplate("Burning flames", "fiery");

// Fade through icy colors
TextFadeAnimator.FadeOutWithTemplate("Frozen solid", "icy");

// Fade through shadow colors
TextFadeAnimator.FadeOutWithTemplate("Into darkness", "shadow");
```

## Documentation

📖 **Full Documentation**: `Documentation/05-Systems/TEXT_FADE_ANIMATION_SYSTEM.md`  
⚡ **Quick Reference**: `Documentation/05-Systems/TEXT_FADE_ANIMATION_QUICKSTART.md`  
📋 **Implementation Details**: `TEXT_FADE_ANIMATION_IMPLEMENTATION_SUMMARY.md`

## Examples

### Combat

```csharp
// Enemy defeated
TextFadeAnimator.FadeOutAlternating("Goblin defeated!");

// Critical hit with fiery effect
TextFadeAnimator.FadeOutWithTemplate("CRITICAL HIT!", "fiery", 
    TextFadeAnimator.FadePattern.CenterExpand);
```

### Status Effects

```csharp
// Buff expiration
TextFadeAnimator.FadeOut("Holy protection fades...", new TextFadeAnimator.FadeConfig
{
    Pattern = TextFadeAnimator.FadePattern.Uniform,
    TotalFrames = 4,
    FrameDelayMs = 150
});
```

### Environment

```csharp
// Portal closing
TextFadeAnimator.FadeOut("The portal closes", new TextFadeAnimator.FadeConfig
{
    Pattern = TextFadeAnimator.FadePattern.CenterCollapse
});
```

## Customization

Full control over animation:

```csharp
TextFadeAnimator.FadeOut("Custom animation", new TextFadeAnimator.FadeConfig
{
    Pattern = TextFadeAnimator.FadePattern.Alternating,
    Direction = TextFadeAnimator.FadeDirection.ToDark,
    ColorTemplate = "fiery",        // Optional
    FrameDelayMs = 100,             // Speed between frames
    TotalFrames = 5,                // Number of animation frames
    ClearAfterFade = true           // Clear line when done
});
```

## Files & Location

- **Core Engine**: `Code/UI/TextFadeAnimator.cs`
- **Examples**: `Code/UI/TextFadeAnimatorExamples.cs`
- **Test Script**: `Scripts/test-fade.bat`

## Next Steps

1. **Try it**: Run `Scripts\test-fade.bat` to see all patterns
2. **Experiment**: Run `Scripts\test-fade.bat --interactive` to customize
3. **Integrate**: Use `TextFadeAnimator.FadeOutAlternating()` in your code
4. **Read docs**: Check `Documentation/05-Systems/TEXT_FADE_ANIMATION_SYSTEM.md` for full API

---

✅ **Ready to use** - No compilation needed, fully integrated with your existing color system!

