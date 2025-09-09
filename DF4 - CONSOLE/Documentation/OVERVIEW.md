# Game Overview

## General Description
This project is a layer-by-layer, test-driven development of a turn-based RPG game written in C#. The game is designed to be modular, with each core system implemented and tested individually before integration. The architecture is intended to be clear, extensible, and easy to maintain.

## Core Features
- **Character System:** Players control a character with stats, inventory, and abilities.
- **Enemy System:** The game features enemies with their own stats and behaviors.
- **Combat System:** Turn-based combat between characters and enemies, utilizing dice rolls for randomness.
- **Item System:** Items can be found, equipped, and used by characters and enemies.
- **Environment System:** Different environments (using the `Environment` class) affect gameplay and encounters.
- **Dice System:** All randomness (e.g., combat, loot drops) is handled by a dedicated dice class.
- **Game Loop:** A main game class orchestrates the flow, user input, and system interactions.
- **Intelligent Delay System:** Delays are only applied when text is being displayed, allowing for fast background calculations in full narrative mode while providing appropriate pacing for action-by-action display.

## Implemented Classes
- `Character`: Represents the player or other characters, with stats, inventory, and equipment slots.
- `Enemy`: Represents adversaries in the game, inheriting from Character.
- `Item`: Represents items that can be used or equipped, with subclasses for weapons and armor.
- `Dice`: Handles all random number generation and dice rolls.
- `Combat`: Manages combat logic, turn order, and action execution.
- `Environment`: Represents different locations or scenarios, including room effects and enemy spawns.
- `Dungeon`: Procedurally generates a sequence of themed rooms and manages dungeon-level progression.
- `Game`: Orchestrates the main game loop, player progression, dungeon flow, inventory, and gear management.
- `Action`: Represents actions (attacks, spells, buffs, etc.) that entities can perform, with support for cooldowns and targeting.
- `BattleNarrative`: Generates poetic 3-act battle descriptions by analyzing combat events and creating narrative prose.
- `BattleEvent`: Tracks individual combat events for narrative generation.
- `Program`: The main entry point for the application, providing test harnesses, launching the game loop, and handling user startup.

## Supporting/Utility Classes
- `Entity`: Abstract base class for all actors (characters, enemies, environments) with action pools and selection logic.
- `ManageGear`: Handles gear management UI and logic, allowing players to equip/unequip items and manage inventory.
- `FlavorText`: Provides procedural generation of names and descriptions for characters, items, environments, and actions.

## Development Approach
- **Layer-by-layer:** Each system is built and tested before moving to the next.
- **Test-driven:** Unit tests are planned for each class and function, but currently no active test files are present in the codebase.
- **Documentation:** All code and systems are documented for clarity and ease of use.

## Graphical User Interface (GUI)
A simple desktop GUI is being planned and scaffolded to provide a modern, user-friendly way to interact with the game. The GUI features:
- **Fighter Stats Panel:** Displays player stats (level, XP, HP, MP, STR, SPD).
- **Event Log Panel:** Shows a scrollable log of game events.
- **Equipment Panel:** Allows equipping/unequipping items and shows inventory in a grid.
- **Dungeons Panel:** Lists available dungeons, their difficulty, and a progress bar for dungeon exploration.
- **Dark Theme:** Modern look with yellow highlights for headers and a grid-based layout.

The GUI will be implemented using WinForms or WPF and will be connected to the core game logic. (Currently in planning/scaffolding stage.)

---

## Action Combo System

The game features a dynamic Action Combo System, inspired by classic RPGs and fighting games. This system allows players to chain together a sequence of actions during combat, with each successful action amplifying the next.

### Action Sources
All actions come from equipped gear - there are no core class actions that every character automatically gets. Actions are obtained from:

- **Weapon Actions**: Each weapon type provides specific actions (e.g., Sword provides PARRY and SWORD SLASH)
- **Armor Actions**: Armor pieces have a chance to provide actions based on their tier and rarity
- **All Weapons**: Always provide their weapon-specific actions (100% chance for both starter and loot weapons)
- **Starter Armor**: Never provide actions (0% chance)

