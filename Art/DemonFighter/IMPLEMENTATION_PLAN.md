# Demon Fighter — animated combat implementation plan

## Current delivery — September 5, 2026

The six follow-up improvement areas are integrated: connected melee and traveling ranged attacks; family-specific movement and spectral fades; persistent status badges/effects; unified battle colors and readable overlays; equipment looks, elite markers and biome stages; and contextual build/combination feedback backed by headless playtests.

The kit contains 46 animated actors and 368 atlases, including ten weapon/armor combinations. All 127 enemy definitions retain exact mappings to 34 families. Seven biome stage variants reuse the established composition. Existing impact sounds synchronize with illustrated damage/heal reveals. Gameplay remains authoritative and independent of animation.

Validation: isolated build, zero warnings/errors; ten targeted suites passed (scene render, visual director, sequence builder/presenter, scene geometry, arena layout, resolve stack, primitive stacking, audio dispatcher and patch profile). Blender validation passed all 46 rigs and 368 actions/atlases. A 760-fight headless sample completed without reported errors; see `Verification/playtest-notes.md`. An alternate-launch GameData fallback bug was corrected with a regression check. No speculative balance changes were applied.

Remaining production work includes live pacing/audio review, dedicated elemental effects, separate DoT/reflect/hazard cues, per-hit multihit timing, unique per-item appearances and deforming skinning. The stage's shown-strike totals are not comprehensive encounter telemetry. The detailed phases below preserve the original design targets and should not be read as a claim that every production acceptance criterion is complete.

## Original phased design

Status: static stage plus prototype animation/VFX integration, September 5, 2026.
The renderer remains Avalonia. All 127 enemy definitions map to 34 reusable model
families, with static GLBs and Blender source. Thirty-six rigid-bone actors
(including the hero and preview demon) have eight clips each, exported as cropped atlases with
frame durations and trim offsets. Immutable sequence cues drive anticipation,
strike, reaction, death/victory and impact/heal/miss/defense feedback. The expanded
rigs articulate legs and use family-specific motion with stronger windups,
lunges, recoil and contact holds. Sparks, slash cores, spell charges/beams and
critical shock rings emphasize the reveal. Exact roster/resource coverage and
offline atlas-content validation guard the larger asset set.
Reduced-motion, animation and effects settings are available. Instant playback
suppresses cosmetics; manual stepping holds poses. Treat Phases 2–3 as a first
prototype, not final acceptance: interactive pacing/audio review, dedicated
DoT/reflect paths, per-hit multihit cues and polish remain open. Phases 4–5 remain
future work beyond the generic casting/effect feedback now present.
Current visual layout puts the scene beside a vertical resolve-card stack after
offscreen inspection showed an above-card layout made the scene too small.

## Target experience

A fixed-camera battle stage presents the player and enemy facing each other.
Characters anticipate, strike, react and recover automatically while the existing
HP, armor, roll, combo and narrative systems explain the outcome. Crimson impacts,
sulfur highlights, angular silhouettes and restrained ink-like effects extend the
supplied poster aesthetic. Readability takes priority over continuous spectacle.

Start with Blender-rendered animation frames in the existing Avalonia application.
Keep editable rigs and meshes for a future real-time 3D renderer. This first release
does not require an engine migration, camera controls or a real-time skeletal runtime.

## What the repository already provides

- `Code/Combat/Sequence/CombatSequenceBuilder.cs` builds presentation steps from
  already-resolved action results, including effective targets and nested retriggers.
- `Code/Combat/Sequence/CombatSequencePresenter.cs` sequences reveals, handles
  Action Lab manual stepping, releases held health displays and triggers feedback.
- `Code/Combat/Sequence/CombatSequenceStep.cs` currently carries text/math beats
  and broad cues; it needs structured visual information or a paired visual record.
- `Code/Combat/Events/CombatEventBus.cs` dispatches events synchronously and is also
  used for gameplay triggers. Its events are not a ready-made animation timeline.
- `Code/UI/Avalonia/GameCanvasControl.cs` owns custom drawing and an existing
  damage-delta refresh timer. Rendering uses character-grid layout coordinates.
- `Code/UI/Avalonia/Layout/CombatArenaHudLayout.cs` reserves the middle arena for
  resolve cards and narrative. That space must be deliberately repartitioned.
- The art kit has static models, scene-positioned GLBs and full-frame PNG layers.
  It has no animation rig, clips, sockets, texture atlas or runtime asset loader.

These observations describe the checkout inspected for this plan. Recheck the
integration points before implementation because the combat UI has active edits.

## Presentation architecture

Resolved action result → immutable visual record → existing sequence presentation
beats → animation/VFX state → Avalonia draw pass.

Proposed responsibilities and locations:

- `Code/Combat/Sequence/CombatVisualRecord.cs`: encounter/action/swing identity,
  source and effective target IDs, action visual key, outcome, actual damage/heal,
  health snapshots and structured status changes. Capture values at resolution;
  do not retain mutable actors as the playback source of truth.
