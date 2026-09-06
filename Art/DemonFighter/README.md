# Demon Fighter — first visual asset kit

This is an animated art prototype for the existing DungeonFighter-v2 project. The
player-facing art uses **Demon Fighter**. An opt-in Avalonia stage is now integrated.

Enable **Settings → Appearance → Battle visuals → Show illustrated battle stage**,
then resume combat. All 127 entries in `GameData/Enemies.json` have exact mappings
in `GameData/Visuals/DemonFighter/enemies.json`, including the starter fixture.
They share 34 model families; fire hounds/lava constructs use crimson materials,
spectral creatures use teal, and woodland creatures use bark/venom tones.
Unknown future enemies retain a named marker (or demon preview in the lab).
The hero now reflects sword, dagger, mace, wand or unarmed equipment, each with cloth and plate looks. Weapon rarity colors the gear label. Seven biome-themed stage variants share the same arena footprint; actual enemy biome mappings take precedence over family fallback.
Visual mode places the scene beside a vertical action-card stack. Small arena
layouts and disabled visual mode retain the existing text layout.

Animation and effect toggles are in the same Battle visuals tab, including a
reduced-motion option. Forty-six actors (34 families, hero, preview demon and ten equipment variants) each
have eight clips: idle, attack, hit, cast, guard, evade, death and victory.
Seven-bone rigid rigs add leg articulation, family-specific pounces, wing beats,
heavy slams, asymmetric anticipation, contact, recoil and recovery. Attack/cast
atlases have ten frames; other clips have six or eight. These remain low-poly
prototype models with rigid joints, rather than deforming production skinning.
Attacks hold anticipation until the final damage/heal reveal; damage numbers,
critical bursts, miss/defense labels and generic heal/effect rings follow cues.
Contact frames hold for up to 35 ms cosmetically; sparks, slash cores and critical
shock rings emphasize impact without changing simulation time.
The bat has animated wings. Instant combat deliberately shows static artwork.

The GUI samples atlas animation on a refresh timer; combat logic never waits on
that timer. The existing sequence presenter still owns combat pacing. A bounded
latest-cue handoff discards superseded cosmetic cues at high speeds. Actor IDs
are weak-reference identities, so stale cues cannot affect a different encounter.

Build `Blender/build_enemy_assets.py` after `build_assets.py`, then run
`Blender/build_roster.py`, `Blender/build_progression.py` and `Blender/build_animations.py` in that order.
These produce `enemy-kit.blend`, `roster-kit.blend`,
`animated-kit.blend`, and `GameData/Visuals/DemonFighter/Animations/clips.json`
with 368 embedded atlas PNGs. The progression kit also preserves equipment variants and biome stages. The animated Blender file preserves named actions
and editable rigs. Existing GLB files remain static exports.