Each action is defined with:
- **Damage Multiplier**: Expressed as a decimal (e.g., 3.0 = 300%)
- **Length**: Duration or speed modifier (e.g., 0.5, 2.0)
- **Description**: Special effects or mechanics
- **Combo Order**: Position in the combo sequence
- **Special Effects**: Such as causesWeaken, causesBleed, stat bonuses, etc.

### Performing Actions
- On your turn, you attempt the next action in your combo sequence (player-selected actions from your Action Pool).
- Roll `1d20 + x` (where `x` is your bonus from loot or effects) with new mechanics:
  - **1-5**: Fail at attack (combo resets)
  - **6-15**: Normal attack (continues combo)
  - **16-20**: Combo attack (triggers combo mode)
- Once combo mode is triggered, subsequent rolls use **11+** threshold to continue the sequence.
- Each successful combo action amplifies the damage by **1.85x** (per step).
- If you fail, the sequence resets to the first action in your combo and combo mode deactivates.

### Action System Architecture

The game features a two-tier action system:

#### **Action Pool**
Contains ALL available actions from two sources (all defined in `Actions.json`):
1. **Weapon Actions**: 
   - **All Weapons**: Guaranteed to have actions (e.g., PARRY, SWORD SLASH)
   - **Both starter and loot weapons**: Always provide their weapon-specific actions
2. **Armor Actions**: Chance-based actions from equipped armor pieces (e.g., HEADBUTT, CHEST BASH)
   - **Note**: Starter armor never has actions - only loot armor has a chance

**Action Pool Properties**: 
- No ordering - just a list of available actions
- Actions show `[IN COMBO]` indicator if they're currently selected for the combo
- Actions have no combo order when in the pool

#### **Combo Sequence**
A subset of actions selected from the Action Pool for the current combo. Players can:
- **Add actions** from Action Pool to Combo Sequence
- **Remove actions** from Combo Sequence back to Action Pool
- **Reorder actions** within the Combo Sequence (swap positions)
- Combo Sequence actions are numbered 1, 2, 3, 4... based on player selection

**Combo Sequence Properties**:
- Sequential ordering (1, 2, 3, 4...)
- Only actions in the combo sequence have combo orders
- Actions removed from combo get their order reset to 0

**Default Setup**: New characters start with an empty Combo Sequence and must select actions from their Action Pool (which initially contains only actions from their starter weapon).

**Combo Management UI**: Accessible through the inventory screen, players can:
1. **Add Action to Combo**: Select from available actions in Action Pool
2. **Remove Action from Combo**: Remove actions from current Combo Sequence
3. **Swap Combo Actions**: Reorder actions within the Combo Sequence
4. **Reset Combo Step**: Reset to the first action in the sequence

### Loot and Bonuses
- Players can acquire loot that grants bonuses to their action rolls (e.g., +1 to the roll).
- These bonuses increase the chance of successfully chaining combos and triggering combo mode.

This system adds depth and excitement to combat, rewarding skillful play and strategic use of loot and abilities.

---

## Enemy System

The game features a comprehensive enemy system with proper level scaling and primary attributes that ensures enemies remain challenging and distinct as the player progresses.

### Enemy Scaling
Enemies scale their stats and capabilities based on their level:

- **Health Scaling**: +15 health per level (e.g., Level 1 = 55 health, Level 5 = 130 health)
- **Base Attribute Scaling**: +2 per level for all attributes (Strength, Agility, Technique)
- **Primary Attribute Bonus**: +1 extra per level for the enemy's primary attribute
- **Total Scaling**: Primary attribute gets +3 per level, others get +2 per level

### Primary Attribute System
Each enemy type has a primary attribute that determines their specialization:

- **Strength Primary**: Orcs, Skeletons, Zombies, Slimes
  - Specialize in physical damage and tanking
  - Higher strength scaling for better basic attacks
