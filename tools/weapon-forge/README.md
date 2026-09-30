# Relic Forge

## Painted raster workshop (current default)

Open `/raster.html` or the root URL for the painted item renderer. The mace, sword, dagger and wand paintings now have true alpha, independently tinted metal/leather/fittings, eight presets, saved color recipes, transparent PNGs, editable layer-pack ZIPs, and a sprite-sheet ZIP with frame metadata. It uses Canvas and PNG assets, not SVG. One painted base is prepared so far; the legacy catalog is available at `/vector-forge.html`. See [raster/README.md](raster/README.md) for the asset contract and production workflow. Run `npm run test:raster` for focused validation.

## Legacy procedural catalog

Standalone modular art preview covering all 417 named rows in the Weapons, Armor, Charms and Consumables catalogs. No dependencies or build step. Game rendering and loot rules are not modified.

## Run

Run `node server.mjs` from this directory and open http://127.0.0.1:4318, or use `Launch Weapon Forge.bat` in the repository root.

## Browse and compare

- Choose a family: swords, maces, daggers, wands/focuses, headwear, body armor, legwear, footwear, charms or consumables. All families is also available.
- Select a card, then choose any catalog entry in Base item. Tier variants and duplicate catalog rows have separate recipe IDs.
- Lock Base item to compare affixes on the same object. Material, quality, prefix and suffix can also be locked. Family and selected-item shuffles retain each card's locked values.
- Pin a card to exclude it from shuffling. Manual edits still apply.
- Replay rebuilds the seeded collection and clears pins and locks. Choose 4, 6 or 10 studies per family.
- PNG (840 × 1260), SVG and recipe JSON exports work for every family. Transparent exports include pigment dropout. Saved four-weapon sessions are extended with the six added families; original studies and v1/v2 recipes remain supported.
- Consumables retain their named purpose and base colors, with equipment affix controls disabled. Print distress remains available.

## Art reference

Follow [STYLE-GUIDE.md](../../Documentation/ArtLab/ArtDirection/STYLE-GUIDE.md) and its ten-mace reference for all families. The current target is sculpted hand-painted volume, component-specific gradients, contact shadows and restrained brush texture. Earlier flat-only guidance is superseded. The guide includes construction requirements and visual acceptance checks for each family; existing models are not automatically considered compliant.

## Visual ownership

Material owns the body and fitting palette (19 materials, including wood, leather and cloth). Quality owns grip/binding/trim colors and workmanship details. Prefix owns construction accents and material-colored embossed crests. Suffix owns the emblem, animal-family ornaments or a poetic hanging seal. Print distress erases pigment from the finished illustration independently of item quality.

There are 44 reusable silhouettes: the original four in `renderer.js` and 40 in `equipment.js`. Catalog names map to these shapes with shared tier details. They are not 417 bespoke illustrations. Weapon subtypes include flails, hammers, curved blades and polearms; magical focuses include books, scrolls, orbs, idols, lanterns and vessels. Armor and provisions have their own shapes and attachment locations.

All 164 suffixes are available, grouped by their source rarity. Animals share visual families; poetic suffixes map to symbolic seals. Related names intentionally share artwork. Counts represent recipe combinations, not guaranteed unique images. This visual sandbox samples uniformly and does not enforce gameplay drop probabilities or combat legality.

## Catalog maintenance

From the repository root:

```sh
node tools/weapon-forge/sync-items.mjs
node tools/weapon-forge/sync-suffixes.mjs
node --test tools/weapon-forge/tests.mjs
```

The generated catalogs preserve source names. Blank consumable placeholder rows are excluded. Tests check exact catalog coverage, silhouette support, recipe validation, deterministic rolls, locks, every item's suffix compatibility, the core material/prefix/suffix matrix and distress/export markup.

Footwear uses the approved forward-facing, slightly outward standing stance from boot-study.html. footwear.js derives plain boots, low shoes, sandals, wraps, fur cuffs, plate, scale and mail treatments from catalog names. Material planes keep a shared upper-left light direction across the mirrored pair. Quality controls cuffs, bindings and soles; prefix and suffix attachments are positioned on both feet. Print distress remains a separate final-art mask.

Headwear: headwear.js maps all 50 catalog entries into 20 silhouette families, including horned, beaked, crested, dragon, kabuto, crown, mask and hood constructions. Per-family affix anchors place emblems at the brow, crown band, hood clasp or gorget. Material lighting and quality bindings remain shared; no per-permutation assets are baked.

