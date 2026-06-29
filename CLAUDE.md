# DungeonFighter-v2 — Codebase Reference

## Project Overview

- **Type:** .NET 8.0 desktop game using Avalonia UI (v11.2.7)
- **Assembly:** `DF` (entry point: `RPGGame.Program`)
- **Namespace:** `RPGGame`
- **Project file:** `Code/Code.csproj`
- **Key dependencies:** Avalonia 11.2.7, ModelContextProtocol 0.5.0-preview.1, SoundFlow (audio), NLayer (MP3), Google.Apis.Sheets.v4

## Directory Structure

```
Code/               C# source (single .csproj)
  Combat/           Combat engine, calculators, narrative, status effects
  Config/           Config classes (UI, Enemy archetypes/balance/DPS/scaling)
  Entity/           Actor, Character, Enemy, builders, managers
  Game/             State machine, handlers, menu system, tuning tools
  Items/            Item, inventory, combo management
  Tests/            Unit tests
  UI/               Avalonia UI, color system, renderers, display
  Utils/            RandomUtility, helpers
GameData/           Runtime data (JSON)
  ActionTables.json         All action definitions
  ModificationTables.json   Item mod tables
  RarityTable.json          Loot rarity weights
  TierDistribution.json     Tier drop distribution
  FlavorText.json           Name/narrative text generation
  EnvironmentalActions.json Room hazard actions
  ColorCodes.json / ColorTemplates.json  UI color definitions
  CombatDelayConfig.json / TitleAnimationConfig.json  Timing
  BalancePatches/           Versioned balance iteration snapshots
  character_*_save.json     Live save files
  character_*_dead.json     Dead character records
  Audio/Music/              MP3 tracks
```

## Core Architecture

### Entity Hierarchy

```
Actor (abstract)               — action pool, all status effects
  └── Character : Actor        — player character (composition-based)
        └── Enemy : Character  — enemy entities
```

**`Actor`** (`Code/Entity/Actor.cs`) — base for every combat participant.
- `ActionPool` — list of `(Action, probability)` tuples; `SelectAction()` does weighted random pick
- Status effects stored directly: poison/burn/bleed (DoT), stun, weaken, roll penalty, damage reduction, vulnerability, harden, fortify, focus, expose, HP regen, armor break, pierce, reflect, silence, stat drain, absorb, temp HP, confusion, mark
- DoT tick methods: `ProcessPoison`, `ProcessBurn`, `ProcessBleedOnAction`
- `UpdateTempEffects(actionLength)` — decay all timed effects each turn

**`Character`** (`Code/Entity/Character.cs`) — player character via composition:
| Component | Class | Responsibility |
|---|---|---|
| Stats | `CharacterStats` | STR/AGI/TEC/INT, class points, temp bonuses |
| Effects | `CharacterEffects` | combo state, shields, rerolls, slow, misc buffs |
| Equipment | `CharacterEquipment` | slot references (Head/Body/Legs/Feet/Weapon) |
| Progression | `CharacterProgression` | level, XP, thresholds |
| Actions | `CharacterActions` | action pool and combo sequence |
| Health | `CharacterHealthManager` | current/max HP, armor, damage, heal |
| Combat | `CharacterCombatCalculator` | damage/speed calculations |
| Facade | `CharacterFacade` | unified public API, delegates to all above |
| Equipment Mgr | `EquipmentManager` | equip/unequip logic |
| Level Up Mgr | `LevelUpManager` | level up/down logic |
| Session Stats | `SessionStatistics` | per-run tracking (kills, damage, etc.) |

Core stats: `Strength`, `Agility`, `Technique`, `Intelligence`  
Class points: `BarbarianPoints`, `WarriorPoints`, `RoguePoints`, `WizardPoints`  
Combo system: `ComboStep`, `ComboSequence`, `IncrementComboStep(lastAction)` with routing via `ComboRouter`  
Save/load: `Character.SaveCharacter()` / `Character.LoadCharacterAsync()` → delegates to `CharacterFacade` → `CharacterSaveService` → JSON in `GameData/`

### Game State Machine

