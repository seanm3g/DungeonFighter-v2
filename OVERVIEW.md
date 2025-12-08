# DungeonFighter Game Overview

**Version 7.0** - *Advanced Mechanics*

## General Description

DungeonFighter is a sophisticated turn-based RPG/dungeon crawler game written in C# using .NET 8.0. The game features advanced combat mechanics with combo systems, intelligent battle narrative generation, and a comprehensive data-driven architecture. Built with modular design principles using established design patterns, the codebase is maintainable, extensible, and thoroughly tested.

## Quick Start

### Installation
```bash
# Clone and navigate to the project
git clone <repo-url>
cd DungeonFighter-v2

# Build and run
cd Code
dotnet run
```

### For Developers
- **Architecture Overview**: See `Documentation/01-Core/ARCHITECTURE.md`
- **Development Guide**: See `Documentation/02-Development/DEVELOPMENT_GUIDE.md`
- **Code Patterns**: See `Documentation/02-Development/CODE_PATTERNS.md`
- **Testing Guide**: See `Documentation/03-Quality/TESTING_STRATEGY.md`

## Core Features

### 🎮 Advanced Combat System
- **Turn-based Combat:** Strategic combat with cooldown-based action timing
- **Action Combo System:** Chain actions together for increased damage (1.85x multiplier per combo step)
- **Dice-based Mechanics:** 1d20 roll system with strategic thresholds
- **Roll Modification System:** Additive, multiplicative, clamp, reroll, exploding dice, multiple dice modes
- **Dynamic Thresholds:** Per-actor critical hit, combo, and hit threshold overrides
- **Conditional Triggers:** Event-driven action effects (OnMiss, OnHit, OnCritical, etc.)
- **Combo Routing:** Jump, skip, repeat, loop, stop, and random combo flow control
- **Tag System:** Flexible tag-based matching for damage modification and filtering
- **Outcome Handlers:** Conditional effects based on combat results (enemy death, HP thresholds)
- **Environmental Actions:** Room-specific effects that impact combat
- **Intelligent Delay System:** Optimized pacing matching action intensity

### 👤 Character & Progression System
- **Dynamic Stats:** Strength, Agility, Technique, Intelligence with level-based scaling
- **XP & Leveling:** Automatic stat increases and health restoration on level up
- **Equipment System:** Weapons, armor with tier-based stats and special abilities
- **Weapon-Based Classes:** Barbarian, Warrior, Rogue, Wizard with unique progression paths
- **Action Pool Management:** Dynamic action selection from equipped gear

### 👹 Enemy & AI System
- **18+ Enemy Types:** Each with unique stats, abilities, and specializations
- **Primary Attribute System:** Enemies specialize in Strength, Agility, or Technique
- **Level Scaling:** Dynamic stat scaling with proper DPS balance
- **Environment-Specific Spawning:** Different enemy types appear in themed dungeons

### 🗺️ Dungeon & Environment System
- **Procedural Generation:** 1-3 rooms per dungeon based on level
- **10 Themed Dungeons:** Forest, Lava, Crypt, Cavern, Swamp, Desert, Ice, Ruins, Castle, Graveyard
- **15+ Room Types:** Each with unique environmental actions and effects
- **Boss Chambers:** Special final rooms with powerful enemies

### 📖 Battle Narrative System
- **Event-Driven Narrative:** Poetic descriptions for significant combat moments
- **Informational Summaries:** Clear, factual combat reporting for regular actions
- **Significant Event Detection:** First blood, health reversals, near death, combo achievements
- **Configurable Display:** Balance between narrative and informational modes

### ✨ Modern GUI & Visual Systems (v6.2)
- **Avalonia-Based Interface:** Modern desktop GUI with ASCII canvas rendering
- **Persistent Layout:** Always-visible character panel with stats, health, and equipment
- **Item Color System:** Rarity-based visual feedback (Common → Transcendent) with 7 tiers
- **Color Configuration:** Data-driven JSON system with 166+ templates and 200+ keywords
- **Title Screen Animation:** Smooth 30 FPS color transition animations
- **Chunked Text Reveal:** Progressive text display with natural timing
- **1920×1080 Resolution:** Optimized for modern displays
- **Mouse & Keyboard Support:** Full input support with clickable UI elements