Consistency review: open /consistency.html to compare representative silhouettes with the same material, quality, seed and finish, both plain and affixed. paint-planes.js gives body armor, trousers, charms and magical focuses shape-specific broad light/shadow planes. Ornaments attach to belts, clasps, bindings or pedestals; higher-tier trim is restricted to suitable surfaces. The three charm bases have distinct settings. Specialized headwear, footwear and consumable renderers extend the 44 underlying shape identifiers described above.

Blade refinement: blade-art.js supplies dedicated scythe, cleaver, saber, rapier, polearm, hook, fang and kris constructions. The scythe uses a sweeping blade and riveted socket, with affixes anchored on its offset shaft. Sword and dagger artwork also uses the shared subtle gradient and pigment finish; print distress remains separately adjustable.

Legwear: legwear.js maps all 50 catalog entries to 13 constructions: breeches, reinforced breeches, trousers, leggings, wraps, tassets, knee guards, shin guards, light greaves, mail greaves, plate greaves, heavy greaves and full plate. Legacy recipe shape IDs remain compatible; the renderer resolves the detailed model from the base item name. Paired pieces have separate affix anchors. Open /legwear-review.html to see all models; the consistency sheet also groups by these models. Run npm test to include legwear coverage checks.

Weapon reference pass: all 200 sword, dagger, mace and wand/focus entries use weapon-paint.js. Gradients restart within each construction face, with separate binding/fitting responses and restrained clipped brush patches. weapon-construction.js adds dimensional morningstar, orb, vessel, lantern and scroll models. Preview the 26 shared family/shape constructions at /weapon-review.html; named overrides continue to use their existing designs. Print distress remains independent.

Remaining-item reference pass: the 217 non-weapon entries now use the shared component paint treatment. Headwear gradients follow local panels; footwear reverses local gradient direction before mirroring to preserve the scene light. Food and apothecary items shade their authored colors without equipment-metal palettes. torso-construction.js supplies the breast shell, shoulder caps, overlapping faulds, garment openings and mail details. /item-review.html compares 62 representative constructions across all remaining families. Existing SVG definitions are preserved without duplicating IDs in paint masks.

Study backgrounds: use the Background picker and Apply to all for a shared color, Randomize per item for independent colors, or the inspector's color picker/Randomize button for just the selected study. Reset backgrounds restores parchment. Colors stay with family study slots through recipe edits and shuffles, persist locally, and are included in recipe exports. Opaque SVG/PNG exports use the selected color; transparent exports stay transparent. Background changes do not alter item recipes or affix locks.

Background palette: add/remove swatches and edit them using color pickers or six-digit hex values. Use palette restricts all study backgrounds to those swatches; Shuffle palette across items and the selected-item Randomize button draw only from that palette. Editing a swatch updates its assigned studies immediately. Palette colors, mode, and assignments persist locally. Applying a single color or resetting backgrounds exits palette mode without deleting the saved palette.

September 23 reference refinement: mace-art.js replaces the pointed mace with broad forged flanges and recessed side faces, models overlapping full-length wraps, and supplies rounded collars shared by the core weapons. Plain pommels now have faceted volume. The shared surface pass differentiates soft materials from metal and uses clustered pigment marks. /reference-review.html compares ten mace recipes, inventory-scale art, and representative families against the supplied concept. The Style reference dialog now shows that same concept. These changes affect forge previews and exports; the standalone forge remains separate from the Avalonia game UI.

Second realism pass: paint-surface.js adds deterministic multiscale pigment modulation with preserved alpha. Core mace faces, leather turns and collar rims are less regular; magical crests have filled shallow relief. Coat construction now includes hollow sleeves, lapels and belt-driven folds. Circlets show the rear band, breeches use a narrower tailored contour, foot bindings overlap, charms have retaining tabs, and apothecary glass has a meniscus and thick base. All remain procedural and recipe-editable.

Painted prefixes: raster.html now offers Flaming, Poisonous, Charged, Enchanted and Blessed for all four painted weapons. Material-colored reliefs are fitted per weapon and included in PNGs, recipes, layer packs and sprite sheets. Per-weapon edits persist across reload. See raster/README.md for authoring and validation.
