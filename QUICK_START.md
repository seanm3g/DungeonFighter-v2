# DungeonFighter v2 - Quick Start Guide

**Version 6.2** - *Production Ready*

## Installation & Running

### Prerequisites
- .NET 8.0 SDK or Runtime
- Windows, macOS, or Linux

### Quick Start
```bash
# Clone and navigate
git clone <your-repo-url>
cd DungeonFighter-v2

# Run the game
dotnet run --project Code/Code.csproj
```

Or if you have a built executable:
```bash
.\DF.exe         # Windows
./DF             # macOS/Linux
```

## First Time Playing?

### 1. Watch the Opening Animation 🎨
- Beautiful animated title screen with color transitions
- Press any key when ready to continue

### 2. Main Menu Options
- **[1] Enter Dungeon** - Start your adventure (creates character if new)
- **[2] View Inventory** - Manage equipment and items
- **[3] Character Info** - View your stats and progression
- **[4] Tuning Console** - Advanced settings (for developers)
- **[5] Save Game** - Save your progress
- **[6] Exit** - Quit the game

### 3. Controls
- **Keyboard**: Press number keys (1-6) for menu options
- **Mouse**: Click on any option
- **Combat**: Choose actions by pressing corresponding keys

### 4. Tips for New Players
- Start with easier dungeons (Level 1-2)
- Equip items from your inventory for better stats
- Watch your health - heal when needed
- Experiment with different combat actions
- Save your progress frequently

## What's New in v6.2? ✨

### Visual Polish
- **Animated Title Screen** - Smooth 30 FPS color transitions
- **Item Colors** - Items colored by rarity (grey → prismatic)
- **1920×1080 Resolution** - Optimized for modern displays
- **Persistent Layout** - Character stats always visible

### Complete Features
- **Full Inventory System** - All 7 actions functional
- **Data-Driven Colors** - 166 templates, 200+ keywords in JSON
- **Combat Text Polish** - Smart wrapping, clean formatting
- **Zero Known Bugs** - All issues resolved

## Need Help?

### Documentation
- **Overview**: See `README.md` in the root folder
- **Full Documentation**: Browse the `Documentation/` folder
  - `Documentation/01-Core/` - Essential project info
  - `Documentation/02-Development/` - Development guides
  - `Documentation/03-Quality/` - Testing and debugging
  - `Documentation/04-Reference/` - Quick references
  - `Documentation/05-Systems/` - System-specific docs

### Key Documentation Files
- **Complete Changelog**: `Documentation/02-Development/CHANGELOG.md`
- **Implementation Details**: `Documentation/02-Development/OCTOBER_2025_IMPLEMENTATION_SUMMARY.md`
- **Architecture Overview**: `Documentation/01-Core/ARCHITECTURE.md`
- **Development Guide**: `Documentation/02-Development/DEVELOPMENT_GUIDE.md`

### Specific Features
- **Title Screen Animation**: See `README_TITLE_SCREEN_ANIMATION.md`
- **Quick Animation Guide**: See `README_QUICK_START_ANIMATION.md`
- **Color System**: See `GameData/README_COLOR_CONFIG.md`

## Customization

### Colors & Templates
Edit these files to customize colors (no code changes needed):
- `GameData/ColorTemplates.json` - Color templates
- `GameData/KeywordColorGroups.json` - Keyword coloring
- See `GameData/README_COLOR_CONFIG.md` for instructions

### Game Settings
- Use **[4] Tuning Console** from main menu
- Adjust combat parameters, difficulty, and more
- Changes are saved automatically

## Game Flow

```
Opening Animation
    ↓
Main Menu
    ↓
[1] Enter Dungeon → Select Dungeon → Explore Rooms → Combat → Loot
    ↓
Victory! → Level Up → Return to Main Menu → Save → Continue
```

## Troubleshooting

### Game Won't Start
- Verify .NET 8.0 is installed: `dotnet --version`
- Try rebuilding: `dotnet build Code/Code.csproj`

### Graphics Issues
- Ensure terminal supports Unicode characters
- Check terminal size is sufficient (220×65 characters minimum)

### Save File Issues
- Save file located at: `GameData/character_save.json`
- Backup your save before major changes

## Advanced Features

### For Developers
- **Built-in Test Suite**: Access through Settings menu
- **Tuning Console**: Real-time parameter adjustment
- **Balance Analysis**: Automated DPS calculations
- **Comprehensive Docs**: See `Documentation/` folder

### Data Files
All game data is JSON-based and easily moddable:
- `GameData/Actions.json` - Combat actions
- `GameData/Enemies.json` - Enemy definitions
- `GameData/Weapons.json` - Weapon items
- `GameData/Armor.json` - Armor items
- `GameData/Dungeons.json` - Dungeon configurations
- `GameData/Rooms.json` - Room definitions

## Community & Support

### Contributing
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

### Reporting Issues
- Check `Documentation/03-Quality/KNOWN_ISSUES.md` first
- Provide detailed reproduction steps
- Include save file if relevant

## Have Fun! 🎮

DungeonFighter v2 is a labor of love featuring deep combat mechanics, rich progression systems, and beautiful ASCII art. Take your time, explore the dungeons, and enjoy the adventure!

---

**Version**: 6.2  
**Status**: Production Ready ✅  
**Last Updated**: October 11, 2025

