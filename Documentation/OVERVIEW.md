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
- **`Program`**: Application entry point with comprehensive test suite (27+ test categories)
- **`DiceResult`**: Specialized result tracking for combo mechanics
- **`GameSettings`**: Singleton configuration management
- **`TuningConfig`**: Dynamic configuration system with real-time parameter adjustment
- **`FormulaEvaluator`**: Mathematical expression evaluator for dynamic formulas
- **`ScalingManager`**: Centralized scaling calculations for items and enemies
- **`BalanceAnalyzer`**: Automated balance testing and DPS calculations
- **`ActionSpeedSystem`**: Intelligent delay system for optimal user experience
- **`BattleHealthTracker`**: Health tracking for battle narrative system

## Development Approach
- **Layer-by-layer Development:** Each system is built and tested before moving to the next layer
- **Test-driven Development:** Comprehensive test suite with 27+ test categories covering all major systems
- **Data-driven Design:** All game content is defined in JSON files for easy modification and expansion
- **Modular Architecture:** Clear separation of concerns with well-defined interfaces between systems
- **Documentation:** Extensive documentation for all classes, systems, and game mechanics

## Current Implementation Status
- **Core Systems:** ✅ Fully implemented and tested
- **Combat System:** ✅ Advanced combo mechanics and battle narrative
- **Character System:** ✅ Complete with progression, equipment, and weapon-based classes
- **Enemy System:** ✅ 18+ enemy types with AI, scaling, and primary attribute specialization
- **Dungeon System:** ✅ Procedural generation with 10 themed dungeons
- **Data System:** ✅ Complete JSON-driven content management
- **Testing Framework:** ✅ Comprehensive test suite with 27+ test categories
- **Dynamic Tuning System:** ✅ Real-time parameter adjustment with FormulaEvaluator and ScalingManager
- **Balance Analysis:** ✅ Automated DPS calculations and combat balance testing
- **Advanced Action System:** ✅ Complex action mechanics with temporary effects and divine rerolls

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

## Advanced Action System

The game features a sophisticated action system with complex mechanics that go beyond simple damage dealing:

### Temporary Effects & Status Modifications
Actions can apply various temporary effects to characters:
- **Stat Bonuses**: Temporary increases to Strength, Agility, Technique, or Intelligence
- **Damage Modifiers**: Extra damage, damage reduction, or damage amplification
- **Action Modifiers**: Length reduction, extra attacks, or turn skipping
- **Special Effects**: Combo amplifier multipliers, divine reroll charges, guaranteed success

### Advanced Action Mechanics
- **Deja Vu**: Track last action for special effects
- **True Strike**: Skip next turn but guarantee next success
- **Flurry/Precision Strike**: Grant extra attacks
- **Opening Volley**: Add extra damage to next attack
- **Sharp Edge**: Reduce incoming damage
- **Taunt**: Reduce action length for duration
- **Pretty Boy Swag**: Amplify combo multipliers
- **Divine Reroll**: Grant reroll charges for failed actions

### Action Duration & Timing
- **Action Length**: Each action has a length value affecting cooldown timing
- **Turn System**: Actions are processed based on their length and timing
- **Cooldown Management**: Actions have individual cooldowns preventing spam
- **Speed System**: Intelligent delay system matches action length for natural pacing

### Action Sources & Management
- **Weapon Actions**: Each weapon type provides specific actions
- **Armor Actions**: High-tier armor can provide additional actions
- **Action Pool**: Dynamic collection of all available actions from equipped gear
- **Combo Sequence**: Ordered subset of actions selected for combo execution
- **Action Bonuses**: Equipment can provide bonuses to action rolls and effects

This advanced system creates deep tactical gameplay where action selection, timing, and sequencing are crucial for success.

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

## Action System Implementation

The game features a comprehensive action system with 30+ advanced actions, each with unique mechanics and effects that go beyond simple damage dealing.

### Implemented Actions

