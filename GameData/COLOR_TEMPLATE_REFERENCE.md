# Color Template Reference

## Overview
This document provides a comprehensive reference for all color templates added to `ColorTemplates.json` for item modifiers, stat bonuses, environments, and status effects.

## Design Guidelines

### Color Scheme Philosophy
1. **Basic Items**: Neutral tones (grey/white spectrum) - `y`, `Y`
2. **Negative Modifiers**: Dark/grey colors - `k`, `K`
3. **Good Items**: Warmer whites with subtle color hints - `Y`, `W`, `C`
4. **Exceptional Items**: Saturated, bright, **mixed color names** - `R`, `O`, `Y`, `M`, `C`, `B`
5. **Environments**: **ALWAYS mixed color names** with thematic combinations
6. **Status Effects**: **ALWAYS mixed color names** with effect-appropriate colors

## Item Modifiers

### Negative Modifiers (Dark/Grey)
- `worn` - Very dark grey (`k`) - Solid
- `dull` - Dark grey (`K`) - Solid

### Basic Positive Modifiers (Neutral)
- `sturdy` - Grey (`y`) - Solid
- `balanced` - Grey (`y`) - Solid

### Uncommon Modifiers (Subtle shimmer)
- `sharp` - Grey to white (`y`, `Y`) - Sequence
- `swift` - Cyan to grey (`C`, `y`) - Sequence  
- `precise` - White to grey (`Y`, `y`) - Sequence
- `reinforced` - Brown to grey (`w`, `y`) - Sequence

### Rare Modifiers (Brighter mixed colors)
- `keen` - White/cyan/white (`Y`, `C`, `Y`) - Sequence
- `agile` - Cyan/white/cyan (`C`, `Y`, `C`) - Sequence
- `lucky` - Gold/white/gold (`W`, `Y`, `W`) - Sequence
- `vampiric` - Dark red/magenta/dark red (`r`, `M`, `r`) - Sequence

### Epic Modifiers (Saturated colors)
- `brutal` - Red/white/red (`R`, `Y`, `R`) - Sequence
- `lightning` - Cyan/white/cyan/white (`C`, `Y`, `C`, `Y`) - Sequence
- `blessed` - Gold/white/magenta/white (`W`, `Y`, `M`, `Y`) - Sequence
- `venomous` - Dark green/green/white (`g`, `G`, `Y`) - Sequence

### Legendary Modifiers (Complex shimmer)
- `masterwork` - Orange/gold/white/gold (`O`, `W`, `Y`, `W`) - Sequence
- `godlike` - Magenta/gold/white/gold/magenta (`M`, `W`, `Y`, `W`, `M`) - Sequence
- `enchanted` - Dark magenta/magenta/cyan/white (`m`, `M`, `C`, `Y`) - Sequence

### Mythic Modifiers (Multi-color radiance)
- `annihilation` - Red/orange/white/magenta/red (`R`, `O`, `Y`, `M`, `R`) - Sequence
- `timewarp` - Cyan/magenta/white/blue/cyan (`C`, `M`, `Y`, `B`, `C`) - Sequence
- `perfect` - White/cyan/magenta/white (`Y`, `C`, `M`, `Y`) - Sequence
- `divine` - Gold/white/magenta/cyan/white (`W`, `Y`, `M`, `C`, `Y`) - Sequence

### Transcendent Modifiers (Prismatic effects)
- `realitybreaker` - Magenta/red/white/cyan/blue/magenta (`M`, `R`, `Y`, `C`, `B`, `M`) - Sequence
- `omnipotent` - White/magenta/cyan/white/gold/white (`Y`, `M`, `C`, `Y`, `W`, `Y`) - Sequence
- `infinite` - Cyan/white/magenta/white/cyan/blue (`C`, `Y`, `M`, `Y`, `C`, `B`) - Sequence
- `cosmic` - Magenta/blue/white/cyan/magenta/white (`M`, `B`, `Y`, `C`, `M`, `Y`) - Sequence

### Magic Find Modifiers
- `magicfingers` - Grey (`y`) - Solid
- `magicallamp` - Gold/orange/white (`W`, `O`, `Y`) - Sequence

## Stat Bonuses (Suffixes)

### Basic Bonuses (Neutral)
- `protection` - Grey (`y`) - Solid
- `vitality` - Grey (`y`) - Solid
- `swiftness` - Grey (`y`) - Solid
- `power` - Grey (`y`) - Solid
- `recovery` - Grey (`y`) - Solid

### Stat Bonuses (White)
- `intelligence` - White (`Y`) - Solid
- `strength` - White (`Y`) - Solid
- `agility` - White (`Y`) - Solid
- `technique` - White (`Y`) - Solid
- `accuracy` - White (`Y`) - Solid

