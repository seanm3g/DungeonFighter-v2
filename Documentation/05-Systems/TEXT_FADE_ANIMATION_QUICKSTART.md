# Text Fade Animation - Quick Start Guide

## 🚀 5-Second Overview

Make text fade from screen with color effects. Every other letter dims first, then all fade away.

## ⚡ Quick Usage

### Simplest Usage (Default Alternating)

```csharp
TextFadeAnimator.FadeOutAlternating("The enemy has been defeated!");
```

### With Custom Timing

```csharp
TextFadeAnimator.FadeOutAlternating("Victory!", frames: 6, delayMs: 150);
```

### Different Patterns

```csharp
// Left to right
TextFadeAnimator.FadeOut("Experience gained!", new TextFadeAnimator.FadeConfig
{
    Pattern = TextFadeAnimator.FadePattern.Sequential
});

// Outside to center
TextFadeAnimator.FadeOut("Portal closes", new TextFadeAnimator.FadeConfig
{
    Pattern = TextFadeAnimator.FadePattern.CenterCollapse
});
```

### With Color Templates

```csharp
// Fire effect
TextFadeAnimator.FadeOutWithTemplate("Burning flames", "fiery");

// Ice effect
TextFadeAnimator.FadeOutWithTemplate("Frozen solid", "icy");

// Shadow effect
TextFadeAnimator.FadeOutWithTemplate("Into darkness", "shadow");
```

## 🎨 Available Patterns

| Pattern | Effect | Best For |
|---------|--------|----------|
| `Alternating` | Every other letter fades | General use, visually interesting |
| `Sequential` | Left to right | Text being erased/consumed |
| `CenterCollapse` | Outside to center | Portals closing, compression |
| `CenterExpand` | Center to outside | Explosions, spreading |
| `Uniform` | All at once | Simple fades, buffs expiring |
| `Random` | Random order | Chaos, disintegration |

## 🎯 Common Scenarios

### Enemy Defeated

```csharp
var text = ColorParser.Colorize("Goblin", "enemy") + " defeated!";
TextFadeAnimator.FadeOutAlternating(text);
```

### Critical Hit

```csharp
TextFadeAnimator.FadeOutWithTemplate("CRITICAL HIT!", "fiery", 
    TextFadeAnimator.FadePattern.CenterExpand);
```

### Buff Expiration

```csharp
TextFadeAnimator.FadeOut("Holy protection fades...", new TextFadeAnimator.FadeConfig
{
    Pattern = TextFadeAnimator.FadePattern.Uniform,
    TotalFrames = 4
});
```

### Status Effect Ending

```csharp
TextFadeAnimator.FadeOutWithTemplate("Poison wears off", "toxic", 
    TextFadeAnimator.FadePattern.Sequential);
```

## ⚙️ Configuration Quick Reference

```csharp
new TextFadeAnimator.FadeConfig
{
    Pattern = TextFadeAnimator.FadePattern.Alternating,  // See table above
    Direction = TextFadeAnimator.FadeDirection.ToDark,   // ToDark, ToBright, TemplateProgression
    ColorTemplate = "fiery",                              // Optional template name
    FrameDelayMs = 100,                                   // 50-200 recommended
    TotalFrames = 5,                                      // 3-8 recommended
    ClearAfterFade = true                                 // Clear line when done
}
```

## 🎬 Test It

```csharp
// Run all demos
TextFadeAnimatorExamples.RunAllDemos();

// Interactive test
TextFadeAnimatorExamples.InteractiveDemo();
```

## 💡 Pro Tips

1. **Match speed to content**: Fast (3 frames, 50ms) for routine, slow (8 frames, 150ms) for dramatic
2. **Use templates for themes**: `fiery` for fire, `icy` for ice, `shadow` for dark effects
3. **Don't overuse**: Only fade important or special messages
4. **Preserve colors**: Existing color markup is automatically handled

## 📚 More Information

See **TEXT_FADE_ANIMATION_SYSTEM.md** for comprehensive documentation.