#### Basic Actions (1-4)
1. **JAB** - Resets enemy combo, 100% damage, 2.0 length
2. **TAUNT** - Reduces next 2 actions by 50% length, +2 combo bonus for 2 turns
3. **STUN** - Stuns enemy for 5 turns, causes weaken effect
4. **CRIT** - 300% damage, 2.0 length

#### Multi-Hit & Extra Attacks (5-6)
5. **FLURRY** - Adds 1 extra attack to next action
6. **PRECISION STRIKE** - Adds 1 extra attack to next action

#### Stat Bonuses (7-11)
7. **MOMENTUM BASH** - Gain 1 STR for duration of dungeon
8. **DANCE** - Gain 1 STR for duration of dungeon
9. **FOCUS** - Gain 1 STR for duration of dungeon
10. **READ BOOK** - Gain 1 STR for duration of dungeon

#### Roll Modifications (12-13)
11. **LUCKY STRIKE** - +1 to next roll
12. **DRUNKEN BRAWLER** - -5 to your next roll, -5 to enemies next roll

#### Multi-Hit Attacks (14)
13. **CLEAVE** - 3 hits at 35% damage each

#### Conditional Damage (15-16)
14. **OVERKILL** - Add 50% damage to next action
15. **SHIELD BASH** - Double STR if below 25% health

#### Special Effects (17-20)
16. **OPENING VOLLEY** - Deal 10 extra damage, -1 per turn
17. **SHARP EDGE** - Reduce damage by 50% each turn
18. **BLOOD FRENZY** - Deal double damage if health below 25%
19. **DEAL WITH THE DEVIL** - Do 5% damage to yourself

#### Health-Based Actions (21-24)
20. **BERZERK** - Double STR if below 25% health
21. **SWING FOR THE FENCES** - 50% chance to attack yourself
22. **TRUE STRIKE** - Skip turn, guarantee next action success
23. **LAST GRASP** - +10 to roll if health below 5%

#### Healing & Recovery (25)
24. **SECOND WIND** - Heal for 5 health if 2nd slot

#### Defensive Actions (26)
25. **QUICK REFLEXES** - -5 to next enemies roll if action fails

#### Special Mechanics (27-30)
26. **DEJA VU** - Repeat the previous action
27. **FIRST BLOOD** - Double damage if enemy at full health
28. **POWER OVERWHELMING** - Double damage if STR ≥ 10
29. **PRETTY BOY SWAG** - Double combo AMP if full health
30. **DIRTY BOY SWAG** - Quadruple damage if at 1 health

### Advanced Action Mechanics

#### Multi-Hit System
- **CLEAVE**: 3 hits at 35% damage each
- **FLURRY/PRECISION STRIKE**: Add extra attacks to next action

#### Self-Damage Effects
- **DEAL WITH THE DEVIL**: 5% self-damage
- **SWING FOR THE FENCES**: 50% chance to attack yourself

#### Roll Modifications
- **LUCKY STRIKE**: +1 to next roll
- **LAST GRASP**: +10 to roll when health below 5%
- **DRUNKEN BRAWLER**: -5 to your roll, -5 to enemy roll
- **QUICK REFLEXES**: -5 to enemy roll if action fails

#### Stat Bonuses
- **MOMENTUM BASH, DANCE, FOCUS, READ BOOK**: +1 STR for dungeon duration
- Temporary stat system with duration tracking

#### Turn Control
- **TRUE STRIKE**: Skip turn but guarantee next success
- **DEJA VU**: Repeat previous action

#### Health Thresholds
- **BLOOD FRENZY, SHIELD BASH, BERZERK**: Trigger below 25% health
- **LAST GRASP**: Trigger below 5% health
- **PRETTY BOY SWAG**: Trigger at full health
- **DIRTY BOY SWAG**: Trigger at 1 health

#### Stat Thresholds
- **POWER OVERWHELMING**: Trigger if STR ≥ 10