### Improved Bonuses (Subtle shimmer)
- `fortification` - Grey to white (`y`, `Y`) - Sequence
- `vigor` - White to grey (`Y`, `y`) - Sequence
- `alacrity` - Cyan to white (`C`, `Y`) - Sequence
- `might` - White to gold (`Y`, `W`) - Sequence
- `resilience` - Grey/white/grey (`y`, `Y`, `y`) - Sequence

### Animal-Themed Bonuses (Thematic colors)
- `bear` - Brown/white/brown (`w`, `Y`, `w`) - Sequence
- `cat` - Grey/cyan/white (`y`, `C`, `Y`) - Sequence
- `owl` - Blue/white/cyan (`B`, `Y`, `C`) - Sequence
- `hawk` - White/cyan/grey (`Y`, `C`, `y`) - Sequence

### High-Tier Bonuses (Mixed colors)
- `precision` - White/cyan/white (`Y`, `C`, `Y`) - Sequence
- `warding` - Cyan/white/cyan (`C`, `Y`, `C`) - Sequence
- `endurance` - White/gold/white (`Y`, `W`, `Y`) - Sequence
- `haste` - Cyan/white/cyan/white (`C`, `Y`, `C`, `Y`) - Sequence
- `destruction` - Red/white/red (`R`, `Y`, `R`) - Sequence
- `regeneration` - Green/white/green (`G`, `Y`, `G`) - Sequence

### Epic Bonuses (Complex shimmer)
- `titan` - White/gold/orange/white (`Y`, `W`, `O`, `Y`) - Sequence
- `wind` - Cyan/white/blue/cyan (`C`, `Y`, `B`, `C`) - Sequence
- `sage` - Blue/magenta/white/blue (`B`, `M`, `Y`, `B`) - Sequence
- `master` - White/cyan/white/gold (`Y`, `C`, `Y`, `W`) - Sequence
- `marksman` - White/gold/cyan/white (`Y`, `W`, `C`, `Y`) - Sequence

### Legendary Bonuses (Multi-color radiance)
- `invulnerability` - Cyan/white/blue/white/cyan (`C`, `Y`, `B`, `Y`, `C`) - Sequence
- `immortality` - Gold/white/magenta/white/gold (`W`, `Y`, `M`, `Y`, `W`) - Sequence
- `phoenix` - Red/orange/white/orange/red (`R`, `O`, `Y`, `O`, `R`) - Sequence
- `colossus` - White/gold/orange/white/gold (`Y`, `W`, `O`, `Y`, `W`) - Sequence
- `storm` - Cyan/white/blue/white/cyan (`C`, `Y`, `B`, `Y`, `C`) - Sequence
- `archmage` - Magenta/cyan/white/blue/magenta (`M`, `C`, `Y`, `B`, `M`) - Sequence
- `grandmaster` - White/magenta/cyan/white/gold (`Y`, `M`, `C`, `Y`, `W`) - Sequence
- `gods` - White/magenta/cyan/blue/white/gold (`Y`, `M`, `C`, `B`, `Y`, `W`) - Sequence

### Magic Find Bonuses (Progressive)
- `discovery` - Grey to gold (`y`, `W`) - Sequence
- `fortune` - Gold/white/gold (`W`, `Y`, `W`) - Sequence
- `treasurehunter` - Gold/orange/white (`W`, `O`, `Y`) - Sequence
- `collector` - Orange/gold/white/gold (`O`, `W`, `Y`, `W`) - Sequence
- `legendaryfortune` - Orange/gold/white/magenta/gold (`O`, `W`, `Y`, `M`, `W`) - Sequence
- `lootmaster` - Gold/orange/magenta/white (`W`, `O`, `M`, `Y`) - Sequence

## Environments (All with mixed colors)

### Natural Environments
- `forest` - Dark green/green/brown (`g`, `G`, `w`) - Forest greens and browns
- `swamp` - Dark green/brown/dark/dark green (`g`, `w`, `k`, `g`) - Murky swamp tones
- `nature` - Dark green/green/white/green (`g`, `G`, `Y`, `G`) - Vibrant natural life
- `ocean` - Dark blue/blue/cyan/blue (`b`, `B`, `C`, `B`) - Deep ocean blues
- `mountain` - Dark/grey/white/grey (`K`, `y`, `Y`, `y`) - Stone and snow
- `desert` - Gold/orange/white/brown (`W`, `O`, `Y`, `w`) - Sand and sun

### Elemental Environments
- `lava` - Dark red/red/orange/red (`r`, `R`, `O`, `R`) - Molten rock
- `volcano` - Red/orange/dark/red (`R`, `O`, `K`, `R`) - Volcanic fury
- `ice` - Cyan/blue/white/cyan (`C`, `B`, `Y`, `C`) - Frozen tundra
- `storm` - Cyan/white/blue/white (`C`, `Y`, `B`, `Y`) - Lightning and thunder

