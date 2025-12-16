# Title Screen Color Animation Update

## Overview
The title screen now features a smooth color transition animation that brings the "DUNGEON FIGHTER" logo to life with a sophisticated warm/cool color progression before settling into the final iconic colors.

## Animation Sequence

### Phase 1: White to Warm/Cool (1 second)
- **Both words start**: Pure white (#ffffff)
- **DUNGEON transitions**: White → Warm white (gold tint)
- **FIGHTER transitions**: White → Cool white (cyan tint)
- **Duration**: 1 second at 30 FPS (smooth animation)

### Phase 2: Warm/Cool to Final Colors (1 second)
- **DUNGEON progresses**: Warm white → Gold (final color)
- **FIGHTER progresses**: Cool white → Cyan → Magenta → Red (final color)
- **Duration**: 1 second at 30 FPS

### Final State
- **DUNGEON**: Gold/Yellow color (color code `&W`)
- **FIGHTER**: Red/Scarlet color (color code `&R`)
- **Total animation time**: ~2 seconds plus 1 second hold

## Implementation Details

### New Files Created

#### `Code/UI/TitleScreenAnimator.cs`
A dedicated animator class that handles the title screen color transitions:
- **`AnimateTitleScreen()`**: Core animation loop that renders frames at 30 FPS
- **`InterpolateColor()`**: Handles warm/cool white transitions
- **`InterpolateCoolToRed()`**: Manages the cyan-to-red color progression
- **`RenderTitleWithColors()`**: Renders the title with custom colors
- **`BuildTitleArtWithColors()`**: Constructs the ASCII art with specified color codes
- **`ShowAnimatedTitleScreen()`**: Public entry point for the complete animation sequence

### Updated Files

#### `Code/UI/Avalonia/CanvasUIManager.cs`
Added public methods to support the animation system:
```csharp
public void Clear()        // Clears the canvas for rendering
public void Refresh()      // Refreshes the canvas to display content
```

#### `Code/UI/Avalonia/MainWindow.axaml.cs`
Updated to use the new animated title screen:
- Calls `TitleScreenAnimator.ShowAnimatedTitleScreen()` instead of static rendering
- Maintains the same black screen intro (1 second)
- Runs animation on background thread to avoid UI blocking

## Color Progression Details

### DUNGEON (Warm Progression)
```
Frame Progress:  0% ───► 50% ───► 75% ───► 100%
Color Code:      Y       Y        y        W
Visual:         White   White   Gold-Grey  Gold
Temperature:    Neutral  Warm    Warm      Warm
```

### FIGHTER (Cool to Hot Progression)
```
Frame Progress:  0% ───► 30% ───► 60% ───► 80% ───► 100%
Color Code:      Y       y        C        r        R
Visual:         White   Grey    Cyan    Dk-Red   Red
Temperature:    Neutral  Cool    Cool    Hot      Hot
```

## Technical Specifications

### Animation Parameters
- **FPS**: 30 frames per second
- **Frame Duration**: ~33ms per frame
- **Phase 1 Duration**: 1000ms (30 frames)
- **Phase 2 Duration**: 1000ms (30 frames)
- **Hold Duration**: 1000ms
- **Total Time**: ~3 seconds

### Color Codes Used
Based on Caves of Qud-style color system:
- `Y` - White (#ffffff)
- `y` - Grey (#b1c9c3)
- `W` - Gold/Yellow (#cfc041)
- `C` - Cyan (#77bfcf)
- `c` - Dark Cyan (#40a4b9)
- `M` - Magenta (#da5bd6)
- `r` - Dark Red (#a64a2e)
- `R` - Red (#d74200)

## User Experience

### What You'll See
1. **Black screen** (1 second) - Building anticipation
2. **White flash** - Both words appear in pure white
3. **Warm glow** - DUNGEON gains a golden warmth
4. **Cool tint** - FIGHTER takes on a cool cyan hue
5. **Color emergence** - Both words transition to their final, iconic colors
6. **Stable display** - Logo settles into gold and red
7. **Press key prompt** - "Press any key to continue" appears

### Visual Impact
- Creates a sense of depth and temperature contrast
- DUNGEON feels warm, inviting, treasure-like
- FIGHTER feels intense, building from cool anticipation to hot action
- Smooth 30 FPS animation provides professional polish
- Color transitions are subtle and elegant

## How to See It

### Run the Game
```bash
dotnet run --project Code/Code.csproj
```
or
```bash
.\DF.exe
```

The animation will play automatically on startup.

### Disable Animation (if needed)
To revert to the static title screen, comment out the animated call in `MainWindow.axaml.cs`:
```csharp
// TitleScreenAnimator.ShowAnimatedTitleScreen();
canvasUI2.RenderOpeningAnimation(); // Use static version
```

## Design Philosophy

### Color Psychology
- **Warm White → Gold (DUNGEON)**: Represents treasure, rewards, ancient mysteries
- **Cool White → Red (FIGHTER)**: Represents calm focus transitioning to intense combat

### Animation Timing
- 1 second per phase provides comfortable viewing without feeling rushed
- 30 FPS ensures smooth, professional animation
- Hold time allows appreciation of final colors before interaction

### Technical Approach
- Non-blocking animation runs on background thread
- Frame-by-frame rendering provides precise color control
- Modular design allows easy adjustment of timing and colors
- Integrates seamlessly with existing color system

## Future Enhancement Possibilities

### Potential Additions
- Fade-in effect for the ASCII art itself
- Subtle "pulse" effect on the divider line
- Particle effects using ASCII characters
- Sound effects synchronized with color transitions
- Configuration options for animation speed/enable/disable

### Customization Options
All timing constants are easily adjustable in `TitleScreenAnimator.cs`:
```csharp
const int warmCoolTransitionMs = 1000;     // Phase 1 duration
const int finalColorTransitionMs = 1000;   // Phase 2 duration
const int framesPerSecond = 30;            // Animation smoothness
```

## Related Systems
- **Color System**: Uses Caves of Qud-style `&X` color codes
- **TextFadeAnimator**: Provides complementary text animation effects
- **AsciiArtAssets**: Central repository for ASCII art and styling
- **Opening Animation**: Classic static title screen (still available)

## Troubleshooting

**Problem**: Animation feels too fast
**Solution**: Increase `warmCoolTransitionMs` and `finalColorTransitionMs` in `TitleScreenAnimator.cs`

**Problem**: Animation feels choppy
**Solution**: Increase `framesPerSecond` (try 60 for ultra-smooth)

**Problem**: Want to skip animation
**Solution**: Press any key during animation - wait functionality TBD

**Problem**: Colors don't look right
**Solution**: Ensure terminal/display supports the full color palette defined in `ColorTemplates.json`

## Documentation References
- Color System: `Documentation/05-Systems/COLOR_SYSTEM_IMPLEMENTATION_SUMMARY.md`
- Opening Animation: `Documentation/05-Systems/OPENING_ANIMATION.md`
- Text Animation: `Documentation/02-Development/TEXT_FADE_ANIMATION_IMPLEMENTATION_SUMMARY.md`
- ASCII Art Assets: `Code/UI/Avalonia/AsciiArtAssets.cs`

---

**Created**: October 11, 2025  
**Feature**: Title Screen Color Animation  
**Status**: ✅ Implemented and Tested

