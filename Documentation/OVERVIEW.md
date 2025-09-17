# DungeonFighter-v2 Game Overview

## General Description
DungeonFighter-v2 is a sophisticated turn-based RPG/dungeon crawler game written in C# using .NET 8.0. The game features advanced combat mechanics with combo systems, intelligent battle narrative generation, and a comprehensive data-driven architecture. Built with modular design principles, each system is implemented and thoroughly tested before integration, creating a robust and extensible game framework.

## Core Features

### Advanced Combat System
- **Turn-based Combat:** Strategic combat with cooldown-based action timing
- **Action Combo System:** Chain actions together for increased damage and effects (1.85x damage multiplier per combo step)
- **Dice-based Mechanics:** 1d20 roll system with thresholds (1-5 fail, 6-15 normal, 16-20 combo trigger)
- **Environmental Actions:** Room-specific effects that impact combat
- **Intelligent Delay System:** Optimized pacing that only applies delays when text is displayed

### Character & Progression System
- **Character Stats:** Strength, Agility, Technique, Intelligence with level-based scaling
- **XP & Leveling:** Automatic stat increases and health restoration on level up
- **Equipment System:** Weapons, armor with tier-based stats and special abilities
- **Action Pool Management:** Dynamic action selection from equipped gear

### Enemy & AI System
- **18+ Enemy Types:** Each with unique stats, abilities, and specializations
- **Primary Attribute System:** Enemies specialize in Strength, Agility, or Technique
- **Level Scaling:** Dynamic stat scaling based on enemy level
- **Environment-specific Spawning:** Different enemy types appear in themed dungeons

### Dungeon & Environment System
- **Procedural Generation:** 1-3 rooms per dungeon based on level
- **10 Themed Dungeons:** Forest, Lava, Crypt, Cavern, Swamp, Desert, Ice, Ruins, Castle, Graveyard
- **15+ Room Types:** Each with unique environmental actions and effects
- **Boss Chambers:** Special final rooms with powerful enemies

### Battle Narrative System
- **Event-driven Narrative:** Poetic descriptions for significant combat moments
- **Informational Summaries:** Clear, factual combat reporting for regular actions
- **Significant Event Detection:** First blood, health reversals, near death, combo achievements
- **Configurable Display:** Balance between narrative and informational modes

### Data-driven Architecture
- **JSON Configuration:** All game data stored in structured JSON files
- **Modular Design:** Easy to add new enemies, actions, rooms, and items
- **Comprehensive Testing:** Built-in test suite with 14+ test categories
- **Cross-platform:** Runs on Windows, macOS, and Linux

## Implemented Classes

### Core Game Classes
- **`Character`**: Player character with stats, inventory, equipment slots, and action pool management
- **`Enemy`**: Adversaries with specialized stats, AI behavior, and level-based scaling
- **`Entity`**: Abstract base class for all actors with action pools and selection logic
- **`Item`**: Base item system with subclasses for weapons, armor, and equipment
- **`WeaponItem`**: Weapons with damage, speed, and weapon-specific actions
- **`ArmorItem`**: Armor pieces with protection values and potential special abilities

### Combat & Action System
- **`Combat`**: Manages turn-based combat, action execution, and battle flow
- **`Action`**: Represents all game actions with cooldowns, targeting, and special effects
- **`ActionLoader`**: Loads and manages action definitions from JSON data
- **`Dice`**: Handles all random number generation with specialized combo mechanics
- **`BattleNarrative`**: Generates event-driven battle descriptions and summaries
- **`BattleEvent`**: Tracks individual combat events for narrative generation

### Environment & Dungeon System
- **`Environment`**: Represents different room types with environmental actions
- **`Dungeon`**: Procedurally generates themed room sequences and manages progression
- **`RoomLoader`**: Loads room definitions and environmental effects from JSON
- **`EnemyFactory`**: Creates enemies with proper scaling and specialization
- **`EnemyLoader`**: Loads enemy definitions and configurations from JSON

### Game Management
- **`Game`**: Main game loop orchestrator, player progression, and system integration
- **`GameSettings`**: Configuration management for game parameters and settings
- **`ManageGear`**: Equipment management UI and inventory system
- **`LootGenerator`**: Procedural loot generation with tier and rarity systems
- **`FlavorText`**: Procedural generation of names and descriptions

### Utility & Support
- **`Program`**: Application entry point with comprehensive test suite
- **`DiceResult`**: Specialized result tracking for combo mechanics
- **`GameSettings`**: Singleton configuration management

## Development Approach
- **Layer-by-layer Development:** Each system is built and tested before moving to the next layer
- **Test-driven Development:** Comprehensive test suite with 14+ test categories covering all major systems
- **Data-driven Design:** All game content is defined in JSON files for easy modification and expansion
- **Modular Architecture:** Clear separation of concerns with well-defined interfaces between systems
- **Documentation:** Extensive documentation for all classes, systems, and game mechanics

