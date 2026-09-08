# Combat UI review: give the battlefield its own space

Design proposal, September 5, 2026. The illustration in the layout study uses
existing game artwork and illustrative encounter values. This review proposes
layout changes; it does not change the running game.

## Findings from the active rendering code

`Code/UI/Avalonia/CombatVisuals/BattleOverlay.cs` draws a 76-pixel permanent header
and a 60-pixel permanent footer over the scene. Together they cover 136 vertical
pixels, approximately 36% of the roughly 380-pixel battlefield in the supplied
screenshot. Status badges start 101 pixels above the bottom, and enemy intent
adds another banner during preparation. These are additive sources of crowding.

`CombatArenaHudLayout` separately reserves five character rows for the enemy HUD
and eight for the fighter HUD/log. Its illustrated layout gives the scene only
two thirds of the remaining center width; the resolve stack occupies the rest.
`LayoutConstants` also reserves an eleven-row combo strip and a three-row
sequence band. These grid-row reservations cannot be added directly to the
pixel overlay measurements without the current font metrics.

The side panels already contain hero identity, stats, gear, thresholds, status
effects, location and enemy details. During active combat, their health bars are
explicitly suppressed in favor of center HUD bars. Simply removing the overlay
will recover visible art but will not reclaim these other reservations.

## Recommended layout

Use the existing left/right columns as compact combatant panels and give the
entire center column to the battlefield. Put permanent information outside the
image rectangle. Size the image uniformly with its original aspect ratio; do
not crop the gateway or stretch the characters to fill the reclaimed space.

The left panel owns the fighter's identity, HP, armor, effects and played-card
stack. The right panel owns the enemy's identity, rank, role, HP, armor, effects,
intent and played-card stack. Card descriptions remain readable in both stacks.
Only their latest card and a small history indicator are expanded by default.

A slim top bar owns dungeon/room, pause and speed. A compact readiness sequence
sits beneath that bar, outside the scene. A bottom dock owns the equipped combo
and the latest combat result, with an expandable log. The battlefield retains
damage numbers, hit flashes, spell effects and brief status application cues.

## Destination for each active information group

- Hero name, level and class: fighter summary. XP and class-point breakdown move
  to its expandable details; show XP gains prominently at encounter completion.
- Weapon, armor and rarity: one fighter equipment line; item inspection exposes
  full gear and modifications. Remove the duplicate equipment line over the art.
- Fighter and enemy HP/armor: respective side summaries. Restore their side bars
  during illustrated combat and remove the dedicated center copies together.
  Preserve delayed HP reveal behavior so bars still land on the damage beat.
- Status effects and immunities: each combatant's summary, with durations and
  values accessible by click/keyboard. An overflow indicator expands the complete
  list. Retain brief particles on the affected actor, but remove persistent
  status labels from the battlefield.
- Enemy rank and role: enemy identity line. Enemy preparation/intent goes on
  the enemy's current action card; it must remain visible while the attack winds
  up. Do not infer a future action before the combat system commits one.
- Readiness and room hazards: one compact ordered strip including the hazard
  whenever it is active. Preserve tie ordering and pause behavior.
- Pause, speed and keyboard hints: top controls. Keep P and PgUp/PgDn working,
  and make visible controls clickable and keyboard-accessible.
- Attacker/roll/outcome/action/defense/damage/effects sequence header: consolidate
  into the resolving card and a compact result line. Roll, armor calculation,
  effects and threshold breakdown stay available in expanded resolution details.
- Fighter and enemy narrative areas: one shared chronological log. Keep the
  latest result visible; expand history on demand instead of keeping two large
  permanent log boxes. Preserve actor attribution and scroll position.
- Played action cards: fighter stack left, enemy stack right. Keep descriptions,
  result labels, preparation state, impact highlight and separate histories.
  Avoid allocating a third central card column or showing a tall empty pile.
- Equipped combo: existing bottom dock, separate from played history. Render
  only actual actions plus a compact slot-capacity indicator. Select an action
  for its description; expand the editor between encounters. Preserve the
  encounter reorder lock and its hit-testing.
- Build advice: equipment/combo inspection and relevant between-room moments.
  Persistent generic tips should not occupy the combat image or log.
- Detailed STR/AGI/TECH/INT, damage/speed, probabilities and gear: expandable
  fighter/enemy inspection sections, with an optional persistent detailed mode
  for players studying the combat math.
- Victory/defeat and encounter totals: result dock after the finishing animation.
  Use authoritative encounter totals rather than the visual director's partial
  "shown strikes" totals. Allow inspection of the final battlefield and log.
- Menus, inventory, room choice and Action Lab: use their own layout policy.
  Preserve full equipment comparisons and lab controls; do not apply combat's
  compact panel policy indiscriminately to those screens.

## Alternative: paired JRPG dock

For narrower windows, place the battlefield above two paired combatant panels.
Each panel retains its HP/effects and latest described action card. Detailed
stats and older cards expand below. This gains horizontal space but consumes
more vertical space, so it should be a responsive alternative rather than the
default for a wide desktop window. Reflow at a measured minimum readable card
width, not merely a hard-coded screen resolution.

The layout study offers both arrangements through its layout design control.
At narrow widths the side-panel option also reflows below the battlefield.
Pause/speed, combo inspection, details and history are local prototype interactions.

## References and interpretation

- Pokémon Legends: Arceus provides a useful reference for making changing action
  order understandable. Its official gameplay page explains variable action
  order and the speed/power tradeoff of agile and strong styles:
  https://legends.arceus.pokemon.com/en-us/gameplay/
- Dragon Quest XI's official battle gallery provides 2D/3D and auto-camera battle
  references: https://www.dq11.jp/system/battle.html

Our design interpretation is to separate combatant summaries, action explanation
and the battle view. Borrow information hierarchy and emphasis, while retaining
Demon Fighter's auto-battle pacing, two played-card histories and existing art.
Manual four-move command selection is not appropriate to copy into this game.

## Implementation order and acceptance checks

1. Define one illustrated-combat layout result for scene, combatant summaries,
   readiness, card stacks, combo dock and log. Use it for rendering, clearing,
   hover regions and hit-testing. Retain a text-mode layout fallback.
2. Move information ownership before deleting duplicates. Relocate permanent
   overlay text, restore side health bars, move stacks, and then remove the
   center HUD/card reservations. Separate transient scene VFX from HUD drawing.
3. Compact the combo/log regions and add responsive reflow and inspection states.
   Preserve the current HP reveal, action preparation and card-result timing.
4. Render complete combat screens at 1280×800 and 1920×1080, including scaled
   fonts. Check long names/descriptions, one-action and full combos, many effects,
   hazard ties, elite enemies, no history, pause, instant mode, reduced motion,
   death/victory, resize, inventory transition and Action Lab.

Success means no permanent text rectangle intersects the battlefield, no health
or intent information disappears, both card descriptions remain accessible,
and the actor/portal silhouettes fit in the larger scene. Measure the resulting
image rectangle at each viewport; do not promise a fixed enlargement percentage
before the font-dependent layout has been implemented.
