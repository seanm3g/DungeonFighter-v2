# DF4 Console RPG Game

A turn-based RPG game written in C# with a focus on modular design and test-driven development.

## How to Run

1. **Compile the project:**
   ```bash
   cd Code
   dotnet build
   ```

2. **Run the game:**
   ```bash
   dotnet run
   ```

## Main Menu Options

### 1. Play Game
Start a new game session. You'll be prompted to:
- Choose a dungeon to explore
- Fight enemies in turn-based combat
- Collect loot and experience points
- Progress through multiple rooms

### 2. Tests
Access various test functions to verify game systems:
- Character leveling and stats
- Item creation and properties
- Dice rolling mechanics
- Action system functionality
- Combat mechanics
- Combo system
- Battle narrative generation
- Enemy scaling
- **Intelligent Delay System** (NEW!)

### 3. Settings
Configure game behavior and appearance:
- **Narrative Balance**: Control the mix between event-driven and poetic narrative
- **Combat Speed**: Adjust how fast combat actions are displayed
- **Text Display Delays**: Enable/disable delays that match action length (NEW!)
- Difficulty multipliers for enemies and players
- Combat display options (health bars, damage numbers, etc.)
- Gameplay features (auto-save, combo system, etc.)

## Intelligent Delay System

The game now features an intelligent delay system that optimizes the user experience:

### How It Works
- **Fast Full Narrative Mode**: When using full narrative mode, calculations happen quickly in the background
- **Action-Length Matching**: When displaying actions individually, delays match the action's length for natural pacing
- **Configurable**: You can disable delays entirely for maximum speed

### Settings
- **Enable Text Display Delays**: Toggle whether delays are applied when text is displayed
- **Combat Speed**: Adjust the overall speed multiplier (0.5 = slow, 2.0 = fast)

### Usage Examples
1. **For Fast Gameplay**: Set Narrative Balance to 1.0 and disable Text Display Delays
2. **For Action-by-Action**: Set Narrative Balance to 0.0 and enable Text Display Delays
3. **For Balanced Experience**: Use default settings (Narrative Balance 0.7, delays enabled)

## Game Systems

### Combat System
- Turn-based combat with dice rolls
- Action combo system with escalating damage
- Enemy scaling based on level and primary attributes
- Environment effects that modify combat

### Narrative System
- Event-driven narrative for significant moments
- Poetic descriptions for important battles
- Configurable balance between factual and narrative text

### Character Progression
- Level-based stat increases
- Equipment system with weapons and armor
- Experience points from defeating enemies

## Testing the Intelligent Delay System

Use the test menu (option 13) to verify the delay system works correctly:
- Tests delays with and without text display
- Verifies action length affects delay duration
- Confirms combat speed setting modifies delays
- Ensures disabled delays result in no waiting

## Configuration Files

The game automatically saves settings to `gamesettings.json` in the Code directory. You can:
- Modify settings through the in-game menu
- Edit the JSON file directly (be careful with syntax)
- Reset to defaults from the settings menu

## Development Notes

- Built with .NET 6.0
- Uses test-driven development approach
- Modular architecture for easy extension
- Comprehensive documentation in OVERVIEW.md and TASKLIST.md 