## Current Implementation Status
- **Core Systems:** ✅ Fully implemented and tested
- **Combat System:** ✅ Advanced combo mechanics and battle narrative
- **Character System:** ✅ Complete with progression and equipment
- **Enemy System:** ✅ 18+ enemy types with AI and scaling
- **Dungeon System:** ✅ Procedural generation with 10 themed dungeons
- **Data System:** ✅ Complete JSON-driven content management
- **Testing Framework:** ✅ Comprehensive test suite integrated

## Future Development Plans
- **Unity Integration:** Planning to port core systems to Unity for enhanced graphics and gameplay
- **GUI Enhancement:** Desktop GUI implementation for improved user experience
- **Save/Load System:** Persistent character progression and game state
- **Additional Content:** More enemy types, dungeons, and equipment
- **Multiplayer Support:** Cooperative and competitive gameplay modes

---

## Action Combo System

The game features a sophisticated Action Combo System that allows players to chain together sequences of actions during combat, with each successful action amplifying the next. This system creates dynamic, skill-based combat that rewards strategic thinking and proper timing.

### Action Sources & Management
All actions come from equipped gear - there are no core class actions that every character automatically gets. Actions are obtained from:

- **Weapon Actions**: Each weapon type provides specific actions (e.g., Sword provides PARRY and SWORD SLASH)
- **Armor Actions**: Armor pieces have a chance to provide actions based on their tier and rarity
- **All Weapons**: Always provide their weapon-specific actions (100% chance for both starter and loot weapons)
- **Starter Armor**: Never provide actions (0% chance)

### Action Pool vs Combo Sequence
The system features a two-tier action management:

**Action Pool**: Contains ALL available actions from equipped gear
- No ordering - just a list of available actions
- Actions show `[IN COMBO]` indicator if they're currently selected for the combo
- Actions have no combo order when in the pool

**Combo Sequence**: A subset of actions selected from the Action Pool for the current combo
- Sequential ordering (1, 2, 3, 4...) based on player selection
- Only actions in the combo sequence have combo orders
- Players can add, remove, and reorder actions within the sequence

### Advanced Dice Mechanics
The system uses sophisticated dice mechanics for action resolution:

- **Initial Roll**: `1d20 + bonus` (where bonus comes from loot or effects)
  - **1-5**: Fail at attack (combo resets to step 1)
  - **6-15**: Normal attack (continues combo sequence)
  - **16-20**: Combo attack (triggers combo mode)
- **Combo Mode**: Once triggered, subsequent rolls use **14+** threshold to continue
- **Damage Amplification**: Each successful combo step multiplies damage
- **Failure Handling**: Failed rolls reset the sequence to the first action and deactivate combo mode

### Combo Management Interface
Players can manage their combo sequences through the inventory screen:

1. **Add Action to Combo**: Select from available actions in Action Pool
2. **Remove Action from Combo**: Remove actions from current Combo Sequence
3. **Swap Combo Actions**: Reorder actions within the Combo Sequence
4. **Reset Combo Step**: Reset to the first action in the sequence

### Action Properties & Effects
Each action is defined with comprehensive properties:
- **Damage Multiplier**: Expressed as a decimal (e.g., 3.0 = 300%)
- **Length**: Duration or speed modifier affecting cooldown timing
- **Description**: Special effects or mechanics
- **Combo Order**: Position in the combo sequence
- **Special Effects**: Such as causesWeaken, causesBleed, stat bonuses, etc.
- **Target Type**: Self, SingleTarget, AreaOfEffect, Environment
- **Cooldown**: Turn-based cooldown system

### Loot and Bonuses
- Players can acquire loot that grants bonuses to their action rolls (e.g., +1 to the roll)
- These bonuses increase the chance of successfully chaining combos and triggering combo mode
- Equipment provides both stat bonuses and potential new actions

This system creates deep, strategic combat that rewards skillful play and proper equipment management.

---

## Enemy System

The game features a sophisticated enemy system with proper level scaling, primary attribute specialization, and environment-specific spawning that ensures enemies remain challenging and distinct as the player progresses.

### Enemy Scaling & Progression
Enemies scale their stats and capabilities based on their level:

- **Health Scaling**: +15 health per level (e.g., Level 1 = 55 health, Level 5 = 130 health)
- **Base Attribute Scaling**: +2 per level for all attributes (Strength, Agility, Technique)
- **Primary Attribute Bonus**: +1 extra per level for the enemy's primary attribute
- **Total Scaling**: Primary attribute gets +3 per level, others get +2 per level
- **Accuracy Scaling**: Higher level enemies have better accuracy (8 + Level/2 difficulty)
- **Reward Scaling**: Gold and XP rewards scale with enemy level