**`GameStateManager`** (`Code/Game/GameStateManager.cs`) — central state authority.
- State: `GameState` enum (MainMenu, Combat, Dungeon, etc.)
- `TransitionToState(newState)` — validates via `GameStateValidator`, fires `StateChanged` event
- `CharacterStateManager` — multi-character registry (add/switch/remove characters)
- `CharacterContext` — per-character dungeon/room context
- Key events: `StateChanged`, `CharacterSwitched`
- `PushComboStripEncounterLock()` / `PopComboStripEncounterLock()` — blocks combo reorder during encounters

**`GameConfiguration`** (`GameConfiguration.Instance`) — singleton tuning config accessed everywhere.  
**`GameConstants`** — static game constants (DefaultRegionId, ComboSequenceIntelligenceThreshold, etc.)

### Combat System (`Code/Combat/`)

| File | Role |
|---|---|
| `CombatManager.cs` | Main combat orchestrator |
| `CombatCalculator.cs` | Damage, roll, armor calculations |
| `TurnManager.cs` | Turn order and speed |
| `CombatTurnHandlerSimplified.cs` | Per-turn execution flow |
| `CombatStateManager.cs` | Combat-local state |
| `BattleNarrativeGenerator.cs` | Text generation for combat events |
| `BattleNarrativeDisplay.cs` | Renders narrative to UI |
| `TurnManager.cs` | Turn ordering |
| `BeatMultipliers.cs` / `BeatTimingConfiguration.cs` | Rhythm-based combat timing multipliers |
| `CombatEventBus.cs` | Event-driven combat notifications |
| `StatusEffectProcessor.cs` | Per-turn status effect ticking |
| `HitCalculator.cs` | Hit/miss d20 resolution |
| `DamageCalculator.cs` | Final damage with modifiers |
| `SpeedCalculator.cs` | Action speed/turn length |
| `OutcomeHandler.cs` / `OutcomeHandlerRegistry.cs` | Action outcome dispatch |
| `EffectHandlerRegistry.cs` | Status effect application dispatch |
| `ThresholdManager.cs` | Health milestone tracking |
| `TagDamageCalculator.cs` | Tag-based damage bonuses |

Status effect handlers live in `Code/Combat/Effects/` and `Code/Combat/Combat/Effects/AdvancedStatusEffects/`.

### Menu System (`Code/Game/Menu/`)

Command pattern:
- `IMenuCommand` — interface for all menu commands
- Concrete commands: `SelectOptionCommand`, `ConfirmCharacterCommand`, `DecreaseStatCommand`, `IncreaseStatCommand`, `CancelCommand`, `ToggleOptionCommand`, `SettingsCommand`, `LoadGameCommand`, `StartNewGameCommand`, `RandomizeCharacterCommand`
- `IMenuHandler` / `MenuHandlerBase` — handler base with `MenuContext`
- `MenuInputRouter` + `MenuInputValidator` — route and validate raw input
- `MenuStateTransitionManager` — drive `GameStateManager` from menu events
- Validation rules per state: `CharacterCreationValidationRules`, `DungeonSelectionValidationRules`, `WeaponSelectionValidationRules`, `MainMenuValidationRules`, `SettingsValidationRules`, `InventoryValidationRules`

### Items (`Code/Items/`)

- `Item.cs` — base item with slots, rarity, mods, bonuses
- `InventoryManager.cs` — add/remove/sort inventory
- `InventoryOperations.cs` — equip/unequip business logic
- `ComboManager.cs` — combo action management for items
- `BasicGearConfig.cs` — gear slot definitions
- `ItemPrefixHelper.cs` — rarity/tier name generation
- `AttributeRequirement.cs` — stat thresholds for equipping

Item mods are defined in `GameData/ModificationTables.json`.

### Actions

Actions loaded from `GameData/ActionTables.json` via `ActionLoader`.  
`Action` has: Name, Type (`ActionType`: Attack, Spell, Support, etc.), Tags (string list), ComboOrder, length, damage/effect values.  
Tags drive behavior via `GameDataTagHelper`, `TagDamageCalculator`, `ActionMechanicTagProcessor`, `ActionRollTagProcessor`.  
Environment/hazard actions from `GameData/EnvironmentalActions.json`.

### UI (`Code/UI/`)

