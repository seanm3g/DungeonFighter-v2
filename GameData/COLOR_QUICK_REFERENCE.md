# Color System Quick Reference

Quick lookup guide for all available color templates and keyword groups in the DungeonFighter-v2 color system.

## Color Templates

All templates defined in `ColorTemplates.json`. Use in markup: `{{template|text}}`

### Elements
| Template | Colors | Effect |
|----------|--------|--------|
| `fiery` | R,O,W,Y,W,O,R | 🔥 Fire/flame effect |
| `icy` | C,B,Y,C,b,C,Y | ❄️ Ice/frost effect |
| `toxic` | g,G,Y,G,g | ☠️ Poison/venom effect |
| `electric` | C,Y,W,Y,C | ⚡ Lightning/shock effect |

### Magic & Divine
| Template | Colors | Effect |
|----------|--------|--------|
| `arcane` | m,M,C,M,m | 🔮 Magical energy |
| `holy` | W,Y,W,Y,W | ✨ Divine light |
| `demonic` | r,R,K,r,R | 😈 Hellfire/dark |
| `ethereal` | C,M,Y,M,C | 👻 Otherworldly glow |
| `corrupted` | m,K,r,K,m | 💀 Dark corruption |

### Nature & Elements
| Template | Colors | Effect |
|----------|--------|--------|
| `natural` | g,G,w,G,g | 🌿 Nature/earth |
| `crystalline` | m,M,B,Y,B,M,m | 💎 Crystal prism |
| `shadow` | K,k,y,k,K | 🌑 Dark shadows |
| `golden` | W,O,W,O,W | 👑 Golden gleam |

### Combat
| Template | Colors | Effect |
|----------|--------|--------|
| `critical` | R,O,Y,O,R | 💥 Critical hit |
| `damage` | R | ❤️ Damage (solid red) |
| `heal` | G | 💚 Healing (solid green) |
| `mana` | B | 💙 Mana/magic (solid blue) |
| `bloodied` | r,R,r,K | 🩸 Blood-soaked |
| `bleeding` | r,R,r,r | 🔴 Bleed effect |

### Status Effects
| Template | Colors | Effect |
|----------|--------|--------|
| `poisoned` | g,G,g,k | ☣️ Poison status |
| `stunned` | W,Y,W,y | ⭐ Stun status |
| `burning` | R,O,R,r | 🔥 Burn status |
| `frozen` | C,B,Y,C | 🧊 Freeze status |

### Rarities
| Template | Colors | Rarity Level |
|----------|--------|--------------|
| `common` | y | ⚪ Common (grey) |
| `uncommon` | G | 🟢 Uncommon (green) |
| `rare` | B | 🔵 Rare (blue) |
| `epic` | M | 🟣 Epic (purple) |
| `legendary` | O | 🟠 Legendary (orange) |

### Basic Solid Colors
| Template | Color | Use |
|----------|-------|-----|
| `red` | R | Simple red |
| `green` | G | Simple green |
| `blue` | B | Simple blue |
| `yellow` | W | Simple yellow/gold |
| `white` | Y | White/bright |
| `cyan` | C | Cyan/aqua |
| `magenta` | M | Magenta/purple |
| `grey` | y | Grey/neutral |
| `dark red` | r | Dark crimson |
| `dark green` | g | Dark green |
| `dark blue` | b | Dark blue |
| `dark orange` | o | Dark orange |
| `orange` | O | Bright orange |
| `brown` | w | Brown/tan |

### Special
| Template | Colors | Effect |
|----------|--------|--------|
| `rainbow` | R,O,W,G,C,B,M | 🌈 Rainbow (alternating) |
| `amorous` | r,R,M,m | 💖 Pink/romantic |
| `bee` | K,W,W,K | 🐝 Yellow/black stripes |
| `forest` | g,G,w,G | 🌲 Forest green/brown |
| `miss` | K | ⚫ Miss/avoid (dark) |

## Keyword Groups

All groups defined in `KeywordColorGroups.json`. Keywords automatically colored when detected in text.

### Combat Keywords

**damage** → `damage` (red)
- damage, hit, strike, attack, slash, pierce, crush, wound, injure, harm, hurt

**critical** → `critical` (fiery red/orange/yellow)
- critical, crit, devastating, crushing blow, massive hit, overwhelming

**heal** → `heal` (green)
- heal, restore, regenerate, recover, mend, revive, health, hp

**action** → `electric` (cyan/yellow)
- jab, taunt, flurry, cleave, shield bash, precision strike, momentum bash, lucky strike, overkill, dance, opening volley, focus, sharp edge, blood frenzy, berzerk, swing for the fences, true strike, last grasp, second wind, quick reflexes, first blood, brutal strike, war cry, web strike, poison bite, devastating blow, power attack, rending slash, execute, backstab, sneak attack

**stun** → `stunned` (white/yellow)
- stun, stunned, daze, dazed, paralyzed, immobilize, disable

**blood** → `bleeding` (red)
- blood, bleed, bleeding, hemorrhage, bloodied, gore, crimson

**death** → `bloodied` (dark red)
- death, die, dies, died, killed, slain, defeated, destroyed, perish

### Element Keywords

**fire** → `fiery` (red/orange/yellow)
- fire, flame, burn, burning, blaze, inferno, ignite, scorch, ember, pyro

