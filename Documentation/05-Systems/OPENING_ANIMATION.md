# Opening Animation System

## Overview
The Dungeon Fighter opening animation system provides a stylized ASCII art title screen that displays when the game starts, creating an immersive entry point for players.

## Features

### Multiple Animation Styles
The system includes multiple ASCII art animations:

1. **Animated Title Screen** (Default for GUI)
   - Smooth color transition animation at 30 FPS
   - Phase 1: Both words start white, blend to warm white (DUNGEON) and cool white (FIGHTER) over 1 second
   - Phase 2: Transition to final colors (gold for DUNGEON, red for FIGHTER) over 1 second
   - Professional, polished intro experience
   - See `TitleScreenAnimator.cs` for implementation details

2. **Standard Animation** (Default for Console)
   - Large, bold block letters using Unicode box-drawing characters
   - Green border frame
   - White "DUNGEON" text
   - Red "FIGHTER" text
   - Yellow prompt text
   - Animated line-by-line reveal with 50ms delays

3. **Simplified Animation**
   - Compact ASCII art using lighter characters
   - Faster display (30ms delays)
   - Good for quick startups
   - Automatically clears after 1 second

4. **Detailed Animation**
   - Classic ASCII art style using underscores and slashes
   - Decorative border
   - Sword emoji decoration
   - 60ms delay between lines

5. **Sword and Shield Animation**
   - Thematic ASCII art featuring a sword and shield
   - Title integrated into the artwork
   - "Enter the Depths" tagline
   - 70ms delay between lines

## Usage

### Default Integration
The opening animation automatically displays when the game starts:

**GUI Mode (Avalonia):**
```csharp
// In MainWindow.axaml.cs - InitializeGame()
TitleScreenAnimator.ShowAnimatedTitleScreen();  // Animated color transitions
```

**Console Mode:**
```csharp
// In Program.cs - LaunchConsoleUI()
OpeningAnimation.ShowOpeningAnimation();  // Classic static display
```

### Manual Invocation
You can call different animation styles programmatically:

```csharp
// Animated title screen with color transitions (recommended for GUI)
TitleScreenAnimator.ShowAnimatedTitleScreen();

// Standard animation (default for console)
OpeningAnimation.ShowOpeningAnimation();

// Simplified animation (faster)
OpeningAnimation.ShowSimplifiedAnimation();

// Detailed animation (classic style)
OpeningAnimation.ShowDetailedAnimation();

// Sword and shield themed
OpeningAnimation.ShowSwordAndShieldAnimation();
```

## Technical Details

### Color Markup Support
The animations use the game's color markup system for styling:
- `{{W|text}}` - White text
- `{{R|text}}` - Red text
- `{{G|text}}` - Green text
- `{{Y|text}}` - Yellow text

### UI Manager Integration
The animation works with both console and custom UI managers:
- **Console Mode**: Full interactive animation with "Press any key" prompt
- **Custom UI Mode**: Automatic 2-second display, then continues

### Animation Timing
- Standard: 50ms per line
- Simplified: 30ms per line  
- Detailed: 60ms per line
- Sword & Shield: 70ms per line

### Screen Management
- Automatically clears the console before displaying
- Clears the console after user input (console mode only)
- Respects custom UI managers without clearing their displays

## Examples

### Standard Animation Output
```
╔═══════════════════════════════════════════════════════════════════════════════╗
║                                                                               ║
 ██████╗ ██╗   ██╗███╗   ██╗ ██████╗ ███████╗ ██████╗ ███╗   ██╗
██╔═══██╗██║   ██║████╗  ██║██╔════╝ ██╔════╝██╔═══██╗████╗  ██║
██║   ██║██║   ██║██╔██╗ ██║██║  ███╗█████╗  ██║   ██║██╔██╗ ██║
██║   ██║██║   ██║██║╚██╗██║██║   ██║██╔══╝  ██║   ██║██║╚██╗██║
╚██████╔╝╚██████╔╝██║ ╚████║╚██████╔╝███████╗╚██████╔╝██║ ╚████║
 ╚═════╝  ╚═════╝ ╚═╝  ╚═══╝ ╚═════╝ ╚══════╝ ╚═════╝ ╚═╝  ╚═══╝
║                                                                               ║
███████╗██╗ ██████╗ ██╗  ██╗████████╗███████╗██████╗ 
██╔════╝██║██╔════╝ ██║  ██║╚══██╔══╝██╔════╝██╔══██╗
█████╗  ██║██║  ███╗███████║   ██║   █████╗  ██████╔╝
██╔══╝  ██║██║   ██║██╔══██║   ██║   ██╔══╝  ██╔══██╗
██║     ██║╚██████╔╝██║  ██║   ██║   ███████╗██║  ██║
╚═╝     ╚═╝ ╚═════╝ ╚═╝  ╚═╝   ╚═╝   ╚══════╝╚═╝  ╚═╝
║                                                                               ║
╚═══════════════════════════════════════════════════════════════════════════════╝

                        [Press any key to begin your quest]
```

