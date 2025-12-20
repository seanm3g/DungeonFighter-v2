# Playing DungeonFighter v2 with Claude Code

## Quick Start

I can now play DungeonFighter v2 interactively! Here are the ways to have me play:

## Method 1: Direct Interactive Play (Recommended)

Simply ask me to play the game. I will:
1. Start the game in interactive mode
2. See the current game state
3. Choose actions from available options
4. Continue playing until victory or defeat

Example: "Play a quick game of DungeonFighter and try to complete the first dungeon"

## Method 2: Batch Commands

You can provide a sequence of commands for me to execute:

```bash
echo -e "1\n2\n1\nquit" | cd "D:\code projects\github projects\DungeonFighter-v2\Code" && dotnet run -- PLAY
```

Or I can create a command file and execute it.

## Game Commands

While playing, you can use:
- **Numbers (1-4)**: Select actions from the menu
- **status**: Show detailed player stats
- **help**: Display help information
- **quit**: Exit the game

## What I'll Display

When playing, I will show you:
- **Current Game State**: Turn number, player status, location
- **Available Actions**: Menu options to choose from
- **Game Events**: Recent combat/narrative messages
- **Stats**: Health, level, equipment, inventory

## How to Ask Me to Play

Here are natural ways to ask:

1. **"Play a game of DungeonFighter"**
   - I'll play through a complete session and show you the results

2. **"Try to reach level 5 in DungeonFighter"**
   - I'll play with that goal in mind

3. **"Play DungeonFighter and make it to the boss"**
   - I'll attempt that specific objective

4. **"Let me watch you play DungeonFighter"**
   - I'll play and narrate what's happening

## Technical Details

- Game starts with `dotnet run -- PLAY`
- Uses the `InteractiveMCPGamePlayer` class
- Commands sent via stdin
- Output parsed from stdout
- All timeout issues have been fixed ✓

## Example Session

```
Turn: 1 | Status: MainMenu
Available Actions:
   [1] New Game
   [2] Load Game
   [3] Settings
   [4] Quit

➤ Choosing action: 1
```

Then the game continues with character creation, dungeon entry, combat, etc.

---

**Ready to play?** Just ask me to start a game!
