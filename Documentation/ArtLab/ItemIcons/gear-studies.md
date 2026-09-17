# Non-weapon gear concept studies

Two concept sheets extend the approved minimal pixel style to all four non-weapon equipment slots. These are art-direction references, not proof of a working renderer or production layer assets. No runtime code or loot rules changed.

`gear-study-01.png`: rows show head, chest, legs and feet. Columns cumulatively show base → material → quality → adjective prefix → stat suffix.

`soft-gear-study-01.png`: hood, coat, trousers and shoe demonstrate cloth/leather treatments. Columns show base → quality → adjective prefix → stat suffix. Cloth/leather here are visual construction studies; this does not add them to the current JSON material pool.

## Component ownership

| Slot | Main surface | Secondary parts | Affix anchors |
| --- | --- | --- | --- |
| Head | Shell or hood fabric | Rim, lining, ties | Forehead, temples, clasp |
| Chest | Plates or coat fabric | Straps, belt, seams | Shoulders, chest clasp, buckle |
| Legs | Guards or trouser fabric | Waistband, straps, patches | Knees, belt, side ties |
| Feet | Shell or shoe upper | Sole, cuff, laces | Ankle, heel, buckle |

Materials recolor only appropriate surfaces. Quality uses scratches/chips on hard surfaces and frays/patches on soft ones. Prefixes use shape changes, reinforcing parts or limited effects. Suffixes use small local accents. The same semantic affix uses slot-specific attachments instead of copying weapon geometry.

## Production checks revealed by the concepts

- Generated cumulative variants drift: the worn shinguard chip is lost when straps are added; the hood's worn hem changes between cells. Production composition must retain the quality layer and clip or occlude it deliberately.
- Enchanted and Intelligence both use purple, and Nimble and Agility both use mint. Distinguish them by location and shape; icons will not encode every stat unambiguously, so tooltips remain authoritative.
- Details are somewhat denser than the weapon reference. Author final layers on an actual 16×16 canvas and check at native scale before catalog expansion.
- Reserve separate attachment slots for simultaneous prefix/suffix details. Apply the existing effect budget when combinations compete for space.

Generated with built-in imagegen. Exact prompts: `gear-study-prompts.json`.
