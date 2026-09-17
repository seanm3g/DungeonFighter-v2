# Equipment sprites

Open `equipment-preview.html` in a browser to browse every item, equip any combination, or remove equipment. The Art Lab in the game now uses the same base and atlas rectangles and reads the active character's five real equipment slots on refresh.

## Assets

All runtime artwork is in `Code/UI/Avalonia/Assets/ArtLab/`:

- `equipment-pixel-base.png`: revised chunky pixel-art base, using the original `hero-loadouts.png` as the style reference.
- `equipment-pixel-head.png`, `equipment-pixel-chest.png`, `equipment-pixel-legs.png`, `equipment-pixel-feet.png`: 50 catalog entries per slot.
- `equipment-pixel-sword.png`: 50 entries; `equipment-pixel-dagger.png`: 51; `equipment-pixel-mace.png`: 48; `equipment-pixel-wand.png`: 51.
- `equipment-sprites.json`: all 400 entries, exact catalog names and tiers, source rectangles, silhouette coverage, hand grips, hair occlusion flags, and destinations in logical 400 × 600 coordinates.
- The previous `equipment-*.png` originals remain available for reference; the renderer loads the revised `equipment-pixel-*.png` images.

These are static front-facing paper-doll assets, not walk/attack animation frames. Both renderers compose on a shared **100 × 150 native pixel grid**, then enlarge with nearest-neighbor sampling. The native game uses integer scale factors when space permits and caches the composed frame until gear changes.

Layer order is base → **feet → legs** → chest → head → weapon → gripping fingers. Each boot is aligned to its own foot, and trouser cuffs/greaves cover boot tops. Weapon grips meet the hand at logical (288,316). Hoods hide the underlying hair and retain the base face through their open center; fully closed helmets still cover the face by design.

Veil and Headdress use a rear drape behind the head and shoulders, plus a front brow ornament and side edges. Their front pieces leave the face unobstructed and retain the base hair. Each part declares `Layer` (`rear` or `front`) in both renderers.

Apparel now uses per-piece `Parts` mappings instead of uniformly scaling a whole inventory icon. Chest pieces fit at the neckline, shoulder seam, belt and hem. Greaves retain their upper section and fit each shin separately to the base pose, ending at y=520; the feet drawn in the source greave icons are excluded. Actual hanging tabards are retained separately. Breeches and tassets keep shorter lengths. Each boot uses its own tightly bounded source. Its shaft follows the shin center and transitions to the foot at the ankle; both soles meet y=564. Weapon handle anchors are measured from their silhouettes. `equipment-fit-review.png` shows six composed loadouts, including the originally reported combination, for visual review.

The base has alpha, but the revised equipment generator returned an RGB checker backdrop. The manifest's `Coverage` rectangles are therefore required alongside those sheets: the rendering mesh excludes connected background and open face/neck regions. Do not draw the raw RGB source rectangles without coverage. The indexer removes small disconnected fragments, insets the coverage mesh one source pixel to exclude pale edge contamination, and opens collar cutouts relative to each garment. It writes metadata only and leaves original artwork pixels intact. Unequipping removes its layer; the classic text UI is unchanged.

## Catalog matching and limits

Artwork was generated using the existing `GameData/Armor.json` and `GameData/Weapons.json` names, tiers, and order. It is a visual interpretation of those names. Generated sheets contain extra variants and irregular spacing; the manifest selects and bounds the usable sprites rather than assuming a uniform grid. Some stylized silhouettes and grip positions may benefit from per-item art polish. Destination rectangles can be tuned independently per item.

The resolver matches the longest whole catalog name within an equipped item's name, then its tier. Thus prefixes/suffixes do not switch the base silhouette, and `Full Plate Boots` does not resolve as `Boots`. Materials, quality, and affixes currently share the corresponding base item artwork; they do not have separate recolors. Legacy saves have no catalog row identifier, so identical name + tier entries use the first matching sprite. Unknown custom gear falls back to its equipment family and nearest tier. Consumables never create an equipment layer.

## Rebuilding and validation

`Scripts/build_equipment_sprite_manifest.py` reads the catalogs, isolates atlas silhouettes and open face/neck regions, and writes the coverage/anchor manifest plus the preview data script. It requires Pillow. It does not modify any image pixels or item statistics. The manually reviewed row/column selections and equipment anchors live in that script.

Build with `dotnet build Code/Code.csproj -p:KeepRunningInstance=true`. Run `ARTTEST` from the `Code` working directory: it checks mapping, coverage, fitted-part bounds, ankle limits, sole baselines and hand grips plus 16 existing live-equipment integration checks. Run `SPRITETEST <output.png>` to render the Veil / Duelist Coat / Knight's Greaves / Pelt Boots / Mace loadout through the actual Avalonia renderer without opening a window or writing a character save; it also verifies frame changes after gear replacement and unequipping.

`Scripts/verify_equipment_preview.cjs` accepts an installed Playwright module path and uses headless Edge by default (`SPRITE_BROWSER_CHANNEL` can select another installed channel). It verifies 400 renders, equipment changes, unequipping, the full portrait resolution, and catalog search, and writes `equipment-preview.png` and `equipment-alignment.png`. `equipment-native.png` is the matching native-renderer capture.

Images were generated with the built-in `image_gen` tool. Original catalog prompts are in `equipment-generation-prompts.json`; the replacement base and all eight pixel-style atlas edit prompts are in `equipment-pixel-prompts.json`.

Randomized visual checks: run `Scripts/spotcheck_equipment.cjs` with the same Playwright module argument. It exercises the Randomize button with a repeatable seed, saves twelve loadouts in `equipment-random-samples.json`, and captures four full-resolution review sheets (`equipment-random-1.png` through `equipment-random-4.png`). These are spot checks, not an assertion that every possible combination has been visually approved.
