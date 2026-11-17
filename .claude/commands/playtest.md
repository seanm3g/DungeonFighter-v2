Play through the game to verify fun factor and spot edge cases.

Usage: /playtest [level] [weapon]
- level: Dungeon level to run (1-5, default: 1)
- weapon: Weapon type to use (Mace/Sword/Dagger/Wand, default: random)

This command launches the Game Tester Agent which:
1. Starts a new game
2. Plays through the specified dungeon level
3. Tests combat mechanics and weapon feel
4. Tracks engagement and fun moments
5. Reports any unexpected behaviors or imbalances
6. Provides player feedback perspective

The agent will:
- Comment on weapon flow and feel
- Note any matchups that feel unfair
- Track difficulty progression
- Identify tedious or satisfying moments
- Suggest improvements from gameplay perspective

Examples:
  /playtest
  /playtest 1
  /playtest 3 Sword
  /playtest 5 Dagger

How to play (MCP `dungeonfighter` server; guide: `.cursor/skills/mcp-gameplay/SKILL.md`):
- `start_new_game`, then loop `get_agent_context` → `handle_input` with a choice's `Input`.
- Read `Screen.Body` for what each screen shows (bag, item comparison, action sequence, skill nodes, regions) and `NewEvents` on each `handle_input` reply for what happened (a dungeon run returns its swing-by-swing combat log).
- `get_item_details` / `get_action_details` give the full item tooltip and action card.
- Make real player decisions between runs: compare and equip gear, shape the action sequence, spend skill points, travel when the level band is outgrown.
