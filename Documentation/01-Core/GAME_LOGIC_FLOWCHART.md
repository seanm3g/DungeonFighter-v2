# DungeonFighter Game Logic Flowchart

This document provides a comprehensive flowchart of all game logic based on the architecture analysis.

## Complete Game Flow

```mermaid
flowchart TD
    A[Program.cs - Application Start] --> B[Game.cs Constructor]
    B --> C[GameTicker.Instance.Start]
    B --> D[GameMenuManager Creation]
    D --> E[ShowMainMenu]
    
    E --> F{Main Menu Choice}
    F -->|1| G[Start New Game]
    F -->|2| H[Load Existing Game]
    F -->|3| I[Show Settings]
    F -->|0| J[Exit Game]
    
    G --> K[GameInitializer.InitializeNewGame]
    H --> L[Character.LoadCharacter]
    L --> M{Character Found?}
    M -->|No| N[Start New Game Instead]
    M -->|Yes| O[GameInitializer.InitializeExistingGame]
    N --> K
    O --> P[Run Game Loop]
    K --> P
    
    P --> Q[GameLoopManager.RunGameLoop]
    Q --> R[Display Welcome Message]
    R --> S{Game Menu Choice}
    
    S -->|1| T[Go to Dungeon Selection]
    S -->|2| U[Show Inventory Menu]
    S -->|0| V[Save & Exit]
    
    U --> W[InventoryManager.ShowGearMenu]
    W --> X{Inventory Choice}
    X -->|1| Y[Equip Item]
    X -->|2| Z[Unequip Item]
    X -->|3| AA[Discard Item]
    X -->|4| BB[Manage Combo Actions]
    X -->|5| T
    X -->|6| CC[Return to Main Menu]
    X -->|0| V
    
    T --> DD[DungeonManagerWithRegistry.RegenerateDungeons]
    DD --> EE[Load Dungeons from JSON]
    EE --> FF[Filter by Player Level]
    FF --> GG[Create 3 Dungeons: Level-1, Level, Level+1]
    GG --> HH[ChooseDungeon - Player Selection]
    HH --> II[selectedDungeon.Generate]
    II --> JJ[DungeonRunner.RunDungeon]
    
    JJ --> KK[Enter Dungeon Message]
    KK --> LL[For Each Room in Dungeon]
    LL --> MM[ProcessRoom]
    MM --> NN[Display Room Description]
    NN --> OO[Clear Player Temp Effects]
    OO --> PP{Has Living Enemies?}
    
    PP -->|Yes| QQ[Get Next Living Enemy]
    PP -->|No| RR[Display Room Completion]
    QQ --> SS[ProcessEnemyEncounter]
    SS --> TT[Display Enemy Stats]
    TT --> UU[Clear Player Temp Effects]
    UU --> VV[Reset Divine Reroll Charges]
    VV --> WW[CombatManager.RunCombat]
    
    WW --> XX[Start Battle Narrative]
    XX --> YY[Initialize Combat Entities]
    YY --> ZZ[Reset Game Time]
    ZZ --> AAA[Reset Environment Action Count]
    AAA --> BBB{Combat Loop: Both Alive?}
    
    BBB -->|Yes| CCC[Get Next Entity to Act]
    BBB -->|No| DDD[End Battle Narrative]
    CCC --> EEE{Entity Type?}
    
    EEE -->|Player| FFF[ProcessPlayerTurn]
    EEE -->|Enemy| GGG[ProcessEnemyTurn]
    EEE -->|Environment| HHH[ProcessEnvironmentTurn]
    
    FFF --> III{Player Stunned?}
    III -->|Yes| JJJ[StunProcessor.ProcessStunnedEntity]
    III -->|No| KKK[ProcessPlayerAction]
    KKK --> LLL[ActionSelector.SelectAction]
    LLL --> MMM[ActionExecutor.ExecuteAction]
    MMM --> NNN[Display Combat Results]
    NNN --> OOO[Update Action Speed System]
    OOO --> PPP[Process Health Regeneration]
    PPP --> BBB
    
    GGG --> QQQ{Enemy Stunned?}
    QQQ -->|Yes| RRR[StunProcessor.ProcessStunnedEntity]
    QQQ -->|No| SSS[ActionExecutor.ExecuteAction]
    SSS --> TTT[Display Combat Results]
    TTT --> UUU[Update Action Speed System]
    UUU --> VVV[Process Damage Over Time Effects]
    VVV --> BBB
    
    HHH --> WWW{Should Environment Act?}
    WWW -->|Yes| XXX[Select Environmental Action]
    WWW -->|No| YYY[Advance Environment Turn]
    XXX --> ZZZ[Execute Area of Effect Action]
    ZZZ --> AAAA[Display Environmental Results]
    AAAA --> BBBB[Update Action Speed System]
    BBBB --> BBB
    YYY --> BBB
    
    DDD --> CCCC{Player Survived?}
    CCCC -->|No| DDDD[Handle Player Defeat]
    CCCC -->|Yes| EEEE[Handle Enemy Defeat]
    DDDD --> FFFF[Delete Save File]
    FFFF --> GGGG[Return False - Game Over]
    EEEE --> HHHH[Add XP Reward]
    HHHH --> II[Display Room Completion]
    II --> JJ[Reset Combo]
    JJ --> PP
    
    RR --> KKKK[DungeonManager.AwardLootAndXP]
    KKKK --> LLLL[RewardManager.AwardLootAndXP]
    LLLL --> MMMM[Generate Loot Based on Level]
    MMMM --> NNNN[Add Items to Inventory]
    NNNN --> OOOO[Add XP to Player]
    OOOO --> PPPP[Check for Level Up]
    PPPP --> QQQQ[Return to Game Menu]
    QQQQ --> S
    
    V --> RRRR[Save Character]
    RRRR --> SSSS[Return False - Exit Game]
    CC --> RRRR
    
    I --> TTTT[SettingsManager.ShowSettings]
    TTTT --> UUUU[Return to Main Menu]
    UUUU --> E
```

