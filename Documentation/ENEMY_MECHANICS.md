# Enemy Mechanics

Status: 2026-10-12 (overnight build pass). Knob: `enemySystem.actionKits` (on in `overnight-v2`, off in `overnight-v1`).

## Why

`Enemies.json` lists 238 distinct action names, but 229 of them did not exist in `Actions.json`. `EnemyActionManager`
turned every missing name into a plain attack, so a Spider's POISON BITE, a Fire Elemental's FIRE BOLT and a Lich's
Raise Dead all did the same thing. Enemies differed only by speed / hit size / HP (identity mode).

## How it works

- `GameData/EnemyActions.json` holds **15 mechanic templates** (rows in the `Actions.json` format, enemy only) and an
  **alias table** from each flavor name in `Enemies.json` to a template.
- With `enemySystem.actionKits` on, a flavor name that is not in `Actions.json` resolves through the catalog: the enemy
  keeps the flavor name ("Toxic Dart") and gets the template's mechanics (`EnemyActionCatalog.TryCreate`).
- The templates live outside `Actions.json` (sheet-owned, used for hero loot), so they never become hero gear actions.
- Every template names the player's answer, so enemy kits give defensive and utility skills a job.
- `enemySystem.identity.calibration` was re-solved with kits on (`Scripts/enemy-calibrate.py`), so every enemy still
  costs about the same HP at each level; kits change *how* a fight is lost, not how often.

## Templates

| Template | Mechanics | Player answer | Enemies |
|---|---|---|---|
| Venom | 85% dmg, speed 1.00; poison 0.5%/tick on connect | kill fast or out-heal the ticks. | 14 (Amber Trapper, Hollow Drifter, Lion Fish, Magma Creeper…) |
| Scorch | 85% dmg, speed 1.00; burn on connect | kill fast; Harden/Fortify blunt the follow-up. | 29 (Ashwing Harpy, Bridge Warden, Cave Ember Toad, Cave Lurcher…) |
| Rend | 85% dmg, speed 1.00; bleed on connect | fewer, bigger actions. | 12 (Alpha Wolf, Ashwing Harpy, Charwood Stalker, Edge Stalker…) |
| Crush | 170% dmg, speed 1.60 | armor, Harden, Fortify. | 18 (Aqueduct Warden, Boar, Cascade Rider, Current Rider…) |
| Sunder | 90% dmg, speed 1.00; expose=1 | Fortify, kill before it stacks. | 11 (Aqueduct Warden, Blood Feeder, Deserter Shade, Fourecourt Guard…) |
| Hex | 80% dmg, speed 1.00; weaken=1 | cheap openers, multi-slot strips. | 11 (Ancestor's Shade, Bear, Cursed Bear, Instinct Eater…) |
| Ensnare | 85% dmg, speed 1.00; slow=1 | speed, AGI, Bronze tempo. | 16 (Amber Sentinel, Amber Trapper, Chronicle Watcher, Cold Creeper…) |
| Disrupt | 90% dmg, speed 1.00; disrupt=1 | Iron Discipline, Unbroken Line, short strips. | 26 (Dead End Mimic, Duplicate, Echo Stalker, Flame Drifter…) |
| Shell | 70% dmg, speed 1.00; harden=1 | Pierce, Expose, Vulnerability. | 12 (Amber Sentinel, Bridge Warden, Dune Roller, Guardian Scarab…) |
| Drain | 90% dmg, speed 1.00; lifesteal=50% | burst, Weaken. | 9 (Blood Feeder, Bone Spreader, Crystal Leech, Depth Drainer…) |
| Frenzy | 40% dmg, speed 1.00; 3 hits | armor (it applies to every hit). | 7 (Alpha Wolf, Cascade Rider, Ember Spirit, Glass Shard Golem…) |
| Mend | 60% dmg, speed 1.00; lifesteal=100% | steady pressure. | 7 (Animated Vine, Burial Lich, Loop Shade, Moon Blessed…) |
| Curse | 80% dmg, speed 1.00; vulnerability=1 | kill it first; Fortify. | 12 (Ancestor's Shade, Bone Spreader, Burial Lich, Chronicle Watcher…) |
| Concuss | 105% dmg, speed 1.00; stun on crit | crit threshold debuffs, Lucky. | 16 (Altitude Wraith, Animated Vine, Cave Lurcher, Charwood Stalker…) |
| Pierce | 95% dmg, speed 1.00; pierce=1 | HP, healing, Fortify. | 6 (Altitude Wraith, Crystal Leech, Glare Stalker, Hawk…) |

## Editing

- Retarget a flavor action: change its entry in `aliases`.
- New mechanic: add a template row (same columns as `Actions.json`; DoTs need `triggerConditions` such as
  `ONCONNECT`, or they only fire on crits) and point aliases at it. Poison, burn, bleed and stun must be trigger bundles
  (`triggerBundlesJson`, as on items): the sheet columns for them are item-only and ignored on actions.
- After editing, re-run `python Scripts/enemy-calibrate.py --patch overnight-v2` and WRSOLVE for the damage curve.

## Open

- Enemy Venom adds 0.5% max HP per tick per connect (trigger bundle value). It stacks for the whole fight, so poison
  enemies punish long fights.
- 60 names had no keyword match and use their archetype's default template; review those aliases by hand.
