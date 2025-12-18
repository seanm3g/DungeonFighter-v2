# Dungeon Fighter v2 - Complete Gameplay Walkthrough

This guide demonstrates how to play through the DungeonFighter game using the MCP server interface and the available game tools.

## Quick Start - Playing via MCP Server

To play the game through the MCP server, you can use any MCP client that supports the stdio transport protocol.

### Option 1: Using Python MCP Client (Recommended)

```bash
cd "DungeonFighter-v2"
python Scripts/play_game.py
```

This launches an interactive Python client that communicates with the MCP server.

### Option 2: Using .NET Directly

```bash
cd "DungeonFighter-v2"
dotnet run --project Code/Code.csproj -- MCP
```

This starts the MCP server on stdio. You can then send MCP requests to it.

### Option 3: Using Claude with MCP Integration

If Claude Code has MCP integration enabled in `.mcp.json`, you can interact with the game directly through Claude using the available tools.

---

## Game Loop - Step-by-Step Walkthrough

### Step 1: Start New Game

**MCP Tool:** `start_new_game`

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "start_new_game",
    "arguments": {}
  }
}
```

**What Happens:**
- Game coordinator initializes
- Main menu displays with options:
  1. New Game
  2. Load Game
  3. Settings
  4. Exit

**Expected Output:**
```
Game initialized successfully!
Main Menu:
  1) New Game
  2) Load Game
  3) Settings
  4) Exit
```

---

### Step 2: Create Character

**MCP Tool:** `handle_input`

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "handle_input",
    "arguments": {
      "input": "1"
    }
  }
}
```

**What Happens:**
- Character creation screen displays
- Shows character class options
- Prompts for character name (or uses generated name)

**Class Options (Example):**
- **Warrior** - High health, balanced damage
- **Rogue** - Speed-focused, critical strikes
- **Mage** - Special abilities, spell casting
- **Paladin** - Defensive, healing abilities

---

### Step 3: View Character Stats