### Mystical Environments
- `crystal` - Dark magenta/magenta/cyan/white (`m`, `M`, `C`, `Y`) - Crystalline shimmer
- `temple` - Gold/white/brown/white (`W`, `Y`, `w`, `Y`) - Sacred architecture
- `shadow` - Dark/very dark/dark magenta/very dark (`K`, `k`, `m`, `k`) - Deep shadows
- `arcane` - Dark magenta/magenta/cyan/blue (`m`, `M`, `C`, `B`) - Magical energy
- `astral` - Magenta/blue/white/cyan (`M`, `B`, `Y`, `C`) - Cosmic wonder
- `temporal` - Cyan/magenta/white/blue (`C`, `M`, `Y`, `B`) - Time distortion
- `dream` - Magenta/cyan/white/dark magenta (`M`, `C`, `Y`, `m`) - Dreamlike state
- `void` - Very dark/dark/dark magenta/dark (`k`, `K`, `m`, `K`) - Empty void
- `dimensional` - Magenta/cyan/blue/white (`M`, `C`, `B`, `Y`) - Reality rifts
- `divine` - Gold/white/magenta/white (`W`, `Y`, `M`, `Y`) - Holy radiance

### Constructed Environments
- `crypt` - Dark/very dark/grey/dark (`K`, `k`, `y`, `K`) - Tomb decay
- `steampunk` - Brown/dark orange/dark/grey (`w`, `o`, `K`, `y`) - Industrial brass
- `underground` - Dark/brown/grey/very dark (`K`, `w`, `y`, `k`) - Cave depths
- `ruins` - Dark/brown/grey/very dark (`K`, `w`, `y`, `k`) - Ancient decay

## Status Effects (All with mixed colors)

### Damage-Over-Time Effects
- `poisoned` - Dark green/green/dark green/very dark (`g`, `G`, `g`, `k`) - Toxic green
- `burning` - Red/orange/red/dark red (`R`, `O`, `R`, `r`) - Fire damage
- `bleeding` - Dark red/red/dark red/dark red (`r`, `R`, `r`, `r`) - Blood loss

### Debuff Effects
- `weakened` - Dark/grey/very dark/grey (`K`, `y`, `k`, `y`) - Loss of strength
- `slowed` - Dark blue/cyan/grey/dark blue (`b`, `C`, `y`, `b`) - Movement reduction
- `stunned` - Gold/white/gold/grey (`W`, `Y`, `W`, `y`) - Dazed and confused
- `frozen` - Cyan/blue/white/cyan (`C`, `B`, `Y`, `C`) - Frozen solid

## Usage Examples

### Item with Modifier
```
{{legendary|Legendary}} {{masterwork|Masterwork}} Sword {{precision|of Precision}}
```
Result: Orange shimmering "Legendary", orange/gold/white "Masterwork", white/cyan "of Precision"

### Environment Description
```
You enter the {{lava|Lava Caves}}. The {{burning|burning}} heat is oppressive.
```
Result: Red/orange flickering "Lava Caves", red/orange "burning"

### Combat Message with Status
```
Enemy is {{poisoned|POISONED}} and {{weakened|WEAKENED}}!
```
Result: Green flickering "POISONED", dark grey pulsing "WEAKENED"

### Transcendent Item
```
{{transcendent|TRANSCENDENT}} {{cosmic|Cosmic}} Blade {{gods|of the Gods}}
```
Result: Prismatic "TRANSCENDENT", multi-color shimmer throughout

## Color Code Quick Reference

### Brightness Tiers
- **Very Dark**: `k` (for worst things)
- **Dark**: `K` (for negative effects)
- **Neutral**: `y` (for basic/common)
- **White**: `Y` (for good/bright)
- **Gold**: `W` (for valuable/excellent)

### Saturated Colors (for exceptional items)
- **Red**: `r` (dark), `R` (bright)
- **Orange**: `o` (dark), `O` (bright)
- **Green**: `g` (dark), `G` (bright)
- **Blue**: `b` (dark), `B` (bright)
- **Cyan**: `c` (dark), `C` (bright)
- **Magenta**: `m` (dark), `M` (bright)

## Implementation Notes

1. **Template Names**: Use lowercase, no spaces (e.g., `realitybreaker`, `treasurehunter`)
2. **Shader Types**:
   - `solid` - Single color, no animation
   - `sequence` - Colors cycle through in order
   - `alternation` - Colors alternate (used for rainbow)
3. **Color Selection**: Follow the quality progression:
   - Worse → Dark (`k`, `K`)
   - Common → Neutral (`y`, `Y`)
   - Good → Warm/Cool whites (`W`, `Y`, `C`)
   - Exceptional → Mixed saturated colors (`R`, `O`, `M`, `B`, `C`)

## Future Additions

Consider adding templates for:
- Individual enemy types
- Special attack types
- Quest/achievement text
- Dungeon room descriptions
- Boss name variations
- Combo indicators
- Critical hit variations