## Combat System Detail

```mermaid
flowchart TD
    A[CombatManager.RunCombat] --> B[Initialize Combat State]
    B --> C[Action Speed System Setup]
    C --> D[Combat Loop Start]
    
    D --> E{Both Entities Alive?}
    E -->|No| F[End Combat]
    E -->|Yes| G[Get Next Entity to Act]
    
    G --> H{Entity Ready?}
    H -->|No| I[Advance Time to Next Ready Entity]
    I --> G
    H -->|Yes| J{Entity Type?}
    
    J -->|Player| K[Player Turn Processing]
    J -->|Enemy| L[Enemy Turn Processing]
    J -->|Environment| M[Environment Turn Processing]
    
    K --> N{Player Stunned?}
    N -->|Yes| O[Process Stun]
    N -->|No| P[Action Selection & Execution]
    P --> Q[Update Action Speed]
    Q --> R[Process Health Regen]
    R --> D
    
    L --> S{Enemy Stunned?}
    S -->|Yes| T[Process Stun]
    S -->|No| U[AI Action Selection]
    U --> V[Action Execution]
    V --> W[Update Action Speed]
    W --> X[Process DoT Effects]
    X --> D
    
    M --> Y{Environment Should Act?}
    Y -->|Yes| Z[Environmental Action]
    Y -->|No| AA[Advance Environment Turn]
    Z --> BB[Area of Effect Execution]
    BB --> CC[Update Action Speed]
    CC --> D
    AA --> D
    
    F --> DD[Return Combat Result]
```

## Action System Detail