**MCP Tool:** `get_player_stats`

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "get_player_stats",
    "arguments": {}
  }
}
```

**Expected Response:**
```json
{
  "name": "Thrall",
  "class": "Warrior",
  "level": 1,
  "experience": 0,
  "maxExperience": 100,
  "health": 100,
  "maxHealth": 100,
  "attributes": {
    "strength": 15,
    "dexterity": 10,
    "intelligence": 8,
    "vitality": 12
  },
  "equipment": {
    "weapon": "Iron Sword",
    "armor": "Leather Armor"
  },
  "gold": 50
}
```

---

### Step 4: View Available Dungeons

**MCP Tool:** `get_available_dungeons`

```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "method": "tools/call",
  "params": {
    "name": "get_available_dungeons",
    "arguments": {}
  }
}
```

**Expected Response:**
```json
{
  "dungeons": [
    {
      "name": "Goblin Cave",
      "minLevel": 1,
      "maxLevel": 3,
      "theme": "cave",
      "recommendedLevel": 1
    },
    {
      "name": "Dark Forest",
      "minLevel": 3,
      "maxLevel": 5,
      "theme": "forest",
      "recommendedLevel": 3
    },
    {
      "name": "Ancient Ruins",
      "minLevel": 5,
      "maxLevel": 8,
      "theme": "ruins",
      "recommendedLevel": 5
    }
  ]
}
```

---

### Step 5: Enter Dungeon

**MCP Tool:** `handle_input`

```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "method": "tools/call",
  "params": {
    "name": "handle_input",
    "arguments": {
      "input": "1"
    }
  }
}
```

**What Happens:**
- Player enters first dungeon (Goblin Cave)
- First room is generated
- Initial game state is set

---

### Step 6: Explore Dungeon Rooms

**MCP Tool:** `get_current_dungeon`

```json
{
  "jsonrpc": "2.0",
  "id": 6,
  "method": "tools/call",
  "params": {
    "name": "get_current_dungeon",
    "arguments": {}
  }
}
```

**Expected Response:**
```json
{
  "name": "Goblin Cave",
  "currentRoom": 1,
  "totalRooms": 8,
  "theme": "cave",
  "currentRoomDescription": "A damp cave entrance. Glowing mushrooms light the way.",
  "enemies": [
    {
      "name": "Goblin Scout",
      "health": 30,
      "maxHealth": 30,
      "level": 1
    }
  ]
}
```

---

### Step 7: Combat System

Each room may contain enemies. Combat follows these steps:

**Get Combat State:**

```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "method": "tools/call",
  "params": {
    "name": "get_combat_state",
    "arguments": {}
  }
}
```

**Expected Response:**
```json
{
  "inCombat": true,
  "currentEnemy": {
    "name": "Goblin Scout",
    "health": 30,
    "maxHealth": 30,
    "level": 1
  },
  "playerHealth": 100,
  "playerMaxHealth": 100,
  "availableActions": [
    {
      "id": 1,
      "name": "Basic Attack",
      "damage": "12-18",
      "description": "A standard melee attack"
    },
    {
      "id": 2,
      "name": "Power Strike",
      "damage": "20-28",
      "description": "A powerful but slower attack",
      "cooldown": 2
    },
    {
      "id": 3,
      "name": "Defend",
      "reduction": "50%",
      "description": "Reduce incoming damage"
    }
  ]
}
```

**Execute Combat Action:**

```json
{
  "jsonrpc": "2.0",
  "id": 8,
  "method": "tools/call",
  "params": {
    "name": "handle_input",
    "arguments": {
      "input": "1"
    }
  }
}
```

**Combat Flow Example:**
1. Player attacks: Basic Attack deals 15 damage to Goblin Scout
2. Goblin Scout attacks: Deals 8 damage to player
3. Player health: 92/100
4. Goblin health: 15/30
5. Continue combat until enemy is defeated

---

### Step 8: Room Completion & Rewards

When all enemies in a room are defeated:

```json
{
  "jsonrpc": "2.0",
  "id": 9,
  "method": "tools/call",
  "params": {
    "name": "get_game_state",
    "arguments": {}
  }
}
```

**What Happens:**
- Gold reward: 25-50 gold
- Experience reward: 20-40 XP
- Potential item drops
- Proceed to next room or dungeon exit

**Expected Output:**
```json
{
  "status": "room_cleared",
  "rewards": {
    "gold": 35,
    "experience": 30
  },
  "playerStats": {
    "gold": 85,
    "experience": 30,
    "level": 1
  },
  "nextRoom": 2,
  "totalRooms": 8
}
```

---

### Step 9: Level Up

When experience reaches maximum:

```json
{
  "playerStats": {
    "level": 2,
    "experience": 0,
    "maxExperience": 150,
    "health": 115,
    "maxHealth": 115,
    "attributes": {
      "strength": 16,
      "dexterity": 11,
      "intelligence": 8,
      "vitality": 13
    }
  },
  "levelUpRewards": {
    "healthIncrease": 15,
    "attributeIncrease": "All attributes increased by 1"
  }
}
```

---

### Step 10: Continue Through Dungeon

Repeat Steps 6-9 for each room in the dungeon.

**Key Mechanics:**
- Each room may contain 1-3 enemies
- Some rooms are empty (treasure/rest rooms)
- Last room usually contains a boss enemy
- Bosses have more health and deal more damage

---

### Step 11: Dungeon Completion

When final boss is defeated:

```json
{
  "jsonrpc": "2.0",
  "id": 20,
  "method": "tools/call",
  "params": {
    "name": "get_game_state",
    "arguments": {}
  }
}
```

**Expected Response:**
```json
{
  "status": "dungeon_completed",
  "dungeonRewards": {
    "gold": 200,
    "experience": 500,
    "itemsDropped": [
      {
        "name": "Steel Sword",
        "rarity": "uncommon",
        "damage": "18-24"
      }
    ]
  },
  "playerStats": {
    "level": 3,
    "experience": 200,
    "gold": 325,
    "health": 130,
    "maxHealth": 130
  },
  "nextAction": "Return to dungeon selection or upgrade equipment"
}
```

---

## Advanced Features

### Inventory Management

```json
{
  "jsonrpc": "2.0",
  "id": 21,
  "method": "tools/call",
  "params": {
    "name": "get_inventory",
    "arguments": {}
  }
}
```

### Save Game Progress

```json
{
  "jsonrpc": "2.0",
  "id": 22,
  "method": "tools/call",
  "params": {
    "name": "save_game",
    "arguments": {}
  }
}
```

### View Recent Output

```json
{
  "jsonrpc": "2.0",
  "id": 23,
  "method": "tools/call",
  "params": {
    "name": "get_recent_output",
    "arguments": {
      "count": 20
    }
  }
}
```

---

## Complete Gameplay Session Example

Here's a minimal example showing a complete sequence:

```bash
# Start MCP server
python Scripts/play_game.py