#### Conditional Damage Multipliers
- **OVERKILL**: +50% damage to next action
- **BLOOD FRENZY, SHIELD BASH, BERZERK**: Double damage when conditions met
- **FIRST BLOOD**: Double damage if enemy at full health
- **POWER OVERWHELMING**: Double damage if STR ≥ 10
- **DIRTY BOY SWAG**: Quadruple damage at 1 health

#### Combo System Enhancements
- **TAUNT**: +2 combo bonus for 2 turns
- **PRETTY BOY SWAG**: Double combo amplifier when at full health

#### Special Effects
- **STUN**: Stuns enemy for 5 turns, causes weaken
- **JAB**: Resets enemy combo
- **TAUNT**: Reduces next 2 actions by 50% length
- **SECOND WIND**: Heals for 5 health
- **OPENING VOLLEY**: 10 extra damage, decays by 1 per turn
- **SHARP EDGE**: 50% damage reduction, decays each turn

### Technical Implementation

#### Action Class Extensions
- Added 25+ new properties for advanced mechanics
- Automatic parsing of action descriptions to set properties
- Support for multi-hit, self-damage, roll bonuses, stat bonuses, etc.

#### Character Class Extensions
- Temporary stat bonuses with duration tracking
- Health percentage calculations
- Stat threshold checking
- Effect duration management

#### JSON Integration
- All 30 actions loaded from Actions.json
- Automatic property parsing from descriptions
- Fallback to hardcoded actions if JSON fails

#### Testing System
- Comprehensive test suite for all 30 actions
- Verification of all advanced mechanics
- Integration with existing test framework

---

## Combat Balance System

The game implements a sophisticated "actions to kill" balance system that ensures consistent combat duration across all levels.

### DPS Balance Implementation

#### Target Specifications
- **Level 1 DPS Target**: ~2.0 DPS for both heroes and enemies
- **Actions to Kill Target**: ~10 actions at level 1
- **Health at Level 1**: 18 base + 2 per level = 20 health
- **Damage at Level 1**: ~17 base damage per action

#### DPS Calculations

**Level 1 Player DPS:**
- **Base Damage**: STR(7) + Highest(7) + Weapon(~3) = ~17 damage
- **Attack Time**: 10.0 - (7 × 0.1) = 9.3 seconds
- **DPS**: 17 ÷ 9.3 = ~1.83 DPS

**Level 1 Enemy DPS:**
- **Base Damage**: STR(~7) + Highest(~7) = ~14 damage
- **Attack Time**: 10.0 - (7 × 0.1) = 9.3 seconds  
- **DPS**: 14 ÷ 9.3 = ~1.51 DPS
- **With Archetype Multipliers**: 1.51 × 0.7-1.2 = ~1.1-1.8 DPS

**Actions to Kill Calculation:**
- **Level 1 Health**: 18 base + 2 per level = 20 health
- **Player vs Enemy**: 20 health ÷ 1.83 DPS = ~11 actions
- **Enemy vs Player**: 20 health ÷ 1.51 DPS = ~13 actions

### Scaling Philosophy

#### Level Progression
- **Health Scaling**: +2 per level (linear)
- **Damage Scaling**: +1 attribute per level (linear)
- **DPS Scaling**: Gradual increase through attribute growth
- **Actions to Kill**: Remains relatively constant (~10-15 actions)

#### Archetype Balance
Different enemy archetypes maintain the same target DPS but distribute it differently:
- **Berserker**: Fast attacks (1.4x speed, 0.71x damage)
- **Assassin**: Quick strikes (1.2x speed, 0.83x damage)  
- **Warrior**: Balanced (1.0x speed, 1.0x damage)
- **Brute**: Slow but strong (0.75x speed, 1.1x damage)
- **Juggernaut**: Very slow but powerful (0.6x speed, 1.2x damage)

### Critical Balance Fixes

#### Problem Identified
The balance was severely broken with:
- **Hero dealing 32 damage** to enemies with only 26 health
- **Enemies dying in 1 hit** instead of the target 10 actions
- **Enemy STR showing 30** instead of expected ~7-8 values
- **Weapon damage scaling was 3-4x too high**

