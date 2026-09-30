# Painted mace trial 01

Generated with the built-in image generation tool, using `../../painted-mace-reference.png` as the visual reference. The selected artifact is `steel-mace-study-v1.png` (1024 × 1536). This is an opaque visual study, not a transparent production sprite or a layered material system. The procedural renderer remains available for comparison.

## Generation prompt

Use case: stylized-concept. Asset type: finished raster inventory sprite for Dungeon Fighter. Use the attached ten-mace concept sheet as the PRIMARY visual reference. Create ONE isolated plain mace matching that exact painting style, construction, proportions and pose, closest to mace 8's steel head with mace 6's dark brown leather grip, warm aged brass collars and rounded brass pommel; no emblem, no jewel. Preserve the reference's broad thick flanged head with a broad recessed front face, substantial almost-black side planes, irregular pale steel bevels, a short enclosing double brass collar, and continuous overlapping dark leather wraps down the handle. Handle points down-left, head up-right, approximately 25 degrees from vertical. Entire item visible with comfortable padding on a portrait canvas, occupying about 85% of the height. Genuinely transparent background with alpha, no painted checkerboard, no paper, numbers, border, cast shadow on a ground, or other objects. Match the reference as closely as possible: hand-painted dark-fantasy game illustration with sculpted weight, broad deliberate brush planes, muted steel and umber, selective warm highlights, dark contact shadows, irregular leather edges and restrained brush variation. Preserve the simple grounded design; do not elaborate into ornate fantasy machinery. NOT a photograph or shiny 3D render. NOT vector art, smooth plastic gradients, cel-shaded clipart, or a line drawing. This should look like one of the original concept sheet's maces carefully painted as a standalone production asset, not a redesign.

## Final background edit prompt

Edit only the background. Replace EVERY gray and white checkerboard square around this mace with a SOLID FLAT WARM TAUPE background, hex #918572. Opaque taupe background, absolutely no checkerboard or white/gray squares remaining. Keep the mace, its pose, scale, steel, leather, brass, paint texture and all object details unchanged. Do not add ground shadows or any background texture. This is a product illustration on flat taupe, not a transparency preview.

The initial output and an extraction retry contained a painted checkerboard. The final selected image uses an opaque background instead. No transparency is claimed.

## Prepared runtime asset

`steel-mace-base-v1.png` is the alpha-extracted painting; `steel-mace-regions-v1.png` is its RGB material map. These are built by `../../raster/prepare-mace.mjs` from the approved study and `steel-mace-matte-source-v1.png`. The original study above remains opaque as an archival source. See `../../raster/README.md` for runtime coloring and exports.