# Session Commands:
start_new_game
# → Character created: Warrior level 1

get_player_stats
# → Health: 100/100, Gold: 50

get_available_dungeons
# → Shows 3 dungeons

handle_input "1"
# → Enter Goblin Cave

get_current_dungeon
# → Room 1 of 8, Goblin Scout present

handle_input "1"
# → Basic Attack on Goblin Scout

handle_input "1"
# → Basic Attack again

# Continue attacking until enemy is defeated

# Room clears, rewards granted, proceed to next room

handle_input "2"
# → Continue to next room

# Repeat combat for remaining rooms

# Final boss defeated, dungeon complete

save_game
# → Progress saved
```

---

## Tips for Playing

1. **Match Dungeon Difficulty** - Choose dungeons appropriate for your character level
2. **Manage Health** - Use defend action to reduce damage when low on health
3. **Save Progress** - Use `save_game` after dungeon completion
4. **Check Inventory** - Monitor equipment and items between dungeons
5. **Upgrade Equipment** - Use gold to buy better weapons and armor
6. **Attribute Distribution** - Different classes benefit from different attributes

---

## Troubleshooting

### Game Won't Start
- Ensure .NET 8.0 SDK is installed
- Check that MCP server is running: `dotnet run --project Code/Code.csproj -- MCP`

### Input Not Recognized
- Ensure input format is correct (usually "1", "2", etc. for menu selection)
- Use `get_available_actions` to see valid inputs for current state

### Combat Stuck
- Try using "3" for defend action to change combat state
- Check if health is 0 (game over)

---

## Game State Flow Chart

```
Start Game
    ↓
Character Creation
    ↓
Main Menu (Dungeon Selection)
    ↓
Enter Dungeon
    ↓
Generate Rooms (5-10 rooms per dungeon)
    ↓
Room Loop:
    ├→ Empty Room → Next Room
    ├→ Enemy Room → Combat Loop:
    │              ├→ Player Action
    │              ├→ Enemy Counter
    │              └→ Repeat until enemy defeated
    │         → Rewards → Next Room
    └→ Boss Room → Combat → Dungeon Complete
    ↓
Return to Dungeon Selection
    ↓
Repeat or Save & Exit
```

---

## MCP Tools Reference

| Tool | Purpose | Returns |
|------|---------|---------|
| `start_new_game` | Initialize new game | Game state |
| `save_game` | Save progress | Success/Fail |
| `handle_input` | Send player action | Updated state |
| `get_available_actions` | List valid actions | Action list |
| `get_game_state` | Full game snapshot | Complete state |
| `get_player_stats` | Character stats | Player stats |
| `get_current_dungeon` | Dungeon info | Dungeon state |
| `get_inventory` | Player items | Inventory |
| `get_combat_state` | Combat info | Combat state |
| `get_recent_output` | Game messages | Message log |
| `get_available_dungeons` | Dungeon list | Dungeon list |

---

## Game Balance & Progression

**Level Progression Example:**
- Level 1: 100 HP, 50 Gold
- Level 2: 115 HP, gained from dungeon 1
- Level 3: 130 HP, gained from dungeon 2
- Level 5: 175 HP, can tackle harder dungeons
- Level 10: 250+ HP, access to endgame content

**Damage Scaling:**
- Base attack: 12-18 damage at level 1
- Scales with Strength attribute
- Special abilities deal 1.5-2x base damage
- Critical hits possible with Dexterity bonus

This walkthrough provides all the information needed to fully play through DungeonFighter v2 using the MCP server interface!