#### Root Causes Fixed

**1. Enemy Stat Double-Scaling:**
- **Problem**: Enemy stats were being scaled twice (first in EnemyLoader.cs, then in Enemy.cs constructor)
- **Fix**: Removed double scaling in Enemy.cs constructor

**2. Weapon Damage Scaling Too High:**
- **Problem**: Weapon damage formulas were adding massive multipliers (1.55x)
- **Fix**: Dramatically reduced weapon scaling multipliers to 0.42x

**3. Enemy DPS System Mismatch:**
- **Problem**: EnemyDPSSystem was using old level scaling formula
- **Fix**: Updated to match current system with no level bonus

### Combat Balance Changes

#### Health Scaling Increases
- **Player Base Health**: 150 → 200 (+33% increase)
- **Health Per Level**: 3 → 5 (+67% increase)
- **Enemy Health Per Level**: 4 → 6 (+50% increase)

#### Attribute Scaling Reductions
- **Player Base Attributes**: 15 → 8 (all stats reduced by ~47%)
- **Enemy Primary Attribute Bonus**: 3 → 2 (reduced enemy scaling)

#### Combat Multiplier Reductions
- **Critical Hit Multiplier**: 1.5 → 1.3 (reduced crit damage)
- **Enemy Damage Multiplier**: 1.0 → 0.7 (30% reduction in enemy damage)

#### Action Damage Multiplier Reductions
High-damage actions reduced from 1.6x to 1.2x (-25% damage):
- CRIT, SHIELD BASH, SHARP EDGE, BLOOD FRENZY, DEAL WITH THE DEVIL, BERZERK, SWING FOR THE FENCES, TRUE STRIKE, LAST GRASP, SECOND WIND, QUICK REFLEXES, PRETTY BOY SWAG

Environmental actions reduced from 1.8x to 1.3x (-28% damage):
- LAVA SPLASH, GHOSTLY WHISPER, SKELETON HAND, RISING DEAD, BOSS RAGE, EARTHQUAKE, CRYSTAL SHARDS, DIVINE JUDGMENT

### Benefits of Balance System

1. **Predictable Combat Duration**: Players can expect roughly 10-15 actions per fight
2. **Scalable Balance**: System works at all levels with consistent feel
3. **Strategic Depth**: More actions = more opportunities for combos and strategy
4. **Clear Progression**: Higher levels = more health and damage, but similar fight duration
5. **Archetype Variety**: Different enemy types feel distinct while maintaining balance

---

## Damage Display System

The game features a comprehensive damage display system that shows both raw damage and armor calculations for complete transparency.

### Armor Breakdown Display

#### Enhancement Request
User requested that damage displays show the armor calculation breakdown in the format:
`deals X damage -(Y armor) = Z damage`

#### Implementation
Created `FormatDamageDisplay()` method in `Combat.cs` that:
- Takes raw damage, actual damage, and target information
- Calculates target's armor value
- Returns formatted string showing the breakdown

#### Display Examples

**Before Enhancement:**
```
[Kobold] uses [Sneak Attack] on [Pax Moonwhisper]: deals 8 damage.
```
*Player couldn't see why 48 raw damage became 8 actual damage*

**After Enhancement:**
```
[Kobold] uses [Sneak Attack] on [Pax Moonwhisper]: deals 48 damage -(40 armor) = 8 damage.
```
*Player can clearly see the armor reduction calculation*

**No Armor Example:**
```
[Kobold] uses [Sneak Attack] on [Goblin]: deals 14 damage.
```
*Simple format when no armor is involved*

### Damage Display Fix

#### Problem Identified
The combat log was showing raw damage before armor reduction, which was confusing and misleading.

#### Fix Applied
Modified all damage display messages throughout the codebase to show the actual damage dealt by calling `CalculateDamage()` which includes armor reduction.

**Fixed all damage displays:**
- Basic attacks
- Critical attacks  
- Combo attacks
- Unique actions
- Auto-success attacks
- Divine reroll attacks
- DEJA VU attacks
- Enemy attacks

