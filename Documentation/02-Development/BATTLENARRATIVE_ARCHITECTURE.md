# BattleNarrative Architecture - Post-Refactoring

## System Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Combat System Integration                │
│                        (CombatManager)                       │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            │ Uses
                            ↓
┌─────────────────────────────────────────────────────────────┐
│          BattleNarrative (Facade - ~200 lines)              │
│                                                              │
│  Responsibilities:                                          │
│  • Track battle events                                     │
│  • Coordinate specialized managers                         │
│  • Maintain narrative event logs                          │
│  • Provide simple public interface                        │
└─────────────────────────────────────────────────────────────┘
        │              │              │               │
        │ delegates    │ delegates    │ delegates     │ delegates
        ↓              ↓              ↓               ↓
    ┌────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────┐
    │ State  │  │   Text   │  │  Taunt   │  │    Event     │
    │Manager │  │ Provider │  │  System  │  │   Analyzer   │
    ├────────┤  ├──────────┤  ├──────────┤  ├──────────────┤
    │ 134    │  │   176    │  │   124    │  │     245      │
    │ lines  │  │  lines   │  │  lines   │  │    lines     │
    └────────┘  └──────────┘  └──────────┘  └──────────────┘
        │              │              │               │
        ↓              ↓              ↓               ↓
    ┌────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────┐
    │ • Flags│  │• Text    │  │• Taunts  │  │• Analysis    │
    │ • State│  │• Fallback│  │• Location│  │• Detection   │
    │ Count  │  │• Replace │  │• Threshold
    └────────┘  └──────────┘  └──────────┘  └──────────────┘
```

---

## Component Interaction Flow

### **1. Event Addition Flow**

```
BattleNarrative.AddEvent(evt)
    │
    ├─ Update health values
    │   └─ finalPlayerHealth, finalEnemyHealth
    │
    └─ eventAnalyzer.UpdateFinalHealth()
        └─ BattleEventAnalyzer
            │
            └─ AnalyzeEventForNarratives()
                │
                ├─ Checks first blood (via stateManager)
                ├─ Checks critical hits (via textProvider)
                ├─ Checks critical misses (via textProvider)
                ├─ Checks environmental effects (via stateManager)
                ├─ Checks health recovery (via textProvider)
                ├─ Checks health leads (via stateManager)
                ├─ Checks taunts (via tauntSystem)
                ├─ Checks health thresholds (via stateManager)
                ├─ Checks intense battle (via stateManager)
                ├─ Checks good combos (via stateManager)
                └─ Checks defeats (via stateManager)
                    │
                    └─ Returns list of triggered narratives
```

### **2. Narrative Retrieval Flow**

```
GetTriggeredNarratives()
    │
    └─ Get last event
        │
        └─ AnalyzeEventForNarratives()
            │
            └─ eventAnalyzer.AnalyzeEvent()
                │
                ├─ Call textProvider.GetRandomNarrative()
                │   └─ Load from FlavorText.json or fallback
                │
                ├─ Call tauntSystem.CheckPlayerTaunt()
                │   └─ Calculate threshold + location detection
                │
                ├─ Call stateManager flag methods
                │   └─ Check/set various state flags
                │
                └─ Return triggered narratives list
```

### **3. Taunt Generation Flow**

```
EventAnalyzer.AddTauntNarratives()
    │
    ├─ TrackActorAction() → stateManager
    │
    └─ tauntSystem.CheckPlayerTaunt()
        │
        ├─ GetPlayerTauntThreshold() → threshold calculation
        │
        ├─ Check if playerActionCount >= threshold
        │
        └─ If yes: GetLocationSpecificTaunt()
            │
            ├─ GetLocationType() → Extract location from environment name
            │
            ├─ textProvider.GetRandomNarrative(tauntKey)
            │
            └─ Replace placeholders: {name}, {enemy}, {player}
```

---

## State Management Lifecycle

### **Initialization**
```
BattleNarrative constructor
    │
    ├─ Create stateManager
    ├─ Create textProvider
    ├─ Create tauntSystem
    ├─ Create eventAnalyzer
    │
    └─ eventAnalyzer.Initialize(playerName, enemyName, environment, health)
```

### **During Battle**
```
For each action:
    │
    ├─ BattleNarrative.AddEvent(evt)
    │   └─ eventAnalyzer.AnalyzeEvent(evt)
    │       └─ Update stateManager flags
    │       └─ Generate narratives via textProvider
    │       └─ Check taunts via tauntSystem
    │       └─ Return triggered narratives
    │
    └─ Display narratives to player
```

### **Battle Completion**
```
Reset for next battle:
    │
    └─ Create new BattleNarrative instance
        └─ All managers reset with new context
```

---

## Data Flow

### **Event Data**
```
BattleEvent
├─ Actor: string
├─ Target: string
├─ Action: string
├─ Damage: int
├─ IsSuccess: bool
├─ IsCombo: bool
├─ ComboStep: int
├─ IsCritical: bool
├─ IsHeal: bool
├─ HealAmount: int
├─ EnvironmentEffect: string
├─ Roll: int
└─ Timestamp: DateTime
```

### **State Data**
```
NarrativeStateManager
├─ One-time flags (bool) - 6 flags
├─ Health threshold flags (bool) - 4 flags
├─ Health lead flags (bool) - 2 flags
├─ Action counters (int) - 2 counters
└─ Taunt counters (int) - 2 counters
```

### **Narrative Output**
```
List<string> narrativeEvents
├─ First blood event
├─ Critical hit events
├─ Environmental events
├─ Health threshold events
├─ Taunt events
├─ Health lead changes
├─ Defeat events
└─ Combo events
```

---

## Component Details

### **NarrativeStateManager**

```csharp
Key Responsibilities:
├─ Encapsulate all boolean flags
├─ Track action counters
├─ Track taunt counters
├─ Manage health threshold states
├─ Provide flag checking properties (HasX pattern)
└─ Provide state setting methods (SetX pattern)