**ice** → `icy` (cyan/blue/white)
- ice, frost, frozen, freeze, chill, cold, glacial, arctic, cryo

**lightning** → `electric` (cyan/yellow)
- lightning, thunder, electric, shock, zap, bolt, spark, voltage

**poison** → `poisoned` (green)
- poison, venom, toxic, poisoned, toxin, venomous, contaminate

**shadow** → `shadow` (dark/grey)
- shadow, darkness, dark, void, abyss, umbral, night, blackness

**holy** → `holy` (white/gold)
- holy, divine, sacred, blessed, light, radiant, celestial, sanctified

### Status Keywords

**buff** → `holy` (white/gold)
- strengthen, fortify, enhance, boost, empower, blessed, buff, augment

**debuff** → `corrupted` (purple/dark)
- weaken, curse, hex, poison, disease, corrupt, debuff, enfeeble

### Item Rarity Keywords

**common** → `common` (grey)
- common

**uncommon** → `uncommon` (green)
- uncommon

**rare** → `rare` (blue)
- rare

**epic** → `epic` (purple)
- epic

**legendary** → `legendary` (orange)
- legendary, mythic, artifact

### World Keywords

**enemy** → `red`
- goblin, orc, skeleton, zombie, dragon, demon, wraith, spider, bat, slime, cultist, bandit, boss, monster, creature, beast, wolf, bear, treant, elemental, golem, salamander, lich, warden, guardian, kobold, soldier, sprite, wyrm, sentinel, priest, yeti, ghoul, vampire, werewolf, minotaur, hydra, chimera

**class** → `cyan`
- warrior, mage, rogue, wizard, barbarian, paladin, ranger, knight, sorcerer

**character** → `golden` (gold)
- you, your, yourself, hero, champion, adventurer

**environment** → `natural` (green/brown)
- entrance, chamber, room, hall, cavern, tunnel, passage, dungeon, crypt, tomb, vault, sanctuary, grove, clearing, lair, den, nest, pool, lake, cave, cathedral, temple, altar, shrine, library, armory, treasury, prison, barracks

**theme** → `crystalline` (purple/blue/white)
- forest, lava, frozen, ice, shadow, crystal, astral, underground, swamp, storm, volcanic, glacial, void, steampunk, mechanical, nature, cosmic

### Resource Keywords

**magic** → `arcane` (purple/cyan)
- magic, spell, arcane, mystical, enchanted, magical, sorcery, wizardry

**nature** → `natural` (green/brown)
- nature, natural, earth, growth, plant, forest, grove, wilderness

**gold** → `golden` (gold)
- gold, coins, currency, treasure, wealth, riches, money

**experience** → `white`
- experience, xp, level, level up, leveled, gained level

## Color Codes

Single-letter codes for inline coloring. Use: `&Xtext` (foreground) or `^Xtext` (background)

### Reds
- `r` = dark red/crimson (#a64a2e)
- `R` = red/scarlet (#d74200)

### Oranges
- `o` = dark orange (#f15f22)
- `O` = orange (#e99f10)

### Yellows/Browns
- `w` = brown (#98875f)
- `W` = gold/yellow (#cfc041)

### Greens
- `g` = dark green (#009403)
- `G` = green (#00c420)

### Blues
- `b` = dark blue (#0048bd)
- `B` = blue/azure (#0096ff)

### Cyans
- `c` = dark cyan/teal (#40a4b9)
- `C` = cyan (#77bfcf)

### Magentas
- `m` = dark magenta/purple (#b154cf)
- `M` = magenta (#da5bd6)

### Greys/Blacks
- `k` = very dark (#0f3b3a)
- `K` = dark grey/black (#155352)
- `y` = grey (#b1c9c3)
- `Y` = white (#ffffff)

## Usage Examples

### Using Templates
```
You found a {{legendary|Sword of Fire}}!
The {{fiery|flames}} consume your enemy.
Cast {{arcane|Fireball}} for {{damage|50}} damage.
```

### Using Color Codes
```
&RRed text&y back to grey
&GGreen&y and &Bblue&y
&W^KGold text on black background&y
```

### Automatic Keyword Coloring
```
Input:  "You hit the goblin for 25 damage!"
Output: "You hit the goblin for 25 damage!" (colored automatically)
         ↑   ↑   ↑         ↑   ↑
      golden  red      red    red
```

## Quick Tips

- Keywords are matched as **whole words only**
- Most keyword matching is **case-insensitive**
- **First match wins** if keywords overlap
- Color codes **cycle through characters** for sequence templates
- Use `&y` to **reset to default** grey color
- Templates can be **nested**: `{{fiery|Fire {{critical|CRIT}}!}}`

## Configuration Files

- **Templates**: `GameData/ColorTemplates.json`
- **Keywords**: `GameData/KeywordColorGroups.json`
- **Documentation**: `GameData/README_COLOR_CONFIG.md`

## Reloading at Runtime

```csharp
// After modifying config files
ColorTemplateLibrary.ReloadFromConfig();
KeywordColorSystem.ReloadFromConfig();
```

---

**Last Updated**: 2025-10-11  
**Total Templates**: 37  
**Total Keyword Groups**: 27  
**Total Keywords**: 200+