#### Benefits
1. **Accurate Information**: Players see exactly how much damage was actually dealt
2. **Clear Combat Feedback**: No confusion about why high damage numbers don't match health loss
3. **Better Game Understanding**: Players can see the effectiveness of armor
4. **Consistent Display**: All damage messages now show actual damage dealt
5. **Transparency**: Players can see exactly how armor affects damage
6. **Educational**: Helps players understand the armor system
7. **Debugging**: Makes it easier to verify armor calculations

### Technical Details

- **Smart Formatting**: Only shows breakdown when armor actually reduces damage
- **Simple Format**: Shows just the final damage when no armor is involved
- **Comprehensive**: Covers all attack types (basic, critical, combo, unique, etc.)
- **Efficient**: Reuses existing armor calculation logic
- **Maintainable**: Single method handles all formatting logic

---

## Dynamic Tuning System

The Dynamic Tuning System allows real-time adjustment and balancing of game parameters without requiring code recompilation. This system provides comprehensive tools for tuning combat mechanics, item scaling, progression curves, and rarity systems.

### Features

#### 🔧 Real-time Parameter Adjustment
- Modify combat parameters (damage, critical hits, attack speeds)
- Adjust item scaling formulas for weapons and armor
- Fine-tune rarity system multipliers
- Customize progression curves for experience and attributes

#### 📊 Balance Analysis Tools
- DPS calculation and analysis across different levels and tiers
- Item distribution analysis with statistical reporting
- Automated combat scenario testing
- Progression curve analysis and growth rate calculations

#### 💾 Configuration Management
- Export current configurations to timestamped files
- Import configuration presets
- Hot-reload configurations without restarting the game
- Formula-based scaling with mathematical expression support

### How to Access

1. **From Main Menu**: Select option "4. Tuning Console" from the main game menu
2. **In-Game**: The tuning console provides real-time access to all parameters

### Tuning Console Menu

#### 1. Combat Parameters
- Critical Hit Threshold and Multiplier
- Minimum Damage values
- Base Attack Time and Agility modifiers

#### 2. Item Scaling Formulas
- Weapon damage scaling by tier and level
- Armor value scaling formulas
- Real-time formula adjustment with immediate effect

#### 3. Rarity System
- Stat bonus multipliers for each rarity tier
- Drop chance formulas
- Rarity distribution tuning

#### 4. Progression Curves
- Experience requirement formulas
- Attribute growth equations
- Linear and quadratic growth factors

#### 5. Test Scaling Calculations
- Live testing of scaling formulas
- Weapon and armor scaling demonstrations
- Rarity multiplier verification

#### 6. Run Balance Analysis
- Comprehensive DPS analysis
- Item generation distribution testing
- Combat scenario simulations
- Progression curve analysis

### Configuration Files

#### Primary Configuration
- `GameData/TuningConfig.json` - Main configuration file with all tuning parameters

#### Extended Configuration
- `GameData/ItemScalingConfig.json` - Weapon-type specific scaling formulas and rarity modifiers

### Formula System

The system supports mathematical expressions with variables:

```json
{
  "WeaponDamageFormula": {
    "Formula": "BaseDamage * (1 + (Tier - 1) * TierScaling + Level * LevelScaling)",
    "TierScaling": 2.5,
    "LevelScaling": 0.8
  }
}
```

#### Supported Operations
- Basic arithmetic: `+`, `-`, `*`, `/`
- Power operations: `^` (converted to Math.Pow)
- Parentheses for grouping
- Variable substitution with exact matching

#### Common Variables
- `BaseDamage`, `BaseArmor` - Base item values
- `Tier` - Item tier (1-5)
- `Level` - Player/enemy level
- `PlayerLevel` - Specific player level
- `TierScaling`, `LevelScaling` - Scaling factors

### Balance Analysis Features

#### DPS Analysis
- Calculates theoretical DPS for different weapon tiers and player levels
- Factors in critical hits, combo potential, and attribute scaling
- Provides growth rate analysis across level ranges

