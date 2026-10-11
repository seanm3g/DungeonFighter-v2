# DungeonFighter → ComfyUI combat scene feed

While the Avalonia game is running, DungeonFighter serves live combat-scene JSON for image pipelines.

## Feed URL

Default base: `http://127.0.0.1:7877/`

| Path | Returns |
|---|---|
| `/health` | `{ ok, service, revision, port, status }` |
| `/combat-scene` | Full envelope (`revision`, `latest`, `scenes`, …) |
| `/combat-scene/latest` | Latest beat object only |
| `/combat-scene/scenes` | Scene array (row 0 = empty room establishing shot) |
| `/combat-scene/wait?since=N&timeoutMs=25000` | Long-poll until revision advances |

File mirrors (same data):

- `GameData/CombatLogs/live.json` — envelope
- `GameData/CombatLogs/live-latest.json` — latest beat

Env knobs:

- `DF_COMBAT_SCENE_FEED=0` — disable the HTTP server
- `DF_COMBAT_SCENE_FEED_PORT=7877` — change port

## Install the ComfyUI node

1. Copy or symlink this folder into ComfyUI:

   `ComfyUI/custom_nodes/dungeonfighter_combat_scene/`

2. Restart ComfyUI.
3. Add node **DF Combat Scene (Live Feed)** (category **DungeonFighter**).
4. Start DungeonFighter, enter combat, queue the node.

### Node tips

- Leave **wait_for_new** on and wire **revision** back into **since_revision** (or bump it each run) so ComfyUI blocks until the next swing.
- Use **prompt** (assembled fields) or individual STRING outputs (`environment`, `camera`, `composition`, …).
- Generate **scenes[0]** (establishing empty room) first, then img2img later beats with denoise ~0.35–0.55 so the room stays locked and fighters stay in the far corner.

## Beat schema

Same as right-click combat-log export:

`character`, `action`, `target`, `environment`, `emotion`, `camera`, `composition`, `gear`, `effects`