### 🔧 Technical Features
- **Data-Driven Architecture:** All game data stored in JSON for easy modification
- **Design Patterns:** Facade, Factory, Registry, Builder, Strategy, Composition, Observer
- **Advanced Mechanics System:** Roll modification, event-driven triggers, combo routing, tag system
- **Status Effect System:** 23 total effects (6 basic + 17 advanced) with stacking and duration tracking
- **Dynamic Tuning System:** Real-time parameter adjustment with FormulaEvaluator
- **Comprehensive Testing:** 27+ test categories with balance analysis
- **Cross-Platform:** Runs on Windows, macOS, and Linux

## Project Structure

```
DungeonFighter-v2/
├── Code/                              # Main application code (organized by system)
│   ├── Actions/                       # Action system and mechanics
│   ├── Combat/                        # Combat logic and narrative
│   ├── Config/                        # Configuration classes by domain
│   ├── Data/                          # Loaders and generators
│   ├── Entity/                        # Character, Enemy, and managers
│   ├── Game/                          # Main game loop and state
│   ├── Items/                         # Item system and inventory
│   ├── UI/                            # Console and Avalonia UI
│   ├── Utils/                         # Shared utilities
│   ├── World/                         # Dungeon and environment systems
│   └── Tests/                         # Test framework
├── GameData/                          # JSON configuration files
│   ├── Actions.json                   # Action definitions
│   ├── Enemies.json                   # Enemy configurations
│   ├── Weapons.json                   # Weapon data
│   ├── Armor.json                     # Armor data
│   ├── Dungeons.json                  # Dungeon definitions
│   └── ... (20+ additional configs)
├── Documentation/                     # Comprehensive project documentation
│   ├── 01-Core/                       # Essential documentation
│   │   ├── ARCHITECTURE.md            # System architecture & design patterns
│   │   ├── OVERVIEW.md                # Detailed system overview
│   │   ├── TASKLIST.md                # Development tasks & progress
│   │   └── GAME_LOGIC_FLOWCHART.md   # Visual flowcharts
│   ├── 02-Development/                # Development guides
│   │   ├── DEVELOPMENT_GUIDE.md       # Comprehensive guide
│   │   ├── CODE_PATTERNS.md           # Patterns and conventions
│   │   ├── DEVELOPMENT_WORKFLOW.md    # Step-by-step process
│   │   ├── REFACTORING_HISTORY.md     # Recent refactoring changes
│   │   └── ... (40+ additional guides)
│   ├── 03-Quality/                    # Testing and performance
│   │   ├── TESTING_STRATEGY.md        # Testing approaches
│   │   ├── DEBUGGING_GUIDE.md         # Debugging techniques
│   │   └── PERFORMANCE_NOTES.md       # Performance considerations
│   ├── 04-Reference/                  # Quick reference
│   │   ├── QUICK_REFERENCE.md         # Fast lookups
│   │   └── INDEX.md                   # Documentation index
│   └── 05-Systems/                    # System-specific documentation
└── Scripts/                           # Build and utility scripts
    ├── count-cs-lines-no-tests.ps1    # Code metrics analyzer
    └── ... (additional scripts)
```

## Recent Updates

### Advanced Mechanics Implementation (v7.0)
- **Roll Modification System**: 9 new files for dice manipulation (additive, multiplicative, clamp, reroll, exploding, multi-dice)
- **Event System**: Observer pattern event bus for conditional triggers
- **Advanced Status Effects**: 17 new effect handlers (Vulnerability, Harden, Fortify, Focus, Expose, HP Regen, Armor Break, Pierce, Reflect, Silence, Stat Drain, Absorb, Temporary HP, Confusion, Cleanse, Mark, Disrupt)
- **Tag System**: 4 files for flexible tag-based matching and filtering
- **Combo Routing**: Flow control system for combo sequences
- **Outcome Handlers**: Conditional effects based on combat results
- **Total New Files**: ~35 files across 4 implementation phases

### Code Organization Improvements (v6.2)
- **BattleNarrative**: 550 → 118 lines (78.5% reduction)
- **Environment**: 763 → 182 lines (76% reduction)
- **CharacterEquipment**: 590 → 112 lines (81% reduction)
- **GameDataGenerator**: 684 → 68 lines (90% reduction)
- **Character**: 539 → 250 lines (54% reduction)