#### Item Distribution Analysis
- Simulates large-scale item generation (1000+ samples)
- Reports tier and rarity distribution percentages
- Identifies potential balance issues automatically

#### Combat Scenarios
- Automated combat simulations with various player/enemy matchups
- Win rate calculations and average combat duration
- Player survivability analysis

#### Progression Analysis
- Experience requirement curves
- Attribute growth visualization
- Growth rate calculations and balance recommendations

### Usage Examples

#### Adjusting Weapon Scaling
1. Access Tuning Console → Item Scaling Formulas
2. Modify Tier Scaling or Level Scaling values
3. Test changes with "Test Scaling Calculations"
4. Run balance analysis to verify impact

#### Balancing Rarity System
1. Access Tuning Console → Rarity System
2. Adjust multipliers for specific rarity tiers
3. Run "Item Distribution Analysis" to verify changes
4. Export configuration when satisfied

#### Combat Balancing
1. Access Tuning Console → Combat Parameters
2. Adjust critical hit rates, damage multipliers
3. Run "Combat Scenario Testing" to evaluate impact
4. Fine-tune based on win rates and combat duration

### Tips for Effective Tuning

#### Start Small
- Make incremental changes (10-20% adjustments)
- Test after each change
- Use the analysis tools to verify impact

#### Use Analysis Tools
- Always run balance analysis after major changes
- Pay attention to growth rates and distribution warnings
- Test multiple scenarios before finalizing changes

#### Document Changes
- Export configurations with descriptive names
- Keep notes on what changes were made and why
- Test both early game and late game scenarios

#### Common Balance Points
- **Early Game**: Focus on tier 1-2 weapons, levels 1-5
- **Mid Game**: Balance tier 3-4 equipment, levels 6-15
- **Late Game**: Ensure tier 5 items and high-level scaling remain challenging

### Troubleshooting

#### Formula Errors
- Check variable names match exactly (case-sensitive)
- Ensure parentheses are balanced
- Verify mathematical operators are supported

#### Configuration Issues
- Use "Reload Configuration" if changes don't appear
- Check JSON syntax in configuration files
- Restart application if major structural changes are made

#### Performance Considerations
- Large analysis samples (>5000) may take time
- Complex formulas with many operations may slow calculations
- Consider simpler formulas for frequently-called calculations

### Integration with Existing Systems

The tuning system integrates seamlessly with:
- **LootGenerator**: Uses scaling formulas for item generation
- **Combat System**: Applies tuning parameters to damage calculations
- **Character Progression**: Uses progression curves for leveling
- **Enemy Scaling**: Applies scaling formulas to enemy stats

All changes take effect immediately without requiring game restart, making iterative tuning fast and efficient.

---

## Game Balance Tuning Configuration

This section contains all the key balance parameters that can be adjusted to tune the game difficulty and progression.

### Character & Health Scaling

#### Base Health
- **Player Base Health**: 50 (starting health for new characters)
- **Health Per Level**: 3 (health gained per character level)
- **Enemy Base Health**: 50 (base health for level 1 enemies)
- **Enemy Health Per Level**: 3 (health gained per enemy level)

#### Health Regeneration
- **Base Health Regen**: 0 (starting health regeneration)
- **Health Regen Scaling**: 0.1 (health regen gained per level)

### Attribute Scaling

#### Player Attribute Scaling
- **Base Attributes**: STR=5, AGI=5, TEC=5, INT=5 (starting attributes)
- **Attributes Per Level**: 1 (attributes gained per level)
- **Primary Attribute Bonus**: 2 (extra attribute per level for primary stat)

#### Enemy Attribute Scaling
- **Base Attributes**: STR=8, AGI=6, TEC=4, INT=4 (starting attributes)
- **Attributes Per Level**: 1 (attributes gained per enemy level)
- **Primary Attribute Bonus**: 2 (extra attribute per level for primary stat)

### Damage & Combat

