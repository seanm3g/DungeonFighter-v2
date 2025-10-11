# Simple Mermaid Flowchart Example

Here's a simple example to test Mermaid rendering:

## Basic Game Flow

```mermaid
flowchart TD
    A[Start Game] --> B{New or Load?}
    B -->|New| C[Create Character]
    B -->|Load| D[Load Character]
    C --> E[Choose Weapon]
    D --> F[Enter Dungeon]
    E --> F
    F --> G[Fight Enemies]
    G --> H{Win?}
    H -->|Yes| I[Get Loot]
    H -->|No| J[Game Over]
    I --> K{Continue?}
    K -->|Yes| F
    K -->|No| L[Save & Exit]
    J --> M[Delete Save]
    L --> N[End]
    M --> N
```

## Combat System

```mermaid
flowchart LR
    A[Combat Start] --> B[Player Turn]
    B --> C[Enemy Turn]
    C --> D{Both Alive?}
    D -->|Yes| B
    D -->|No| E[Combat End]
```

## Character Stats

```mermaid
graph TD
    A[Character] --> B[Stats]
    A --> C[Equipment]
    A --> D[Actions]
    
    B --> E[Strength]
    B --> F[Agility]
    B --> G[Technique]
    B --> H[Intelligence]
    
    C --> I[Weapon]
    C --> J[Armor]
    
    D --> K[Basic Attack]
    D --> L[Special Actions]
```

## How to Use This:

1. **Copy the code** between the ```mermaid and ``` markers
2. **Go to [mermaid.live](https://mermaid.live/)**
3. **Paste the code** in the editor
4. **See the diagram** render automatically
5. **Download** as PNG, SVG, or PDF

## Tips:

- Use **TD** for Top-Down flowcharts
- Use **LR** for Left-Right flowcharts
- Use **{text}** for decision points
- Use **[text]** for process boxes
- Use **-->** for arrows
- Use **|label|** for arrow labels