### Architecture Patterns
- **Registry Pattern**: EffectHandlerRegistry, EnvironmentalEffectRegistry, RollModifierRegistry, TagRegistry
- **Facade Pattern**: Enhanced with specialized managers
- **Builder Pattern**: CharacterBuilder, EnemyBuilder
- **Strategy Pattern**: Effect handlers and environmental effects
- **Observer Pattern**: CombatEventBus for event-driven mechanics
- **Singleton Pattern**: TagRegistry, CombatEventBus, ActionUsageTracker
- **Composition Pattern**: Throughout codebase for better modularity

See `Documentation/02-Development/REFACTORING_HISTORY.md` and `Documentation/04-Systems/ADVANCED_MECHANICS_IMPLEMENTATION.md` for detailed changes.

## Development Resources

### Essential Documentation
- **`Documentation/01-Core/ARCHITECTURE.md`** - Complete system architecture
- **`Documentation/02-Development/DEVELOPMENT_GUIDE.md`** - Development workflow
- **`Documentation/02-Development/CODE_PATTERNS.md`** - Code patterns and best practices
- **`Documentation/03-Quality/TESTING_STRATEGY.md`** - Testing approaches

### Quick References
- **`Documentation/04-Reference/QUICK_REFERENCE.md`** - Fast lookups
- **`Documentation/04-Reference/INDEX.md`** - Complete index
- **`README.md`** - Installation and quick start

## Performance Targets

| Metric | Target | Status |
|--------|--------|--------|
| Combat Response | <100ms for simple actions | ✅ Met |
| Menu Navigation | <50ms response time | ✅ Met |
| Data Loading | <500ms for all game data | ✅ Met |
| Memory Usage | <200MB peak usage | ✅ Met |
| Startup Time | <5 seconds total | ✅ Met |
| Frame Rate | 30+ FPS animations | ✅ Met |
| Combat Duration | ~10-15 actions per fight | ✅ Met |

## Game Balance

The game implements a sophisticated "actions to kill" balance system ensuring consistent combat duration:

- **Level 1 DPS Target**: ~2.0 DPS for both heroes and enemies
- **Actions to Kill Target**: ~10 actions at level 1
- **Health at Level 1**: 20 health
- **Damage at Level 1**: ~17 base damage per action

See `Documentation/01-Core/OVERVIEW.md` section "Combat Balance System" for detailed formulas and analysis.

## Testing

The game includes a comprehensive test suite with 27+ test categories:

1. Run the game: `dotnet run`
2. Select "Settings" from the main menu
3. Choose "Tests" from the settings menu
4. Select from available test categories or "All Tests"

Available test categories:
- Character Leveling & Stats
- Item Creation & Properties
- Combat Mechanics
- Combo System Tests
- Battle Narrative Generation
- Enemy Scaling & AI
- Balance Analysis
- And 20+ more...

See `Documentation/03-Quality/TESTING_STRATEGY.md` for detailed testing information.

## Current Implementation Status

| System | Status | Notes |
|--------|--------|-------|
| Core Systems | ✅ Complete | Fully tested and documented |
| Combat System | ✅ Complete | Advanced combo and narrative systems |
| Character System | ✅ Complete | Refactored with specialized managers |
| Enemy System | ✅ Complete | 18+ types with proper scaling |
| Dungeon System | ✅ Complete | Procedural generation working |
| Data System | ✅ Complete | JSON-driven content management |
| UI System | ✅ Complete | Modern Avalonia GUI implemented |
| Action System | ✅ Complete | Advanced mechanics with 30+ actions |
| World System | ✅ Complete | Environment effects and status system |
| Dynamic Tuning | ✅ Complete | Real-time parameter adjustment |

## Contribution Guidelines

1. Read `Documentation/02-Development/DEVELOPMENT_GUIDE.md`
2. Follow patterns in `Documentation/02-Development/CODE_PATTERNS.md`
3. Add tests for new functionality
4. Update relevant documentation
5. Run test suite before committing

## License

This project is open source. Feel free to modify and distribute according to your needs.

## Questions or Issues?

- **Architecture Questions**: See `Documentation/01-Core/ARCHITECTURE.md`
- **Development Questions**: See `Documentation/02-Development/DEVELOPMENT_GUIDE.md`
- **Known Issues**: See `Documentation/03-Quality/PROBLEM_SOLUTIONS.md`
- **Performance Issues**: See `Documentation/03-Quality/PERFORMANCE_NOTES.md`

---

For detailed information, see the **Documentation/** folder structure. Start with `Documentation/01-Core/` for essential guides.

