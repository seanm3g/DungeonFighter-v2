# Keyword Color Patterns - Quick Start Guide

## Overview

The Keyword Color System automatically applies beautiful color patterns to specific words in your game text. This guide covers the newly added keyword groups for actions, characters, enemies, and environments.

## New Keyword Groups Added

### 1. **Actions** (Electric Pattern)
Combat actions and abilities are colored with an electric blue/cyan pattern.

**Keywords Include:**
- Basic attacks: `jab`, `taunt`, `flurry`, `cleave`
- Special moves: `shield bash`, `precision strike`, `momentum bash`
- Advanced techniques: `backstab`, `sneak attack`, `brutal strike`
- Power moves: `blood frenzy`, `berzerk`, `swing for the fences`
- And many more combat actions...

**Example:**
```csharp
string message = "You use flurry to strike the enemy!";
string colored = KeywordColorSystem.Colorize(message);
// Output: "You use {{electric|flurry}} to strike the enemy!"
```

### 2. **Character** (Golden Pattern)
Player references are highlighted with a golden color.

**Keywords Include:**
- `you`, `your`, `yourself`
- `hero`, `champion`, `adventurer`

**Example:**
```csharp
string message = "You are the champion of this realm!";
string colored = KeywordColorSystem.Colorize(message);
// Output: "{{golden|You}} are the {{golden|champion}} of this realm!"
```

### 3. **Environment** (Natural Pattern)
Room types and locations use a natural green/brown pattern.

**Keywords Include:**
- Rooms: `entrance`, `chamber`, `room`, `hall`, `cavern`
- Passages: `tunnel`, `passage`, `corridor`
- Special areas: `dungeon`, `crypt`, `tomb`, `vault`, `sanctuary`
- Natural features: `grove`, `clearing`, `lair`, `den`, `nest`
- Water features: `pool`, `lake`
- Structures: `cave`, `cathedral`, `temple`, `altar`, `shrine`
- Utility rooms: `library`, `armory`, `treasury`, `prison`, `barracks`

**Example:**
```csharp
string message = "You enter the dungeon chamber. The crypt is dark.";
string colored = KeywordColorSystem.Colorize(message);
// Output: "You enter the {{natural|dungeon}} {{natural|chamber}}. The {{natural|crypt}} is dark."
```

### 4. **Theme** (Crystalline Pattern)
Environmental themes use a magical crystalline color pattern.

**Keywords Include:**
- Natural: `forest`, `nature`, `swamp`
- Elemental: `lava`, `volcanic`, `frozen`, `ice`, `glacial`, `storm`
- Mystical: `shadow`, `void`, `crystal`, `astral`, `cosmic`
- Industrial: `underground`, `steampunk`, `mechanical`

**Example:**
```csharp
string message = "The frozen forest is covered in crystal and shadow.";
string colored = KeywordColorSystem.Colorize(message);
// Output: "The {{crystalline|frozen}} {{crystalline|forest}} is covered in {{crystalline|crystal}} and {{crystalline|shadow}}."
```

### 5. **Expanded Enemy Keywords** (Red Pattern)
Many more enemy types have been added to the existing enemy group.

**New Enemies Include:**
- Animals: `wolf`, `bear`, `yeti`
- Fantasy: `treant`, `elemental`, `golem`, `salamander`
- Undead: `lich`, `ghoul`, `vampire`, `werewolf`
- Guardians: `warden`, `guardian`, `sentinel`
- Constructs: `soldier`, `sprite`, `wyrm`
- Legendary: `minotaur`, `hydra`, `chimera`
- Others: `kobold`, `priest`, `beast`

**Example:**
```csharp
string message = "The lich commands the golem and wyrm to attack!";
string colored = KeywordColorSystem.Colorize(message);
// Output: "The {{red|lich}} commands the {{red|golem}} and {{red|wyrm}} to attack!"
```

## Quick Usage

### Automatic Coloring (All Keywords)
```csharp
string text = "You use cleave in the dungeon chamber against the goblin!";
string colored = KeywordColorSystem.Colorize(text);
UIManager.WriteLine(colored);
```