- `Code/UI/Avalonia/CombatVisuals/CombatVisualDirector.cs`: one encounter-scoped
  playback coordinator. Connect to sequence reveals and encounter lifecycle,
  distinguish cosmetic elapsed time from authoritative combat progression.
- `CombatSceneState.cs`: actor pose, facing, stage position, effects and display
  snapshots. Worker-thread events enter a queue; the UI thread owns render state.
- `CombatSceneRenderer.cs`: clipped scene drawing in pixel coordinates inside the
  arena rectangle, with explicit ordering and cached bitmap resources.
- `SpriteAnimationPlayer.cs` and `CombatVfxSystem.cs`: atlas frame selection,
  attack offsets, impact flashes and a bounded collection of reusable effects.
- `CombatVisualAssetCache.cs`: encounter preload, manifest validation, fallback,
  bitmap disposal and bounded retention between rooms.
- `GameData/Visuals/`: actor, animation and action-effect manifests. Proposed
  settings belong in the existing configuration system via its singleton.

Names above are proposed new files, not existing APIs. Prefer small components
around the current presenter over a second independent combat playback system.

### Timing rules

1. Combat resolves once. Animation never rolls dice, applies damage, changes
   status duration, consumes game RNG or determines the winner.
2. Setup starts anticipation; the action reveal selects attack/cast/guard pose.
   The final damage/heal reveal commits the impact, number and displayed HP.
   Outcome accents may appear earlier, but injury feedback waits for impact.
3. Audit the current outcome SFX cue versus health-release cue. Align impact
   audio deliberately and avoid playing it once from each presentation system.
4. Misses have an attack and whiff/evade cue without injury feedback. Zero damage
   is not automatically a block: use resolved defense/outcome information.
5. Retriggers and multi-hit actions have distinct ordered swing IDs. Do not
   replay one hit for every ActionExecuted/ActionHit/ActionCritical notification.
6. Death follows the final relevant impact. Include player death, reflected
   damage, self-target effects, DoT deaths and hazards outside normal attacks.
7. Existing speed settings govern playback. Shorten anticipation/recovery at
   high speed; instant mode snaps to the final state and allocates no VFX backlog.
   Preserve important outcome ordering while dropping redundant cosmetic effects.
8. Action Lab manual mode holds the relevant pose until advancement. Exiting,
   undoing, resetting or changing characters cancels queued work and releases HP
   holds. Stale encounter IDs cannot update a new fight.
9. A visual error falls back to text presentation without stalling combat.
   Simulations, console/MCP use and disabled UI do not load sprite resources.

## Phase 1 — stage integration and layout

Deliver a static in-game scene before producing a large animation library.
Implemented: optional Appearance setting, embedded scene/actor layers, separate
scene draw pass, text-mode fallback, Action Lab prototype cast, named unmapped
enemies, vertical resolve cards, and main-window branding. Build and offscreen
render checks completed; a full interactive gameplay review remains pending.

- Add an opt-in visual combat setting and preserve text-only presentation.
- Give the arena a dedicated scene rectangle. Keep names, HP and armor above
  or beside it; use a compact resolve strip below it and retain combo controls.
  Keep expanded math/narrative available in the existing detail/log presentation.
- At narrow window sizes, reduce scene space or switch to the compact/text view;
  do not shrink combat text until it becomes unreadable.
- Establish drawing order: background, ground effects, contact shadows, actors,
  foreground VFX, HUD/text, then tooltips/menus. Shake only the scene layer.
- Add bitmap support at a deliberate canvas render pass after the background
  clear. An image behind the current opaque canvas would be hidden.
- Preload the current scene and verify aspect ratio, clipping and input routing.
- Map the prototype demon only to an explicit preview/test encounter or suitable
  enemy visual key. Unmapped enemies retain a labeled fallback.
- Update player-facing branding to Demon Fighter where touched, preserving
  project, namespace and save identities.

Acceptance: a live encounter shows the stage and correct actor identity without
covering cards, bars, combo controls or tooltips; resizing and text mode work.

## Phase 2 — animation-ready Blender pipeline

- Refine silhouettes at their actual on-screen size before adding detail.
- Build simple humanoid and demon armatures. Rigid-parent armor to bones; use
  basic skinning for deforming sections. Start with a controlled cape pose.
- Establish local ground origins, consistent scale, facing, foot pivots and
  weapon/hand/chest/head sockets. Separate weapons from body meshes.
- Author clips with named anticipation, impact and recovery markers. Export
  both inward-facing views when needed; mirroring asymmetric gear is not assumed.
- Start with 12–16 authored frames per second, while interpolating stage motion
  at the display refresh rate. Treat these as prototype settings to evaluate.
- Export transparent cropped frames into padded atlas pages. Preserve original
  frame bounds, trim offsets, ground pivots, durations and socket coordinates.
  Use consistent trim bounds or offsets so characters do not jitter.
