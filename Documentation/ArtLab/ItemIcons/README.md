# Item icon system

The approved minimal direction is now a deterministic, code-authored 16×16 transparent pixel compositor. It covers all 414 named catalog entries: 200 weapons, 200 armor pieces and 14 consumables. Blank consumable rows are skipped. There are 19 material palettes (including Cloth, Leather and Wood), 11 qualities, 23 adjective prefixes and 164 stat/animal/story suffixes.

## Run the demo

Double-click **Dungeon Fighter Item Icons.bat** from the repository root, or run:

```powershell
dotnet run --project Code -p:KeepRunningInstance=true -- ICONLAB
```

The app opens the **Rarity Gallery**: six color-coded rows from Common through Mythic, with 6, 12, or 24 samples per rarity. Generate repeats the current seed; Shuffle chooses a new seed. Filter by weapon/armor family and item tier. Select any card to inspect its full affixes, export its transparent native PNG, or save the full grouped batch as JSON. Consumables have a separate unranked view. The app does not load a character or modify saves.

The rarity preview uses the embedded catalog's quality ranks, weapon material ladders, eligible affix ranks, and RarityTable suffix counts. Base-item tier is a separate filter. It deliberately samples equal numbers per rarity, not drop probabilities; it does not simulate live tuning overrides, action bonuses, or player magic find.

Click **Affix editor** for the original unrestricted editor, or launch with `ICONEDITOR`. Choose a family/item, material and quality, and add any number of prefixes and suffixes. Select a generated card to load its complete recipe into the controls. Use the same seed and Generate to reproduce a batch; Next batch advances it. Locks preserve selected properties. Browse catalog displays every item in the selected family. Export a transparent native 16×16 PNG or a versioned JSON recipe using the export buttons lower in the scrollable controls panel.

These are cosmetic permutations, not a loot probability simulator. It deliberately allows combinations outside weapon material ladders. Consumable food/liquid palettes preserve food/effect identity; their material field does not repaint edible contents. Actual game icons use the item's structured Material, Modifications and StatBonuses.

## Deliverables

- `production/catalog-atlas.png`: transparent native atlas, 24 columns of 16×16 cells.
- `production/catalog-atlas.json`: exact source rectangles, catalog row IDs and default recipes.
- `production/catalog-review.png`: enlarged labeled review of all items.
- `production/affixes-review.png`: every supported material and affix on a reference sword.
- `production/permutations-review.png` and `permutations.json`: reproducible mixed examples.
- `production/lab-review.png`: actual rendered demo UI.
- `production/verification.txt`: most recent verification result.
- `production/rarity-gallery.png`: rendered rarity gallery UI.
- `production/rarity-batch.json`: example grouped batch.
- `production/rarity-verification.txt`: rarity sampler and UI verification results.

An individual entry does not require a separately generated bitmap for each combination. Render recipes on demand; a bounded 512-frame cache reuses recent icons. The single-prefix/single-suffix cosmetic space alone is 373,792,320 combinations when None is included for quality/prefix/suffix. Multiple affixes and visual seeds expand that space further. A sheet of every permutation would be impractical; the compositor supports the space without storing it.

## Integration and rendering rules

`ItemIconCatalog` reads embedded snapshots of the six relevant GameData JSON catalogs at build time. Rebuild after catalog edits. Catalog row IDs distinguish duplicate names in the demo/atlas. Existing saves lack a base catalog ID; runtime resolution uses the longest whole catalog name, matching family, then closest tier, with a stable tie break. Unknown/modded names receive an appropriate family silhouette and neutral fallbacks.

`ItemIconShapes` contains authored pixel component masks for 42 silhouettes, including non-staff magic items such as books, charms, skulls and lanterns. Related catalog entries share these modular silhouettes and receive tier/seed fitting accents; the system does not promise a unique silhouette for every catalog row. The masks are original production pixel definitions, not slices of the generated concept boards.

`ItemIconRenderer` composes the material surface, grip/lining, fittings, prefix structure, quality wear, outlines and affix accents. Wear pixels are protected from later ornaments. Stable sorting makes affix list ordering irrelevant. Food and potion contents retain their own palettes. Output uses only opaque color pixels or full transparency, with integer scaling.

To preserve legibility, a frame shows at most one external effect and two local accent motifs. Structural prefix changes can coexist with those accents. Extra accents remain in the recipe and gameplay tooltip; the demo names the suppressed details. Animal suffixes share readable motifs by taxon (feather, fang, shell, etc.); related suffixes can share artwork. Icons complement names and tooltips rather than uniquely encoding every stat. Generic action bonuses do not get separate visual layers.

The same renderer is wired into regular inventory/comparison item names and the Art Lab bag. Text-grid icons are hidden when the text row is shorter than 16 logical pixels to avoid clipping. Full and partial panel clears remove their icons; hover overlays draw above them. Character-fit equipment portrait sprites remain a separate system.

## Verification / regeneration

```powershell
dotnet build Code/Code.csproj -o Code/bin/IconLab/net8.0 -p:KeepRunningInstance=true
& Code/bin/IconLab/net8.0/DF.exe ICONTEST Documentation/ArtLab/ItemIcons/production
```

ICONTEST checks every catalog entry against every individual material and affix, plus 4,096 seeded mixed recipes. It checks alpha, dimensions, deterministic ordering/cache eviction, structured item resolution, non-mutation of game data, retained scars, unknown fallback, canvas clearing, demo card selection, locks, family filtering and add/remove controls. It regenerates the atlas/review artifacts. This is exhaustive single-affix coverage plus mixed sampling, not an exhaustive test of every mathematical combination.

The earlier `system-proposal.md` and imagegen studies document the approved direction. Runtime rendering no longer calls image generation.

Run `DF.exe RARITYTEST Documentation/ArtLab/ItemIcons/production` for focused rarity-gallery tests and a refreshed screenshot. It checks reproducible rows, rank-appropriate quality/materials, independent family/tier filters, counts, card selection, shuffle, and empty/consumable states.
