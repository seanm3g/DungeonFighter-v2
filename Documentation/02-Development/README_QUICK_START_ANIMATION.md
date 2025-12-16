# Quick Start - Title Screen Animation

## What's New? 🎨

Your title screen now has a beautiful **color transition animation**!

## What You'll See

When you start the game:

1. **Black screen** (1 second)
2. **White flash** - Both "DUNGEON" and "FIGHTER" appear in pure white
3. **Color divergence** (1 second):
   - **DUNGEON** gets a warm golden glow ☀️
   - **FIGHTER** gets a cool cyan tint ❄️
4. **Final colors emerge** (1 second):
   - **DUNGEON** → Gold (treasure/wealth theme)
   - **FIGHTER** → Red (combat/intensity theme)
5. **"Press any key to continue"** appears

**Total time**: ~3 seconds of smooth animation at 30 FPS

## Try It Now!

### Run the Game
```bash
dotnet run --project Code/Code.csproj
```
or just double-click:
```bash
DF.exe
```

Watch the title screen come to life!

## Want to Customize?

### Change Animation Speed
Edit `Code/UI/TitleScreenAnimator.cs`:
- Line 24: `const int warmCoolTransitionMs = 1000;` (Phase 1 duration)
- Line 25: `const int finalColorTransitionMs = 1000;` (Phase 2 duration)
- Line 26: `const int framesPerSecond = 30;` (Smoothness - try 60!)

### Disable Animation
Edit `Code/UI/Avalonia/MainWindow.axaml.cs` line 64:
```csharp
// Change this:
TitleScreenAnimator.ShowAnimatedTitleScreen();

// To this:
canvasUI2.RenderOpeningAnimation();  // Old static version
```

## More Information

- **Full Details**: `README_TITLE_SCREEN_ANIMATION.md`
- **Implementation**: `IMPLEMENTATION_SUMMARY_TITLE_ANIMATION.md`
- **System Docs**: `Documentation/05-Systems/OPENING_ANIMATION.md`

## Enjoy! 🎮

The animation adds a professional touch to your game's opening. It's subtle, smooth, and sets the mood for adventure!

---

**Feature**: Title Screen Color Animation  
**Status**: ✅ Ready to Use  
**Build**: ✅ Passing

