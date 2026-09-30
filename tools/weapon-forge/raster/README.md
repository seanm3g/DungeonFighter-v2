# Painted item pipeline

The default forge opens `raster.html`. The previous 417-row procedural catalog remains at `vector-forge.html`. The flanged mace, arming sword, broad dagger and crystal wand each have a prepared painted base. Color presets are tint variations, not separately modeled physical materials. No existing gameplay renderer or affix rules are changed.

## Asset contract

Each asset in `manifest.js` has an immutable ID/version, a full RGBA painting, a same-size RGBA material map, named regions, reference luminance and a normalized pivot. The map uses red for head/body, green for leather/bindings and blue for fittings. Each foreground pixel belongs to exactly one region. Painting alpha owns the silhouette. Region images are authoring assets, never shown in the normal artwork.

`core.js` preserves source alpha and the original painting when tint amounts are zero. Tinting maps painted luminance through a continuous curve with black and white fixed, preserving brush variation instead of flattening highlights. Presets and custom per-region colors use the same engine. Canvas performs final scaling with smoothing; PNGs include no SVG wrapper, filters, fonts or external references.

The prepared mace is derived from the approved painting. `prepare-mace.mjs` uses the aligned checker-background generation as an extraction matte, floods its border-connected neutral background, retains the connected object, and replaces contaminated boundary colors with interior samples. It assigns the material regions with authored seams. This extraction is specific to these aligned source images; new paintings need their own validated matte and region definitions.

To rebuild, install `sharp` as an offline authoring dependency or pass its installed package path:

```
node raster/prepare-mace.mjs /absolute/path/to/sharp
node --test raster/tests.mjs
```

Runtime needs no packages, API keys or image generation calls.

## Exports

- PNG: selected color recipe at 1024×1536, 512×768, 256×384, or 64×96. Transparent by default; opaque exports use the chosen color, never the preview checkerboard.
- Colors: validated versioned JSON, persisted locally and importable without arbitrary asset URLs.
- Layer pack: base PNG, RGB region map, three tint-baked transparent layers, recipe and asset manifest. Composite the layers at (0,0) with source-over. ZIP uses standard stored entries and CRC32.
- Sprite sheet: eight presets in a 4×2 atlas, 256×384 frames with 2-pixel transparent gutters, plus frame rectangles, pivots, dimensions and recipes. PNG and JSON are packaged together.

## Adding an item

1. Generate a painting against the approved reference. Preserve its master file.
2. Produce and visually inspect a clean alpha cutout on light, dark and checker grounds, including inventory size.
3. Author a same-sized region map along actual material boundaries. Check saturated test colors for spill.
4. Add the asset metadata and reference luminance values. Register supported assets explicitly; do not reuse a mace for unrelated silhouettes.
5. The studio selector reads registered assets automatically. Verify original colors, independent regions, alpha, PNG sizes and layer composition.

Five painted prefix reliefs are implemented for all four weapons. Suffix motifs, construction-changing prefixes, expansion to the full catalog and wiring the C# UI remain subsequent integration work.

## Additional weapon bases

The weapon selector now includes the flanged mace, arming sword, broad dagger and crystal wand. Each has a 1024 × 1536 transparent base and an aligned RGB material mask. All eight presets, PNG exports, recipe import/export, layer packs and sprite sheets follow the selected weapon. Color and prefix edits for each weapon persist across switching and reload. The stable mask channel names remain head/grip/fittings; labels describe blade or crystal and shaft where appropriate.

Rebuild the three new cutouts with `node raster/prepare-weapons.mjs /path/to/sharp sword` (or dagger/wand). Source paintings and generation prompts live in `assets/painted/weapon-prompts-v1.md`. Background extraction retains only the main weapon silhouette and cleans its edge; material boundaries are authored per painting. Further weapon subtypes require their own paintings.

## Painted prefixes

Flaming, Poisonous, Charged, Enchanted and Blessed use five generated raster motifs, fitted into 20 weapon-specific relief maps. They are not 20 separately generated weapon paintings. Each surface has its own center, scale, angle, shear and host-region restriction in prefixes.js. Mace crests occupy a front flange, sword/dagger crests sit above the guard, and wand crests are stamped on the crystal face. The preparation step preserves motif aspect ratios, removes white negative space (including rune holes), and clips relief to fully opaque pixels of the intended material.

Runtime shadePrefix samples a single material palette from the already-tinted host surface, then paints an opaque stamp using the relief artwork’s own shadows and highlights. Underlying brush marks do not show through the stamp. Only contour antialiasing has fractional coverage; holes stay transparent and the weapon alpha stays unchanged. The five mappings follow the existing restrained material-colored crest direction; no colored emission is introduced. Small inventory images necessarily lose the finest relief detail; the workshop includes a close-up.

Recipes retain version 1 with optional prefix; old files migrate to none and unknown prefix IDs are rejected. Presets retain the selected prefix. Hold-to-compare and restore-original show the unmodified base. PNG and sprite-sheet exports include the prefix. Layer packs retain separate base material layers and add prefix.png, prefix-relief.png and prefix.json. Composite prefix.png last. Only fully opaque host pixels receive relief, allowing the layer pack to reconstruct the flattened artwork to rounding tolerance.

Authoring: node raster/prepare-prefixes.mjs /path/to/sharp

Validation: node --test raster/tests.mjs; node raster/verify-prefixes.mjs /path/to/sharp

Sources, extracted motifs, fitted maps, review image and generation prompts are in assets/painted/prefixes/. Generation used built-in imagegen; prompts are in PROMPTS.md. Runtime needs no new dependencies.

Placement refinement: larger face-centered crests have clearance above the fittings. Circular seals retain their proportions; Charged compensates for its intrinsic diagonal. prefixSurface() provides per-motif layout to preparation, detail previews and layer-pack metadata.

Proportion/seam correction: the mace uses steel-mace-base-v2.png and its v2 material map, prepared with prepare-weapons.mjs (mace). Its stable recipe ID is retained for saved colors. The v1 source and prepared files remain archived. Sword and dagger blade masks follow polygon boundaries to exclude raised guard tips; sword grip boundaries follow the collar/pommel seams. Sword crests sit nearer the guard, dagger crests follow the hilt axis, and wand crests occupy the crystal (head region). Mace generation prompt: assets/painted/mace-v2-prompt.md.
