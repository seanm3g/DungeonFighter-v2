# Modular inventory icons — concept 01

Status: proposed art system, awaiting art-direction approval. No runtime changes or full catalog generation.

Revision 02: user requested much more minimal, pixel-y artwork. `modular-study-02-minimal.png` supersedes the first board for style exploration: target a 16×16 logical canvas, chunky silhouettes, approximately 3–5 colors, and affix details of only a few pixels. This is still generated concept reference, not a verified native-resolution atlas. Built-in imagegen edit prompt: `modular-study-02.prompt.txt`. The original 32×32 direction below is retained only as historical context; use the revised 16×16 target going forward.

## Visual direction

See `modular-study-01.png`: four weapon families, each showing material, quality, and suffix treatments. This generated board is visual reference, not a slice-ready sprite atlas. Its illustrated breakdown is not yet a set of registered layers.

Target 32×32 transparent pixels per finished inventory icon, nearest-neighbor scaling, a consistent upper-left light, dark outlines, and approximately three shades per material. Reserve a small margin for effects. Keep silhouettes readable without effects. Weapons point upper right; armor can be front-facing and independent of character proportions.

## Composition recipe

1. Base catalog identity selects the silhouette and compatible component set.
2. Material selects the palette and surface pattern for the main component: blade, mace head, wand shaft, or armor body. Grips and bindings retain their own palettes.
3. Quality selects wear masks and fitting variants: Broken chips, Worn scuffs, Battle Scarred notches, Masterwork clean fittings. Avoid obscuring the base silhouette.
4. Suffixes select small accents at authored anchors: Accuracy rune near the guard, Agility tassel near the grip, ferocity binding or rune. These are proposed visual associations, not changes to gameplay.
5. A compatible magical property can supply a restrained back/front pixel effect. The sheet's Arcane study is an exploratory treatment, not a verified catalog suffix.

Sword component slots: blade, guard, grip, pommel, detail, rear effect, front effect. Other families define their own slots: mace head/shaft/wrap; wand shaft/tip/collar; armor body/trim/fasteners. Each family shares a fixed canvas, anchor coordinates, and attachment seam conventions. Wear masks clip to the affected component.

Draw order: rear effect → main components → fittings → clipped wear → suffix accents → front effect. Render from authored transparent pixel layers and masks, not fresh image generation per permutation.

## Rules that keep combinations readable

- Preserve base identity across affixes; avoid changing every part simultaneously.
- Material owns the main surface palette, quality owns wear/fittings, suffix owns small ornaments, and effects own only the reserved outer margin.
- Allow one dominant effect and up to two small accents. Stable priority resolves collisions; all gameplay suffixes still appear in the tooltip.
- Multiple suffixes targeting one anchor resolve deterministically instead of overlapping.
- Use neutral fallback components for unknown visual mappings. Never invent mechanical effects from icon art.
- Derive recipes from structured item properties and stable catalog identifiers where available. Use name parsing only as a compatibility fallback.
- Store a visual version and seed so the same item keeps the same appearance across inventory, tooltips, and sessions. Cache composed icons by recipe.
- Component variations are cosmetic unless backed by an actual item property; a decorative leather grip does not grant a leather material bonus.

## Fit with current game data

`Code/Data/ItemMaterialRules.cs` defines sword Bronze/Gold/Mithril, dagger Glass/Obsidian/Shadow, mace Bone/Steel/Iron, and wand Willow/Silver/Crystal ladders. The concept samples those materials rather than changing loot rules. `GameData/PrefixMaterialQuality.json` supplies quality names; `GameData/StatBonuses.json` supplies suffixes. Material-trigger mechanics remain distinct from stat-suffix visuals.

## Next phase, only after approval

First author registered production layers for these four weapon families and a small representative affix set. Verify actual 32×32 readability, attachment seams, alpha, and combinations. Then build a small seeded permutation demo with Randomize, seed entry, family/material/quality/suffix selectors, locks for individual choices, layer toggles, and native/enlarged previews. A normal mode follows existing item-generation constraints; an explicitly cosmetic exploration mode can show other art combinations. Expand to armor, consumables, and the entire catalog only after the pilot is approved.

Generation: built-in imagegen. Full prompt is saved in `modular-study-01.prompt.txt`.