#### Damage Calculation
- **Base Damage Formula**: STR + Highest Attribute + Weapon Damage + Roll Bonus
- **Critical Hit Threshold**: 20 (roll needed for critical hit)
- **Critical Hit Multiplier**: 2.0 (damage multiplier for critical hits)
- **Minimum Damage**: 1 (minimum damage dealt regardless of armor)

#### Attack Speed
- **Base Attack Speed**: 1.0 (starting attack speed)
- **Agility Speed Bonus**: 0.03 (attack speed gained per agility point)
- **Minimum Attack Speed**: 0.2 (minimum attack speed)

#### Armor System
- **Armor Reduction**: 1:1 (1 armor reduces 1 damage)
- **Enemy Armor Per Level**: 1 (armor gained per enemy level)

### Equipment & Items

#### Weapon Damage Scaling
- **Tier 1 Base Damage**: 5-8 (damage range for tier 1 weapons)
- **Tier 2 Base Damage**: 8-12 (damage range for tier 2 weapons)
- **Tier 3 Base Damage**: 12-16 (damage range for tier 3 weapons)
- **Tier 4 Base Damage**: 16-20 (damage range for tier 4 weapons)
- **Tier 5 Base Damage**: 20-25 (damage range for tier 5 weapons)

#### Armor Scaling
- **Tier 1 Base Armor**: 2-4 (armor range for tier 1 armor)
- **Tier 2 Base Armor**: 4-6 (armor range for tier 2 armor)
- **Tier 3 Base Armor**: 6-8 (armor range for tier 3 armor)
- **Tier 4 Base Armor**: 8-10 (armor range for tier 4 armor)
- **Tier 5 Base Armor**: 10-12 (armor range for tier 5 armor)

#### Item Bonuses
- **Bonus Damage Range**: 1-10 (random bonus damage on weapons)
- **Bonus Attack Speed Range**: 1-10 (random bonus attack speed on weapons)

### Experience & Progression

#### Experience Scaling
- **Base XP to Level 2**: 100 (XP needed for first level up)
- **XP Scaling Factor**: 1.5 (multiplier for each subsequent level)
- **Enemy XP Reward Base**: 10 (base XP reward from enemies)
- **Enemy XP Per Level**: 5 (additional XP per enemy level)

#### Gold Rewards
- **Enemy Gold Base**: 5 (base gold reward from enemies)
- **Enemy Gold Per Level**: 3 (additional gold per enemy level)

### Loot & Rarity

#### Loot Generation
- **Loot Chance Base**: 0.3 (30% chance for loot to drop)
- **Loot Chance Per Level**: 0.05 (5% additional chance per level)
- **Maximum Loot Chance**: 0.8 (80% maximum loot chance)

#### Rarity Weights
- **Common Weight**: 50 (weight for common items)
- **Uncommon Weight**: 25 (weight for uncommon items)
- **Rare Weight**: 15 (weight for rare items)
- **Epic Weight**: 8 (weight for epic items)
- **Legendary Weight**: 2 (weight for legendary items)

### Combat Mechanics

#### Roll System
- **Miss Threshold**: 1-5 (rolls that result in misses)
- **Basic Attack Threshold**: 6-13 (rolls for basic attacks)
- **Combo Threshold**: 14-20 (rolls for combo actions)
- **Critical Threshold**: 20+ (rolls for critical hits)

#### Combo System
- **Combo Reset on Miss**: true (combo resets when missing)
- **Combo Reset on Basic Attack**: true (combo resets on basic attacks)
- **Combo Amplifier Base**: 1.0 (base combo damage multiplier)
- **Combo Amplifier at TEC=5**: 1.05 (amplifier when Technique is 5)
- **Combo Amplifier Max**: 2.0 (maximum amplifier at max Technique)
- **Combo Amplifier Max Tech**: 100 (Technique level for maximum amplifier)