- **Avalonia layer** (`UI/Avalonia/`): `CanvasLayoutManager`, `DisplayModeManager`, segment renderers (`StandardSegmentRenderer`, `TemplateSegmentRenderer`), menu renderers, `TextWrapper`, scroll/buffer management
- **Color system** (`UI/ColorSystem/`): `ColoredTextBuilder`, `ColorLayerSystem`, `CharacterColorProfile`, `ColorUtils`, `ItemComparisonFormatter`, `MenuDisplayColoredText`; rendered by `FormatRenderer` / `ConsoleColoredTextRenderer`
- **Block display** (`UI/BlockDisplay/`): `BlockRendererFactory`
- **Spacing** (`UI/Spacing/`): `SpacingRules`, `SpacingValidator`, `CombatLogSpacingManager`
- `IUIManager` — UI abstraction interface
- `UIConfig.cs` — UI dimension/font config
- `BrightnessMaskConfiguration.cs` — brightness overlay settings

### MCP Integration (`RPGGame.MCP`)

The game exposes itself as an MCP server for AI control:
- `GamePlaySession` (`Code/Game/GamePlaySession.cs`) — session lifecycle (Initialize, StartNewGame, ExecuteAction, GetGameState, SaveGame)
- `GameWrapper` + `McpToolState` — bridge between MCP tools and game instance
- Tool groups: `GameControlTools`, `NavigationTools`, `InformationTools`
- `InteractiveMCPGamePlayer.cs`, `AutomatedGameplaySession.cs`, `FastAutomatedPlayer.cs` — player implementations

### Balance / Tuning Tools

All in `Code/Game/`:
- `AutomatedTuningEngine.cs` — run simulations and auto-apply suggestions
- `BalanceValidator.cs` + `BalanceTuningGoals.cs` — validate balance targets
- `MatchupAnalyzer.cs` — character vs enemy matchup stats
- `ParameterSensitivityAnalyzer.cs` — sensitivity sweeps
- `WhatIfTester.cs` — hypothetical config testing
- `SimulationData.cs` — simulation result models
- `Tuning/Suggesters/` — adjustment suggesters per domain (enemy, player, weapon, global, duration)
- `Tuning/TuningSuggestionApplier.cs` / `TuningSummaryGenerator.cs` / `UndoRedoManager.cs`
- `BattleStatistics/BattleStatisticsCalculator.cs` / `BattleExecutor.cs` — aggregate battle stats
- Google Sheets export via `Google.Apis.Sheets.v4`

### Action Interaction Lab

`Code/Game/ActionInteractionLab/` — sandbox for testing action interactions mid-game:
- `LabStep.cs`, `ActionLabCatalogSync.cs`, `ActionLabRightPanelEnemyAdjustment.cs`
- `Character.ApplyActionLabLevelDelta(delta)` — level up/down without banners
- `Character.ActionLabArmorBonus` — sandbox armor override

## Key Patterns

**Config access:** `GameConfiguration.Instance.Character.PlayerBaseHealth` etc. — always use the singleton.

**Facade pattern:** `Character` delegates everything through `CharacterFacade`; never bypass it to reach sub-components directly.

**Composition over inheritance:** `Character` has `Stats`, `Effects`, `Equipment`, etc. as properties rather than deep inheritance.

**Data-driven:** Actions, mods, rarities, enemies, flavor text — all JSON in `GameData/`.

**Event system:** State transitions fire `StateChanged`; combat fires via `CombatEventBus`; character switches fire `CharacterSwitched`.

**Save files:** Named `character_{FirstName}_{LastName}_{Level}_{shortHash}_save.json` or `_dead.json`. Loaded async via `Character.LoadCharacterAsync()`.

## Build

```bash
cd Code
dotnet build          # Debug
dotnet run            # Run
dotnet publish -r win-x64 --self-contained   # Single-file release
```

Output assembly: `DF.exe`. Avalonia/SkiaSharp native libs are self-extracted at startup (`IncludeNativeLibrariesForSelfExtract=true`).

## Tests

Unit tests in `Code/Tests/Unit/`. Run with `dotnet test` from the `Code/` directory.
Key test files: `ActionExecutionComponentsTest`, `BattleNarrativeManagersTest`, `GameDataGeneratorTest`, `TierDistributionTest`, `ColoredTextBuilderTest`.