For faster exports on a multi-core machine, run four Blender processes with
`build_animations.py -- 0 4` through `-- 3 4` (arguments follow Blender's `--`).
Wait for every worker to finish, then run `Blender/merge_animation_workers.py`.
Intermediate kits live in ignored `Verification/animation-workers/`. Do not build
the game while atlases are exporting; the merged manifest is published last.
`Verification/contact-sheet.ps1` regenerates the model review sheet.
The exporter uses OptiX when available, with a CPU fallback, persistent render
data and per-clip camera crops. A final `resume` argument reuses complete atlases
newer than the roster kit; omit it after changing poses, materials or lighting.
`Blender/validate_animations.py` checks nonempty, changing frames and transparent
crop borders, and reports decoded atlas memory in `Verification/atlas-validation.json`.
The native `CombatSceneRender` suite checks every live enemy mapping, every family
atlas and still, anticipation/contact timing, enemy switching and instant mode.

Validated September 5, 2026: 127 roster entries, 46 animated actors and 368 atlases.
All GLBs contain meshes; every actor retains its editable rig and eight actions.
Atlas content checks passed for dimensions, visible/changing frames and crop
borders. The largest actor atlas set decodes to 10.11 MiB; the renderer retains
only the hero and current enemy sets. The isolated build had zero warnings/errors,
and the targeted renderer, director, sequence, layout, audio and configuration suites passed.
Native offscreen captures verified wolf, lich, bat and other family integration;
this does not replace a live pacing/playfeel review.

Current limits: no dedicated elemental projectiles, persistent status visuals,
separate DoT/reflect reaction cues, per-hit multihit timing, equipment variants,
new audio synchronization or production-quality skinning. A multihit result
currently shows its aggregate damage once; nested retriggers remain separate.
See `IMPLEMENTATION_PLAN.md` for remaining work.

## Experience follow-up

Played action results, scheduler readiness, P pause/resume, existing Page Up/Down speed controls, committed enemy intent, equipment appearance previews, applied-HP encounter reports, XP progress, rest checkpoints and equipped-build guidance are integrated. See `Verification/experience-pass.md` for exact behavior, validation and limits. The final build passed eleven targeted suites; the corrected equipped-build probe completed 120 fights.

## September 5 presentation improvements

- Melee actors approach their targets and recover; ranged attacks travel across the stage. Beast pounces, heavy slams and spectral evade fades preserve family identity. Motion never changes combat results.
- Existing impact audio now commits at the illustrated damage/heal reveal, with duplicate playback protection.
- Persistent status badges show actual values and available turn counters, supported by poison, flame, bleed, shield, stun and armor-break effects. Overflow remains in the existing status panel.
- Bone text, muted borders, sulfur selection and crimson threat accents unify the battle panels. Gear, rank, next combo slot and contextual loadout advice appear on the stage.
- Victory/defeat feedback includes **shown strike** totals; these are cosmetic cue totals, not full encounter statistics including DoT or reflection.
- A real headless sample of 760 combats completed without reported errors. See `Verification/playtest-notes.md` for results and limitations. No balance values were changed. An alternate-launch GameData path bug discovered during testing was fixed separately.

Native offscreen previews are `Verification/polished-encounter.png` and `Verification/impact-integration.png`. Live pacing, sound quality and enjoyment still need an interactive play session.

## Art direction

Use a fixed orthographic battle stage with Blender-rendered PNG combatants in
Avalonia. This fits the existing 2D desktop renderer without adding a real-time
3D engine. Keep the editable scene and GLB meshes for later 3D experiments.

The supplied posters are visual references: sulfur yellow, crimson, ink black,
bone, chipped surfaces, horns, steel and a fanged doorway. Their wording is not
an instruction or the new game title. The generated concept explores a more
detailed destination; the Blender kit is an intentionally simpler first blockout.

## Deliverables

- `Concepts/demon-fighter-concept.png`: built-in image generation concept sheet.
- `Concepts/prompt.txt`: exact generation prompt and provenance.
- `Blender/demon-fighter-kit.blend`: editable meshes, materials, camera and lights.
- `Blender/build_assets.py`: deterministic Blender 5.1 build/export/validation script.
- `../../GameData/Visuals/DemonFighter/battle-stage.png`: full rendered composition.
- `../../GameData/Visuals/DemonFighter/arena-background.png`: environment without actors.
- `fighter-layer.png`, `demon-layer.png`, `portal-layer.png` in that output directory:
  transparent, full-canvas layers.
- `demon-fighter-kit.glb`, `fighter.glb`, `demon.glb`, `portal.glb`: 3D exports.
- `manifest.json`: dimensions, compositing contract and verification results.

The output directory above is relative to the repository root: `GameData/Visuals/DemonFighter`.

## Rebuild

From the repository root in PowerShell:

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.1/blender.exe' --background --python Art/DemonFighter/Blender/build_assets.py
```

This replaces only this kit's generated `.blend`, PNG, GLB and manifest files.
Save manual Blender edits under a different filename before rebuilding.
The script validates PNG dimensions and alpha, GLB headers and mesh presence.
No game saves or gameplay configuration are read or modified.

## 2D integration contract

All PNGs are 1600 by 900. Scale them together with one uniform scale and one
destination rectangle, preserving the 16:9 aspect ratio. Draw the background,
then the fighter and demon layers. Do not separately center or crop the actor
images: their transparent padding carries their stage positions. The portal is
already in the background; its optional separate layer is for alternate scenes.

The composite is the lighting reference. Individual character layers do not
include ground shadows or inter-object occlusion, so compositing will not be
pixel-identical. Add simple contact shadows when integrating moving actors.

Keep HP, status text, combo order and combat narrative as live Avalonia controls
or existing canvas text. Use crimson for enemy/threat accents and sulfur for
selection and action emphasis, with bone-white body text on dark panels.
Avoid baking statistics or button labels into artwork.

## 3D limitations and next implementation step

Meshes are named and grouped by Arena, Portal, Fighter and Demon. Individual
GLBs preserve the scene positions rather than centering each model at the origin.
The Blender scene has procedural surface grain; GLB exports retain base colors
but do not bake that grain. Materials are not yet a complete portable texture set.

The original GLBs are static meshes without baked 3D texture atlases. The separate
animated Blender kit adds rigid-bone rigs and actions; the runtime uses rendered
2D atlases. Deforming skin/cape rigs and unique per-item armor remain future work.

The optional stage adds a bounded bitmap renderer, embedded assets and layout
geometry, with no new runtime dependencies or combat math changes. An isolated
build, geometry tests and native offscreen rendering verify the first integration.
The offscreen rendering test can save an image when DEMON_FIGHTER_RENDER_CHECK is
set to an absolute PNG output path. It does not open the game or load a save.