Key Methods:
├─ ResetAllStates() - Clear all flags
├─ SetFirstBloodOccurred() - Mark first blood
├─ CanPlayerTaunt - Check if player can taunt
├─ IncrementPlayerActionCount() - Track actions
└─ SetPlayerHealthLead() - Update health lead
```

### **NarrativeTextProvider**

```csharp
Key Responsibilities:
├─ Load narrative text from FlavorText.json
├─ Provide fallback narratives
├─ Handle placeholder replacement
├─ Manage random selection
└─ Support 40+ narrative event types

Key Methods:
├─ GetRandomNarrative(eventType) - Load and randomize
├─ GetFallbackNarrative(eventType) - Fallback text
└─ ReplacePlaceholders(narrative, replacements) - Text substitution

Supported Events:
├─ firstBlood
├─ criticalHit, criticalMiss
├─ environmentalAction
├─ healthRecovery, healthLeadChange
├─ below50Percent, below10Percent
├─ playerDefeated, enemyDefeated
├─ intenseBattle
├─ playerTaunt_*, enemyTaunt_* (8 locations)
└─ escalatingTension
```

### **TauntSystem**

```csharp
Key Responsibilities:
├─ Detect location type from environment name
├─ Generate location-specific taunts
├─ Calculate taunt thresholds
├─ Check if taunt should trigger
└─ Replace taunt placeholders

Location Types:
├─ library
├─ underwater
├─ lava
├─ crypt
├─ crystal
├─ temple
├─ forest
└─ generic (default)

Key Methods:
├─ GetLocationType(environmentName) - Detect location
├─ GetLocationSpecificTaunt(...) - Generate taunt
├─ GetPlayerTauntThreshold(count, settings) - Threshold
├─ CheckPlayerTaunt(...) - Check trigger
└─ CheckEnemyTaunt(...) - Check trigger
```

### **BattleEventAnalyzer**

```csharp
Key Responsibilities:
├─ Analyze individual battle events
├─ Determine which narratives to trigger
├─ Coordinate between managers
├─ Track health percentages
├─ Detect significant events
└─ Manage event-to-narrative mapping

Key Methods:
├─ Initialize(...) - Set up context
├─ UpdateFinalHealth(...) - Update for calculations
├─ AnalyzeEvent(evt, settings) - Main analysis
├─ TrackActorAction(actorName) - Count actions
└─ Helper methods for each narrative type:
    ├─ AddHealthLeadNarratives(...)
    ├─ AddTauntNarratives(...)
    ├─ AddHealthThresholdNarratives(...)
    └─ AddIntenseBattleNarrative(...)
```

---

## Manager Coordination Example

### **Scenario: Player deals critical hit for 25 damage**

```
1. BattleNarrative.AddEvent(criticalHitEvent)
   └─ Update finalEnemyHealth: 50 - 25 = 25
   
2. eventAnalyzer.UpdateFinalHealth(100, 25)
   └─ Store latest health for percentage calculations
   
3. eventAnalyzer.AnalyzeEvent(evt)
   └─ Check Critical Hit:
      └─ textProvider.GetRandomNarrative("criticalHit")
         └─ Load from FlavorText.json
         └─ Replace "{name}" with "Hero"
         └─ Return "A devastating blow strikes true..."
      
   └─ Check Health Threshold (50%):
      └─ Calculate: 25 / 50 = 50%
      └─ stateManager.HasEnemyBelow50Percent? → false
      └─ stateManager.SetEnemyBelow50Percent()
      └─ textProvider.GetRandomNarrative("below50Percent")
      └─ Replace "{name}" with "Goblin"
      └─ Return "Goblin staggers under injuries..."
      
   └─ Return triggered narratives:
      ["A devastating blow strikes true...",
       "Goblin staggers under injuries..."]
```

---

## Extension Points

### **Add New Narrative Type**

1. Add fallback in `NarrativeTextProvider.GetFallbackNarrative()`
2. Add detection logic in `BattleEventAnalyzer` helper method
3. Update `FlavorText.json` with actual narratives
4. Add unit tests

### **Add New Location for Taunts**

1. Add location check in `TauntSystem.GetLocationType()`
2. Add location-specific narratives to `NarrativeTextProvider.GetFallbackNarrative()`
3. Update `FlavorText.json` with location taunts
4. Add unit tests

### **Change State Tracking**

1. Add new flag/counter to `NarrativeStateManager`
2. Add detection logic to `BattleEventAnalyzer`
3. Add unit tests for new state

---

## Benefits Summary

| Aspect | Benefit |
|--------|---------|
| **Readability** | Clear manager responsibilities |
| **Maintainability** | Changes isolated to specific managers |
| **Testability** | Each manager independently testable |
| **Reusability** | Managers can be used elsewhere |
| **Extensibility** | Easy to add new narrative types/locations |
| **Performance** | No degradation from modular design |
| **Documentation** | Clear separation of concerns |

---

## Related Files

- `Code/Combat/BattleNarrative.cs` - Main facade
- `Code/Combat/NarrativeStateManager.cs` - State management
- `Code/Combat/NarrativeTextProvider.cs` - Text generation
- `Code/Combat/TauntSystem.cs` - Taunt logic
- `Code/Combat/BattleEventAnalyzer.cs` - Event analysis
- `Code/Utils/FlavorText.cs` - Narrative text data
- `GameData/FlavorText.json` - Narrative configuration

---

*Architecture Documentation*  
*Pattern: Facade + Specialized Managers*  
*Status: Reference documentation*