### Sword and Shield Animation Output
```
         ╔═════════════════════════════════════════════════════════╗
         ║                                                         ║
              /\                                  ___
             /  \              D U N G E O N         /   \
            /____\                                  |  O  |
           /  ||  \           F I G H T E R        |     |
          /   ||   \                                |     |
         /    ||    \                               |     |
        /     ||     \                              |     |
       /      ||      \                             \     /
      /       ||       \                             \___/
     /========||========\
            [||||]                          ⚔ Enter the Depths ⚔
         ║                                                         ║
         ╚═════════════════════════════════════════════════════════╝

                      [Press any key to begin]
```

## Configuration Options

### Changing the Default Animation
To use a different animation style by default, modify `Program.cs`:

```csharp
// Change from ShowOpeningAnimation() to another style
OpeningAnimation.ShowSimplifiedAnimation();  // Faster startup
// or
OpeningAnimation.ShowSwordAndShieldAnimation();  // Thematic
```

### Disabling the Animation
To disable the opening animation entirely, comment out or remove the line in `Program.cs`:

```csharp
// OpeningAnimation.ShowOpeningAnimation();  // Disabled
```

### Adjusting Animation Speed
Modify the `Thread.Sleep()` values in `OpeningAnimation.cs`:

```csharp
Thread.Sleep(50);  // Change to desired milliseconds
```

## Adding Custom Animations

### Creating a New Animation Style
Add a new method to `OpeningAnimation.cs`:

```csharp
public static void ShowCustomAnimation()
{
    if (UIManager.GetCustomUIManager() == null)
    {
        Console.Clear();
    }
    
    string[] asciiArt = new string[]
    {
        "{{W|Your ASCII art here}}",
        "{{R|More lines...}}",
        // ... more lines
    };
    
    foreach (string line in asciiArt)
    {
        UIManager.WriteLine(line, UIMessageType.Title);
        Thread.Sleep(60); // Adjust timing as needed
    }
    
    if (UIManager.GetCustomUIManager() == null)
    {
        Console.ReadKey(true);
        Console.Clear();
    }
    else
    {
        Thread.Sleep(2000);
    }
}
```

## Integration with Other Systems

### Color System
The opening animation fully integrates with the game's keyword color system, supporting:
- Template-based coloring: `{{template|text}}`
- Direct color codes: `{{R|red text}}`
- Nested color formatting

### UI Configuration
The animation respects the `UIManager` configuration:
- Honors `DisableAllOutput` flag if set
- Works with custom UI managers (like CanvasUIManager)
- Uses `UIMessageType.Title` for proper delay configuration

### Game Initialization
The animation displays before:
- Game data generation
- Configuration loading
- Main menu display

## Performance Considerations

### Console Mode
- Total animation time: ~1 second (20 lines × 50ms)
- User can skip by pressing any key
- Console clearing is instant

### Custom UI Mode
- No user interaction required
- Fixed 2-second display time
- No console clearing operations

### Memory
- Minimal memory footprint
- ASCII art stored as string arrays
- No persistent resources

## Future Enhancements

### Potential Additions
1. **Animated ASCII Art**: Sequential frame-based animations
2. **Sound Effects**: Integration with audio system when available
3. **Configuration File**: Load animation style from settings
4. **Random Selection**: Randomly choose from multiple animation styles
5. **Seasonal Themes**: Holiday or event-specific animations
6. **Player Statistics**: Show play time or achievements on title screen

## Related Documentation
- **COLOR_SYSTEM.md**: Color markup syntax and templates
- **UI_SYSTEM.md**: UI manager architecture
- **GAME_FLOW.md**: Application startup sequence
- **README_TITLE_SCREEN_ANIMATION.md**: Detailed animated title screen documentation
- **TEXT_FADE_ANIMATION_IMPLEMENTATION_SUMMARY.md**: Text animation system