### Primary Attribute Specialization
Each enemy type has a primary attribute that determines their combat specialization:

- **Strength Primary**: Orcs, Skeletons, Zombies, Slimes
  - Specialize in physical damage and tanking
  - Higher strength scaling for devastating basic attacks
  - Focus on high damage output and survivability
- **Agility Primary**: Goblins, Bandits, Spiders, Bats
  - Specialize in speed and precision
  - Higher agility scaling for quick jabs and dodging
  - Focus on fast, accurate attacks and mobility
- **Technique Primary**: Cultists, Wraiths
  - Specialize in magical and special attacks
  - Higher technique scaling for powerful special abilities
  - Focus on unique mechanics and status effects

### Enemy Types & Specializations

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

### Advanced Enemy Types
The game also features higher-level enemies with unique abilities:

- **Crystal Golem**: Level 5, high strength and technique - Crystal-based attacks
- **Prism Spider**: Level 4, high agility and technique - Light manipulation
- **Shard Beast**: Level 4, balanced strength and agility - Crystal shard attacks
- **Stone Guardian**: Level 6, very high strength and technique - Defensive abilities
- **Temple Warden**: Level 5, high technique - Ancient magic
- **Ancient Sentinel**: Level 7, extremely high stats - Boss-level enemy

### Environment-Specific Spawning
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

### Unified Damage System
All damage follows a consistent formula:

**Base Damage = STR + (Highest Attribute) + Weapon Damage**
- **STR**: Character's Strength stat
- **Highest Attribute**: The highest of STR, AGI, TEC, or INT
- **Weapon Damage**: Damage from equipped weapon

**Final Damage = Base Damage - Target's Armor**
- **Armor**: Sum of all equipped armor pieces
- **Minimum Damage**: 1 (damage cannot be reduced below 1)

**Action Multipliers**: Actions can multiply the base damage (e.g., 2.5x for critical hits)

This system ensures that different enemy types feel distinct and challenging while maintaining balanced combat encounters.

---

## Battle Narrative System

The game features an innovative event-driven battle narrative system that provides informational summaries for regular combat while triggering poetic narrative for significant moments. This approach ensures that important events are highlighted while maintaining clear, factual information for normal combat actions.

### How It Works
- **Event Collection**: All combat actions (player combos, enemy attacks, environmental effects) are recorded as `BattleEvent` objects with timestamps, damage values, and success/failure status
- **Significant Event Detection**: The system monitors for specific significant events that warrant narrative attention
- **Informational Summary**: Regular combat actions are summarized with factual information (damage dealt, combos executed, etc.)
- **Narrative Events**: Significant moments trigger poetic narrative descriptions that are added to the summary

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
- **Configurable**: Players can adjust the balance between narrative and informational display

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

This system transforms mechanical combat into an emotionally resonant narrative experience that emphasizes the human element of conflict while maintaining clear gameplay information.

---

## Intelligent Delay System

The game features an intelligent delay system that optimizes the user experience by only applying delays when text is actually being displayed to the player. This system provides the best of both worlds: fast background calculations for full narrative mode and appropriate pacing for action-by-action display.

### How It Works
- **Text Display Detection**: The system tracks whether action messages are actually displayed to the player based on the narrative balance setting
- **Conditional Delays**: Delays are only applied when `EnableTextDisplayDelays` is enabled AND text is being displayed
- **Action-Length Matching**: When delays are applied, they match the length of the action being performed (e.g., a 2.0 length action gets a longer delay than a 0.5 length action)
- **Speed Adjustment**: Delays respect the `CombatSpeed` setting, allowing players to adjust the pace of combat

### Benefits
- **Fast Full Narrative Mode**: When using full narrative mode (NarrativeBalance = 1.0), calculations happen quickly in the background without unnecessary delays
- **Appropriate Action Pacing**: When displaying actions individually (NarrativeBalance = 0.0), delays match action length for natural-feeling combat flow
- **Configurable**: Players can disable delays entirely for maximum speed or adjust combat speed to their preference
- **Consistent**: All combat participants (player, enemies, environment) use the same delay system

### Settings
- **EnableTextDisplayDelays**: Controls whether delays are applied when text is displayed (default: true)
- **CombatSpeed**: Multiplier for combat timing (0.5 = slow, 2.0 = fast, default: 1.0)
- **NarrativeBalance**: Controls the balance between narrative and informational display (0.0 = action-by-action, 1.0 = full narrative)

### Example Behavior
- **Full Narrative Mode**: Combat resolves quickly, then a poetic summary is displayed
- **Action-by-Action Mode**: Each action displays with a delay proportional to its length
- **Disabled Delays**: All combat happens at maximum speed regardless of narrative mode

This system ensures that the game feels responsive and engaging regardless of the player's preferred narrative style while maintaining optimal performance.

---

*This overview will be updated as new features and systems are added to the project.* 