- **Agility Primary**: Goblins, Bandits, Spiders, Bats
  - Specialize in speed and precision
  - Higher agility scaling for better jabs and dodging
- **Technique Primary**: Cultists, Wraiths
  - Specialize in magical and special attacks
  - Higher technique scaling for better special abilities

### Enemy Types and Specializations

**Strength Specialists:**
- **Orc**: High strength and health, low agility - Tanky bruisers
- **Skeleton**: Balanced stats, strength focus - Undead warriors
- **Zombie**: High health and strength, very low agility - Slow but tough
- **Slime**: High health, low agility - Gelatinous tanks

**Agility Specialists:**
- **Goblin**: Balanced stats, good agility - Quick skirmishers
- **Bandit**: High agility, balanced other stats - Fast rogues
- **Spider**: High agility, low health - Quick but fragile
- **Bat**: Very high agility, low health and strength - Extremely fast

**Technique Specialists:**
- **Cultist**: High technique, moderate other stats - Magic users
- **Wraith**: High technique and agility, moderate health - Spectral casters

### Damage Calculation System
All damage follows a unified formula:

**Base Damage = STR + (Highest Attribute) + Weapon Damage**
- **STR**: Character's Strength stat
- **Highest Attribute**: The highest of STR, AGI, TEC, or INT
- **Weapon Damage**: Damage from equipped weapon

**Final Damage = Base Damage - Target's Armor**
- **Armor**: Sum of all equipped armor pieces
- **Minimum Damage**: 1 (damage cannot be reduced below 1)

**Action Multipliers**: Actions can multiply the base damage (e.g., 2.5x for critical hits)

### Accuracy Scaling
Higher level enemies have better accuracy:
- **Difficulty**: 8 + (Level / 2)
- Level 1 enemies need 8+ to hit
- Level 5 enemies need 10+ to hit
- Level 10 enemies need 13+ to hit

### Reward Scaling
Enemy rewards scale with level:
- **Gold**: 5 + (Level * 3)
- **XP**: 10 + (Level * 5)

### Environment-Specific Enemies
Different environments spawn different enemy types with appropriate specializations:

- **Forest**: Goblin (Agility), Bandit (Agility), Spider (Agility)
- **Lava**: Wraith (Technique), Slime (Strength), Bat (Agility)
- **Crypt**: Skeleton (Strength), Zombie (Strength), Wraith (Technique)
- **Cavern**: Orc (Strength), Bat (Agility), Slime (Strength)
- **Swamp**: Slime (Strength), Spider (Agility), Cultist (Technique)
- **Desert**: Bandit (Agility), Cultist (Technique), Wraith (Technique)
- **Ice**: Wraith (Technique), Skeleton (Strength), Bat (Agility)
- **Ruins**: Cultist (Technique), Skeleton (Strength), Bandit (Agility)
- **Castle**: Bandit (Agility), Cultist (Technique), Wraith (Technique)
- **Graveyard**: Zombie (Strength), Wraith (Technique), Skeleton (Strength)

This primary attribute system ensures that different enemy types feel distinct and challenging in different ways, creating more strategic combat encounters.

---

## Battle Narrative System

The game features an event-driven battle narrative system that provides informational summaries for regular combat while triggering poetic narrative for significant moments. This approach ensures that important events are highlighted while maintaining clear, factual information for normal combat actions.

### How It Works
- **Event Collection**: All combat actions (player combos, enemy attacks, environmental effects) are recorded as `BattleEvent` objects with timestamps, damage values, and success/failure status.
- **Significant Event Detection**: The system monitors for specific significant events that warrant narrative attention.
- **Informational Summary**: Regular combat actions are summarized with factual information (damage dealt, combos executed, etc.).
- **Narrative Events**: Significant moments trigger poetic narrative descriptions that are added to the summary.

### Significant Events That Trigger Narrative

1. **First Blood**: The first significant damage dealt (>10 damage) in a battle
   - *Example: "Warrior draws first blood with a Heroic Strike that deals 25 damage to Goblin!"*