```mermaid
flowchart TD
    A[Action Selection] --> B{Entity Type?}
    B -->|Player| C[Player Action Selection]
    B -->|Enemy| D[Enemy AI Action Selection]
    B -->|Environment| E[Environment Action Selection]
    
    C --> F[Display Available Actions]
    F --> G[Player Input]
    G --> H[Validate Action Choice]
    H --> I[Get Selected Action]
    
    D --> J[AI Decision Logic]
    J --> K[Select Best Action]
    K --> L[Get Selected Action]
    
    E --> M[Check Environmental Conditions]
    M --> N[Select Environmental Action]
    N --> O[Get Selected Action]
    
    I --> P[Action Execution]
    L --> P
    O --> P
    
    P --> Q[Calculate Action Effects]
    Q --> R[Apply Damage/Effects]
    R --> S[Process Status Effects]
    S --> T[Update Action Speed System]
    T --> U[Display Results]
    U --> V[Return to Combat Loop]
```

## Character System Detail

```mermaid
flowchart TD
    A[Character Creation] --> B[CharacterBuilder.Build]
    B --> C[Set Base Stats]
    C --> D[Generate Random Name]
    D --> E[Initialize Equipment Slots]
    E --> F[Load Starting Gear]
    F --> G[Equip Starting Items]
    G --> H[Initialize Action Pool]
    H --> I[Set Default Combo]
    I --> J[Character Ready]
    
    J --> K[Character Operations]
    K --> L[Equipment Management]
    K --> M[Combat Operations]
    K --> N[Progression System]
    K --> O[Save/Load System]
    
    L --> P[EquipItem]
    L --> Q[UnequipItem]
    L --> R[Get Equipment Stats]
    
    M --> S[TakeDamage]
    M --> T[Heal]
    M --> U[Apply Effects]
    M --> V[Calculate Combat Stats]
    
    N --> W[AddXP]
    N --> X[LevelUp]
    N --> Y[Stat Increases]
    N --> Z[Health Restoration]
    
    O --> AA[SaveCharacter]
    O --> BB[LoadCharacter]
    O --> CC[DeleteSaveFile]
```

## Dungeon System Detail

```mermaid
flowchart TD
    A[Dungeon Generation] --> B[Load Dungeon Data from JSON]
    B --> C[Filter by Player Level]
    C --> D[Create 3 Dungeons: Level-1, Level, Level+1]
    D --> E[Player Selects Dungeon]
    E --> F[Generate Dungeon Rooms]
    
    F --> G[Room Generation Process]
    G --> H[Create Room Environment]
    H --> I[Generate Enemies for Room]
    I --> J[Set Environmental Effects]
    J --> K[Room Complete]
    
    K --> L[Dungeon Execution]
    L --> M[For Each Room in Dungeon]
    M --> N[Enter Room]
    N --> O[Display Room Description]
    O --> P[Process Room Encounters]
    
    P --> Q{Has Enemies?}
    Q -->|Yes| R[Combat Encounter]
    Q -->|No| S[Room Complete]
    R --> T[Combat Resolution]
    T --> U{Player Survived?}
    U -->|No| V[Game Over]
    U -->|Yes| W[Next Enemy or Room Complete]
    W --> Q
    
    S --> X[Room Completion Rewards]
    X --> Y{More Rooms?}
    Y -->|Yes| M
    Y -->|No| Z[Dungeon Complete]
    Z --> AA[Final Rewards]
    AA --> BB[Return to Game Menu]
```

## Inventory & Equipment System Detail