### Selective Group Coloring
```csharp
// Only color actions and enemies
string text = "You use jab to hit the orc!";
string colored = KeywordColorSystem.ColorizeWithGroups(text, "action", "enemy");
UIManager.WriteLine(colored);
```

### Add Custom Keywords to Existing Groups
```csharp
// Add new action names
KeywordColorSystem.AddKeywordsToGroup("action", "whirlwind", "thunderstrike", "meteor strike");

// Add new enemy types
KeywordColorSystem.AddKeywordsToGroup("enemy", "kraken", "phoenix", "leviathan");

// Add new room types
KeywordColorSystem.AddKeywordsToGroup("environment", "observatory", "workshop", "garden");
```

## Complete Example: Combat Scenario

```csharp
// A full combat message using multiple keyword groups
string combat = @"
You enter the frozen chamber of the dungeon.
A lich and two golems block your path!

Round 1:
You use precision strike against the lich for critical damage!
Your champion strikes with flurry, hitting the golem!

Round 2:
The hero executes a brutal strike. The lich is defeated!
You use cleave to damage both golems!

Victory! Your adventurer gains experience!
";

string colored = KeywordColorSystem.Colorize(combat);
UIManager.WriteLine(colored);
```

This will automatically color:
- **Character keywords** (`you`, `your`, `hero`, `champion`, `adventurer`) in **golden**
- **Environment keywords** (`frozen`, `chamber`, `dungeon`) in **natural** and **crystalline**
- **Enemy keywords** (`lich`, `golem`, `golems`) in **red**
- **Action keywords** (`precision strike`, `flurry`, `brutal strike`, `cleave`) in **electric**
- **Other keywords** (`critical`, `damage`, `experience`) in their respective patterns

## Integration with Existing Systems

The keyword system works seamlessly with:
- Manual color codes (`&R`, `&G`, `&B`, etc.)
- Color templates (`{{fiery|text}}`, `{{icy|text}}`, etc.)
- Event significance (Critical, Important, Normal, etc.)
- Dungeon depth progression

## Performance Notes

- Keyword coloring is fast (<0.1ms per message)
- Keywords are matched as whole words only
- Case-insensitive by default
- Already colored text is not re-colored
- Use `KeywordColorSystem.ColorizeWithGroups()` for better performance when you only need specific groups

## Available Color Patterns

| Pattern | Visual Effect | Used By |
|---------|--------------|---------|
| `electric` | Electric blue/cyan discharge | Actions |
| `golden` | Golden gleam | Character, Gold |
| `natural` | Natural green/brown | Environment, Nature |
| `crystalline` | Magical prism effect | Theme, Crystal |
| `red` / `damage` | Red (solid or fire) | Enemies, Damage |
| `fiery` | Fire effect | Critical, Fire |
| `icy` | Ice effect | Ice, Frozen |
| `holy` | Divine light | Holy, Buffs |
| `shadow` | Dark shadows | Shadow, Darkness |
| `arcane` | Magical energy | Magic, Spells |
| `bleeding` | Blood effect | Blood, Bleeding |
| `poisoned` | Poison green | Poison |
| `stunned` | Stun flash | Stun |

## Testing Your Keywords

Run the demonstration to see all keywords in action:

```csharp
// Run all keyword demos
KeywordColorExamples.RunAllDemos();

// Or run specific demos
KeywordColorExamples.DemoCombatActionsAndCharacter();
KeywordColorExamples.DemoRoomsAndThemes();
KeywordColorExamples.DemoExpandedEnemies();
```

## Summary

The keyword color system now provides automatic coloring for:
1. ✅ **30+ combat actions** with electric pattern
2. ✅ **6 character/player references** with golden pattern
3. ✅ **25+ room/environment types** with natural pattern
4. ✅ **15+ environment themes** with crystalline pattern
5. ✅ **20+ additional enemy types** with red pattern

Just call `KeywordColorSystem.Colorize(text)` and watch your game text come alive with color!

---

*For complete documentation, see [COLOR_SYSTEM.md](COLOR_SYSTEM.md)*

