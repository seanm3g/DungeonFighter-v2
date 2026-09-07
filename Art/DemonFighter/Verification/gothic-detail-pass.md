# Gothic model detail pass

The user-supplied reference is retained in `../References/gothic-detail-reference.png`.
It guides the bone, weathered iron, torn crimson cloth, ruined sanctuary, and warm
rim lighting. This is a detailed stylized 3D interpretation; it does not reproduce
the illustration's hand-painted outlines, realistic anatomy, or bespoke sculpting.

## Asset changes

- All 46 presentations receive added geometry and revised materials: 34 enemy
  families, the base fighter and demon, and ten weapon/armor fighter variants.
- Inset armor plates, carapace/scales, fur-like surface tufts, metal fasteners,
  worn edges, material roughness, fine surface bump, and color variation.
- Fighter capes are folded meshes with irregular hems. Plate equipment has
  layered shoulder and hip armor; swords have guard detail and an inset garnet.
- Demon gains torn wings, bone struts, overlapping bone armor and scapular spikes.
- All seven biome stages plus the default stage receive rear ruined buttresses,
  hanging standards, masonry debris, floor fissures, stairs and a skull crown.
- Cold fill and warm rim light replace lime illumination. Portal particles use
  amber to fit the new stage lighting; combat and selection semantics are unchanged.

The 1600×900 camera, collection IDs and actor positions are preserved. Animation
frames retain the 800×450 canvas and clip timing. Rebuilt clips use 16 samples
rather than 8; static renders use 24 samples. Extra render-time geometry adds no
runtime 3D rendering dependency to Avalonia.

## Reproduction

Run these scripts with Blender 5.1 in background mode from the repository root:

1. `Art/DemonFighter/Blender/refine_models.py` builds `detailed-kit.blend` from the
   unchanged `progression-kit.blend` and saves the review render and mesh audit.
2. Inspect `gothic-detail-preview.png`, then run `export_detailed.py` for all static
   layers, stages and GLBs.
3. Run `build_animations.py -- N 4`, once for each N from 0 through 3. Use at most
   two concurrent Blender render processes on the 12 GB development GPU; serial
   jobs proved faster once the scenes started approaching the VRAM limit.
4. Run `merge_animation_workers.py` after all four workers finish.
5. Run `validate_detailed.py`, `validate_animations.py` and `detail_contact_sheet.py`.
6. Build `Code/Code.csproj` into `Art/DemonFighter/Verification/bin` using
   `--no-restore -p:OS=Unix`, preserving any currently running game process.

`resume` only accepts atlases newer than both their selected source kit and the
animation script, preventing stale blockout frames from surviving a model rebuild.

## Portability and limits

GLBs contain the detailed meshes and base PBR material colors. Procedural grain
and weathering are rendered into the PNGs; they are not baked GLB texture maps.
The editable animated kit retains the existing rigid-part rigs and eight actions
per actor. Cloth and wings follow those attachments rather than a new deforming
skin or cloth simulation. Enemy variants still reuse their established families.

The validation JSON files and native Avalonia screenshots are generated checks,
not evidence of a manual interactive gameplay session.

Static export validation passed: 46 presentations covering 127 enemy definitions,
eight stages including the default, 1,961 actor meshes versus 1,007 in the blockout.
Every presentation has added geometry. All actor attachment tags, PNG dimensions,
transparency and trim borders, and all exported GLB headers/mesh payloads passed.
`detailed-roster.png` provides the complete visual inspection sheet; its index is
stored alongside it in `detailed-roster-index.json`.

Animation validation passed for all 46 rigs, 368 named actions and 368 atlases:
dimensions, visible/changing frames and transparent trim borders. The largest
actor atlas set decodes to 11.38 MiB (baseline 10.11 MiB). No working-frame PNGs
remain. The isolated build succeeded with zero warnings and zero errors.
`CombatSceneRender` passed all 245 checks, and `CombatSceneGeometry` passed.
The 1280×800 and 1920×1080 native offscreen renders and equipment thumbnail were
visually inspected after rebuilding with the new embedded artwork.

Updated executable: `bin/DF.exe` relative to this directory. Close the old game
and open that executable when ready to use the new assets.
