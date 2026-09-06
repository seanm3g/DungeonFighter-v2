# UI polish pass

- Solid ink-colored action cards with a neutral archive border and colored resolving border.
- Explicit resolving/last-played headers, clearer description spacing, and compact result text without a redundant UNUSED line.
- Readiness and keyboard controls backed by a dark stage header; enemy intent uses a separate warning banner.
- Removed the overlapping stage title, added a fine scene frame, and moved projectile rendering beneath the HUD.
- Nine bounded portal motes add movement without consuming combat RNG or accumulating particles. Disabled for instant/reduced-motion/VFX-off modes and while paused.
- Shorter default build guidance.

Validation: clean isolated build; CombatSceneRender, FighterResolveActionStack, CanvasPrimitiveStacking, CombatSceneGeometry and CombatExperience passed. Native offscreen captures at 1280x800 and 1920x1080 verify card bounds and presentation. These captures use fixture combatants and do not replace live gameplay review.

Previews: battle-1280.png and battle-1920.png in this directory.