- Export arena-only art and portal effects separately: the current background
  already contains the portal, so it cannot serve as the clean plate for every
  portal animation. Keep contact shadows separate from moving characters.
- Add deterministic rebuild and validation for missing clips, bounds, alpha,
  pivots, texture dimensions and manifest references.

Do not turn the existing full-frame actor layers into an animation sequence.
One decoded 1600×900 RGBA frame is about 5.5 MiB; 60 frames would consume about
330 MiB per actor before additional renderer copies. Size atlases by actual
screen footprint and measure decoded memory, not PNG file size.

Acceptance: both characters have stable ground alignment, repeatable exports,
and usable clip metadata. Source rigs and baked renders are saved together.

## Phase 3 — one complete animated encounter

Minimum clips for each of the two prototype combatants:

- Idle loop and ready/anticipation.
- Basic attack with recovery; use a stage lunge rather than requiring a walk cycle.
- Hit reaction, guard and evade.
- Death; player victory pose after combat has actually ended.

First VFX set:

- Sword/claw arc and a short directional impact burst.
- Guard sparks and a distinct miss/evade streak.
- Damage/heal numbers drawn as live text, with limited overlap.
- Critical accent using a larger slash and brief local contrast change.
- Contact shadows and a restrained portal pulse.

Wire this into the sequence presenter with a fake-clock testable visual director.
Ship one fighter versus one suitable enemy with hit, miss, guard, critical, death
and victory. Do not expand the asset count until this encounter stays synchronized.

Acceptance: impact, injury reaction, audio and HP reveal line up at supported
speeds; text-only and instant modes produce the same final combat results.

## Phase 4 — spells, statuses and action coverage

Add cast and self-buff clips, then reusable effect families driven by action
visual keys and structured tags. A small family library should cover many actions:

- Melee: slash, thrust, heavy strike; combo accents vary intensity and timing.
- Ranged/spell: projectile, beam or target burst; travel is presentation only.
- Heal/shield: rising bone-white/sulfur marks and a brief protective outline.
- Burn/poison/bleed: distinct flame, mote and cut shapes; persistent restrained
  indicators with tick bursts only when the corresponding result occurs.
- Stun/weaken/buff: a readable icon or silhouette accent plus existing text.
- Environment: a scene or target effect without inventing a humanoid attacker.

Colors support the poster palette, while shapes/icons distinguish effects without
relying on color alone. Define precedence: explicit action override, then effect
family/tag mapping, then generic fallback. Unknown actions remain playable.

Acceptance: representative attack, spell, heal, support, hazard and DoT paths
have correct source/target feedback, including reflected and nested outcomes.

## Phase 5 — polish, accessibility and rollout

- Provide reduced motion, screen-shake strength, flash intensity and VFX density
  settings. Reduced motion removes lunges/shake; disable large-area flashes by
  default and retain readable static impact indicators.
- Reuse the current audio system. Bind new sounds to presentation markers and
  existing volume/mute controls, with limits on simultaneous repeated sounds.
- Stop animation refresh when the scene is hidden or inactive; release assets on
  teardown. Avoid per-frame image decoding and unbounded particle allocation.
- Profile on the user's machine at representative window sizes and combat speeds.
  Initial goals: smooth 60 Hz presentation where supported, no growing queue,
  and an encounter visual working set near or below 128 MiB. These are proposed
  budgets, not measured results; revise after the first vertical slice profile.
- Add enemy archetypes and weapon variants incrementally after the shared rigs
  and rendering footprint are proven. Keep animated backgrounds sparse.
- Make visual mode the default only after parity, performance and readability
  checks pass. Retain a reliable text-only fallback.

## Verification and implementation boundaries

Automated checks should cover deterministic cue ordering, single-impact delivery,
effective targets, nested swings, death ordering, pause/manual-step cancellation,
speed changes, instant mode, stale encounters, missing assets and resource disposal.
Compare representative recorded outcomes with visuals enabled and disabled.

Use the Action Interaction Lab for visual inspection: normal hit, miss, critical,
zero damage, healing, reflected hit, retrigger chain, DoT kill and hazard kill.
Check resizing, display scaling, tooltip layering, settings overlays and repeated
room/character changes. Extend the existing presenter and layout test suites.

Before running the repository build, account for its current pre-build target,
which forcibly stops running DF processes. Use an agreed safe verification setup
so an unsaved play session is not interrupted unexpectedly.

Keep implementation changes reviewable in this order: static stage/layout;
asset loader and manifests; visual records/director; rigs and minimum clips;
first encounter VFX; extended actions/statuses; performance and rollout.

First implementation scope: Phases 1–3, one arena, one fighter, one enemy.
Complete roster coverage is now implemented with shared prototype families.
Unique named-enemy skins, real-time 3D, advanced cloth, cinematic cameras and
per-item equipment appearance remain later expansions.