```mermaid
flowchart TD
    A[Inventory Management] --> B[Show Inventory Menu]
    B --> C{Player Choice}
    
    C -->|1| D[Equip Item]
    C -->|2| E[Unequip Item]
    C -->|3| F[Discard Item]
    C -->|4| G[Manage Combo Actions]
    C -->|5| H[Continue to Dungeon]
    C -->|6| I[Return to Main Menu]
    C -->|0| J[Exit Game]
    
    D --> K[Select Item from Inventory]
    K --> L[Get Item Slot Type]
    L --> M[Unequip Current Item]
    M --> N[Destroy Previous Item]
    N --> O[Equip New Item]
    O --> P[Remove from Inventory]
    P --> Q[Update Character Stats]
    Q --> B
    
    E --> R[Select Equipment Slot]
    R --> S[Unequip Item from Slot]
    S --> T[Add to Inventory]
    T --> U[Update Character Stats]
    U --> B
    
    F --> V[Select Item to Discard]
    V --> W[Remove from Inventory]
    W --> X[Item Destroyed]
    X --> B
    
    G --> Y[Combo Management]
    Y --> Z[View Current Combo]
    Z --> AA[Add Action to Combo]
    Z --> BB[Remove Action from Combo]
    Z --> CC[Reorder Combo Actions]
    Z --> DD[Reset Combo]
    AA --> Y
    BB --> Y
    CC --> Y
    DD --> Y
    Y --> B
```

## Data Flow Architecture

```mermaid
flowchart LR
    A[JSON Data Files] --> B[Data Loaders]
    B --> C[Data Classes]
    C --> D[Managers]
    D --> E[Game Logic]
    E --> F[UI Display]
    
    A1[Actions.json] --> B1[ActionLoader]
    A2[Enemies.json] --> B2[EnemyLoader]
    A3[Weapons.json] --> B3[ItemGenerator]
    A4[Armor.json] --> B4[ItemGenerator]
    A5[Dungeons.json] --> B5[DungeonManager]
    A6[Rooms.json] --> B6[RoomLoader]
    A7[TuningConfig.json] --> B7[GameConfiguration]
    
    B1 --> C1[ActionData]
    B2 --> C2[EnemyData]
    B3 --> C3[WeaponItem]
    B4 --> C4[ArmorItem]
    B5 --> C5[DungeonData]
    B6 --> C6[RoomData]
    B7 --> C7[ConfigData]
    
    C1 --> D1[ActionFactory]
    C2 --> D2[EnemyFactory]
    C3 --> D3[InventoryManager]
    C4 --> D3
    C5 --> D4[DungeonManager]
    C6 --> D5[RoomGenerator]
    C7 --> D6[GameConfiguration]
    
    D1 --> E1[CombatManager]
    D2 --> E1
    D3 --> E2[Character]
    D4 --> E3[DungeonRunner]
    D5 --> E3
    D6 --> E4[All Systems]
    
    E1 --> F1[Combat Display]
    E2 --> F2[Character Display]
    E3 --> F3[Dungeon Display]
    E4 --> F4[UI Manager]
```

## Key Design Patterns Used

```mermaid
flowchart TD
    A[Design Patterns] --> B[Manager Pattern]
    A --> C[Factory Pattern]
    A --> D[Strategy Pattern]
    A --> E[Registry Pattern]
    A --> F[Facade Pattern]
    A --> G[Builder Pattern]
    A --> H[Observer Pattern]
    A --> I[Composition Pattern]
    
    B --> B1[CombatManager]
    B --> B2[DungeonManager]
    B --> B3[InventoryManager]
    B --> B4[CharacterHealthManager]
    
    C --> C1[EnemyFactory]
    C --> C2[ActionFactory]
    C --> C3[ItemGenerator]
    
    D --> D1[ActionSelector]
    D --> D2[ActionExecutor]
    D --> D3[EffectHandlerRegistry]
    
    E --> E1[EffectHandlerRegistry]
    E --> E2[EnvironmentalEffectRegistry]
    
    F --> F1[CharacterFacade]
    F --> F2[GameDisplayManager]
    
    G --> G1[CharacterBuilder]
    G --> G2[EnemyBuilder]
    
    H --> H1[BattleHealthTracker]
    H --> H2[CombatResults]
    
    I --> I1[Character with Managers]
    I --> I2[CombatManager with Specialized Handlers]
```

This comprehensive flowchart shows the complete game logic flow from application startup through all major systems including combat, character management, dungeon exploration, inventory management, and data flow architecture. Each major system is broken down into its component processes, showing how the modular architecture enables clean separation of concerns while maintaining interconnected functionality.