2. **Health Reversal**: When someone goes from having higher health percentage to lower health than their opponent
   - *Example: "Hero turns the tide! Their relentless assault has brought Villain to their knees!"*

3. **Near Death**: When someone drops below 20% health but is still alive
   - *Example: "Fighter staggers, bloodied and battered, but their spirit refuses to break!"*

4. **Good Combo**: When someone successfully executes a 3+ step combo sequence
   - *Example: "ComboMaster unleashes a devastating combo sequence! Each strike flows into the next with deadly precision!"*

### Key Features
- **Event-Driven**: Narrative only appears when significant events occur
- **Informational Base**: Regular combat uses clear, factual summaries
- **Poetic Highlights**: Significant moments are described with evocative language
- **Multiple Events**: Multiple significant events can occur in a single battle
- **Contextual**: Narrative descriptions are tailored to the specific event and combatants

### Benefits
- **Clear Information**: Players always get factual combat summaries
- **Emotional Impact**: Significant moments are highlighted with narrative flair
- **Efficient**: No unnecessary narrative for routine combat actions
- **Engaging**: Important events feel more impactful and memorable
- **Balanced**: Combines the best of both informational and narrative approaches

### Example Output

**Battle with First Blood and Good Combo:**
```
Warrior defeats Goblin! Total damage dealt: 55 vs 5 received. Combos executed: 1 vs 0.

Warrior draws first blood with a Heroic Strike that deals 25 damage to Goblin!
Warrior unleashes a devastating combo sequence! Each strike flows into the next with deadly precision!
```

**Battle with Health Reversal and Near Death:**
```
Villain defeats Hero! Total damage dealt: 75 vs 65 received. Combos executed: 0 vs 0.

Hero draws first blood with a Light Attack that deals 15 damage to Villain!
Hero turns the tide! Their relentless assault has brought Villain to their knees!
Hero staggers, bloodied and battered, but their spirit refuses to break!
```

**Regular Battle (No Significant Events):**
```
Fighter defeats Skeleton! Total damage dealt: 45 vs 20 received. Combos executed: 0 vs 0.
```

This system transforms mechanical combat into an emotionally resonant narrative experience that emphasizes the human element of conflict.

---

## Intelligent Delay System

The game features an intelligent delay system that optimizes the user experience by only applying delays when text is actually being displayed to the player. This system provides the best of both worlds: fast background calculations for full narrative mode and appropriate pacing for action-by-action display.

### How It Works
- **Text Display Detection**: The system tracks whether action messages are actually displayed to the player based on the narrative balance setting.
- **Conditional Delays**: Delays are only applied when `EnableTextDisplayDelays` is enabled AND text is being displayed.
- **Action-Length Matching**: When delays are applied, they match the length of the action being performed (e.g., a 2.0 length action gets a longer delay than a 0.5 length action).
- **Speed Adjustment**: Delays respect the `CombatSpeed` setting, allowing players to adjust the pace of combat.

### Benefits
- **Fast Full Narrative Mode**: When using full narrative mode (NarrativeBalance = 1.0), calculations happen quickly in the background without unnecessary delays.
- **Appropriate Action Pacing**: When displaying actions individually (NarrativeBalance = 0.0), delays match action length for natural-feeling combat flow.
- **Configurable**: Players can disable delays entirely for maximum speed or adjust combat speed to their preference.
- **Consistent**: All combat participants (player, enemies, environment) use the same delay system.

### Settings
- **EnableTextDisplayDelays**: Controls whether delays are applied when text is displayed (default: true)
- **CombatSpeed**: Multiplier for combat timing (0.5 = slow, 2.0 = fast, default: 1.0)

### Example Behavior
- **Full Narrative Mode**: Combat resolves quickly, then a poetic summary is displayed
- **Action-by-Action Mode**: Each action displays with a delay proportional to its length
- **Disabled Delays**: All combat happens at maximum speed regardless of narrative mode

This system ensures that the game feels responsive and engaging regardless of the player's preferred narrative style.

---

*This file will be updated as new features and layers are added.* 