#### Combo Damage Scaling
- **Formula**: Linear scaling from 1.05 (at TEC=5) to 2.0 (at TEC=100)
- **Exponential Scaling**: Each combo step multiplies damage by amp^step
  - Combo 1: amp^1 (e.g., 1.05^1 = 1.05x damage)
  - Combo 2: amp^2 (e.g., 1.05^2 = 1.10x damage)
  - Combo 3: amp^3 (e.g., 1.05^3 = 1.16x damage)
  - Combo 4: amp^4 (e.g., 1.05^4 = 1.22x damage)
  - Combo 5: amp^5 (e.g., 1.05^5 = 1.28x damage)

### Enemy Scaling

#### Enemy Difficulty
- **Enemy Health Multiplier**: 1.0 (global enemy health multiplier)
- **Enemy Damage Multiplier**: 1.0 (global enemy damage multiplier)
- **Enemy Level Variance**: ±1 (enemy level can vary by this amount from room level)

#### Enemy Types
- **Goblin**: Base Health=80, STR=4, AGI=6, TEC=2, INT=3, Primary=AGI
- **Orc**: Base Health=95, STR=10, AGI=4, TEC=2, INT=2, Primary=STR
- **Skeleton**: Base Health=85, STR=6, AGI=4, TEC=3, INT=4, Primary=STR
- **Bandit**: Base Health=85, STR=6, AGI=8, TEC=3, INT=5, Primary=AGI
- **Cultist**: Base Health=85, STR=4, AGI=5, TEC=8, INT=7, Primary=TEC
- **Spider**: Base Health=75, STR=3, AGI=10, TEC=3, INT=2, Primary=AGI
- **Slime**: Base Health=95, STR=7, AGI=3, TEC=2, INT=1, Primary=STR
- **Bat**: Base Health=70, STR=3, AGI=12, TEC=4, INT=3, Primary=AGI
- **Zombie**: Base Health=100, STR=8, AGI=2, TEC=1, INT=1, Primary=STR
- **Wraith**: Base Health=85, STR=5, AGI=7, TEC=6, INT=8, Primary=TEC

### Action System

#### Action Probabilities
- **Basic Attack Weight**: 0.7 (70% chance for basic attack)
- **Special Action Weight**: 0.3 (30% chance for special action)

#### Action Effects
- **Bleed Damage Per Turn**: 2 (damage from bleed effect)
- **Bleed Duration**: 3 (turns bleed lasts)
- **Stun Duration**: 1 (turns stun lasts)
- **Weaken Damage Reduction**: 0.2 (20% damage reduction from weaken)

### Environment Effects

#### Environmental Modifiers
- **Forest Damage Bonus**: 1.0 (no change)
- **Lava Damage Bonus**: 1.2 (20% damage increase)
- **Ice Damage Reduction**: 0.8 (20% damage reduction)
- **Swamp Speed Reduction**: 0.9 (10% speed reduction)

### UI & Display

#### Text Display
- **Enable Text Delays**: true (enable combat text delays)
- **Base Delay Per Action**: 400 (milliseconds delay per action length)
- **Minimum Delay**: 50 (minimum delay in milliseconds)
- **Combat Speed Multiplier**: 1.0 (speed multiplier for combat text)

### Save System

#### Auto-Save
- **Auto-Save on Level Up**: true (save automatically when leveling up)
- **Auto-Save on Death**: true (save automatically when dying)
- **Auto-Save Interval**: 300 (seconds between auto-saves)

### Usage Notes

- All values can be adjusted to fine-tune game balance
- Decimal values are supported for multipliers and percentages
- Boolean values use true/false
- Ranges are specified as "min-max" format
- Some values may require game restart to take effect
- Test changes in small increments to maintain game balance

## Related Documentation

- **`ARCHITECTURE.md`**: Detailed system architecture and design patterns
- **`DEVELOPMENT_GUIDE.md`**: Comprehensive development guide
- **`TASKLIST.md`**: Current development tasks and progress
- **`QUICK_REFERENCE.md`**: Fast lookup for game systems
- **`INDEX.md`**: Complete documentation index

---

*This overview will be updated as new features and systems are added to the project.* 